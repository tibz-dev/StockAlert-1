using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.Infrastructure.Services;

public class OutboundMessageSender : IOutboundMessageSender
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public OutboundMessageSender(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OutboundMessageResult> SendEmailAsync(
        string destination,
        string subject,
        string body)
    {
        var host = _configuration["Communication:Smtp:Host"];
        var fromEmail = _configuration["Communication:Smtp:FromEmail"];
        var username = _configuration["Communication:Smtp:Username"];
        var password = _configuration["Communication:Smtp:Password"];

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(fromEmail))
        {
            return NotConfigured("SMTP email provider is not configured.");
        }

        try
        {
            var port = int.TryParse(
                _configuration["Communication:Smtp:Port"],
                out var configuredPort)
                ? configuredPort
                : 587;

            var enableSsl = !bool.TryParse(
                    _configuration["Communication:Smtp:EnableSsl"],
                    out var configuredSsl)
                || configuredSsl;

            using var message = new MailMessage
            {
                From = new MailAddress(
                    fromEmail,
                    _configuration["Communication:Smtp:FromName"]
                        ?? "StockAlert"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(destination);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username)
                && !string.IsNullOrWhiteSpace(password))
            {
                client.Credentials = new NetworkCredential(
                    username,
                    password);
            }

            await client.SendMailAsync(message);

            return new OutboundMessageResult(
                true,
                true,
                null,
                null);
        }
        catch (Exception ex)
        {
            return new OutboundMessageResult(
                false,
                true,
                null,
                ex.Message);
        }
    }

    public Task<OutboundMessageResult> SendSmsAsync(
        string destination,
        string body)
    {
        return SendTwilioMessageAsync(
            destination,
            body,
            isWhatsApp: false);
    }

    public Task<OutboundMessageResult> SendWhatsAppAsync(
        string destination,
        string body)
    {
        return SendTwilioMessageAsync(
            destination,
            body,
            isWhatsApp: true);
    }

    private async Task<OutboundMessageResult> SendTwilioMessageAsync(
        string destination,
        string body,
        bool isWhatsApp)
    {
        var accountSid = _configuration["Communication:Twilio:AccountSid"];
        var authToken = _configuration["Communication:Twilio:AuthToken"];
        var fromNumber = isWhatsApp
            ? _configuration["Communication:Twilio:WhatsAppFromNumber"]
            : _configuration["Communication:Twilio:SmsFromNumber"];

        if (string.IsNullOrWhiteSpace(accountSid)
            || string.IsNullOrWhiteSpace(authToken)
            || string.IsNullOrWhiteSpace(fromNumber))
        {
            return NotConfigured(
                isWhatsApp
                    ? "Twilio WhatsApp provider is not configured."
                    : "Twilio SMS provider is not configured.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            var normalizedTo = NormalizeE164(destination);
            var normalizedFrom = NormalizeE164(fromNumber);

            var values = new Dictionary<string, string>
            {
                ["From"] = isWhatsApp
                    ? $"whatsapp:{normalizedFrom}"
                    : normalizedFrom,
                ["To"] = isWhatsApp
                    ? $"whatsapp:{normalizedTo}"
                    : normalizedTo
            };

            var contentSid = isWhatsApp
                ? _configuration[
                    "Communication:Twilio:WhatsAppContentSid"]
                : null;

            if (isWhatsApp && !string.IsNullOrWhiteSpace(contentSid))
            {
                values["ContentSid"] = contentSid;
                values["ContentVariables"] = JsonSerializer.Serialize(
                    new Dictionary<string, string>
                    {
                        ["1"] = body
                    });
            }
            else
            {
                values["Body"] = body;
            }

            using var form = new FormUrlEncodedContent(values);
            using var response = await client.PostAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
                form);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new OutboundMessageResult(
                    false,
                    true,
                    null,
                    ExtractTwilioError(responseBody)
                        ?? $"Twilio returned HTTP {(int)response.StatusCode}.");
            }

            using var json = JsonDocument.Parse(responseBody);
            var sid = json.RootElement.TryGetProperty(
                "sid",
                out var sidElement)
                ? sidElement.GetString()
                : null;

            return new OutboundMessageResult(
                true,
                true,
                sid,
                null);
        }
        catch (Exception ex)
        {
            return new OutboundMessageResult(
                false,
                true,
                null,
                ex.Message);
        }
    }

    private static string NormalizeE164(string value)
    {
        var trimmed = value.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0") && digits.Length == 10)
        {
            return "+27" + digits[1..];
        }

        return trimmed.StartsWith("+")
            ? "+" + digits
            : "+" + digits;
    }

    private static string? ExtractTwilioError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            return document.RootElement.TryGetProperty(
                "message",
                out var message)
                ? message.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static OutboundMessageResult NotConfigured(string message)
    {
        return new OutboundMessageResult(
            false,
            false,
            null,
            message);
    }
}
