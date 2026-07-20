// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Admin;

namespace osu.Game.Overlays.Toolbar
{
    public partial class ToolbarAdminButton : ToolbarOverlayToggleButton
    {
        private readonly IBindable<APIUser> localUser = new Bindable<APIUser>();

        [BackgroundDependencyLoader]
        private void load(AdminOverlay adminOverlay, IAPIProvider api)
        {
            SetIcon(new SpriteIcon
            {
                Icon = FontAwesome.Solid.UserShield,
            });

            TooltipMain = "admin";
            TooltipSub = "server management";

            StateContainer = adminOverlay;

            localUser.BindTo(api.LocalUser);
            localUser.BindValueChanged(_ => updateVisibility(), true);
        }

        private void updateVisibility()
        {
            bool isAdmin = localUser.Value?.IsAdmin == true;

            Alpha = isAdmin ? 1 : 0;
            AlwaysPresent = isAdmin;
        }

        protected override bool OnClick(osu.Framework.Input.Events.ClickEvent e)
        {
            if (localUser.Value?.IsAdmin != true)
                return true;

            return base.OnClick(e);
        }
    }
}