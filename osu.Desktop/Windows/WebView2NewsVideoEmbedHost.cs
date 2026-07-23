#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using osu.Framework.Graphics;
using osu.Game.Overlays.News.Displays;

namespace osu.Desktop.Windows
{
    internal sealed partial class WebView2NewsVideoEmbedHost : Component, INewsVideoEmbedHost
    {
        private const string virtual_host = "news-video.rinarii.local";

        private readonly object sync = new object();
        private readonly ManualResetEventSlim ready = new ManualResetEventSlim();

        private Thread uiThread;
        private NewsVideoApplicationContext context;
        private VideoForm currentForm;

        public void OpenVideo(string url, string title)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            post(() =>
            {
                currentForm?.Dispose();
                currentForm = null;

                IntPtr ownerHandle = Process.GetCurrentProcess().MainWindowHandle;

                currentForm = new VideoForm(
                    prepareEmbedUrl(url),
                    string.IsNullOrWhiteSpace(title) ? "News video" : title);

                if (ownerHandle != IntPtr.Zero)
                    currentForm.Form.Show(new WindowHandleOwner(ownerHandle));
                else
                    currentForm.Form.Show();

                currentForm.Form.Activate();
            });
        }

        public void CloseVideo()
        {
            post(() =>
            {
                currentForm?.Dispose();
                currentForm = null;
            });
        }

        private void ensureThread()
        {
            lock (sync)
            {
                if (uiThread != null)
                    return;

                uiThread = new Thread(runUiThread)
                {
                    Name = "News Video WebView2 Modal Host",
                    IsBackground = true,
                };

                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
            }
        }

        private void post(Action action)
        {
            ensureThread();

            if (Thread.CurrentThread == uiThread)
            {
                action();
                return;
            }

            if (!ready.Wait(TimeSpan.FromSeconds(5)))
                return;

            context?.Post(action);
        }

        private void runUiThread()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            context = new NewsVideoApplicationContext();
            ready.Set();

            Application.Run(context);
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
            var query = parseQuery(uri.Query);

            query["parent"] = virtual_host;

            if (!query.ContainsKey("autoplay"))
                query["autoplay"] = "false";

            var builder = new UriBuilder(uri)
            {
                Query = buildQuery(query),
            };

