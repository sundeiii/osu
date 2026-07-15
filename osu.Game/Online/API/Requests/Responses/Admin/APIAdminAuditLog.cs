// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses.Admin
{
    public class APIAdminAuditLogEntry
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("admin_id")]
        public int? AdminId { get; set; }

        [JsonProperty("admin_name")]
        public string AdminName { get; set; } = "System";

        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        [JsonProperty("target_type")]
        public string? TargetType { get; set; }

        [JsonProperty("target_id")]
        public long? TargetId { get; set; }

        [JsonProperty("details")]
        public string? Details { get; set; }

        [JsonProperty("ip_address")]
        public string? IpAddress { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }
    }

    public class APIAdminAuditLogsResponse
    {
        [JsonProperty("entries")]
        public List<APIAdminAuditLogEntry> Entries { get; set; } =
            new List<APIAdminAuditLogEntry>();

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
    }
}
