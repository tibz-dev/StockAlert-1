using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.Infrastructure.Services;

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public BarcodeLookupService(
        IApplicationDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<BarcodeLookupResultDto> LookupAsync(string barcode)
    {
        var normalized = Normalize(barcode);

        var local = await _context.Products
            .AsNoTracking()
            .Where(product =>
                !product.IsDeleted
                && product.Barcode == normalized)
            .Select(product => new
            {
                product.Id,
                product.Name
            })
            .FirstOrDefaultAsync();

        if (local != null)
        {
            return new BarcodeLookupResultDto(
                normalized,
                true,
                true,
                local.Id,
                local.Name,
                null,
                null,
                "StockAlert");
        }

        var client = _httpClientFactory.CreateClient();

        try
        {
            var response = await client.GetFromJsonAsync<UpcResponse>(
                $"https://api.upcitemdb.com/prod/trial/lookup?upc={Uri.EscapeDataString(normalized)}");

            var item = response?.Items?.FirstOrDefault();

            if (item != null)
            {
                return new BarcodeLookupResultDto(
                    normalized,
                    true,
                    false,
                    null,
                    item.Title,
                    item.Brand,
                    item.Images?.FirstOrDefault(),
                    "UPCitemdb");
            }
        }
        catch
        {
            // External lookup is best-effort. Manual product capture remains available.
        }

        return new BarcodeLookupResultDto(
            normalized,
            false,
            false,
            null,
            null,
            null,
            null,
            "Manual");
    }

    private static string Normalize(string barcode)
    {
        var normalized = new string(
            (barcode ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray());

        if (normalized.Length < 8 || normalized.Length > 14)
        {
            throw new ArgumentException(
                "Barcode must contain between 8 and 14 digits.");
        }

        return normalized;
    }

    private sealed class UpcResponse
    {
        [JsonPropertyName("items")]
        public List<UpcItem>? Items { get; set; }
    }

    private sealed class UpcItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("images")]
        public List<string>? Images { get; set; }
    }
}
