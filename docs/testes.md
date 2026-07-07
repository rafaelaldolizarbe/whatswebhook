# Rodar os testes automatizados

Da pasta `whatsapp-webhook/`:

```bash
dotnet test src/WhatsAppWebhook.Tests
```

Cobre hoje:
- Verificação do `hub.challenge` (token correto/incorreto).
- Validação da assinatura HMAC (`X-Hub-Signature-256`).
- Idempotência por `WamId` (constraint única no banco).
- `WhatsAppSender` (payload enviado à Graph API + persistência do outbound), com `HttpMessageHandler` fake — não bate na Meta de verdade.
- `DefaultConversationFlow` (qual mensagem é enviada para cada botão/entrada), com um `IWhatsAppSender` fake.

## Rodar só um arquivo/teste específico

```bash
dotnet test src/WhatsAppWebhook.Tests --filter "FullyQualifiedName~WebhookSignatureValidatorTests"
```

## Build sem rodar testes

```bash
dotnet build src/WhatsAppWebhook
```
