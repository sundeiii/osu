// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
// This file is originally created by GooGuTeam.

using System.Net.Http;
using osu.Framework.IO.Network;

namespace osu.Game.Online.API.Requests
{
    public class UnlockMedalRequest : APIRequest
    {
        public int MedalId { get; }

        protected override string Target => $"me/achievements/{MedalId}";

        public UnlockMedalRequest(int medalId)
        {
            MedalId = medalId;
        }

        protected override WebRequest CreateWebRequest()
        {
            WebRequest webRequest = base.CreateWebRequest();
            webRequest.Method = HttpMethod.Put;
            return webRequest;
        }
    }
}
