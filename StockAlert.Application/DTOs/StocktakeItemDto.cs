namespace StockAlert.Application.DTOs;

public record StocktakeItemDto(
    Guid Id,
    Guid? ProductId,
    string? ExternalProductId,
    string ProductName,
    string? Barcode,
    decimal UnitValue,
    int? ExpectedQuantity,
    int? FirstCount,
    string? FirstCountedBy,
    DateTime? FirstCountedAt,
    int? SecondCount,
    string? SecondCountedBy,
    DateTime? SecondCountedAt,
    int? FinalCount,
    int? VarianceQuantity,
    decimal? VarianceValue,
    string Status,
    string? ResolutionReason,
    string? ApprovedBy,
    DateTime? ApprovedAt
);
