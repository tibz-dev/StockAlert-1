using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using StockAlert.Desktop.Wpf.Models;
using StockAlert.Desktop.Wpf.Services;

namespace StockAlert.Desktop.Wpf;

public partial class MainWindow : Window
{
    private readonly ProductApiClient _client = new();
    private IReadOnlyList<ProductRow> _products = Array.Empty<ProductRow>();

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _client.Dispose();
    }

    private async void LoadProducts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Loading products...";
            _products = await _client.GetProductsAsync(ApiUrlBox.Text, TokenBox.Password);
            ApplySearch();
            StatusText.Text = $"Loaded {_products.Count} products.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not load products.";
            MessageBox.Show(ex.Message, "StockAlert API error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplySearch();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        ApplySearch();
        if (ProductsGrid.Items.Count == 1)
            ProductsGrid.SelectedIndex = 0;
    }

    private void ApplySearch()
    {
        if (ProductsGrid is null || SearchBox is null) return;
        var query = SearchBox.Text.Trim();
        ProductsGrid.ItemsSource = _products.Where(p =>
            query.Length == 0 ||
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (p.Barcode?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToArray();
    }

    private async void PrintLabel_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductRow product)
        {
            MessageBox.Show("Select a product first.", "Barcode label");
            return;
        }
        try
        {
            var svg = await _client.GetBarcodeSvgAsync(ApiUrlBox.Text, TokenBox.Password, product.Id);
            var path = Path.Combine(Path.GetTempPath(), $"StockAlert-label-{Guid.NewGuid():N}.svg");
            await File.WriteAllTextAsync(path, svg);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            StatusText.Text = "Label opened for preview/printing using the default SVG application.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Label generation failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
