using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ImageMagick;
using Microsoft.Web.WebView2.Core;

namespace JeffView;

public partial class MainWindow : Window
{
    private string? _currentRemoteUrl;
    private string _userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JeffView", "BrowserData");

    private class DatasetInfo
    {
        public int Id { get; set; }
        public string Start { get; set; } = "";
        public string End { get; set; } = "";
        public override string ToString() => $"Dataset {Id}";
    }

    private List<DatasetInfo> _datasets = new List<DatasetInfo>
    {
        new DatasetInfo { Id = 1, Start = "00000001", End = "00003158" },
        new DatasetInfo { Id = 2, Start = "00003159", End = "00003379" },
        new DatasetInfo { Id = 3, Start = "00003380", End = "00005704" },
        new DatasetInfo { Id = 4, Start = "00005705", End = "00008408" },
        new DatasetInfo { Id = 5, Start = "00008409", End = "00008528" },
        new DatasetInfo { Id = 6, Start = "00008529", End = "00009015" },
        new DatasetInfo { Id = 7, Start = "00009016", End = "00009675" },
        new DatasetInfo { Id = 8, Start = "00009676", End = "00039024" },
        new DatasetInfo { Id = 9, Start = "00039025", End = "01262781" },
        new DatasetInfo { Id = 10, Start = "01262782", End = "02209721" },
        new DatasetInfo { Id = 11, Start = "02209722", End = "02730264" },
        new DatasetInfo { Id = 12, Start = "02730265", End = "02858497" }
    };

    public MainWindow()
    {
        InitializeComponent();
        DatasetComboBox.ItemsSource = _datasets;
        DatasetComboBox.SelectedIndex = 0;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, _userDataFolder);
            await mainWebView.EnsureCoreWebView2Async(env);
            
            await mainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@" 
                (function() { 
                    const checkAndClick = () => { 
                        const yesButton = document.querySelector('button#age-button-yes'); 
                        if (yesButton) { 
                            yesButton.click(); 
                            console.log('JeffView: Automatically bypassed age verification.'); 
                        } 
                    };
                    checkAndClick();
                    document.addEventListener('DOMContentLoaded', checkAndClick);
                    setInterval(checkAndClick, 1000);
                })();
            ");

            mainWebView.CoreWebView2.NavigationCompleted += (s, args) => { 
                StatusLabel.Text = "Status: " + (args.IsSuccess ? "Page Loaded" : "Navigation Error"); 
            };

            mainWebView.CoreWebView2.Navigate("https://www.justice.gov/epstein/doj-disclosures/data-set-1-files");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Initialization Error: {ex.Message}");
        }
    }

    private void DatasetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DatasetComboBox.SelectedItem is DatasetInfo info)
        {
            RangeLabel.Text = $"Range: {info.Start} - {info.End}";
            RemoteIdBox.Text = info.Start;
        }
    }

    private void VerifyAge_Click(object sender, RoutedEventArgs e)
    {
        mainWebView.CoreWebView2.Navigate("https://www.justice.gov/epstein/doj-disclosures/data-set-1-files");
    }

    private void LoadRemote_Click(object sender, RoutedEventArgs e)
    {
        if (DatasetComboBox.SelectedItem is not DatasetInfo info) return; 
        
        string docId = RemoteIdBox.Text.Trim();
        if (docId.Length < 8) docId = docId.PadLeft(8, '0');

        _currentRemoteUrl = $"https://www.justice.gov/epstein/files/DataSet%20{info.Id}/EFTA{docId}.pdf";
        mainWebView.CoreWebView2.Navigate(_currentRemoteUrl);
        StatusLabel.Text = $"Navigating to EFTA{docId}...";
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
        {
            mainWebView.CoreWebView2.Navigate(new Uri(dialog.FileName).AbsoluteUri);
            StatusLabel.Text = $"Loaded Local: {Path.GetFileName(dialog.FileName)}";
        }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Please use the browser's PDF toolbar (Save icon) to save the document.", "Info");
    }

    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        string currentUrl = mainWebView.Source.ToString();
        if (!currentUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show("Please load a PDF document first.", "Note");
            return;
        }

        var conversionDialog = new ConversionWindow { Owner = this };
        if (conversionDialog.ShowDialog() == true)
        {
            await PerformBrowserConversion(currentUrl, conversionDialog.SelectedFormat, conversionDialog.CompressionLevel);
        }
    }

    private async Task PerformBrowserConversion(string url, string format, int quality)
    {
        StatusLabel.Text = "Capturing session data for conversion...";
        
        string script = "async function getBytes(url) {\r\n" +
                        "    const response = await fetch(url);\r\n" +
                        "    const blob = await response.blob();\r\n" +
                        "    return new Promise((resolve) => {\r\n" +
                        "        const reader = new FileReader();\r\n" +
                        "        reader.onloadend = () => resolve(reader.result.split(',')[1]);\r\n" +
                        "        reader.readAsDataURL(blob);\r\n" +
                        "    });\r\n" +
                        "}" +
                        "getBytes('" + url + "');";

        try
        {
            string base64 = await mainWebView.ExecuteScriptAsync(script);
            base64 = base64.Trim('"');

            if (string.IsNullOrEmpty(base64) || base64 == "null")
            {
                throw new Exception("Browser failed to capture PDF stream. Ensure you are fully verified.");
            }

            byte[] pdfBytes = Convert.FromBase64String(base64);
            string tempFile = Path.Combine(Path.GetTempPath(), "jeffview_convert.pdf");
            await File.WriteAllBytesAsync(tempFile, pdfBytes);

            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = $"{format.ToUpper()} Files (*.{format.ToLower()})|*.{format.ToLower()}" };
            if (dialog.ShowDialog() == true)
            {
                string outputPath = dialog.FileName;
                string extension = Path.GetExtension(outputPath);
                string baseName = outputPath.Substring(0, outputPath.Length - extension.Length);

                StatusLabel.Text = "Processing images...";
                await Task.Run(() => {
                    var settings = new MagickReadSettings { Density = new Density(300) };
                    using (var images = new MagickImageCollection())
                    {
                        images.Read(tempFile, settings);
                        int i = 0;
                        foreach (var image in images)
                        {
                            image.Format = (MagickFormat)Enum.Parse(typeof(MagickFormat), format, true);
                            image.Quality = (uint)quality;
                            image.Write($"{baseName}_page_{i + 1}{extension}");
                            i++;
                        }
                    }
                });
                System.Windows.MessageBox.Show("Conversion complete!");
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Conversion failed: {ex.Message}");
        }
        finally
        {
            StatusLabel.Text = "Ready";
        }
    }

    private void PrevDoc_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(RemoteIdBox.Text.Trim(), out int currentId))
        {
            if (currentId > 1)
            {
                RemoteIdBox.Text = (currentId - 1).ToString("D8");
                LoadRemote_Click(sender, e);
            }
        }
    }

    private void NextDoc_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(RemoteIdBox.Text.Trim(), out int currentId))
        {
            RemoteIdBox.Text = (currentId + 1).ToString("D8");
            LoadRemote_Click(sender, e);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
}
