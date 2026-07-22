// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses.Admin;

namespace osu.Game.Online.API.Requests.Admin
{
    public class UpdateAdminNewsPostRequest
        : APIRequest<APIAdminNewsPost>
    {
        private readonly int postId;
        private readonly AdminNewsPostPayload payload;

        public UpdateAdminNewsPostRequest(
            int postId,
            AdminNewsPostPayload payload)
        {
            this.postId = postId;
            this.payload = payload;
        }

        protected override string Target => string.Empty;

        protected override string Uri =>
            $@"{API!.Endpoints.APIUrl}/api/admin/news/{postId}";

        protected override WebRequest CreateWebRequest()
        {
            WebRequest request = base.CreateWebRequest();

            request.Method = HttpMethod.Put;
            request.ContentType = "application/json";
            request.AddRaw(JsonConvert.SerializeObject(payload));

            return request;
        }
    }
}