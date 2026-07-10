// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// This file is partly modified by GooGuTeam.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.News;
using osu.Game.Overlays.News.Displays;
using osu.Game.Overlays.News.Sidebar;

namespace osu.Game.Overlays
{
    public partial class NewsOverlay : OnlineOverlay<NewsHeader>
    {
        private readonly Bindable<string> article = new Bindable<string>();

        private readonly Container sidebarContainer;
        private readonly NewsSidebar sidebar;
        private readonly Container content;

        private GetNewsRequest listingRequest;
        private GetNewsPostRequest articleRequest;

        private Cursor lastCursor;

        /// <summary>
        /// The year currently being displayed.
        /// If null, the main listing is being displayed.
        /// </summary>
        private int? displayedYear;

        private CancellationTokenSource cancellationToken;
        private bool displayUpdateRequired = true;

        public NewsOverlay()
            : base(OverlayColourScheme.Purple, false)
        {
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize)
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension()
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        sidebarContainer = new Container
                        {
                            AutoSizeAxes = Axes.X,
                            Child = sidebar = new NewsSidebar()
                        },
                        content = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Avoid requesting news before the overlay is first opened.
            article.BindValueChanged(change =>
            {
                if (change.NewValue == null)
                    loadListing();
                else
                    loadArticle(change.NewValue);
            });
        }

        protected override NewsHeader CreateHeader()
        {
            return new NewsHeader
            {
                ShowFrontPage = ShowFrontPage
            };
        }

        protected override void PopIn()
        {
            base.PopIn();

            if (!displayUpdateRequired)
                return;

            article.TriggerChange();
            displayUpdateRequired = false;
        }

        protected override void PopOutComplete()
        {
            base.PopOutComplete();
            displayUpdateRequired = true;
        }

        public void ShowFrontPage()
        {
            // Force a refresh when already on the front page.
            if (article.Value == null)
                loadListing();
            else
                article.Value = null;

            Show();
        }

        public void ShowYear(int year)
        {
            article.Value = null;
            loadListing(year);
            Show();
        }

        public void ShowArticle(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return;

            // Bindables don't fire when assigning the same value.
            // Reload manually if the same article is selected again.
            if (article.Value == slug)
                loadArticle(slug);
            else
                article.Value = slug;

            Show();
        }

        protected void LoadDisplay(Drawable display)
        {
            ScrollFlow.ScrollToStart();

            LoadComponentAsync(
                display,
                loaded =>
                {
                    content.Child = loaded;
                    Loading.Hide();
                },
                (cancellationToken = new CancellationTokenSource()).Token);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            sidebarContainer.Height = DrawHeight;

            sidebarContainer.Y = (float)Math.Clamp(
                ScrollFlow.Current - Header.DrawHeight,
                0,
                Math.Max(
                    ScrollFlow.ScrollContent.DrawHeight
                    - DrawHeight
                    - Header.DrawHeight,
                    0));
        }

        private void loadListing(int? year = null)
        {
            Header.SetFrontPage();

            displayedYear = year;
            lastCursor = null;

            beginLoading(true);

            listingRequest = new GetNewsRequest(displayedYear);

            listingRequest.Success += response => Schedule(() =>
            {
                lastCursor = response.Cursor;
                sidebar.Metadata.Value = response.SidebarMetadata;

                sidebarContainer.Show();

                var listing = new ArticleListing(getMorePosts);

                listing.AddPosts(
                    response.NewsPosts,
                    response.Cursor != null);

                LoadDisplay(listing);
            });

            listingRequest.Failure += _ => Schedule(() =>
            {
                Loading.Hide();
            });

            API.PerformAsync(listingRequest);
        }

        private void getMorePosts()
        {
            beginLoading(false);

            listingRequest = new GetNewsRequest(
                displayedYear,
                lastCursor);

            listingRequest.Success += response => Schedule(() =>
            {
                lastCursor = response.Cursor;

                if (content.Child is ArticleListing listing)
                {
                    listing.AddPosts(
                        response.NewsPosts,
                        response.Cursor != null);
                }
            });

            API.PerformAsync(listingRequest);
        }

        private void loadArticle(string slug)
        {
            beginLoading(true);

            // We do not need the year/archive sidebar while reading.
            sidebarContainer.Hide();

            // Initially show the slug while loading.
            // It is replaced with the real title after the request succeeds.
            Header.SetArticle(slug);

            articleRequest = new GetNewsPostRequest(slug);

            articleRequest.Success += post => Schedule(() =>
            {
                Header.SetArticle(
                    string.IsNullOrWhiteSpace(post.Title)
                        ? slug
                        : post.Title);

                LoadDisplay(new ArticleDisplay(post));
            });

            articleRequest.Failure += _ => Schedule(() =>
            {
                Header.SetArticle("Unable to load article");

                LoadDisplay(new ArticleErrorDisplay(
                    "The news article could not be loaded."));
            });

            API.PerformAsync(articleRequest);
        }

        private void beginLoading(bool showLoadingOverlay)
        {
            listingRequest?.Cancel();
            articleRequest?.Cancel();
            cancellationToken?.Cancel();

            if (showLoadingOverlay)
                Loading.Show();
        }

        protected override void Dispose(bool isDisposing)
        {
            listingRequest?.Cancel();
            articleRequest?.Cancel();
            cancellationToken?.Cancel();

            base.Dispose(isDisposing);
        }

        private partial class ArticleErrorDisplay : CompositeDrawable
        {
            private readonly string message;

            public ArticleErrorDisplay(string message)
            {
                this.message = message;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(40),
                    Children = new Drawable[]
                    {
                        new osu.Game.Graphics.Containers.TextFlowContainer(text =>
                        {
                            text.Font = osu.Game.Graphics.OsuFont.GetFont(
                                size: 20,
                                weight: osu.Game.Graphics.FontWeight.Bold);
                        })
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = message
                        }
                    }
                };
            }
        }
    }
}