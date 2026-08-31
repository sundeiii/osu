// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Online.Chat;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Screens.Select;
using osu.Game.Utils;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;

namespace osu.Game.Overlays.Settings.Sections.General
{
    public partial class QuickActionSettings : SettingsSubsection
    {
        [Resolved(CanBeNull = true)]
        private FirstRunSetupOverlay? firstRunSetupOverlay { get; set; }

        [Resolved(CanBeNull = true)]
        private OsuGame? game { get; set; }

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private bool statusSyncRunning;

        private class RinariBeatmapStatus
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;
        }

        private class RinariBeatmapSetStatus
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("beatmaps")]
            public RinariBeatmapStatus[] Beatmaps { get; set; } =
                Array.Empty<RinariBeatmapStatus>();
        }

        protected override LocalisableString Header => GeneralSettingsStrings.QuickActionsHeader;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, Storage storage, IDialogOverlay? dialogOverlay)
        {

            AddRange(new Drawable[]
            {
                new SettingsButtonV2
                {
                    Text = GeneralSettingsStrings.RunSetupWizard,
                    Keywords = new[] { @"first run", @"initial", @"getting started", @"import", @"tutorial", @"recommended beatmaps" },
                    TooltipText = FirstRunSetupOverlayStrings.FirstRunSetupDescription,
                    Action = () => firstRunSetupOverlay?.Show(),
                },
                new SettingsButtonV2
                {
                    Text = GeneralSettingsStrings.LearnMoreAboutLazer,
                    TooltipText = GeneralSettingsStrings.LearnMoreAboutLazerTooltip,
                    BackgroundColour = colours.YellowDark,
                    Action = () => game?.ShowWiki(@"Help_centre/Upgrading_to_lazer")
                },
                new SettingsButtonV2
                {
                    Text = GeneralSettingsStrings.ReportIssue,
                    TooltipText = GeneralSettingsStrings.ReportIssueTooltip,
                    BackgroundColour = colours.YellowDarker,
                    Action = () => dialogOverlay?.Push(new IssueReportDialog(() =>
                        game?.OpenUrlExternally(@"https://github.com/sundeiii/osu/issues", LinkWarnMode.NeverWarn)
                    )),
                },
            });

            Add(new SettingsButtonV2
            {
                Text = "Sync Rinari beatmap statuses",
                TooltipText = "Checks all local beatmapsets against Rinari and marks missing ones as UNKNOWN.",
                BackgroundColour = colours.YellowDarker.Darken(0.5f),
                Keywords = new[] { @"rinari", @"beatmap", @"status", @"sync", @"cache", @"missing", @"ranked", @"unknown" },
                Action = syncBeatmapStatuses,
            });

            Add(new SettingsButtonV2
            {
                Text = GeneralSettingsStrings.ExportLogs,
                BackgroundColour = colours.YellowDarker.Darken(0.5f),
                Keywords = new[] { @"bug", "report", "logs", "files" },
                Action = () => Task.Run(exportLogs),
            });

            exportStorage = (storage as OsuStorage)?.GetExportStorage() ?? storage.GetStorageForDirectory(@"exports");
        }

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        private Storage exportStorage = null!;

        private async void syncBeatmapStatuses()
        {
            if (statusSyncRunning)
                return;

            statusSyncRunning = true;

            ProgressNotification notification = new ProgressNotification
            {
                State = ProgressNotificationState.Active,
                Text = "Syncing Rinari beatmap statuses...",
                Progress = 0,
            };

            notifications?.Post(notification);

            try
            {
                using var http = new HttpClient();

                string json = await http.GetStringAsync(
                    "https://lazer-api.rinarii.de/api/v2/rinari/beatmapset-statuses"
                ).ConfigureAwait(false);

                var serverStatuses =
                    JsonSerializer.Deserialize<RinariBeatmapSetStatus[]>(json)
                    ?? Array.Empty<RinariBeatmapSetStatus>();

                Dictionary<int, BeatmapOnlineStatus> statusBySetId = new();
                Dictionary<int, BeatmapOnlineStatus> statusByBeatmapId = new();

                foreach (var item in serverStatuses)
                {
                    if (Enum.TryParse(
                            item.Status,
                            true,
                            out BeatmapOnlineStatus parsedSetStatus))
                    {
                        statusBySetId[item.Id] = parsedSetStatus;
                    }

                    foreach (var beatmap in item.Beatmaps)
                    {
                        if (Enum.TryParse(
                                beatmap.Status,
                                true,
                                out BeatmapOnlineStatus parsedBeatmapStatus))
                        {
                            statusByBeatmapId[beatmap.Id] = parsedBeatmapStatus;
                        }
                    }
                }

                Schedule(() =>
                {
                    try
                    {
                        int onlineSets = 0;
                        int missingSets = 0;
                        int skippedSets = 0;
                        int onlineBeatmaps = 0;
                        int missingBeatmaps = 0;
                        int updatedBeatmapSets = 0;
                        int updatedBeatmaps = 0;

                        realm.Write(r =>
                        {
                            foreach (var beatmapSet in r.All<BeatmapSetInfo>().AsEnumerable())
                            {
                                int setId = beatmapSet.OnlineID;

                                if (setId <= 0)
                                {
                                    skippedSets++;
                                    continue;
                                }

                                BeatmapOnlineStatus setStatus;

                                if (statusBySetId.TryGetValue(setId, out var serverSetStatus))
                                {
                                    setStatus = serverSetStatus;
                                    onlineSets++;

                                    lock (SongSelect.MissingServerBeatmapSets)
                                        SongSelect.MissingServerBeatmapSets.Remove(setId);
                                }
                                else
                                {
                                    setStatus = BeatmapOnlineStatus.None;
                                    missingSets++;

                                    lock (SongSelect.MissingServerBeatmapSets)
                                        SongSelect.MissingServerBeatmapSets.Add(setId);
                                }

                                if (beatmapSet.Status != setStatus)
                                {
                                    beatmapSet.Status = setStatus;
                                    updatedBeatmapSets++;
                                }

                                if (setStatus == BeatmapOnlineStatus.None)
                                    beatmapSet.DateRanked = null;

                                foreach (var beatmap in beatmapSet.Beatmaps)
                                {
                                    BeatmapOnlineStatus beatmapStatus;

                                    if (setStatus == BeatmapOnlineStatus.None)
                                    {
                                        beatmapStatus = BeatmapOnlineStatus.None;
                                        missingBeatmaps++;
                                    }
                                    else if (beatmap.OnlineID > 0
                                             && statusByBeatmapId.TryGetValue(
                                                 beatmap.OnlineID,
                                                 out var serverBeatmapStatus))
                                    {
                                        beatmapStatus = serverBeatmapStatus;
                                        onlineBeatmaps++;
                                    }
                                    else
                                    {
                                        beatmapStatus = BeatmapOnlineStatus.None;
                                        missingBeatmaps++;
                                    }

                                    if (beatmap.Status != beatmapStatus)
                                    {
                                        beatmap.Status = beatmapStatus;
                                        updatedBeatmaps++;
                                    }
                                }
                            }
                        });

                        notification.CompletionText =
                            $"Rinari status sync finished. " +
                            $"Sets: {onlineSets} online, {missingSets} missing, {skippedSets} without ID. " +
                            $"Beatmaps: {onlineBeatmaps} online, {missingBeatmaps} missing. " +
                            $"Updated: {updatedBeatmapSets} sets, {updatedBeatmaps} beatmaps.";

                        notification.Progress = 1;
                        notification.State = ProgressNotificationState.Completed;
                    }
                    catch (Exception e)
                    {
                        Logger.Log(
                            $"Rinari beatmap status Realm sync failed: {e}",
                            LoggingTarget.Runtime,
                            LogLevel.Important
                        );

                        notification.Text = "Rinari beatmap status sync failed.";
                        notification.State = ProgressNotificationState.Cancelled;
                    }
                    finally
                    {
                        statusSyncRunning = false;
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Log(
                    $"Rinari beatmap status sync failed: {e}",
                    LoggingTarget.Runtime,
                    LogLevel.Important
                );

                Schedule(() =>
                {
                    notification.Text = "Rinari beatmap status sync failed.";
                    notification.State = ProgressNotificationState.Cancelled;
                    statusSyncRunning = false;
                });
            }
        }

        private void exportLogs()
        {
            ProgressNotification notification = new ProgressNotification
            {
                State = ProgressNotificationState.Active,
                Text = NotificationsStrings.LogsExportOngoing,
            };

            notifications?.Post(notification);

            const string archive_filename = "compressed-logs.zip";

            try
            {
                GlobalStatistics.OutputToLog();
                Logger.Flush();

                var logStorage = Logger.Storage;

                using (var outStream = exportStorage.CreateFileSafely(archive_filename))
                using (var zip = ZipArchive.CreateArchive())
                {
                    foreach (string? f in logStorage.GetFiles(string.Empty, "*.log"))
                        FileUtils.AttemptOperation(z => z.AddEntry(f, logStorage.GetStream(f), closeStream: true), zip, throwOnFailure: false);

                    zip.SaveTo(outStream, new ZipWriterOptions(CompressionType.Deflate));
                }
            }
            catch
            {
                notification.State = ProgressNotificationState.Cancelled;

                // cleanup if export is failed or canceled.
                exportStorage.Delete(archive_filename);
                throw;
            }

            notification.CompletionText = NotificationsStrings.LogsExportFinished;
            notification.CompletionClickAction = () => exportStorage.PresentFileExternally(archive_filename);

            notification.State = ProgressNotificationState.Completed;
        }
    }
}
