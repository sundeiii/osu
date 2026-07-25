// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Game.Configuration
{
    public static class AnarchySettingsState
    {
        public static bool Relax { get; set; }

        public static bool RemoveHidden { get; set; }

        public static bool TimewarpEnabled { get; set; }

        public static BindableDouble TimewarpRate { get; } =
            new BindableDouble(1.0)
            {
                MinValue = 0.5,
                MaxValue = 3.0,
                Precision = 0.01,
            };

        public static bool ApproachRateEnabled { get; set; }

        public static double ApproachRate { get; set; } = 9.0;

        public static bool AimAssist { get; set; }

        public static BindableInt AimAssistSpeed { get; } =
            new BindableInt(5)
            {
                MinValue = 1,
                MaxValue = 11,
            };

        public static BindableInt AimAssistStartingDistance { get; } =
            new BindableInt(66)
            {
                MinValue = 0,
                MaxValue = 66,
            };

        public static BindableInt AimAssistStoppingDistance { get; } =
            new BindableInt(30)
            {
                MinValue = 0,
                MaxValue = 66,
            };

        public static bool AimAssistOnSliders { get; set; }
    }
}