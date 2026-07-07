# Configurar/atualizar o webhook no painel da Meta

## Onde

[developers.facebook.com/apps](https://developers.facebook.com/apps) → seu App → **WhatsApp** → **Configuration** (ou **Use cases → Customize → Configuration** se o App foi criado pelo fluxo "Connect with customers through WhatsApp").

## Passos

1. Em **Webhook**, clique em **Edit**.
2. **Callback URL:** `https://SUA-URL-PUBLICA/webhook` (a URL do túnel — ver [tunel-cloudflared.md](./tunel-cloudflared.md) — ou o domínio real depois do deploy).
3. **Verify Token:** o mesmo valor de `META_VERIFY_TOKEN` no `.env`.
4. Clique em **Verify and Save**.
   - Isso faz a Meta chamar `GET /webhook` com `hub.challenge`. Se o token bater, ela aceita.
   - Se der erro aqui: confira se a app está rodando, se o túnel está de pé, e se o token no painel é *exatamente* igual ao do `.env`.
5. Na lista de campos (webhook fields), assine **`messages`** (é o único que usamos nesta fase — os demais como `account_alerts`, `message_template_status_update` etc. não são tratados pelo nosso código).

## Testar envio de uma mensagem real

1. No **WhatsApp → Configuração da API**, adicione seu próprio número em "To" (números de teste precisam ser verificados manualmente, até 5).
2. Mande uma mensagem do seu WhatsApp pessoal para o número de teste da Meta.
3. Confira se chegou no banco:

```bash
sqlite3 whatsapp-webhook/data/app.db "SELECT WamId, Direction, Type, Body FROM Messages ORDER BY Id DESC LIMIT 5;"
```
(se não tiver `sqlite3` instalado, dá pra usar Python: `python3 -c "import sqlite3; ..."`)

## Erros comuns

| Sintoma | Causa provável |
|---|---|
| "Verify and Save" falha | App local não está rodando, túnel caiu, ou `Verify Token` diferente do `.env` |
| Mensagem enviada não chega no banco | `META_APP_SECRET` errado/vazio → nosso endpoint rejeita com 403 (confira o log da app) |
| Erro 131030 ao tentar enviar mensagem | Número de destino não está na lista de "To" verificados (só vale para número de teste grátis) |
| Bot não responde depois de ~24h de token temporário | `META_ACCESS_TOKEN` temporário expirou — gerar um novo, ou criar token permanente de System User (seção 6 do `HERE.md`) |
