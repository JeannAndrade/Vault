# ---------------------------------------------------------------------------
# Estágio de build: base comum a todos os estágios de publicação.
# Restaura as dependências em camada separada para aproveitar o cache.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY global.json ./

# Somente os .csproj de cada cadeia de dependência (Api e Web são independentes).
COPY Domain/Domain.csproj Domain/
COPY Persistence/Persistence.csproj Persistence/
COPY Application/Application.csproj Application/
COPY Service/Service.csproj Service/
COPY Presentation/Presentation.csproj Presentation/

RUN dotnet restore Service/Service.csproj \
  && dotnet restore Presentation/Presentation.csproj

# Código-fonte (o que não deve entrar está no .dockerignore)
COPY . .

# ---------------------------------------------------------------------------
# Publicação da Vault.Api
# ---------------------------------------------------------------------------
FROM build AS publish-api
RUN dotnet publish Service/Service.csproj -c Release -o /app --no-restore

# ---------------------------------------------------------------------------
# Publicação da Vault.Web
# ---------------------------------------------------------------------------
FROM build AS publish-web
RUN dotnet publish Presentation/Presentation.csproj -c Release -o /app --no-restore

# ---------------------------------------------------------------------------
# Imagem final da Vault.Api
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

# Apenas estas pastas são graváveis pelo usuário não-root; o restante de /app é somente leitura.
# /app/logs será montada como volume nomeado: na primeira montagem, o volume herda este dono.
RUN mkdir -p /app/logs /app/internal_logs \
  && chown "$APP_UID" /app/logs /app/internal_logs

COPY --from=publish-api /app ./
COPY --chmod=0555 docker/healthcheck.sh /usr/local/bin/healthcheck

EXPOSE 8080
USER $APP_UID

# Readiness: só fica "healthy" quando o banco aceita conexão (ver MapLumiaHealthChecks)
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --start-interval=2s --retries=5 \
  CMD ["healthcheck", "8080", "/health/ready"]

ENTRYPOINT ["dotnet", "Service.dll"]

# ---------------------------------------------------------------------------
# Geração dos migration bundles: um executável autossuficiente por contexto.
# Em tempo de build o bundle não conecta ao banco; em execução, ele usa as
# IDesignTimeDbContextFactory do projeto, que leem as variáveis de ambiente.
# ---------------------------------------------------------------------------
FROM build AS migrations-build
ARG TARGETARCH

RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.12

# Identificador de runtime compatível com a arquitetura da imagem
RUN case "$TARGETARCH" in \
  amd64) echo linux-x64 ;; \
  arm64) echo linux-arm64 ;; \
  *) echo "Arquitetura não suportada: $TARGETARCH" >&2; exit 1 ;; \
  esac > /tmp/rid

RUN /tools/dotnet-ef migrations bundle \
  --project Persistence/Persistence.csproj \
  --startup-project Persistence/Persistence.csproj \
  --context VaultDbContext \
  --configuration Release \
  --self-contained -r "$(cat /tmp/rid)" \
  --output /bundles/efbundle-vault \
  --force

RUN /tools/dotnet-ef migrations bundle \
  --project Persistence/Persistence.csproj \
  --startup-project Persistence/Persistence.csproj \
  --context VaultIdentityDbContext \
  --configuration Release \
  --self-contained -r "$(cat /tmp/rid)" \
  --output /bundles/efbundle-identity \
  --force

# ---------------------------------------------------------------------------
# Imagem final do migrador: só o necessário para executar os bundles
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrations
WORKDIR /app

# Bundles autossuficientes podem extrair bibliotecas nativas ao iniciar;
# /tmp é gravável pelo usuário não-root.
ENV DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/dotnet-bundle

COPY --from=migrations-build --chmod=0555 /bundles/ ./
COPY --chmod=0555 docker/migrate.sh /usr/local/bin/migrate

USER $APP_UID
ENTRYPOINT ["migrate"]

# ---------------------------------------------------------------------------
# Imagem final da Vault.Web (único componente publicado ao host)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

# Apenas estas pastas são graváveis pelo usuário não-root; o restante de /app é somente leitura.
# /app/logs será montada como volume nomeado: na primeira montagem, o volume herda este dono.
RUN mkdir -p /app/logs /app/internal_logs \
  && chown "$APP_UID" /app/logs /app/internal_logs

COPY --from=publish-web /app ./

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Presentation.dll"]
