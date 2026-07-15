// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses.Admin;

namespace osu.Game.Online.API.Requests.Admin
{
    public class GetAdminBeatmapsRequest : APIRequest<APIAdminBeatmapsResponse>
    {
        private readonly string query;
        private readonly string status;
        private readonly int page;
        private readonly int limit;

        public GetAdminBeatmapsRequest(
            string query = "",
            string status = "all",
            int page = 1,
            int limit = 50)
        {
            this.query = query;
            this.status = status;
            this.page = page;
            this.limit = limit;
        }

        protected override string Target => string.Empty;

        protected override string Uri =>
            $@"{API!.Endpoints.APIUrl}/api/admin/beatmaps";

        protected override WebRequest CreateWebRequest()
        {
            WebRequest request = base.CreateWebRequest();

            if (!string.IsNullOrWhiteSpace(query))
            {
                request.AddParameter(
                    "q",
                    query,
                    RequestParameterType.Query);
            }

            request.AddParameter(
                "status",
                status,
                RequestParameterType.Query);

            request.AddParameter(
                "page",
                page.ToString(),
                RequestParameterType.Query);

            request.AddParameter(
                "limit",
                limit.ToString(),
                RequestParameterType.Query);

            return request;
        }
    }
}