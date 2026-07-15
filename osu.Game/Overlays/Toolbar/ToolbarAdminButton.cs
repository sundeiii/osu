// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Admin;

namespace osu.Game.Overlays.Toolbar
{
    public partial class ToolbarAdminButton : ToolbarOverlayToggleButton
    {
        [BackgroundDependencyLoader]
        private void load(AdminOverlay adminOverlay)
        {
            SetIcon(new SpriteIcon
            {
                Icon = FontAwesome.Solid.UserShield,
            });

            TooltipMain = "admin";
            TooltipSub = "server management";

            StateContainer = adminOverlay;
        }
    }
}