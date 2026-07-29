// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class AnarchySettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.AnarchySettings";

        private static string getKey(string key) => $@"{prefix}:{key}";

        public static LocalisableString Header =>
            new TranslatableString(getKey(@"header"), @"Anarchy");

        public static LocalisableString Relax =>
            new TranslatableString(getKey(@"relax"), @"Relax");

        public static LocalisableString RelaxDescription =>
            new TranslatableString(
                getKey(@"relax_description"),
                @"Automatically presses gameplay buttons when the cursor is over a hittable object.");

        public static LocalisableString RemoveHidden =>
            new TranslatableString(getKey(@"remove_hidden"), @"Remove Hidden");

        public static LocalisableString RemoveHiddenDescription =>
            new TranslatableString(
                getKey(@"remove_hidden_description"),
                @"Keeps approach circles visible while the Hidden mod is active.");

        public static LocalisableString EnableTimewarp =>
            new TranslatableString(getKey(@"enable_timewarp"), @"Enable Timewarp");

        public static LocalisableString EnableTimewarpDescription =>
            new TranslatableString(
                getKey(@"enable_timewarp_description"),
                @"Changes the speed of gameplay and audio.");

        public static LocalisableString TimewarpRate =>
            new TranslatableString(getKey(@"timewarp_rate"), @"Timewarp rate");

        public static LocalisableString TimewarpRateDescription =>
            new TranslatableString(
                getKey(@"timewarp_rate_description"),
                @"Controls the gameplay speed multiplier used by Timewarp.");

        public static LocalisableString EnableApproachRateChanger =>
            new TranslatableString(
                getKey(@"enable_approach_rate_changer"),
                @"Enable AR changer");

        public static LocalisableString EnableApproachRateChangerDescription =>
            new TranslatableString(
                getKey(@"enable_approach_rate_changer_description"),
                @"Overrides the beatmap's approach rate during gameplay.");

        public static LocalisableString ApproachRate =>
            new TranslatableString(getKey(@"approach_rate"), @"Approach rate");

        public static LocalisableString ApproachRateDescription =>
            new TranslatableString(
                getKey(@"approach_rate_description"),
                @"Controls how early hit objects become visible.");

        public static LocalisableString AimAssist =>
            new TranslatableString(getKey(@"aim_assist"), @"Aim assist");

        public static LocalisableString AimAssistDescription =>
            new TranslatableString(
                getKey(@"aim_assist_description"),
                @"Corrects near-misses onto hittable circles using Skooter aim correction.");

        public static LocalisableString CorrectionStrength =>
            new TranslatableString(
                getKey(@"correction_strength"),
                @"Correction strength");

        public static LocalisableString CorrectionStrengthDescription =>
            new TranslatableString(
                getKey(@"correction_strength_description"),
                @"Controls how close the cursor must be to an object before aim correction activates.");

        public static LocalisableString RelativeCorrection =>
            new TranslatableString(
                getKey(@"relative_correction"),
                @"Relative correction");

        public static LocalisableString RelativeCorrectionDescription =>
            new TranslatableString(
                getKey(@"relative_correction_description"),
                @"Adds the hit object's radius to the configured correction strength.");
    }
}