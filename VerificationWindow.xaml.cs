using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace JeffView;

public partial class VerificationWindow : Window
{
    private string _userDataFolder;
    private string? _targetUrl;
    public byte[]? DownloadedPdfBytes { get; private set; }

    public VerificationWindow(string userDataFolder, string? targetUrl = null)
    {
        _userDataFolder = userDataFolder;
        _targetUrl = targetUrl;
        InitializeComponent();
        InitializeBrowser();
    }

    private async void InitializeBrowser()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, _userDataFolder);
            await webView.EnsureCoreWebView2Async(env);
            
            string startUrl = _targetUrl ?? "https://www.justice.gov/epstein";
            webView.CoreWebView2.Navigate(startUrl);
            
            webView.NavigationCompleted += WebView_NavigationCompleted;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to initialize verification browser: " + ex.Message);
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;

        string currentUrl = webView.Source.ToString();

        if (currentUrl.Contains("age-verification"))
        {
            string script = "(function() {\r\n" +
                            "    const buttons = Array.from(document.querySelectorAll('input[type=\"submit\"], button'));\r\n" +
                            "    const yesButton = buttons.find(b => (b.value || b.textContent || '').trim().toLowerCase() === 'yes');\r\n" +
                            "    if (yesButton) {\r\n" +
                            "        yesButton.click();\r\n" +
                            "        return 'clicked';\r\n" +
                            "    }\r\n" +
                            "    return 'not_found';\r\n" +
                            "})();";
            await webView.ExecuteScriptAsync(script);
        }
        else if (_targetUrl != null && (currentUrl.EndsWith(".pdf") || !currentUrl.Contains("age-verification")))
        {
            await TryDownloadPdf();
        }
    }

    private async Task TryDownloadPdf()
    {
        if (_targetUrl == null) return;

        string script = "async function fetchPdf(url) {\r\n" +
                        "    try {\r\n" +
                        "        const response = await fetch(url);\r\n" +
                        "        if (!response.ok) return 'error:' + response.status;\r\n" +
                        "        const contentType = response.headers.get('content-type');\r\n" +
                        "        if (contentType && contentType.includes('text/html')) return 'is_html';\r\n" +
                        "        const blob = await response.blob();\r\n" +
                        "        return new Promise((resolve) => {\r\n" +
                        "            const reader = new FileReader();\r\n" +
                        "            reader.onloadend = () => resolve(reader.result.split(',')[1]);\r\n" +
                        "            reader.readAsDataURL(blob);\r\n" +
                        "        });\r\n" +
                        "    } catch (e) {\r\n" +
                        "        return 'error:' + e.message;\r\n" +
                        "    }\r\n" +
                        "}\r\n" +
                        "fetchPdf('" + _targetUrl + "');";

        try
        {
            string result = await webView.ExecuteScriptAsync(script);
            result = result.Trim('"');

            if (result == "is_html")
            {
                return;
            }

            if (result.StartsWith("error:"))
            {
                return;
            }

            if (!string.IsNullOrEmpty(result) && result != "null")
            {
                DownloadedPdfBytes = Convert.FromBase64String(result);
                DialogResult = true;
                Close();
            }
        }
        catch { }
    }

    private async void Done_Click(object sender, RoutedEventArgs e)
    {
        if (_targetUrl != null)
        {
            await TryDownloadPdf();
        }
        else
        {
            DialogResult = true;
            Close();
        }
    }
}
