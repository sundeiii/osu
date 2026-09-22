// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;

namespace osu.Game.Configuration
{
    public static class AnarchySettingsState
    {
        public static bool Relax { get; set; }

        /// <summary>
        /// How far from a hit object's start time Relax clicks, in milliseconds. Negative is early.
        /// </summary>
        public static double RelaxOffset { get; set; }

        /// <summary>
        /// A random amount, up to this many milliseconds either way, added to <see cref="RelaxOffset"/> for each hit object.
        /// </summary>
        public static double RelaxJitter { get; set; }

        public static bool RemoveHidden { get; set; }

        private static readonly BindableBool timewarp_enabled = new BindableBool();

        public static bool TimewarpEnabled
        {
            get => timewarp_enabled.Value;
            set => timewarp_enabled.Value = value;
        }

        /// <summary>
        /// Allows displays which show speed dependent values (like length and BPM) to update when Timewarp is toggled.
        /// </summary>
        public static IBindable<bool> TimewarpEnabledBindable => timewarp_enabled;

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

        public static BindableInt AimCorrectionStrength { get; } =
            new BindableInt(30)
            {
                MinValue = 0,
                MaxValue = 60,
            };

        public static bool AimCorrectionRelative { get; set; }

        /// <summary>
        /// The speed gameplay actually runs at, given the selected <paramref name="mods"/>.
        /// While Timewarp is on it replaces Double Time and Half Time (they are ignored during gameplay),
        /// but any other mod which changes the speed still applies on top of it.
        /// </summary>
        public static double GetEffectiveRate(IEnumerable<Mod> mods)
        {
            if (!TimewarpEnabled)
                return ModUtils.CalculateRateWithMods(mods);

            return ModUtils.CalculateRateWithMods(mods.Where(m => m is not (ModDoubleTime or ModHalfTime))) * TimewarpRate.Value;
        }
    }
}
