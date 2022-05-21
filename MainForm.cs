#nullable disable warnings

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebPageHost
{
    public partial class MainForm : Form
    {
        // Custom form events
        public EventHandler FormLoaded;

        // WebView2
        public WebView2 webView { get; private set; }
        private bool isWebViewInitialized;
        private string userDataFolderName;
        public EventHandler WebViewDOMContentLoaded;

        // Properties
        public string Url
        {
            get { return url; }
            set { 
                url = value; 
                if (this.isWebViewInitialized)
                {
                    NavigateToUrl(this.Url);
                }
            }
        }
        private string url;

        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        private string title;
        private bool isTitleGiven;

        public bool AllowSso
        {
            get { return allowSso; }
            set { allowSso = value; }
        }
        private bool allowSso;


        // MainForm
        public MainForm(string userDataFolderName, string url, string title = "", bool allowSso = false)
        {
            if (String.IsNullOrWhiteSpace(userDataFolderName))
            {
                throw new ArgumentException("Parameter 'userDataFolderName' is required");
            }
            if (String.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Parameter 'url' is required");
            }

            this.Url = url;
            this.Title = title;
            this.isTitleGiven = !String.IsNullOrWhiteSpace(title);
            this.allowSso = allowSso;
            this.userDataFolderName = userDataFolderName;

            AutoScaleMode = AutoScaleMode.Dpi;

            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Initialize WebView2
            var webViewEnv = await CoreWebView2Environment.CreateAsync(null, this.userDataFolderName,
                new CoreWebView2EnvironmentOptions() { AllowSingleSignOnUsingOSPrimaryAccount = AllowSso });
            this.webView = new WebView2();
            this.SuspendLayout();
            this.Controls.Add(this.webView);
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.webView.Parent = this;
            this.webView.Name = "webView";
            this.webView.CreationProperties = null;
            var padding = 0;
            this.webView.Bounds = new Rectangle(padding, padding, ClientSize.Width - 2*padding, ClientSize.Height - 2*padding);
            this.webView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.webView.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView.ZoomFactor = 1D;
            this.webView.Visible = true;
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.ResumeLayout(true);
            this.webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            await webView.EnsureCoreWebView2Async(webViewEnv);

            this.webView.NavigationCompleted += WebView_NavigationCompleted;

            // Initialize web content
            this.isWebViewInitialized = true;
            NavigateToUrl(this.Url);
            UpdateFormTitle(this.Title, this.Url);

            // Raise form loaded event
            FormLoaded?.Invoke(this, new EventArgs());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "<Pending>")]
        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.NewWindowRequested += (s, e) => e.NewWindow = webView.CoreWebView2;
            webView.CoreWebView2.SourceChanged += (s, e) => this.url = webView.Source.AbsoluteUri; // update field directly not using setter to avoid navigation
            webView.CoreWebView2.DocumentTitleChanged += (s, e) => {
                if (!isTitleGiven) this.Text = webView.CoreWebView2.DocumentTitle;
            };
            webView.CoreWebView2.DOMContentLoaded += async (s, e) => {
                WebViewDOMContentLoaded?.Invoke(s, e);

                var coreWebView = (CoreWebView2)s;
                var favIconUrl = string.Empty;
                if (coreWebView != null)
                {
                    favIconUrl = await coreWebView.ExecuteScriptAsync("document.querySelector(\"link[rel~= 'icon']\").href");
                }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                if (!String.IsNullOrEmpty(favIconUrl))
                {
                    UpdateFormIcon(favIconUrl);
                }
#pragma warning restore CS4014
            };
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Workaround for bug in earlier WebView2 version where webViewControl.Focus() was not working correctly
            var child = GetWindow(webView.Handle, GW_CHILD);
            SetFocus(child);
        }

        private void UpdateFormTitle(string text, string url)
        {
            var title = isTitleGiven ? text : url.ToLower();
            this.Text = title;
        }

        private async Task UpdateFormIcon(string favIconUrl)
        {
            var iconUrl = favIconUrl.Replace("\"", "");
            try
            {
                using var httpClient = new HttpClient();
                byte[] imageBytes = await httpClient.GetByteArrayAsync(iconUrl);
                if (null != imageBytes && imageBytes.Length > 0)
                {
                    try
                    {
                        await using (var ms = new MemoryStream(imageBytes))
                        {
                            this.Icon = new Icon(ms);
                        }
                    }
                    catch (Exception)
                    {
                        await using (var ms = new MemoryStream(imageBytes))
                        {
                            var bitmap = new Bitmap(ms);
                            var iconHandle = bitmap.GetHicon();
                            this.Icon = Icon.FromHandle(iconHandle);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Trace.TraceWarning("Favicon could not be retrieved");
            }
        }

        private void NavigateToUrl(string url)
        {
            webView.CoreWebView2.Navigate(url);
        }

        // Interop
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        
        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);
        
        public const uint GW_CHILD = 5;
    }
}
