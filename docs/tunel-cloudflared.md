# Expor o webhook local com cloudflared (para testar com a Meta antes do deploy)

A Meta exige HTTPS público — `localhost` não serve. Enquanto a app não está na VM,
usamos um túnel temporário. Isso substitui, por enquanto, o Caddy + domínio da seção 7.

## Pré-requisito

App já rodando local na porta 5000 (ver [rodar-local.md](./rodar-local.md)).

## Comando

Em outro terminal:

```bash
cloudflared tunnel --url http://localhost:5000
```

Não precisa de conta Cloudflare — é o "quick tunnel". A saída mostra algo como:

```
Your quick Tunnel has been created! Visit it at (it may take some time to be reachable):
https://algumas-palavras-aleatorias.trycloudflare.com
```

Essa URL + `/webhook` é o que você cola no painel da Meta (ver [webhook-meta.md](./webhook-meta.md)).

## Importante

- A URL **muda a cada vez** que você roda o comando de novo — não é fixa.
- O túnel só existe enquanto o comando `cloudflared` estiver rodando. Se fechar o terminal, ele cai e a Meta para de conseguir entregar mensagens.
- Sempre que a URL mudar, é preciso ir de novo no painel da Meta e atualizar o Callback URL.
- É só para desenvolvimento/teste. O deploy real (seção 7 do `HERE.md`) usa Caddy com domínio fixo e certificado automático — ver [publish-deploy.md](./publish-deploy.md).

## Testar se o túnel está de pé

```bash
curl "https://SUA-URL.trycloudflare.com/webhook?hub.mode=subscribe&hub.verify_token=<META_VERIFY_TOKEN>&hub.challenge=teste123"
```

Deve responder `200` com `teste123` no corpo — mesmo teste do local, só que pela URL pública.
