// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;

namespace osu.Game.Online.API.Requests.Admin
{
    public class RestrictAdminUserRequest : APIRequest
    {
        private readonly int userId;
        private readonly string reason;

        public RestrictAdminUserRequest(int userId, string reason)
        {
            this.userId = userId;
            this.reason = reason;
        }

        protected override string Target => string.Empty;

        protected override string Uri =>
            $@"{API!.Endpoints.APIUrl}/api/admin/users/{userId}/restrict";

        protected override WebRequest CreateWebRequest()
        {
            WebRequest request = base.CreateWebRequest();

            request.Method = HttpMethod.Post;
            request.ContentType = "application/json";

            request.AddRaw(
                JsonConvert.SerializeObject(new
                {
                    reason
                }));

            return request;
        }
    }
}