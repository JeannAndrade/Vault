# Especificação técnica 0021: Consolidado de Movimentos (Vault)

- Data: 2026-10-30
- Status: **em elaboração (parte 1 de 3: Passos 0 a 2)**
- ADR relacionado: [0022](../adr/0022-consolidado-de-movimentos.md)
- Repositório afetado: **Vault** (Persistence, Application, Service, Presentation/Web). **Sem alteração no Domain, no LumiaFoundation, no schema e sem migration.**

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.
> **Partes:** 1 = Passos 0 a 2 (este arquivo); 2 = Passo 3 (Service); 3 = Passos 4 e 5 (Web e fechamento).

---

## 1. Resumo

Novo item **Consolidado** em **Dashboards**. O usuário escolhe até quatro filtros (Tipo de Renda, Corretora, Produto, Emissor, um valor por dimensão, combinados com `E`) e, opcionalmente, uma dimensão de agrupamento. A tela mostra **Qtde Movimentos, Total Aportado e Total Líquido** do conjunto e, com agrupamento, uma linha por grupo com o **percentual do Total Líquido** do grupo sobre o todo. Só Movimentos **ativos** entram.

## 2. Decisões aprovadas

| Item | Decisão |
| --- | --- |
| D1 | Filtros **e** agrupamento opcional por uma dimensão, com coluna de percentual |
| D2 | Só Movimentos `EstaAtivo = true`; `Pendente` fora, com nota na tela |
| D3 | Objetivo inativo não filtra; o total pode diferir da tela inicial (nota na tela) |
| D4 | O resultado só é exibido quando há filtro (ver P2) |
| D5 | Um valor por dimensão; sem multiseleção |
| T1 | Agregação **no banco** (`Count`, `Sum`, `GroupBy`) |
| T2 | Resolvida por D2: **sem** `CompoeSaldoDoObjetivo`; a entidade `Movimento` não muda |
| D6 | Um tipo de filtro por camada (`ConsolidadoFiltro` em Persistence, `ConsolidadoParameters` em Application, `ConsolidadoFiltroDto` em Service), mapeados com `ToXxx()`; não reutiliza `MovimentoFiltro` |
| D7 | Com agrupamento, o total é a **soma das linhas** (uma só consulta); sem agrupamento, uma consulta de agregado |
| D8 | Ordem das linhas: Total Líquido decrescente, nome e Id ascendentes |
| D9 | Percentual: `TotalLiquido do grupo / TotalLiquido do conjunto`; conjunto zerado resulta em 0; fração (0,325 = 32,5%) |
| D10 | `CancellationToken` obrigatório e último parâmetro (ADR 0017) |
| D11 | Sem Domain: não há regra de negócio nova (soma e razão) |

### Pontos a confirmar

| Item | Pergunta | Recomendação |
| --- | --- | --- |
| P1 | O percentual é sobre o **Total Líquido** (fatia do dinheiro hoje)? Ou também sobre o Aportado? | Só Total Líquido, uma coluna |
| P2 | "Exibir só quando filtrado": escolher **só agrupamento, sem filtro**, também exibe (ex.: "quanto tenho em cada corretora")? | Sim: exibir quando houver filtro **ou** agrupamento. Hoje o ADR exige filtro; ajustar o ADR e a Api se você concordar |

## 3. Atenção antes de começar

1. **UserId** vem só do token e é aplicado em toda consulta. GUID desconhecido ou de outro usuário devolve zeros, nunca 404.
2. **Dinheiro em `decimal`.** O percentual também é `decimal` (fração) até o Web.
3. **InMemory não valida SQL.** `GroupBy` com `Sum` funciona no InMemory, mas a tradução para o MariaDB só é conferida na validação manual (Passo 1.5).
4. **Sem relógio** nesta feature.
5. **Sem dependência nova**; Domain e LumiaFoundation intocados.
6. **Status do ADR:** começa **Proposta**; muda para **Aceita** na parte 3.

## 4. Ordem de implementação e entrega desta spec

Cada passo termina com `dotnet build` e `dotnet test` verdes.

| # | Passo | Projeto | Entrega |
| --- | --- | --- | --- |
| 0 | ADR 0022 como "Proposta" | Docs | **parte 1 (este arquivo)** |
| 1 | Filtro, projeções e repositório | Persistence | **parte 1 (este arquivo)** |
| 2 | Query do consolidado | Application | **parte 1 (este arquivo)** |
| 3 | Endpoint, DTOs e contrato | Service (Api) | parte 2 |
| 4 | Página, cliente da Api, menu | Presentation (Web) | parte 3 |
| 5 | Validação ponta a ponta e fechamento | Docs | parte 3 |

---

## Passo 0: ADR 0022 como "Proposta"

- Criar `Docs/adr/0022-consolidado-de-movimentos.md` com o texto entregue junto, **Status: Proposta (2026-10-30)**.
- Atualizar o índice de ADRs, se existir.

