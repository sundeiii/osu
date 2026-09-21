// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Statistics.Session
{
    /// <summary>
    /// Browse the stats of every recorded session, or of all plays combined.
    /// </summary>
    public partial class SessionBrowserScreen : OsuScreen
    {
        public override bool HideOverlaysOnEnter => true;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [Resolved]
        private SessionStatsStore store { get; set; } = null!;

        private FillFlowContainer<SessionListItem> list = null!;
        private OsuSpriteText heading = null!;
        private Container detailHolder = null!;
        private CancellationTokenSource? loadCancellation;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    // leave room for the toolbar at the top and the back button at the bottom.
                    Padding = new MarginPadding { Top = Overlays.Toolbar.Toolbar.HEIGHT + 20, Bottom = 80, Left = 20, Right = 20 },
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 340),
                            new Dimension(GridSizeMode.Absolute, 20),
                            new Dimension(),
                        },
                        Content = new[]
                        {
                            new[] { createListPanel(), new Container(), createDetailPanel() }
                        }
                    }
                }
            };

            populateList();
        }

        private Drawable createListPanel() => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            CornerRadius = 10,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = list = new FillFlowContainer<SessionListItem>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(8),
                        Spacing = new Vector2(0, 4),
                    }
                }
            }
        };

        private Drawable createDetailPanel() => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            CornerRadius = 10,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(20),
                        Spacing = new Vector2(0, 16),
                        Children = new Drawable[]
                        {
                            heading = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(size: 24, weight: FontWeight.Bold),
                            },
                            detailHolder = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                            },
                        }
                    }
                }
            }
        };

        private void populateList()
        {
            var sessions = store.GetSessions();

            if (sessions.Count == 0)
            {
                heading.Text = "No sessions yet";

                detailHolder.Child = new OsuSpriteText
                {
                    Text = "Finish a map to the results screen and your stats will show up here.",
                    Font = OsuFont.GetFont(size: 16),
                    Colour = Color4.White.Opacity(0.6f),
                };

                return;
            }

            var entries = new List<SessionEntry>
            {
                new SessionEntry("All time", sessions.SelectMany(s => s.Plays).OrderBy(p => p.PlayedAt).ToList()),
            };

            entries.AddRange(sessions.Select(s => new SessionEntry(
                $"{s.Start.LocalDateTime:ddd d MMM · HH:mm}{(s.SessionId == store.SessionId ? " (current)" : string.Empty)}",
                s.Plays)));

            foreach (var entry in entries)
            {
                SessionListItem? item = null;

                item = new SessionListItem(entry)
                {
                    Action = () => select(item!),
                };

                list.Add(item);
            }

            // prefer the current session, otherwise the most recent one.
            bool currentHasPlays = sessions.Any(s => s.SessionId == store.SessionId);
            select(list[currentHasPlays ? entries.FindIndex(e => e.Title.EndsWith("(current)")) : 1]);
        }

        private void select(SessionListItem item)
        {
            foreach (var other in list)
                other.Selected = other == item;

            heading.Text = item.Entry.Title;

            loadCancellation?.Cancel();
            loadCancellation = new CancellationTokenSource();

            LoadComponentAsync(new SessionStatsDisplay(item.Entry.Plays, showPlayList: true), display => detailHolder.Child = display, loadCancellation.Token);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            loadCancellation?.Cancel();
        }

        public record SessionEntry(string Title, IReadOnlyList<SessionPlayRecord> Plays)
        {
            public string Subtitle
            {
                get
                {
                    var summary = SessionSummary.Create(Plays);
                    string unstableRate = summary.AverageUnstableRate?.ToString("N1") ?? "-";

                    return $"{Plays.Count} play{(Plays.Count == 1 ? string.Empty : "s")} · {summary.AverageAccuracy:P1} · UR {unstableRate}";
                }
            }
        }

        public partial class SessionListItem : OsuClickableContainer
        {
            public SessionEntry Entry { get; }

            private Box background = null!;
            private bool selected;

            public bool Selected
            {
                get => selected;
                set
                {
                    selected = value;
                    updateState();
                }
            }

            public SessionListItem(SessionEntry entry)
            {
                Entry = entry;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                Masking = true;
                CornerRadius = 6;

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Highlight1,
                        Alpha = 0,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(10),
                        Spacing = new Vector2(0, 2),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = Entry.Title,
                                Font = OsuFont.GetFont(size: 15, weight: FontWeight.SemiBold),
                            },
                            new OsuSpriteText
                            {
                                Text = Entry.Subtitle,
                                Font = OsuFont.GetFont(size: 12),
                                Colour = Color4.White.Opacity(0.7f),
                            },
                        }
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                updateState();
            }

            protected override bool OnHover(osu.Framework.Input.Events.HoverEvent e)
            {
                updateState();
                return base.OnHover(e);
            }

            protected override void OnHoverLost(osu.Framework.Input.Events.HoverLostEvent e)
            {
                base.OnHoverLost(e);
                updateState();
            }

            private void updateState() => background?.FadeTo(selected ? 0.3f : IsHovered ? 0.12f : 0f, 100);
        }
    }
}
