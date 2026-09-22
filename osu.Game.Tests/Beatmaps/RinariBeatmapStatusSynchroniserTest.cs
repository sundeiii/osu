// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Screens.Select;
using osu.Game.Tests.Database;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class RinariBeatmapStatusSynchroniserTest : RealmTest
    {
        [Test]
        public void TestKnownSetsAndBeatmapsGetTheirServerStatus()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                int setId = 0;
                int[] beatmapIds = null!;

                await realm.WriteAsync(r =>
                {
                    var beatmapSet = TestResources.CreateTestBeatmapSetInfo(2);
                    r.Add(beatmapSet);

                    setId = beatmapSet.OnlineID;
                    beatmapIds = beatmapSet.Beatmaps.OrderBy(b => b.OnlineID).Select(b => b.OnlineID).ToArray();
                });

                string json = $$"""
                                 [{"id":{{setId}},"status":"Ranked","beatmaps":[
                                     {"id":{{beatmapIds[0]}},"status":"Ranked"},
                                     {"id":{{beatmapIds[1]}},"status":"Loved"}
                                 ]}]
                                 """;

                var sync = createSynchroniser(realm, json);
                var result = await sync.SyncNowAsync();

                Assert.That(result, Is.Not.Null);
                Assert.That(result!.OnlineSets, Is.EqualTo(1));
                Assert.That(result.OnlineBeatmaps, Is.EqualTo(2));
                Assert.That(result.UpdatedBeatmapSets, Is.EqualTo(1));
                Assert.That(result.UpdatedBeatmaps, Is.EqualTo(2));

                realm.Run(r =>
                {
                    var beatmapSet = r.All<BeatmapSetInfo>().Single(s => s.OnlineID == setId);
                    Assert.That(beatmapSet.Status, Is.EqualTo(BeatmapOnlineStatus.Ranked));

                    var ordered = beatmapSet.Beatmaps.OrderBy(b => b.OnlineID).ToArray();
                    Assert.That(ordered[0].Status, Is.EqualTo(BeatmapOnlineStatus.Ranked));
                    Assert.That(ordered[1].Status, Is.EqualTo(BeatmapOnlineStatus.Loved));
                });
            });
        }

        [Test]
        public void TestUnknownSetGetsMarkedNoneAndFlaggedMissing()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                int setId = 0;

                await realm.WriteAsync(r =>
                {
                    var beatmapSet = TestResources.CreateTestBeatmapSetInfo(1);
                    r.Add(beatmapSet);
                    setId = beatmapSet.OnlineID;
                });

                // the server doesn't know about this set at all.
                var sync = createSynchroniser(realm, "[]");
                var result = await sync.SyncNowAsync();

                Assert.That(result, Is.Not.Null);
                Assert.That(result!.MissingSets, Is.EqualTo(1));
                Assert.That(result.MissingBeatmaps, Is.EqualTo(1));

                lock (SongSelect.MissingServerBeatmapSets)
                    Assert.That(SongSelect.MissingServerBeatmapSets, Does.Contain(setId));

                realm.Run(r =>
                {
                    var beatmapSet = r.All<BeatmapSetInfo>().Single(s => s.OnlineID == setId);
                    Assert.That(beatmapSet.Status, Is.EqualTo(BeatmapOnlineStatus.None));
                    Assert.That(beatmapSet.DateRanked, Is.Null);
                    Assert.That(beatmapSet.Beatmaps.Single().Status, Is.EqualTo(BeatmapOnlineStatus.None));
                });
            });
        }

        [Test]
        public void TestPreviouslyMissingSetIsUnflaggedOnceFound()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                int setId = 0;
                int beatmapId = 0;

                await realm.WriteAsync(r =>
                {
                    var beatmapSet = TestResources.CreateTestBeatmapSetInfo(1);
                    r.Add(beatmapSet);
                    setId = beatmapSet.OnlineID;
                    beatmapId = beatmapSet.Beatmaps.Single().OnlineID;
                });

                var sync = createSynchroniser(realm, "[]");
                await sync.SyncNowAsync();

                lock (SongSelect.MissingServerBeatmapSets)
                    Assert.That(SongSelect.MissingServerBeatmapSets, Does.Contain(setId));

                sync.FetchStatusesJson = _ => Task.FromResult($$"""[{"id":{{setId}},"status":"Ranked","beatmaps":[{"id":{{beatmapId}},"status":"Ranked"}]}]""");
                await sync.SyncNowAsync();

                lock (SongSelect.MissingServerBeatmapSets)
                    Assert.That(SongSelect.MissingServerBeatmapSets, Does.Not.Contain(setId));

                realm.Run(r => Assert.That(r.All<BeatmapSetInfo>().Single(s => s.OnlineID == setId).Status, Is.EqualTo(BeatmapOnlineStatus.Ranked)));
            });
        }

        [Test]
        public void TestFetchFailureLeavesExistingStatusesUntouched()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                int setId = 0;

                await realm.WriteAsync(r =>
                {
                    var beatmapSet = TestResources.CreateTestBeatmapSetInfo(1);
                    beatmapSet.Status = BeatmapOnlineStatus.Ranked;
                    r.Add(beatmapSet);
                    setId = beatmapSet.OnlineID;
                });

                var sync = createSynchroniser(realm, "[]");
                sync.FetchStatusesJson = _ => throw new HttpRequestException("simulated network failure");

                var result = await sync.SyncNowAsync();

                Assert.That(result, Is.Null);

                // a failed fetch must never look like "the server doesn't know about this beatmap" and wipe its status.
                realm.Run(r => Assert.That(r.All<BeatmapSetInfo>().Single(s => s.OnlineID == setId).Status, Is.EqualTo(BeatmapOnlineStatus.Ranked)));
            });
        }

        [Test]
        public void TestRealmWriteNeverRunsOnTheCallingThread()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                await realm.WriteAsync(r => r.Add(TestResources.CreateTestBeatmapSetInfo(1)));

                var sync = createSynchroniser(realm, "[]");
                int callingThreadId = Environment.CurrentManagedThreadId;

                await sync.SyncNowAsync();

                // must not lag whatever called this (e.g. the game's update thread, when queued automatically
                // after a download): the network fetch and the realm write both happen off that thread.
                Assert.That(sync.LastApplyThreadIdForTesting, Is.Not.Null);
                Assert.That(sync.LastApplyThreadIdForTesting, Is.Not.EqualTo(callingThreadId));
            });
        }

        [Test]
        public void TestConcurrentSyncsAreSerialisedNotDropped()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                int setId = 0;

                await realm.WriteAsync(r =>
                {
                    var beatmapSet = TestResources.CreateTestBeatmapSetInfo(1);
                    r.Add(beatmapSet);
                    setId = beatmapSet.OnlineID;
                });

                var gate = new TaskCompletionSource<string>();
                int fetchCount = 0;

                var sync = createSynchroniser(realm, "[]");
                sync.FetchStatusesJson = async _ =>
                {
                    Interlocked.Increment(ref fetchCount);
                    return await gate.Task;
                };

                // fired without awaiting, as a manual "sync now" click landing while an automatic sync is still running would be.
                Task<RinariSyncResult?> first = sync.SyncNowAsync();
                Task<RinariSyncResult?> second = sync.SyncNowAsync();

                gate.SetResult($$"""[{"id":{{setId}},"status":"Ranked","beatmaps":[]}]""");

                var results = await Task.WhenAll(first, second);

                // the old bool-flag guard silently dropped whichever call landed second (returning null) instead of waiting its turn.
                Assert.That(results[0], Is.Not.Null);
                Assert.That(results[1], Is.Not.Null);
                Assert.That(fetchCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void TestScopedSyncOnlyTouchesTheGivenSet()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                int downloadedSetId = 0, otherSetId = 0;

                await realm.WriteAsync(r =>
                {
                    var downloaded = TestResources.CreateTestBeatmapSetInfo(1);
                    r.Add(downloaded, update: true);
                    downloadedSetId = downloaded.OnlineID;
                });

                // already ranked locally, and the server disagrees (says it's now missing) - a scoped sync
                // for the *other* set must not touch this one at all.
                await realm.WriteAsync(r =>
                {
                    var other = TestResources.CreateTestBeatmapSetInfo(1);
                    other.Status = BeatmapOnlineStatus.Ranked;
                    r.Add(other, update: true);
                    otherSetId = other.OnlineID;
                });

                // the server's response always covers everything it knows about; only "downloaded" is ranked, and
                // "other" is conspicuously absent, as if it had been removed - the scope must still protect it.
                string json = $$"""[{"id":{{downloadedSetId}},"status":"Ranked","beatmaps":[]}]""";
                var sync = createSynchroniser(realm, json);

                var result = await sync.SyncScopedForTesting(new[] { downloadedSetId });

                Assert.That(result, Is.Not.Null);
                Assert.That(result!.UpdatedBeatmapSets, Is.EqualTo(1));

                realm.Run(r =>
                {
                    Assert.That(r.All<BeatmapSetInfo>().Single(s => s.OnlineID == downloadedSetId).Status, Is.EqualTo(BeatmapOnlineStatus.Ranked));

                    // untouched, even though the same server response implies it should have been marked missing.
                    Assert.That(r.All<BeatmapSetInfo>().Single(s => s.OnlineID == otherSetId).Status, Is.EqualTo(BeatmapOnlineStatus.Ranked));
                });

                lock (SongSelect.MissingServerBeatmapSets)
                    Assert.That(SongSelect.MissingServerBeatmapSets, Does.Not.Contain(otherSetId));
            });
        }

        [Test]
        public void TestQueuedScopesAccumulateAcrossABurstInsteadOfReplacingEachOther()
        {
            RunTestWithRealmAsync(async (realm, _) =>
            {
                int setAId = 0, setBId = 0;

                await realm.WriteAsync(r =>
                {
                    var setA = TestResources.CreateTestBeatmapSetInfo(1);
                    r.Add(setA, update: true);
                    setAId = setA.OnlineID;
                });

                await realm.WriteAsync(r =>
                {
                    var setB = TestResources.CreateTestBeatmapSetInfo(1);
                    r.Add(setB, update: true);
                    setBId = setB.OnlineID;
                });

                string json = $$"""[{"id":{{setAId}},"status":"Ranked","beatmaps":[]},{"id":{{setBId}},"status":"Loved","beatmaps":[]}]""";
                var sync = createSynchroniser(realm, json);

                // two downloads finishing moments apart, as QueueAutoSync would be called twice before the debounce fires.
                var result = await sync.SyncScopedForTesting(new[] { setAId, setBId });

                Assert.That(result!.UpdatedBeatmapSets, Is.EqualTo(2));

                realm.Run(r =>
                {
                    Assert.That(r.All<BeatmapSetInfo>().Single(s => s.OnlineID == setAId).Status, Is.EqualTo(BeatmapOnlineStatus.Ranked));
                    Assert.That(r.All<BeatmapSetInfo>().Single(s => s.OnlineID == setBId).Status, Is.EqualTo(BeatmapOnlineStatus.Loved));
                });
            });
        }

        private static RinariBeatmapStatusSynchroniser createSynchroniser(osu.Game.Database.RealmAccess realm, string json)
        {
            var sync = new RinariBeatmapStatusSynchroniser();
            sync.SetRealmForTesting(realm);
            sync.FetchStatusesJson = _ => Task.FromResult(json);
            return sync;
        }
    }
}
