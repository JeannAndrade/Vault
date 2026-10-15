# Especificação técnica 0013: Distribuição do Valor Líquido por capital e tempo (Vault)

- Data: 2026-10-14
- Status: aprovada para implementação
- ADR relacionado: [0013](../adr/0013-distribuicao-do-valor-liquido.md)
- Repositório afetado: **Vault** (Domain, Persistence, Application, Service, Presentation/Web). **Sem alteração no LumiaFoundation, no schema e sem migration.**

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.
> O documento é entregue por camadas. Esta versão cobre o cabeçalho, o Passo 0 (ADR), o Passo 1 (Domain) e o Passo 2 (Persistence).

---

## 1. Resumo

O usuário escolhe um título (Tipo de Renda, Corretora, Produto e Emissor, os quatro obrigatórios) e informa o valor líquido total que ele tem hoje. O sistema distribui esse valor entre os Movimentos **ativos** do título, ponderando o valor de cada aporte e o tempo que ele ficou investido, e mostra um **preview**. Ao confirmar, a Api recalcula a distribuição e grava `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` de todos os aportes do lote, em uma única transação. A soma dos valores fecha exatamente no total informado.

## 2. Decisões aprovadas

| Item | Decisão |
|---|---|
| D1 | Algoritmo: taxa efetiva média, ganho projetado por aporte e normalização do ganho (seção do Passo 1) |
| D2 | Tempo em dias corridos: `(dataReferência − DataInvestimento).Days / 365`; `t` negativo é tratado como zero |
| D3 | Data de referência via `IDateTimeProvider`, só a data (UTC) |
| D4 | Quatro filtros obrigatórios (`TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId`); tipo de filtro próprio, sem reutilizar `MovimentoFiltro` |
| D5 | Só `EstaAtivo = true`, escopado por `UserId`; objetivo inativo não filtra |
| D6 | Sem paginação e sem limite de itens |
| D7 | Preview antes de gravar; na gravação a Api **recalcula** (sem carimbo do preview) |
| D8 | Controller novo, `DistribuicaoValorLiquidoController`, em `api/movimentos/distribuicao-valor-liquido` |
| D9 | Cálculo em serviço de domínio puro (sem I/O e sem relógio) |
| D10 | O aporte **mais recente** (ordem `DataInvestimento`, `Id`) absorve os centavos de arredondamento |
| D11 | Erros de regra viram 422: sem aportes na gravação, todos com `t = 0` e ganho diferente de zero, e valor negativo em algum aporte |
| D12 | Sem schema, sem migration, sem índice novo (gatilho: `EXPLAIN` em volume realista) |
| D13 | `DataAtualizacaoValorLiquido` = hoje (só a data) em todos os aportes do lote |
| D14 | `CancellationToken` **obrigatório** (sem `= default`) nas novas assinaturas |
| D15 | Tela nova, `/Movimentos/Distribuicao`, no menu "Manutenção" |

Regras de negócio adicionais: lucro negativo é permitido; `EhReinvestimento = true` entra normalmente; perda total (`V = 0`) zera todos os valores; `G = 0` zera todos os ganhos.

## 3. Atenção antes de começar

1. **`CancellationToken` obrigatório** nas novas assinaturas de repositório, query e command, e propagado de ponta a ponta. `OperationCanceledException` não é logada como erro (padrão de `GetMovimentosDesatualizadosQuery`).
2. **Dinheiro em `decimal`.** O `double` é usado só nos pesos do cálculo (`Math.Pow` não existe para `decimal`).
3. **Dependências entre camadas:** Service → Application → Persistence → Domain. O Domain não referencia nenhuma outra camada.
4. **Multi-tenant:** o `UserId` é sempre aplicado no repositório. Nenhum caminho pode ler ou gravar Movimentos de outro usuário.
5. **A Api nunca recebe valores calculados pelo Web.** Ela recalcula a partir do banco.
6. **Sem engolir exceções:** falhas são logadas e relançadas, como nas queries atuais. Erros de regra de negócio ficam **fora do `try`** (são erros do cliente, 422, não falha do serviço).
7. **Independente do ADR 0012:** a feature não usa a paginação nem o filtro da listagem. Pode ser implementada antes, depois ou em paralelo.
8. **Todo código, comentários, commits e documentação em português.**
9. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo.

