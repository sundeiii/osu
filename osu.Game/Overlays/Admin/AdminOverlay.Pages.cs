// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osuTK;

namespace osu.Game.Overlays.Admin
{
    public partial class AdminOverlay
    {
        private FillFlowContainer createOverviewPage()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 22),
                Children = new Drawable[]
                {
                    createSectionHeading(
                        "overview",
                        "Live platform statistics and moderation status."),

                    statsContainer = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 236,
                    },

                    createSectionHeading(
                        "platform",
                        "Use the tabs above to manage users, scores, beatmaps, reports and infrastructure."),
                },
            };
        }

        private FillFlowContainer createUsersPage()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 18),
                Children = new Drawable[]
                {
                    createSectionHeading(
                        "users",
                        "Search and manage accounts across stable and lazer."),

                    createSearchToolbar(
                        searchBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 44,
                            PlaceholderText = "search username or user ID",
                        },
                        "search",
                        searchUsers,
                        "clear",
                        clearSearch),

                    usersStatusText = createStatusText("users have not been loaded"),

                    usersFlow = createVerticalResultsFlow(),
                },
            };
        }

        private FillFlowContainer createReportsPage()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 18),
                Children = new Drawable[]
                {
                    createSectionHeading(
                        "reports",
                        "Review reports submitted against players, scores and beatmaps."),

                    createSearchToolbar(
                        reportsSearchBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 44,
                            PlaceholderText = "search report ID, username or target",
                        },
                        "search",
                        loadReports,
                        "clear",
                        () =>
                        {
                            reportsSearchBox.Text = string.Empty;
                            loadReports();
                        }),

                    reportsStatusText = createStatusText("reports have not been loaded"),
                    reportsFlow = createVerticalResultsFlow(),
                },
            };
        }

        private FillFlowContainer createScoresPage()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 18),
                Children = new Drawable[]
                {
                    createSectionHeading(
                        "scores",
                        "Search, inspect, recalculate and invalidate submitted scores."),

                    createSearchToolbar(
                        scoresSearchBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 44,
                            PlaceholderText = "score ID, username, user ID or beatmap ID",
                        },
                        "search",
                        loadScores,
                        "clear",
                        () =>
                        {
                            scoresSearchBox.Text = string.Empty;
                            loadScores();
                        }),

                    scoresStatusText = createStatusText("scores have not been loaded"),
                    scoresFlow = createVerticalResultsFlow(),
                },
            };
        }

        private FillFlowContainer createSystemPage()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 20),
                Children = new Drawable[]
                {
                    createSectionHeading(
                        "system",
                        "Live infrastructure and service health across stable and lazer."),

                    systemStatusText = createStatusText("system status has not been loaded"),

                    createSectionHeading(
                        "platforms",
                        "Shared accounts with stable-origin and lazer-origin score totals."),

                    systemSummaryContainer = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 236,
                    },

                    createSectionHeading(
                        "services",
                        "Application, database, Redis and multiplayer health."),

                    systemServicesFlow = createVerticalResultsFlow(),
                },
            };
        }

        private FillFlowContainer createBeatmapsPage()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 18),
                Children = new Drawable[]
                {
                    createSectionHeading(
                        "beatmaps",
                        "Search beatmaps, manage ranked status and refresh cached metadata."),

                    createSearchToolbar(
                        beatmapsSearchBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 44,
                            PlaceholderText = "beatmap ID, beatmapset ID, title, artist or mapper",
                        },
                        "search",
                        searchBeatmaps,
                        "clear",
                        () =>
                        {
                            beatmapsSearchBox.Text = string.Empty;
                            beatmapPage = 1;
                            loadBeatmaps();
                        }),

                    createBeatmapStatusFilters(),

                    beatmapsStatusText = createStatusText("beatmaps have not been loaded"),

                    createBeatmapPagination(),

                    beatmapsFlow = createVerticalResultsFlow(),
                },
            };
        }

        private Drawable createBeatmapStatusFilters()
        {
            var filters = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(6),
                Children = new Drawable[]
                {
                    beatmapStatusAllButton = createBeatmapStatusFilterButton(
                        "all",
                        "all"),

                    beatmapStatusRankedButton = createBeatmapStatusFilterButton(
                        "ranked",
                        "ranked"),

                    beatmapStatusQualifiedButton = createBeatmapStatusFilterButton(
                        "qualified",
                        "qualified"),

                    beatmapStatusLovedButton = createBeatmapStatusFilterButton(
                        "loved",
                        "loved"),

                    beatmapStatusPendingButton = createBeatmapStatusFilterButton(
                        "pending",
                        "pending"),

                    beatmapStatusWipButton = createBeatmapStatusFilterButton(
                        "wip",
                        "wip"),

                    beatmapStatusGraveyardButton = createBeatmapStatusFilterButton(
                        "graveyard",
                        "graveyard"),
                },
            };

            Scheduler.AddOnce(updateBeatmapStatusFilterColours);

            return filters;
        }

        private RoundedButton createBeatmapStatusFilterButton(
            string text,
            string status)
        {
            return new RoundedButton
            {
                Width = text.Length > 8 ? 112 : 92,
                Height = 34,
                Text = text,
                BackgroundColour = colours.Gray5,
                Action = () => setBeatmapStatusFilter(status),
            };
        }

        private Drawable createBeatmapPagination()
        {
            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 42,
                ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 120),
                    new Dimension(GridSizeMode.Absolute, 170),
                    new Dimension(GridSizeMode.Absolute, 120),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        Empty(),
                        beatmapsPreviousPageButton = new RoundedButton
                        {
                            Width = 104,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "previous",
                            BackgroundColour = colours.Gray5,
                            Action = () => changeBeatmapPage(-1),
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = beatmapsPageText = new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "page 1 / 1",
                                Font = OsuFont.GetFont(
                                    size: 13,
                                    weight: FontWeight.Bold),
                                Colour = colours.GrayB,
                            },
                        },
                        beatmapsNextPageButton = new RoundedButton
                        {
                            Width = 104,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "next",
                            BackgroundColour = colours.Purple3,
                            Action = () => changeBeatmapPage(1),
                        },
                        Empty(),
                    },
                },
            };
        }

        private FillFlowContainer createAuditLogPage()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 18),
                Children = new Drawable[]
                {
                    createSectionHeading(
                        "audit log",
                        "Review administration actions across the combined stable and lazer platform."),

                    createSearchToolbar(
                        auditLogSearchBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 44,
                            PlaceholderText = "administrator, target, action or reason",
                        },
                        "search",
                        searchAuditLog,
                        "clear",
                        () =>
                        {
                            auditLogSearchBox.Text = string.Empty;
                            auditLogPage = 1;
                            loadAuditLog();
                        }),

                    createAuditLogTargetTypeFilters(),

                    auditLogStatusText = createStatusText(
                        "audit events have not been loaded"),

                    createAuditLogPagination(),

                    auditLogFlow = createVerticalResultsFlow(),
                },
            };
        }

        private Drawable createAuditLogTargetTypeFilters()
        {
            var filters = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(6),
                Children = new Drawable[]
                {
                    auditLogAllButton = createAuditLogFilterButton(
                        "all",
                        "all"),

                    auditLogUsersButton = createAuditLogFilterButton(
                        "users",
                        "user"),

                    auditLogScoresButton = createAuditLogFilterButton(
                        "scores",
                        "score"),

                    auditLogBeatmapsButton = createAuditLogFilterButton(
                        "beatmaps",
                        "beatmap"),

                    auditLogReportsButton = createAuditLogFilterButton(
                        "reports",
                        "report"),

                    auditLogSystemButton = createAuditLogFilterButton(
                        "system",
                        "system"),
                },
            };

            Scheduler.AddOnce(updateAuditLogFilterColours);

            return filters;
        }

        private RoundedButton createAuditLogFilterButton(
            string text,
            string targetType)
        {
            return new RoundedButton
            {
                Width = text.Length > 7 ? 108 : 92,
                Height = 34,
                Text = text,
                BackgroundColour = colours.Gray5,
                Action = () => setAuditLogTargetTypeFilter(targetType),
            };
        }

        private Drawable createAuditLogPagination()
        {
            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 42,
                ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 120),
                    new Dimension(GridSizeMode.Absolute, 170),
                    new Dimension(GridSizeMode.Absolute, 120),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        Empty(),
                        auditLogPreviousPageButton = new RoundedButton
                        {
                            Width = 104,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "previous",
                            BackgroundColour = colours.Gray5,
                            Action = () => changeAuditLogPage(-1),
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = auditLogPageText = new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "page 1 / 1",
                                Font = OsuFont.GetFont(
                                    size: 13,
                                    weight: FontWeight.Bold),
                                Colour = colours.GrayB,
                            },
                        },
                        auditLogNextPageButton = new RoundedButton
                        {
                            Width = 104,
                            Height = 36,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "next",
                            BackgroundColour = colours.Purple3,
                            Action = () => changeAuditLogPage(1),
                        },
                        Empty(),
                    },
                },
            };
        }

        private FillFlowContainer createSectionHeading(string title, string description)
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = title,
                        Font = OsuFont.GetFont(
                            size: 23,
                            weight: FontWeight.Bold),
                    },
                    new OsuSpriteText
                    {
                        Text = description,
                        Font = OsuFont.GetFont(size: 13),
                        Colour = colours.GrayB,
                    },
                },
            };
        }

        private OsuSpriteText createStatusText(string text)
        {
            return new OsuSpriteText
            {
                Text = text,
                Font = OsuFont.GetFont(size: 13),
                Colour = colours.GrayB,
            };
        }

        private FillFlowContainer createVerticalResultsFlow()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
            };
        }

        private Container createSearchToolbar(
            OsuTextBox textBox,
            string primaryText,
            Action primaryAction,
            string secondaryText,
            Action secondaryAction)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 58,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(18),
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Horizontal = 12,
                            Vertical = 7,
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute, 130),
                            new Dimension(GridSizeMode.Absolute, 130),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                textBox,
                                new RoundedButton
                                {
                                    Width = 118,
                                    Height = 44,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = primaryText,
                                    Action = primaryAction,
                                },
                                new RoundedButton
                                {
                                    Width = 118,
                                    Height = 44,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = secondaryText,
                                    BackgroundColour = colours.Gray5,
                                    Action = secondaryAction,
                                },
                            },
                        },
                    },
                },
            };
        }

        private void clearSearch()
        {
            searchBox.Text = string.Empty;
            searchUsers();
        }
    }
}