            return builder.Uri.ToString();
        }

        private static string prepareYoutubeWatchUrl(string url)
        {
            var uri = new Uri(url);
            var query = parseQuery(uri.Query);

            if (!query.TryGetValue("v", out string videoId) || string.IsNullOrWhiteSpace(videoId))
                return url;

            return $"https://www.youtube.com/embed/{Uri.EscapeDataString(videoId)}";
        }

        private static string prepareYoutubeShortUrl(string url)
        {
            var uri = new Uri(url);
            string videoId = uri.AbsolutePath.Trim('/');

            if (string.IsNullOrWhiteSpace(videoId))
                return url;

            return $"https://www.youtube.com/embed/{Uri.EscapeDataString(videoId)}";
        }

        private static Dictionary<string, string> parseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(query))
                return result;

            query = query.TrimStart('?');

            foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pieces = part.Split('=', 2);
                string key = Uri.UnescapeDataString(pieces[0]);

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                string value = pieces.Length > 1
                    ? Uri.UnescapeDataString(pieces[1])
                    : string.Empty;

                result[key] = value;
            }

            return result;
        }

        private static string buildQuery(Dictionary<string, string> query)
        {
            var parts = new List<string>();

            foreach ((string key, string value) in query)
                parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value ?? string.Empty)}");

            return string.Join("&", parts);
        }

        private static string createEmbedHtml(string embedUrl, string title)
        {
            string fontCss = createFontFaceCss();

            return $@"
<!doctype html>
<html>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<style>
{fontCss}

* {{
    box-sizing: border-box;
}}

html, body {{
    margin: 0;
    padding: 0;
    width: 100%;
    height: 100%;
    background: #0d0a12;
    overflow: hidden;
    font-family: 'Torus', 'Segoe UI', system-ui, sans-serif;
    color: white;
}}

body {{
    padding: 14px;
}}

.card {{
    width: 100%;
    height: 100%;
    background: #050505;
    border-radius: 14px;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    box-shadow: 0 24px 80px rgba(0, 0, 0, 0.55);
}}

.header {{
    height: 54px;
    min-height: 54px;
    background: linear-gradient(90deg, #17111f 0%, #1f1730 55%, #2b1d45 100%);
    border-left: 5px solid #663bdc;
    display: flex;
    align-items: center;
    padding: 0 10px 0 18px;
    gap: 14px;
}}

.title-block {{
    flex: 1;
    min-width: 0;
}}

.title {{
    font-size: 15px;
    font-family: 'Torus', 'Segoe UI', system-ui, sans-serif;
    font-weight: 800;
    line-height: 18px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    letter-spacing: 0.1px;
}}

.subtitle {{
    margin-top: 2px;
    font-size: 11px;
    font-family: 'Torus', 'Segoe UI', system-ui, sans-serif;
    color: #b8adc8;
    font-weight: 600;
}}

.close {{
    border: 0;
    outline: 0;
    height: 36px;
    padding: 0 18px;
    border-radius: 9px;
    background: rgba(255, 255, 255, 0.08);
    color: #fff;
    font-family: 'Torus', 'Segoe UI', system-ui, sans-serif;
    font-size: 13px;
    font-weight: 800;
    cursor: pointer;
    transition: background 120ms ease, transform 120ms ease;
}}

.close:hover {{
    background: #d94b5f;
    transform: translateY(-1px);
}}

.player-wrap {{
    flex: 1;
    background: #000;
    padding: 0;
    min-height: 0;
}}

#player {{
    width: 100%;
    height: 100%;
    border: 0;
    display: block;
    background: #000;
}}
</style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <div class='title-block'>
                <div class='title'>{escapeHtml(title)}</div>
                <div class='subtitle'>focused video player</div>
            </div>
            <button class='close' onclick='chrome.webview.postMessage(""close"")'>close</button>
        </div>

        <div class='player-wrap'>
            <iframe
                id='player'
                src='{escapeHtml(embedUrl)}'
                allow='autoplay; encrypted-media; picture-in-picture; fullscreen'
                allowfullscreen>
            </iframe>
        </div>
    </div>
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

        private static string createFontFaceCss()
        {
            string regular = getEmbeddedFontDataUri("Torus-Regular");
            string semiBold = getEmbeddedFontDataUri("Torus-SemiBold");
            string bold = getEmbeddedFontDataUri("Torus-Bold");

            var css = new List<string>();

            if (!string.IsNullOrWhiteSpace(regular))
            {
                css.Add($@"
@font-face {{
    font-family: 'Torus';
    src: url('{regular}') format('truetype');
    font-weight: 400;
    font-style: normal;
}}");
            }

            if (!string.IsNullOrWhiteSpace(semiBold))
            {
                css.Add($@"
@font-face {{
    font-family: 'Torus';
    src: url('{semiBold}') format('truetype');
    font-weight: 600 700;
    font-style: normal;
}}");
            }

            if (!string.IsNullOrWhiteSpace(bold))
            {
                css.Add($@"
@font-face {{
    font-family: 'Torus';
    src: url('{bold}') format('truetype');
    font-weight: 800 900;
    font-style: normal;
}}");
            }

            return string.Join("\n", css);
        }

        private static string getEmbeddedFontDataUri(string fontName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string[] resources;

                try
                {
                    resources = assembly.GetManifestResourceNames();
                }
                catch
                {
                    continue;
                }

                foreach (string resource in resources)
                {
                    // Do not require .ttf/.otf at the end.
                    // osu resource names may be transformed.
                    if (!resource.Contains(fontName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    using Stream stream = assembly.GetManifestResourceStream(resource);

                    if (stream == null)
                        continue;

                    using var memory = new MemoryStream();
                    stream.CopyTo(memory);

                    if (memory.Length == 0)
                        continue;

                    return $"data:font/ttf;base64,{Convert.ToBase64String(memory.ToArray())}";
                }
            }

            dumpFontResourcesForDebug();
            return string.Empty;
        }

        private static void dumpFontResourcesForDebug()
        {
            try
            {
                string debugPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "rinari-news-video-font-resources.txt");

                using var writer = new StreamWriter(debugPath, false);

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string[] resources;

                    try
                    {
                        resources = assembly.GetManifestResourceNames();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (string resource in resources)
                    {
                        if (resource.Contains("Torus", StringComparison.OrdinalIgnoreCase)
                            || resource.Contains("Font", StringComparison.OrdinalIgnoreCase)
                            || resource.Contains("font", StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine($"{assembly.GetName().Name}: {resource}");
                        }
                    }
                }
            }
            catch
            {
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (uiThread == null)
                return;

            post(() =>
            {
                currentForm?.Dispose();
                currentForm = null;
                context?.ExitThread();
            });
        }

        private sealed class NewsVideoApplicationContext : ApplicationContext
        {
            private readonly System.Windows.Forms.Timer timer;
            private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

            public NewsVideoApplicationContext()
            {
                timer = new System.Windows.Forms.Timer
                {
                    Interval = 16,
                };

                timer.Tick += (_, _) =>
                {
                    while (queue.TryDequeue(out var action))
                        action();
                };

                timer.Start();
            }

            public void Post(Action action)
            {
                queue.Enqueue(action);
            }

            protected override void Dispose(bool disposing)
            {
                timer?.Stop();
                timer?.Dispose();

                base.Dispose(disposing);
            }
        }

        private sealed class VideoForm : IDisposable
        {
            public readonly Form Form;

            private readonly WebView2 browser;
            private readonly string htmlFolder;
            private readonly string htmlPath;
            private readonly string url;
            private readonly string title;

            private bool reallyDisposing;

            public VideoForm(string url, string title)
            {
                this.url = url;
                this.title = title;

                htmlFolder = Path.Combine(
                    Path.GetTempPath(),
                    "rinari-news-video-modal");

                Directory.CreateDirectory(htmlFolder);

                htmlPath = Path.Combine(htmlFolder, "embed.html");

                Form = new Form
                {
                    Text = title,
                    Width = 1180,
                    Height = 720,
                    MinimumSize = new System.Drawing.Size(860, 520),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    BackColor = System.Drawing.Color.FromArgb(13, 10, 18),
                    KeyPreview = true,
                };

                browser = new WebView2
                {
                    Dock = DockStyle.Fill,
                    DefaultBackgroundColor = System.Drawing.Color.FromArgb(13, 10, 18),
                };

                Form.Controls.Add(browser);

                Form.FormClosing += (_, e) =>
                {
                    if (reallyDisposing)
                        return;

                    e.Cancel = true;
                    Dispose();
                };

                Form.KeyDown += (_, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                        Form.Close();
                };

                Form.Shown += async (_, _) =>
                {
                    await browser.EnsureCoreWebView2Async();

                    browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                    browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    browser.CoreWebView2.Settings.IsStatusBarEnabled = false;

                    browser.CoreWebView2.WebMessageReceived += (_, args) =>
                    {
                        if (args.TryGetWebMessageAsString() == "close")
                            Form.Close();
                    };

                    browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        virtual_host,
                        htmlFolder,
                        CoreWebView2HostResourceAccessKind.Allow);

                    File.WriteAllText(htmlPath, createEmbedHtml(this.url, this.title));
                    browser.CoreWebView2.Navigate($"https://{virtual_host}/embed.html");
                };
            }

            public void Dispose()
            {
                if (Form.IsDisposed)
                    return;

                reallyDisposing = true;

                browser?.Dispose();
                Form?.Close();
                Form?.Dispose();
            }
        }

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