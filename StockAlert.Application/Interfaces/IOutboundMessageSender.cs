using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IOutboundMessageSender
{
    Task<OutboundMessageResult> SendEmailAsync(
        string destination,
        string subject,
        string body);

    Task<OutboundMessageResult> SendSmsAsync(
        string destination,
        string body);

    Task<OutboundMessageResult> SendWhatsAppAsync(
        string destination,
        string body);
}
