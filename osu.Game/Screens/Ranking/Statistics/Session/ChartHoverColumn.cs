// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Statistics.Session
{
    /// <summary>
    /// An invisible hoverable strip which shows a tooltip and a subtle highlight, used to make chart points inspectable.
    /// </summary>
    public partial class ChartHoverColumn : Container, IHasTooltip
    {
        public LocalisableString TooltipText { get; }

        private readonly Box highlight;

        public ChartHoverColumn(LocalisableString tooltipText)
        {
            TooltipText = tooltipText;

            Child = highlight = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.White,
                Alpha = 0,
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            highlight.FadeTo(0.08f, 100);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            highlight.FadeOut(150);
        }
    }
}
