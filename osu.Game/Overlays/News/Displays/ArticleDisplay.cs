// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
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
        private const float article_width = 720;

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
                    Bottom = 80
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
                        createArticleBody(colourProvider)
                    }
                }
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
                        Colour = colourProvider.Background6
                    },
                    createHeroImage(),
                    new DateBadge(post.PublishedAt)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Margin = new MarginPadding
                        {
                            Top = 12,
                            Right = 16
                        }
                    }
                }
            };
        }

        private Drawable createHeroImage()
        {
            if (string.IsNullOrWhiteSpace(post.FirstImage))
            {
                return new NewsPostBackground(null)
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                };
            }

            return new DelayedLoadUnloadWrapper(() => new NewsPostBackground(post.FirstImage)
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fill,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            })
            {
                RelativeSizeAxes = Axes.Both
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
                        Text = post.Title
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
                        Text = $"by {post.Author}"
                    }
                }
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
                Text = post.Preview
            };
        }

        private Drawable createArticleBody(OverlayColourProvider colourProvider)
        {
            var body = new FillFlowContainer
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 18)
            };

            string content = post.Content ?? string.Empty;

            string[] blocks = content
                              .Replace("\r\n", "\n")
                              .Split(
                                  new[] { "\n\n" },
                                  StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawBlock in blocks)
            {
                string block = rawBlock.Trim();

                if (string.IsNullOrWhiteSpace(block))
                    continue;

                if (block.StartsWith("## "))
                {
                    body.Add(createSectionHeading(
                        block.Substring(3),
                        colourProvider));

                    continue;
                }

                if (block.StartsWith("# "))
                {
                    body.Add(createSectionHeading(
                        block.Substring(2),
                        colourProvider));

                    continue;
                }

                if (block.StartsWith("> "))
                {
                    body.Add(createQuote(
                        block.Substring(2),
                        colourProvider));

                    continue;
                }

                string[] lines = block.Split('\n');

                if (lines.All(line => line.TrimStart().StartsWith("- ")))
                {
                    body.Add(createList(lines));
                    continue;
                }

                body.Add(createParagraph(block));
            }

            if (body.Children.Count == 0)
            {
                body.Add(createParagraph(
                    "This article does not contain any content."));
            }

            return body;
        }

        private Drawable createSectionHeading(
            string text,
            OverlayColourProvider colourProvider)
        {
            return new FillFlowContainer
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Children = new Drawable[]
                {
                    new TextFlowContainer(sprite =>
                    {
                        sprite.Font = OsuFont.GetFont(
                            size: 24,
                            weight: FontWeight.Bold);
                    })
                    {
                        Width = article_width,
                        AutoSizeAxes = Axes.Y,
                        Text = text
                    },
                    new Box
                    {
                        Width = article_width,
                        Height = 1,
                        Colour = colourProvider.Light1.Opacity(0.25f)
                    }
                }
            };
        }

        private Drawable createParagraph(string text)
        {
            return new TextFlowContainer(sprite =>
            {
                sprite.Font = OsuFont.GetFont(
                    size: 16,
                    weight: FontWeight.Regular);
            })
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Text = text.Replace("\n", " ")
            };
        }

        private Drawable createList(string[] lines)
        {
            var list = new FillFlowContainer
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Padding = new MarginPadding
                {
                    Left = 12
                }
            };

            foreach (string line in lines)
            {
                string item = line.Trim();

                if (item.StartsWith("- "))
                    item = item.Substring(2);

                list.Add(new TextFlowContainer(sprite =>
                {
                    sprite.Font = OsuFont.GetFont(
                        size: 16,
                        weight: FontWeight.Regular);
                })
                {
                    Width = article_width - 12,
                    AutoSizeAxes = Axes.Y,
                    Text = $"•  {item}"
                });
            }

            return list;
        }

        private Drawable createQuote(
            string text,
            OverlayColourProvider colourProvider)
        {
            return new Container
            {
                Width = article_width,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 5,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 4,
                        Colour = colourProvider.Light1
                    },
                    new TextFlowContainer(sprite =>
                    {
                        sprite.Font = OsuFont.GetFont(
                            size: 16,
                            weight: FontWeight.Regular);
                    })
                    {
                        Width = article_width,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding
                        {
                            Horizontal = 20,
                            Vertical = 16
                        },
                        Text = text
                    }
                }
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
                        Colour = colourProvider.Background6.Opacity(0.65f)
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
                            Vertical = 6
                        }
                    }
                };
            }
        }
    }
}