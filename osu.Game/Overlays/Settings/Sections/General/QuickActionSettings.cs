// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
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

        [Resolved]
        private RinariBeatmapStatusSynchroniser rinariStatusSynchroniser { get; set; } = null!;

        private bool statusSyncRunning;

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
                TooltipText = "Checks every local beatmapset against Rinari and marks missing ones as UNKNOWN. "
                             + "Importing or downloading a beatmap already re-checks that one automatically; use this to catch up "
                             + "everything else too, e.g. after ranking maps you already had.",
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
            // the synchroniser itself no-ops overlapping runs; this flag is only to keep the button from posting
            // a second "syncing..." notification while one is already in flight.
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

            RinariSyncResult? result = await rinariStatusSynchroniser.SyncNowAsync().ConfigureAwait(true);

            statusSyncRunning = false;

            if (result == null)
            {
                notification.Text = "Rinari beatmap status sync failed.";
                notification.State = ProgressNotificationState.Cancelled;
                return;
            }

            notification.CompletionText = result.ToCompletionText();
            notification.Progress = 1;
            notification.State = ProgressNotificationState.Completed;
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
