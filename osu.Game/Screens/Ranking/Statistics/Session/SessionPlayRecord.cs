// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using osu.Game.Configuration;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Screens.Ranking.Statistics.Session
{
    /// <summary>
    /// A summary of one finished play, persisted locally so that stats can be shown across a session.
    /// </summary>
    public class SessionPlayRecord
    {
        /// <summary>
        /// Width (in milliseconds) of each bin of <see cref="HitErrorHistogram"/>.
        /// </summary>
        public const int HISTOGRAM_BIN_WIDTH = 5;

        /// <summary>
        /// Number of bins on each side of zero in <see cref="HitErrorHistogram"/>.
        /// </summary>
        public const int HISTOGRAM_HALF_BINS = 20;

        [JsonProperty("score_id")]
        public Guid ScoreID { get; set; }

        [JsonProperty("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonProperty("played_at")]
        public DateTimeOffset PlayedAt { get; set; }

        [JsonProperty("beatmap")]
        public string Beatmap { get; set; } = string.Empty;

        [JsonProperty("ruleset")]
        public string Ruleset { get; set; } = string.Empty;

        [JsonProperty("mods")]
        public string[] Mods { get; set; } = [];

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        /// <summary>
        /// Accuracy in the range 0 to 1.
        /// </summary>
        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty("miss_count")]
        public int MissCount { get; set; }

        [JsonProperty("unstable_rate")]
        public double? UnstableRate { get; set; }

        /// <summary>
        /// Average hit error in milliseconds. Negative means early.
        /// </summary>
        [JsonProperty("average_hit_error")]
        public double? AverageHitError { get; set; }

        /// <summary>
        /// Hit error counts, from -100ms to +100ms in <see cref="HISTOGRAM_BIN_WIDTH"/>ms bins.
        /// Out of range hits are clamped into the outermost bins.
        /// </summary>
        [JsonProperty("hit_error_histogram")]
        public int[] HitErrorHistogram { get; set; } = [];

        [JsonProperty("setup")]
        public AnarchySetupSnapshot Setup { get; set; } = new AnarchySetupSnapshot();

        /// <summary>
        /// Creates a record from a just-finished <paramref name="score"/>.
        /// </summary>
        public static SessionPlayRecord FromScore(ScoreInfo score, string sessionId, DateTimeOffset playedAt, AnarchySetupSnapshot setup)
        {
            var hitEvents = score.HitEvents;

            int[] histogram = new int[HISTOGRAM_HALF_BINS * 2 + 1];

            foreach (var e in hitEvents.Where(HitEventExtensions.AffectsUnstableRate))
            {
                int bin = (int)Math.Clamp(Math.Round(e.TimeOffset / HISTOGRAM_BIN_WIDTH), -HISTOGRAM_HALF_BINS, HISTOGRAM_HALF_BINS);
                histogram[bin + HISTOGRAM_HALF_BINS]++;
            }

            // unstable rate requires the gameplay rate to be known for every event.
            double? unstableRate = hitEvents.All(e => e.GameplayRate != null) ? hitEvents.CalculateUnstableRate()?.Result : null;

            return new SessionPlayRecord
            {
                ScoreID = score.ID,
                SessionId = sessionId,
                PlayedAt = playedAt,
                Beatmap = score.BeatmapInfo?.ToString() ?? string.Empty,
                Ruleset = score.Ruleset.ShortName,
                Mods = score.Mods.Select(m => m.Acronym).ToArray(),
                TotalScore = score.TotalScore,
                Accuracy = score.Accuracy,
                MaxCombo = score.MaxCombo,
                MissCount = score.Statistics.GetValueOrDefault(HitResult.Miss),
                UnstableRate = unstableRate,
                AverageHitError = hitEvents.CalculateAverageHitError(),
                HitErrorHistogram = histogram,
                Setup = setup,
            };
        }
    }

    /// <summary>
    /// The state of the Anarchy settings at the time of a play.
    /// </summary>
    public class AnarchySetupSnapshot
    {
        [JsonProperty("relax")]
        public bool Relax { get; set; }

        [JsonProperty("relax_offset")]
        public double RelaxOffset { get; set; }

        [JsonProperty("relax_jitter")]
        public double RelaxJitter { get; set; }

        [JsonProperty("aim_assist")]
        public bool AimAssist { get; set; }

        [JsonProperty("aim_correction_strength")]
        public int AimCorrectionStrength { get; set; }

        [JsonProperty("aim_correction_relative")]
        public bool AimCorrectionRelative { get; set; }

        [JsonProperty("timewarp")]
        public bool Timewarp { get; set; }

        [JsonProperty("timewarp_rate")]
        public double TimewarpRate { get; set; }

        [JsonProperty("approach_rate_override")]
        public bool ApproachRateOverride { get; set; }

        [JsonProperty("approach_rate")]
        public double ApproachRate { get; set; }

        [JsonProperty("remove_hidden")]
        public bool RemoveHidden { get; set; }

        /// <summary>
        /// A short human readable description of the enabled features, including their exact values.
        /// </summary>
        [JsonIgnore]
        public string Label => buildLabel(exact: true);

        /// <summary>
        /// Like <see cref="Label"/>, but only says which features were on, ignoring how they were set up.
        /// Two plays with the same group label used the same kind of setup, even if e.g. Timewarp ran at different speeds.
        /// </summary>
        [JsonIgnore]
        public string GroupLabel => buildLabel(exact: false);

        private string buildLabel(bool exact)
        {
            var parts = new List<string>();

            if (Relax)
            {
                var details = new List<string>();

                if (exact && RelaxOffset != 0)
                    details.Add($"{RelaxOffset.ToString("+0.#;-0.#", CultureInfo.InvariantCulture)} ms");

                if (exact && RelaxJitter > 0)
                    details.Add($"±{RelaxJitter.ToString("0.#", CultureInfo.InvariantCulture)} ms");

                parts.Add(details.Count == 0 ? "Relax" : $"Relax ({string.Join(", ", details)})");
            }

            if (AimAssist)
                parts.Add("Aim assist");

            if (Timewarp)
                parts.Add(exact ? $"Timewarp {TimewarpRate.ToString("0.##", CultureInfo.InvariantCulture)}x" : "Timewarp");

            if (ApproachRateOverride)
                parts.Add(exact ? $"AR {ApproachRate.ToString("0.#", CultureInfo.InvariantCulture)}" : "AR override");

            if (RemoveHidden)
                parts.Add("No Hidden");

            return parts.Count == 0 ? "Manual" : string.Join(" + ", parts);
        }

        /// <summary>
        /// Captures the current state of <see cref="AnarchySettingsState"/>.
        /// </summary>
        public static AnarchySetupSnapshot Capture() => new AnarchySetupSnapshot
        {
            Relax = AnarchySettingsState.Relax,
            RelaxOffset = AnarchySettingsState.RelaxOffset,
            RelaxJitter = AnarchySettingsState.RelaxJitter,
            AimAssist = AnarchySettingsState.AimAssist,
            AimCorrectionStrength = AnarchySettingsState.AimCorrectionStrength.Value,
            AimCorrectionRelative = AnarchySettingsState.AimCorrectionRelative,
            Timewarp = AnarchySettingsState.TimewarpEnabled,
            TimewarpRate = AnarchySettingsState.TimewarpRate.Value,
            ApproachRateOverride = AnarchySettingsState.ApproachRateEnabled,
            ApproachRate = AnarchySettingsState.ApproachRate,
            RemoveHidden = AnarchySettingsState.RemoveHidden,
        };
    }
}
