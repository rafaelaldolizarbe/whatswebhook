using System.Security.Cryptography;
using System.Text;

namespace WhatsAppWebhook.Endpoints;

public static class WebhookSignatureValidator
{
    private const string SignaturePrefix = "sha256=";

    public static bool IsValid(string rawBody, string? signatureHeader, string? appSecret)
    {
        if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(appSecret))
        {
            return false;
        }

        if (!signatureHeader.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var providedHash = signatureHeader[SignaturePrefix.Length..];

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var computedHex = Convert.ToHexStringLower(computedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(providedHash));
    }
}
