using System.Windows;
using System.Windows.Controls;

namespace JeffView;

public partial class ConversionWindow : Window
{
    public string SelectedFormat { get; private set; } = "Jpeg";
    public int CompressionLevel { get; private set; } = 80;

    public ConversionWindow()
    {
        InitializeComponent();
    }

    private void Convert_Click(object sender, RoutedEventArgs e)
    {
        SelectedFormat = (FormatComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Jpeg";
        CompressionLevel = (int)QualitySlider.Value;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
