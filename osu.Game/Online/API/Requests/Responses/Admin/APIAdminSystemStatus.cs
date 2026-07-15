// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses.Admin
{
    public class APIAdminSystemStatus
    {
        [JsonProperty("overall_status")]
        public string OverallStatus { get; set; } = string.Empty;

        [JsonProperty("checked_at")]
        public DateTimeOffset CheckedAt { get; set; }

        [JsonProperty("stable")]
        public APIAdminPlatformStatus Stable { get; set; } =
            new APIAdminPlatformStatus();

        [JsonProperty("lazer")]
        public APIAdminPlatformStatus Lazer { get; set; } =
            new APIAdminPlatformStatus();

        [JsonProperty("services")]
        public List<APIAdminServiceStatus> Services { get; set; } =
            new List<APIAdminServiceStatus>();
    }

    public class APIAdminPlatformStatus
    {
        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("uptime_seconds")]
        public long UptimeSeconds { get; set; }

        [JsonProperty("online_users")]
        public int OnlineUsers { get; set; }

        [JsonProperty("total_users")]
        public int TotalUsers { get; set; }

        [JsonProperty("total_scores")]
        public long TotalScores { get; set; }

        [JsonProperty("scores_last_24_hours")]
        public int ScoresLast24Hours { get; set; }

        [JsonProperty("active_rooms")]
        public int ActiveRooms { get; set; }

        [JsonProperty("stable_scores")]
        public long StableScores { get; set; }

        [JsonProperty("lazer_scores")]
        public long LazerScores { get; set; }
    }

    public class APIAdminServiceStatus
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("latency_ms")]
        public double? LatencyMilliseconds { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("checked_at")]
        public DateTimeOffset CheckedAt { get; set; }
    }
}