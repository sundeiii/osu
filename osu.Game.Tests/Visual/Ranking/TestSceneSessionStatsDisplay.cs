// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics.Cursor;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics.Session;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneSessionStatsDisplay : OsuTestScene
    {
        private SessionStatsStore store = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Dependencies.Cache(store = new SessionStatsStore(LocalStorage));
        }

        [SetUp]
        public void SetUp() => Schedule(() => Clear());

        [Test]
        public void TestEmptySession()
        {
            AddStep("show display", showDisplay);
            AddAssert("display loaded", () => this.ChildrenOfType<SessionStatsDisplay>().Single().IsLoaded);
        }

        [Test]
        public void TestPopulatedSession()
        {
            AddStep("record a session of plays", () =>
            {
                var random = new Random(1234);

                for (int i = 0; i < 12; i++)
                {
                    // drift towards more consistent hits as the session goes on, and switch setups half way.
                    double spread = 40 - i * 2.5;
                    var setup = new AnarchySetupSnapshot { Relax = i >= 6, AimAssist = i >= 6 };

                    store.Record(createScore(random, spread), setup, DateTimeOffset.Now.AddMinutes(i));
                }
            });

            AddStep("show display", showDisplay);
            AddAssert("display loaded", () => this.ChildrenOfType<SessionStatsDisplay>().Single().IsLoaded);
        }

        private void showDisplay() => Child = new OsuTooltipContainer(null)
        {
            RelativeSizeAxes = Axes.Both,
            Child = new SessionStatsDisplay
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 500,
                RelativeSizeAxes = Axes.None,
            }
        };

        private static ScoreInfo createScore(Random random, double spread) => new ScoreInfo
        {
            ID = Guid.NewGuid(),
            Ruleset = new OsuRuleset().RulesetInfo,
            Accuracy = 0.9 + random.NextDouble() * 0.09,
            HitEvents = Enumerable.Range(0, 200)
                                  .Select(_ => new HitEvent((random.NextDouble() - 0.5) * spread, 1.0, HitResult.Great, new HitObject(), null, null))
                                  .ToList(),
        };
    }
}
