# Especificação técnica 0017: Dashboard de Rentabilidade (Vault), parte 2 de 4

- Data: 2026-10-20
- Status: **implementada (fechamento em 2026-10-20)**
- ADR relacionado: [0018](../adr/0018-dashboard-de-rentabilidade.md)
- Esta parte continua a [parte 1](0017-vault-dashboard-rentabilidade-parte-1.md) (Passos 0 e 1: ADR e Domain). As decisões D1 a D17 e as "Atenções" da parte 1 continuam valendo.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e como validar.
> Todo código, comentário, commit e documentação em português.

---

## Escolhas desta parte (a confirmar)

| Item | Escolha |
|---|---|
| E1 | O filtro do repositório é só `UserId` e `EstaAtivo`. A elegibilidade (30 dias, cálculo válido) fica no Application, via `RentabilidadeMovimento.ElegivelParaRanking`: data aritmética em SQL seria frágil e a regra já mora no Domain |
| E2 | A **ordem dos grupos** (Tipo de Renda por nome) vem do **repositório**, pela collation do banco, como nos ADRs 0009 e 0015. O Application preserva essa ordem (o `GroupBy` do LINQ mantém a ordem da primeira ocorrência). Evita depender da cultura do servidor para ordenar acentos ("Ações") |
| E3 | Pastas novas `Application/Dashboards/...`, espelhando `Application/Movimentos/DistribuicaoValorLiquido` (modelos e parâmetros) e `Application/Movimentos/Queries/...` (query) |
| E4 | A validação de `top` e `ordem` fica no Application (`CommandValidator.Validate`, como nas queries da distribuição), de modo que a Api responda **422** sem código extra de validação no controller |
| E5 | O item do ranking traz também `DiasInvestido` (dias entre as duas datas). A tela pode ou não exibi-lo; ajuda a julgar a anualizada de aportes recentes (ADR 0018, Consequências) |
| E6 | Sem ordenação do "top" no repositório: o ranking é calculado em memória (D12) |

---

## Passo 2: Persistence

**Sem alteração de schema.** Nenhuma coluna, índice, migration ou mudança em `MovimentoMapping` (D17). O filtro `(UserId, EstaAtivo)` é atendido pelo prefixo do índice `IX_Movimentos_UserId_EstaAtivo_DataVencimento` (ADR 0010).

### 2.1 `IMovimentoRepository`

Um método novo:

- `Task<List<Movimento>> GetAtivosParaRankingAsync(Guid ownerId, CancellationToken cancellationToken)`

`CancellationToken` obrigatório, último parâmetro, sem valor padrão (ADR 0017).

### 2.2 `MovimentoRepository`

- Partir de `QueryWithRelatedEntities()` (já faz `AsNoTracking` e `Include` de Objetivo, TipoRenda, Corretora, Produto e Emissor).
- Filtro: `UserId == ownerId && EstaAtivo`. **Sem** filtrar `AtualizaPorCotacao` (D5: Movimentos por cotação entram), sem filtrar Objetivo (Objetivo inativo não filtra) e sem filtrar vencimento (vencidos ainda ativos entram).
- Ordem: `TipoRenda.Nome` ascendente e depois `TipoRendaId` (desempate determinístico entre Tipos de Renda de mesmo nome).
- Comentário curto no código explicando que a ordem define a ordem dos grupos na tela (E2) e remetendo ao ADR 0018, no estilo dos comentários de `GetDesatualizadosAsync`.
- `ToListAsync(cancellationToken)`.

### 2.3 Outros projetos que implementam a interface

Adicionar o método novo (lançando `NotSupportedException`) em `Application.Test/Movimentos/TestDoubles/FakeMovimentoRepository.cs`. Procurar outras implementações (`grep -rn ": IMovimentoRepository"`) antes de compilar; os `Mock<IMovimentoRepository>` não precisam de ajuste.

### 2.4 Testes (`Persistence.Test/Movimentos/MovimentoRepositoryTests.cs`)

Seguir os testes existentes (provider InMemory, `CreateContext()`, `AdicionarMovimento`). O helper `AdicionarMovimento` não define `EstaAtivo` (nasce `false`) e fixa o Tipo de Renda "CDB": definir `EstaAtivo = true` em cada Movimento e acrescentar ao helper um parâmetro opcional `tipoRendaNome` (padrão "CDB") para os testes de ordem.

