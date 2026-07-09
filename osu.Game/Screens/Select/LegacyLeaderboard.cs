// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Screens.Play.Leaderboards;
using osu.Game.Skinning;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;
using CommonStrings = osu.Game.Localisation.CommonStrings;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// torii: el panel de ranking abajo a la izquierda estilo osu!stable para la UI legacy. dropdown
    /// de scope ("Local Ranking") mas la lista de scores del beatmap (badge de rank / player / score /
    /// accuracy), y el grafico "selection-norecords" de stable cuando no hay nada. usa el
    /// <see cref="LeaderboardManager"/> compartido solo mientras la UI legacy esta activa asi no se
    /// pelea con los fetches del leaderboard moderno.
    /// </summary>
    public partial class LegacyLeaderboard : CompositeDrawable
    {
        public readonly Bindable<BeatmapLeaderboardScope> Scope = new Bindable<BeatmapLeaderboardScope>(BeatmapLeaderboardScope.Local);

        /// <summary>
        /// torii: lo dispara SongSelect (con carousel.ScrollToSelection). hoverear la leaderboard hace
        /// volver el carousel a la cancion seleccionada, como en el modo normal. solo se dispara sobre
        /// la region real de la leaderboard (scores/dropdown/PB), no sobre la zona vacia de al lado.
        /// </summary>
        public Action? HoverScrollRequested { get; init; }

        private InputManager inputManager = null!;
        private bool wasLeaderboardHovered;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>si la UI legacy (y por ende este leaderboard) esta visible ahora.</summary>
        private Bindable<bool> legacyActive = null!;

        [Resolved]
        private LeaderboardManager leaderboardManager { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        private readonly IBindable<LeaderboardScores?> scores = new Bindable<LeaderboardScores?>();

        private Bindable<ScoringMode> scoringMode = null!;

        private OsuScrollContainer scoreScroll = null!;
        private Container scoreContainer = null!;
        private Container personalBest = null!;
        private LegacyStableDropdown<BeatmapLeaderboardScope> scopeDropdown = null!;
        private Sprite noRecords = null!;
        private LoadingSpinner loadingSpinner = null!;

        private const float row_step = 52;
        // en stable el contenido de la fila llega a ~x258 (time-ago) / x236 (acc) en espacio 480; la
        // carta es un poco mas ancha que eso -> ~430 en el espacio x1.6.
        private const float list_width = 410;
        private const int max_rows = 8;

        // la lista muestra max_rows a la vez (como stable); si hay mas, scrollean.
        private const float list_top = 150;
        private const float scroll_height = max_rows * row_step;
        // el Personal Best va pegado abajo del scroll (no forma parte), con un huequito.
        private const float pb_top = list_top + scroll_height + 12;

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin)
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                // lista de scores con drag-scroll. las filas las posiciono a mano (Y = i * step)
                // adentro del scroll asi cada una hace su propio slide-in sin pelearse con el flow.
                // alto fijo (max_rows) para que la zona de abajo a la izquierda siempre se quede con
                // el input (el carousel no dragea ahi) y los scores de mas scrollean.
                scoreScroll = new OsuScrollContainer
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(2, list_top),
                    Width = list_width,
                    Height = scroll_height,
                    ScrollbarVisible = false,
                    Child = scoreContainer = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    },
                },
                loadingSpinner = new LoadingSpinner
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Position = new Vector2(2 + list_width / 2, list_top + 180),
                },
                noRecords = new Sprite
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Position = new Vector2(2 + list_width / 2, list_top + 150),
                    Texture = skin.GetTexture(@"selection-norecords"),
                    Alpha = 0,
                },
                // seccion Personal Best, pegada abajo del scroll (no scrollea con la lista).
                personalBest = new Container
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(2, pb_top),
                    Width = list_width,
                    AutoSizeAxes = Axes.Y,
                },
                // se dibuja ultimo asi el menu abierto del dropdown queda arriba de los scores / "no records".
                scopeDropdown = new LegacyStableDropdown<BeatmapLeaderboardScope>
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Depth = -10,
                    Position = new Vector2(8, 117),
                    Width = 309,
                    AccentColour = new Color4(31 / 255f, 184 / 255f, 255 / 255f, 1f),
                    HoverColour = new Color4(255 / 255f, 102 / 255f, 171 / 255f, 1f),
                    Items = Enum.GetValues<BeatmapLeaderboardScope>(),
                    ItemText = scope => $@"{scope} Ranking",
                    Current = { BindTarget = Scope },
                },
            };
        }

        // overlay de pantalla completa, pero solo el contenido del leaderboard (scores / dropdown)
        // tiene que capturar input. el lado derecho vacio deja pasar asi se puede dragear el carousel.
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            foreach (var child in InternalChildren)
            {
                if (child.ReceivePositionalInputAt(screenSpacePos))
                    return true;
            }

            return false;
        }

        protected override void Update()
        {
            base.Update();

            // torii: cuando el mouse entra a la region real de la leaderboard (la misma que captura
            // input) disparamos el scroll-a-la-seleccion una sola vez. la zona vacia de al lado no
            // entra aca (RPIAt da false ahi), asi sigue siendo solo para drag-scroll.
            bool over = HoverScrollRequested != null && inputManager != null
                        && ReceivePositionalInputAt(inputManager.CurrentState.Mouse.Position);

            if (over && !wasLeaderboardHovered)
                HoverScrollRequested?.Invoke();

            wasLeaderboardHovered = over;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager()!;

            legacyActive = config.GetBindable<bool>(OsuSetting.ToriiLegacySongSelectFooter);

            var savedScope = config.GetBindable<string>(OsuSetting.ToriiLegacyLeaderboardScope);

            if (Enum.TryParse(savedScope.Value, out BeatmapLeaderboardScope loadedScope))
                Scope.Value = loadedScope;

            Scope.BindValueChanged(scope => savedScope.Value = scope.NewValue.ToString());

            scoringMode = config.GetBindable<ScoringMode>(OsuSetting.ScoreDisplayMode);
            scoringMode.BindValueChanged(_ => updateScores());

            scores.BindTo(leaderboardManager.Scores);
            scores.BindValueChanged(_ => updateScores());

            beatmap.BindValueChanged(_ => refetch());
            ruleset.BindValueChanged(_ => refetch());
            Scope.BindValueChanged(_ => refetch());
            legacyActive.BindValueChanged(active =>
            {
                if (active.NewValue)
                    refetch();
            }, true);

            updateScores();
        }

        private void refetch()
        {
            // solo me adueño del leaderboard mientras la UI legacy esta visible; si no, se lo dejo al
            // leaderboard moderno asi no se pisan los fetches.
            if (!legacyActive.Value)
                return;

            leaderboardManager.FetchWithCriteria(new LeaderboardCriteria(beatmap.Value?.BeatmapInfo, ruleset.Value, Scope.Value, null));
        }

        private static long getDisplayScore(ScoreInfo score, ScoringMode mode)
        {
            if (mode == ScoringMode.Standardised)
                return score.TotalScore;

            if (score.LegacyTotalScore is long legacyTotalScore && legacyTotalScore > 0)
                return legacyTotalScore;

            return score.GetDisplayScore(mode);
        }

        private void updateScores()
        {
            if (IsDisposed)
                return;

            var result = scores.Value;

            // null == hay un fetch en curso: limpio, muestro el spinner y espero los resultados.
            if (result == null)
            {
                scoreContainer.Clear();
                personalBest.Clear();
                noRecords.FadeOut(80);
                loadingSpinner.Show();
                return;
            }

            loadingSpinner.Hide();
            scoreContainer.Clear();
            personalBest.Clear();
            scoreScroll.ScrollToStart(false);

            var top = result.TopScores;

            if (top == null || top.Count == 0)
            {
                noRecords.ClearTransforms();
                noRecords.ScaleTo(0.9f).Then().ScaleTo(1f, 600, Easing.OutQuint);
                noRecords.FadeInFromZero(300, Easing.OutQuint);
                return;
            }

            noRecords.FadeOut(80);

            // hasta 50 (el tope de stable); lo que pase de max_rows scrollea adentro de la lista.
            var list = top
                .OrderByDescending(s => getDisplayScore(s, scoringMode.Value))
                .Take(50)
                .ToList();
            int cascade = 0;

            for (int i = 0; i < list.Count; i++)
            {
                long? diff = i < list.Count - 1
                    ? getDisplayScore(list[i], scoringMode.Value) - getDisplayScore(list[i + 1], scoringMode.Value)
                    : null;

                addAnimated(scoreContainer, new LegacyScoreRow(list[i], list[i].User.Username, diff, scoringMode.Value) { Y = i * row_step }, cascade++);
            }

            // seccion Personal Best, pegada abajo del scroll (las posiciones son relativas a personalBest).
            addAnimated(personalBest, new StrokedLegacyText
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopCentre,
                Position = new Vector2(list_width / 2, 0),
                Text = @"Personal Best",
                Font = LegacyFonts.Get(20, FontWeight.Bold),
            }, cascade++);

            if (result.UserScore != null)
            {
                string name = result.UserScore.Position is int pos
                    ? $"#{pos:#,0} of {result.TotalScores:#,0}"
                    : result.UserScore.User.Username;

                addAnimated(personalBest, new LegacyScoreRow(result.UserScore, name, null, scoringMode.Value) { Y = 28 }, cascade++);
            }
            else
            {
                // el placeholder vacio igual lleva la misma caja oscura, como si hubiera un score ahi.
                addAnimated(personalBest, new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 50,
                    Y = 28,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 4,
                            Child = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(new Color4(0f, 0f, 0f, 0.5f), new Color4(0f, 0f, 0f, 0.12f)),
                            },
                        },
                        new StrokedLegacyText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = @"No personal record set",
                            Font = LegacyFonts.Get(20, FontWeight.Light),
                        },
                    },
                }, cascade++);
            }
        }

        /// <summary>
        /// agrega un elemento al <paramref name="target"/> con el pop-in en cascada de stable: entra
        /// deslizandose desde la izquierda y aparece, escalonado 50ms por elemento, con rebote OutBack.
        /// la "ola" de arriba hacia abajo.
        /// </summary>
        private void addAnimated(Container target, Drawable d, int cascadeIndex)
        {
            float targetX = d.X;
            target.Add(d);

            d.MoveToX(targetX - 30).FadeOut();
            d.Delay(cascadeIndex * 50).FadeIn(200, Easing.OutQuint).MoveToX(targetX, 400, Easing.OutBack);
        }

        private partial class LegacyScoreRow : OsuClickableContainer, IHasContextMenu, IHasCustomTooltip<ScoreInfo>
        {
            private readonly ScoreInfo score;
            private readonly string displayName;
            private readonly long? scoreDiff;
            private readonly ScoringMode scoringMode;

            private static readonly OverlayColourProvider fallback_colour = new OverlayColourProvider(OverlayColourScheme.Blue);

            [Resolved]
            private ScoreManager scoreManager { get; set; } = null!;

            [Resolved]
            private IAPIProvider api { get; set; } = null!;

            [Resolved(CanBeNull = true)]
            private OverlayColourProvider? colourProvider { get; set; }

            [Resolved(CanBeNull = true)]
            private Bindable<IReadOnlyList<Mod>>? selectedMods { get; set; }

            [Resolved(CanBeNull = true)]
            private ISongSelect? songSelect { get; set; }

            [Resolved(CanBeNull = true)]
            private IDialogOverlay? dialogOverlay { get; set; }

            [Resolved(CanBeNull = true)]
            private OsuGame? game { get; set; }

            private Box hoverHighlight = null!;

            public LegacyScoreRow(ScoreInfo score, string displayName, long? scoreDiff, ScoringMode scoringMode)
            {
                this.score = score;
                this.displayName = displayName;
                this.scoreDiff = scoreDiff;
                this.scoringMode = scoringMode;
            }

            private static Drawable createRankDrawable(ISkinSource skin, ScoreRank rank)
            {
                var texture = skin.GetTexture($@"ranking-{rank}-small");

                // Normal skins: use the skin's rank letter.
                // Example: BTMC skin, normal SS/S/A/etc texture.
                if (texture != null && texture.Height > 0 && texture.Width <= texture.Height * 8)
                {
                    const float rank_height = 42f;
                    float rankWidth = texture.Width * (rank_height / texture.Height);

                    return new Sprite
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(56, 0),
                        Size = new Vector2(rankWidth, rank_height),
                        Texture = texture,
                    };
                }

                // Weird wide skins: do not use the giant strip as the letter.
                // Zynight uses the wide texture as row art, handled by createRankRowBackground().
                return new UpdateableRank(rank)
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Position = new Vector2(56, 0),
                    Size = new Vector2(40),
                };
            }

            private static Drawable? createRankRowBackground(ISkinSource skin, ScoreRank rank)
            {
                var texture = skin.GetTexture($@"ranking-{rank}-small");

                if (texture == null || texture.Height <= 0)
                    return null;

                // Normal rank icons should NOT become row backgrounds.
                if (texture.Width <= texture.Height * 8)
                    return null;

                // Wide skin rank textures (Zynight / Zylice style):
                // take the LEFT part only.
                const float targetAspect = 1123f / 174f;

                float sourceHeight = texture.Height;
                float sourceWidth = Math.Min(texture.Width, sourceHeight * targetAspect);

                const float cut_left = 72f;

                texture = texture.Crop(new RectangleF(
                    cut_left,
                    0,
                    Math.Max(1, sourceWidth - cut_left),
                    sourceHeight
                ));

                return new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Child = new Sprite
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,

                        // Move wide skin row art to the right.
                        Position = new Vector2(138, 0),

                        Height = 50,
                        Width = 50 * (texture.Width / (float)texture.Height),
                        Texture = texture,
                        Alpha = 0.65f,
                    },
                };
            }

            [BackgroundDependencyLoader]
            private void load(ISkinSource skin)
            {
                RelativeSizeAxes = Axes.X;
                Height = 50;
                Masking = true;
                CornerRadius = 4;

                if (songSelect?.CanPresentScore == true)
                    Action = () => songSelect.PresentScore(score);

                string modString = string.Join(@",", score.Mods.Select(m => m.Acronym));

                var rightColumn = new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    X = -10,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 1),
                };

                if (modString.Length > 0)
                    rightColumn.Add(shadowed(modString, 14, FontWeight.Bold, Anchor.TopRight));

                rightColumn.Add(shadowed($"{score.Accuracy:0.00%}", 16, FontWeight.Bold, Anchor.TopRight));
                rightColumn.Add(shadowed(scoreDiff is long d ? $"+{d:#,0}" : @"-", 13, FontWeight.Regular, Anchor.TopRight, new Color4(0.82f, 0.86f, 0.9f, 1f)));


                var rankRowBackground = createRankRowBackground(skin, score.Rank);


                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 4,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(new Color4(0f, 0f, 0f, 0.5f), new Color4(0f, 0f, 0f, 0.12f)),
                            },
                        },
                    },

                    // Zynight/wide skin rank artwork fitted into the row background.
                    rankRowBackground ?? Empty(),

                    hoverHighlight = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },

                    new UpdateableAvatar(score.User, isInteractive: false)
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 6,
                        Size = new Vector2(44),
                        Masking = true,
                        CornerRadius = 4,
                    },

                    createRankDrawable(skin, score.Rank),

                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 114,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            shadowed(displayName, 25, FontWeight.Regular, Anchor.TopLeft),
                            shadowed($"Score: {getDisplayScore(score, scoringMode):#,0} ({score.MaxCombo:#,0}x)", 17, FontWeight.Regular, Anchor.TopLeft),
                        },
                    },

                    rightColumn,
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                hoverHighlight.FadeTo(0.1f, 100, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hoverHighlight.FadeOut(250, Easing.OutQuint);
                base.OnHoverLost(e);
            }

            ITooltip<ScoreInfo> IHasCustomTooltip<ScoreInfo>.GetCustomTooltip() => new BeatmapLeaderboardScore.LeaderboardScoreTooltip(colourProvider ?? fallback_colour);

            ScoreInfo IHasCustomTooltip<ScoreInfo>.TooltipContent => score;

            MenuItem[] IHasContextMenu.ContextMenuItems
            {
                get
                {
                    var items = new List<MenuItem>();

                    // los system mods nunca se copian.
                    var copyableMods = score.Mods.Where(m => m.Type != ModType.System).ToArray();

                    if (copyableMods.Length > 0 && selectedMods != null)
                        items.Add(new OsuMenuItem(SongSelectStrings.UseTheseMods, MenuItemType.Highlighted, () => selectedMods.Value = copyableMods));

                    if (score.OnlineID > 0)
                        items.Add(new OsuMenuItem(CommonStrings.CopyLink, MenuItemType.Standard, () => game?.CopyToClipboard($@"{api.Endpoints.WebsiteUrl}/scores/{score.OnlineID}")));

                    if (score.Files.Count <= 0)
                        return items.ToArray();

                    if (items.Count > 0)
                        items.Add(new OsuMenuItemSpacer());

                    if (songSelect?.CanPresentScore == true)
                        items.Add(new OsuMenuItem(SongSelectStrings.WatchReplay, MenuItemType.Standard, () => songSelect.PresentScore(score, ScorePresentType.Gameplay)));

                    items.Add(new OsuMenuItem(CommonStrings.Export, MenuItemType.Standard, () => scoreManager.Export(score)));
                    items.Add(new OsuMenuItem(osu.Game.Resources.Localisation.Web.CommonStrings.ButtonsDelete, MenuItemType.Destructive, () => dialogOverlay?.Push(new LocalScoreDeleteDialog(score))));

                    return items.ToArray();
                }
            }

            // estilo stable: texto con un contorno oscuro finito asi se lee sobre fondos claros o
            // cargados (stable contornea el texto en vez de ponerle drop-shadow).
            private static StrokedLegacyText shadowed(string text, float size, FontWeight weight, Anchor anchor, Color4? colour = null) => new StrokedLegacyText
            {
                Anchor = anchor,
                Origin = anchor,
                Text = text,
                Font = LegacyFonts.Get(size, weight),
                Colour = colour ?? Color4.White,
            };
        }
    }
}
