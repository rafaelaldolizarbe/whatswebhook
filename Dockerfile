FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

COPY src/WhatsAppWebhook/WhatsAppWebhook.csproj WhatsAppWebhook/
RUN dotnet restore WhatsAppWebhook/WhatsAppWebhook.csproj

COPY src/WhatsAppWebhook/ WhatsAppWebhook/
RUN dotnet publish WhatsAppWebhook/WhatsAppWebhook.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained false \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

RUN useradd --create-home --shell /usr/sbin/nologin whatsapp
COPY --from=build /app/publish .
# /data é onde o volume nomeado do docker-compose (app-data) é montado — precisa
# existir com o dono certo ANTES do mount, senão o Docker inicializa o volume
# como root e o SQLite (rodando como usuário whatsapp) não consegue escrever nele.
RUN mkdir -p /data && chown -R whatsapp:whatsapp /app /data
USER whatsapp

ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "WhatsAppWebhook.dll"]