- **`GetAtivosParaRankingAsync_RetornaSomenteAtivosDoUsuario`:** dois ativos do usuário, um inativo, um ativo de **outro** usuário. Resultado: só os dois ativos do usuário.
- **`GetAtivosParaRankingAsync_NaoFiltraVencidosCotacaoNemObjetivoInativo`:** Movimento ativo com `DataVencimento` no passado, outro com `AtualizaPorCotacao = true` e outro cujo Objetivo tem `EstaAtivo = false`. Todos os três voltam.
- **`GetAtivosParaRankingAsync_CarregaEntidadesRelacionadas`:** Objetivo, TipoRenda, Corretora, Produto e Emissor vêm preenchidos (não nulos).
- **`GetAtivosParaRankingAsync_OrdenaPorNomeDoTipoRendaEDepoisPorId`:** três Movimentos de Tipos de Renda "Tesouro", "CDB" e "LCI" (nessa ordem de inserção) voltam como CDB, LCI e Tesouro. Dois Movimentos de Tipos de Renda com o mesmo nome ficam agrupados e ordenados por `TipoRendaId` (calcular o esperado com `OrderBy` sobre os Ids, sem depender de Id fixo).
- **`GetAtivosParaRankingAsync_NaoRastreiaAsEntidades`:** limpar o `ChangeTracker` antes da consulta e conferir que as entidades retornadas ficam `Detached`.
- **Sem dados:** usuário sem Movimentos ativos recebe lista vazia.

### 2.5 Como validar

```
dotnet build
dotnet test Persistence.Test --filter FullyQualifiedName~GetAtivosParaRanking
```

**Critério de pronto:** testes verdes, sem alteração em `Persistence/Migrations`, nenhum pacote novo.

---

## Passo 3: Application

Tudo com `CancellationToken` obrigatório (D16). `OperationCanceledException` não é logada como erro. **Sem relógio:** a query não recebe `IDateTimeProvider` (D2).

### 3.1 Tipos (pasta `Application/Dashboards/Rentabilidade/`, namespace `Application.Dashboards.Rentabilidade`)

- **`OrdemRentabilidade`** (enum): `Maiores = 0` (padrão) e `Menores = 1`.
- **`RentabilidadeParameters`** (`record`, no estilo de `DistribuicaoValorLiquidoParameters`):
  - constantes públicas `TopPadrao = 25` e `TopMaximo = 100`;
  - `int Top { get; init; } = TopPadrao`, com `[Range(1, TopMaximo, ErrorMessage = "O top deve estar entre {1} e {2}")]`;
  - `OrdemRentabilidade Ordem { get; init; } = OrdemRentabilidade.Maiores`, com `[EnumDataType(typeof(OrdemRentabilidade))]` para rejeitar valores numéricos fora do enum (por exemplo `ordem=7`).
- **`RentabilidadeItemModel`** (`record`): `Guid Id`, `string? ProdutoNome`, `string? EmissorNome`, `string? ObjetivoNome`, `string? CorretoraNome`, `decimal ValorAporte`, `decimal ValorLiquidoAtual`, `int DiasInvestido`, `double Acumulada`, `double Diaria`, `double Anualizada` (frações: 0,225 = 22,5%, D14). Fábrica estática `FromDomain(Movimento movimento, RentabilidadeMovimento rentabilidade)`, com `?.` nos nomes, como em `ProximoVencimentoModel`.
- **`RentabilidadeGrupoModel`** (`record`): `Guid TipoRendaId`, `string? TipoRendaNome`, `List<RentabilidadeItemModel> Itens`.

### 3.2 Query (pasta `Application/Dashboards/Queries/GetRentabilidadesPorTipoRenda/`)

- **`IGetRentabilidadesPorTipoRendaQuery`:** `Task<List<RentabilidadeGrupoModel>> ExecuteAsync(Guid ownerId, RentabilidadeParameters parametros, CancellationToken cancellationToken)`.
- **`GetRentabilidadesPorTipoRendaQuery`:** construtor com `IRepositoryManager` e `ILoggerManager`. Registrada automaticamente pelo `AddServicesFromAssembly` (sem alterar `Program.cs`; confirmar na parte 3).

**Algoritmo, nesta ordem:**

1. `CommandValidator.Validate(parametros)` **fora do `try`**: erro de entrada é do cliente (422), não falha do serviço.
2. Dentro do `try`: `GetAtivosParaRankingAsync(ownerId, cancellationToken)`.
3. Para cada Movimento: `RentabilidadeCalculador.Calcular(ValorAporte, ValorLiquidoAtual, DataInvestimento, DataAtualizacaoValorLiquido)`. Descartar quando o resultado for `null` ("sem cálculo") ou `ElegivelParaRanking` for falso.
4. Agrupar por `TipoRendaId` **preservando a ordem do repositório** (E2). Nenhum grupo vazio chega à resposta (D10), porque só entram grupos com itens elegíveis.
5. Dentro de cada grupo: `Maiores` ordena `Anualizada` decrescente; `Menores`, crescente; nos dois casos, desempate por `Id` ascendente (D9). Aplicar `Take(Top)` **por grupo**.
6. Mapear para `RentabilidadeGrupoModel` e `RentabilidadeItemModel`.
7. `catch (Exception ex) when (ex is not OperationCanceledException)`: `LogError($"Algo deu errado no método de serviço {nameof(GetRentabilidadesPorTipoRendaQuery)}: {ex}")` e relançar (padrão de `GetMovimentosDesatualizadosQuery`).

