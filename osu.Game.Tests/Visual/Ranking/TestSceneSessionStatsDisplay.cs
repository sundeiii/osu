// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Graphics.Cursor;
using osu.Game.Screens.Ranking.Statistics.Session;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneSessionStatsDisplay : OsuTestScene
    {
        [SetUp]
        public void SetUp() => Schedule(() => Clear());

        [Test]
        public void TestEmptySession()
        {
            AddStep("show display", () => showDisplay(createStore(pastSessions: 0, currentPlays: 0)));
            AddUntilStep("display loaded", () => this.ChildrenOfType<SessionStatsDisplay>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("no charts for empty session", () => !this.ChildrenOfType<SessionTrendChart>().Any());
        }

        [Test]
        public void TestSinglePlay()
        {
            AddStep("show display", () => showDisplay(createStore(pastSessions: 0, currentPlays: 1)));
            AddUntilStep("display loaded", () => this.ChildrenOfType<SessionStatsDisplay>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("both trend charts present", () => this.ChildrenOfType<SessionTrendChart>().Count() == 2);
            AddAssert("histogram present", () => this.ChildrenOfType<HitErrorHistogramChart>().Count() == 1);
        }

        [Test]
        public void TestPopulatedSession()
        {
            AddStep("show display", () => showDisplay(createStore(pastSessions: 0, currentPlays: 12)));
            AddUntilStep("display loaded", () => this.ChildrenOfType<SessionStatsDisplay>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("both trend charts present", () => this.ChildrenOfType<SessionTrendChart>().Count() == 2);
            AddAssert("every play is hoverable on both charts", () => trendColumnCount() == 2 * 12);
        }

        [Test]
        public void TestOnlySessionPlaysAreShown()
        {
            SessionStatsStore store = null!;

            AddStep("show display with history", () => showDisplay(store = createStore(pastSessions: 2, currentPlays: 4)));
            AddUntilStep("display loaded", () => this.ChildrenOfType<SessionStatsDisplay>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("history exists", () => store.AllPlays.Count > store.CurrentSessionPlays.Count);
            AddAssert("only current session charted", () => trendColumnCount() == 2 * 4);
        }

        // the histogram has hover columns of its own, so only count the ones belonging to the trend charts.
        private int trendColumnCount() => this.ChildrenOfType<SessionTrendChart>().Sum(c => c.ChildrenOfType<ChartHoverColumn>().Count());

        private SessionStatsStore createStore(int pastSessions, int currentPlays)
            => SessionStatsTestHelper.CreateStore(LocalStorage.GetStorageForDirectory(Guid.NewGuid().ToString("N")), pastSessions, currentPlays);

        private void showDisplay(SessionStatsStore store) => Child = new DependencyProvidingContainer
        {
            RelativeSizeAxes = Axes.Both,
            CachedDependencies = new (Type, object)[] { (typeof(SessionStatsStore), store) },
            Child = new OsuTooltipContainer(null)
            {
                RelativeSizeAxes = Axes.Both,
                Child = new SessionStatsDisplay
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    // matches the width of the results screen statistics area.
                    RelativeSizeAxes = Axes.None,
                    Width = 1000,
                }
            }
        };
    }
}
