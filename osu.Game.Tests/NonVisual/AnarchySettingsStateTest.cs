// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class AnarchySettingsStateTest
    {
        [TearDown]
        public void TearDown()
        {
            AnarchySettingsState.TimewarpEnabled = false;
            AnarchySettingsState.TimewarpRate.Value = 1;
        }

        private static void enableTimewarp(double rate)
        {
            AnarchySettingsState.TimewarpEnabled = true;
            AnarchySettingsState.TimewarpRate.Value = rate;
        }

        [Test]
        public void TestWithoutTimewarpTheModsDecideTheRate()
        {
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[] { new OsuModDoubleTime() }), Is.EqualTo(1.5));
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[] { new OsuModHalfTime() }), Is.EqualTo(0.75));
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[0]), Is.EqualTo(1));
        }

        [Test]
        public void TestTimewarpSetsTheRateWithoutMods()
        {
            enableTimewarp(1.25);

            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[0]), Is.EqualTo(1.25));
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[] { new OsuModHardRock() }), Is.EqualTo(1.25));
        }

        [Test]
        public void TestTimewarpReplacesDoubleTimeAndHalfTimeInsteadOfStackingWithThem()
        {
            // gameplay ignores these mods' speed while Timewarp is on, so the display must too.
            enableTimewarp(1.25);

            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[] { new OsuModDoubleTime() }), Is.EqualTo(1.25));
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[] { new OsuModHalfTime() }), Is.EqualTo(1.25));
        }

        [Test]
        public void TestTimewarpRateChangesAreReflectedImmediately()
        {
            enableTimewarp(1.5);
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[0]), Is.EqualTo(1.5));

            AnarchySettingsState.TimewarpRate.Value = 2;
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[0]), Is.EqualTo(2));

            AnarchySettingsState.TimewarpEnabled = false;
            Assert.That(AnarchySettingsState.GetEffectiveRate(new Mod[0]), Is.EqualTo(1));
        }

        [Test]
        public void TestTogglingTimewarpNotifiesListeners()
        {
            int notifications = 0;
            AnarchySettingsState.TimewarpEnabledBindable.ValueChanged += _ => notifications++;

            AnarchySettingsState.TimewarpEnabled = true;
            AnarchySettingsState.TimewarpEnabled = false;

            Assert.That(notifications, Is.EqualTo(2));
        }
    }
}