Observações:

- Perda total (−100%) e prejuízo permanecem no ranking (D8 e D9): em `Menores` aparecem primeiro; em `Maiores` ficam no fim e só entram se o top comportar.
- Não há tratamento de `NaN` no Application: o calculador já devolve `null` para qualquer resultado não finito.
- `Id` ascendente como desempate: dois Movimentos com a mesma anualizada saem sempre na mesma ordem.

### 3.3 Testes (`Application.Test/Dashboards/Queries/GetRentabilidadesPorTipoRenda/GetRentabilidadesPorTipoRendaQueryTests.cs`)

No estilo de `GetMovimentosDesatualizadosQueryTests`: `Mock<IMovimentoRepository>`, `Mock<ILoggerManager>` e `Mock<IRepositoryManager>` com `SetupGet(r => r.Movimento)`. Os Movimentos de teste são montados em memória, com `TipoRenda`, `Produto`, `Emissor`, `Objetivo` e `Corretora` preenchidos e as datas escolhidas para dar dias exatos (por exemplo, 365). Caso de referência: aporte 1.000, valor líquido 1.100, 365 dias → anualizada 0,10.

- **Repassa `ownerId` e o token** ao repositório (`Verify` com `Times.Once`).
- **Maiores (padrão):** três Movimentos no mesmo Tipo de Renda saem em ordem decrescente de anualizada.
- **Menores:** os mesmos três saem em ordem crescente.
- **Top por grupo:** com `Top = 2` e dois Tipos de Renda de 3 Movimentos cada, cada grupo traz só 2 itens.
- **Desempate por `Id`:** dois Movimentos com a mesma anualizada saem em ordem crescente de `Id` (calcular o esperado com `OrderBy` sobre os Ids).
- **Grupos preservam a ordem do repositório:** o fake devolve CDB, CDB, LCI, Tesouro; a resposta tem os grupos nessa ordem, cada um com seus itens.
- **Menos de 30 dias fica fora:** 29 dias é excluído e 30 dias entra.
- **"Sem cálculo" fica fora:** Movimento com data de atualização igual à de investimento (0 dia) ou com `ValorAporte = 0` não aparece.
- **Tipo de Renda sem elegíveis é omitido:** um grupo só com Movimentos de 10 dias não aparece na resposta.
- **Perda total:** `ValorLiquidoAtual = 0` com 100 dias aparece como o **primeiro** item em `Menores`, com os três valores −1.
- **Prejuízo permanece:** valor líquido 900 sobre aporte 1.000 (365 dias) tem anualizada −0,10 e aparece em `Menores` antes de um item positivo.
- **Mapeamento dos campos:** nomes (Produto, Emissor, Objetivo, Corretora), `ValorAporte`, `ValorLiquidoAtual`, `DiasInvestido` e as três rentabilidades.
- **Lista vazia:** repositório sem Movimentos devolve lista vazia, sem exceção.
- **Parâmetros inválidos:** `Top = 0`, `Top = 101` e `Ordem = (OrdemRentabilidade)7` lançam `CommandValidationException` e **não consultam** o repositório (`Times.Never`).
- **Falha do repositório:** loga com o nome da query e relança a mesma exceção.
- **Cancelamento:** `OperationCanceledException` é relançada **sem** `LogError`.

### 3.4 Como validar

```
dotnet build
dotnet test Application.Test --filter FullyQualifiedName~GetRentabilidadesPorTipoRenda
dotnet test
```

O teste de reflexão do ADR 0017 (`Service.Test`) deve continuar verde: a nova interface e o novo método de repositório já nascem com o token como último parâmetro, e a allowlist de pendências não muda.

**Critério de pronto:** build e testes verdes, sem pacote novo, sem relógio na query, `CancellationToken` obrigatório em toda assinatura nova, `UserId` aplicado no repositório.

---

## Fora do escopo desta parte

Controller, DTOs e conversão do parâmetro `ordem` da URL (parte 3); página, cliente da Api, menu e fechamento do ADR (parte 4).

Ponto a resolver na parte 3: com `[ApiController]`, um valor de `ordem` que não seja `maiores` nem `menores` falha no *model binding* e responde 400 antes de chegar ao Application. Como a decisão D11 pede 422, a parte 3 define como tratar isso.
