// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osuTK;

namespace osu.Game.Overlays.News.Displays
{
    public partial class ArticleDisplay : CompositeDrawable
    {
        private readonly APINewsPost post;

        public ArticleDisplay(APINewsPost post)
        {
            this.post = post;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 20),
                Padding = new MarginPadding
                {
                    Horizontal = 40,
                    Top = 30,
                    Bottom = 60
                },
                Children = new Drawable[]
                {
                    createHeaderImage(),
                    createTitle(),
                    createMetadata(colourProvider),
                    createDivider(colourProvider),
                    createContent()
                }
            };
        }

        private Drawable createHeaderImage()
        {
            if (string.IsNullOrWhiteSpace(post.FirstImage))
                return Empty();

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 320,
                Masking = true,
                CornerRadius = 10,
                Child = new NewsPostBackground(post.FirstImage)
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                }
            };
        }

        private Drawable createTitle()
        {
            return new TextFlowContainer(text =>
            {
                text.Font = OsuFont.GetFont(
                    size: 30,
                    weight: FontWeight.Bold);
            })
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = post.Title ?? post.Slug
            };
        }

        private Drawable createMetadata(OverlayColourProvider colourProvider)
        {
            string author = string.IsNullOrWhiteSpace(post.Author)
                ? "rinari"
                : post.Author;

            string date = post.PublishedAt == default
                ? string.Empty
                : post.PublishedAt.ToLocalisableString(@"dd MMMM yyyy");

            string metadata = string.IsNullOrWhiteSpace(date)
                ? $"by {author}"
                : $"by {author} · {date}";

            return new TextFlowContainer(text =>
            {
                text.Font = OsuFont.GetFont(
                    size: 14,
                    weight: FontWeight.Regular);

                text.Colour = colourProvider.Light1;
            })
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = metadata
            };
        }

        private Drawable createDivider(OverlayColourProvider colourProvider)
        {
            return new Box
            {
                RelativeSizeAxes = Axes.X,
                Height = 2,
                Colour = colourProvider.Background4
            };
        }

        private Drawable createContent()
        {
            string content = post.Content;

            if (string.IsNullOrWhiteSpace(content))
                content = post.Preview;

            if (string.IsNullOrWhiteSpace(content))
                content = "This news post does not contain any content.";

            return new TextFlowContainer(text =>
            {
                text.Font = OsuFont.GetFont(
                    size: 18,
                    weight: FontWeight.Regular);
            })
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = content
            };
        }
    }
}