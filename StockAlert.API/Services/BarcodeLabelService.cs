using System.Globalization;
using System.Text;
using System.Xml;
using ZXing;
using ZXing.Common;

namespace StockAlert.API.Services;

/// <summary>Builds a printable Code 128 label from a stored product barcode.</summary>
public static class BarcodeLabelService
{
    public static string CreateSvg(string barcode, string productName, decimal price)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length > 64)
            throw new ArgumentException("Barcode is missing or too long.");

        // The ZXing encoder creates the actual, scanner-readable Code 128 bars.
        var matrix = new MultiFormatWriter().encode(
            barcode, BarcodeFormat.CODE_128, 580, 100);
        var xml = new StringBuilder();
        xml.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"600\" height=\"180\" viewBox=\"0 0 600 180\">");
        xml.Append("<rect width=\"600\" height=\"180\" fill=\"white\"/>");
        xml.Append("<g fill=\"black\">");
        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                if (matrix[x, y])
                    xml.Append($"<rect x=\"{x + 10}\" y=\"{y + 34}\" width=\"1\" height=\"1\"/>");
            }
        }
        xml.Append("</g>");
        var safeName = XmlEscape(productName.Length > 55 ? productName[..55] : productName);
        var safeBarcode = XmlEscape(barcode);
        var safePrice = price.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"));
        xml.Append($"<text x=\"300\" y=\"20\" font-size=\"15\" text-anchor=\"middle\" fill=\"black\">{safeName}</text>");
        xml.Append($"<text x=\"300\" y=\"151\" font-size=\"12\" text-anchor=\"middle\" fill=\"black\">{safeBarcode}</text>");
        xml.Append($"<text x=\"300\" y=\"172\" font-size=\"15\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"black\">R {safePrice}</text>");
        xml.Append("</svg>");
        return xml.ToString();
    }

    private static string XmlEscape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;
}
