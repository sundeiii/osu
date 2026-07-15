// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Overlays.Dashboard.Home.News
{
    public partial class NewsTitleLink : OsuHoverContainer
    {
        private readonly APINewsPost post;

        [Resolved]
        private NewsOverlay newsOverlay { get; set; } = null!;

        public NewsTitleLink(APINewsPost post)
        {
            this.post = post;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Child = new TextFlowContainer(text =>
            {
                text.Font = OsuFont.GetFont(
                    weight: FontWeight.Bold);
            })
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = post.Title
            };

            HoverColour = colourProvider.Light1;
            TooltipText = "read news";

            Action = () => newsOverlay.ShowArticle(post.Slug);
        }
    }
}