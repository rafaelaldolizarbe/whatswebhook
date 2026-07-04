namespace WhatsAppWebhook.Endpoints;

public static class WebhookVerifier
{
    public static WebhookVerificationResult Verify(string? mode, string? verifyToken, string? challenge, string? expectedToken)
    {
        var isValid = mode == "subscribe"
            && !string.IsNullOrEmpty(expectedToken)
            && verifyToken == expectedToken;

        return new WebhookVerificationResult(isValid, isValid ? challenge ?? string.Empty : null);
    }
}

public readonly record struct WebhookVerificationResult(bool IsValid, string? Challenge);
