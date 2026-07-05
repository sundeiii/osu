// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;

namespace osu.Game.Overlays
{
    public class OverlayColourProvider
    {
        /// <summary>
        /// Fired when the colour provider changes and drawables should re-apply colours.
        /// Torii compatibility hook.
        /// </summary>
        public event Action? ColoursChanged;

        /// <summary>
        /// The hue degree associated with the colour shades provided by this <see cref="OverlayColourProvider"/>.
        /// </summary>
        public int Hue { get; private set; }

        private readonly int baseHue;

        public OverlayColourProvider(OverlayColourScheme colourScheme)
            : this(colourScheme.GetHue())
        {
        }

        public OverlayColourProvider(int hue)
        {
            Hue = hue;
            baseHue = hue;
        }

        public Color4 Colour0 => getColour(1, 0.8f);
        public Color4 Colour1 => getColour(1, 0.7f);
        public Color4 Colour2 => getColour(0.8f, 0.6f);
        public Color4 Colour3 => getColour(0.6f, 0.5f);
        public Color4 Colour4 => getColour(0.4f, 0.3f);

        public Color4 Highlight1 => getColour(1, 0.7f);
        public Color4 Content1 => getColour(0.4f, 1);
        public Color4 Content2 => getColour(0.4f, 0.9f);
        public Color4 Light1 => getColour(0.4f, 0.8f);
        public Color4 Light2 => getColour(0.4f, 0.75f);
        public Color4 Light3 => getColour(0.4f, 0.7f);
        public Color4 Light4 => getColour(0.4f, 0.5f);
        public Color4 Dark1 => getColour(0.2f, 0.35f);
        public Color4 Dark2 => getColour(0.2f, 0.3f);
        public Color4 Dark3 => getColour(0.2f, 0.25f);
        public Color4 Dark4 => getColour(0.2f, 0.2f);
        public Color4 Dark5 => getColour(0.2f, 0.15f);
        public Color4 Dark6 => getColour(0.2f, 0.1f);
        public Color4 Foreground1 => getColour(0.1f, 0.6f);
        public Color4 Background1 => getColour(0.1f, 0.4f);
        public Color4 Background2 => getColour(0.1f, 0.3f);
        public Color4 Background3 => getColour(0.1f, 0.25f);
        public Color4 Background4 => getColour(0.1f, 0.2f);
        public Color4 Background5 => getColour(0.1f, 0.15f);
        public Color4 Background6 => getColour(0.1f, 0.1f);

        public void ChangeColourScheme(OverlayColourScheme colourScheme) => ChangeColourScheme(colourScheme.GetHue());

        public void ChangeColourScheme(int hue)
        {
            Hue = hue;
            ColoursChanged?.Invoke();
        }

        /// <summary>
        /// Torii compatibility hook used by footer/custom hue code.
        /// </summary>
        public void ChangeAccentColourScheme(float hue)
        {
            Hue = normaliseHue(hue);
            ColoursChanged?.Invoke();
        }

        /// <summary>
        /// Torii compatibility hook used by footer/custom hue code.
        /// </summary>
        public void ResetAccentToBase()
        {
            Hue = baseHue;
            ColoursChanged?.Invoke();
        }

        private static int normaliseHue(float hue)
        {
            int result = (int)MathF.Round(hue) % 360;
            return result < 0 ? result + 360 : result;
        }

        private Color4 getColour(float saturation, float lightness) => Framework.Graphics.Colour4.FromHSL(Hue / 360f, saturation, lightness);
    }
}