**Critério de pronto:** arquivo criado, no padrão dos ADRs anteriores.

---

## Passo 1: Persistence

Pasta `Persistence/Movimentos/`. Sem schema, índice ou migration.

### 1.1 Tipos

- **`ConsolidadoFiltro`**: `sealed record` imutável com `Guid? TipoRendaId`, `CorretoraId`, `ProdutoId` e `EmissorId`; propriedade `bool TemFiltro` (algum valor preenchido). Fica **sem relação** com `MovimentoFiltro`.
- **`DimensaoConsolidado`**: enum `TipoRenda`, `Corretora`, `Produto`, `Emissor`.
- **`ConsolidadoAgregado`**: `int Quantidade`, `decimal TotalAportado`, `decimal TotalLiquido`.
- **`ConsolidadoGrupoItem`**: `Guid Id`, `string Nome`, `int Quantidade`, `decimal TotalAportado`, `decimal TotalLiquido`.

### 1.2 `IMovimentoRepository`

Dois métodos novos, `CancellationToken` obrigatório e último parâmetro:

- `Task<ConsolidadoAgregado> GetConsolidadoAsync(Guid ownerId, ConsolidadoFiltro filtro, CancellationToken cancellationToken)`
- `Task<List<ConsolidadoGrupoItem>> GetConsolidadoAgrupadoAsync(Guid ownerId, ConsolidadoFiltro filtro, DimensaoConsolidado dimensao, CancellationToken cancellationToken)`

### 1.3 `MovimentoRepository`

Um método privado monta a consulta base: `FindByCondition(..., false)` com `UserId == ownerId && EstaAtivo` e, para cada campo não nulo do filtro, o `Where` correspondente. Sem `Include`, sem filtro de Objetivo.

**`GetConsolidadoAsync`:** `GroupBy` por constante, projetando `Count`, `Sum(ValorAporte)` e `Sum(ValorLiquidoAtual)` e `SingleOrDefaultAsync`; resultado nulo (conjunto vazio) vira `ConsolidadoAgregado` zerado. Uma consulta só, sem `COALESCE` manual.

**`GetConsolidadoAgrupadoAsync`:** `switch` na dimensão com **quatro ramos explícitos**, cada um com `GroupBy` por `(Id da dimensão, Nome da dimensão)` (navegação para Tipo de Renda, Corretora, Produto ou Emissor), projetando `ConsolidadoGrupoItem` com os três agregados. Ordenação no banco: `TotalLiquido` decrescente, depois `Nome` e `Id`. Sem montar expressões dinamicamente.

Comentário curto: a ausência de filtro de saldo/Pendente é intencional (ADR 0022); a ordem define a ordem das linhas na tela.

### 1.4 Outros projetos

Adicionar os dois métodos (lançando `NotSupportedException`) em `Application.Test/Movimentos/TestDoubles/FakeMovimentoRepository.cs`. Os `Mock<IMovimentoRepository>` não precisam de ajuste.

### 1.5 Testes (`Persistence.Test/Movimentos/MovimentoRepositoryTests.cs`)

Seguir o estilo atual (InMemory). Criar um helper que receba as entidades de dimensão já criadas, para montar Movimentos em tipos, corretoras, produtos e emissores distintos, com `EstaAtivo`, `ValorAporte` e `ValorLiquidoAtual` definidos.

`GetConsolidadoAsync`:

- Soma e conta só ativos do usuário (inativo e de outro usuário ficam fora).
- Cada filtro isolado, e combinações de dois ou mais (interseção).
- `Pendente`, `Gasto` e `Reinvestido` (montados com `Encerrar`) ficam fora.
- Objetivo inativo **não** exclui o Movimento.
- Sem correspondência (inclusive `Guid.Empty` e ID de outro usuário): quantidade 0 e totais 0, sem exceção.
- Casos de precisão: soma de valores com centavos exata.

`GetConsolidadoAgrupadoAsync`:

- Uma linha por grupo, para cada uma das quatro dimensões (`Theory`), com Id, nome, quantidade e totais corretos.
- Respeita os filtros e o isolamento por usuário.
- Ordem: Total Líquido decrescente; empate por nome e depois Id.
- Agrupar por dimensão também filtrada: uma linha.
- Sem dados: lista vazia.
- A soma das linhas confere com o agregado da consulta sem agrupamento para o mesmo filtro.

**Validação manual contra MariaDB (obrigatória):** ao rodar a tela (parte 3), conferir no log do EF que as duas consultas foram traduzidas sem erro e que a soma das linhas confere com um `SELECT` manual. Em volume realista, `EXPLAIN` (gatilho do ADR 0022).

**Como validar:** `dotnet build` e `dotnet test Persistence.Test --filter "FullyQualifiedName~Consolidado"`.

**Critério de pronto:** testes verdes, nenhum arquivo em `Persistence/Migrations`, `Movimento` inalterado, `CancellationToken` obrigatório nos dois métodos.

---

## Passo 2: Application

