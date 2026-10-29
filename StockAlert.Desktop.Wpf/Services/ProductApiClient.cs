using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using StockAlert.Desktop.Wpf.Models;

namespace StockAlert.Desktop.Wpf.Services;

public sealed class ProductApiClient : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public async Task<StaffSession> GetSessionAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/Auth/me"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StaffSession>()
            ?? throw new InvalidOperationException("Could not retrieve the authenticated staff profile.");
    }

    public async Task<IReadOnlyList<ProductRow>> GetProductsAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/Products"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProductRow>>() ?? new List<ProductRow>();
    }

    public async Task<IReadOnlyList<SupplierRow>> GetSuppliersAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), "api/Suppliers"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SupplierRow>>()
            ?? new List<SupplierRow>();
    }

    public async Task<IReadOnlyList<CustomerRow>> GetCustomersAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), "api/Customers"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CustomerRow>>()
            ?? new List<CustomerRow>();
    }

    public async Task<IReadOnlyList<StockMovementRow>> GetStockMovementsAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), "api/stock-movements?take=100"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StockMovementRow>>()
            ?? new List<StockMovementRow>();
    }

    public async Task<IReadOnlyList<StocktakeSessionRow>> GetStocktakesAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), "api/stocktakes"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StocktakeSessionRow>>()
            ?? new List<StocktakeSessionRow>();
    }

    public async Task<StocktakeDetail> GetStocktakeAsync(string apiUrl, string jwt, Guid id)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), $"api/stocktakes/{id}"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StocktakeDetail>()
            ?? throw new InvalidOperationException("Empty stocktake response.");
    }

    public async Task SubmitStocktakeCountAsync(string apiUrl, string jwt,
        Guid sessionId, Guid itemId, int quantity, bool recount)
    {
        Configure(apiUrl, jwt);
        var action = recount ? "recount" : "count";
        using var response = await _http.PostAsJsonAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"),
            $"api/stocktakes/{sessionId}/items/{itemId}/{action}"),
            new { quantity });
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }

    public async Task CreateStocktakeAsync(string apiUrl, string jwt, string title, string? location, string? zone)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.PostAsJsonAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/stocktakes"),
            new { title, location, zone, source = "StockAlert", blindCount = true });
        await EnsureSuccessAsync(response);
    }

    public async Task ResolveStocktakeItemAsync(string apiUrl, string jwt,
        Guid sessionId, Guid itemId, int finalQuantity, string reason)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.PostAsJsonAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"),
                $"api/stocktakes/{sessionId}/items/{itemId}/resolve"),
            new { finalQuantity, reason });
        await EnsureSuccessAsync(response);
    }

    public async Task CompleteStocktakeAsync(string apiUrl, string jwt, Guid sessionId)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.PostAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), $"api/stocktakes/{sessionId}/complete"), null);
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }

    public async Task CreateQuoteAsync(string apiUrl, string jwt, string customerName,
        Guid? customerId, IReadOnlyList<(Guid ProductId, int Quantity)> items, string? notes)
    {
        Configure(apiUrl, jwt);
        var payload = new
        {
            customerId,
            customerName,
            customerCompanyName = (string?)null,
            customerEmail = (string?)null,
            customerPhoneNumber = (string?)null,
            customerWhatsAppNumber = (string?)null,
            customerHasWhatsApp = false,
            customerAddress = (string?)null,
            salespersonId = (Guid?)null,
            validUntil = (DateTime?)null,
            notes,
            depositPercentage = (decimal?)null,
            depositRequired = (decimal?)null,
            items = items.Select(item => new
            {
                productId = item.ProductId,
                quantity = item.Quantity,
                unitPrice = (decimal?)null
            }).ToArray()
        };
        using var response = await _http.PostAsJsonAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/Quotes"), payload);
        await EnsureSuccessAsync(response);
    }

    public async Task UpdateQuoteStatusAsync(string apiUrl, string jwt, Guid id, string status)
    {
        Configure(apiUrl, jwt);
        using var request = new HttpRequestMessage(HttpMethod.Patch,
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), $"api/Quotes/{id}/status"))
        {
            Content = JsonContent.Create(new { status })
        };
        using var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    public async Task ConvertQuoteToSaleAsync(string apiUrl, string jwt, Guid id)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.PostAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), $"api/Quotes/{id}/convert-to-sale"), null);
        await EnsureSuccessAsync(response);
    }

    public async Task RecordQuotePaymentAsync(string apiUrl, string jwt, Guid id,
        decimal amount, string method, string? reference)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.PostAsJsonAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), $"api/Quotes/{id}/payments"),
            new { amount, method, reference });
        await EnsureSuccessAsync(response);
    }

    public async Task<IReadOnlyList<QuoteRow>> GetQuotesAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), "api/Quotes"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<QuoteRow>>() ?? new List<QuoteRow>();
    }

    public async Task<CheckoutPreview> PreviewCheckoutAsync(string apiUrl, string jwt,
        IReadOnlyList<CheckoutCartLine> cart)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.PostAsJsonAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/checkout/preview"),
            new { items = cart.Select(x => new { productId = x.ProductId, quantity = x.Quantity }).ToArray() });
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CheckoutPreview>()
            ?? throw new InvalidOperationException("Empty checkout preview response.");
    }

    public async Task<CheckoutReceipt> CommitCashCheckoutAsync(string apiUrl, string jwt,
        IReadOnlyList<CheckoutCartLine> cart, Guid operationId, decimal expectedTotal)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.PostAsJsonAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/checkout/cash"),
            new { clientOperationId = operationId,
                  items = cart.Select(x => new { productId = x.ProductId, quantity = x.Quantity }).ToArray(),
                  cashReceived = true, expectedTotal });
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CheckoutReceipt>()
            ?? throw new InvalidOperationException("Empty checkout receipt response.");
    }

    public async Task<SaleReceiptResult> RecordSaleAsync(string apiUrl, string jwt,
        Guid productId, int quantity, string? customerName)
    {
        Configure(apiUrl, jwt);
        // One product per API sale; do not fake a multi-line atomic checkout.
        using var response = await _http.PostAsJsonAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/Sales"),
            new { productId, quantity, customerName,
                  clientOperationId = Guid.NewGuid() });
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<SaleReceiptResult>()
            ?? throw new InvalidOperationException("Sale created without a receipt response.");
    }

    public async Task<IReadOnlyList<SaleRow>> GetSalesAsync(string apiUrl, string jwt)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(
            new Uri(apiUrl.TrimEnd('/') + "/"), "api/Sales"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SaleRow>>() ?? new List<SaleRow>();
    }

    public async Task<string> GetBarcodeSvgAsync(string apiUrl, string jwt, Guid id)
    {
        Configure(apiUrl, jwt);
        using var response = await _http.GetAsync(new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), $"api/Products/{id}/barcode-label"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private void Configure(string apiUrl, string jwt)
    {
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps &&
                !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new ArgumentException("Use an HTTPS API address (HTTP allowed only for localhost).");

        if (string.IsNullOrWhiteSpace(jwt))
            throw new ArgumentException("An authenticated API token is required.");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt.Trim());
    }


    public async Task<string> LoginAsync(string apiUrl, string email, string password)
    {
        ConfigureAddress(apiUrl);
        using var response = await _http.PostAsJsonAsync(
            new Uri(new Uri(apiUrl.TrimEnd('/') + "/"), "api/Auth/login"),
            new { email, password });
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (!response.IsSuccessStatusCode || result?.Success != true ||
            string.IsNullOrWhiteSpace(result.Token))
            throw new InvalidOperationException(result?.Message ?? "Login failed.");
        return result.Token;
    }

    public async Task SaveProductAsync(string apiUrl, string jwt, ProductRow product, bool isNew)
    {
        Configure(apiUrl, jwt);
        var request = new
        {
            product.Name,
            product.Description,
            product.Barcode,
            product.Price,
            StockQuantity = product.StockQuantity,
            product.CategoryName,
            product.SupplierName,
            product.SupplierEmail
        };
        var address = new Uri(new Uri(apiUrl.TrimEnd('/') + "/"),
            isNew ? "api/Products" : $"api/Products/{product.Id}");
        using var response = isNew
            ? await _http.PostAsJsonAsync(address, request)
            : await _http.PutAsJsonAsync(address, request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }

    private sealed class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
    }

    private static void ConfigureAddress(string apiUrl)
    {
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new ArgumentException("Use HTTPS (HTTP allowed only on localhost).");
    }

    public void Dispose() => _http.Dispose();
}
