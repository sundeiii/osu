// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses.Admin;
using osuTK.Graphics;

namespace osu.Game.Overlays.Admin
{
    public partial class AdminOverlay
        : TabbableOnlineOverlay<AdminOverlayHeader, AdminOverlayTab>
    {
        private IAPIProvider api;
        private OsuColour colours;

        private Container statsContainer;
        private FillFlowContainer usersFlow;
        private FillFlowContainer reportsFlow;
        private FillFlowContainer scoresFlow;
        private FillFlowContainer beatmapsFlow;
        private FillFlowContainer newsFlow;
        private FillFlowContainer auditLogFlow;

        private OsuSpriteText reportsStatusText;
        private OsuSpriteText scoresStatusText;
        private OsuSpriteText systemStatusText;
        private OsuSpriteText beatmapsStatusText;
        private OsuSpriteText newsStatusText;
        private OsuSpriteText auditLogStatusText;
        private OsuSpriteText usersStatusText;

        private OsuTextBox searchBox;
        private OsuTextBox reportsSearchBox;
        private OsuTextBox scoresSearchBox;
        private OsuTextBox beatmapsSearchBox;
        private OsuTextBox auditLogSearchBox;

        private OsuTextBox newsTitleBox;
        private OsuTextBox newsSlugBox;
        private OsuTextBox newsAuthorBox;
        private OsuTextBox newsPreviewBox;
        private OsuTextBox newsImageBox;
        private OsuTextBox newsPublishedAtBox;
        private OsuTextBox newsContentBox;

        private Container systemSummaryContainer;
        private FillFlowContainer systemServicesFlow;

        private int userSearchGeneration;
        private int scoreSearchGeneration;
        private int beatmapSearchGeneration;
        private int auditLogSearchGeneration;

        private int beatmapPage = 1;
        private int beatmapPageCount = 1;

        private RoundedButton beatmapsPreviousPageButton;
        private RoundedButton beatmapsNextPageButton;
        private OsuSpriteText beatmapsPageText;

        private string beatmapStatusFilter = "all";

        private RoundedButton beatmapStatusAllButton;
        private RoundedButton beatmapStatusRankedButton;
        private RoundedButton beatmapStatusQualifiedButton;
        private RoundedButton beatmapStatusLovedButton;
        private RoundedButton beatmapStatusPendingButton;
        private RoundedButton beatmapStatusWipButton;
        private RoundedButton beatmapStatusGraveyardButton;

        private int auditLogPage = 1;
        private int auditLogPageCount = 1;
        private string auditLogTargetTypeFilter = "all";

        private RoundedButton auditLogAllButton;
        private RoundedButton auditLogUsersButton;
        private RoundedButton auditLogScoresButton;
        private RoundedButton auditLogBeatmapsButton;
        private RoundedButton auditLogReportsButton;
        private RoundedButton auditLogSystemButton;

        private RoundedButton auditLogPreviousPageButton;
        private RoundedButton auditLogNextPageButton;
        private OsuSpriteText auditLogPageText;

        private AdminOverlayTab currentPage;

        private APIAdminNewsPost selectedNewsPost;

        public AdminOverlay()
            : base(OverlayColourScheme.Purple)
        {
        }

        protected override Color4 BackgroundColour => ColourProvider.Background6;

        [BackgroundDependencyLoader]
        private void load(IAPIProvider api, OsuColour colours)
        {
            this.api = api;
            this.colours = colours;
        }

        protected override AdminOverlayHeader CreateHeader()
            => new AdminOverlayHeader();

        protected override void CreateDisplayToLoad(AdminOverlayTab tab)
        {
            currentPage = tab;

            Drawable display;

            switch (tab)
            {
                case AdminOverlayTab.Overview:
                    display = createOverviewPage();
                    break;

                case AdminOverlayTab.Users:
                    display = createUsersPage();
                    searchBox.OnCommit += (_, _) => searchUsers();
                    break;

                case AdminOverlayTab.Scores:
                    display = createScoresPage();
                    scoresSearchBox.OnCommit += (_, _) => loadScores();
                    break;

                case AdminOverlayTab.Beatmaps:
                    display = createBeatmapsPage();
                    beatmapsSearchBox.OnCommit += (_, _) => searchBeatmaps();
                    break;

                case AdminOverlayTab.Reports:
                    display = createReportsPage();
                    reportsSearchBox.OnCommit += (_, _) => loadReports();
                    break;

                case AdminOverlayTab.News:
                    display = createNewsPage();
                    break;

                case AdminOverlayTab.System:
                    display = createSystemPage();
                    break;

                case AdminOverlayTab.AuditLog:
                    display = createAuditLogPage();
                    auditLogSearchBox.OnCommit += (_, _) => searchAuditLog();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tab), tab, null);
            }

