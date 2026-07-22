// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Admin
{
    public class AdminNewsPostPayload
    {
        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;

        [JsonProperty("first_image")]
        public string? FirstImage { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("preview")]
        public string Preview { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }
    }
}