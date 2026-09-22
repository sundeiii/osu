// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Database;
using osu.Game.Screens.Select;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Keeps locally imported beatmaps' online status in sync with the Rinari server, which uses its own
    /// ranked/loved/graveyard statuses rather than the standard osu! ones the ordinary metadata lookup understands.
    /// </summary>
    /// <remarks>
    /// This is queued automatically whenever a beatmap is imported or otherwise processed with online lookups enabled
    /// (see <see cref="QueueAutoSync"/>), and can also be triggered directly, e.g. from a settings button.
    /// </remarks>
    public partial class RinariBeatmapStatusSynchroniser : Component
    {
        private const string status_endpoint = "https://lazer-api.rinarii.de/api/v2/rinari/beatmapset-statuses";

        /// <summary>
        /// How long to wait after the last queued request before actually syncing, so that importing many
        /// beatmaps in a row (e.g. a batch download) results in one sync rather than one per beatmap.
        /// </summary>
        private const double debounce_delay = 3000;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        /// <summary>
        /// Sets the <see cref="RealmAccess"/> directly, bypassing DI. For use by tests that construct this
        /// component outside of a dependency container.
        /// </summary>
        internal void SetRealmForTesting(RealmAccess value) => realm = value;

        /// <summary>
        /// Fetches the raw statuses JSON. Overridable for testing; defaults to the real HTTP request.
        /// </summary>
        internal Func<CancellationToken, Task<string>> FetchStatusesJson { get; set; }

        private readonly HttpClient http = new HttpClient();

        // serialises runs rather than dropping them, so a manual "sync now" click that lands while an
        // automatic sync is already in flight waits its turn instead of being reported as a failure.
        private readonly SemaphoreSlim runLock = new SemaphoreSlim(1, 1);

        private ScheduledDelegate? queuedSync;

        // beatmapset online IDs an automatic sync should actually touch, accumulated across a debounce burst.
        // null means "touch everything" (a full sync was requested at some point during the burst).
        private readonly object pendingLock = new object();
        private HashSet<int>? pendingScope = new HashSet<int>();

        public RinariBeatmapStatusSynchroniser()
        {
            FetchStatusesJson = token => http.GetStringAsync(status_endpoint, token);
        }

        /// <summary>
        /// Queues a sync of every locally known beatmapset, debounced against other calls to this method or
        /// <see cref="QueueAutoSync(System.Collections.Generic.IReadOnlyCollection{int})"/>.
        /// </summary>
        public void QueueAutoSync()
        {
            lock (pendingLock)
                pendingScope = null;

            scheduleDebounced();
        }

        /// <summary>
        /// Queues a sync of only the given beatmapsets (by online ID), debounced and merged with any other
        /// pending request, so it never touches the rest of the library.
        /// </summary>
        public void QueueAutoSync(IReadOnlyCollection<int> beatmapSetOnlineIds)
        {
            if (beatmapSetOnlineIds.Count == 0)
                return;

            lock (pendingLock)
                pendingScope?.UnionWith(beatmapSetOnlineIds);

            scheduleDebounced();
        }

        private void scheduleDebounced()
        {
            queuedSync?.Cancel();
            queuedSync = Scheduler.AddDelayed(() =>
            {
                HashSet<int>? scope;

                lock (pendingLock)
                {
                    scope = pendingScope;
                    pendingScope = new HashSet<int>();
                }

                if (scope != null && scope.Count == 0)
                    return;

                runSync(quiet: true, scope);
            }, debounce_delay);
        }

        /// <summary>
        /// Runs a sync of every locally known beatmapset now, skipping the debounce (but still waiting for any
        /// sync already in progress). Intended for a manual "sync now" action.
        /// </summary>
        /// <returns>A summary of the sync, or <see langword="null"/> if it failed.</returns>
        public Task<RinariSyncResult?> SyncNowAsync()
        {
            queuedSync?.Cancel();
            return runSync(quiet: false, scope: null);
        }

        /// <summary>
        /// Runs a scoped sync immediately, bypassing the debounce/<see cref="Scheduler"/> queueing that
        /// <see cref="QueueAutoSync(System.Collections.Generic.IReadOnlyCollection{int})"/> normally goes through.
        /// For testing only, so the scoping logic can be verified without a running <see cref="Scheduler"/>.
        /// </summary>
        internal Task<RinariSyncResult?> SyncScopedForTesting(IReadOnlyCollection<int> beatmapSetOnlineIds) =>
            runSync(quiet: false, new HashSet<int>(beatmapSetOnlineIds));

        /// <param name="quiet">Whether to skip logging if nothing changed.</param>
        /// <param name="scope">Beatmapset online IDs to limit the sync to, or <see langword="null"/> for all of them.</param>
        private async Task<RinariSyncResult?> runSync(bool quiet, IReadOnlySet<int>? scope)
        {
            await runLock.WaitAsync().ConfigureAwait(false);

            try
            {
                string json = await FetchStatusesJson(CancellationToken.None).ConfigureAwait(false);
                var serverStatuses = RinariBeatmapSetStatus.ParseAll(json);

                // explicitly off the calling thread (which may be the game's update thread, e.g. when this
                // was queued via QueueAutoSync) so that iterating and writing local beatmapsets never costs a frame.
                // the server can only be asked for its whole catalogue, but a non-null scope keeps the write itself
                // (and so what the local library visibly changes) limited to the beatmapsets that asked for it.
                var result = await Task.Run(() => applyToRealm(serverStatuses, scope)).ConfigureAwait(false);

                if (!quiet || result.UpdatedBeatmapSets > 0 || result.UpdatedBeatmaps > 0)
                {
                    Logger.Log(
                        $"Rinari status sync finished. Sets: {result.OnlineSets} online, {result.MissingSets} missing, {result.SkippedSets} without ID. "
                        + $"Beatmaps: {result.OnlineBeatmaps} online, {result.MissingBeatmaps} missing. "
                        + $"Updated: {result.UpdatedBeatmapSets} sets, {result.UpdatedBeatmaps} beatmaps.",
                        LoggingTarget.Runtime);
                }

                return result;
            }
            catch (Exception e)
            {
                Logger.Log($"Rinari beatmap status sync failed: {e}", LoggingTarget.Runtime, LogLevel.Important);
                return null;
            }
            finally
            {
                runLock.Release();
            }
        }

        /// <summary>
        /// The managed thread ID <see cref="applyToRealm"/> last ran on. For testing only, to confirm it never
        /// runs on whichever thread called <see cref="SyncNowAsync"/> or <see cref="QueueAutoSync"/>.
        /// </summary>
        internal int? LastApplyThreadIdForTesting { get; private set; }

        private RinariSyncResult applyToRealm(IReadOnlyList<RinariBeatmapSetStatus> serverStatuses, IReadOnlySet<int>? scope)
        {
            LastApplyThreadIdForTesting = Environment.CurrentManagedThreadId;

            var statusBySetId = new Dictionary<int, BeatmapOnlineStatus>();
            var statusByBeatmapId = new Dictionary<int, BeatmapOnlineStatus>();

            foreach (var item in serverStatuses)
            {
                if (Enum.TryParse(item.Status, true, out BeatmapOnlineStatus parsedSetStatus))
                    statusBySetId[item.Id] = parsedSetStatus;

                foreach (var beatmap in item.Beatmaps)
                {
                    if (Enum.TryParse(beatmap.Status, true, out BeatmapOnlineStatus parsedBeatmapStatus))
                        statusByBeatmapId[beatmap.Id] = parsedBeatmapStatus;
                }
            }

            int onlineSets = 0, missingSets = 0, skippedSets = 0;
            int onlineBeatmaps = 0, missingBeatmaps = 0;
            int updatedBeatmapSets = 0, updatedBeatmaps = 0;

            realm.Write(r =>
            {
                var beatmapSets = scope == null
                    ? r.All<BeatmapSetInfo>().AsEnumerable()
                    : r.All<BeatmapSetInfo>().AsEnumerable().Where(s => scope.Contains(s.OnlineID));

                foreach (var beatmapSet in beatmapSets)
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
                        else if (beatmap.OnlineID > 0 && statusByBeatmapId.TryGetValue(beatmap.OnlineID, out var serverBeatmapStatus))
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

            return new RinariSyncResult(onlineSets, missingSets, skippedSets, onlineBeatmaps, missingBeatmaps, updatedBeatmapSets, updatedBeatmaps);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            http.Dispose();
            runLock.Dispose();
        }

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
            public RinariBeatmapStatus[] Beatmaps { get; set; } = Array.Empty<RinariBeatmapStatus>();

            public static RinariBeatmapSetStatus[] ParseAll(string json)
                => System.Text.Json.JsonSerializer.Deserialize<RinariBeatmapSetStatus[]>(json) ?? Array.Empty<RinariBeatmapSetStatus>();
        }
    }

    /// <summary>
    /// A summary of what a <see cref="RinariBeatmapStatusSynchroniser"/> sync run found and changed.
    /// </summary>
    public record RinariSyncResult(
        int OnlineSets,
        int MissingSets,
        int SkippedSets,
        int OnlineBeatmaps,
        int MissingBeatmaps,
        int UpdatedBeatmapSets,
        int UpdatedBeatmaps)
    {
        public string ToCompletionText() =>
            $"Rinari status sync finished. "
            + $"Sets: {OnlineSets} online, {MissingSets} missing, {SkippedSets} without ID. "
            + $"Beatmaps: {OnlineBeatmaps} online, {MissingBeatmaps} missing. "
            + $"Updated: {UpdatedBeatmapSets} sets, {UpdatedBeatmaps} beatmaps.";
    }
}
