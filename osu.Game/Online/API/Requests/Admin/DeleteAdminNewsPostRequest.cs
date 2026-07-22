// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using osu.Framework.IO.Network;

namespace osu.Game.Online.API.Requests.Admin
{
    public class DeleteAdminNewsPostRequest : APIRequest
    {
        private readonly int postId;

        public DeleteAdminNewsPostRequest(int postId)
        {
            this.postId = postId;
        }

        protected override string Target => string.Empty;

        protected override string Uri =>
            $@"{API!.Endpoints.APIUrl}/api/admin/news/{postId}";

        protected override WebRequest CreateWebRequest()
        {
            WebRequest request = base.CreateWebRequest();

            request.Method = HttpMethod.Delete;

            return request;
        }
    }
}