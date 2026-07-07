# Publicar e fazer deploy

> **Status:** só a parte de `dotnet publish` está pronta. Os arquivos de deploy
> (`whatsapp-webhook.service` do systemd e o `Caddyfile`) da seção 7 do `HERE.md`
> ainda **não foram gerados** — isso é o próximo bloco de trabalho. Este arquivo
> será completado quando fizermos isso.

## Gerar o binário publicado (o que já dá pra fazer hoje)

Da pasta `whatsapp-webhook/`:

```bash
dotnet publish src/WhatsAppWebhook -c Release -r linux-arm64 --self-contained false -o publish/
```

- `-r linux-arm64`: a VM Oracle Cloud é ARM64 (seção 7 do `HERE.md`).
- `--self-contained false`: exige o runtime do ASP.NET Core instalado na VM (mais leve). Alternativa self-contained:

```bash
dotnet publish src/WhatsAppWebhook -c Release -r linux-arm64 --self-contained true -o publish/
```
(gera um binário maior, mas que roda sem precisar instalar o runtime na VM.)

## O que falta (próximo bloco)

- `deploy/whatsapp-webhook.service` — unit do systemd (`Restart=always`, usuário não-root, `EnvironmentFile`).
- `deploy/Caddyfile` — proxy reverso com HTTPS automático.
- Checklist de pré-requisitos da VM: portas 80/443 liberadas (Security List da OCI + UFW/iptables), DNS do subdomínio apontando pro IP antes de subir o Caddy.

Assim que isso for feito, este documento ganha os comandos de `scp`/deploy e o passo a passo de subir o serviço na VM.
