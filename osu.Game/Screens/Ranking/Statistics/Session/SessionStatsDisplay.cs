// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Screens.Ranking.Statistics.Session
{
    /// <summary>
    /// Shows how the current session is going: summary numbers, unstable rate per play, and a comparison between setups.
    /// </summary>
    public partial class SessionStatsDisplay : CompositeDrawable
    {
        private const float bar_area_height = 90;
        private const float bar_width = 14;
        private const float bar_spacing = 3;
        private const int max_bars = 30;

        [Resolved]
        private SessionStatsStore store { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            var plays = store.CurrentSessionPlays;
            var summary = SessionSummary.Create(plays);

            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 12),
            };

            flow.Add(new SimpleStatisticTable(2, new SimpleStatisticItem[]
            {
                new TextItem("Plays this session", summary.PlayCount.ToString()),
                new TextItem("Average accuracy", summary.AverageAccuracy.ToString("P2")),
                new TextItem("Average unstable rate", formatNumber(summary.AverageUnstableRate)),
                new TextItem("Best unstable rate", formatNumber(summary.BestUnstableRate)),
                new TextItem("Average hit error", formatHitError(summary.AverageHitError)),
                new TextItem("Total misses", summary.TotalMisses.ToString()),
            }));

            var chartPlays = plays.Where(p => p.UnstableRate != null).TakeLast(max_bars).ToList();

            if (chartPlays.Count > 1)
                flow.Add(createChart(chartPlays));

            if (summary.BySetup.Count > 1)
                flow.Add(createSetupComparison(summary));

            InternalChild = flow;
        }

        private Drawable createChart(IReadOnlyList<SessionPlayRecord> chartPlays)
        {
            double maxUnstableRate = chartPlays.Max(p => p.UnstableRate!.Value);

            var bars = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(bar_spacing, 0),
            };

            for (int i = 0; i < chartPlays.Count; i++)
            {
                var play = chartPlays[i];
                bool isLatest = i == chartPlays.Count - 1;

                bars.Add(new PlayBar(play)
                {
                    Size = new Vector2(bar_width, bar_area_height),
                    Child = new Box
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        RelativeSizeAxes = Axes.Both,
                        // never let a bar fully disappear.
                        Height = (float)Math.Max(play.UnstableRate!.Value / maxUnstableRate, 0.03),
                        Colour = isLatest ? colours.Yellow : colours.Blue,
                    }
                });
            }

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = "Unstable rate per play (lower is better, latest on the right)",
                        Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.SemiBold),
                    },
                    bars,
                }
            };
        }

        private Drawable createSetupComparison(SessionSummary summary)
        {
            var rows = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
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
                           + $" · {setupSummary.AverageAccuracy:P2}",
                    Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE),
                });
            }

            return rows;
        }

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

        private partial class PlayBar : Container, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            public PlayBar(SessionPlayRecord play)
            {
                TooltipText = $"{play.Beatmap}\nUR {play.UnstableRate:N2} · {play.Accuracy:P2} · {play.Setup.Label}";
            }
        }
    }
}
