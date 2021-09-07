using Microsoft.Web.WebView2.Core;
using Spectre.Console;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WebPageHost
{
    public partial class MainForm : Form
    {
        private bool initialized = false;

        private string url;
        public string Url
        {
            get { return url; }
            set { 
                url = value; 
                if (this.initialized)
                {
                    NavigateToUrl(this.Url);
                }
            }
        }

        private string title;
        public string Title
        {
            get { return title; }
            set { title = value; }
        }


        public MainForm(string url, string title = "")
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL must not be empty");
            }
            this.Url = url;
            this.Title = title;
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            this.initialized = true;
            this.webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            NavigateToUrl(this.Url);
            UpdateFormTitle(this.Title, this.Url);
            UpdateFormIcon(this.Url);
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested; ;
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.NewWindow = webView.CoreWebView2;
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            String uri = webView.Source.AbsoluteUri;
            this.url = uri; // update field directly not using setter to avoid navigation
        }

        private async void UpdateFormTitle(string text, string url)
        {
            bool isTitleGiven = !String.IsNullOrWhiteSpace(text);
            var title = isTitleGiven ? text : url.ToLower();
            this.Text = title;

            if (!isTitleGiven)
            {
                // Try to get web page title from url
                var x = new WebClient();
                var source = await x.DownloadStringTaskAsync(url);
                title = Regex.Match(source, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                if (!String.IsNullOrWhiteSpace(title))
                {
                    this.Text = title;
                }
            }
        }

        private async void UpdateFormIcon(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var favIconUrl = url.TrimEnd('/') + "/favicon.ico";
                byte[] imageBytes = await httpClient.GetByteArrayAsync(favIconUrl);
                if (null != imageBytes && imageBytes.Length > 0)
                {
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        this.Icon = new Icon(ms);
                    }
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[yellow]Favicon could not be retrieved.[/]");
            }
        }

        private void NavigateToUrl(string url)
        {
            webView.CoreWebView2.Navigate(url);
        }
    }
}
