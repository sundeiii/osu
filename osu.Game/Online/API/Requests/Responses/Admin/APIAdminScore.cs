// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;

namespace osu.Game.Online.API.Requests.Responses.Admin
{
    public class APIAdminScore
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("user_id")]
        public long? UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        [JsonProperty("beatmap_title")]
        public string BeatmapTitle { get; set; } = string.Empty;

        [JsonProperty("beatmap_version")]
        public string BeatmapVersion { get; set; } = string.Empty;

        [JsonProperty("difficulty_rating")]
        public double DifficultyRating { get; set; }

        [JsonProperty("rank")]
        public string Rank { get; set; } = string.Empty;

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("pp")]
        public double? PP { get; set; }

        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty("mods")]
        public List<string> Mods { get; set; } = new List<string>();

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        [JsonProperty("classic_total_score")]
        public long ClassicTotalScore { get; set; }

        [JsonProperty("origin")]
        public string Origin { get; set; } = string.Empty;

        [JsonProperty("gamemode")]
        public string Gamemode { get; set; } = string.Empty;

        [JsonProperty("has_replay")]
        public bool HasReplay { get; set; }

        [JsonProperty("ranked")]
        public bool Ranked { get; set; }

        [JsonProperty("passed")]
        public bool Passed { get; set; }

        [JsonProperty("flagged")]
        public bool Flagged { get; set; }

        [JsonProperty("replay_download_url")]
        public string? ReplayDownloadUrl { get; set; }

        [JsonProperty("beatmapset")]
        public APIBeatmapSet BeatmapSet { get; set; } = new APIBeatmapSet();

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        public ScoreInfo CreateScoreInfo()
        {
            bool isStable = Origin.Equals("stable", StringComparison.OrdinalIgnoreCase);

            return new ScoreInfo
            {
                OnlineID = isStable ? -1 : Id,
                LegacyOnlineID = isStable ? Id : -1,
                HasOnlineReplay = HasReplay,
                TotalScore = TotalScore,
                LegacyTotalScore = ClassicTotalScore > 0 ? ClassicTotalScore : null,
                Accuracy = Accuracy,
                MaxCombo = MaxCombo,
                PP = PP,
                Ranked = Ranked,
                Passed = Passed,
                Date = CreatedAt ?? DateTimeOffset.MinValue,
                User = new APIUser
                {
                    Id = (int)(UserId ?? 0),
                    Username = Username,
                },
            };
        }
    }

    public class APIAdminScoresResponse
    {
        [JsonProperty("scores")]
        public List<APIAdminScore> Scores { get; set; } = new List<APIAdminScore>();

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
    }
}