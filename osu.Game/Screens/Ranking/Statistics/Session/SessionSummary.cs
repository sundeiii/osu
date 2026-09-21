// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Screens.Ranking.Statistics.Session
{
    /// <summary>
    /// Aggregated numbers over a set of <see cref="SessionPlayRecord"/>s.
    /// </summary>
    public class SessionSummary
    {
        public int PlayCount { get; private init; }

        public double AverageAccuracy { get; private init; }

        public int TotalMisses { get; private init; }

        public double? AverageUnstableRate { get; private init; }

        /// <summary>
        /// The lowest (best) unstable rate of any play.
        /// </summary>
        public double? BestUnstableRate { get; private init; }

        /// <summary>
        /// The mean of each play's average hit error. Negative means early.
        /// </summary>
        public double? AverageHitError { get; private init; }

        /// <summary>
        /// The same numbers, split by <see cref="AnarchySetupSnapshot.Label"/>, in order of first appearance.
        /// Only populated for the top level summary.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, SessionSummary>> BySetup { get; private init; } = [];

        public static SessionSummary Create(IReadOnlyList<SessionPlayRecord> plays) => create(plays, true);

        private static SessionSummary create(IReadOnlyList<SessionPlayRecord> plays, bool splitBySetup)
        {
            double[] unstableRates = plays.Where(p => p.UnstableRate != null).Select(p => p.UnstableRate!.Value).ToArray();
            double[] hitErrors = plays.Where(p => p.AverageHitError != null).Select(p => p.AverageHitError!.Value).ToArray();

            return new SessionSummary
            {
                PlayCount = plays.Count,
                AverageAccuracy = plays.Count == 0 ? 0 : plays.Average(p => p.Accuracy),
                TotalMisses = plays.Sum(p => p.MissCount),
                AverageUnstableRate = unstableRates.Length == 0 ? null : unstableRates.Average(),
                BestUnstableRate = unstableRates.Length == 0 ? null : unstableRates.Min(),
                AverageHitError = hitErrors.Length == 0 ? null : hitErrors.Average(),
                BySetup = splitBySetup
                    ? plays.GroupBy(p => p.Setup.Label)
                           .Select(g => new KeyValuePair<string, SessionSummary>(g.Key, create(g.ToList(), false)))
                           .ToList()
                    : [],
            };
        }
    }
}
