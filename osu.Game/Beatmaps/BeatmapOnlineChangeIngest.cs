// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osu.Game.Database;
using osu.Game.Online.Metadata;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Ingests any changes that happen externally to the client, reprocessing as required.
    /// </summary>
    public partial class BeatmapOnlineChangeIngest : Component
    {
        private readonly IBeatmapUpdater beatmapUpdater;
        private readonly RealmAccess realm;
        private readonly MetadataClient metadataClient;
        private readonly RinariBeatmapStatusSynchroniser? rinariStatusSynchroniser;

        public BeatmapOnlineChangeIngest(IBeatmapUpdater beatmapUpdater, RealmAccess realm, MetadataClient metadataClient, RinariBeatmapStatusSynchroniser? rinariStatusSynchroniser = null)
        {
            this.beatmapUpdater = beatmapUpdater;
            this.realm = realm;
            this.metadataClient = metadataClient;
            this.rinariStatusSynchroniser = rinariStatusSynchroniser;

            metadataClient.ChangedBeatmapSetsArrived += changesDetected;
        }

        private void changesDetected(int[] beatmapSetIds)
        {
            // May want to batch incoming updates further if the background realm operations ever becomes a concern.
            realm.Run(r =>
            {
                foreach (int id in beatmapSetIds)
                {
                    var matchingSet = r.All<BeatmapSetInfo>().FirstOrDefault(s => s.OnlineID == id);

                    if (matchingSet != null)
                        beatmapUpdater.Queue(matchingSet.ToLive(realm), MetadataLookupScope.OnlineFirst);
                }
            });

            // the server telling us a set changed is also a good time to re-check its Rinari status,
            // since that isn't something the ordinary metadata lookup above understands.
            // scoped to just the reported sets, so this never re-checks the rest of the library either.
            rinariStatusSynchroniser?.QueueAutoSync(beatmapSetIds);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            metadataClient.ChangedBeatmapSetsArrived -= changesDetected;
        }
    }
}