Pastas `Application/Dashboards/Consolidado/` (tipos) e `Application/Dashboards/Queries/GetConsolidado/` (query), espelhando a 0017.

### 2.1 Tipos

- **`ConsolidadoParameters`** (`record`, estilo de `RentabilidadeParameters`): quatro `Guid?` de filtro e `DimensaoConsolidado? AgruparPor` com `[EnumDataType]`. Método `ToFiltro()` devolve o `ConsolidadoFiltro` do Persistence.
- **`ConsolidadoLinhaModel`** (`record`): `Guid? Id` e `string? Nome` (nulos na linha de total), `int Quantidade`, `decimal TotalAportado`, `decimal TotalLiquido` e `decimal PercentualLiquido`.
- **`ConsolidadoModel`** (`record`): `ConsolidadoLinhaModel Total` e `List<ConsolidadoLinhaModel> Grupos` (vazia sem agrupamento).

### 2.2 `GetConsolidadoQuery`

`IGetConsolidadoQuery` e `GetConsolidadoQuery(IRepositoryManager, ILoggerManager)` (sem relógio). `ExecuteAsync(Guid ownerId, ConsolidadoParameters parametros, CancellationToken cancellationToken)` retorna `ConsolidadoModel`.

1. `CommandValidator.Validate(parametros)` **fora do `try`**; se não houver nenhum filtro **e** nenhum `AgruparPor`, `CommandValidationException("Informe ao menos um filtro ou um agrupamento.")`.
2. Sem agrupamento: `GetConsolidadoAsync`; o total tem `PercentualLiquido` igual a 1 quando o conjunto tem líquido diferente de zero, senão 0; `Grupos` vazia.
3. Com agrupamento: `GetConsolidadoAgrupadoAsync`; o **total é a soma das linhas** (quantidade, aportado e líquido); cada grupo recebe `PercentualLiquido = TotalLiquido do grupo / TotalLiquido total`, ou 0 quando o total for zero. A ordem do repositório é preservada.
4. `catch (Exception ex) when (ex is not OperationCanceledException)`: log e relança (padrão das queries).

Observações: o percentual não é arredondado (só exibição); os percentuais podem não somar exatamente 1 no Web por causa do arredondamento.

### 2.3 Testes (`Application.Test/Dashboards/Queries/GetConsolidado/GetConsolidadoQueryTests.cs`)

Estilo dos testes de queries atuais (`Mock<IMovimentoRepository>`, `Mock<ILoggerManager>`).

- Sem filtro: `CommandValidationException`, **sem consultar** o repositório.
- `AgruparPor` fora do enum: `CommandValidationException`.
- Sem agrupamento: repassa `ownerId`, o filtro mapeado e o token ao repositório; `Total` com os valores do agregado; `Grupos` vazia.
- Com agrupamento: chama só o método agrupado, com a dimensão certa (`Theory` das quatro).
- Total igual à soma das linhas; percentuais corretos (por exemplo, 600 e 400 de 1.000 → 0,6 e 0,4).
- Total líquido zero: percentuais 0, sem exceção.
- Conjunto vazio: total zerado e lista vazia.
- Ordem do repositório preservada.
- Falha do repositório: loga com o nome da query e relança; cancelamento relançado sem `LogError`.
- Mapeamento de `ConsolidadoParameters.ToFiltro()` leva os quatro campos.

**Como validar:** `dotnet build`, `dotnet test Application.Test --filter "FullyQualifiedName~Consolidado"` e `dotnet test` completo (o teste de reflexão do ADR 0017 deve continuar verde; o registro por convenção é conferido na parte 2).

**Critério de pronto:** testes verdes, `CancellationToken` obrigatório, nenhuma consulta fora do repositório, total sempre igual à soma das linhas.

---

## Partes 2 e 3 (a detalhar)

- **Parte 2, Service:** `GET api/dashboards/consolidado?tipoRendaId=&corretoraId=&produtoId=&emissorId=&agruparPor=` no `DashboardsController` (já com `[RequestValidation]`), `ConsolidadoFiltroDto` com `ToParameters()`, DTOs de resposta (`total` e `grupos`, percentual como fração), testes de controller, DTO, contrato JSON e registro; validação manual com `curl`, inclusive 422 (sem filtro, GUID malformado, `agruparPor` inválido) e isolamento entre usuários.
- **Parte 3, Web e fechamento:** cliente da Api, página `/Dashboards/Consolidado` (quatro selects com `--`, "Agrupar por", Limpar filtro, filtros na URL, tabela do total e das linhas com a coluna **Percentual**, notas sobre `Pendente` e Objetivo inativo), formatador de percentual reaproveitando o existente, item no menu, roteiro ponta a ponta, documentação e ADR 0022 como **Aceita**.

## Fora de escopo

Multiseleção, agrupamento por mais de uma dimensão, gráficos, exportação, inclusão de encerrados, persistência do filtro, qualquer alteração de schema, Domain ou LumiaFoundation.
