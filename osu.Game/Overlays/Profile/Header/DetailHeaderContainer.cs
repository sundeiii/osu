// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Profile.Header.Components;

namespace osu.Game.Overlays.Profile.Header
{
    public partial class DetailHeaderContainer : CompositeDrawable
    {
        public readonly Bindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        private Box extendedDetailsSeparator = null!;
        private ExtendedDetails extendedDetails = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING, Vertical = 10 },
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new MainDetails
                            {
                                RelativeSizeAxes = Axes.X,
                                User = { BindTarget = User }
                            },
                            extendedDetailsSeparator = new Box
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 2,
                                Colour = colourProvider.Background6,
                                Margin = new MarginPadding { Horizontal = 15 }
                            },
                            extendedDetails = new ExtendedDetails
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                User = { BindTarget = User }
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            User.BindValueChanged(user => updateDisplay(user.NewValue?.User), true);
        }

        private void updateDisplay(APIUser? user)
        {
            bool restricted = user?.IsRestricted == true;

            extendedDetailsSeparator.FadeTo(restricted ? 0 : 1, 180, Easing.OutQuint);
            extendedDetails.FadeTo(restricted ? 0 : 1, 180, Easing.OutQuint);
        }
    }
}