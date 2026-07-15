// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses.Admin
{
    public class APIAdminStats
    {
        [JsonProperty("total_users")]
        public int TotalUsers { get; set; }

        [JsonProperty("online_users")]
        public int OnlineUsers { get; set; }

        [JsonProperty("scores_today")]
        public int ScoresToday { get; set; }

        [JsonProperty("scores_total")]
        public int ScoresTotal { get; set; }

        [JsonProperty("open_reports")]
        public int OpenReports { get; set; }

        [JsonProperty("active_bans")]
        public int ActiveBans { get; set; }

        [JsonProperty("registered_today")]
        public int RegisteredToday { get; set; }
    }
}