            LoadDisplay(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding
                {
                    Horizontal = 56,
                    Vertical = 28,
                },
                Child = display,
            });

            Scheduler.AddOnce(refreshAll);
        }

        private void showPage(AdminOverlayTab page)
        {
            Header.Current.Value = page;
        }

        private void setBeatmapStatusFilter(string status)
        {
            if (beatmapStatusFilter == status)
                return;

            beatmapStatusFilter = status;
            beatmapPage = 1;

            updateBeatmapStatusFilterColours();
            loadBeatmaps();
        }

        private void updateBeatmapStatusFilterColours()
        {
            if (beatmapStatusAllButton == null)
                return;

            beatmapStatusAllButton.BackgroundColour =
                beatmapStatusFilter == "all" ? colours.Purple2 : colours.Gray5;

            beatmapStatusRankedButton.BackgroundColour =
                beatmapStatusFilter == "ranked" ? colours.Blue2 : colours.Gray5;

            beatmapStatusQualifiedButton.BackgroundColour =
                beatmapStatusFilter == "qualified" ? colours.Purple2 : colours.Gray5;

            beatmapStatusLovedButton.BackgroundColour =
                beatmapStatusFilter == "loved" ? colours.Pink2 : colours.Gray5;

            beatmapStatusPendingButton.BackgroundColour =
                beatmapStatusFilter == "pending" ? colours.Orange2 : colours.Gray5;

            beatmapStatusWipButton.BackgroundColour =
                beatmapStatusFilter == "wip" ? colours.Orange2 : colours.Gray5;

            beatmapStatusGraveyardButton.BackgroundColour =
                beatmapStatusFilter == "graveyard" ? colours.Gray4 : colours.Gray5;
        }

        private void searchBeatmaps()
        {
            beatmapPage = 1;
            loadBeatmaps();
        }

        private void changeBeatmapPage(int delta)
        {
            int target = Math.Clamp(
                beatmapPage + delta,
                1,
                beatmapPageCount);

            if (target == beatmapPage)
                return;

            beatmapPage = target;
            loadBeatmaps();
        }

        private void updateBeatmapPagination(int total, int pageSize)
        {
            beatmapPageCount = Math.Max(
                1,
                (int)Math.Ceiling(total / (double)pageSize));

            beatmapPage = Math.Clamp(
                beatmapPage,
                1,
                beatmapPageCount);

            if (beatmapsPageText != null)
                beatmapsPageText.Text =
                    $"page {beatmapPage:N0} / {beatmapPageCount:N0}";

            if (beatmapsPreviousPageButton != null)
                beatmapsPreviousPageButton.Enabled.Value =
                    beatmapPage > 1;

            if (beatmapsNextPageButton != null)
                beatmapsNextPageButton.Enabled.Value =
                    beatmapPage < beatmapPageCount;
        }

        private void setAuditLogTargetTypeFilter(string targetType)
        {
            if (auditLogTargetTypeFilter == targetType)
                return;

            auditLogTargetTypeFilter = targetType;
            auditLogPage = 1;

            updateAuditLogFilterColours();
            loadAuditLog();
        }

        private void updateAuditLogFilterColours()
        {
            if (auditLogAllButton == null)
                return;

            auditLogAllButton.BackgroundColour =
                auditLogTargetTypeFilter == "all"
                    ? colours.Purple2
                    : colours.Gray5;

            auditLogUsersButton.BackgroundColour =
                auditLogTargetTypeFilter == "user"
                    ? colours.Blue2
                    : colours.Gray5;

            auditLogScoresButton.BackgroundColour =
                auditLogTargetTypeFilter == "score"
                    ? colours.Pink2
                    : colours.Gray5;

            auditLogBeatmapsButton.BackgroundColour =
                auditLogTargetTypeFilter == "beatmap"
                    ? colours.Purple2
                    : colours.Gray5;

            auditLogReportsButton.BackgroundColour =
                auditLogTargetTypeFilter == "report"
                    ? colours.Orange2
                    : colours.Gray5;

            auditLogSystemButton.BackgroundColour =
                auditLogTargetTypeFilter == "system"
                    ? colours.Green2
                    : colours.Gray5;
        }

        private void searchAuditLog()
        {
            auditLogPage = 1;
            loadAuditLog();
        }

        private void changeAuditLogPage(int delta)
        {
            int target = Math.Clamp(
                auditLogPage + delta,
                1,
                auditLogPageCount);

            if (target == auditLogPage)
                return;

            auditLogPage = target;
            loadAuditLog();
        }

        private void updateAuditLogPagination(int total, int pageSize)
        {
            auditLogPageCount = Math.Max(
                1,
                (int)Math.Ceiling(total / (double)pageSize));

            auditLogPage = Math.Clamp(
                auditLogPage,
                1,
                auditLogPageCount);

            if (auditLogPageText != null)
            {
                auditLogPageText.Text =
                    $"page {auditLogPage:N0} / {auditLogPageCount:N0}";
            }

            if (auditLogPreviousPageButton != null)
            {
                auditLogPreviousPageButton.Enabled.Value =
                    auditLogPage > 1;
            }

            if (auditLogNextPageButton != null)
            {
                auditLogNextPageButton.Enabled.Value =
                    auditLogPage < auditLogPageCount;
            }
        }

        private void refreshAll()
        {
            switch (currentPage)
            {
                case AdminOverlayTab.Overview:
                    loadStats();
                    break;

                case AdminOverlayTab.Users:
                    searchUsers();
                    break;

                case AdminOverlayTab.Scores:
                    loadScores();
                    break;

                case AdminOverlayTab.Beatmaps:
                    loadBeatmaps();
                    break;

                case AdminOverlayTab.Reports:
                    loadReports();
                    break;
                
                case AdminOverlayTab.News:
                    loadNews();
                    break;
                
                case AdminOverlayTab.System:
                    loadSystemStatus();
                    break;

                case AdminOverlayTab.AuditLog:
                    loadAuditLog();
                    break;
            }
        }
    }
}
