# Migrations do EF Core

As migrations ficam versionadas em `src/WhatsAppWebhook/Data/Migrations/` e são aplicadas
automaticamente no startup (`db.Database.Migrate()` no `Program.cs`) — não precisa rodar
`dotnet ef database update` manualmente em uso normal.

Só é preciso rodar comandos aqui quando **o modelo de dados muda** (nova entidade, novo campo em
`Contact`/`Message`, etc.).

## Pré-requisito

```bash
dotnet tool install --global dotnet-ef   # só na primeira vez
```

## Criar uma nova migration depois de alterar as entidades

Da pasta `whatsapp-webhook/src/WhatsAppWebhook`:

```bash
dotnet ef migrations add NomeDaMudanca -o Data/Migrations
```

Isso gera 3 arquivos em `Data/Migrations/` — cheque o Designer/snapshot antes de commitar.

## Aplicar manualmente num banco específico (raramente necessário)

```bash
DB_PATH=/caminho/para/app.db dotnet ef database update
```

## Desfazer a última migration (antes de aplicar/commitar)

```bash
dotnet ef migrations remove
```

## Ver o SQL que uma migration gera (sem aplicar)

```bash
dotnet ef migrations script
```
