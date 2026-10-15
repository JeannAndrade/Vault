# Especificação técnica 0013: Distribuição do Valor Líquido por capital e tempo (Vault)

- Data: 2026-10-14
- Status: aprovada para implementação
- ADR relacionado: [0013](../adr/0013-distribuicao-do-valor-liquido.md)
- Repositório afetado: **Vault** (Domain, Persistence, Application, Service, Presentation/Web). **Sem alteração no LumiaFoundation, no schema e sem migration.**

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.
> O documento é entregue por camadas. Esta parte cobre o cabeçalho, o Passo 0 (ADR) e o Passo 1 (Domain).

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

_(continua: Passo 2, Persistence)_
