// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Overlays.BeatmapSet.Buttons;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Admin;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.API.Requests.Responses.Admin;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking;
using osu.Game.Users.Drawables;
using osuTK;

namespace osu.Game.Overlays.Admin
{
    public partial class AdminOverlay
    {
        private readonly OverlayColourProvider adminColourProvider =
            new OverlayColourProvider(OverlayColourScheme.Purple);

        protected override IReadOnlyDependencyContainer CreateChildDependencies(
            IReadOnlyDependencyContainer parent)
        {
            var dependencies =
                new DependencyContainer(base.CreateChildDependencies(parent));

            dependencies.CacheAs(adminColourProvider);

            return dependencies;
        }

        private GridContainer createFiveColumnStatGrid(
            Drawable[] firstRow,
            Drawable[]? secondRow = null)
        {
            bool hasSecondRow = secondRow?.Length > 0;

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = hasSecondRow ? 236 : 112,
                ColumnDimensions = createFiveEqualColumns(),
                RowDimensions = hasSecondRow
                    ? createTwoCardRows()
                    : new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 112),
                    },
                Content = hasSecondRow
                    ? new[]
                    {
                        normaliseStatRow(firstRow),
                        createEmptyGridRow(),
                        normaliseStatRow(secondRow!),
                    }
                    : new[]
                    {
                        normaliseStatRow(firstRow),
                    },
            };
        }

        private static Dimension[] createFiveEqualColumns()
        {
            return new[]
            {
                new Dimension(),
                new Dimension(),
                new Dimension(),
                new Dimension(),
                new Dimension(),
            };
        }

        private static Dimension[] createTwoCardRows()
        {
            return new[]
            {
                new Dimension(GridSizeMode.Absolute, 112),
                new Dimension(GridSizeMode.Absolute, 12),
                new Dimension(GridSizeMode.Absolute, 112),
            };
        }

        private Drawable[] normaliseStatRow(Drawable[] cards)
        {
            var result = new Drawable[5];

            for (int i = 0; i < result.Length; i++)
            {
                if (i < cards.Length)
                {
                    cards[i].RelativeSizeAxes = Axes.Both;
                    cards[i].Width = 1;
                    cards[i].Height = 1;

                    result[i] = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Right = i < 4 ? 12 : 0,
                        },
                        Child = cards[i],
                    };
                }
                else
                {
                    result[i] = Empty();
                }
            }

            return result;
        }

        private Drawable[] createEmptyGridRow()
        {
            return new Drawable[]
            {
                Empty(),
                Empty(),
                Empty(),
                Empty(),
                Empty(),
            };
        }

        private Drawable createNavigationCell(
            AdminNavigationCard card,
            bool hasRightSpacing)
        {
            card.RelativeSizeAxes = Axes.Both;
            card.Width = 1;
            card.Height = 1;

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Right = hasRightSpacing ? 12 : 0,
                },
                Child = card,
            };
        }

        private Drawable createEmptyState(
            string title,
            string description,
            Colour4 accentColour)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 120,
                Masking = true,
                CornerRadius = 12,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(18),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 5,
                        Colour = accentColour,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 4),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = title,
                                Font = OsuFont.GetFont(
                                    size: 18,
                                    weight: FontWeight.Bold),
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = description,
                                Font = OsuFont.GetFont(size: 12),
                                Colour = colours.GrayB,
                            },
                        },
                    },
                },
            };
        }

        private Drawable createInformationRow(
            string title,
            string description,
            string value,
            Colour4 accentColour)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 72,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(16),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 4,
                        Colour = accentColour,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Left = 18,
                            Right = 18,
                            Vertical = 10,
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute, 220),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Spacing = new Vector2(0, 2),
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Text = title,
                                            Font = OsuFont.GetFont(
                                                size: 15,
                                                weight: FontWeight.Bold),
                                        },
                                        new OsuSpriteText
                                        {
                                            Text = description,
                                            Font = OsuFont.GetFont(size: 11),
                                            Colour = colours.GrayB,
                                        },
                                    },
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Text = value,
                                    Font = OsuFont.GetFont(
                                        size: 14,
                                        weight: FontWeight.Bold),
                                    Colour = accentColour,
                                },
                            },
                        },
                    },
                },
            };
        }

        private partial class AdminBeatmapCover : CompositeDrawable
        {
            private readonly APIBeatmapSet beatmapSet;
            private readonly Container coverContainer;

            public AdminBeatmapCover(APIBeatmapSet beatmapSet)
            {
                this.beatmapSet = beatmapSet;

                RelativeSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 8;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(25),
                    },

                    coverContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                LoadComponentAsync(
                    new OnlineBeatmapSetCover(
                        beatmapSet,
                        BeatmapSetCoverType.Card)
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    coverContainer.Add);
            }
        }

        private partial class AdminPreviewButton : CompositeDrawable
        {
            private readonly APIBeatmapSet beatmapSet;

            public AdminPreviewButton(APIBeatmapSet beatmapSet)
            {
                this.beatmapSet = beatmapSet;

                Width = 38;
                Height = 30;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChild = new PreviewButton
                {
                    RelativeSizeAxes = Axes.Both,
                    BeatmapSet = beatmapSet,
                };
            }
        }

        private partial class AdminBeatmapSetRow : CompositeDrawable
        {
            private readonly APIAdminBeatmap[] beatmaps;
            private readonly APIAdminBeatmap representative;

            [Resolved]
            private OsuGame game { get; set; } = null!;

            public AdminBeatmapSetRow(APIAdminBeatmap[] beatmaps)
            {
                this.beatmaps = beatmaps
                    .OrderBy(beatmap => beatmap.DifficultyRating)
                    .ThenBy(beatmap => beatmap.Id)
                    .ToArray();

                representative = this.beatmaps[0];

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                CornerRadius = 12;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Colour4 statusColour = getStatusColour(representative.Status, colours);

                double highestStarRating = beatmaps.Max(beatmap => beatmap.DifficultyRating);
                Colour4 highestDifficultyColour = colours.ForStarDifficulty(highestStarRating);

                BorderThickness = 2;
                BorderColour = highestDifficultyColour;

                Drawable thumbnail = hasUsableBeatmapSet(representative.BeatmapSet)
                    ? new AdminBeatmapCover(representative.BeatmapSet)
                    : new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(25),
                    };

                var difficultyFlow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 4),
                };

                foreach (APIAdminBeatmap beatmap in beatmaps)
                    difficultyFlow.Add(new AdminBeatmapDifficultyRow(beatmap));

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
                        Colour = highestDifficultyColour,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding
                        {
                            Left = 16,
                            Right = 16,
                            Vertical = 12,
                        },
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 96,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.Absolute, 190),
                                    new Dimension(),
                                    new Dimension(GridSizeMode.Absolute, 180),
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding { Right = 14 },
                                            Child = thumbnail,
                                        },
                                        createSetDetails(statusColour, colours),
                                        createSetActions(colours),
                                    },
                                },
                            },
                            difficultyFlow,
                        },
                    },
                };
            }

            private Drawable createSetDetails(Colour4 statusColour, OsuColour colours)
            {
                DateTimeOffset? latestUpdate = beatmaps
                    .Where(beatmap => beatmap.LastUpdated.HasValue)
                    .Select(beatmap => beatmap.LastUpdated)
                    .Max();

                string updated = latestUpdate.HasValue
                    ? latestUpdate.Value.ToLocalTime().ToString("dd MMM yyyy, HH:mm")
                    : "unknown";

                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Spacing = new Vector2(0, 3),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = $"{representative.Artist} — {representative.Title}",
                            Font = OsuFont.GetFont(size: 17, weight: FontWeight.Bold),
                        },
                        new OsuSpriteText
                        {
                            Text = $"mapped by {representative.Creator} · {beatmaps.Length:N0} difficulties",
                            Font = OsuFont.GetFont(size: 11),
                            Colour = colours.GrayB,
                        },
                        new OsuSpriteText
                        {
                            Text = $"set {representative.BeatmapsetId} · updated {updated}",
                            Font = OsuFont.GetFont(size: 10),
                            Colour = OsuColour.Gray(145),
                        },
                        new BeatmapSetOnlineStatusPill
                        {
                            Status = getBeatmapOnlineStatus(representative.Status),
                            Animated = false,
                            TextSize = OsuFont.Style.Caption2.Size,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Margin = new MarginPadding { Top = 3 },
                        },
                    },
                };
            }

            private static BeatmapOnlineStatus getBeatmapOnlineStatus(string status)
            {
                switch (status.ToLowerInvariant())
                {
                    case "ranked":
                    case "approved":
                        return BeatmapOnlineStatus.Ranked;

                    case "qualified":
                        return BeatmapOnlineStatus.Qualified;

                    case "loved":
                        return BeatmapOnlineStatus.Loved;

                    case "pending":
                        return BeatmapOnlineStatus.Pending;

                    case "wip":
                        return BeatmapOnlineStatus.WIP;

                    case "graveyard":
                        return BeatmapOnlineStatus.Graveyard;

                    default:
                        return BeatmapOnlineStatus.None;
                }
            }

            private Drawable createSetActions(OsuColour colours)
            {
                var actions = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Spacing = new Vector2(0, 7),
                };

                var buttons = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5),
                };

                if (hasUsableBeatmapSet(representative.BeatmapSet))
                    buttons.Add(new AdminPreviewButton(representative.BeatmapSet));

                buttons.Add(new RoundedButton
                {
                    Width = 82,
                    Height = 30,
                    Text = "open set",
                    BackgroundColour = colours.Purple3,
                    Action = () => game.ShowBeatmap(representative.Id),
                });

                actions.Add(buttons);
                return actions;
            }

            private partial class AdminBeatmapDifficultyRow : CompositeDrawable
            {
                private readonly APIAdminBeatmap beatmap;

                [Resolved]
                private OsuGame game { get; set; } = null!;

                public AdminBeatmapDifficultyRow(APIAdminBeatmap beatmap)
                {
                    this.beatmap = beatmap;

                    RelativeSizeAxes = Axes.X;
                    Height = 46;
                    Masking = true;
                    CornerRadius = 8;
                }

                [BackgroundDependencyLoader]
                private void load(OsuColour colours)
                {
                    InternalChildren = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = OsuColour.Gray(21),
                        },
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding
                            {
                                Left = 14,
                                Right = 10,
                                Vertical = 7,
                            },
                            ColumnDimensions = new[]
                            {
                                new Dimension(),
                                new Dimension(GridSizeMode.Absolute, 105),
                                new Dimension(GridSizeMode.Absolute, 165),
                                new Dimension(GridSizeMode.Absolute, 76),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Direction = FillDirection.Vertical,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Spacing = new Vector2(0, 1),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Text = beatmap.Version,
                                                Font = OsuFont.GetFont(size: 13, weight: FontWeight.Bold),
                                            },
                                            new OsuSpriteText
                                            {
                                                Text = $"map {beatmap.Id}",
                                                Font = OsuFont.GetFont(size: 9),
                                                Colour = colours.Gray9,
                                            },
                                        },
                                    },
                                    new StarRatingDisplay(
                                        new StarDifficulty(beatmap.DifficultyRating, 0),
                                        StarRatingDisplaySize.Small)
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                    },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = $"AR {beatmap.ApproachRate:0.#} · OD {beatmap.OverallDifficulty:0.#} · CS {beatmap.CircleSize:0.#}",
                                        Font = OsuFont.GetFont(size: 10),
                                        Colour = colours.GrayB,
                                    },
                                    new RoundedButton
                                    {
                                        Width = 66,
                                        Height = 30,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = "open",
                                        BackgroundColour = colours.Blue3,
                                        Action = () => game.ShowBeatmap(beatmap.Id),
                                    },
                                },
                            },
                        },
                    };
                }
            }

            private static Colour4 getStatusColour(string status, OsuColour colours)
            {
                switch (status.ToLowerInvariant())
                {
                    case "ranked":
                    case "approved":
                        return colours.Blue3;

                    case "loved":
                        return colours.Pink3;

                    case "qualified":
                        return colours.Purple3;

                    case "pending":
                    case "wip":
                        return colours.Orange3;

                    case "graveyard":
                        return colours.Gray5;

                    default:
                        return colours.GrayB;
                }
            }
        }

        private partial class AdminScoreRow : CompositeDrawable
        {
            private readonly APIAdminScore score;

            [Resolved]
            private OsuGame game { get; set; } = null!;

            public AdminScoreRow(APIAdminScore score)
            {
                this.score = score;

                RelativeSizeAxes = Axes.X;
                Height = 126;
                Masking = true;
                CornerRadius = 12;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                string mods = score.Mods.Count > 0
                    ? string.Join(" ", score.Mods.Select(mod => $"+{mod}"))
                    : "NM";

                string playedAt = score.CreatedAt.HasValue
                    ? score.CreatedAt.Value.ToLocalTime().ToString("dd MMM yyyy, HH:mm")
                    : "unknown date";

                long displayedScore = score.ClassicTotalScore > 0
                    ? score.ClassicTotalScore
                    : score.TotalScore;

                Colour4 originColour = score.Origin.Equals("stable", StringComparison.OrdinalIgnoreCase)
                    ? colours.Purple3
                    : colours.Pink3;

                Drawable thumbnail = hasUsableBeatmapSet(score.BeatmapSet)
                    ? new AdminBeatmapCover(score.BeatmapSet)
                    : new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(25),
                    };

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
                        Colour = originColour,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Left = 16,
                            Right = 16,
                            Vertical = 12,
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 190),
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute, 165),
                            new Dimension(GridSizeMode.Absolute, 150),
                            new Dimension(GridSizeMode.Absolute, 200),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Right = 14 },
                                    Child = thumbnail,
                                },
                                createScoreDetails(mods, playedAt, colours),
                                createScoreValueBlock(displayedScore, colours),
                                createScorePerformanceBlock(colours),
                                createScoreActions(originColour, colours),
                            },
                        },
                    },
                };
            }

            private Drawable createScoreDetails(string mods, string playedAt, OsuColour colours)
            {
                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Spacing = new Vector2(0, 3),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = $"{score.BeatmapTitle} [{score.BeatmapVersion}]",
                            Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                        },
                        new OsuSpriteText
                        {
                            Text = $"{score.Username} · map {score.BeatmapId} · {score.DifficultyRating:0.00}★ · {mods}",
                            Font = OsuFont.GetFont(size: 11),
                            Colour = colours.GrayB,
                        },
                        new OsuSpriteText
                        {
                            Text = $"score #{score.Id} · {playedAt}",
                            Font = OsuFont.GetFont(size: 10),
                            Colour = OsuColour.Gray(145),
                        },
                    },
                };
            }

            private Drawable createScoreValueBlock(long value, OsuColour colours)
            {
                return new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = value.ToString("N0"),
                            Font = OsuFont.GetFont(size: 17, weight: FontWeight.Bold),
                        },
                        new OsuSpriteText
                        {
                            Text = score.Origin.Equals("stable", StringComparison.OrdinalIgnoreCase)
                                ? "classic score"
                                : "lazer score",
                            Font = OsuFont.GetFont(size: 9),
                            Colour = colours.GrayB,
                        },
                    },
                };
            }

            private Drawable createScorePerformanceBlock(OsuColour colours)
            {
                return new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = $"{score.Accuracy * 100:0.00}% · {score.PP.GetValueOrDefault():0.##}pp",
                            Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold),
                        },
                        new OsuSpriteText
                        {
                            Text = $"{score.MaxCombo:N0}x combo · rank {score.Rank}",
                            Font = OsuFont.GetFont(size: 10),
                            Colour = colours.GrayB,
                        },
                    },
                };
            }

            private Drawable createScoreActions(Colour4 originColour, OsuColour colours)
            {
                var actions = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Spacing = new Vector2(0, 6),
                };

                actions.Add(new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Text = score.Origin.ToUpperInvariant(),
                    Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                    Colour = originColour,
                });

                var buttons = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5),
                };

                if (hasUsableBeatmapSet(score.BeatmapSet))
                    buttons.Add(new AdminPreviewButton(score.BeatmapSet));

                if (score.HasReplay)
                {
                    buttons.Add(new ReplayDownloadButton(score.CreateScoreInfo())
                    {
                        Width = 50,
                        Height = 30,
                    });
                }

                buttons.Add(new RoundedButton
                {
                    Width = 56,
                    Height = 30,
                    Text = "map",
                    BackgroundColour = colours.Purple3,
                    Action = () => game.ShowBeatmap(score.BeatmapId),
                });

                actions.Add(buttons);
                return actions;
            }

        }

        private partial class AdminAuditLogRow : CompositeDrawable
        {
            private readonly APIAdminAuditLogEntry entry;

            public AdminAuditLogRow(APIAdminAuditLogEntry entry)
            {
                this.entry = entry;

                RelativeSizeAxes = Axes.X;
                Height = 112;
                Masking = true;
                CornerRadius = 10;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Colour4 accent = getAuditAccent(entry.TargetType, colours);

                string createdAt = entry.CreatedAt.HasValue
                    ? entry.CreatedAt.Value
                        .ToLocalTime()
                        .ToString("dd MMM yyyy, HH:mm:ss")
                    : "unknown time";

                string target = string.IsNullOrWhiteSpace(entry.TargetType)
                    ? "no target"
                    : entry.TargetId.HasValue
                        ? $"{entry.TargetType} #{entry.TargetId.Value}"
                        : entry.TargetType!;

                string details = string.IsNullOrWhiteSpace(entry.Details)
                    ? "No additional details."
                    : entry.Details!;

                string source = string.IsNullOrWhiteSpace(entry.IpAddress)
                    ? $"event #{entry.Id}"
                    : $"event #{entry.Id} · {entry.IpAddress}";

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
                        Colour = accent,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Left = 18,
                            Right = 18,
                            Vertical = 12,
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute, 250),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 3),
                                    Children = new Drawable[]
                                    {
                                        new FillFlowContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(8, 0),
                                            Children = new Drawable[]
                                            {
                                                new OsuSpriteText
                                                {
                                                    Text = formatAuditAction(
                                                        entry.Action),
                                                    Font = OsuFont.GetFont(
                                                        size: 14,
                                                        weight: FontWeight.Bold),
                                                    Colour = accent,
                                                },
                                                new OsuSpriteText
                                                {
                                                    Text = target,
                                                    Font = OsuFont.GetFont(
                                                        size: 11,
                                                        weight: FontWeight.Bold),
                                                    Colour = colours.GrayB,
                                                },
                                            },
                                        },
                                        new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = details,
                                            Font = OsuFont.GetFont(size: 12),
                                            Colour = colours.GrayD,
                                        },
                                        new OsuSpriteText
                                        {
                                            Text = source,
                                            Font = OsuFont.GetFont(size: 9),
                                            Colour = colours.Gray9,
                                        },
                                    },
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Spacing = new Vector2(0, 3),
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Text = entry.AdminName,
                                            Font = OsuFont.GetFont(
                                                size: 13,
                                                weight: FontWeight.Bold),
                                        },
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Text = entry.AdminId.HasValue
                                                ? $"admin #{entry.AdminId.Value}"
                                                : "system",
                                            Font = OsuFont.GetFont(size: 10),
                                            Colour = colours.GrayB,
                                        },
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Text = createdAt,
                                            Font = OsuFont.GetFont(size: 10),
                                            Colour = colours.Gray9,
                                        },
                                    },
                                },
                            },
                        },
                    },
                };
            }

            private static string formatAuditAction(string action)
            {
                if (string.IsNullOrWhiteSpace(action))
                    return "UNKNOWN ACTION";

                return action
                    .Replace('_', ' ')
                    .ToUpperInvariant();
            }

            private static Colour4 getAuditAccent(
                string? targetType,
                OsuColour colours)
            {
                switch (targetType?.ToLowerInvariant())
                {
                    case "user":
                        return colours.Blue3;

                    case "score":
                        return colours.Pink3;

                    case "beatmap":
                        return colours.Purple3;

                    case "report":
                        return colours.Orange3;

                    case "system":
                        return colours.Green3;

                    default:
                        return colours.Gray5;
                }
            }
        }

        private static bool hasUsableBeatmapSet(APIBeatmapSet? beatmapSet)
        {
            return beatmapSet != null && beatmapSet.OnlineID > 0;
        }

        private partial class AdminStatCard : CompositeDrawable
        {
            public AdminStatCard(
                string label,
                string value,
                string description,
                Colour4 accentColour)
            {
                RelativeSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 12;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(18),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 5,
                        Colour = accentColour,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Left = 18,
                            Right = 12,
                            Vertical = 13,
                        },
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 2),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = label.ToUpperInvariant(),
                                Font = OsuFont.GetFont(
                                    size: 11,
                                    weight: FontWeight.Bold),
                                Colour = accentColour,
                            },
                            new OsuSpriteText
                            {
                                Text = value,
                                Font = OsuFont.GetFont(
                                    size: 28,
                                    weight: FontWeight.Bold),
                            },
                            new OsuSpriteText
                            {
                                Text = description,
                                Font = OsuFont.GetFont(size: 11),
                                Colour = OsuColour.Gray(160),
                            },
                        },
                    },
                };
            }
        }

        private partial class AdminNavigationCard : OsuClickableContainer
        {
            private Box background = null!;
            private readonly bool available;

            public AdminNavigationCard(
                string title,
                string description,
                Colour4 accentColour,
                Action? action = null)
            {
                RelativeSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 12;

                available = action != null;
                Action = action ?? (() => { });
                Alpha = available ? 1 : 0.55f;

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(18),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 4,
                        Colour = accentColour,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(16),
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = title,
                                Font = OsuFont.GetFont(
                                    size: 18,
                                    weight: FontWeight.Bold),
                            },
                            new OsuSpriteText
                            {
                                Text = description,
                                Font = OsuFont.GetFont(size: 12),
                                Colour = OsuColour.Gray(170),
                                RelativeSizeAxes = Axes.X,
                            },
                        },
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (available)
                    background.FadeColour(OsuColour.Gray(30), 120);

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (available)
                    background.FadeColour(OsuColour.Gray(18), 120);

                base.OnHoverLost(e);
            }
        }

        private partial class AdminUserRow : CompositeDrawable
        {
            private readonly APIAdminUser user;
            private readonly Action refresh;
            private readonly APIUser profileUser;

            [Resolved]
            private IAPIProvider api { get; set; } = null!;

            [Resolved]
            private OsuGame game { get; set; } = null!;

            private Box background = null!;
            private OsuSpriteText stateText = null!;
            private RoundedButton restrictionButton = null!;

            public AdminUserRow(APIAdminUser user, Action refresh)
            {
                this.user = user;
                this.refresh = refresh;

                profileUser = new APIUser
                {
                    Id = user.Id,
                    Username = user.Username,
                    AvatarUrl = user.AvatarUrl,
                };

                RelativeSizeAxes = Axes.X;
                Height = 82;
                Masking = true;
                CornerRadius = 10;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                string lastSeen = user.LastVisit.HasValue
                    ? user.LastVisit.Value.ToLocalTime().ToString("dd MMM yyyy, HH:mm")
                    : "never";

                string statistics =
                    $"{user.PP:N0}pp  ·  " +
                    $"{user.Accuracy:0.00}%  ·  " +
                    $"{user.PlayCount:N0} plays";

                var avatarContainer = new Container
                {
                    Size = new Vector2(56),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Masking = true,
                    CornerRadius = 8,
                };

                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(16),
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Horizontal = 14,
                            Vertical = 10,
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 68),
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute, 150),
                            new Dimension(GridSizeMode.Absolute, 105),
                            new Dimension(GridSizeMode.Absolute, 140),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                avatarContainer,
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Spacing = new Vector2(0, 1),
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Text = user.Username,
                                            Font = OsuFont.GetFont(
                                                size: 17,
                                                weight: FontWeight.Bold),
                                        },
                                        new OsuSpriteText
                                        {
                                            Text =
                                                $"ID {user.Id} · " +
                                                $"{user.CountryCode} · " +
                                                statistics,
                                            Font = OsuFont.GetFont(size: 11),
                                            Colour = colours.GrayB,
                                        },
                                        new OsuSpriteText
                                        {
                                            Text = $"Last seen {lastSeen}",
                                            Font = OsuFont.GetFont(size: 10),
                                            Colour = OsuColour.Gray(145),
                                        },
                                    },
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 2),
                                        Children = new Drawable[]
                                        {
                                            stateText = new OsuSpriteText
                                            {
                                                Text = getStateText(user),
                                                Font = OsuFont.GetFont(
                                                    size: 11,
                                                    weight: FontWeight.Bold),
                                                Colour = getStateColour(user, colours),
                                            },
                                            new OsuSpriteText
                                            {
                                                Text = "account status",
                                                Font = OsuFont.GetFont(size: 9),
                                                Colour = colours.Gray9,
                                            },
                                        },
                                    },
                                },
                                new RoundedButton
                                {
                                    Width = 92,
                                    Height = 38,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "view",
                                    BackgroundColour = colours.Blue3,
                                    Action = openProfile,
                                },
                                restrictionButton = new RoundedButton
                                {
                                    Width = 126,
                                    Height = 38,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = user.IsRestricted ? "unrestrict" : "restrict",
                                    BackgroundColour = user.IsRestricted
                                        ? colours.Green3
                                        : colours.Red3,
                                    Action = toggleRestriction,
                                },
                            },
                        },
                    },
                };

                LoadComponentAsync(
                    new DrawableAvatar(profileUser)
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                    },
                    avatarContainer.Add);
            }

            private static string getStateText(APIAdminUser user)
            {
                if (user.IsRestricted)
                    return "RESTRICTED";

                if (user.IsSilenced)
                    return "SILENCED";

                return "ACTIVE";
            }

            private static Colour4 getStateColour(
                APIAdminUser user,
                OsuColour colours)
            {
                if (user.IsRestricted)
                    return colours.Red3;

                if (user.IsSilenced)
                    return colours.Orange3;

                return colours.Green3;
            }

            private void openProfile()
            {
                game.ShowUser(profileUser);
            }

            private void toggleRestriction()
            {
                restrictionButton.Enabled.Value = false;

                APIRequest request;

                if (user.IsRestricted)
                {
                    stateText.Text = "UNRESTRICTING...";
                    request = new UnrestrictAdminUserRequest(user.Id);
                }
                else
                {
                    stateText.Text = "RESTRICTING...";
                    request = new RestrictAdminUserRequest(
                        user.Id,
                        "Restricted through osu!lazer admin overlay");
                }

                request.Success += () => refresh();

                request.Failure += error =>
                {
                    restrictionButton.Enabled.Value = true;
                    stateText.Text = $"FAILED: {error.Message}";
                };

                _ = api.PerformAsync(request);
            }
        }
    }
}