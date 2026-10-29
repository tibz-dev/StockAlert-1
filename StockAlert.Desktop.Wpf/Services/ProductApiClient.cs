using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using StockAlert.Desktop.Wpf.Models;

namespace StockAlert.Desktop.Wpf.Services;

public sealed class ProductApiClient : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

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
