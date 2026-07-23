// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
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
        private const float article_width = 820;

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
            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding
                {
                    Horizontal = 30,
                    Top = 20,
                    Bottom = 80,
                },
                Child = new FillFlowContainer
                {
                    Width = article_width,
                    AutoSizeAxes = Axes.Y,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 22),
                    Children = new Drawable[]
                    {
                        createHero(colourProvider),
                        createHeading(),
                        createIntroduction(),
                        createArticleBody(),
                    },
                },
            };
        }

        private Drawable createHero(OverlayColourProvider colourProvider)
        {
            return new Container
            {
                Width = article_width,
                Height = 210,
                Masking = true,
                CornerRadius = 6,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background6,
                    },
                    createHeroImage(),
                    new DateBadge(post.PublishedAt)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Margin = new MarginPadding
                        {
                            Top = 12,
                            Right = 16,
                        },
                    },
                },
            };
        }

        private Drawable createHeroImage()
        {
            if (string.IsNullOrWhiteSpace(post.FirstImage))
            {
                return new NewsPostBackground(string.Empty)
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };
            }

            return new DelayedLoadUnloadWrapper(() => new NewsPostBackground(post.FirstImage)
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fill,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            })
            {
                RelativeSizeAxes = Axes.Both,
            };
        }

        private Drawable createHeading()
        {
            return new FillFlowContainer
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 3),
                Children = new Drawable[]
                {
                    new TextFlowContainer(text =>
                    {
                        text.Font = OsuFont.GetFont(
                            size: 30,
                            weight: FontWeight.Bold);
                    })
                    {
                        Width = article_width,
                        AutoSizeAxes = Axes.Y,
                        Text = post.Title,
                    },
                    new TextFlowContainer(text =>
                    {
                        text.Font = OsuFont.GetFont(
                            size: 14,
                            weight: FontWeight.Bold);
                    })
                    {
                        Width = article_width,
                        AutoSizeAxes = Axes.Y,
                        Text = $"by {post.Author}",
                    },
                },
            };
        }

        private Drawable createIntroduction()
        {
            if (string.IsNullOrWhiteSpace(post.Preview))
                return Empty();

            return new TextFlowContainer(text =>
            {
                text.Font = OsuFont.GetFont(
                    size: 17,
                    weight: FontWeight.Regular);
            })
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Text = post.Preview,
            };
        }

        private Drawable createArticleBody()
        {
            string content = post.Content ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
                content = "This article does not contain any content.";

            return new NewsMarkdownContainer
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Text = content,
            };
        }

        private partial class DateBadge : CircularContainer
        {
            private readonly DateTimeOffset date;

            public DateBadge(DateTimeOffset date)
            {
                this.date = date;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                AutoSizeAxes = Axes.Both;
                Masking = true;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background6.Opacity(0.65f),
                    },
                    new OsuSpriteText
                    {
                        Font = OsuFont.GetFont(
                            size: 10,
                            weight: FontWeight.Bold),
                        Text = date.ToString("dd MMM yyyy").ToUpperInvariant(),
                        Margin = new MarginPadding
                        {
                            Horizontal = 18,
                            Vertical = 6,
                        },
                    },
                };
            }
        }
    }
}