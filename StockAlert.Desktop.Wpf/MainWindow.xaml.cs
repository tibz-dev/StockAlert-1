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
    private Guid? _selectedStocktakeId;

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
        StockMovementsPage.Visibility = Visibility.Collapsed;
        StocktakesPage.Visibility = Visibility.Collapsed;
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

    private async void StockMovements_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(StockMovementsPage);
        Title = "StockAlert Desktop | Stock Movements";
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in on the Inventory page first.");
            StockMovementsStatus.Text = "Loading stock movements...";
            var movements = await _client.GetStockMovementsAsync(ApiUrlBox.Text, _sessionToken);
            StockMovementsGrid.ItemsSource = movements;
            StockMovementsStatus.Text = $"{movements.Count} movements loaded (latest 100).";
        }
        catch (Exception ex) { StockMovementsStatus.Text = ex.Message; }
    }

    private async void Stocktakes_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(StocktakesPage);
        Title = "StockAlert Desktop | Stocktakes";
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in on the Inventory page first.");
            StocktakesStatus.Text = "Loading stocktakes...";
            var sessions = await _client.GetStocktakesAsync(ApiUrlBox.Text, _sessionToken);
            StocktakesGrid.ItemsSource = sessions;
            StocktakesStatus.Text = $"{sessions.Count} stocktake sessions loaded.";
        }
        catch (Exception ex) { StocktakesStatus.Text = ex.Message; }
    }

    private async void OpenStocktake_Click(object sender, RoutedEventArgs e)
    {
        if (StocktakesGrid.SelectedItem is not StocktakeSessionRow selected)
        {
            StocktakesStatus.Text = "Select a stocktake session first.";
            return;
        }
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in first.");
            _selectedStocktakeId = selected.Id;
            await ReloadStocktakeItemsAsync();
        }
        catch (Exception ex) { StocktakesStatus.Text = ex.Message; }
    }

    private async Task ReloadStocktakeItemsAsync()
    {
        if (!_selectedStocktakeId.HasValue || string.IsNullOrWhiteSpace(_sessionToken))
            throw new InvalidOperationException("Open a stocktake and sign in first.");
        var detail = await _client.GetStocktakeAsync(ApiUrlBox.Text, _sessionToken, _selectedStocktakeId.Value);
        StocktakeItemsGrid.ItemsSource = detail.Items;
        StocktakesStatus.Text = $"Stocktake: {detail.Title} ({detail.Items.Count} items).";
    }

    private async void SubmitStocktakeCount_Click(object sender, RoutedEventArgs e) =>
        await SubmitSelectedCountAsync(false);

    private async void SubmitStocktakeRecount_Click(object sender, RoutedEventArgs e) =>
        await SubmitSelectedCountAsync(true);

    private async Task SubmitSelectedCountAsync(bool recount)
    {
        try
        {
            if (!_selectedStocktakeId.HasValue || string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Open a stocktake and sign in first.");
            if (!int.TryParse(StocktakeQuantityBox.Text, out var quantity) || quantity < 0)
                throw new ArgumentException("Count must be a non-negative whole number.");

            var selectedItem = StocktakeItemsGrid.SelectedItem as StocktakeItemRow;
            var scanned = StocktakeBarcodeBox.Text.Trim();
            var items = StocktakeItemsGrid.ItemsSource as IEnumerable<StocktakeItemRow>;
            StocktakeItemRow? item;
            if (scanned.Length > 0)
            {
                var matches = items?.Where(i => string.Equals(
                    i.Barcode, scanned, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
                if (matches == null || matches.Length != 1)
                    throw new InvalidOperationException("Barcode must match exactly one item in this stocktake.");
                item = matches[0];
            }
            else
            {
                item = selectedItem;
            }

            if (item == null)
                throw new InvalidOperationException("Select a stocktake item or scan its barcode.");

            await _client.SubmitStocktakeCountAsync(ApiUrlBox.Text, _sessionToken,
                _selectedStocktakeId.Value, item.Id, quantity, recount);
            await ReloadStocktakeItemsAsync();
            StocktakesStatus.Text = recount ? "Recount recorded." : "Count recorded.";
            StocktakeBarcodeBox.Clear();
            StocktakeQuantityBox.Clear();
        }
        catch (Exception ex) { StocktakesStatus.Text = ex.Message; }
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
