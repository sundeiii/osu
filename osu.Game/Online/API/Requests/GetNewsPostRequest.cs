// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetNewsPostRequest : APIRequest<APINewsPost>
    {
        private readonly string slug;

        public GetNewsPostRequest(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("News post slug cannot be empty.", nameof(slug));

            this.slug = slug;
        }

        protected override string Target => $"news/{Uri.EscapeDataString(slug)}";
    }
}