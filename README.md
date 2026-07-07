# WhatsApp Webhook

Backend do bot de atendimento via WhatsApp Cloud API para o portfólio pessoal do Rafael.
ASP.NET Core Minimal API + EF Core/SQLite. Ver `HERE.md` (na raiz do projeto) para o
contexto completo e os critérios de aceite.

## Variáveis de ambiente

Copiar/preencher em `.env` na raiz deste projeto (ignorado pelo Git, nunca commitar):

| Variável | Descrição |
|---|---|
| `META_VERIFY_TOKEN` | String arbitrária, definida por nós; usada na verificação do webhook |
| `META_APP_SECRET` | App Secret do App na Meta (validação HMAC do `X-Hub-Signature-256`) |
| `META_ACCESS_TOKEN` | Token de acesso (temporário em teste; permanente de System User em produção) |
| `META_PHONE_NUMBER_ID` | ID do número de telefone (não é o telefone em si) |
| `META_GRAPH_VERSION` | Versão da Graph API, ex.: `v21.0` |
| `DB_PATH` | Caminho do arquivo SQLite (default: `./data/app.db`) |
| `META_GRAPH_BASE_URL` | Opcional, só para apontar a um mock local durante testes — nunca definir em produção |

## Como rodar as coisas do dia a dia

| O que | Onde |
|---|---|
| Rodar a app localmente | [docs/rodar-local.md](./docs/rodar-local.md) |
| Rodar os testes | [docs/testes.md](./docs/testes.md) |
| Expor local com túnel público (testar com a Meta antes do deploy) | [docs/tunel-cloudflared.md](./docs/tunel-cloudflared.md) |
| Configurar/atualizar o webhook no painel da Meta | [docs/webhook-meta.md](./docs/webhook-meta.md) |
| Criar/aplicar migrations do EF Core | [docs/migrations-ef-core.md](./docs/migrations-ef-core.md) |
| Publicar e fazer deploy | [docs/publish-deploy.md](./docs/publish-deploy.md) |

## Exemplos de `curl`

Verificação do webhook (GET):

```bash
curl "http://localhost:5000/webhook?hub.mode=subscribe&hub.verify_token=SEU_TOKEN&hub.challenge=teste123"
```

Simular mensagem de texto recebida (POST) — requer calcular o HMAC do corpo com o `META_APP_SECRET`:

```bash
BODY='{"object":"whatsapp_business_account","entry":[{"id":"1","changes":[{"value":{"messaging_product":"whatsapp","contacts":[{"profile":{"name":"Teste"},"wa_id":"5511999999999"}],"messages":[{"from":"5511999999999","id":"wamid.TESTE123","timestamp":"1751500000","type":"text","text":{"body":"oi"}}]},"field":"messages"}]}]}'
SIG=$(python3 -c "import hmac,hashlib,sys; print('sha256='+hmac.new(b'$META_APP_SECRET', sys.argv[1].encode(), hashlib.sha256).hexdigest())" "$BODY")
curl -X POST http://localhost:5000/webhook \
  -H "Content-Type: application/json" \
  -H "X-Hub-Signature-256: $SIG" \
  -d "$BODY"
```

## Status do MVP

Ver seção 10 (Critérios de aceite) do `HERE.md`. Implementado até aqui: verificação do
webhook, recepção com validação HMAC e idempotência, persistência em SQLite, envio via
Graph API e menu interativo inicial. Pendente: deploy (systemd + Caddy) na VM Oracle Cloud.
