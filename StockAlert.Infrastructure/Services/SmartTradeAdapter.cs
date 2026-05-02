using System.Net.Http.Json;
using StockAlert.Application.Interfaces;
using StockAlert.Application.DTOs;

namespace StockAlert.Infrastructure.Services;

public class SmartTradeAdapter : IExternalStockService
{
    private readonly HttpClient _httpClient;

    public SmartTradeAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // You would set the BaseAddress to SmartTrade's API URL
    }

    public async Task<IEnumerable<ProductDto>> SyncFromExternalAsync()
    {
        // In a real MVP, you'd fetch this from SmartTrade's API
        // var externalData = await _httpClient.GetFromJsonAsync<List<SmartTradeItem>>("v1/stock");

        // For now, return an empty list to satisfy the interface while we fix the build
        return Enumerable.Empty<ProductDto>();
    }

    public async Task<bool> PushSaleToExternalAsync(SaleDto sale)
    {
        // Example logic: Tell SmartTrade a sale happened here
        var response = await _httpClient.PostAsJsonAsync("api/sales/sync", sale);
        return response.IsSuccessStatusCode;
    }
}