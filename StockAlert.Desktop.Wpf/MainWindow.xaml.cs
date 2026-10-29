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
    private ProductRow? _posProduct;
    private readonly List<CheckoutCartLine> _cartLines = new();
    private Guid _cartOperationId = Guid.NewGuid();
    private bool _cartPosting;

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
        QuotesPage.Visibility = Visibility.Collapsed;
        SalesPage.Visibility = Visibility.Collapsed;
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

    private async void CreateStocktake_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in first.");
            var title = NewStocktakeTitleBox.Text.Trim();
            if (title.Length == 0)
                throw new ArgumentException("Enter a stocktake title.");
            await _client.CreateStocktakeAsync(ApiUrlBox.Text, _sessionToken,
                title, NewStocktakeLocationBox.Text.Trim(), null);
            NewStocktakeTitleBox.Clear();
            StocktakesStatus.Text = "Stocktake created. Refreshing...";
            Stocktakes_Click(sender, e);
        }
        catch (Exception ex) { StocktakesStatus.Text = ex.Message; }
    }

    private async void ResolveStocktake_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken) || !_selectedStocktakeId.HasValue)
                throw new InvalidOperationException("Sign in and open a stocktake first.");
            if (StocktakeItemsGrid.SelectedItem is not StocktakeItemRow item)
                throw new InvalidOperationException("Select an item to resolve.");
            if (!int.TryParse(StocktakeQuantityBox.Text, out var quantity) || quantity < 0)
                throw new ArgumentException("Enter a non-negative final quantity.");
            var reason = StocktakeResolutionReasonBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Provide a reason for resolution.");
            await _client.ResolveStocktakeItemAsync(ApiUrlBox.Text, _sessionToken,
                _selectedStocktakeId.Value, item.Id, quantity, reason);
            await ReloadStocktakeItemsAsync();
            StocktakesStatus.Text = "Item resolution saved.";
        }
        catch (Exception ex) { StocktakesStatus.Text = ex.Message; }
    }

    private async void CompleteStocktake_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken) || !_selectedStocktakeId.HasValue)
                throw new InvalidOperationException("Sign in and open a stocktake first.");
            if (MessageBox.Show("Complete this stocktake? This action may finalise inventory adjustments.",
                "Confirm completion", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            await _client.CompleteStocktakeAsync(ApiUrlBox.Text, _sessionToken,
                _selectedStocktakeId.Value);
            StocktakesStatus.Text = "Stocktake completed.";
            _selectedStocktakeId = null;
            StocktakeItemsGrid.ItemsSource = null;
            Stocktakes_Click(sender, e);
        }
        catch (Exception ex) { StocktakesStatus.Text = ex.Message; }
    }

    private async void CreateQuote_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in first.");
            // Retrieve fresh products before building the quote.
            _products = await _client.GetProductsAsync(ApiUrlBox.Text, _sessionToken);
            var editor = new QuoteEditorWindow(_products) { Owner = this };
            if (editor.ShowDialog() != true) return;
            await _client.CreateQuoteAsync(ApiUrlBox.Text, _sessionToken,
                editor.CustomerName, null, editor.Items, editor.Notes);
            await RefreshQuotesAsync();
            QuotesStatus.Text = "Quote created successfully.";
        }
        catch (Exception ex) { QuotesStatus.Text = ex.Message; }
    }

    private QuoteRow SelectedQuote() =>
        QuotesGrid.SelectedItem as QuoteRow
        ?? throw new InvalidOperationException("Select a quote first.");

    private string AuthorizedToken() => _sessionToken
        ?? throw new InvalidOperationException("Sign in first.");

    private async Task RefreshQuotesAsync()
    {
        var quotes = await _client.GetQuotesAsync(ApiUrlBox.Text, AuthorizedToken());
        QuotesGrid.ItemsSource = quotes;
        QuotesStatus.Text = $"{quotes.Count} quotes loaded.";
    }

    private async void UpdateQuoteStatus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var quote = SelectedQuote();
            var status = (QuoteStatusCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString()
                ?? throw new InvalidOperationException("Select a status.");
            if (MessageBox.Show($"Set quote {quote.QuoteNumber} to {status}?",
                "Confirm status change", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            await _client.UpdateQuoteStatusAsync(ApiUrlBox.Text, AuthorizedToken(), quote.Id, status);
            await RefreshQuotesAsync();
            QuotesStatus.Text = $"Quote changed to {status}.";
        }
        catch (Exception ex) { QuotesStatus.Text = ex.Message; }
    }

    private async void ConvertQuote_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var quote = SelectedQuote();
            if (MessageBox.Show($"Convert quote {quote.QuoteNumber} to a sale? This may deduct stock.",
                "Confirm conversion", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            await _client.ConvertQuoteToSaleAsync(ApiUrlBox.Text, AuthorizedToken(), quote.Id);
            await RefreshQuotesAsync();
            QuotesStatus.Text = "Quote converted to sale. Refresh Sales to see the transaction.";
        }
        catch (Exception ex) { QuotesStatus.Text = ex.Message; }
    }

    private async void RecordQuotePayment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var quote = SelectedQuote();
            if (!decimal.TryParse(QuotePaymentAmountBox.Text, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.GetCultureInfo("en-ZA"), out var amount) || amount <= 0)
                throw new ArgumentException("Enter a valid positive payment amount.");
            if (amount > quote.BalanceDue)
                throw new ArgumentException("Payment cannot exceed the quote's current balance.");
            var method = (QuotePaymentMethodCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString()
                ?? throw new InvalidOperationException("Select a payment method.");
            var reference = QuotePaymentReferenceBox.Text.Trim();
            if (MessageBox.Show($"Record a {method} payment of R {amount:N2} for quote {quote.QuoteNumber}? Confirm funds were actually received.",
                "Confirm received payment", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            await _client.RecordQuotePaymentAsync(ApiUrlBox.Text, AuthorizedToken(),
                quote.Id, amount, method, string.IsNullOrWhiteSpace(reference) ? null : reference);
            await RefreshQuotesAsync();
            QuotePaymentAmountBox.Clear();
            QuotePaymentReferenceBox.Clear();
            QuotesStatus.Text = "Payment recorded.";
        }
        catch (Exception ex) { QuotesStatus.Text = ex.Message; }
    }

    private async void Quotes_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(QuotesPage);
        Title = "StockAlert Desktop | Quotes";
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in on the Inventory page first.");
            QuotesStatus.Text = "Loading quotes...";
            var quotes = await _client.GetQuotesAsync(ApiUrlBox.Text, _sessionToken);
            QuotesGrid.ItemsSource = quotes;
            QuotesStatus.Text = $"{quotes.Count} quotes loaded.";
        }
        catch (Exception ex) { QuotesStatus.Text = ex.Message; }
    }

    private void RenderCart()
    {
        CartGrid.ItemsSource = null;
        CartGrid.ItemsSource = _cartLines.ToArray();
        var estimate = _cartLines.Sum(x => x.LineTotal);
        CartStatus.Text = $"{_cartLines.Sum(x => (long)x.Quantity)} units · estimated subtotal R {estimate:N2}. Preview required before checkout.";
    }

    private void CartAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_cartPosting) return;
            var barcode = CartBarcodeBox.Text.Trim();
            if (string.IsNullOrEmpty(barcode))
                throw new ArgumentException("Scan or enter a barcode.");
            if (!int.TryParse(CartQuantityBox.Text, out var quantity) || quantity <= 0)
                throw new ArgumentException("Quantity must be a positive whole number.");
            var matches = _products.Where(x => string.Equals(x.Barcode, barcode,
                StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Barcode must match exactly one loaded product. Sign in or refresh inventory.");
            var product = matches[0];
            var existing = _cartLines.FirstOrDefault(x => x.ProductId == product.Id);
            int combined = checked((existing?.Quantity ?? 0) + quantity);
            if (combined > product.AvailableQuantity)
                throw new InvalidOperationException($"Only {product.AvailableQuantity} unreserved units available.");
            if (existing != null) existing.Quantity = combined;
            else _cartLines.Add(new CheckoutCartLine
            {
                ProductId = product.Id, ProductName = product.Name,
                Barcode = product.Barcode, Quantity = quantity, UnitPrice = product.Price
            });
            _cartOperationId = Guid.NewGuid();
            CartBarcodeBox.Clear();
            CartQuantityBox.Text = "1";
            RenderCart();
            CartBarcodeBox.Focus();
        }
        catch (Exception ex) { CartStatus.Text = ex.Message; }
    }

    private void CartRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_cartPosting) return;
        if (CartGrid.SelectedItem is CheckoutCartLine selected)
        {
            _cartLines.Remove(selected);
            _cartOperationId = Guid.NewGuid();
            RenderCart();
        }
    }

    private void CartClear_Click(object sender, RoutedEventArgs e)
    {
        if (_cartPosting) return;
        _cartLines.Clear();
        _cartOperationId = Guid.NewGuid();
        RenderCart();
    }

    private async void CartPreview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_cartPosting) return;
            if (_cartLines.Count == 0) throw new InvalidOperationException("Add products first.");
            var preview = await _client.PreviewCheckoutAsync(ApiUrlBox.Text, AuthorizedToken(), _cartLines);
            CartStatus.Text = $"Server preview: {preview.TotalUnits} units, subtotal R {preview.Subtotal:N2}. {preview.Note}";
        }
        catch (Exception ex) { CartStatus.Text = ex.Message; }
    }

    private async void CartCheckout_Click(object sender, RoutedEventArgs e)
    {
        if (_cartPosting) return;
        try
        {
            if (_cartLines.Count == 0) throw new InvalidOperationException("Add products first.");
            var token = AuthorizedToken();
            var preview = await _client.PreviewCheckoutAsync(ApiUrlBox.Text, token, _cartLines);
            if (MessageBox.Show(
                $"Server-priced subtotal R {preview.Subtotal:N2} for {preview.TotalUnits} units.\\n\\nHave you physically received the full CASH payment? VAT may affect the final total.",
                "Confirm Cash Received", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            _cartPosting = true;
            var receipt = await _client.CommitCashCheckoutAsync(ApiUrlBox.Text, token,
                _cartLines, _cartOperationId);
            CartStatus.Text = $"Checkout posted: {receipt.ReceiptNumber} · R {receipt.Total:N2} (cash).";
            _cartLines.Clear();
            _cartOperationId = Guid.NewGuid();
            CartGrid.ItemsSource = null;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            CartStatus.Text = $"Checkout not confirmed: {ex.Message} Retry the same cart only after checking whether the sale was posted.";
            // Retain operation ID so the same cart can be retried idempotently.
        }
        finally { _cartPosting = false; }
    }

    private async void PosLookup_Click(object sender, RoutedEventArgs e)
    {
        _posProduct = null;
        try
        {
            var token = AuthorizedToken();
            var barcode = PosBarcodeBox.Text.Trim();
            if (barcode.Length == 0)
                throw new ArgumentException("Scan or enter a barcode.");
            var products = await _client.GetProductsAsync(ApiUrlBox.Text, token);
            var matches = products.Where(p => string.Equals(p.Barcode, barcode,
                StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Barcode must match exactly one active product.");
            _posProduct = matches[0];
            PosPreviewText.Text = $"{_posProduct.Name} | R {_posProduct.Price:N2} each | Available: {_posProduct.AvailableQuantity}";
        }
        catch (Exception ex) { PosPreviewText.Text = ex.Message; }
    }

    private async void PosSale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var token = AuthorizedToken();
            if (_posProduct == null ||
                !string.Equals(_posProduct.Barcode, PosBarcodeBox.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Find the scanned product before recording a sale.");
            if (!int.TryParse(PosQuantityBox.Text, out var quantity) || quantity <= 0)
                throw new ArgumentException("Quantity must be a positive whole number.");
            if (quantity > _posProduct.AvailableQuantity)
                throw new InvalidOperationException("Quantity exceeds currently available stock.");
            var total = _posProduct.Price * quantity;
            var customer = PosCustomerBox.Text.Trim();
            if (MessageBox.Show($"Record {quantity} x {_posProduct.Name} (estimated R {total:N2})?\nThis saves a sale and deducts stock. It does NOT collect or verify payment.",
                "Confirm inventory-impacting sale", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;
            var receipt = await _client.RecordSaleAsync(ApiUrlBox.Text, token,
                _posProduct.Id, quantity, string.IsNullOrEmpty(customer) ? null : customer);
            PosPreviewText.Text = $"Sale recorded: {receipt.ReceiptNumber}, R {receipt.TotalPrice:N2}. Payment not processed.";
            _posProduct = null;
            PosBarcodeBox.Clear();
            PosQuantityBox.Text = "1";
            await RefreshAsync();
            var sales = await _client.GetSalesAsync(ApiUrlBox.Text, token);
            SalesGrid.ItemsSource = sales;
            SalesStatus.Text = $"{sales.Count} sales loaded.";
        }
        catch (Exception ex) { PosPreviewText.Text = ex.Message; }
    }

    private async void Sales_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(SalesPage);
        Title = "StockAlert Desktop | Sales";
        try
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
                throw new InvalidOperationException("Sign in on the Inventory page first.");
            SalesStatus.Text = "Loading sales...";
            var sales = await _client.GetSalesAsync(ApiUrlBox.Text, _sessionToken);
            SalesGrid.ItemsSource = sales;
            SalesStatus.Text = $"{sales.Count} sales loaded.";
        }
        catch (Exception ex) { SalesStatus.Text = ex.Message; }
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
