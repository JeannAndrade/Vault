# Especificação técnica 0018: Fluxo Mensal (Vault), parte 2 de 4

- Data: 2026-10-24
- Status: implementada (fechamento em 2026-10-24)
- ADR relacionado: [0019](../adr/0019-fluxo-mensal.md)
- Esta parte continua a [parte 1](0018-vault-fluxo-mensal.md) (Passos 0 e 1: ADR e Domain). As decisões D1 a D14 e as "Atenções" da parte 1 continuam valendo.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e como validar.
> Todo código, comentário, commit e documentação em português.

---

## Escolhas desta parte (a confirmar)

| Item | Escolha |
| --- | --- |
| E1 | O repositório devolve **projeções leves** (records seladas em `Persistence/Movimentos/`, ao lado de `MovimentoFiltro`), não entidades: sem `Include`, sem tracking e só as colunas necessárias (D8) |
| E2 | Retorno: o filtro de data tem **só o limite superior** (`DataVencimento < limiteSuperior`), para trazer também os vencidos ainda ativos (D4). Investido: janela `[inicio, fim)` |
| E3 | Sem `OrderBy` no repositório: a Application agrupa em memória e a ordem dos 12 meses vem da `JanelaMensal`. Isso evita ordenação no banco (filesort) sem ganho algum |
| E4 | Os modelos da Application trazem `Ano` e `Mes` (inteiros); o formato `AAAA-MM` é decisão de contrato e fica no Service (parte 3) |
| E5 | As queries **não têm parâmetros de entrada**, então não há `CommandValidator` (diferente do ranking da 0017); o único dado variável é o relógio |
| E6 | Pastas novas: `Application/Dashboards/FluxoMensal/` (modelos) e `Application/Dashboards/Queries/GetRetornoMensal/` e `.../GetInvestidoMensal/` (queries), espelhando a organização da 0017 |
| E7 | **Ponto da parte 1 resolvido (`Kind`):** o repositório não configura `DateTimeKind`, então o provedor MySQL materializa as datas como `Unspecified`, o mesmo `Kind` gerado pela `JanelaMensal`. Além disso, a comparação de `DateTime` em .NET ignora o `Kind` (compara ticks). **Nenhuma mudança na parte 1** |

---

## Passo 2: Persistence

**Sem alteração de schema.** Nenhuma coluna, índice, migration ou mudança em `MovimentoMapping` (D14).

- Retorno: o filtro `(UserId, EstaAtivo, DataVencimento)` é atendido pelo índice `IX_Movimentos_UserId_EstaAtivo_DataVencimento` (ADR 0010).
- Investido: o filtro `(UserId, DataInvestimento)` é atendido pelo índice de `MovimentoMapping` (ADR 0009), com nome gerado pela convenção do EF (`IX_Movimentos_UserId_DataInvestimento`).

### 2.1 Projeções (pasta `Persistence/Movimentos/`)

`record` seladas e imutáveis, sem dependência de `Movimento`:

- **`RetornoPrevistoItem`**: `DateTime DataVencimento` (não nula: o filtro já exclui os sem vencimento) e `decimal ValorLiquidoAtual`.
- **`AporteInvestidoItem`**: `DateTime DataInvestimento`, `decimal ValorAporte` e `bool EhReinvestimento`.

### 2.2 `IMovimentoRepository`

Dois métodos novos, com `CancellationToken` obrigatório, último parâmetro e sem valor padrão (ADR 0017):

- `Task<List<RetornoPrevistoItem>> GetRetornoPrevistoAsync(Guid ownerId, DateTime limiteSuperior, CancellationToken cancellationToken)`
- `Task<List<AporteInvestidoItem>> GetInvestidoAsync(Guid ownerId, DateTime inicio, DateTime fim, CancellationToken cancellationToken)`

### 2.3 `MovimentoRepository`

**`GetRetornoPrevistoAsync`:**

