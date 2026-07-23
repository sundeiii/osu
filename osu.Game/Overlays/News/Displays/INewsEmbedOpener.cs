// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Overlays.News.Displays
{
    public interface INewsEmbedOpener
    {
        void OpenEmbed(string url, string title);
    }
}