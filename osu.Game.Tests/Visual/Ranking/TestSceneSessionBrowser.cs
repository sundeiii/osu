// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens.Ranking.Statistics.Session;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneSessionBrowser : ScreenTestScene
    {
        private SessionStatsStore store = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            // three finished sessions, plus the current one.
            Dependencies.Cache(store = SessionStatsTestHelper.CreateStore(LocalStorage, pastSessions: 3, currentPlays: 6));
        }

        [Test]
        public void TestBrowseSessions()
        {
            AddStep("open browser", () => LoadScreen(new SessionBrowserScreen()));

            // "all time" plus one entry per session.
            AddUntilStep("screen is titled", () => this.ChildrenOfType<OsuSpriteText>().Any(t => t.Text.ToString() == "Session stats"));
            AddUntilStep("all sessions listed", () => this.ChildrenOfType<SessionBrowserScreen.SessionListItem>().Count() == store.GetSessions().Count + 1);
            AddUntilStep("current session shown by default", () => this.ChildrenOfType<SessionStatsDisplay>().Any(d => d.IsLoaded));

            AddStep("select all time", () => this.ChildrenOfType<SessionBrowserScreen.SessionListItem>().First().TriggerClick());
            AddUntilStep("all time details shown", () => this.ChildrenOfType<SessionStatsDisplay>().Any(d => d.IsLoaded));

            AddStep("select oldest session", () => this.ChildrenOfType<SessionBrowserScreen.SessionListItem>().Last().TriggerClick());
            AddUntilStep("session details shown", () => this.ChildrenOfType<SessionStatsDisplay>().Any(d => d.IsLoaded));
            AddAssert("trend charts shown", () => this.ChildrenOfType<SessionTrendChart>().Any());
            AddAssert("combined histogram shown", () => this.ChildrenOfType<HitErrorHistogramChart>().Any());
        }
    }

    public partial class TestSceneSessionBrowserEmpty : ScreenTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Dependencies.Cache(new SessionStatsStore(LocalStorage));
        }

        [Test]
        public void TestNoSessionsYet()
        {
            AddStep("open browser", () => LoadScreen(new SessionBrowserScreen()));
            AddUntilStep("screen loaded", () => this.ChildrenOfType<SessionBrowserScreen>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("nothing listed", () => !this.ChildrenOfType<SessionBrowserScreen.SessionListItem>().Any());
        }
    }
}
