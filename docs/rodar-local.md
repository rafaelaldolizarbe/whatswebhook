# Rodar localmente

## Pré-requisito: preencher o `.env`

Arquivo `whatsapp-webhook/.env` (ignorado pelo Git, nunca commitar):

```
META_VERIFY_TOKEN=escolha-qualquer-string
META_APP_SECRET=       # Configurações do App → Básico → Chave Secreta do App
META_ACCESS_TOKEN=     # WhatsApp → Configuração da API (temporário, 24h)
META_PHONE_NUMBER_ID=  # WhatsApp → Configuração da API
META_GRAPH_VERSION=v21.0
```

## Comando

Da pasta `whatsapp-webhook/src/WhatsAppWebhook`:

```bash
set -a; source ../../.env; set +a
DB_PATH="../../data/app.db" dotnet run --urls http://localhost:5000
```

- `set -a` faz o `source` exportar todas as variáveis do `.env` como variáveis de ambiente.
- `DB_PATH` aponta o SQLite para `whatsapp-webhook/data/app.db` (pasta ignorada pelo Git). Sem essa variável, o default é `./data/app.db` relativo à pasta onde você rodou o comando.
- A porta `5000` é a mesma que o Caddy vai usar em produção (seção 7 do `HERE.md`).

## Verificar que subiu

```bash
curl "http://localhost:5000/webhook?hub.mode=subscribe&hub.verify_token=<o-mesmo-valor-do-META_VERIFY_TOKEN>&hub.challenge=teste123"
```

Deve responder `200` com o corpo `teste123`.

## Parar

`Ctrl+C` no terminal onde rodou, ou:

```bash
pkill -f "WhatsAppWebhook"
```

> Repare que o processo real (`dotnet run` → apphost) não se chama `WhatsAppWebhook.dll`,
> e sim só `WhatsAppWebhook` — confira com `pgrep -af WhatsAppWebhook` antes de assumir
> que matou o processo certo, principalmente depois de trocar variáveis no `.env`.
