#nullable disable warnings

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WebPageHost;

public partial class MainForm : Form
{
    // Custom form events
    public EventHandler FormLoaded;

    // WebView2
    public WebView2 WebView { get; private set; }
    private bool isWebViewInitialized;
    private readonly string userDataFolderName;
    public EventHandler WebViewDOMContentLoaded;

    // Properties
    public string Url {
        get => url;
        set {
            url = value;
            if (isWebViewInitialized) {
                NavigateToUrl(Url);
            }
        }
    }
    private string url;

    public string Title { get; set; }

    private readonly bool isTitleGiven;

    public bool DisableSso { get; set; }

    // MainForm
    public MainForm(string userDataFolderName, string url, string title = "", bool disableSso = false)
    {
        if (string.IsNullOrWhiteSpace(userDataFolderName)) {
            throw new ArgumentException("Parameter 'userDataFolderName' is required");
        }
        if (string.IsNullOrWhiteSpace(url)) {
            throw new ArgumentException("Parameter 'url' is required");
        }

        Url = url;
        Title = title;
        isTitleGiven = !string.IsNullOrWhiteSpace(title);
        DisableSso = disableSso;
        this.userDataFolderName = userDataFolderName;

        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        // Initialize WebView2
        CoreWebView2Environment webViewEnv = await CoreWebView2Environment.CreateAsync(null, userDataFolderName,
            new CoreWebView2EnvironmentOptions() { AllowSingleSignOnUsingOSPrimaryAccount = !DisableSso });
        WebView = new WebView2();
        SuspendLayout();
        Controls.Add(WebView);
        ((System.ComponentModel.ISupportInitialize)WebView).BeginInit();
        WebView.Parent = this;
        WebView.Name = "webView";
        WebView.CreationProperties = null;
        int padding = 0;
        WebView.Bounds = new Rectangle(padding, padding, ClientSize.Width - (2 * padding), ClientSize.Height - (2 * padding));
        WebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        WebView.DefaultBackgroundColor = System.Drawing.Color.White;
        WebView.ZoomFactor = 1D;
        WebView.Visible = true;
        ((System.ComponentModel.ISupportInitialize)WebView).EndInit();
        ResumeLayout(true);
        WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        await WebView.EnsureCoreWebView2Async(webViewEnv);

        WebView.NavigationCompleted += WebView_NavigationCompleted;

        // Initialize web content
        isWebViewInitialized = true;
        NavigateToUrl(Url);
        UpdateFormTitle(Title, Url);

        // Raise form loaded event
        FormLoaded?.Invoke(this, new EventArgs());
    }

    private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        WebView.CoreWebView2.NewWindowRequested += (s, e) => e.NewWindow = WebView.CoreWebView2;
        WebView.CoreWebView2.SourceChanged += (s, e) => url = WebView.Source.AbsoluteUri; // update field directly not using setter to avoid navigation
        WebView.CoreWebView2.DocumentTitleChanged += (s, e) => {
            if (!isTitleGiven) {
                Text = WebView.CoreWebView2.DocumentTitle;
            }
        };
        WebView.CoreWebView2.DOMContentLoaded += async (s, e) => {
            WebViewDOMContentLoaded?.Invoke(s, e);

            var coreWebView = (CoreWebView2)s;
            string favIconUrl = string.Empty;
            if (coreWebView != null) {
                favIconUrl = await coreWebView.ExecuteScriptAsync("document.querySelector(\"link[rel~= 'icon']\").href");
            }

            if (!string.IsNullOrEmpty(favIconUrl)) {
                _ = UpdateFormIcon(favIconUrl);
            }
        };
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Workaround for bug in earlier WebView2 version where webViewControl.Focus() was not working correctly
        IntPtr child = Win32Interop.GetWindow(WebView.Handle, Win32Interop.GW_CHILD);
        _ = Win32Interop.SetFocus(child);
    }

    private void UpdateFormTitle(string text, string url)
    {
        string title = isTitleGiven ? text : url.ToLower();
        Text = title;
    }

    private async Task UpdateFormIcon(string favIconUrl)
    {
        string iconUrl = favIconUrl.Replace("\"", "");
        try {
            using var httpClient = new HttpClient();
            byte[] imageBytes = await httpClient.GetByteArrayAsync(iconUrl);
            if (null != imageBytes && imageBytes.Length > 0) {
                try {
                    await using var ms = new MemoryStream(imageBytes);
                    Icon = new Icon(ms);
                }
                catch (Exception) {
                    await using var ms = new MemoryStream(imageBytes);
                    var bitmap = new Bitmap(ms);
                    IntPtr iconHandle = bitmap.GetHicon();
                    Icon = Icon.FromHandle(iconHandle);
                }
            }
        }
        catch (Exception) {
            Trace.TraceWarning("Favicon could not be retrieved");
        }
    }

    private void NavigateToUrl(string url)
    {
        WebView.CoreWebView2.Navigate(url);
    }
}
