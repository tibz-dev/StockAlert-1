using StockAlert.Domain.Enums;

namespace StockAlert.Domain.Entities;

public class StocktakeSession
{
    public Guid Id { get; set; }
    public required string ReferenceNumber { get; set; }
    public required string Title { get; set; }
    public string? Location { get; set; }
    public string? Zone { get; set; }
    public required string Source { get; set; }
    public StocktakeStatus Status { get; set; } = StocktakeStatus.Counting;
    public bool BlindCount { get; set; } = true;

    public required string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime SourceSnapshotAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public ICollection<StocktakeItem> Items { get; set; } =
        new List<StocktakeItem>();
}
