using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace WebPageHost
{
    public partial class MainForm : Form
    {
        // TODO: support back navigation
        // TODO: copy current full URL (as CLI result?)
        // TODO: support download of files
        // TODO: allow selection of HTML and return as CLI result

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
            this.Url = url;
            this.title = String.IsNullOrWhiteSpace(title) ? url : title;
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            this.initialized = true;
            NavigateToUrl(this.Url);
        }

        private void NavigateToUrl(string url)
        {
            webView.CoreWebView2.Navigate(url);
            // TODO: Update window icon with web page icon
        }
    }
}
