// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osuTK;
using static osu.Game.Input.Handlers.ReplayInputHandler;

namespace osu.Game.Rulesets.Osu.UI
{
    public partial class DrawableOsuRuleset : DrawableRuleset<OsuHitObject>
    {
        private Bindable<bool>? cursorHideEnabled;

        private const uint mouseeventf_move = 0x0001;

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        public new OsuInputManager KeyBindingInputManager =>
            (OsuInputManager)base.KeyBindingInputManager;

        public new OsuPlayfield Playfield =>
            (OsuPlayfield)base.Playfield;

        protected new OsuRulesetConfigManager Config =>
            (OsuRulesetConfigManager)base.Config;

        // Hidden Relax state.
        private const double relax_hit_offset = 0;

        private bool relaxWasEnabled;
        private bool relaxIsDown;
        private bool relaxWasLeft;
        private double relaxLastStateChangeTime;

        public DrawableOsuRuleset(
            Ruleset ruleset,
            IBeatmap beatmap,
            IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        [BackgroundDependencyLoader]
        private void load(ReplayPlayer? replayPlayer)
        {
            if (replayPlayer != null)
            {
                ReplayAnalysisOverlay analysisOverlay;

                PlayfieldAdjustmentContainer.Add(
                    analysisOverlay =
                        new ReplayAnalysisOverlay(replayPlayer.Score.Replay));

                Overlays.Add(
                    analysisOverlay.CreateProxy().With(
                        p => p.Depth = float.NegativeInfinity));

                replayPlayer.AddSettings(
                    new ReplayAnalysisSettings(Config));

                cursorHideEnabled =
                    Config.GetBindable<bool>(
                        OsuRulesetSetting.ReplayCursorHideEnabled);

                cursorHideEnabled.BindValueChanged(
                    enabled =>
                        Playfield.Cursor.FadeTo(
                            enabled.NewValue ? 0 : 1),
                    true);
            }
        }

        protected override void Update()
        {
            base.Update();

            updateAnarchyRelax();
            updateAnarchyAimAssist();
        }

        private void updateAnarchyRelax()
        {
            bool enabled = AnarchySettingsState.Relax;

            /*
             * Never generate Relax input while watching a replay.
             */
            bool hasReplay =
                KeyBindingInputManager.ReplayInputHandler != null;

            if (hasReplay)
            {
                if (relaxWasEnabled)
                    disableAnarchyRelax();

                relaxWasEnabled = false;
                return;
            }

            /*
             * Detect live setting changes.
             */
            if (enabled != relaxWasEnabled)
            {
                relaxWasEnabled = enabled;

                if (enabled)
                    enableAnarchyRelax();
                else
                    disableAnarchyRelax();
            }

            if (!enabled)
                return;

            bool requiresHold = false;
            bool requiresHit = false;

            double time = Playfield.Clock.CurrentTime;

            foreach (DrawableOsuHitObject drawable in
                     Playfield.HitObjectContainer.AliveObjects
                              .OfType<DrawableOsuHitObject>())
            {
                /*
                 * Not close enough to the object's hit time.
                 */
                if (time <
                    drawable.HitObject.StartTime -
                    OsuModRelax.RELAX_LENIENCY)
                {
                    break;
                }

                /*
                 * Already judged, or beyond the object's hittable duration.
                 */
                if (drawable.IsHit ||
                    drawable.HitObject is IHasDuration duration &&
                    time > duration.EndTime)
                {
                    continue;
                }

                switch (drawable)
                {
                    case DrawableHitCircle circle:
                        handleHitCircle(circle);
                        break;

                    case DrawableSlider slider:
                        /*
                         * Handles overlapping / 2B slider heads.
                         */
                        if (!slider.HeadCircle.IsHit)
                            handleHitCircle(slider.HeadCircle);

                        requiresHold |=
                            slider.SliderInputManager.IsMouseInFollowArea(
                                slider.Tracking.Value);

                        break;

                    case DrawableSpinner spinner:
                        requiresHold |=
                            spinner.HitObject.SpinsRequired > 0;

                        break;
                }
            }

            if (requiresHit)
            {
                changeRelaxState(false, time);
                changeRelaxState(true, time);
            }

            if (requiresHold)
            {
                changeRelaxState(true, time);
            }
            else if (relaxIsDown &&
                     time - relaxLastStateChangeTime >
                     AutoGenerator.KEY_UP_DELAY)
            {
                changeRelaxState(false, time);
            }

            void handleHitCircle(DrawableHitCircle circle)
            {
                if (!circle.HitArea.IsHovered)
                    return;

                double hitTime = circle.HitObject.StartTime + relax_hit_offset;

                if (time < hitTime)
                    return;

                Debug.Assert(
                    circle.HitObject.HitWindows != null);

                requiresHit |=
                    circle.HitObject.HitWindows.CanBeHit(
                        time - circle.HitObject.StartTime);
            }
        }

        private void updateAnarchyAimAssist()
        {
            if (!AnarchySettingsState.AimAssist)
                return;

            if (KeyBindingInputManager.ReplayInputHandler != null)
                return;

            double time = Playfield.Clock.CurrentTime;

            var objects =
                Playfield.HitObjectContainer.AliveObjects
                        .OfType<DrawableOsuHitObject>()
                        .Where(drawable => !drawable.IsHit)
                        .Where(drawable => time <= getEndTime(drawable))
                        .OrderBy(drawable => drawable.HitObject.StartTime)
                        .ToList();

            if (objects.Count == 0)
                return;

            /*
            * In lazer, AliveObjects is not the same as stable's currentHitObjectIndex.
            * Always targeting the first unhit object is safer and avoids AA pulling away before the current note is judged.
            */
            DrawableOsuHitObject target = objects[0];

            if (target is DrawableSpinner)
                return;

            if (target is DrawableSlider && !AnarchySettingsState.AimAssistOnSliders)
                return;

            if (target.HitObject.StartTime - time >= 240)
                return;

            Vector2 cursorPosition =
                Playfield.ToLocalSpace(
                    KeyBindingInputManager.CurrentState.Mouse.Position);

            Vector2 targetPosition = getTargetPosition(target);

            float distance = Vector2.Distance(cursorPosition, targetPosition);

            int startingDistance = AnarchySettingsState.AimAssistStartingDistance.Value;
            int stoppingDistance = AnarchySettingsState.AimAssistStoppingDistance.Value;
            int speed = AnarchySettingsState.AimAssistSpeed.Value;

            if (distance >= startingDistance)
                return;

            float hitObjectRadius = (float)target.HitObject.Radius;
            float stopRadius = hitObjectRadius * stoppingDistance / 100f;

            if (distance <= stopRadius)
                return;

            Vector2 delta = targetPosition - cursorPosition;

            if (delta.LengthSquared <= 0)
                return;

            float step =
                Math.Clamp(
                    distance / (5f - speed / 10f) / 10f,
                    0f,
                    5f);

            Vector2 movement =
                Vector2.Normalize(delta) *
                Math.Min(step, distance);

            /*
            * Stable calculates movement in gamefield units and passes it directly to MoveMouseBy().
            * Do NOT convert to screen-space here.
            */
            if (OperatingSystem.IsWindows())
            {
                mouse_event(
                    mouseeventf_move,
                    (int)Math.Round(movement.X),
                    (int)Math.Round(movement.Y),
                    0,
                    UIntPtr.Zero);
            }
            else
            {
                Vector2 nextPosition = cursorPosition + movement;

                new MousePositionAbsoluteInput
                {
                    Position = Playfield.ToScreenSpace(nextPosition)
                }.Apply(
                    KeyBindingInputManager.CurrentState,
                    KeyBindingInputManager);
            }

            static double getEndTime(DrawableOsuHitObject drawable)
                => drawable.HitObject is IHasDuration duration
                    ? duration.EndTime
                    : drawable.HitObject.StartTime;

            static Vector2 getTargetPosition(DrawableOsuHitObject drawable)
                => drawable.Position;
        }

        private void enableAnarchyRelax()
        {
            /*
             * Prevent physical keyboard/mouse tapping from being sent as
             * gameplay input while hidden Relax is active.
             */
            KeyBindingInputManager.AllowGameplayInputs = false;

            /*
             * Start from a released input state.
             */
            releaseRelaxInput();
        }

        private void disableAnarchyRelax()
        {
            /*
             * Release any generated key before restoring normal tapping.
             */
            releaseRelaxInput();

            KeyBindingInputManager.AllowGameplayInputs = true;
        }

        private void changeRelaxState(bool down, double time)
        {
            if (relaxIsDown == down)
                return;

            relaxIsDown = down;
            relaxLastStateChangeTime = time;

            var state = new ReplayState<OsuAction>
            {
                PressedActions = new List<OsuAction>()
            };

            if (down)
            {
                state.PressedActions.Add(
                    relaxWasLeft
                        ? OsuAction.LeftButton
                        : OsuAction.RightButton);

                relaxWasLeft = !relaxWasLeft;
            }

            state.Apply(
                KeyBindingInputManager.CurrentState,
                KeyBindingInputManager);
        }

        private void releaseRelaxInput()
        {
            if (!relaxIsDown)
                return;

            relaxIsDown = false;

            var state = new ReplayState<OsuAction>
            {
                PressedActions = new List<OsuAction>()
            };

            state.Apply(
                KeyBindingInputManager.CurrentState,
                KeyBindingInputManager);
        }

        public override DrawableHitObject<OsuHitObject>?
            CreateDrawableRepresentation(OsuHitObject h) => null;

        public override bool ReceivePositionalInputAt(
            Vector2 screenSpacePos) => true;

        protected override Playfield CreatePlayfield() =>
            new OsuPlayfield();

        protected override PassThroughInputManager CreateInputManager() =>
            new OsuInputManager(Ruleset.RulesetInfo);

        public override PlayfieldAdjustmentContainer
            CreatePlayfieldAdjustmentContainer() =>
                new OsuPlayfieldAdjustmentContainer
                {
                    AlignWithStoryboard = true
                };

        protected override ResumeOverlay CreateResumeOverlay()
        {
            if (Mods.Any(
                    m => m is OsuModAutopilot or OsuModTouchDevice))
            {
                return new DelayedResumeOverlay
                {
                    Scale = new Vector2(0.65f)
                };
            }

            return new OsuResumeOverlay();
        }

        protected override ReplayInputHandler CreateReplayInputHandler(
            Replay replay) =>
                new OsuFramedReplayInputHandler(replay);

        protected override ReplayRecorder CreateReplayRecorder(
            Score score) =>
                new OsuReplayRecorder(score);

        public override double GameplayStartTime
        {
            get
            {
                if (Objects.FirstOrDefault() is OsuHitObject first)
                {
                    return first.StartTime -
                           Math.Max(2000, first.TimePreempt);
                }

                return 0;
            }
        }
    }
}