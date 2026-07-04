using System.Security.Cryptography;
using System.Text;
using WhatsAppWebhook.Endpoints;
using Xunit;

namespace WhatsAppWebhook.Tests;

public class WebhookSignatureValidatorTests
{
    private const string AppSecret = "segredo-do-app";
    private const string Body = """{"object":"whatsapp_business_account","entry":[]}""";

    private static string ComputeSignature(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return "sha256=" + Convert.ToHexStringLower(hash);
    }

    [Fact]
    public void IsValid_ComAssinaturaCorreta_RetornaTrue()
    {
        var signature = ComputeSignature(Body, AppSecret);

        Assert.True(WebhookSignatureValidator.IsValid(Body, signature, AppSecret));
    }

    [Fact]
    public void IsValid_ComAssinaturaDeCorpoDiferente_RetornaFalse()
    {
        var signature = ComputeSignature("outro corpo qualquer", AppSecret);

        Assert.False(WebhookSignatureValidator.IsValid(Body, signature, AppSecret));
    }

    [Fact]
    public void IsValid_ComSegredoErrado_RetornaFalse()
    {
        var signature = ComputeSignature(Body, "segredo-errado");

        Assert.False(WebhookSignatureValidator.IsValid(Body, signature, AppSecret));
    }

    [Fact]
    public void IsValid_SemHeaderDeAssinatura_RetornaFalse()
    {
        Assert.False(WebhookSignatureValidator.IsValid(Body, null, AppSecret));
    }

    [Fact]
    public void IsValid_SemAppSecretConfigurado_RetornaFalse()
    {
        var signature = ComputeSignature(Body, AppSecret);

        Assert.False(WebhookSignatureValidator.IsValid(Body, signature, null));
    }

    [Fact]
    public void IsValid_ComPrefixoAusente_RetornaFalse()
    {
        var hash = ComputeSignature(Body, AppSecret)["sha256=".Length..];

        Assert.False(WebhookSignatureValidator.IsValid(Body, hash, AppSecret));
    }
}
