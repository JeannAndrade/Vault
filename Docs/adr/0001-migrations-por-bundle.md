# ADR 0001: Migrations por bundle, em container dedicado

- Status: Aceita (2026-10-03)

## Contexto

O banco é atualizado por dois contextos (`VaultDbContext` e `VaultIdentityDbContext`) e as migrations precisam rodar antes da Api.

## Decisão

Um migration bundle autossuficiente por contexto, gerado no build (`dotnet ef migrations bundle --self-contained`) e executado em sequência por `docker/migrate.sh`, no container `migrations` (imagem `runtime-deps`, usuário não-root). A Api só inicia após `service_completed_successfully`.

## Alternativas consideradas

- `dotnet-ef` com SDK em tempo de execução: imagem pesada e recompilação a cada subida.
- `Database.Migrate()` no startup da Api: a Api passaria a exigir privilégio de DDL e haveria corrida com mais de uma réplica.

## Consequências

- Imagem enxuta, build reprodutível e falha de migration barra a Api.
- Dois bundles a manter e build mais lento.
- Um único usuário de banco para Api e migrador (dívida consciente; separar quando houver ambiente real).
- Os dois contextos compartilham a tabela `__EFMigrationsHistory`.
