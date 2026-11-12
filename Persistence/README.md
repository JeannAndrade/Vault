# Persistence

Implementação das portas da Application com EF Core e MariaDB, os contextos (`VaultDbContext` e `VaultIdentityDbContext`), os mapeamentos e as migrations. Decisões: [ADR 0023](../Docs/adr/0023-inversao-da-persistencia-e-pacotes.md) e [ADR 0001](../Docs/adr/0001-migrations-por-bundle.md).

## Dependências permitidas

- Projetos: `Application` e `Domain`.
- Pacotes: `Lumia.Foundation.EFRepository`, `Lumia.Foundation.Auth` e os de configuração e EF Core. Nenhum pacote de ASP.NET Core.
- Quem usa: apenas o Service, que é a raiz de composição. O Web nunca acessa o banco: só a Api, por contrato HTTP.

## Registro

O Service chama um único `AddPersistence(IConfiguration)`, que configura os dois contextos e registra **somente** `IRepositoryManager`. Os repositórios por entidade não vão para o contêiner: o `RepositoryManager` os cria sob demanda sobre o mesmo `DbContext`. `ConfigureDatabase` e `ConfigureIdentityDatabase` continuam públicos para as `IDesignTimeDbContextFactory` (pasta `ContextFactory/`), usadas na geração de migrations.

O registro por convenção (`AddServicesFromAssembly`) não alcança estas classes, porque a interface fica na Application, em outro assembly.

## Estrutura

- `Context/` e `Mappings/`: contextos e mapeamentos das entidades.
- `<Agregado>/`: `<Entidade>Repository`, que estende `BaseRepository<T>` e implementa a porta `I<Entidade>Repository` da Application.
- `Management/`: `RepositoryManager`, que estende `BaseRepositoryManager` e implementa `IRepositoryManager`.
- `Migrations/`: migrations dos dois contextos.

## Regras

- **Multi-tenant:** toda consulta filtra por `OwnerId`. Um ID de outro usuário se comporta como inexistente.
- **`CancellationToken`:** repassado a todas as chamadas assíncronas do EF Core (ADR 0017).
- **Mudança de schema:** exige discussão prévia e uma migration nova; o schema atual não é redesenhado.

## Como adicionar um repositório

1. Declare `I<Entidade>Repository` em `Application/Repositories/`, com os tipos de filtro e projeção que ele usar.
2. Implemente `<Entidade>Repository` em `Persistence/<Agregado>/`.
3. Adicione a propriedade em `IRepositoryManager` (Application) e em `RepositoryManager`, criada por `Lazy`.
4. Não registre o repositório no contêiner.
5. Testes em `Persistence.Test` (`Microsoft.EntityFrameworkCore.InMemory`) e, para consultas com regra de escopo, um teste de que dados de outro `OwnerId` não aparecem.

## Migrations

Geradas com as factories de `ContextFactory/` e aplicadas pelo migration bundle (ver `Docs/adr/0001-migrations-por-bundle.md` e `Docs/Docker.md`).

## Como testar

`dotnet test Persistence.Test`
