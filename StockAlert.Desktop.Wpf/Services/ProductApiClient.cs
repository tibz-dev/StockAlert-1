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

    public void Dispose() => _http.Dispose();
}