- Partir de `FindByCondition(..., false)` (sem tracking), **sem** `QueryWithRelatedEntities()`: não há `Include`.
- Filtro: `UserId == ownerId && EstaAtivo && DataVencimento != null && DataVencimento < limiteSuperior`. **Sem** filtrar Objetivo (Objetivo inativo não filtra, D7), sem filtrar `AtualizaPorCotacao` e sem limite inferior (vencidos ativos entram, D4).
- `Select` direto para `RetornoPrevistoItem` (`DataVencimento!.Value` e `ValorLiquidoAtual`), `ToListAsync(cancellationToken)`.
- Sem `OrderBy` (E3).
- Comentário curto no código: o limite superior é exclusivo e não há limite inferior de propósito (vencidos ativos somam no mês atual); remeter ao ADR 0019.

**`GetInvestidoAsync`:**

- Mesmo ponto de partida (`FindByCondition(..., false)`, sem `Include`).
- Filtro: `UserId == ownerId && DataInvestimento >= inicio && DataInvestimento < fim`. **Sem** filtrar `EstaAtivo` (D5: exceção deliberada) e sem filtrar Objetivo (D7).
- `Select` para `AporteInvestidoItem`, `ToListAsync(cancellationToken)`, sem `OrderBy`.
- Comentário curto: a ausência de `EstaAtivo` é intencional (o histórico de aportes não some com o resgate); remeter ao ADR 0019.

### 2.4 Outros projetos que implementam a interface

Adicionar os dois métodos (lançando `NotSupportedException`) em `Application.Test/Movimentos/TestDoubles/FakeMovimentoRepository.cs`. Procurar outras implementações (`grep -rn ": IMovimentoRepository"`) antes de compilar; os `Mock<IMovimentoRepository>` não precisam de ajuste.

### 2.5 Testes (`Persistence.Test/Movimentos/MovimentoRepositoryTests.cs`)

Seguir os testes existentes (provider InMemory, `CreateContext()`, `AdicionarMovimento`). Ajustes no helper `AdicionarMovimento`: parâmetro opcional `objetivoEstaAtivo` (padrão `true`) para os testes de Objetivo inativo. Em cada teste, definir `EstaAtivo`, `DataVencimento`, `ValorAporte`, `ValorLiquidoAtual` e `EhReinvestimento` sobre o Movimento devolvido, antes de salvar.

`GetRetornoPrevistoAsync`:

- **`..._RetornaSomenteAtivosComVencimentoDoUsuario`:** um ativo com vencimento, um inativo, um ativo sem vencimento e um ativo de **outro** usuário. Resultado: só o primeiro.
- **`..._IncluiVencidosAtivos`:** ativo com `DataVencimento` bem no passado (por exemplo, meses antes do limite) volta.
- **`..._LimiteSuperiorEhExclusivo`:** `DataVencimento` exatamente igual ao limite **não** volta; um tick antes do limite volta.
- **`..._NaoFiltraObjetivoInativo`:** Movimento ativo cujo Objetivo tem `EstaAtivo = false` volta.
- **`..._ProjetaDataEValor`:** os itens trazem `DataVencimento` e `ValorLiquidoAtual` corretos (e **não** `ValorAporte`: dois valores diferentes no Movimento provam qual foi lido).
- **Sem dados:** usuário sem Movimentos elegíveis recebe lista vazia.

`GetInvestidoAsync`:

- **`..._IncluiInativosEReinvestimentos`:** dois Movimentos na janela (um ativo, um **inativo**, um deles reinvestimento): os dois voltam, com `EhReinvestimento` correto.
- **`..._ExcluiOutroUsuario`:** aporte de outro usuário na janela não volta.
- **`..._JanelaInicioInclusivoFimExclusivo`:** `DataInvestimento == inicio` volta; `DataInvestimento == fim` **não** volta; um tick antes do fim volta; um tick antes do início não volta.
- **`..._NaoFiltraObjetivoInativo`:** Movimento cujo Objetivo está inativo volta.
- **`..._ProjetaDataEValor`:** os itens trazem `DataInvestimento` e `ValorAporte` (e **não** `ValorLiquidoAtual`: valores diferentes no Movimento).
- **Sem dados:** lista vazia.

### 2.6 Como validar

```
dotnet build
dotnet test Persistence.Test --filter "FullyQualifiedName~GetRetornoPrevisto|FullyQualifiedName~GetInvestido"
```

