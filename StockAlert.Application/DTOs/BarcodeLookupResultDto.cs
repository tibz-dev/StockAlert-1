namespace StockAlert.Application.DTOs;

public record BarcodeLookupResultDto(
    string Barcode,
    bool Found,
    bool IsLocalProduct,
    Guid? ProductId,
    string? ProductName,
    string? Brand,
    string? ImageUrl,
    string Source
);
