# ADR 0023: Inversão da dependência da persistência e pacotes da Application

- Status: Proposta (2026-11-11). **Parcial, a completar na próxima interação.**

## Contexto

O Vault é a referência de arquitetura para projetos futuros (o próximo é o Flashcard), mas hoje as dependências não apontam para dentro:

- `Application.csproj` referencia `Persistence.csproj`. A Application depende do EF Core de forma transitiva (via `Lumia.Foundation.EFRepository`) e dos tipos concretos de persistência.
- `IRepositoryManager : IBaseRepositoryManager` e as interfaces `I<Entidade>Repository` (com os filtros e projeções que elas usam, como `MovimentoFiltro` e `RetornoPrevistoItem`) moram na Persistence e são injetadas nas queries e commands.
- `Application.csproj` referencia `Lumia.Foundation.AspNetCore` inteiro, trazendo o framework web, só para usar `CommandValidator`, que está em `Lumia.Foundation.Core`. Também referencia `Lumia.Foundation.Logger`, tratado no ADR 0024.

Verificado no código (Vault `main` e LumiaFoundation `main`):

- `IBaseRepositoryManager` declara **apenas** `Task SaveAsync(CancellationToken)`. Uma porta própria da Application pode declarar esse mesmo método sem herdar do pacote EF; `RepositoryManager : BaseRepositoryManager, IRepositoryManager` continua válido, porque o método público de `BaseRepositoryManager` satisfaz o membro da interface.
- `AddServicesFromAssembly` só casa `I<Classe>` quando a interface está no **mesmo assembly** da classe. Com as portas na Application e as implementações na Persistence, nenhum repositório seria registrado por convenção: o registro explícito é obrigatório. A varredura da Application não é afetada (interfaces são ignoradas).
- Hoje o Service chama `ConfigureDatabase`, `ConfigureRepositoryManager`, `ConfigureIdentityDatabase` e `AddServicesFromAssembly(typeof(IGetCorretoraQuery).Assembly)` em sequência; os `IDesignTimeDbContextFactory` (`PersistenceContextFactory`, `IdentityContextFactory`) usam a configuração do banco para gerar migrations.

## Decisão (proposta)

1. **Portas na Application.** `IRepositoryManager` e as `I<Entidade>Repository` passam para `Application/Repositories/`, namespace `Application.Repositories` (confirmado). Os tipos de filtro e projeção usados por elas movem junto. `IRepositoryManager` declara `SaveAsync(CancellationToken)` sozinho, sem herdar de `IBaseRepositoryManager`.
2. **Referência invertida.** A Persistence referencia a Application; a Application deixa de referenciar a Persistence. As implementações (`RepositoryManager`, `*Repository`) continuam na Persistence e estendem `BaseRepositoryManager`.
3. **Raiz de composição no Service.** O Service referencia Application e Persistence. A Persistence expõe **um único** método de registro, `AddPersistence(IConfiguration)`, que inclui o contexto de identidade (confirmado). `ConfigureDatabase` e `ConfigureIdentityDatabase` continuam públicos para os `IDesignTimeDbContextFactory`.
4. **Pacotes.** A Application referencia só `Lumia.Foundation.Core` (para `CommandValidator`, `PagedList` e exceções de domínio). `Lumia.Foundation.AspNetCore` e `Lumia.Foundation.Logger` saem; o Service declara os dois explicitamente.
5. **Camadas-alvo** (válidas para o Vault e para o Flashcard): Domain → Core; Application → Domain e Core; Persistence → Application, Domain, EFRepository e Auth; Service → Application, Persistence e Foundation; Presentation → só pacotes da Foundation.

## Alternativas consideradas

- **Portas num projeto próprio** (por exemplo, `Application.Abstractions`): a Persistence não dependeria da Application inteira, mas cria um projeto a mais e separa as portas dos casos de uso que as definem. Pode ser reavaliado se a Application crescer.
- **Portas no Domain:** clássico em DDD, mas os filtros e projeções são tipos orientados a consulta, não regras de domínio, e inchariam o Domain.
- **Manter como está e só testar com teste de arquitetura:** não corrige a dependência; apenas a documenta.

## Consequências (parciais)

- O teste reflexivo do ADR 0017 varre `typeof(Persistence.Corretoras.ICorretoraRepository).Assembly`; passa a varrer a Application.
- O migration bundle (ADR 0001, `Dockerfile`) passa a incluir a Application, já que a Persistence a referencia.
- Os ADRs 0012 e 0013 e as specs ficam desatualizados quanto à direção das camadas e aos namespaces dos filtros.
- `ConfigureRepositoryManager` (usado em `Service/Program.cs` e em `Persistence.Test`) é substituído por `AddPersistence`.
- Dívida mantida: duplicação `*Parameters` → `*Filtro` (ADR 0012, D6), fora do escopo.

## A completar

1. Inventário dos tipos de filtro e projeção que movem com as portas (`MovimentoFiltro`, `MovimentoTituloFiltro`, `ConsolidadoFiltro`, `RetornoPrevistoItem`, `AporteInvestidoItem`) e dos que ficam na Persistence.
2. Conteúdo exato de `AddPersistence` e o que fica de fora (os `IDesignTimeDbContextFactory`).
3. Plano de execução em passos com commits, na ordem acordada: ADR 0024 antes; pacotes; mover portas e inverter a referência; teste de arquitetura por referência.
4. Teste de arquitetura: como garantir a direção das referências (`GetReferencedAssemblies()`).
5. "Como validar": build, testes e `docker compose up --build`, incluindo o migration bundle.
6. Atualizações de documentação: ADRs 0012/0013, README por projeto.
