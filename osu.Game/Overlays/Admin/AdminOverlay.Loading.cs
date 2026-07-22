// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Admin;
using osu.Game.Online.API.Requests.Responses.Admin;

namespace osu.Game.Overlays.Admin
{
    public partial class AdminOverlay
    {
        private void loadReports()
        {
                        reportsFlow.Clear();
            reportsStatusText.Text = "0 open reports";

            reportsFlow.Add(createEmptyState(
                "No reports loaded",
                "Connect this page to /api/admin/reports.",
                colours.Orange3));
        }

        private void loadNews()
        {
            Loading.Show();
            newsStatusText.Text = "loading news posts...";

            var request = new GetAdminNewsRequest();

            request.Success += posts =>
            {
                newsFlow.Clear();

                if (posts.Count == 0)
                {
                    newsFlow.Add(createEmptyState(
                        "No news posts",
                        "Create a new lazer client news post using the editor.",
                        colours.Purple3));
                }
                else
                {
                    foreach (APIAdminNewsPost post in posts)
                        newsFlow.Add(new AdminNewsPostRow(
                            post,
                            selectNewsPost,
                            deleteNewsPost));
                }

                newsStatusText.Text = posts.Count == 1
                    ? "1 news post"
                    : $"{posts.Count:N0} news posts";

                Loading.Hide();
            };

            request.Failure += error =>
            {
                Loading.Hide();
                newsStatusText.Text = $"Could not load news posts: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private void selectNewsPost(APIAdminNewsPost post)
        {
            selectedNewsPost = post;

            newsTitleBox.Text = post.Title;
            newsSlugBox.Text = post.Slug;
            newsAuthorBox.Text = post.Author;
            newsPreviewBox.Text = post.Preview;
            newsImageBox.Text = post.FirstImage ?? string.Empty;
            newsContentBox.Text = post.Content ?? string.Empty;
            newsPublishedAtBox.Text = post.PublishedAt
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");

            newsStatusText.Text = $"editing news post #{post.Id}";
        }

        private void clearNewsEditor()
        {
            selectedNewsPost = null;

            newsTitleBox.Text = string.Empty;
            newsSlugBox.Text = string.Empty;
            newsAuthorBox.Text = api.LocalUser.Value.Username;
            newsPreviewBox.Text = string.Empty;
            newsImageBox.Text = string.Empty;
            newsPublishedAtBox.Text = string.Empty;
            newsContentBox.Text = string.Empty;

            newsStatusText.Text = "creating new news post";
        }

        private void saveNewsPost()
        {
            if (string.IsNullOrWhiteSpace(newsTitleBox.Text))
            {
                newsStatusText.Text = "title is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(newsSlugBox.Text))
                newsSlugBox.Text = slugify(newsTitleBox.Text);

            if (string.IsNullOrWhiteSpace(newsAuthorBox.Text))
                newsAuthorBox.Text = api.LocalUser.Value.Username;

            if (string.IsNullOrWhiteSpace(newsPreviewBox.Text))
            {
                newsStatusText.Text = "preview is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(newsContentBox.Text))
            {
                newsStatusText.Text = "content is required";
                return;
            }

            AdminNewsPostPayload payload = createNewsPayload();

            APIRequest<APIAdminNewsPost> request = selectedNewsPost == null
                ? new CreateAdminNewsPostRequest(payload)
                : new UpdateAdminNewsPostRequest(selectedNewsPost.Id, payload);

            Loading.Show();
            newsStatusText.Text = selectedNewsPost == null
                ? "creating news post..."
                : $"updating news post #{selectedNewsPost.Id}...";

            request.Success += post =>
            {
                Loading.Hide();

                selectedNewsPost = post;

                newsStatusText.Text = $"saved news post #{post.Id}";
                loadNews();
            };

            request.Failure += error =>
            {
                Loading.Hide();
                newsStatusText.Text = $"Could not save news post: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private AdminNewsPostPayload createNewsPayload()
        {
            DateTimeOffset? publishedAt = null;

            if (!string.IsNullOrWhiteSpace(newsPublishedAtBox.Text)
                && DateTimeOffset.TryParse(
                    newsPublishedAtBox.Text,
                    out DateTimeOffset parsed))
            {
                publishedAt = parsed;
            }

            return new AdminNewsPostPayload
            {
                Author = newsAuthorBox.Text.Trim(),
                FirstImage = string.IsNullOrWhiteSpace(newsImageBox.Text)
                    ? null
                    : newsImageBox.Text.Trim(),
                Slug = newsSlugBox.Text.Trim(),
                Title = newsTitleBox.Text.Trim(),
                Preview = newsPreviewBox.Text.Trim(),
                Content = newsContentBox.Text,
                PublishedAt = publishedAt,
            };
        }

        private void deleteNewsPost(APIAdminNewsPost post)
        {
            Loading.Show();
            newsStatusText.Text = $"deleting news post #{post.Id}...";

            var request = new DeleteAdminNewsPostRequest(post.Id);

            request.Success += () =>
            {
                Loading.Hide();

                if (selectedNewsPost?.Id == post.Id)
                    clearNewsEditor();

                newsStatusText.Text = $"deleted news post #{post.Id}";
                loadNews();
            };

            request.Failure += error =>
            {
                Loading.Hide();
                newsStatusText.Text = $"Could not delete news post: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private static string slugify(string text)
        {
            string slug = text
                .Trim()
                .ToLowerInvariant();

            slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
            slug = Regex.Replace(slug, @"^-+|-+$", "");

            return string.IsNullOrWhiteSpace(slug)
                ? "news-post"
                : slug;
        }

        private void loadScores()
        {
            int generation = ++scoreSearchGeneration;

            Loading.Show();
            scoresStatusText.Text = "searching submitted scores...";

            var request = new GetAdminScoresRequest(
                query: scoresSearchBox.Text,
                type: "recent",
                origin: "all",
                page: 1,
                limit: 50);

            request.Success += response =>
            {
                if (generation != scoreSearchGeneration)
                    return;

                scoresFlow.Clear();

                if (response.Scores.Count == 0)
                {
                    scoresFlow.Add(createEmptyState(
                        "No scores found",
                        "Try another score ID, username, user ID or beatmap ID.",
                        colours.Pink3));
                }
                else
                {
                    foreach (APIAdminScore score in response.Scores)
                        scoresFlow.Add(new AdminScoreRow(score));
                }

                scoresStatusText.Text = response.Total == 1
                    ? "1 score found"
                    : $"{response.Total:N0} scores found · newest first";
                Loading.Hide();
            };

            request.Failure += error =>
            {
                if (generation != scoreSearchGeneration)
                    return;

                Loading.Hide();
                scoresStatusText.Text = $"Could not load scores: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private void loadBeatmaps()
        {
            int generation = ++beatmapSearchGeneration;

            Loading.Show();
            beatmapsStatusText.Text = "searching beatmap database...";

            const int pageSize = 20;

            var request = new GetAdminBeatmapsRequest(
                query: beatmapsSearchBox.Text,
                status: beatmapStatusFilter,
                page: beatmapPage,
                limit: pageSize);

            request.Success += response =>
            {
                if (generation != beatmapSearchGeneration)
                    return;

                beatmapsFlow.Clear();
                updateBeatmapPagination(response.Total, pageSize);

                if (response.Beatmaps.Count == 0)
                {
                    beatmapsFlow.Add(createEmptyState(
                        "No beatmaps found",
                        "Try another beatmap ID, set ID, title, artist or mapper.",
                        colours.Purple3));
                }
                else
                {
                    var groupedSets = response.Beatmaps
                        .Where(beatmap => beatmap.BeatmapsetId > 0)
                        .GroupBy(beatmap => beatmap.BeatmapsetId)
                        .OrderByDescending(group => group.Max(beatmap => beatmap.LastUpdated))
                        .ThenBy(group => group.Key)
                        .ToArray();

                    foreach (var set in groupedSets)
                        beatmapsFlow.Add(new AdminBeatmapSetRow(set.ToArray()));

                    string filterLabel = beatmapStatusFilter == "all"
                        ? "all statuses"
                        : beatmapStatusFilter;

                    beatmapsStatusText.Text =
                        $"{groupedSets.Length:N0} sets · " +
                        $"{response.Beatmaps.Count:N0} difficulties shown · " +
                        $"{filterLabel} · " +
                        $"page {beatmapPage:N0}/{beatmapPageCount:N0}";
                }

                if (response.Beatmaps.Count == 0)
                {
                    beatmapsStatusText.Text = response.Total == 1
                        ? "1 beatmap found"
                        : $"{response.Total:N0} beatmaps found · page {beatmapPage:N0}/{beatmapPageCount:N0}";
                }
                Loading.Hide();
            };

            request.Failure += error =>
            {
                if (generation != beatmapSearchGeneration)
                    return;

                Loading.Hide();
                beatmapsStatusText.Text = $"Could not load beatmaps: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private void loadAuditLog()
        {
            int generation = ++auditLogSearchGeneration;

            Loading.Show();
            auditLogStatusText.Text = "loading Titanic audit events...";

            const int pageSize = 30;

            var request = new GetAdminAuditLogRequest(
                query: auditLogSearchBox.Text,
                targetType: auditLogTargetTypeFilter,
                action: "all",
                page: auditLogPage,
                limit: pageSize);

            request.Success += response =>
            {
                if (generation != auditLogSearchGeneration)
                    return;

                auditLogFlow.Clear();
                updateAuditLogPagination(response.Total, pageSize);

                if (response.Entries.Count == 0)
                {
                    auditLogFlow.Add(createEmptyState(
                        "No audit events found",
                        "Try another administrator, action, target ID or reason.",
                        colours.Orange3));
                }
                else
                {
                    foreach (APIAdminAuditLogEntry entry in response.Entries)
                        auditLogFlow.Add(new AdminAuditLogRow(entry));
                }

                string filterLabel = auditLogTargetTypeFilter == "all"
                    ? "all categories"
                    : $"{auditLogTargetTypeFilter} actions";

                auditLogStatusText.Text =
                    $"{response.Total:N0} audit events · " +
                    $"{filterLabel} · " +
                    $"page {auditLogPage:N0}/{auditLogPageCount:N0}";

                Loading.Hide();
            };

            request.Failure += error =>
            {
                if (generation != auditLogSearchGeneration)
                    return;

                Loading.Hide();
                auditLogStatusText.Text =
                    $"Could not load audit events: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private void loadSystemStatus()
        {
            Loading.Show();
            systemStatusText.Text = "checking services...";

            var request = new GetAdminSystemStatusRequest();

            request.Success += response =>
            {
                systemSummaryContainer.Clear();
                systemServicesFlow.Clear();

                string overall = response.OverallStatus.ToUpperInvariant();

                systemSummaryContainer.Child = createFiveColumnStatGrid(
                    new Drawable[]
                    {
                        new AdminStatCard(
                            "overall status",
                            overall,
                            $"checked {response.CheckedAt.ToLocalTime():HH:mm:ss}",
                            getServiceColour(response.OverallStatus)),

                        new AdminStatCard(
                            "stable",
                            response.Stable.Status.ToUpperInvariant(),
                            $"{response.Stable.OnlineUsers:N0} online",
                            getServiceColour(response.Stable.Status)),

                        new AdminStatCard(
                            "lazer",
                            response.Lazer.Status.ToUpperInvariant(),
                            $"{response.Lazer.OnlineUsers:N0} online",
                            getServiceColour(response.Lazer.Status)),

                        new AdminStatCard(
                            "total users",
                            Math.Max(
                                response.Stable.TotalUsers,
                                response.Lazer.TotalUsers
                            ).ToString("N0"),
                            "shared accounts",
                            colours.Blue3),

                        new AdminStatCard(
                            "active rooms",
                            response.Lazer.ActiveRooms.ToString("N0"),
                            "lazer multiplayer",
                            colours.Green3),
                    },
                    new Drawable[]
                    {
                        new AdminStatCard(
                            "stable scores",
                            response.Lazer.StableScores.ToString("N0"),
                            "stable-origin scores",
                            colours.Purple3),

                        new AdminStatCard(
                            "lazer scores",
                            response.Lazer.LazerScores.ToString("N0"),
                            "lazer-origin scores",
                            colours.Pink3),

                        new AdminStatCard(
                            "all scores",
                            response.Lazer.TotalScores.ToString("N0"),
                            "combined score records",
                            colours.Orange3),
                    });

                foreach (APIAdminServiceStatus service in response.Services)
                {
                    string value = service.Status.ToUpperInvariant();

                    if (service.LatencyMilliseconds.HasValue)
                        value += $" · {service.LatencyMilliseconds.Value:0.##} ms";

                    systemServicesFlow.Add(
                        createInformationRow(
                            string.IsNullOrWhiteSpace(service.DisplayName)
                                ? service.Name
                                : service.DisplayName,
                            string.IsNullOrWhiteSpace(service.Message)
                                ? $"Checked {service.CheckedAt.ToLocalTime():HH:mm:ss}"
                                : service.Message,
                            value,
                            getServiceColour(service.Status)));
                }

                systemStatusText.Text =
                    $"Last checked {response.CheckedAt.ToLocalTime():dd MMM yyyy, HH:mm:ss}";
                Loading.Hide();
            };

            request.Failure += error =>
            {
                Loading.Hide();
                systemStatusText.Text = $"Could not load system status: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private void loadStats()
        {
            Loading.Show();

            var request = new GetAdminStatsRequest();

            request.Success += stats =>
            {
                statsContainer.Clear();

                statsContainer.Child = createFiveColumnStatGrid(
                    new Drawable[]
                    {
                        new AdminStatCard(
                            "total users",
                            stats.TotalUsers.ToString("N0"),
                            "registered accounts",
                            colours.Blue3),

                        new AdminStatCard(
                            "online now",
                            stats.OnlineUsers.ToString("N0"),
                            "active sessions",
                            colours.Green3),

                        new AdminStatCard(
                            "scores today",
                            stats.ScoresToday.ToString("N0"),
                            "submitted in 24 hours",
                            colours.Pink3),

                        new AdminStatCard(
                            "total scores",
                            stats.ScoresTotal.ToString("N0"),
                            "all score records",
                            colours.Purple3),

                        new AdminStatCard(
                            "active restrictions",
                            stats.ActiveBans.ToString("N0"),
                            "restricted accounts",
                            colours.Red3),
                    },
                    new Drawable[]
                    {
                        new AdminStatCard(
                            "open reports",
                            stats.OpenReports.ToString("N0"),
                            "awaiting review",
                            colours.Orange3),

                        new AdminStatCard(
                            "registered today",
                            stats.RegisteredToday.ToString("N0"),
                            "new accounts today",
                            colours.Orange3),
                    });

                Loading.Hide();
            };

            request.Failure += error =>
            {
                Loading.Hide();
            };

            _ = api.PerformAsync(request);
        }

        private void searchUsers()
        {
            int generation = ++userSearchGeneration;

            Loading.Show();
            usersStatusText.Text = "loading users...";

            var request = new GetAdminUsersRequest(
                searchBox.Text,
                limit: 50);

            request.Success += response =>
            {
                if (generation != userSearchGeneration)
                    return;

                usersFlow.Clear();

                APIAdminUser[] orderedUsers = response.Users
                    .OrderBy(user => user.Id)
                    .ToArray();

                if (orderedUsers.Length == 0)
                {
                    usersFlow.Add(createEmptyState(
                        "No users found",
                        "Try another username or user ID.",
                        colours.Blue3));
                }
                else
                {
                    foreach (APIAdminUser user in orderedUsers)
                        usersFlow.Add(new AdminUserRow(user, refreshAll));
                }

                Loading.Hide();

                usersStatusText.Text = response.Total == 1
                    ? "1 user found"
                    : $"{response.Total:N0} users found · ordered by user ID";
            };

            request.Failure += error =>
            {
                if (generation != userSearchGeneration)
                    return;

                Loading.Hide();
                usersStatusText.Text = $"user search failed: {error.Message}";
            };

            _ = api.PerformAsync(request);
        }

        private Colour4 getServiceColour(string status)
        {
            switch (status.ToLowerInvariant())
            {
                case "operational":
                case "online":
                case "healthy":
                case "ok":
                    return colours.Green3;

                case "degraded":
                case "warning":
                case "slow":
                    return colours.Orange3;

                case "offline":
                case "outage":
                case "failed":
                case "unhealthy":
                case "error":
                    return colours.Red3;

                default:
                    return colours.Gray5;
            }
        }
    }
}