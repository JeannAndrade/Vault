# ADR 0023: Inversão da dependência da persistência e pacotes da Application

- Status: Aceita (2026-11-11)

## Contexto

O Vault é a referência de arquitetura para projetos futuros (o próximo é o Flashcard), mas as dependências não apontavam para dentro:

- `Application.csproj` referenciava `Persistence.csproj`. A Application dependia do EF Core de forma transitiva (via `Lumia.Foundation.EFRepository`) e dos tipos concretos de persistência.
- `IRepositoryManager : IBaseRepositoryManager` e as interfaces `I<Entidade>Repository` (com os filtros e projeções que elas usam) moravam na Persistence e eram injetadas nas queries e commands.
- `Application.csproj` referenciava `Lumia.Foundation.AspNetCore` inteiro, trazendo o framework web, só para usar `CommandValidator`, que está em `Lumia.Foundation.Core`. Também referenciava `Lumia.Foundation.Logger` (tratado no ADR 0024).
- A pasta e o namespace `Persistence/Managment` tinham erro de grafia.

Verificado no código (Vault e LumiaFoundation, `main`):

- `IBaseRepositoryManager` declara **apenas** `Task SaveAsync(CancellationToken)`. A porta da Application declara o mesmo método sem herdar do pacote EF; `RepositoryManager : BaseRepositoryManager, IRepositoryManager` continua válido, porque o método público de `BaseRepositoryManager` satisfaz o membro da interface.
- `AddServicesFromAssembly` só casa `I<Classe>` quando a interface está no **mesmo assembly** da classe. Com a porta na Application e a implementação na Persistence, nada seria registrado por convenção: o registro explícito é obrigatório. A varredura da Application não é afetada (interfaces são ignoradas).

## Decisão

1. **Portas na Application.** `IRepositoryManager` e as `I<Entidade>Repository` ficam em `Application/Repositories/` (namespace `Application.Repositories`). Os tipos de filtro e projeção usados por elas movem junto: `MovimentoFiltro`, `MovimentoTituloFiltro`, `ConsolidadoFiltro`, `RetornoPrevistoItem` e `AporteInvestidoItem`. `IRepositoryManager` declara `SaveAsync(CancellationToken)` sozinho.
2. **Referência invertida.** A Persistence referencia a Application e o Domain; a Application deixa de referenciar a Persistence. As implementações (`RepositoryManager`, `*Repository`) ficam na Persistence e estendem `BaseRepositoryManager`.
3. **Raiz de composição no Service.** O Service referencia Application e Persistence e chama um único `AddPersistence(IConfiguration)`, que registra os dois contextos (negócio e identidade) e **somente** `IRepositoryManager`. Os repositórios por entidade não são registrados no contêiner: a Application só os alcança por `IRepositoryManager`, que os cria por `Lazy` sobre o mesmo `DbContext`. `ConfigureDatabase` e `ConfigureIdentityDatabase` continuam públicos para os `IDesignTimeDbContextFactory` (migrations).
4. **Pacotes.** A Application referencia só `Domain` e `Lumia.Foundation.Core` (`CommandValidator`, `PagedList`, exceções de domínio). `Lumia.Foundation.AspNetCore` e `Lumia.Foundation.Logger` saem; o Service os declara explicitamente, nas mesmas versões.
5. **Nome.** `Managment` passa a `Management`, em commit isolado.
6. **Camadas-alvo** (válidas para o Vault e para o Flashcard): Domain → Core; Application → Domain e Core; Persistence → Application, Domain, EFRepository e Auth; Service → Application, Persistence e Foundation; Presentation → só pacotes da Foundation.
7. **Direção garantida por teste.** `Service.Test/Arquitetura/DependenciasEntreCamadasTests` lê os `.csproj` e falha se uma camada referenciar projeto ou pacote fora da regra, ou se surgir projeto de produção sem regra.

## Alternativas consideradas

- **Portas num projeto próprio** (por exemplo, `Application.Abstractions`): a Persistence não dependeria da Application inteira, mas cria um projeto a mais e separa as portas dos casos de uso que as definem. Pode ser reavaliado se a Application crescer.
- **Portas no Domain:** clássico em DDD, mas filtros e projeções são tipos orientados a consulta, não regras de domínio, e inchariam o Domain.
- **Só documentar e testar a direção, sem inverter:** não corrige a dependência.
- **Teste de arquitetura:** `GetReferencedAssemblies()` só enxerga referências usadas no código e deixaria passar um pacote declarado e ainda não usado (o caso do AspNetCore na Application); `NetArchTest` ou `ArchUnitNET` exigiriam dependência nova. Escolhida a leitura dos `.csproj`, sem dependência nova. Limitação: cobre só as referências diretas, de onde nasce qualquer vazamento transitivo.

## Consequências

- O teste reflexivo do ADR 0017 passa a varrer `Application.Repositories` em vez de `Persistence`.
- O migration bundle (ADR 0001, `Dockerfile`) passa a incluir a Application, já que a Persistence a referencia; o `Dockerfile` já copia o `Application.csproj` antes do restore.
- `ConfigureRepositoryManager` foi substituído por `AddPersistence` (Service e `Persistence.Test`).
- Novos repositórios exigem: interface em `Application/Repositories/`, implementação na Persistence e uma propriedade em `IRepositoryManager`/`RepositoryManager`. Não há registro por entidade.
- Dívida mantida: duplicação `*Parameters` → `*Filtro` (ADR 0012, D6), fora do escopo.

## Execução

Quatro commits, cada um com build e testes verdes: `6f702e5` (ADR 0024, queries sem log), `c86857e` (`Management`), `b34895a` (portas, referência invertida, `AddPersistence`, pacotes) e `6707f7e` (remoção do registro duplicado dos repositórios). O teste de arquitetura vem em commit próprio.

## Como validar

1. `dotnet build` e `dotnet test` na solução, ambos verdes.
2. `dotnet test Service.Test --filter "FullyQualifiedName~DependenciasEntreCamadasTests"`.
3. Teste de mutação: adicionar temporariamente um `ProjectReference` de Application para Persistence; o teste deve falhar apontando `Persistence`.
4. `dotnet list Application package --include-transitive`: sem `Lumia.Foundation.AspNetCore`, `Lumia.Foundation.Logger`, EF Core nem ASP.NET Core.
5. `docker compose up --build`: Api e Web sobem saudáveis e o migration bundle roda sem erro.

## Pendências de documentação

- ADR 0012: a restrição "Service → Application → Persistence → Domain; Persistence não enxerga Application" e a tabela que coloca `MovimentoFiltro` na Persistence ficam invertidas; atualizar e referenciar este ADR.
- ADR 0017: a regra do `catch ... when` foi superada pelo ADR 0024; a referência ao teste de reflexão sobre `Persistence` mudou de assembly.
- READMEs: criar para Application e Persistence (hoje só existem para Domain, Service e Presentation), com a regra de dependência e como registrar um novo repositório.
- Specs antigas são histórico e não são reescritas.