## 4. Ordem de implementação

Cada passo termina com `dotnet build` e `dotnet test` verdes antes do seguinte.

| # | Passo | Projeto |
|---|---|---|
| 0 | ADR 0013 como "Proposta" | Vault (docs) |
| 1 | Calculador de domínio | Domain |
| 2 | Repositório | Persistence |
| 3 | Query de simulação e command de gravação | Application |
| 4 | Controller e DTOs | Service (Api) |
| 5 | Página, cliente da Api e menu | Presentation (Web) |
| 6 | Documentação final e fechamento do ADR | Vault |

---

## Passo 0: ADR 0013 como "Proposta"

- Criar `Docs/adr/0013-distribuicao-do-valor-liquido.md` com o texto do arquivo `0013-distribuicao-do-valor-liquido.md` entregue junto com esta especificação, **Status: Proposta (2026-10-14)**.
- Atualizar o índice de ADRs, se existir.

**Critério de pronto:** arquivo criado, no padrão dos ADRs anteriores, status "Proposta".

---

## Passo 1: Domain

Pasta: `Domain/Movimentos/Distribuicao/`. Sem pacote novo (o Domain já referencia `Lumia.Foundation.Core`).

### 1.1 Tipos

Todos imutáveis (`record` seladas), sem dependência de `Movimento`, para o cálculo ser testável sem montar entidades.

- **`AporteParaDistribuicao`**: `Guid Id`, `decimal ValorAporte`, `DateTime DataInvestimento`.
- **`AporteDistribuido`**: `Guid Id`, `decimal ValorAporte`, `decimal Ganho`, `decimal ValorLiquido`.
- **`ErroDistribuicao`** (enum): `SemAportes`, `SemBaseDeTempo`, `ValorNegativo`, `CalculoInvalido`.
- **`ResultadoDistribuicao`**: resultado com sucesso ou erro.
  - Sucesso: `IReadOnlyList<AporteDistribuido> Aportes` (na ordem `DataInvestimento`, `Id`), `decimal TotalAportado`, `decimal ValorLiquidoTotal`, `decimal GanhoTotal` e `double? TaxaEfetivaAnual`.
  - Erro: `ErroDistribuicao Erro`.
  - Propriedade `Sucesso` e fábricas estáticas (`Ok(...)`, `Falha(erro)`). Quem consome decide como transformar o erro em exceção; o Domain não conhece HTTP.

### 1.2 `DistribuicaoValorLiquidoCalculador`

Classe **estática** (função pura, sem estado nem dependências; evita registro em DI sem ganho de testabilidade). Método:

- `ResultadoDistribuicao Calcular(IReadOnlyList<AporteParaDistribuicao> aportes, decimal valorLiquidoTotal, DateTime dataReferencia)`

**Pré-condições (erro de programação, não de regra):** `valorLiquidoTotal < 0` lança `ArgumentOutOfRangeException`; `aportes` nulo ou algum `ValorAporte <= 0` lança `ArgumentException`. A validação de entrada do usuário é da camada Application.

**Algoritmo, nesta ordem:**

