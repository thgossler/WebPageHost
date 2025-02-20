// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable disable warnings

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;

namespace WebPageHost;

public partial class MainForm : Form
{
    // Custom form events
    public EventHandler FormLoaded;

    // WebView2
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public WebView2 WebView { get; private set; }
    private Label cover;
    private bool isWebViewInitialized;
    private bool isWebPageColorModeInitialized = true;
    private static string UserDataFolderName;
    public EventHandler WebViewDOMContentLoaded;

    // Properties
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title { get; set; }

    private string buttonTitle;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ButtonTitle {
        get => buttonTitle;
        set {
            buttonTitle = value;
            bool showButton = !string.IsNullOrWhiteSpace(buttonTitle);
            button.Visible = showButton;
            if (showButton) {
                button.Text = buttonTitle;
                button.AutoSize = buttonTitle.Length > 20;
                UpdateButtonPlacement();
            }
        }
    }

    public static string UserDataFolderPath
    {
        get {
            var appDataBasePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var baseFolderPath = Path.Combine(appDataBasePath, Common.ProgramName);
            var userDataFolderPath = Path.Combine(baseFolderPath, UserDataFolderName);
            return userDataFolderPath;
        }
    }

    private void UpdateButtonPlacement()
    {
        if (!string.IsNullOrWhiteSpace(buttonTitle)) {
            button.Left = (Width - button.Width) / 2;
            button.Top = ClientSize.Height - button.Height - (button.Height / 3);
            if (WebView != null) {
                WebView.Height = ClientSize.Height - button.Height - (button.Height * 2 / 3);
            }
        }
        else {
            if (WebView != null) {
                WebView.Height = ClientSize.Height;
            }
        }
    }

    private readonly bool isTitleGiven;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DisableSso { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SuppressCertErrors { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ForceDarkMode { get; set; }

    // MainForm
    public MainForm(string userDataFolderName, string url, string title = "", bool disableSso = false, bool suppressCertErrors = false, bool forceDarkMode = false)
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
        ForceDarkMode = forceDarkMode;
        SuppressCertErrors = suppressCertErrors;
        UserDataFolderName = userDataFolderName;

        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        // Initialize WebView2
        var forceDarkMode = Application.IsDarkModeEnabled && ForceDarkMode;
        var options = new CoreWebView2EnvironmentOptions(forceDarkMode ? "--enable-features=WebContentsForceDark" : "") {
            AllowSingleSignOnUsingOSPrimaryAccount = !DisableSso
        };
        CoreWebView2Environment webViewEnv = await CoreWebView2Environment.CreateAsync(null, UserDataFolderPath, options);

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
        WebView.DefaultBackgroundColor = Application.IsDarkModeEnabled ? System.Drawing.Color.DarkGray : System.Drawing.Color.White;
        WebView.ZoomFactor = 1D;
        WebView.Visible = true;
        ((System.ComponentModel.ISupportInitialize)WebView).EndInit();
        ResumeLayout(true);

        WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        await WebView.EnsureCoreWebView2Async(webViewEnv);

        WebView.NavigationCompleted += WebView_NavigationCompleted;

        // Display a loading label with centered text indicating progress
        cover = new Label {
            Dock = DockStyle.Fill,
            BackColor = Application.IsDarkModeEnabled ? SystemColors.Control : WebView.DefaultBackgroundColor,
            Text = "...",
            Font = new Font("Segoe UI", 48, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
        };
        Controls.Add(cover);
        cover.BringToFront();

        // Initialize web content
        isWebViewInitialized = true;
        NavigateToUrl(Url);
        UpdateFormTitle(Title, Url);
        UpdateButtonPlacement();

        // Raise form loaded event
        FormLoaded?.Invoke(this, new EventArgs());
    }

    private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (SuppressCertErrors) {
            WebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
        }

        if (Application.IsDarkModeEnabled) {
            WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            isWebPageColorModeInitialized = false;
        }

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
                favIconUrl = await GetFavIconUrlWithRetry(coreWebView);
            }
            if (!string.IsNullOrEmpty(favIconUrl)) {
                _ = UpdateFormIcon(favIconUrl);
            }
        };
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!isWebPageColorModeInitialized) {
            isWebPageColorModeInitialized = true;
            // Workaround for bug in WebView2 where setting PreferredColorScheme to Dark does not work on first load
            WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            WebView.CoreWebView2.Reload();
            return;
        }

        cover.Visible = false;
        cover.Dispose();
        cover = null;

        // Workaround for bug in earlier WebView2 version where webViewControl.Focus() was not working correctly
        IntPtr child = Win32Interop.GetWindow(WebView.Handle, Win32Interop.GW_CHILD);
        _ = Win32Interop.SetFocus(child);
    }

    private async Task<string> GetFavIconUrlWithRetry(CoreWebView2 coreWebView, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        string favIconUrl = string.Empty;
        for (int attempt = 0; attempt < maxRetries; attempt++) {
            try {
                favIconUrl = await coreWebView.ExecuteScriptAsync("document.querySelector(\"link[rel~= 'icon']\").href");
                favIconUrl = favIconUrl.Replace("null", "");

                if (!string.IsNullOrEmpty(favIconUrl)) {
                    return favIconUrl;
                }

                favIconUrl = coreWebView.FaviconUri;
                if (!string.IsNullOrEmpty(favIconUrl)) {
                    return favIconUrl;
                }
            }
            catch (Exception) {
                // Log the exception if needed
            }

            await Task.Delay(delayMilliseconds);
        }

        return favIconUrl;
    }

    private void UpdateFormTitle(string text, string url)
    {
        string title = isTitleGiven ? text : url.ToLower();
        Text = title;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private const int WM_SETICON = 0x80;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;

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
                // Explicitly update taskbar icons after changing the form icon.
                SendMessage(this.Handle, WM_SETICON, new IntPtr(ICON_SMALL), Icon.Handle);
                SendMessage(this.Handle, WM_SETICON, new IntPtr(ICON_BIG), Icon.Handle);
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

    private void button_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void MainForm_Resize(object sender, EventArgs e)
    {
        UpdateButtonPlacement();
    }
}
