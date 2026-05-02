// This logic will live inside your SmartTradeAdapter eventually
using StockAlert.Domain.Entities;

public static class ExternalMapping
{
    public static Product MapFromExternal(dynamic externalItem)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = externalItem.name,
            Price = externalItem.unit_price,
            StockQuantity = externalItem.qty_on_hand,
            ExternalId = externalItem.id.ToString(), // Store their ID
            ExternalSource = "SmartTrade"
        };
    }
}