1. `aportes` vazio → `Falha(SemAportes)`.
2. Ordenar por `DataInvestimento` ascendente e depois `Id` ascendente (o último é o mais recente).
3. Para cada aporte: `t_i = max(0, (dataReferencia.Date − DataInvestimento.Date).Days / 365.0)`.
4. `S = ΣA_i` e `G = V − S`.
5. Se `V = 0`: todos com `ValorLiquido = 0` e `Ganho = −A_i`; `TaxaEfetivaAnual = −1`. Fim.
6. Se `G = 0`: todos com `Ganho = 0` e `ValorLiquido = A_i`; `TaxaEfetivaAnual = 0`. Fim.
7. `T = Σ(A_i × t_i) / S`. Se `T = 0` (todos com `t = 0`) e `G ≠ 0` → `Falha(SemBaseDeTempo)`.
8. `r = (V / S)^(1 / T) − 1` (em `double`).
9. `g_i = A_i × ((1 + r)^t_i − 1)` e `Σg`. Se algum valor não for finito ou `Σg = 0` → `Falha(CalculoInvalido)`.
10. `Ganho_i = G × g_i / Σg`, arredondado a 2 casas (`MidpointRounding.AwayFromZero`) para todos **menos o último**. O último recebe `G − Σ(ganhos arredondados dos demais)`, de modo que a soma dos ganhos seja exatamente `G`.
11. `ValorLiquido_i = A_i + Ganho_i`. Se algum for negativo → `Falha(ValorNegativo)`.
12. Retornar sucesso com `TaxaEfetivaAnual = r`.

Observação (decisão P1 do ADR): como o aporte mais recente pode ter `t = 0`, ele pode receber ou perder alguns centavos de ganho por causa da compensação. É esperado e aceito.

### 1.3 Testes (`Domain.Test`)

Remover o `UnitTest1.cs` de exemplo. Criar `Movimentos/Distribuicao/DistribuicaoValorLiquidoCalculadorTests.cs`, com data de referência fixa em todos os testes (por exemplo, `2026-10-14`) e aportes criados em dias exatos antes dela (365 dias = 1 ano).

- **Cenário do ADR** (aportes 1.200 com 730 dias, 2.000 com 365 dias e 800 com 0 dia; `V = 5.000`): a soma dos `ValorLiquido` é exatamente `5.000`; a soma dos ganhos é exatamente `1.000`; o aporte de 730 dias tem ganho maior que o de 365 dias; o ganho do aporte mais recente fica entre `-0,02` e `0,02`. Não fixar valores decimais exatos, só as propriedades.
- **Duas datas e valores iguais, `G = 1.000,01`:** os dois aportes têm a mesma ponderação; o primeiro fica com `500,01` e o último com `500,00` (compensação de centavos).
- **Aporte único:** recebe todo o ganho e `ValorLiquido = V`.
- **`G = 0`:** todos os ganhos zero; sem exceção.
- **`V = 0`:** todos os valores zero e ganhos `−A_i`.
- **Lucro negativo moderado** (por exemplo, `V = 0,9 × S`, com datas distintas): sucesso, todos os ganhos negativos e a soma exata.
- **Todos com `t = 0` e `G ≠ 0`:** `Falha(SemBaseDeTempo)`. Com `G = 0`: sucesso com ganhos zero.
- **Data de investimento futura:** tratada como `t = 0` (equivale a aporte de hoje).
- **Prejuízo extremo → `ValorNegativo`:** aportes de 100 com 1.095 dias e de 900 com 4 dias, `V = 50`. O resultado esperado é `Falha(ValorNegativo)`. Rodar e conferir; se um ajuste de dias mudar o resultado, ajustar `V` mantendo a intenção.
- **Ordem de entrada irrelevante:** a mesma lista embaralhada produz o mesmo resultado, na ordem `DataInvestimento`, `Id`.
- **Lista vazia:** `Falha(SemAportes)`.
- **Pré-condições:** `V` negativo → `ArgumentOutOfRangeException`; `ValorAporte <= 0` → `ArgumentException`.

**Como validar:** `dotnet test Domain.Test`.

**Critério de pronto:** testes verdes, sem dependência nova, sem uso de relógio dentro do calculador.

---

## Passo 2: Persistence

Sem pacote novo, sem alteração de mapeamento, sem migration. As colunas e os índices necessários já existem (`UserId`, `EstaAtivo` e as FKs com índice `(UserId, X)`).

### 2.1 `TituloFiltro`

