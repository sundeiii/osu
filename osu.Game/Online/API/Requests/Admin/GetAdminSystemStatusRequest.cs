// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses.Admin;

namespace osu.Game.Online.API.Requests.Admin
{
    public class GetAdminSystemStatusRequest : APIRequest<APIAdminSystemStatus>
    {
        protected override string Target => string.Empty;

        protected override string Uri =>
            $@"{API!.Endpoints.APIUrl}/api/admin/system";
    }
}