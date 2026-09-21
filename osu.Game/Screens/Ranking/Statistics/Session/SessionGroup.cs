// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Screens.Ranking.Statistics.Session
{
    /// <summary>
    /// All plays recorded during a single session.
    /// </summary>
    public class SessionGroup
    {
        public string SessionId { get; }

        /// <summary>
        /// The plays of this session, oldest first.
        /// </summary>
        public IReadOnlyList<SessionPlayRecord> Plays { get; }

        public DateTimeOffset Start { get; }

        public DateTimeOffset End { get; }

        public SessionGroup(string sessionId, IEnumerable<SessionPlayRecord> plays)
        {
            SessionId = sessionId;
            Plays = plays.OrderBy(p => p.PlayedAt).ToList();

            Start = Plays.Count == 0 ? DateTimeOffset.MinValue : Plays[0].PlayedAt;
            End = Plays.Count == 0 ? DateTimeOffset.MinValue : Plays[^1].PlayedAt;
        }
    }
}
