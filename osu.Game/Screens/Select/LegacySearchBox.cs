// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// torii: el campo de busqueda estilo osu!stable para la barra de arriba del song-select legacy.
    /// un label "Search:" verde-amarillo seguido de un text box inline sobre un fondo oscuro suave (asi
    /// se sigue leyendo sobre el fondo del beatmap, como el menu-button-background tenue de stable). va
    /// two-way con el filter query compartido asi escribir filtra el carousel igual que la busqueda
    /// moderna (que esta oculta).
    /// </summary>
    public partial class LegacySearchBox : CompositeDrawable
    {
        public Bindable<string> Current { get; } = new Bindable<string>();

        public Action? SelectPreviousAction { get; init; }
        public Action? SelectNextAction { get; init; }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = new Container
            {
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 5,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(0f, 0f, 0f, 0.4f),
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.X,
                        Height = 26,
                        Padding = new MarginPadding { Horizontal = 8 },
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = @"Search:",
                                Font = LegacyFonts.Get(20, FontWeight.Bold),
                                Colour = Color4.GreenYellow,
                                Shadow = true,
                            },
                            new SearchTextBox(this)
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Width = 200,
                                Height = 24,
                                Current = { BindTarget = Current },
                            },
                        },
                    },
                },
            };
        }

        private partial class SearchTextBox : FocusedTextBox
        {
            private readonly LegacySearchBox owner;

            public SearchTextBox(LegacySearchBox owner)
            {
                this.owner = owner;

                PlaceholderText = @"Type to search!";
                HoldFocus = true;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                BackgroundUnfocused = Color4.Transparent;
                BackgroundFocused = Color4.Transparent;
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                switch (e.Key)
                {
                    case Key.Enter:
                    case Key.KeypadEnter:
                        return false;

                    case Key.Left:
                        owner.SelectPreviousAction?.Invoke();
                        return true;

                    case Key.Right:
                        owner.SelectNextAction?.Invoke();
                        return true;

                    case Key.Up:
                    case Key.Down:
                        return false;
                }

                return base.OnKeyDown(e);
            }
        }
    }
}
