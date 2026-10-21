namespace StockAlert.Application.DTOs;

public record StocktakeSessionDto(
    Guid Id,
    string ReferenceNumber,
    string Title,
    string? Location,
    string? Zone,
    string Source,
    string Status,
    bool BlindCount,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime SourceSnapshotAt,
    DateTime? CompletedAt,
    string? CompletedBy,
    int TotalItems,
    int CountedItems,
    int MatchedItems,
    int VarianceItems,
    int RecountRequiredItems,
    int ReviewRequiredItems,
    decimal VarianceValue,
    List<StocktakeItemDto> Items
);