**Atenção:** o provider InMemory **não valida a tradução para SQL**. Antes de fechar o passo, rodar as duas consultas uma vez contra o MariaDB de desenvolvimento (a tela da parte 4 serve) e conferir no log do EF que a projeção por construtor é traduzida sem erro. Para o uso dos índices, fazer o `EXPLAIN` em volume realista quando houver (gatilho do ADR 0019).

**Critério de pronto:** testes verdes, sem alteração em `Persistence/Migrations`, nenhum pacote novo, nenhuma entidade carregada (só projeções).

---

## Passo 3: Application

Tudo com `CancellationToken` obrigatório (D13). `OperationCanceledException` não é logada como erro. **Aqui há relógio:** as queries recebem `IDateTimeProvider` (`LumiaFoundation.Core.TimeService`), como `GetMovimentosDesatualizadosQuery`, e repassam "hoje" ao Domain.

### 3.1 Tipos (pasta `Application/Dashboards/FluxoMensal/`, namespace `Application.Dashboards.FluxoMensal`)

- **`RetornoMensalModel`** (`record`): `int Ano`, `int Mes`, `decimal Total` (soma de `ValorLiquidoAtual`) e `int Quantidade` (número de Movimentos).
- **`InvestidoMensalModel`** (`record`): `int Ano`, `int Mes`, `decimal Novo` (`EhReinvestimento = false`), `decimal Reinvestimento` (`true`) e `decimal Total`, que é **sempre** `Novo + Reinvestimento`. Preferir propriedade calculada a campo, para a invariante não depender de quem monta o modelo.

Dinheiro em `decimal`, sem arredondamento: a soma de valores com 2 casas continua exata.

### 3.2 Queries

**`IGetRetornoMensalQuery`** (`Application/Dashboards/Queries/GetRetornoMensal/`): `Task<List<RetornoMensalModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)`.

**`GetRetornoMensalQuery`:** construtor com `IRepositoryManager`, `ILoggerManager` e `IDateTimeProvider`. Registrada automaticamente pelo `AddServicesFromAssembly` (sem alterar `Program.cs`; confirmar na parte 3).

Algoritmo, nesta ordem:

1. Dentro do `try`: `hoje = dateTimeProvider.GetDateTime()`; `janela = JanelaMensal.Proximos(hoje)`.
2. `itens = GetRetornoPrevistoAsync(ownerId, janela[^1].Fim, cancellationToken)`. O limite superior é o `Fim` do 12º mês.
3. Para cada item, escolher o mês de destino: se `DataVencimento` for anterior ao `Fim` do **primeiro** intervalo, vai para o **primeiro** (mês atual, onde somam os vencidos ativos, D4); senão, vai para o intervalo que o `Contem`. Item que não caiba em nenhum intervalo (não deveria vir do repositório) é **ignorado**, sem exceção.
4. Montar **sempre 12** `RetornoMensalModel`, na ordem da janela, com `Total` e `Quantidade` somados; meses sem itens saem com 0 e 0 (D2).
5. `catch (Exception ex) when (ex is not OperationCanceledException)`: `LogError($"Algo deu errado no método de serviço {nameof(GetRetornoMensalQuery)}: {ex}")` e relançar.

**`IGetInvestidoMensalQuery`** (`Application/Dashboards/Queries/GetInvestidoMensal/`): mesma forma, com `Task<List<InvestidoMensalModel>>`.

**`GetInvestidoMensalQuery`:** mesmo construtor e mesmo tratamento de erro.

1. `hoje` pelo relógio; `janela = JanelaMensal.Anteriores(hoje)`.
2. `itens = GetInvestidoAsync(ownerId, janela[0].Inicio, janela[^1].Fim, cancellationToken)`.
3. Para cada item, achar o intervalo pelo `Contem`; item fora de todos é ignorado.
4. Montar **sempre 12** `InvestidoMensalModel`, na ordem da janela: `Novo` soma `ValorAporte` dos itens com `EhReinvestimento = false`; `Reinvestimento`, dos com `true`.
5. Mesmo `catch` e mesmo log, com o nome da query.

Observações:

- Aporte com `DataInvestimento` futura **dentro do mês atual** entra no mês atual; futura além dele fica de fora (já filtrada pelo `fim` no repositório; ADR 0019).
- O relógio é UTC (D1): a virada de mês ocorre à 0h UTC, ou seja, às 21h BRT do último dia do mês. É o comportamento aceito, coberto por teste.
- 12 intervalos contra N itens é busca linear sobre 12 posições: custo desprezível na escala de controle pessoal; não otimizar.

