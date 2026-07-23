// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Text;
using System.Text.RegularExpressions;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Containers.Markdown;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game;
using osu.Game.Audio;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Containers.Markdown;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Users;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.News.Displays
{
    public partial class NewsMarkdownContainer : OsuMarkdownContainer
    {
        protected override OsuMarkdownContainerOptions Options => new OsuMarkdownContainerOptions
        {
            CustomContainers = true,
            BlockAttributes = true,
            Footnotes = true,
        };

        protected override void AddMarkdownComponent(
            IMarkdownObject markdownObject,
            FillFlowContainer container,
            int level)
        {
            if (markdownObject is ParagraphBlock paragraphBlock)
            {
                string paragraphText = getInlineText(paragraphBlock.Inline);

                if (tryGetEmbedMarker(paragraphText, out string embedType, out string embedUrl))
                {
                    container.Add(new NewsEmbedCard(embedType, embedUrl));
                    return;
                }
            }

            if (markdownObject is HeadingBlock headingBlock)
            {
                string headingText = getHeadingText(headingBlock);

                if (headingText.Contains("@@FLAG:", StringComparison.Ordinal))
                {
                    container.Add(new NewsFlagHeading(headingBlock, headingText));
                    return;
                }
            }

            base.AddMarkdownComponent(markdownObject, container, level);
        }

        public override OsuMarkdownTextFlowContainer CreateTextFlow()
            => new NewsMarkdownTextFlowContainer();

        private static string getHeadingText(HeadingBlock headingBlock)
        {
            var builder = new StringBuilder();
            appendInlineText(builder, headingBlock.Inline);
            return builder.ToString();
        }

        private static string getInlineText(ContainerInline inline)
        {
            var builder = new StringBuilder();
            appendInlineText(builder, inline);
            return builder.ToString();
        }

        private static bool tryGetEmbedMarker(string text, out string type, out string url)
        {
            type = string.Empty;
            url = string.Empty;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var match = Regex.Match(
                text.Trim(),
                @"^@@EMBED:(?<type>iframe|video|audio):(?<url>.+)@@$",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return false;

            type = match.Groups["type"].Value.ToLowerInvariant();
            url = match.Groups["url"].Value.Trim();

            return !string.IsNullOrWhiteSpace(url);
        }

        private static void appendInlineText(StringBuilder builder, Inline inline)
        {
            if (inline == null)
                return;

            if (inline is ContainerInline containerInline)
            {
                for (Inline child = containerInline.FirstChild; child != null; child = child.NextSibling)
                    appendInlineText(builder, child);

                return;
            }

            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;

                case CodeInline code:
                    builder.Append(code.Content);
                    break;

                case LineBreakInline:
                    builder.Append(' ');
                    break;
            }
        }

        private partial class NewsMarkdownTextFlowContainer : OsuMarkdownTextFlowContainer
        {
            protected override void AddImage(LinkInline linkInline)
            {
                string url = normaliseUrl(linkInline.Url ?? string.Empty);

                if (isAvatarUrl(url))
                {
                    AddDrawable(new InlineNewsAvatar(url)
                    {
                        Margin = new MarginPadding
                        {
                            Vertical = 4,
                            Right = 8,
                        },
                    });

                    return;
                }

                if (tryGetParentLinkUrl(linkInline, out string parentUrl)
                    && tryGetBeatmapId(normaliseUrl(parentUrl), out int beatmapId))
                {
                    AddDrawable(new NewsBeatmapLinkedImage(linkInline, beatmapId));
                    return;
                }

                base.AddImage(linkInline);
            }

            protected override void AddLinkText(string text, LinkInline linkInline)
            {
                string url = normaliseUrl(linkInline.Url ?? string.Empty);

                if (tryGetBeatmapId(url, out int beatmapId))
                {
                    AddDrawable(new NewsBeatmapLinkText(text, linkInline, beatmapId));
                    return;
                }

                base.AddLinkText(text, linkInline);
            }

            private static bool tryGetBeatmapId(string url, out int beatmapId)
            {
                beatmapId = 0;

                if (string.IsNullOrWhiteSpace(url))
                    return false;

                Match hashMatch = Regex.Match(
                    url,
                    @"#(?:osu|taiko|fruits|mania)/(?<id>\d+)",
                    RegexOptions.IgnoreCase);

                if (hashMatch.Success)
                    return int.TryParse(hashMatch.Groups["id"].Value, out beatmapId);

                Match beatmapMatch = Regex.Match(
                    url,
                    @"/beatmaps/(?<id>\d+)",
                    RegexOptions.IgnoreCase);

                if (beatmapMatch.Success)
                    return int.TryParse(beatmapMatch.Groups["id"].Value, out beatmapId);

                return false;
            }

            private static bool tryGetParentLinkUrl(LinkInline imageLinkInline, out string url)
            {
                url = string.Empty;

                if (imageLinkInline.Parent is not LinkInline parentLink)
                    return false;

                if (parentLink.IsImage)
                    return false;

                if (string.IsNullOrWhiteSpace(parentLink.Url))
                    return false;

                url = parentLink.Url;
                return true;
            }

            private partial class NewsBeatmapLinkedImage : OsuClickableContainer
            {
                private readonly int beatmapId;

                [Resolved]
                private OsuGame game { get; set; }

                public NewsBeatmapLinkedImage(LinkInline linkInline, int beatmapId)
                {
                    this.beatmapId = beatmapId;

                    AutoSizeAxes = Axes.Both;
                    Action = () => game.ShowBeatmap(beatmapId);

                    Child = new OsuMarkdownImage(linkInline);
                }
            }

            private partial class NewsBeatmapLinkText : OsuMarkdownLinkText
            {
                private readonly int beatmapId;

                [Resolved]
                private OsuGame game { get; set; }

                public NewsBeatmapLinkText(string text, LinkInline linkInline, int beatmapId)
                    : base(text, linkInline)
                {
                    this.beatmapId = beatmapId;
                }

                protected override void OnLinkPressed()
                {
                    game.ShowBeatmap(beatmapId);
                }
            }

            private static bool isAvatarUrl(string url)
                => url.Contains("/wiki/shared/avatars/", StringComparison.OrdinalIgnoreCase)
                   || url.Contains("a.ppy.sh/", StringComparison.OrdinalIgnoreCase);

            private static string normaliseUrl(string url)
            {
                if (string.IsNullOrWhiteSpace(url))
                    return string.Empty;

                url = url.Trim();

                if (url.StartsWith("/", StringComparison.Ordinal))
                    return "https://osu.ppy.sh" + url;

                return url;
            }
        }

        private partial class NewsFlagHeading : CompositeDrawable
        {
            private readonly HeadingBlock headingBlock;
            private readonly string headingText;

            public NewsFlagHeading(HeadingBlock headingBlock, string headingText)
            {
                this.headingBlock = headingBlock;
                this.headingText = headingText;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                int level = Math.Clamp(headingBlock.Level, 1, 6);
                float fontSize = getHeadingFontSize(level);

                var flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    Margin = new MarginPadding
                    {
                        Top = 20,
                        Bottom = 6,
                    },
                };

                addHeadingParts(flow, headingText, fontSize);

                InternalChild = flow;
            }

            private static void addHeadingParts(
                FillFlowContainer flow,
                string text,
                float fontSize)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return;

                int index = 0;

                foreach (Match match in Regex.Matches(text, @"@@FLAG:(?<flag>[A-Z]{2})@@"))
                {
                    if (match.Index > index)
                    {
                        addHeadingText(
                            flow,
                            text.Substring(index, match.Index - index),
                            fontSize);
                    }

                    string countryCode = match.Groups["flag"].Value;

                    if (Enum.TryParse(countryCode, true, out CountryCode parsed))
                    {
                        flow.Add(new InlineNewsFlag(parsed)
                        {
                            Margin = new MarginPadding
                            {
                                Right = 3,
                                Top = 4,
                            },
                        });
                    }

                    index = match.Index + match.Length;
                }

                if (index < text.Length)
                    addHeadingText(flow, text.Substring(index), fontSize);
            }

            private static void addHeadingText(
                FillFlowContainer flow,
                string text,
                float fontSize)
            {
                text = text.Replace("  ", " ").TrimStart();

                if (string.IsNullOrWhiteSpace(text))
                    return;

                flow.Add(new OsuSpriteText
                {
                    Text = text,
                    Font = OsuFont.GetFont(
                        size: fontSize,
                        weight: FontWeight.Bold),
                });
            }

            private static float getHeadingFontSize(int level)
            {
                switch (level)
                {
                    case 1:
                        return 30;

                    case 2:
                        return 24;

                    case 3:
                        return 20;

                    case 4:
                        return 17;

                    case 5:
                        return 15;

                    default:
                        return 14;
                }
            }
        }

        private partial class NewsEmbedCard : CompositeDrawable
        {
            private readonly string type;
            private readonly string url;

            public NewsEmbedCard(string type, string url)
            {
                this.type = type;
                this.url = url;

                RelativeSizeAxes = Axes.X;
                Masking = true;
                CornerRadius = 12;
                Margin = new MarginPadding
                {
                    Vertical = 10,
                };

                Height = isAudioUrl(url) ? 96 : 126;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                string title = type switch
                {
                    "audio" => "Audio preview",
                    "video" => "Embedded video",
                    _ => "Embedded video",
                };

                if (!isAudioUrl(url))
                {
                    InternalChild = new NewsVideoEmbedCard(url, title);
                    return;
                }

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(16),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 5,
                        Colour = colours.Yellow,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Left = 18,
                            Right = 14,
                            Vertical = 12,
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 54),
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute, 74),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Container
                                {
                                    Width = 42,
                                    Height = 42,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    CornerRadius = 9,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = colours.Yellow,
                                        },
                                        new SpriteIcon
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Size = new Vector2(19),
                                            Icon = FontAwesome.Solid.Music,
                                        },
                                    },
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Spacing = new Vector2(0, 4),
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Text = title,
                                            Font = OsuFont.GetFont(size: 17, weight: FontWeight.Bold),
                                        },
                                        new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = url,
                                            Font = OsuFont.GetFont(size: 11),
                                            Colour = colours.GrayB,
                                        },
                                    },
                                },
                                new AudioEmbedPlayer(url)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                },
                            },
                        },
                    },
                };
            }

            private static bool isAudioUrl(string candidate)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    return false;

                return candidate.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                       || candidate.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
                       || candidate.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);
            }
        }

        private partial class NewsVideoEmbedCard : CompositeDrawable
        {
            private readonly string url;
            private readonly string title;

            private Box background;
            private Box buttonBackground;
            private SpriteIcon playIcon;

            private Color4 buttonNormalColour;
            private Color4 buttonHoverColour;
            private Color4 backgroundNormalColour;
            private Color4 backgroundHoverColour;

            [Resolved(canBeNull: true)]
            private INewsVideoEmbedHost videoEmbedHost { get; set; }

            public NewsVideoEmbedCard(string url, string title)
            {
                this.url = url;
                this.title = title;

                RelativeSizeAxes = Axes.X;
                Height = 126;
                Masking = true;
                CornerRadius = 12;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                string platform = getPlatformName(url);

                backgroundNormalColour = OsuColour.Gray(15);
                backgroundHoverColour = OsuColour.Gray(19);
                buttonNormalColour = colours.Purple3;
                buttonHoverColour = new Color4(0.48f, 0.30f, 1f, 1f);

                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = backgroundNormalColour,
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 5,
                        Colour = colours.Purple3,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Left = 22,
                            Right = 22,
                            Vertical = 16,
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 58),
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute, 168),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new OsuClickableContainer
                                {
                                    Width = 46,
                                    Height = 46,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Action = openVideo,
                                    Child = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Masking = true,
                                        CornerRadius = 10,
                                        Children = new Drawable[]
                                        {
                                            buttonBackground = new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = buttonNormalColour,
                                            },
                                            playIcon = new SpriteIcon
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Size = new Vector2(21),
                                                Icon = FontAwesome.Solid.Play,
                                            },
                                        },
                                    },
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Spacing = new Vector2(0, 5),
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Text = title,
                                            Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold),
                                        },
                                        new OsuSpriteText
                                        {
                                            Text = platform,
                                            Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                            Colour = colours.GrayA,
                                        },
                                        new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = url,
                                            Font = OsuFont.GetFont(size: 10),
                                            Colour = colours.Gray7,
                                        },
                                    },
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new RoundedButton
                                    {
                                        Width = 150,
                                        Height = 46,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = "watch video",
                                        BackgroundColour = colours.Purple3,
                                        Action = openVideo,
                                    },
                                },
                            },
                        },
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeColour(backgroundHoverColour, 120, Easing.OutQuint);
                buttonBackground.FadeColour(buttonHoverColour, 120, Easing.OutQuint);
                playIcon.ScaleTo(1.08f, 120, Easing.OutQuint);

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeColour(backgroundNormalColour, 120, Easing.OutQuint);
                buttonBackground.FadeColour(buttonNormalColour, 120, Easing.OutQuint);
                playIcon.ScaleTo(1, 120, Easing.OutQuint);

                base.OnHoverLost(e);
            }

            private void openVideo()
            {
                videoEmbedHost?.OpenVideo(url, title);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                videoEmbedHost?.CloseVideo();
            }

            private static string getPlatformName(string candidate)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    return "Video";

                if (candidate.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                    || candidate.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                    return "YouTube video";

                if (candidate.Contains("twitch.tv", StringComparison.OrdinalIgnoreCase))
                    return "Twitch video";

                return "External video";
            }
        }

        private partial class AudioEmbedPlayer : Container
        {
            public IBindable<bool> Playing => playing;

            private readonly BindableBool playing = new BindableBool();
            private readonly string url;

            private PreviewTrack preview;
            private Color4 hoverColour;
            private readonly SpriteIcon icon;
            private readonly LoadingSpinner loadingSpinner;

            private const float transition_duration = 500;

            [Resolved]
            private PreviewTrackManager previewTrackManager { get; set; }

            public AudioEmbedPlayer(string url)
            {
                this.url = url;

                Width = 42;
                Height = 42;
                AutoSizeAxes = Axes.None;
                RelativeSizeAxes = Axes.None;

                AddRange(new Drawable[]
                {
                    icon = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        FillMode = FillMode.Fit,
                        RelativeSizeAxes = Axes.Both,
                        Icon = FontAwesome.Solid.Play,
                    },
                    loadingSpinner = new LoadingSpinner
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(15),
                    },
                });

                playing.ValueChanged += playingStateChanged;
            }

            private bool loading
            {
                set
                {
                    if (value)
                    {
                        icon.FadeTo(0.5f, transition_duration, Easing.OutQuint);
                        loadingSpinner.Show();
                    }
                    else
                    {
                        icon.FadeTo(1, transition_duration, Easing.OutQuint);
                        loadingSpinner.Hide();
                    }
                }
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colour)
            {
                hoverColour = colour.Yellow;
            }

            protected override bool OnClick(ClickEvent e)
            {
                playing.Toggle();
                return true;
            }

            protected override bool OnHover(HoverEvent e)
            {
                icon.FadeColour(hoverColour, 120, Easing.InOutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (!playing.Value)
                    icon.FadeColour(Color4.White, 120, Easing.InOutQuint);

                base.OnHoverLost(e);
            }

            private void playingStateChanged(ValueChangedEvent<bool> e)
            {
                icon.Icon = e.NewValue ? FontAwesome.Solid.Stop : FontAwesome.Solid.Play;
                icon.FadeColour(e.NewValue || IsHovered ? hoverColour : Color4.White, 120, Easing.InOutQuint);

                if (e.NewValue)
                {
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        playing.Value = false;
                        return;
                    }

                    if (preview != null)
                    {
                        attemptStart();
                        return;
                    }

                    loading = true;

                    LoadComponentAsync(preview = previewTrackManager.GetUrl(url), loadedPreview =>
                    {
                        Schedule(() =>
                        {
                            if (preview != loadedPreview)
                            {
                                loadedPreview?.Dispose();
                                return;
                            }

                            AddInternal(loadedPreview);
                            loading = false;

                            loadedPreview.Stopped += () => Schedule(() => playing.Value = false);

                            if (playing.Value)
                                attemptStart();
                        });
                    });
                }
                else
                {
                    preview?.Stop();
                    loading = false;
                }
            }

            private void attemptStart()
            {
                if (preview?.Start() != true)
                    playing.Value = false;
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                preview?.Stop();
                preview?.Dispose();
                preview = null;
            }
        }

        private partial class InlineNewsFlag : CompositeDrawable
        {
            public InlineNewsFlag(CountryCode countryCode)
            {
                Size = new Vector2(18, 12);

                InternalChild = new UpdateableFlag
                {
                    RelativeSizeAxes = Axes.Both,
                    CountryCode = countryCode,
                };
            }
        }

        private partial class InlineNewsAvatar : Sprite
        {
            private readonly string url;

            public InlineNewsAvatar(string url)
            {
                this.url = url;

                Size = new Vector2(42, 42);
                FillMode = FillMode.Fit;
            }

            [BackgroundDependencyLoader]
            private void load(LargeTextureStore textures)
            {
                Texture = textures.Get(url);
            }
        }
    }
}