namespace StockAlert.Application.DTOs;

public record CommunicationStatusDto(
    bool EmailConfigured,
    bool SmsConfigured,
    bool WhatsAppConfigured
);
