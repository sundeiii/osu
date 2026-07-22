// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;

namespace osu.Game.Overlays.Admin
{
    public partial class AdminOverlayHeader
        : TabControlOverlayHeader<AdminOverlayTab>
    {
        protected override OverlayTitle CreateTitle()
            => new AdminOverlayTitle();

        private partial class AdminOverlayTitle : OverlayTitle
        {
            public AdminOverlayTitle()
            {
                Title = "server administration";
                Description = "Manage the combined stable and lazer platform.";
                Icon = FontAwesome.Solid.Server;
            }
        }
    }

    public enum AdminOverlayTab
    {
        Overview,
        Users,
        Scores,
        Beatmaps,
        Reports,
        News,
        System,
        AuditLog,
    }
}
