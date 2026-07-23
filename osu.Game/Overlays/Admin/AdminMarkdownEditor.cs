// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.News.Displays;
using osuTK;

namespace osu.Game.Overlays.Admin
{
    public partial class AdminMarkdownEditor : CompositeDrawable
    {
        public Bindable<string> Current { get; } = new Bindable<string>(string.Empty);

        public string Text => getCurrentText();

        public Action<NewsFrontMatter> FrontMatterPasted;

        private FillFlowContainer lineFlow;
        private OsuTextBox insertBox;
        private OsuSpriteText statusText;
        private NewsMarkdownContainer markdownPreview;
        private OsuColour colours;

        private readonly List<LineEntry> lines = new List<LineEntry>();

        private bool rebuilding;

        public AdminMarkdownEditor()
        {
            RelativeSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = 10;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            this.colours = colours;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = OsuColour.Gray(10),
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(12),
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        createTopToolbar(colours),
                        createSecondToolbar(colours),

                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Relative, 0.50f),
                                new Dimension(GridSizeMode.Absolute, 14),
                                new Dimension(GridSizeMode.Relative, 0.50f),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    createLineEditorPanel(),
                                    Empty(),
                                    createMarkdownPreviewPanel(),
                                },
                            },
                        },
                    },
                },
            };

            insertBox.OnCommit += (_, _) => appendInsertBox(false);

            Current.BindValueChanged(change =>
            {
                if (rebuilding)
                    return;

                setLinesFromText(change.NewValue ?? string.Empty);
            }, true);
        }

        private Drawable createTopToolbar(OsuColour colours)
        {
            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 40,
                ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 130),
                    new Dimension(GridSizeMode.Absolute, 130),
                    new Dimension(GridSizeMode.Absolute, 130),
                    new Dimension(GridSizeMode.Absolute, 130),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        insertBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 40,
                            LengthLimit = 30000,
                            PlaceholderText = "quick line input only; use paste body for full posts",
                        },
                        new RoundedButton
                        {
                            Width = 118,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "add text",
                            BackgroundColour = colours.Blue3,
                            Action = () => appendInsertBox(false),
                        },
                        new RoundedButton
                        {
                            Width = 118,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "add heading",
                            BackgroundColour = colours.Purple3,
                            Action = () => appendInsertBox(true),
                        },
                        new RoundedButton
                        {
                            Width = 118,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "paste body",
                            BackgroundColour = colours.Green3,
                            Action = pasteBodyFromClipboard,
                        },
                        new RoundedButton
                        {
                            Width = 118,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "clear all",
                            BackgroundColour = colours.Red3,
                            Action = Clear,
                        },
                    },
                },
            };
        }

        private Drawable createSecondToolbar(OsuColour colours)
        {
            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 34,
                ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 130),
                    new Dimension(GridSizeMode.Absolute, 130),
                    new Dimension(GridSizeMode.Absolute, 130),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        statusText = new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = "0 lines",
                            Font = OsuFont.GetFont(size: 12),
                            Colour = colours.GrayB,
                        },
                        new RoundedButton
                        {
                            Width = 118,
                            Height = 30,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "blank line",
                            BackgroundColour = colours.Gray5,
                            Action = () => addLine(lines.Count, string.Empty),
                        },
                        new RoundedButton
                        {
                            Width = 118,
                            Height = 30,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "quote",
                            BackgroundColour = colours.Orange3,
                            Action = () => appendPrefixed("> "),
                        },
                        new RoundedButton
                        {
                            Width = 118,
                            Height = 30,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "bullet",
                            BackgroundColour = colours.Green3,
                            Action = () => appendPrefixed("- "),
                        },
                    },
                },
            };
        }

        private Drawable createLineEditorPanel()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(8),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(8),
                        Spacing = new Vector2(0, 6),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = "raw editable markdown",
                                Font = OsuFont.GetFont(
                                    size: 12,
                                    weight: FontWeight.Bold),
                                Colour = colours.GrayB,
                            },

                            new BasicScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = lineFlow = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 6),
                                },
                            },
                        },
                    },
                },
            };
        }

        private Drawable createMarkdownPreviewPanel()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(8),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(8),
                        Spacing = new Vector2(0, 6),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = "rendered preview",
                                Font = OsuFont.GetFont(
                                    size: 12,
                                    weight: FontWeight.Bold),
                                Colour = colours.GrayB,
                            },

                            new BasicScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = markdownPreview = new NewsMarkdownContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text = string.Empty,
                                },
                            },
                        },
                    },
                },
            };
        }

        public void Clear()
        {
            lines.Clear();

            if (lineFlow != null)
                lineFlow.Clear();

            if (insertBox != null)
                insertBox.Text = string.Empty;

            syncCurrentFromLines();
            updateStatus();
            updateMarkdownPreview();
        }

        public void InsertMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            string existing = getCurrentText();

            if (!string.IsNullOrWhiteSpace(existing)
                && !existing.EndsWith('\n'))
            {
                existing += "\n";
            }

            setLinesFromText(existing + text);
            syncCurrentFromLines();
        }

        private void pasteBodyFromClipboard()
        {
            string clipboard = getWindowsClipboardText();

            if (string.IsNullOrWhiteSpace(clipboard))
                return;

            NewsFrontMatter frontMatter = parseFrontMatter(clipboard);
            string body = stripNewsFrontMatter(clipboard);

            body = extractPreviewAndBanner(body, frontMatter);
            body = convertOsuWebNewsMarkdownToClientMarkdown(body);

            setLinesFromText(body);
            syncCurrentFromLines();

            FrontMatterPasted?.Invoke(frontMatter);

            if (insertBox != null)
                insertBox.Text = string.Empty;
        }

        private void appendPrefixed(string prefix)
        {
            if (insertBox == null)
                return;

            string text = insertBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = prefix.TrimEnd();

            addLine(lines.Count, prefix + text);
            insertBox.Text = string.Empty;
        }

        private void appendInsertBox(bool heading)
        {
            if (insertBox == null)
                return;

            string text = insertBox.Text;

            if (string.IsNullOrWhiteSpace(text))
                return;

            string normalised = text.Replace("\r\n", "\n");

            string[] newLines = normalised
                .Split('\n')
                .Select(line => heading && !line.StartsWith("# ", StringComparison.Ordinal)
                    ? "# " + line
                    : line)
                .ToArray();

            foreach (string line in newLines)
                addLine(lines.Count, line);

            insertBox.Text = string.Empty;
        }

        private void setLinesFromText(string text)
        {
            if (lineFlow == null)
                return;

            lines.Clear();

            string normalised = (text ?? string.Empty)
                .Replace("\r\n", "\n");

            if (normalised.Length > 0)
            {
                foreach (string line in normalised.Split('\n'))
                {
                    lines.Add(new LineEntry
                    {
                        Text = preprocessMarkdownLine(line),
                    });
                }
            }

            rebuildLineRows();
            syncCurrentFromLines();
        }

        private void addLine(int index, string text)
        {
            index = Math.Clamp(index, 0, lines.Count);

            lines.Insert(index, new LineEntry
            {
                Text = preprocessMarkdownLine(text ?? string.Empty),
            });

            rebuildLineRows();
            syncCurrentFromLines();
        }

        private void deleteLine(LineEntry entry)
        {
            lines.Remove(entry);

            rebuildLineRows();
            syncCurrentFromLines();
        }

        private void insertLineAfter(LineEntry entry)
        {
            int index = lines.IndexOf(entry);

            if (index < 0)
                index = lines.Count - 1;

            addLine(index + 1, string.Empty);
        }

        private void rebuildLineRows()
        {
            if (lineFlow == null)
                return;

            foreach (LineEntry entry in lines)
            {
                if (entry.TextBox != null)
                    entry.Text = entry.TextBox.Text;
            }

            lineFlow.Clear();

            for (int i = 0; i < lines.Count; i++)
            {
                LineEntry entry = lines[i];
                int lineNumber = i + 1;

                lineFlow.Add(createLineRow(lineNumber, entry));
            }

            updateStatus();
            updateMarkdownPreview();
        }

        private Drawable createLineRow(int lineNumber, LineEntry entry)
        {
            OsuTextBox textBox = new OsuTextBox
            {
                RelativeSizeAxes = Axes.X,
                Height = 36,
                LengthLimit = 30000,
                Text = entry.Text ?? string.Empty,
                PlaceholderText = "markdown line",
            };

            entry.TextBox = textBox;

            textBox.OnCommit += (_, _) =>
            {
                entry.Text = textBox.Text;
                syncCurrentFromLines();
            };

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 38,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 34),
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 76),
                    new Dimension(GridSizeMode.Absolute, 76),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = lineNumber.ToString("N0"),
                            Font = OsuFont.GetFont(
                                size: 12,
                                weight: FontWeight.Bold),
                            Colour = colours.Gray8,
                        },

                        textBox,

                        new RoundedButton
                        {
                            Width = 66,
                            Height = 32,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "+",
                            BackgroundColour = colours.Gray5,
                            Action = () => insertLineAfter(entry),
                        },

                        new RoundedButton
                        {
                            Width = 66,
                            Height = 32,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "delete",
                            BackgroundColour = colours.Red3,
                            Action = () => deleteLine(entry),
                        },
                    },
                },
            };
        }

        private void syncCurrentFromLines()
        {
            string text = getCurrentText();

            rebuilding = true;
            Current.Value = text;
            rebuilding = false;

            updateStatus();
            updateMarkdownPreview();
        }

        private string getCurrentText()
        {
            foreach (LineEntry entry in lines)
            {
                if (entry.TextBox != null)
                    entry.Text = entry.TextBox.Text;
            }

            return string.Join("\n", lines.Select(entry => entry.Text ?? string.Empty));
        }

        private void updateStatus()
        {
            if (statusText == null)
                return;

            int lineCount = lines.Count;

            statusText.Text = lineCount == 1
                ? "1 line · edit left, preview right"
                : $"{lineCount:N0} lines · edit left, preview right";
        }

        private void updateMarkdownPreview()
        {
            if (markdownPreview == null)
                return;

            string markdown = getCurrentText();

            markdownPreview.Text = string.IsNullOrWhiteSpace(markdown)
                ? "_No content yet._"
                : markdown;
        }

        private static string extractPreviewAndBanner(
            string content,
            NewsFrontMatter frontMatter)
        {
            string normalised = (content ?? string.Empty)
                .Replace("\r\n", "\n")
                .TrimStart();

            if (string.IsNullOrWhiteSpace(normalised))
                return string.Empty;

            string[] blocks = normalised.Split(
                new[] { "\n\n" },
                StringSplitOptions.None);

            var remainingBlocks = new List<string>();
            bool previewFound = false;
            bool bannerFound = false;

            foreach (string rawBlock in blocks)
            {
                string block = rawBlock.Trim();

                if (string.IsNullOrWhiteSpace(block))
                    continue;

                if (!previewFound
                    && !block.StartsWith("#", StringComparison.Ordinal)
                    && !block.StartsWith("!", StringComparison.Ordinal)
                    && !block.StartsWith("<", StringComparison.Ordinal))
                {
                    frontMatter.Preview = block;
                    previewFound = true;
                    continue;
                }

                if (previewFound
                    && !bannerFound
                    && tryGetStandaloneMarkdownImage(block, out string imageUrl))
                {
                    frontMatter.FirstImage = normaliseOsuUrl(imageUrl);
                    bannerFound = true;
                    continue;
                }

                remainingBlocks.Add(rawBlock.TrimEnd());
            }

            return string.Join("\n\n", remainingBlocks).Trim();
        }

        private static bool tryGetStandaloneMarkdownImage(
            string block,
            out string imageUrl)
        {
            imageUrl = null;

            var match = Regex.Match(
                block.Trim(),
                @"^!\[[^\]]*\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)$");

            if (!match.Success)
                return false;

            imageUrl = match.Groups["url"].Value;
            return true;
        }

        private static string convertOsuWebNewsMarkdownToClientMarkdown(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            string text = content.Replace("\r\n", "\n");

            text = Regex.Replace(
                text,
                @"<style[\s\S]*?</style>",
                string.Empty,
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"<iframe[\s\S]*?src=""(?<src>[^""]+)""[\s\S]*?</iframe>",
                "\n\n@@EMBED:iframe:${src}@@\n\n",
                RegexOptions.IgnoreCase);

            // Rendered osu-web HTML flag spans -> native osu-wiki flag syntax.
            // Source markdown flags are intentionally left untouched here.
            text = Regex.Replace(
                text,
                @"<span[^>]*class=""[^""]*flag-country[^""]*""[^>]*background-image:\s*url\(['""]?(?<url>[^'"")]+)['""]?\)[^>]*>\s*</span>",
                flagSpanToOsuWikiFlag,
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"^:::\{[^}]+\}::\s*$",
                string.Empty,
                RegexOptions.Multiline);

            text = Regex.Replace(
                text,
                @"^:::\s*$",
                string.Empty,
                RegexOptions.Multiline);

            text = Regex.Replace(
                text,
                @"^::\{#[^}]+\}::\s*$",
                string.Empty,
                RegexOptions.Multiline);

            text = Regex.Replace(
                text,
                @"<audio[\s\S]*?<source\s+src=""(?<src>[^""]+)""[\s\S]*?</audio>",
                "\n\n@@EMBED:audio:${src}@@\n\n",
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"<video[\s\S]*?<source\s+src=""(?<src>[^""]+)""[\s\S]*?</video>",
                "\n\n@@EMBED:video:${src}@@\n\n",
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"<a\s+class=""avatar[^""]*""[^>]*style=""[^""]*background-image:\s*url\(['""]?(?<url>[^'"")]+)['""]?\)[^""]*""[^>]*>\s*</a>\s*<p\s+class=""news-chat-quote__username"">\s*<a[^>]*>(?<name>[^<]+)</a>\s*</p>",
                match =>
                {
                    string avatar = normaliseOsuUrl(match.Groups["url"].Value);
                    string name = match.Groups["name"].Value.Trim();

                    return $"\n![]({avatar}) **{name}**\n";
                },
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"<a\s+class=""avatar[^""]*""[^>]*style=""[^""]*background-image:\s*url\(['""]?(?<url>[^'"")]+)['""]?\)[^""]*""[^>]*>\s*</a>",
                match => "\n![](" + normaliseOsuUrl(match.Groups["url"].Value) + ")\n",
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"<p\s+class=""news-chat-quote__username"">\s*<a[^>]*>(?<name>[^<]+)</a>\s*</p>",
                "\n**${name}**\n",
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"</?div[^>]*>",
                string.Empty,
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"<p[^>]*>",
                string.Empty,
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"</p>",
                "\n\n",
                RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"!\[(?<alt>[^\]]*)\]\(/(?<url>[^)\s]+)(?<title>\s+""[^""]*"")?\)",
                "![${alt}](https://osu.ppy.sh/${url}${title})");

            text = Regex.Replace(
                text,
                @"(?<!!)\]\(/(?<url>[^)]+)\)",
                "](https://osu.ppy.sh/${url})");

            text = Regex.Replace(
                text,
                @"\n{3,}",
                "\n\n");

            text = convertHeadingFlagsToMarkers(text);
            text = normaliseClientMarkdownLineIndentation(text);

            return text.Trim();
        }

        private static string preprocessMarkdownLine(string line)
        {
            if (line == null)
                return string.Empty;

            line = Regex.Replace(
                line,
                @"<span[^>]*class=""[^""]*flag-country[^""]*""[^>]*background-image:\s*url\(['""]?(?<url>[^'"")]+)['""]?\)[^>]*>\s*</span>",
                flagSpanToOsuWikiFlag,
                RegexOptions.IgnoreCase);

            line = Regex.Replace(
                line,
                @"!\[(?<alt>[^\]]*)\]\(/(?<url>[^)\s]+)(?<title>\s+""[^""]*"")?\)",
                "![${alt}](https://osu.ppy.sh/${url}${title})");

            line = Regex.Replace(
                line,
                @"(?<!!)\]\(/(?<url>[^)]+)\)",
                "](https://osu.ppy.sh/${url})");

            line = convertHeadingFlagsToMarkers(line);

            return line.TrimStart();
        }

        private static string normaliseClientMarkdownLineIndentation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] lines = text.Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                {
                    lines[i] = string.Empty;
                    continue;
                }

                string trimmedStart = line.TrimStart();

                if (trimmedStart.StartsWith("|", StringComparison.Ordinal)
                    || trimmedStart.StartsWith("-", StringComparison.Ordinal)
                    || trimmedStart.StartsWith("*", StringComparison.Ordinal)
                    || trimmedStart.StartsWith(">", StringComparison.Ordinal)
                    || trimmedStart.StartsWith("```", StringComparison.Ordinal)
                    || Regex.IsMatch(trimmedStart, @"^\d+\."))
                {
                    lines[i] = trimmedStart;
                    continue;
                }

                lines[i] = trimmedStart;
            }

            return string.Join("\n", lines);
        }

        private static string flagSpanToOsuWikiFlag(Match match)
        {
            string url = match.Groups["url"].Value;

            var codepointMatch = Regex.Match(
                url,
                @"(?<a>[0-9a-fA-F]{5})-(?<b>[0-9a-fA-F]{5})\.svg");

            if (!codepointMatch.Success)
                return string.Empty;

            int firstCodepoint = Convert.ToInt32(codepointMatch.Groups["a"].Value, 16);
            int secondCodepoint = Convert.ToInt32(codepointMatch.Groups["b"].Value, 16);

            char first = (char)('A' + firstCodepoint - 0x1F1E6);
            char second = (char)('A' + secondCodepoint - 0x1F1E6);

            if (first < 'A' || first > 'Z' || second < 'A' || second > 'Z')
                return string.Empty;

            return $"::{{ flag={first}{second} }}:: ";
        }

        private static NewsFrontMatter parseFrontMatter(string content)
        {
            var frontMatter = new NewsFrontMatter();

            if (string.IsNullOrWhiteSpace(content))
                return frontMatter;

            string normalised = content.Replace("\r\n", "\n").TrimStart();

            if (!normalised.StartsWith("---", StringComparison.Ordinal))
                return frontMatter;

            int firstEnd = normalised.IndexOf('\n');

            if (firstEnd < 0)
                return frontMatter;

            int secondFence = normalised.IndexOf("\n---", firstEnd, StringComparison.Ordinal);

            if (secondFence < 0)
                return frontMatter;

            string header = normalised.Substring(firstEnd + 1, secondFence - firstEnd - 1);

            foreach (string rawLine in header.Split('\n'))
            {
                string line = rawLine.Trim();

                int colon = line.IndexOf(':');

                if (colon < 0)
                    continue;

                string key = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim().Trim('"');

                switch (key)
                {
                    case "title":
                        frontMatter.Title = value;
                        break;

                    case "date":
                        frontMatter.Date = value;
                        break;

                    case "series":
                        frontMatter.Series = value;
                        break;
                }
            }

            return frontMatter;
        }

        private static string stripNewsFrontMatter(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            string normalised = content.Replace("\r\n", "\n").TrimStart();

            if (!normalised.StartsWith("---", StringComparison.Ordinal))
                return content;

            int firstEnd = normalised.IndexOf('\n');

            if (firstEnd < 0)
                return content;

            int secondFence = normalised.IndexOf("\n---", firstEnd, StringComparison.Ordinal);

            if (secondFence < 0)
                return content;

            int bodyStart = secondFence + "\n---".Length;

            if (bodyStart < normalised.Length && normalised[bodyStart] == '\n')
                bodyStart++;

            return normalised.Substring(bodyStart).TrimStart();
        }

        private static string convertHeadingFlagsToMarkers(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] lines = text.Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (!Regex.IsMatch(line, @"^\s*#{1,6}\s+"))
                    continue;

                lines[i] = Regex.Replace(
                    line,
                    @"::\{\s*flag=(?<flag>[A-Z]{2})\s*\}::\s*",
                    match => $"@@FLAG:{match.Groups["flag"].Value.ToUpperInvariant()}@@ ");
            }

            return string.Join("\n", lines);
        }

        private static string normaliseOsuUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            url = url.Trim();

            if (url.StartsWith("/", StringComparison.Ordinal))
                return "https://osu.ppy.sh" + url;

            return url;
        }

        private static string getWindowsClipboardText()
        {
            const uint unicode_text = 13;

            if (!IsClipboardFormatAvailable(unicode_text))
                return string.Empty;

            if (!OpenClipboard(IntPtr.Zero))
                return string.Empty;

            try
            {
                IntPtr handle = GetClipboardData(unicode_text);

                if (handle == IntPtr.Zero)
                    return string.Empty;

                IntPtr pointer = GlobalLock(handle);

                if (pointer == IntPtr.Zero)
                    return string.Empty;

                try
                {
                    return Marshal.PtrToStringUni(pointer) ?? string.Empty;
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        public class NewsFrontMatter
        {
            public string Title;
            public string Date;
            public string Series;
            public string Preview;
            public string FirstImage;
        }

        private class LineEntry
        {
            public string Text;
            public OsuTextBox TextBox;
        }
    }
}