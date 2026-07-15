// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests.Responses.Admin
{
    public class APIAdminBeatmap
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("beatmapset_id")]
        public int BeatmapsetId { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("creator")]
        public string Creator { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonProperty("difficulty_rating")]
        public double DifficultyRating { get; set; }

        [JsonProperty("bpm")]
        public double BPM { get; set; }

        [JsonProperty("total_length")]
        public int TotalLength { get; set; }

        [JsonProperty("max_combo")]
        public int? MaxCombo { get; set; }

        [JsonProperty("ar")]
        public double ApproachRate { get; set; }

        [JsonProperty("cs")]
        public double CircleSize { get; set; }

        [JsonProperty("od")]
        public double OverallDifficulty { get; set; }

        [JsonProperty("hp")]
        public double DrainRate { get; set; }

        [JsonProperty("checksum")]
        public string? Checksum { get; set; }

        [JsonProperty("beatmapset")]
        public APIBeatmapSet BeatmapSet { get; set; } = new APIBeatmapSet();

        [JsonProperty("last_updated")]
        public DateTimeOffset? LastUpdated { get; set; }

        [JsonProperty("ranked_date")]
        public DateTimeOffset? RankedDate { get; set; }
    }

    public class APIAdminBeatmapsResponse
    {
        [JsonProperty("beatmaps")]
        public List<APIAdminBeatmap> Beatmaps { get; set; } = new List<APIAdminBeatmap>();

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
    }
}
