// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses.Admin
{
    public class APIAdminUser
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonProperty("country_code")]
        public string CountryCode { get; set; } = string.Empty;

        [JsonProperty("is_restricted")]
        public bool IsRestricted { get; set; }

        [JsonProperty("is_silenced")]
        public bool IsSilenced { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("join_date")]
        public DateTimeOffset JoinDate { get; set; }

        [JsonProperty("last_visit")]
        public DateTimeOffset? LastVisit { get; set; }

        [JsonProperty("global_rank")]
        public int? GlobalRank { get; set; }

        [JsonProperty("country_rank")]
        public int? CountryRank { get; set; }

        [JsonProperty("pp")]
        public double PP { get; set; }

        [JsonProperty("ranked_score")]
        public long RankedScore { get; set; }

        [JsonProperty("hit_accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("play_count")]
        public int PlayCount { get; set; }

        [JsonProperty("groups")]
        public List<Dictionary<string, object>> Groups { get; set; } =
            new List<Dictionary<string, object>>();
    }
}