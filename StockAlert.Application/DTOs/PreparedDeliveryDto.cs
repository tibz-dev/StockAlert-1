namespace StockAlert.Application.DTOs;

public record PreparedDeliveryDto(
    Guid DeliveryLogId,
    string DocumentType,
    Guid DocumentId,
    string Channel,
    string Destination,
    string Status,
    string ActionUrl
);
