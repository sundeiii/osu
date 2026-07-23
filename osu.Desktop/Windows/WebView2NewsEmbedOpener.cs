#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using osu.Framework.Graphics;
using osu.Game.Overlays.News.Displays;

namespace osu.Desktop.Windows
{
    internal partial class WebView2NewsEmbedOpener : Component, INewsEmbedOpener
    {
        private const string virtual_host = "news-embed.rinarii.local";

        private Form currentForm;

        public void OpenEmbed(string url, string title)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            Thread thread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (currentForm != null && !currentForm.IsDisposed)
                    currentForm.Close();

                string embedUrl = prepareEmbedUrl(url);

                IntPtr ownerHandle = Process.GetCurrentProcess().MainWindowHandle;

                var form = new Form
                {
                    Text = string.IsNullOrWhiteSpace(title) ? "News embed" : title,
                    Width = 1120,
                    Height = 680,
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    TopMost = true,
                    BackColor = System.Drawing.Color.FromArgb(18, 18, 18),
                };

                currentForm = form;

                var root = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = System.Drawing.Color.FromArgb(18, 18, 18),
                };

                var topBar = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 42,
                    BackColor = System.Drawing.Color.FromArgb(28, 21, 34),
                };

                var titleLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = string.IsNullOrWhiteSpace(title) ? "News embed" : title,
                    ForeColor = System.Drawing.Color.White,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Padding = new Padding(14, 0, 0, 0),
                };

                var closeButton = new Button
                {
                    Dock = DockStyle.Right,
                    Width = 86,
                    Text = "close",
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = System.Drawing.Color.White,
                    BackColor = System.Drawing.Color.FromArgb(91, 55, 190),
                };

                closeButton.FlatAppearance.BorderSize = 0;
                closeButton.Click += (_, _) => form.Close();

                var browser = new WebView2
                {
                    Dock = DockStyle.Fill,
                    DefaultBackgroundColor = System.Drawing.Color.Black,
                };

                topBar.Controls.Add(titleLabel);
                topBar.Controls.Add(closeButton);

                root.Controls.Add(browser);
                root.Controls.Add(topBar);

                form.Controls.Add(root);

                form.Shown += async (_, _) =>
                {
                    await browser.EnsureCoreWebView2Async();

                    browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                    browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    browser.CoreWebView2.Settings.IsStatusBarEnabled = false;

                    string htmlFolder = Path.Combine(
                        Path.GetTempPath(),
                        "rinari-news-embed");

                    Directory.CreateDirectory(htmlFolder);

                    string htmlPath = Path.Combine(htmlFolder, "embed.html");

                    File.WriteAllText(htmlPath, createEmbedHtml(embedUrl));

                    browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        virtual_host,
                        htmlFolder,
                        CoreWebView2HostResourceAccessKind.Allow);

                    browser.CoreWebView2.Navigate($"https://{virtual_host}/embed.html");
                };

                form.FormClosed += (_, _) =>
                {
                    if (ReferenceEquals(currentForm, form))
                        currentForm = null;

                    browser.Dispose();
                    root.Dispose();
                };

                if (ownerHandle != IntPtr.Zero)
                    form.Show(new WindowHandleOwner(ownerHandle));
                else
                    form.Show();

                Application.Run(form);
            });

            thread.Name = "News WebView2 Embed";
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static string prepareEmbedUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            url = url.Trim();

            if (url.Contains("player.twitch.tv", StringComparison.OrdinalIgnoreCase))
                return prepareTwitchUrl(url);

            if (url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase))
                return prepareYoutubeWatchUrl(url);

            if (url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
                return prepareYoutubeShortUrl(url);

            return url;
        }

        private static string prepareTwitchUrl(string url)
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);

            query.Set("parent", virtual_host);

            if (string.IsNullOrWhiteSpace(query.Get("autoplay")))
                query.Set("autoplay", "false");

            var builder = new UriBuilder(uri)
            {
                Query = query.ToString() ?? string.Empty,
            };

            return builder.Uri.ToString();
        }

        private static string prepareYoutubeWatchUrl(string url)
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            string videoId = query.Get("v");

            if (string.IsNullOrWhiteSpace(videoId))
                return url;

            return $"https://www.youtube.com/embed/{escapeUrl(videoId)}";
        }

        private static string prepareYoutubeShortUrl(string url)
        {
            var uri = new Uri(url);
            string videoId = uri.AbsolutePath.Trim('/');

            if (string.IsNullOrWhiteSpace(videoId))
                return url;

            return $"https://www.youtube.com/embed/{escapeUrl(videoId)}";
        }

        private static string createEmbedHtml(string embedUrl)
        {
            return $@"
<!doctype html>
<html>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<style>
html, body {{
    margin: 0;
    padding: 0;
    width: 100%;
    height: 100%;
    background: #000;
    overflow: hidden;
}}
#player {{
    position: fixed;
    inset: 0;
    width: 100%;
    height: 100%;
    border: 0;
    background: #000;
}}
</style>
</head>
<body>
    <iframe
        id='player'
        src='{escapeHtml(embedUrl)}'
        allow='autoplay; encrypted-media; picture-in-picture; fullscreen'
        allowfullscreen>
    </iframe>
</body>
</html>";
        }

        private static string escapeHtml(string value)
            => (value ?? string.Empty)
               .Replace("&", "&amp;")
               .Replace("\"", "&quot;")
               .Replace("'", "&#39;")
               .Replace("<", "&lt;")
               .Replace(">", "&gt;");

        private static string escapeUrl(string value)
            => Uri.EscapeDataString(value ?? string.Empty);

        private sealed class WindowHandleOwner : IWin32Window
        {
            public IntPtr Handle { get; }

            public WindowHandleOwner(IntPtr handle)
            {
                Handle = handle;
            }
        }
    }
}