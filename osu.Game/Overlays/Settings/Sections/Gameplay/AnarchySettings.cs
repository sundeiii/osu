// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Gameplay
{
    public partial class AnarchySettings : SettingsSubsection
    {
        protected override LocalisableString Header => AnarchySettingsStrings.Header;

        public override IEnumerable<LocalisableString> FilterTerms =>
            base.FilterTerms.Concat(new LocalisableString[]
            {
                AnarchySettingsStrings.Header,
                AnarchySettingsStrings.Relax,
                AnarchySettingsStrings.RelaxDescription,
                AnarchySettingsStrings.RemoveHidden,
                AnarchySettingsStrings.RemoveHiddenDescription,
                AnarchySettingsStrings.EnableTimewarp,
                AnarchySettingsStrings.EnableTimewarpDescription,
                AnarchySettingsStrings.TimewarpRate,
                AnarchySettingsStrings.TimewarpRateDescription,
                AnarchySettingsStrings.EnableApproachRateChanger,
                AnarchySettingsStrings.EnableApproachRateChangerDescription,
                AnarchySettingsStrings.ApproachRate,
                AnarchySettingsStrings.ApproachRateDescription,
                AnarchySettingsStrings.AimAssist,
                AnarchySettingsStrings.AimAssistDescription,
                AnarchySettingsStrings.CorrectionStrength,
                AnarchySettingsStrings.CorrectionStrengthDescription,
                AnarchySettingsStrings.RelativeCorrection,
                AnarchySettingsStrings.RelativeCorrectionDescription,
                "autoclick",
                "hidden",
                "hd",
                "speed",
                "rate",
                "ar",
                "aim assistance",
                "aim correction",
                "skooter",
            });

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Bindable<bool> relax =
                config.GetBindable<bool>(OsuSetting.AnarchyRelax);

            Bindable<bool> removeHidden =
                config.GetBindable<bool>(OsuSetting.AnarchyRemoveHidden);

            Bindable<bool> timewarpEnabled =
                config.GetBindable<bool>(OsuSetting.AnarchyTimewarpEnabled);

            Bindable<double> timewarpRate =
                config.GetBindable<double>(OsuSetting.AnarchyTimewarpRate);

            Bindable<bool> approachRateEnabled =
                config.GetBindable<bool>(OsuSetting.AnarchyApproachRateEnabled);

            Bindable<double> approachRate =
                config.GetBindable<double>(OsuSetting.AnarchyApproachRate);

            Bindable<bool> aimAssist =
                config.GetBindable<bool>(OsuSetting.AnarchyAimAssist);

            Bindable<int> aimCorrectionStrength =
                config.GetBindable<int>(OsuSetting.AnarchyAimCorrectionStrength);

            Bindable<bool> aimCorrectionRelative =
                config.GetBindable<bool>(OsuSetting.AnarchyAimCorrectionRelative);

            timewarpEnabled.BindValueChanged(
                change => AnarchySettingsState.TimewarpEnabled = change.NewValue,
                true);

            timewarpRate.BindValueChanged(
                change => AnarchySettingsState.TimewarpRate.Value = change.NewValue,
                true);

            approachRateEnabled.BindValueChanged(
                change => AnarchySettingsState.ApproachRateEnabled = change.NewValue,
                true);

            approachRate.BindValueChanged(
                change => AnarchySettingsState.ApproachRate = change.NewValue,
                true);

            relax.BindValueChanged(
                change => AnarchySettingsState.Relax = change.NewValue,
                true);

            removeHidden.BindValueChanged(
                change => AnarchySettingsState.RemoveHidden = change.NewValue,
                true);

            aimAssist.BindValueChanged(
                change => AnarchySettingsState.AimAssist = change.NewValue,
                true);

            aimCorrectionStrength.BindValueChanged(
                change => AnarchySettingsState.AimCorrectionStrength.Value = change.NewValue,
                true);

            aimCorrectionRelative.BindValueChanged(
                change => AnarchySettingsState.AimCorrectionRelative = change.NewValue,
                true);

            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = AnarchySettingsStrings.Relax,
                    HintText = AnarchySettingsStrings.RelaxDescription,
                    Current = relax,
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = AnarchySettingsStrings.RemoveHidden,
                    HintText = AnarchySettingsStrings.RemoveHiddenDescription,
                    Current = removeHidden,
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = AnarchySettingsStrings.EnableTimewarp,
                    HintText = AnarchySettingsStrings.EnableTimewarpDescription,
                    Current = timewarpEnabled,
                }),

                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = AnarchySettingsStrings.TimewarpRate,
                    HintText = AnarchySettingsStrings.TimewarpRateDescription,
                    Current = timewarpRate,
                    KeyboardStep = 0.01f,
                    LabelFormat = value => $"{value:0.00}x",
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = AnarchySettingsStrings.EnableApproachRateChanger,
                    HintText = AnarchySettingsStrings.EnableApproachRateChangerDescription,
                    Current = approachRateEnabled,
                }),

                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = AnarchySettingsStrings.ApproachRate,
                    HintText = AnarchySettingsStrings.ApproachRateDescription,
                    Current = approachRate,
                    KeyboardStep = 0.1f,
                    LabelFormat = value => $"AR {value:0.0}",
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = AnarchySettingsStrings.AimAssist,
                    HintText = AnarchySettingsStrings.AimAssistDescription,
                    Current = aimAssist,
                }),

                new SettingsItemV2(new FormSliderBar<int>
                {
                    Caption = AnarchySettingsStrings.CorrectionStrength,
                    HintText = AnarchySettingsStrings.CorrectionStrengthDescription,
                    Current = aimCorrectionStrength,
                    KeyboardStep = 1,
                    LabelFormat = value => $"{value}",
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = AnarchySettingsStrings.RelativeCorrection,
                    HintText = AnarchySettingsStrings.RelativeCorrectionDescription,
                    Current = aimCorrectionRelative,
                }),
            };
        }
    }
}