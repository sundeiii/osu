// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Statistics.Session
{
    /// <summary>
    /// Shows how a set of plays went: summary numbers, unstable rate and accuracy trends,
    /// the hit error distribution, and a comparison between setups.
    /// </summary>
    public partial class SessionStatsDisplay : CompositeDrawable
    {
        private const float column_spacing = 30;
        private const int max_listed_plays = 100;

        [Resolved]
        private SessionStatsStore? store { get; set; }

        private readonly IReadOnlyList<SessionPlayRecord>? plays;
        private readonly bool detailed;

        /// <param name="plays">The plays to show, oldest first. Defaults to the plays of the current session.</param>
        /// <param name="detailed">
        /// Whether to also show the combined hit error distribution, the comparison between setups and a list of individual plays.
        /// Leave off where a single play's own statistics are already shown next to it, like the results screen.
        /// </param>
        public SessionStatsDisplay(IReadOnlyList<SessionPlayRecord>? plays = null, bool detailed = false)
        {
            this.plays = plays;
            this.detailed = detailed;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            var shownPlays = plays ?? store?.CurrentSessionPlays ?? [];
            var summary = SessionSummary.Create(shownPlays);

            var table = new SimpleStatisticTable(2, new SimpleStatisticItem[]
            {
                new TextItem("Plays", summary.PlayCount.ToString()),
                new TextItem("Average accuracy", formatAccuracy(summary.AverageAccuracy)),
                new TextItem("Average unstable rate", formatNumber(summary.AverageUnstableRate)),
                new TextItem("Best unstable rate", formatNumber(summary.BestUnstableRate)),
                new TextItem("Average hit error", formatHitError(summary.AverageHitError)),
                new TextItem("Total misses", summary.TotalMisses.ToString()),
            });

            InternalChild = detailed ? createDetailed(shownPlays, summary, table) : createCompact(shownPlays, table);
        }

        /// <summary>
        /// A single short row, so the results screen (which already has plenty of other statistics) can still show everything on one screen.
        /// </summary>
        private static Drawable createCompact(IReadOnlyList<SessionPlayRecord> shownPlays, Drawable table)
        {
            if (shownPlays.Count == 0)
                return table;

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.32f),
                    new Dimension(GridSizeMode.Absolute, column_spacing),
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, column_spacing),
                    new Dimension(),
                },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    new[]
                    {
                        table,
                        new Container(),
                        createUnstableRateChart(shownPlays, compact: true),
                        new Container(),
                        createAccuracyChart(shownPlays, compact: true),
                    }
                },
            };
        }

        private static Drawable createDetailed(IReadOnlyList<SessionPlayRecord> shownPlays, SessionSummary summary, Drawable table)
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 16),
                Child = table,
            };

            if (shownPlays.Count > 0)
            {
                flow.Add(createTwoColumns(
                    createUnstableRateChart(shownPlays, compact: false),
                    createAccuracyChart(shownPlays, compact: false)));

                flow.Add(createTwoColumns(
                    new HitErrorHistogramChart(SessionSummary.CombineHistograms(shownPlays)),
                    createSetupComparison(summary)));

                flow.Add(createPlayList(shownPlays));
            }

            return flow;
        }

        private static SessionTrendChart createUnstableRateChart(IReadOnlyList<SessionPlayRecord> shownPlays, bool compact)
            => new SessionTrendChart(compact ? "Unstable rate" : "Unstable rate (lower is better)", shownPlays, p => p.UnstableRate, v => v.ToString("N2"), lowerIsBetter: true, compact);

        private static SessionTrendChart createAccuracyChart(IReadOnlyList<SessionPlayRecord> shownPlays, bool compact)
            => new SessionTrendChart("Accuracy", shownPlays, p => p.Accuracy, formatAccuracy, lowerIsBetter: false, compact);

        private static Drawable createTwoColumns(Drawable left, Drawable right) => new GridContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            ColumnDimensions = new[]
            {
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, column_spacing),
                new Dimension(),
            },
            RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
            Content = new[] { new[] { left, new Container(), right } },
        };

        private static Drawable createSetupComparison(SessionSummary summary)
        {
            var rows = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
                Child = new OsuSpriteText
                {
                    Text = "By setup",
                    Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.SemiBold),
                }
            };

            foreach (var (label, setupSummary) in summary.BySetup)
            {
                rows.Add(new OsuSpriteText
                {
                    Text = $"{label}: {setupSummary.PlayCount} play{(setupSummary.PlayCount == 1 ? string.Empty : "s")}"
                           + $" · UR {formatNumber(setupSummary.AverageUnstableRate)}"
                           + $" · {formatAccuracy(setupSummary.AverageAccuracy)}",
                    Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE),
                });
            }

            return rows;
        }

        private static Drawable createPlayList(IReadOnlyList<SessionPlayRecord> allPlays)
        {
            var rows = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
            };

            rows.Add(new OsuSpriteText
            {
                Text = allPlays.Count > max_listed_plays ? $"Plays (latest {max_listed_plays} of {allPlays.Count})" : "Plays",
                Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.SemiBold),
            });

            rows.Add(createPlayRow("Time", "Beatmap", "Setup", "Accuracy", "UR", "Hit error", "Misses", isHeader: true));

            foreach (var play in allPlays.Reverse().Take(max_listed_plays))
            {
                rows.Add(createPlayRow(
                    play.PlayedAt.LocalDateTime.ToString("d MMM HH:mm"),
                    play.Beatmap,
                    play.Setup.Label,
                    formatAccuracy(play.Accuracy),
                    formatNumber(play.UnstableRate),
                    formatHitError(play.AverageHitError),
                    play.MissCount.ToString(),
                    isHeader: false));
            }

            return rows;
        }

        private static Drawable createPlayRow(string time, string beatmap, string setup, string accuracy, string unstableRate, string hitError, string misses, bool isHeader)
        {
            var font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: isHeader ? FontWeight.Bold : FontWeight.Regular);
            Color4 colour = isHeader ? Color4.White.Opacity(0.6f) : Color4.White;

            // long texts are truncated (with the full text available as a tooltip) rather than spilling into the next column.
            Drawable cell(string text, bool truncate = false) => truncate
                ? new TruncatingSpriteText
                {
                    Text = text,
                    Font = font,
                    Colour = colour,
                    RelativeSizeAxes = Axes.X,
                }
                : new OsuSpriteText
                {
                    Text = text,
                    Font = font,
                    Colour = colour,
                };

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 110),
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 400),
                    new Dimension(GridSizeMode.Absolute, 80),
                    new Dimension(GridSizeMode.Absolute, 70),
                    new Dimension(GridSizeMode.Absolute, 120),
                    new Dimension(GridSizeMode.Absolute, 60),
                },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    new[]
                    {
                        cell(time),
                        cell(beatmap, truncate: true),
                        cell(setup, truncate: true),
                        cell(accuracy),
                        cell(unstableRate),
                        cell(hitError),
                        cell(misses),
                    }
                },
            };
        }

        // the game's own formatter, so accuracy never shows a different number than the results screen does.
        private static string formatAccuracy(double accuracy) => accuracy.FormatAccuracy().ToString();

        private static string formatNumber(double? value) => value?.ToString("N2") ?? "-";

        private static string formatHitError(double? value)
            => value == null ? "-" : $"{Math.Abs(value.Value):N2} ms {(value.Value < 0 ? "early" : "late")}";

        private partial class TextItem : SimpleStatisticItem<string>
        {
            public TextItem(LocalisableString name, string value)
                : base(name)
            {
                Value = value;
            }

            protected override LocalisableString DisplayValue(string value) => value;
        }
    }
}
