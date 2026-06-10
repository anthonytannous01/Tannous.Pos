using System.Security.Cryptography;
using System.Text;

namespace Tannous.Pos.Infrastructure.Delivery.Adapters;

/// <summary>
/// Shared HMAC-SHA256 webhook signature verification for delivery channel adapters.
/// Computes a lowercase hex digest of the raw body keyed by the channel webhook secret
/// and compares against the provided signature using a constant-time comparison.
/// </summary>
internal static class DeliveryWebhookSignature
{
    public static bool IsValid(string rawBody, string providedSignature, string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(providedSignature) || string.IsNullOrWhiteSpace(webhookSecret))
            return false;

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));

            // Accept either hex or base64 signatures; normalise the provided value to bytes.
            var providedBytes = TryDecode(providedSignature.Trim());
            if (providedBytes == null) return false;

            return CryptographicOperations.FixedTimeEquals(computed, providedBytes);
        }
        catch
        {
            return false;
        }
    }

    private static byte[]? TryDecode(string signature)
    {
        // Strip optional "sha256=" prefix used by some platforms.
        var value = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signature["sha256=".Length..]
            : signature;

        // Try hex first.
        if (value.Length % 2 == 0 && value.All(Uri.IsHexDigit))
        {
            try { return Convert.FromHexString(value); }
            catch { /* fall through */ }
        }

        // Then try base64.
        try { return Convert.FromBase64String(value); }
        catch { return null; }
    }
}
