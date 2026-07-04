using System;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API;

namespace osu.Game.Online.Solo
{
    public class UploadReplayRequest : APIRequest<object>
    {
        private readonly long scoreId;
        private readonly int userId;
        private readonly int beatmapId;
        private readonly byte[] replayBytes;

        public UploadReplayRequest(
            long scoreId,
            int userId,
            int beatmapId,
            byte[] replayBytes)
        {
            this.scoreId = scoreId;
            this.userId = userId;
            this.beatmapId = beatmapId;
            this.replayBytes = replayBytes;
        }

        // Required by APIRequest, but unused because Uri is overridden.
        protected override string Target => string.Empty;

        protected override string Uri =>
            $@"{API!.Endpoints.APIUrl}/_lio/scores/replay";

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();

            request.Method = HttpMethod.Post;
            request.ContentType = "application/json";
            request.Timeout = 30000;

            request.AddRaw(JsonConvert.SerializeObject(new
            {
                score_id = scoreId,
                user_id = userId,
                beatmap_id = beatmapId,
                mreplay = Convert.ToBase64String(replayBytes),
            }));

            return request;
        }
    }
}