Pasta `Persistence/Movimentos/`, ao lado de `MovimentoFiltro`. Record **selada**, imutável, com os quatro campos **não anuláveis**: `TipoRendaId`, `CorretoraId`, `ProdutoId` e `EmissorId` (todos `Guid`). Não reutilizar `MovimentoFiltro` (campos opcionais, inclui Objetivo e `EstaAtivo`). A obrigatoriedade dos quatro é validada na Application; o repositório apenas filtra e, com um `Guid.Empty`, devolve lista vazia.

### 2.2 `IMovimentoRepository` e `MovimentoRepository`

Novo método, **sem alterar** os existentes, com `CancellationToken` obrigatório:

- `Task<List<Movimento>> GetAtivosPorTituloAsync(Guid ownerId, TituloFiltro titulo, bool trackChanges, CancellationToken cancellationToken)`

Implementação:

- Parte de `FindByCondition(..., trackChanges)` (o mesmo ponto de partida do `GetAsync` atual), com os filtros: `UserId == ownerId`, `EstaAtivo` e os quatro campos de `titulo` (todos com `==`). O `UserId` é sempre aplicado, mesmo quando os demais filtros já identificam o título (isolamento multi-tenant).
- `Include` **somente de `Objetivo`**: é a única entidade relacionada que varia dentro de um título e a única de que o preview precisa. Não usar `QueryWithRelatedEntities()`, que força `AsNoTracking` e carrega cinco includes sem necessidade.
- Ordenação `DataInvestimento` ascendente e depois `Id` ascendente; `ToListAsync(cancellationToken)`.
- A mesma consulta serve à simulação (`trackChanges: false`) e à gravação (`trackChanges: true`), para a Application poder alterar as entidades e chamar `SaveAsync(cancellationToken)`.
- Comentário no método: a ordem do banco serve só para uma listagem estável. **O "aporte mais recente" é definido pelo calculador do Domain, que reordena em memória**, porque a ordenação de `Guid` no MariaDB (texto) pode divergir da de `Guid.CompareTo` do .NET nos empates de data. Nenhuma regra de negócio pode depender da ordem devolvida aqui.

### 2.3 Impacto em outros projetos

- `FakeMovimentoRepository` (`Application.Test/Movimentos/TestDoubles`) implementa `IMovimentoRepository` e deixa de compilar. Neste passo, acrescentar o método novo lançando `NotSupportedException`, no mesmo estilo dos demais métodos não usados. O Passo 3 o completa se algum teste precisar.
- Os mocks com Moq não são afetados.

### 2.4 Testes (`Persistence.Test/Movimentos/MovimentoRepositoryTests.cs`)

Criar um auxiliar de teste que monte Movimentos compartilhando as mesmas entidades relacionadas (o `AdicionarMovimento` atual cria entidades novas a cada chamada, o que não serve para testar um título). Cenários:

- Devolve somente os aportes **ativos** do título do usuário.
- **Isolamento por usuário:** um Movimento de outro usuário com os mesmos quatro IDs não é devolvido.
- **Cada filtro exclui:** quatro "vizinhos", cada um diferindo do título em apenas um dos campos (Tipo de Renda, Corretora, Produto ou Emissor), não são devolvidos.
- **Ignora inativos.**
- **Ordem:** `DataInvestimento` ascendente. Para datas iguais, verificar apenas que o conjunto devolvido está correto (não assumir a ordem de `Id`, que depende do provedor).
- **Carrega o `Objetivo`** (`Objetivo.Nome` preenchido).
- **`trackChanges`:** com `false`, as entidades ficam `Detached`; com `true`, ficam `Unchanged`, e uma alteração de `ValorLiquidoAtual` seguida de `SaveChangesAsync` persiste (recarregar em um contexto novo e conferir).
- **Título sem aportes:** lista vazia.

**Como validar:** `dotnet test Persistence.Test` e `dotnet test Application.Test` (este para confirmar que o fake compila).

**Critério de pronto:** testes verdes nos dois projetos; nenhum arquivo de migration alterado; `git status` sem mudanças em `Migrations/`.

---

_(continua: Passo 3, Application)_
