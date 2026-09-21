// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics.Session;

namespace osu.Game.Tests.NonVisual.Ranking
{
    [TestFixture]
    public class SessionStatsStoreTest
    {
        private static ScoreInfo createScore(params double[] timeOffsets) => new ScoreInfo
        {
            ID = Guid.NewGuid(),
            Ruleset = new OsuRuleset().RulesetInfo,
            TotalScore = 1_000_000,
            Accuracy = 0.97,
            MaxCombo = 100,
            HitEvents = timeOffsets.Select(t => new HitEvent(t, 1.0, HitResult.Great, new HitObject(), null, null)).ToList(),
        };

        [Test]
        public void TestRecordCapturesHitErrors()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");
            var store = new SessionStatsStore(storage);

            var record = store.Record(createScore(-10, -10, 0, 3, 3), new AnarchySetupSnapshot(), DateTimeOffset.Now);

            Assert.That(record, Is.Not.Null);
            Assert.That(record!.AverageHitError, Is.EqualTo(-2.8).Within(0.0001));
            Assert.That(record.UnstableRate, Is.Not.Null);
            Assert.That(record.Accuracy, Is.EqualTo(0.97));

            // -10ms is two bins left of zero, 0ms is the centre bin, and 3ms rounds to one bin right of zero.
            int centre = SessionPlayRecord.HISTOGRAM_HALF_BINS;
            Assert.That(record.HitErrorHistogram[centre - 2], Is.EqualTo(2));
            Assert.That(record.HitErrorHistogram[centre], Is.EqualTo(1));
            Assert.That(record.HitErrorHistogram[centre + 1], Is.EqualTo(2));
        }

        [Test]
        public void TestOutOfRangeHitsAreClampedIntoOutermostBins()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");
            var store = new SessionStatsStore(storage);

            var record = store.Record(createScore(-500, 500), new AnarchySetupSnapshot(), DateTimeOffset.Now)!;

