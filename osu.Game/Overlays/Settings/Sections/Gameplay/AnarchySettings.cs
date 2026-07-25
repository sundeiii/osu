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

namespace osu.Game.Overlays.Settings.Sections.Gameplay
{
    public partial class AnarchySettings : SettingsSubsection
    {
        protected override LocalisableString Header => "Anarchy";

        public override IEnumerable<LocalisableString> FilterTerms =>
            base.FilterTerms.Concat(new LocalisableString[]
            {
                "anarchy",
                "relax",
                "autoclick",
                "hidden",
                "hd",
                "timewarp",
                "speed",
                "rate",
                "approach rate",
                "ar",
                "aim assist",
                "aim assistance",
                "osu buddy",
                "osubuddy",
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

            Bindable<int> aimAssistSpeed =
                config.GetBindable<int>(OsuSetting.AnarchyAimAssistSpeed);

            Bindable<int> aimAssistStartingDistance =
                config.GetBindable<int>(OsuSetting.AnarchyAimAssistStartingDistance);

            Bindable<int> aimAssistStoppingDistance =
                config.GetBindable<int>(OsuSetting.AnarchyAimAssistStoppingDistance);

            Bindable<bool> aimAssistOnSliders =
                config.GetBindable<bool>(OsuSetting.AnarchyAimAssistOnSliders);

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

            aimAssistSpeed.BindValueChanged(
                change => AnarchySettingsState.AimAssistSpeed.Value = change.NewValue,
                true);

            aimAssistStartingDistance.BindValueChanged(
                change => AnarchySettingsState.AimAssistStartingDistance.Value = change.NewValue,
                true);

            aimAssistStoppingDistance.BindValueChanged(
                change => AnarchySettingsState.AimAssistStoppingDistance.Value = change.NewValue,
                true);

            aimAssistOnSliders.BindValueChanged(
                change => AnarchySettingsState.AimAssistOnSliders = change.NewValue,
                true);

            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Relax",
                    Current = relax,
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Remove Hidden",
                    Current = removeHidden,
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Enable Timewarp",
                    Current = timewarpEnabled,
                }),

                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = "Timewarp rate",
                    Current = timewarpRate,
                    KeyboardStep = 0.01f,
                    LabelFormat = v => $"{v:0.00}x",
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Enable AR changer",
                    Current = approachRateEnabled,
                }),

                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = "Approach rate",
                    Current = approachRate,
                    KeyboardStep = 0.1f,
                    LabelFormat = v => $"AR {v:0.0}",
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Aim assist",
                    Current = aimAssist,
                }),

                new SettingsItemV2(new FormSliderBar<int>
                {
                    Caption = "Aim assist strength",
                    Current = aimAssistSpeed,
                    KeyboardStep = 1,
                    LabelFormat = v => $"{v}",
                }),

                new SettingsItemV2(new FormSliderBar<int>
                {
                    Caption = "Activation radius",
                    Current = aimAssistStartingDistance,
                    KeyboardStep = 1,
                    LabelFormat = v => $"{v}px",
                }),

                new SettingsItemV2(new FormSliderBar<int>
                {
                    Caption = "Stop radius",
                    Current = aimAssistStoppingDistance,
                    KeyboardStep = 1,
                    LabelFormat = v => $"{v}%",
                }),

                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Assist sliders",
                    Current = aimAssistOnSliders,
                }),
            };
        }
    }
}