### 3.3 Testes (`Application.Test/Dashboards/Queries/GetRetornoMensal/GetRetornoMensalQueryTests.cs` e `.../GetInvestidoMensal/GetInvestidoMensalQueryTests.cs`)

No estilo de `GetMovimentosDesatualizadosQueryTests`: `Mock<IMovimentoRepository>`, `Mock<ILoggerManager>`, `Mock<IDateTimeProvider>` e `Mock<IRepositoryManager>` com `SetupGet(r => r.Movimento)`. Relógio padrão dos testes: `2026-10-24 15:30 UTC`.

`GetRetornoMensalQueryTests`:

- **Repassa `ownerId`, limite e token:** `GetRetornoPrevistoAsync` chamado uma vez com o `ownerId`, o limite `2027-10-01` e o token (`Verify`).
- **12 meses sempre presentes:** repositório vazio devolve 12 itens, `2026-10` a `2027-09`, em ordem crescente, todos com `Total = 0` e `Quantidade = 0`.
- **Soma por mês:** três itens no mesmo mês somam `Total` e `Quantidade` apenas nesse mês.
- **Fronteiras do mês:** vencimento em `2026-10-31 23:59:59` conta em outubro; em `2026-11-01 00:00:00`, em novembro.
- **Vencidos ativos somam no mês atual:** itens em `2026-03-15` e `2026-10-05` somam ambos em `2026-10`, com `Quantidade = 2`.
- **Virada de ano:** item em `2027-01-10` cai em `2027-01`.
- **Item fora da janela é ignorado:** o fake devolve um item em `2027-10-01` (defensivo); nenhum mês o contabiliza e não há exceção.
- **Fronteira UTC:** relógio em `2026-10-31 23:30 UTC` → o primeiro mês é `2026-10`; em `2026-11-01 00:30 UTC` → é `2026-11` e o limite passa para `2027-11-01`.
- **Falha do repositório:** loga com o nome da query e relança a mesma exceção.
- **Cancelamento:** `OperationCanceledException` é relançada **sem** `LogError`.

`GetInvestidoMensalQueryTests`:

- **Repassa `ownerId`, janela e token:** `GetInvestidoAsync` com `ownerId`, `inicio = 2025-11-01`, `fim = 2026-11-01` e o token.
- **12 meses sempre presentes:** repositório vazio devolve 12 itens, `2025-11` a `2026-10`, em ordem crescente, todos zerados.
- **Separa novo e reinvestimento:** no mesmo mês, dois aportes novos e um reinvestimento resultam em `Novo`, `Reinvestimento` e `Total` corretos; `Total == Novo + Reinvestimento`.
- **Mês só com reinvestimento / só com novo:** o outro campo fica 0.
- **Fronteiras do mês:** aporte em `2026-09-30 23:59:59` conta em setembro; em `2026-10-01 00:00:00`, em outubro.
- **Aporte futuro no mês atual conta no atual:** `2026-10-30` entra em `2026-10`.
- **Item fora da janela é ignorado:** um item em `2025-10-31` (defensivo) não aparece.
- **Virada de ano:** aporte em `2026-01-05` cai em `2026-01`.
- **Falha do repositório** e **cancelamento:** como no Retorno.

### 3.4 Como validar

```
dotnet build
dotnet test Application.Test --filter "FullyQualifiedName~GetRetornoMensal|FullyQualifiedName~GetInvestidoMensal"
dotnet test
```

O teste de reflexão do ADR 0017 (`Service.Test`) deve continuar verde: as novas interfaces e os novos métodos de repositório já nascem com o token como último parâmetro, e a allowlist de pendências não muda.

**Critério de pronto:** build e testes verdes, sem pacote novo, `CancellationToken` obrigatório em toda assinatura nova, `UserId` aplicado no repositório, e sempre 12 meses na resposta.

---

## Fora do escopo desta parte

Controller, DTOs, formato `AAAA-MM` e rotas (parte 3); página, cliente da Api, gráficos, menu e fechamento do ADR (parte 4).