            Assert.That(record.HitErrorHistogram.First(), Is.EqualTo(1));
            Assert.That(record.HitErrorHistogram.Last(), Is.EqualTo(1));
        }

        [Test]
        public void TestSameScoreIsNotRecordedTwice()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");
            var store = new SessionStatsStore(storage);
            var score = createScore(0, 1, 2);

            Assert.That(store.Record(score, new AnarchySetupSnapshot(), DateTimeOffset.Now), Is.Not.Null);
            Assert.That(store.Record(score, new AnarchySetupSnapshot(), DateTimeOffset.Now), Is.Null);
            Assert.That(store.CurrentSessionPlays, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestPersistsAcrossSessions()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");

            var first = new SessionStatsStore(storage);
            first.Record(createScore(1, 2, 3), new AnarchySetupSnapshot { Relax = true }, DateTimeOffset.Now);

            var second = new SessionStatsStore(storage);

            Assert.That(second.SessionId, Is.Not.EqualTo(first.SessionId));
            Assert.That(second.AllPlays, Has.Count.EqualTo(1));
            Assert.That(second.AllPlays[0].Setup.Relax, Is.True);

            // a new session starts empty even though history was loaded.
            Assert.That(second.CurrentSessionPlays, Is.Empty);

            second.Record(createScore(1, 2, 3), new AnarchySetupSnapshot(), DateTimeOffset.Now);

            Assert.That(second.AllPlays, Has.Count.EqualTo(2));
            Assert.That(second.CurrentSessionPlays, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestCorruptFileStartsEmptyInsteadOfThrowing()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");

            using (var stream = storage.GetStream(SessionStatsStore.FILENAME, System.IO.FileAccess.Write, System.IO.FileMode.Create))
            using (var writer = new System.IO.StreamWriter(stream))
                writer.Write("{ this is not json");

            SessionStatsStore? store = null;
            Assert.DoesNotThrow(() => store = new SessionStatsStore(storage));
            Assert.That(store!.AllPlays, Is.Empty);
        }

        [Test]
        public void TestSummaryIsSplitBySetup()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");
            var store = new SessionStatsStore(storage);

            var manual = new AnarchySetupSnapshot();
            var relax = new AnarchySetupSnapshot { Relax = true, AimAssist = true };

            store.Record(createScore(-20, 20, -20, 20), manual, DateTimeOffset.Now);
            store.Record(createScore(-2, 2, -2, 2), relax, DateTimeOffset.Now);
            store.Record(createScore(-4, 4, -4, 4), relax, DateTimeOffset.Now);

            var summary = SessionSummary.Create(store.CurrentSessionPlays);

            Assert.That(summary.PlayCount, Is.EqualTo(3));
            Assert.That(summary.BySetup.Select(s => s.Key), Is.EqualTo(new[] { "Manual", "Relax + Aim assist" }));
            Assert.That(summary.BySetup[0].Value.PlayCount, Is.EqualTo(1));
            Assert.That(summary.BySetup[1].Value.PlayCount, Is.EqualTo(2));

            // the relaxed plays are more consistent, so their average unstable rate is lower and the best overall belongs to them.
            Assert.That(summary.BySetup[1].Value.AverageUnstableRate, Is.LessThan(summary.BySetup[0].Value.AverageUnstableRate));
            Assert.That(summary.BestUnstableRate, Is.EqualTo(summary.BySetup[1].Value.BestUnstableRate));
        }

        [Test]
        public void TestSessionsAreGroupedNewestFirst()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");

            var older = new SessionStatsStore(storage);
            older.Record(createScore(1, 2, 3), new AnarchySetupSnapshot(), DateTimeOffset.Now.AddDays(-2));
            older.Record(createScore(1, 2, 3), new AnarchySetupSnapshot(), DateTimeOffset.Now.AddDays(-2).AddMinutes(5));

            var newer = new SessionStatsStore(storage);
            newer.Record(createScore(1, 2, 3), new AnarchySetupSnapshot(), DateTimeOffset.Now.AddDays(-1));

            var sessions = newer.GetSessions();

            Assert.That(sessions, Has.Count.EqualTo(2));
            Assert.That(sessions[0].SessionId, Is.EqualTo(newer.SessionId));
            Assert.That(sessions[0].Plays, Has.Count.EqualTo(1));
            Assert.That(sessions[1].SessionId, Is.EqualTo(older.SessionId));
            Assert.That(sessions[1].Plays, Has.Count.EqualTo(2));
            Assert.That(sessions[1].Plays[0].PlayedAt, Is.LessThan(sessions[1].Plays[1].PlayedAt));
        }

        [Test]
        public void TestHistogramsAreCombined()
        {
            using var storage = new TemporaryNativeStorage("session-stats-test");
            var store = new SessionStatsStore(storage);

            store.Record(createScore(0, 0, 5), new AnarchySetupSnapshot(), DateTimeOffset.Now);
            store.Record(createScore(0, -5), new AnarchySetupSnapshot(), DateTimeOffset.Now);

            int[] combined = SessionSummary.CombineHistograms(store.CurrentSessionPlays);
            int centre = SessionPlayRecord.HISTOGRAM_HALF_BINS;

            Assert.That(combined, Has.Length.EqualTo(centre * 2 + 1));
            Assert.That(combined[centre], Is.EqualTo(3));
            Assert.That(combined[centre + 1], Is.EqualTo(1));
            Assert.That(combined[centre - 1], Is.EqualTo(1));
            Assert.That(combined.Sum(), Is.EqualTo(5));
        }

        [Test]
        public void TestHistogramsWithUnexpectedLayoutAreIgnored()
        {
            var plays = new[]
            {
                new SessionPlayRecord { HitErrorHistogram = new[] { 1, 2, 3 } },
                new SessionPlayRecord { HitErrorHistogram = new int[SessionPlayRecord.HISTOGRAM_HALF_BINS * 2 + 1] },
            };

            Assert.That(SessionSummary.CombineHistograms(plays).Sum(), Is.EqualTo(0));
        }

        [Test]
        public void TestSetupLabels()
        {
            Assert.That(new AnarchySetupSnapshot().Label, Is.EqualTo("Manual"));
            Assert.That(new AnarchySetupSnapshot { Relax = true, Timewarp = true, TimewarpRate = 1.5 }.Label, Is.EqualTo("Relax + Timewarp 1.5x"));
            Assert.That(new AnarchySetupSnapshot { ApproachRateOverride = true, ApproachRate = 8.7, RemoveHidden = true }.Label, Is.EqualTo("AR 8.7 + No Hidden"));
        }
    }
}
