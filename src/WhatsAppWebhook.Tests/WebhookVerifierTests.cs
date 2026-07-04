using WhatsAppWebhook.Endpoints;
using Xunit;

namespace WhatsAppWebhook.Tests;

public class WebhookVerifierTests
{
    [Fact]
    public void Verify_ComModoESubscribeETokenCorreto_RetornaChallenge()
    {
        var result = WebhookVerifier.Verify("subscribe", "meu-token", "123456", "meu-token");

        Assert.True(result.IsValid);
        Assert.Equal("123456", result.Challenge);
    }

    [Fact]
    public void Verify_ComTokenIncorreto_RetornaInvalido()
    {
        var result = WebhookVerifier.Verify("subscribe", "token-errado", "123456", "meu-token");

        Assert.False(result.IsValid);
        Assert.Null(result.Challenge);
    }

    [Fact]
    public void Verify_ComModoDiferenteDeSubscribe_RetornaInvalido()
    {
        var result = WebhookVerifier.Verify("outro", "meu-token", "123456", "meu-token");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Verify_SemTokenEsperadoConfigurado_RetornaInvalido()
    {
        var result = WebhookVerifier.Verify("subscribe", "meu-token", "123456", expectedToken: null);

        Assert.False(result.IsValid);
    }
}
