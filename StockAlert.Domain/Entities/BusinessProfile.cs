namespace StockAlert.Domain.Entities;

public class BusinessProfile
{
    public Guid Id { get; set; }

    public string BusinessName { get; set; } = "StockAlert Business";
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public bool IsVatRegistered { get; set; }
    public decimal DefaultVatRate { get; set; }

    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "South Africa";

    public string? BranchName { get; set; }
    public string? BranchNumber { get; set; }

    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankBranchCode { get; set; }
    public string? BankAccountType { get; set; }

    public string CurrencyCode { get; set; } = "ZAR";
    public int QuoteValidityDays { get; set; } = 14;
    public string? ReceiptFooter { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
