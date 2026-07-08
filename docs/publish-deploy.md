# Publicar e fazer deploy

> **Mudança de abordagem:** o `HERE.md` (seção 7) originalmente previa `dotnet publish` direto +
> systemd. Decidimos migrar para **Docker + Terraform** (ver conversa do dia) porque o plano é
> vender soluções parecidas pra outros clientes depois, e essa combinação replica muito mais fácil.
> O systemd/publish direto não foi implementado — ficou obsoleto por essa decisão.

## Visão geral

- **Terraform** (`deploy/terraform/`) provisiona a VM do zero na Oracle Cloud (rede, security list, instância ARM Ampere `VM.Standard.A1.Flex`, Always Free). O `cloud-init` já deixa Docker instalado e as portas 80/443 liberadas no firewall interno da VM.
- **Docker Compose** (`deploy/docker-compose.yml`) roda dois containers na VM: `app` (nosso backend) e `caddy` (reverse proxy + HTTPS automático).
- **Nenhum dos dois é aplicado automaticamente por mim** — Terraform cria recursos reais e cobráveis, então `terraform apply` é sempre rodado manualmente por você.

## 1. Provisionar a VM (Terraform)

```bash
cd whatsapp-webhook/deploy/terraform
cp terraform.tfvars.example terraform.tfvars   # preencher compartment_id e ajustar se necessário
terraform init
terraform plan      # confira o que vai ser criado
terraform apply      # cria de fato — cobra na conta OCI
```

Ao final, anote o `instance_public_ip` do output — vai precisar dele pro DNS e pro SSH.

## 2. Apontar o DNS

Criar um registro **A** do subdomínio escolhido (ex.: `api.seudominio.com.br`) para o `instance_public_ip`, no provedor onde o domínio está registrado (Registro.br). Sem isso, o Caddy não consegue emitir o certificado — o Let's Encrypt valida o domínio via DNS/HTTP antes de gerar o TLS.

## 3. Enviar o código para a VM

Não há CI/CD ainda — o envio é manual via `rsync`/`scp`:

```bash
rsync -avz --exclude 'bin' --exclude 'obj' --exclude '.git' --exclude 'data' \
  /home/rafael/projetos/profile_bcknd_support/whatsapp-webhook/ \
  ubuntu@<instance_public_ip>:/opt/whatsapp-webhook/
```

Copie também o `.env` (com os valores de produção, não os de teste) separadamente, com uma permissão restrita:

```bash
scp /home/rafael/projetos/profile_bcknd_support/whatsapp-webhook/.env ubuntu@<instance_public_ip>:/opt/whatsapp-webhook/.env
ssh ubuntu@<instance_public_ip> "chmod 600 /opt/whatsapp-webhook/.env"
```

Atualize o `Caddyfile` (`deploy/Caddyfile`) com o domínio real antes de enviar — hoje ele está com o placeholder `api.SEUDOMINIO.com.br`.

## 4. Subir os containers

```bash
ssh ubuntu@<instance_public_ip>
cd /opt/whatsapp-webhook/deploy
docker compose up -d --build
docker compose logs -f
```

## 5. Verificar

```bash
curl "https://api.seudominio.com.br/webhook?hub.mode=subscribe&hub.verify_token=<TOKEN>&hub.challenge=teste"
```

Deve responder `200` com o corpo `teste` — igual aos testes que já fizemos com o túnel `cloudflared`, só que agora num domínio de verdade com certificado válido.

## Atualizações futuras (deploy de uma nova versão)

```bash
# local: enviar código atualizado
rsync -avz --exclude 'bin' --exclude 'obj' --exclude '.git' --exclude 'data' \
  whatsapp-webhook/ ubuntu@<instance_public_ip>:/opt/whatsapp-webhook/

# na VM: reconstruir e reiniciar só o container da app (Caddy não precisa reiniciar)
ssh ubuntu@<instance_public_ip> "cd /opt/whatsapp-webhook/deploy && docker compose up -d --build app"
```

## Destruir a infraestrutura (parar de ser cobrado)

```bash
cd whatsapp-webhook/deploy/terraform
terraform destroy
```
⚠️ Isso apaga a VM e os dados nela (incluindo o SQLite, que não tem backup fora da VM ainda). Faça
backup do `data/app.db` antes, se precisar.
