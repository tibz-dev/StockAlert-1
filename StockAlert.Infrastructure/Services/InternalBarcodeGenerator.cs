using System.Security.Cryptography;

namespace StockAlert.Infrastructure.Services;

public static class InternalBarcodeGenerator
{
    public static string Generate()
    {
        // Code 128-compatible internal identifier; SQL Server checks uniqueness.
        return $"SA-{Convert.ToHexString(RandomNumberGenerator.GetBytes(12))}";
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Barcode cannot be empty.");

        var barcode = value.Trim().ToUpperInvariant();

        if (barcode.StartsWith("SA-", StringComparison.Ordinal))
        {
            if (barcode.Length != 27 || !barcode[3..].All(Uri.IsHexDigit))
                throw new ArgumentException("Invalid StockAlert internal barcode.");
            return barcode;
        }

        if (barcode.Length is < 8 or > 14 || !barcode.All(char.IsAsciiDigit))
            throw new ArgumentException("External barcode must contain 8 to 14 digits.");

        return barcode;
    }
}
