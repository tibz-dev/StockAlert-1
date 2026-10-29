using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using StockAlert.Desktop.Wpf.Models;
using StockAlert.Desktop.Wpf.Services;
using StockAlert.Desktop.Wpf.Views;

namespace StockAlert.Desktop.Wpf;

public partial class MainWindow : Window
{
    private readonly ProductApiClient _client = new();
    private string? _sessionToken;
    private IReadOnlyList<ProductRow> _products = Array.Empty<ProductRow>();

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _client.Dispose();
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(DashboardPage);
        Title = "StockAlert Desktop | Dashboard";
        UpdateDashboard();
    }

    private void Inventory_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(InventoryPage);
        Title = "StockAlert Desktop | Inventory";
    }

    private void ShowPage(UIElement page)
    {
        DashboardPage.Visibility = Visibility.Collapsed;
        InventoryPage.Visibility = Visibility.Collapsed;
        SuppliersPage.Visibility = Visibility.Collapsed;
        CustomersPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible;
    }

    private async void Suppliers_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(SuppliersPage);
        Title = "StockAlert Desktop | Suppliers";
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in on the Inventory page first.");
            SuppliersStatus.Text = "Loading suppliers...";
            var suppliers = await _client.GetSuppliersAsync(ApiUrlBox.Text, _sessionToken);
            SuppliersGrid.ItemsSource = suppliers;
            SuppliersStatus.Text = $"{suppliers.Count} suppliers loaded.";
        }
        catch (Exception ex)
        {
            SuppliersStatus.Text = ex.Message;
        }
    }

    private async void Customers_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(CustomersPage);
        Title = "StockAlert Desktop | Customers";
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in on the Inventory page first.");
            CustomersStatus.Text = "Loading customers...";
            var customers = await _client.GetCustomersAsync(ApiUrlBox.Text, _sessionToken);
            CustomersGrid.ItemsSource = customers;
            CustomersStatus.Text = $"{customers.Count} customers loaded.";
        }
        catch (Exception ex)
        {
            CustomersStatus.Text = ex.Message;
        }
    }

    private void UpdateDashboard()
    {
        TotalProductsText.Text = _products.Count.ToString("N0");
        LowStockText.Text = _products.Count(p => p.StockQuantity < 5).ToString("N0");
        OnHandText.Text = _products.Sum(p => (long)p.StockQuantity).ToString("N0");
        AvailableText.Text = _products.Sum(p => (long)p.AvailableQuantity).ToString("N0");
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _sessionToken = null;
            _products = Array.Empty<ProductRow>();
            ApplySearch();
            _sessionToken = await _client.LoginAsync(ApiUrlBox.Text, EmailBox.Text, LoginPasswordBox.Password);
            LoginPasswordBox.Clear();
            StatusText.Text = "Signed in. Loading inventory...";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            LoginPasswordBox.Clear();
            StatusText.Text = "Sign-in failed.";
            MessageBox.Show(ex.Message, "Sign In", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(_sessionToken))
            throw new InvalidOperationException("Sign in first.");
        _products = await _client.GetProductsAsync(ApiUrlBox.Text, _sessionToken);
        ApplySearch();
        UpdateDashboard();
        StatusText.Text = $"Loaded {_products.Count} products.";
    }

    private async void AddProduct_Click(object sender, RoutedEventArgs e) =>
        await OpenEditorAsync(null);

    private async void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductRow product)
        {
            MessageBox.Show("Select a product to edit.", "Inventory");
            return;
        }
        await OpenEditorAsync(product);
    }

    private async Task OpenEditorAsync(ProductRow? product)
    {
        if (string.IsNullOrWhiteSpace(_sessionToken))
        {
            MessageBox.Show("Please sign in first.", "Inventory");
            return;
        }
        var editor = new ProductEditorWindow(product) { Owner = this };
        if (editor.ShowDialog() != true || editor.Result == null) return;
        try
        {
            await _client.SaveProductAsync(ApiUrlBox.Text, _sessionToken, editor.Result, product == null);
            await RefreshAsync();
            StatusText.Text = "Product saved successfully.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not save product", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadProducts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Loading products...";
            await RefreshAsync();
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
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in first.");
            var svg = await _client.GetBarcodeSvgAsync(ApiUrlBox.Text, _sessionToken, product.Id);
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
