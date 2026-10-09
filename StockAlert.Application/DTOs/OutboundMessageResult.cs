namespace StockAlert.Application.DTOs;

public record OutboundMessageResult(
    bool Sent,
    bool ProviderConfigured,
    string? ProviderReference,
    string? ErrorMessage
);
