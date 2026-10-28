# Especificação técnica 0020: Projeção do Objetivo (Vault)

- Data: 2026-10-27
- Status: **em elaboração (parte 1 de 4: Passos 0 e 1, ADR e Domain)**
- ADR relacionado: [0021](../adr/0021-projecao-de-objetivo.md)
- Pré-requisito: **spec 0019 (encerramento de Movimento) implementada**, pelo menos o Passo 1 (Domain) e a migration, porque a projeção usa `MotivoEncerramento`.
- Repositório afetado: **Vault** (Domain, Application, Service, Presentation/Web). **Sem schema, sem migration, sem dependência nova e sem alteração no LumiaFoundation.**

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.
> **Partes:** 1 = Passos 0 e 1 (este arquivo); 2 = Passo 2 (Application); 3 = Passo 3 (Service); 4 = Passos 4 e 5 (Web e fechamento).

---

## 1. Resumo

Cada Objetivo ativo passa a mostrar **em quantos meses a Meta será atingida**, em três cenários (ADR 0021):

| Cenário | Aporte mensal | Rentabilidade |
| --- | --- | --- |
| Conservador | realizado | 0% |
| Realista | realizado | histórica |
| Planejado | `AporteMensal` do Objetivo | histórica |

O cálculo é uma função pura no Domain, sem banco e sem relógio. A tabela de Objetivos da tela inicial também passa a usar o **saldo único** (Movimentos ativos e encerrados como `Pendente`) em `Qtde Movimentos`, `Total Aportado`, `Total Líquido` e `% Meta`.

Esta parte entrega o ADR como "Proposta" e todo o Domain: a regra do saldo na entidade e o `ProjecaoObjetivoCalculador` com os testes.

## 2. Decisões aprovadas

| Item | Decisão |
| --- | --- |
| D1 | **Saldo** = Movimentos **ativos** ou encerrados como **`Pendente`**; `Gasto` e `Reinvestido` não contam. A regra mora num único lugar, `Movimento.CompoeSaldoDoObjetivo`, e é reutilizada pelo resumo e pela projeção |
| D2 | Serviço de domínio **estático e puro** (`ProjecaoObjetivoCalculador`), no estilo do `RentabilidadeCalculador` e do `DistribuicaoValorLiquidoCalculador`: sem I/O e sem relógio; "hoje" entra como parâmetro |
| D3 | Entrada por **record leve** (`MovimentoParaProjecao`), como o `AporteParaDistribuicao`: o Domain não depende de `Include` nem de entidade rastreada, e o teste monta os dados sem montar entidades |
| D4 | Saída por records: `ResultadoProjecao` com três `CenarioProjecao`, cada um com `SituacaoProjecao`, meses e data prevista |
| D5 | **Aporte realizado:** `ValorAporte` com `EhReinvestimento = false`, ativos ou encerrados, em até **12 meses completos anteriores ao mês corrente**, dividido pelos meses da janela (meses sem aporte contam zero) |
| D6 | **Taxa histórica:** só Movimentos **ativos**, ponderada por capital e tempo; período de cada Movimento termina em `DataAtualizacaoValorLiquido`; tempo médio ponderado abaixo de **90 dias** resulta em 0% e baixa confiança; limitada a **0% até 30% ao ano** |
| D7 | **Prazo** pela forma fechada; resultado arredondado **para cima**, em meses; acima de **1.200 meses** vira "mais de 100 anos" |
| D8 | Dinheiro em `decimal`; `double` **só** nos logaritmos e potências, com verificação de valor finito |
| D9 | **Dado inválido não derruba a tela inicial:** valores fora do esperado (aporte menor ou igual a zero, líquido negativo, aporte planejado negativo) são tratados de forma defensiva, sem exceção. Só lista nula lança `ArgumentNullException` |
| D10 | Constantes nomeadas e públicas, num só lugar (janela de 12 meses, mínimo de 90 dias, teto de 30%, limite de 1.200 meses) |
| D11 | `DataPrevista` é o **primeiro dia** do mês previsto; a tela mostra só mês e ano |

## 3. Atenção antes de começar

1. **O Domain não usa relógio nem lê configuração.** "Hoje" é parâmetro; a Application o lê do `IDateTimeProvider` (parte 2).
2. **O Domain não referencia nenhuma outra camada.** O cálculo recebe tipos do próprio Domain.
3. **Determinismo:** o mesmo conjunto de entradas dá sempre o mesmo resultado. Nada de `DateTime.UtcNow`, `Random` ou ordem dependente de coleção (a soma não depende da ordem dos itens).
4. **Aritmética:** `Math.Pow` e `Math.Log` trabalham em `double`; toda conversão de `decimal` para `double` acontece na fronteira do cálculo, e o resultado só é usado se for finito (`double.IsFinite`). Valores monetários devolvidos permanecem `decimal`, arredondados a 2 casas (`AwayFromZero`).
5. **Dados reais são imperfeitos:** `DataAtualizacaoValorLiquido` pode ser anterior à `DataInvestimento` (o período vira zero), valores podem ser zero, e um Objetivo pode não ter nenhum Movimento. O calculador **não lança exceção** nesses casos (D9).
6. **Sem dependência nova** e sem alteração de contrato da LumiaFoundation.
7. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo (parte 4). O ADR 0014 passa a constar como complementado pelo 0021 também no fechamento.

## 4. Ordem de implementação e entrega desta spec

Cada passo termina com `dotnet build` e `dotnet test` verdes antes do seguinte.

| # | Passo | Projeto | Entrega |
| --- | --- | --- | --- |
| 0 | ADR 0021 como "Proposta" | Docs | **parte 1 (este arquivo)** |
| 1 | Regra do saldo e `ProjecaoObjetivoCalculador` | Domain | **parte 1 (este arquivo)** |
| 2 | Resumo com saldo único, projeção e relógio | Application | parte 2 |
| 3 | DTOs do resumo | Service (Api) | parte 3 |
| 4 | Coluna "Previsão" e detalhe expansível | Presentation (Web) | parte 4 |
| 5 | Validação ponta a ponta, documentação e fechamento do ADR | Docs | parte 4 |

---

## Passo 0: ADR 0021 como "Proposta"

- Criar `Docs/adr/0021-projecao-de-objetivo.md` com o texto do arquivo entregue anteriormente, **Status: Proposta (2026-10-27)**.
- **Não** alterar o ADR 0014 agora: a anotação "Complementada pelo ADR 0021" entra no fechamento, junto com a mudança de comportamento (parte 4).

**Critério de pronto:** arquivo criado no padrão dos ADRs anteriores, status "Proposta".

---

## Passo 1: Domain

Sem pacote novo. O projeto já tem `ImplicitUsings` e `Nullable` habilitados.

### 1.1 `Movimento.CompoeSaldoDoObjetivo`

Em `Domain/Movimentos/Movimento.cs`, nova propriedade **somente leitura**:

- `bool CompoeSaldoDoObjetivo`: verdadeiro quando `EstaAtivo` é verdadeiro **ou** `MotivoEncerramento` é `Pendente`.
- Sem `set`, portanto **não mapeada** pelo EF (propriedades sem setter não são mapeadas por convenção). O teste do 1.5 confirma que não vira coluna.
- É a **única** definição da regra de saldo. O resumo (parte 2) e a montagem da entrada da projeção usam esta propriedade; nenhum outro código repete a condição.

Por que na entidade: é uma regra de negócio do Domain ("o que conta como patrimônio do Objetivo"), e duplicá-la na Application e no cálculo faria as duas divergirem na primeira mudança.

Por que usar `EstaAtivo ||` e não só o motivo: `EstaAtivo` continua sendo mantido pela invariante do ADR 0020 e é o campo que as consultas já usam; a condição fica correta mesmo em dados legados.

### 1.2 Tipos (`Domain/Objetivos/Projecao/`)

Todos `sealed record` imutáveis, no estilo da pasta `Domain/Movimentos/Distribuicao/`.

- **`MovimentoParaProjecao`** (entrada, um por Movimento do Objetivo): `decimal ValorAporte`, `decimal ValorLiquidoAtual`, `DateTime DataInvestimento`, `DateTime DataAtualizacaoValorLiquido`, `bool EhReinvestimento`, `bool EstaAtivo`, `bool CompoeSaldo`.
  - `CompoeSaldo` chega **pronto** (vem de `Movimento.CompoeSaldoDoObjetivo`), para o calculador não reimplementar a regra.
- **`SituacaoProjecao`** (enum, valores nesta ordem): `Prevista`, `Atingida`, `MaisDeCemAnos`, `SemPrevisao`. Os nomes viram texto na Api (parte 3), então são contrato.
- **`CenarioProjecao`**: `SituacaoProjecao Situacao`, `int? Meses` e `DateTime? DataPrevista`.
  - Construtores estáticos nomeados para evitar combinações inválidas: `Prevista(meses, dataPrevista)`, `Atingida()`, `MaisDeCemAnos()` e `SemPrevisao()`.
  - `Meses` e `DataPrevista` só têm valor em `Prevista`; nos demais são nulos.
- **`ResultadoProjecao`**:
  - `decimal Saldo` e `decimal SaldoPendente` (parcela do saldo que é `Pendente`);
  - `decimal? AporteRealizadoMensal` (nulo quando indisponível) e `int MesesObservados` (meses da janela do aporte realizado, zero quando indisponível);
  - `decimal AportePlanejadoMensal`;
  - `double TaxaAnualEstimada` e `double TaxaMensalEstimada`;
  - `bool BaixaConfianca`;
  - `CenarioProjecao Conservador`, `Realista` e `Planejado`.

### 1.3 `ProjecaoObjetivoCalculador`

Classe **estática** em `Domain/Objetivos/Projecao/`.

**Constantes públicas (D10):**

- `MesesJanelaAporte = 12`
- `DiasMinimosParaTaxa = 90`
- `TaxaAnualMaxima = 0.30` (`double`)
- `MesesMaximos = 1200`
- `DiasPorAno = 365` (mesma base dos ADRs 0013 e 0018)

**Assinatura:** `ResultadoProjecao Calcular(IReadOnlyList<MovimentoParaProjecao> movimentos, decimal meta, decimal aporteMensalPlanejado, DateTime hoje)`.

- Lista nula: `ArgumentNullException`. Lista vazia é válida.
- `hoje`: só a data conta (`.Date`); hora e `Kind` são ignorados.

#### Passo A: Saldo

- `Saldo` = soma de `ValorLiquidoAtual` dos itens com `CompoeSaldo`, tratando valor negativo como zero (D9).
- `SaldoPendente` = a mesma soma restrita aos itens com `CompoeSaldo` e **sem** `EstaAtivo`.
- Ambos arredondados a 2 casas.

#### Passo B: Aporte realizado

Trabalha com **índice de mês** `ano × 12 + mês`, para a aritmética de meses não depender de `AddMonths` nem do dia.

1. `indiceAtual` = índice do mês de `hoje`.
2. Elegíveis: itens com `EhReinvestimento = false`, `ValorAporte > 0` e `DataInvestimento.Date <= hoje.Date`. Entram ativos **e** encerrados, de qualquer motivo.
3. Sem elegíveis: indisponível (`AporteRealizadoMensal = null`, `MesesObservados = 0`).
4. `indicePrimeiro` = o menor índice entre os elegíveis.
5. `indiceInicio = max(indiceAtual − 12, indicePrimeiro)` e `MesesObservados = indiceAtual − indiceInicio`.
6. `MesesObservados = 0` (o primeiro aporte é do mês corrente): indisponível.
7. Soma dos `ValorAporte` dos elegíveis com índice em `[indiceInicio, indiceAtual − 1]`. Aportes anteriores à janela e os do mês corrente **não** entram.
8. `AporteRealizadoMensal = soma / MesesObservados`, arredondado a 2 casas (`AwayFromZero`). Pode ser zero (histórico sem aporte dentro da janela); isso **não** é "indisponível".

#### Passo C: Taxa histórica

1. Itens com `EstaAtivo` e `ValorAporte > 0` (os **ativos**, só eles).
2. Nenhum item: `TaxaAnualEstimada = 0`, `TaxaMensalEstimada = 0`, `BaixaConfianca = true`.
3. Para cada item: `dias_i = max(0, (DataAtualizacaoValorLiquido.Date − DataInvestimento.Date).Days)`.
4. `T` (em dias) = `Σ(A_i × dias_i) / ΣA_i`, onde `A_i` é o `ValorAporte`.
5. `T < 90` (`DiasMinimosParaTaxa`): taxa 0 e `BaixaConfianca = true`. `T = 90` já calcula.
6. Caso contrário: `fator = ΣV_i / ΣA_i` (`V_i` negativo vale zero) e `r = fator^(365 / T) − 1`.
7. Resultado não finito: taxa 0 e `BaixaConfianca = true`.
8. Limitar `r` a `[0, TaxaAnualMaxima]`: negativo vira 0 e acima do teto vira o teto. Esse ajuste **não** liga `BaixaConfianca`.
9. `TaxaMensalEstimada = (1 + r)^(1/12) − 1`.

#### Passo D: Cenários

Função interna `CalcularCenario(saldo, meta, aporte?, taxaMensal, hoje)`, aplicada três vezes:

| Cenário | `aporte` | `taxaMensal` |
| --- | --- | --- |
| Conservador | aporte realizado | 0 |
| Realista | aporte realizado | `TaxaMensalEstimada` |
| Planejado | `max(aporteMensalPlanejado, 0)` | `TaxaMensalEstimada` |

Ordem das decisões, a primeira que valer encerra:

1. `meta <= 0`: **`SemPrevisao`**.
2. `saldo >= meta`: **`Atingida`** (vale mesmo com aporte indisponível).
3. `aporte` indisponível: **`SemPrevisao`**.
4. Prazo `n` (em `double`, `S` saldo, `M` meta, `A` aporte e `i` taxa mensal):
   - `i = 0`: `A = 0` → **`SemPrevisao`**; senão `n = (M − S) / A`.
   - `i > 0`: `den = S·i + A`; `den <= 0` → **`SemPrevisao`**; senão `n = ln((M·i + A) / den) / ln(1 + i)`.
5. `n` não finito ou `n <= 0` (não deveria ocorrer com `S < M`): **`SemPrevisao`**.
6. `meses = Math.Ceiling(Math.Round(n, 9))`. O `Round` evita que ruído de ponto flutuante transforme 12,0000000001 em 13.
7. `meses > 1200`: **`MaisDeCemAnos`**.
8. Senão: **`Prevista`**, com `DataPrevista` = primeiro dia do mês de `hoje` somado de `meses` meses.

`A = 0` com `i > 0` é válido (o saldo cresce só por rendimento) e cai na fórmula `ln(M/S)/ln(1+i)`; com `S = 0` o `den` é zero e o resultado é **`SemPrevisao`**.

### 1.4 Por que assim (o "porquê")

- **Janela de meses completos:** o mês corrente está incompleto; contá-lo puxaria a média para baixo na maior parte do mês e faria a previsão piorar e melhorar conforme o dia do mês.
- **Índice de mês em vez de `AddMonths`:** comparar `ano × 12 + mês` não tem armadilha de fim de mês nem de fuso, e é trivial de testar.
- **Arredondar `n` antes do `Ceiling`:** é a parte mais traiçoeira da fórmula. Um prazo exato de 12 meses pode sair como 12,0000000000001 e virar 13 se não houver o `Round`.
- **Dado inválido sem exceção:** a tela inicial lista todos os Objetivos; um dado estranho em um deles não pode derrubar a tela inteira, e o resultado mais seguro é "sem previsão" ou valores tratados.
- **`CompoeSaldo` pronto na entrada:** a regra do saldo é do Domain e fica na entidade; o calculador é só aritmética.

### 1.5 Testes (`Domain.Test`)

Estilo dos testes atuais (`RentabilidadeCalculadorTests`): datas fixas, sem relógio, `Theory` com valores conhecidos. Usar `hoje = 2026-10-27` salvo indicação; um auxiliar `MovimentoParaProjecao` com padrões razoáveis para os cenários não repetirem os sete campos.

#### `MovimentoCompoeSaldoTests` (`Domain.Test/Movimentos/`)

- Ativo: verdadeiro.
- Encerrado como `Pendente`: verdadeiro.
- Encerrado como `Gasto` e como `Reinvestido`: falso.
- Persistence: teste no `MovimentoMappingTests` de que `CompoeSaldoDoObjetivo` **não** é propriedade mapeada do modelo.

#### `ProjecaoObjetivoCalculadorTests` (`Domain.Test/Objetivos/Projecao/`)

**Saldo**

| Cenário | Esperado |
| --- | --- |
| Ativo (100) + `Pendente` (50) | `Saldo` = 150 e `SaldoPendente` = 50 |
| `Gasto` e `Reinvestido` com valores altos | não entram |
| `ValorLiquidoAtual` negativo | conta como zero |
| Lista vazia | `Saldo` 0, sem exceção |
| Lista nula | `ArgumentNullException` |

**Aporte realizado** (`hoje = 2026-10-27`, janela de out/2025 a set/2026)

| Cenário | Esperado |
| --- | --- |
| Aportes novos em 2026-01-15 (1.200) e 2026-03-10 (600); reinvestimento em 2026-05-05 (5.000) | `MesesObservados` 9 (jan a set), realizado **200**; o reinvestimento fica de fora |
| Primeiro aporte em 2024-01 e um aporte de 2.400 em 2025-12-01 | janela cheia de 12 meses, realizado **200**; o aporte antigo não entra |
| Único aporte em 2026-10-05 (mês corrente) | indisponível: `null` e 0 meses |
| Único aporte em 2026-09-10 | 1 mês, realizado igual ao valor do aporte |
| Nenhum aporte novo (só reinvestimentos) | indisponível |
| Encerrados como `Gasto` | **entram** no realizado (foi dinheiro novo investido) |
| Aporte com data futura | ignorado |
| `ValorAporte` zero ou negativo | ignorado |
| Janela com meses sem aporte | meses sem aporte contam zero no divisor |

**Taxa histórica**

| Cenário | Esperado |
| --- | --- |
| 1 ativo: aporte 1.000, líquido 1.100, 365 dias | `TaxaAnualEstimada` 0,10 e `TaxaMensalEstimada` ≈ **0,007974140429** |
| 1 ativo: aporte 1.000, líquido 1.020, 90 dias | calcula (limite inclusivo): mensal ≈ **0,006714999783** |
| O mesmo com 89 dias | taxa 0 e `BaixaConfianca` verdadeiro |
| Dois ativos: (1.000, 1.100, 365 dias) e (3.000, 3.030, 90 dias) | `T` = 158,75 dias; mensal ≈ **0,006146799677** |
| Líquido 2.000 sobre 1.000 em 365 dias | limitada ao teto: anual 0,30 e mensal ≈ **0,022104450594**; `BaixaConfianca` **falso** |
| Líquido menor que o aporte (perda) | taxa 0; `BaixaConfianca` falso |
| Líquido zero | taxa 0, sem exceção |
| Só encerrados, nenhum ativo | taxa 0 e `BaixaConfianca` verdadeiro |
| `DataAtualizacaoValorLiquido` anterior à `DataInvestimento` | período zero, sem exceção |
| Movimentos encerrados com alto rendimento | **não** influenciam a taxa |
| Valor parado: atualização 200 dias após o investimento, e `hoje` 600 dias depois | o período usado é de **200** dias, não 600 (a taxa não depende de "hoje"): mesmo resultado para qualquer `hoje` |

**Prazo (cenários)**

| Entrada | Esperado |
| --- | --- |
| `S` 10.000, `M` 20.000, `A` 1.000, `i` 0 | **Prevista**, 10 meses, data prevista 2027-08-01 |
| `S` 10.000, `M` 20.000, `A` 1.000, `i` 1% | `n` ≈ 8,7446 → **9 meses** |
| `S` 0, `M` 12.000, `A` 1.000, `i` 1% | `n` ≈ 11,3894 → **12 meses** |
| `S` 10.000, `M` 20.000, `A` 0, `i` 1% | `n` ≈ 69,66 → **70 meses** |
| `S` 0, `M` 12.000, `A` 1.000, `i` 0 | `n` exato 12 → **12 meses** (e não 13: guarda do `Round`) |
| `S` 20.000, `M` 20.000 | **Atingida** nos três cenários, inclusive com aporte indisponível e planejado zero |
| `S` 25.000, `M` 20.000 | **Atingida** |
| `S` 0, `M` 120.000, `A` 100, `i` 0 | 1.200 meses → **Prevista** (limite inclusivo) |
| `S` 0, `M` 120.100, `A` 100, `i` 0 | 1.201 meses → **MaisDeCemAnos** |
| `A` 0, `i` 0 | **SemPrevisao** |
| `A` 0, `i` > 0, `S` 0 | **SemPrevisao** |
| `A` indisponível e `S < M` | **SemPrevisao** nos cenários que dependem do realizado; o **Planejado** continua calculado |
| `Meta` zero ou negativa | **SemPrevisao** |
| Planejado negativo | tratado como zero |
| `DataPrevista` | sempre o primeiro dia do mês; mês de virada de ano correto (por exemplo, `hoje` em 2026-12-15 com 2 meses → 2027-02-01) |

**Cenários entre si**

- Com taxa 0 (baixa confiança), **Realista = Conservador**.
- Realista nunca é mais lento que Conservador (mesma entrada, taxa maior ou igual).
- Planejado maior que realizado dá prazo menor ou igual.
- Planejado igual ao realizado dá o mesmo prazo do Realista.

**Geral**

- `hoje` com hora e `Kind` diferentes: mesmo resultado.
- Mesma entrada em ordem diferente: mesmo resultado (a soma não depende da ordem).
- Entrada com muitos itens e valores extremos (por exemplo, `decimal` bem grande): resultado finito ou "sem previsão", **sem exceção**.
- Datas padrão (`DateTime.MinValue` em `DataInvestimento` e em `DataAtualizacaoValorLiquido`, como nos fixtures de teste que não preenchem datas): **sem exceção**; o período é zero e a taxa fica em 0% com baixa confiança.
- Constantes públicas com os valores do ADR (12, 90, 0,30, 1.200, 365).

### 1.6 Como validar

- `dotnet build` da solution.
- `dotnet test Domain.Test --filter "FullyQualifiedName~Projecao"` e `--filter "FullyQualifiedName~CompoeSaldo"`.
- `dotnet test Persistence.Test --filter "FullyQualifiedName~MovimentoMapping"` (o teste de que `CompoeSaldoDoObjetivo` não é coluna).
- Conferência independente dos números da tabela: os valores de referência acima foram obtidos com a fórmula do ADR 0021 em uma calculadora externa; se o dev quiser, pode reproduzi-los numa planilha com `LN` e `ARRED.PARA.CIMA`.

**Critério de pronto:** testes verdes; calculador sem relógio, sem I/O e sem exceção para dado inválido; regra de saldo definida uma única vez, na entidade; `CompoeSaldoDoObjetivo` fora do schema (nenhuma migration nova).

---

## Partes 2 a 4 (a detalhar nas próximas entregas)

Escopo já definido no ADR 0021, para orientar:

- **Parte 2, Application:** `GetResumoObjetivosListQuery` recebe `IDateTimeProvider` e lê "hoje" uma vez; `ResumoObjetivoModel` passa a calcular `QtdeMovimentos`, `TotalAportado`, `TotalLiquido` e `PercentualMeta` sobre `CompoeSaldoDoObjetivo` e acrescenta o resultado da projeção (mapeando as entidades para `MovimentoParaProjecao`); `PercentualMeta` com proteção para `Meta` menor ou igual a zero (a criação já exige `Meta` de pelo menos 1, mas dado legado não pode derrubar a tela); sem consulta nova, `CancellationToken` já propagado; testes da query e do modelo.
- **Parte 3, Service:** campos aditivos em `ResumoObjetivoDto` (objeto `Projecao` com os três cenários, enum de situação como texto por atributo, como no ADR 0020), testes de DTO e de contrato JSON; os campos atuais mantêm nome e tipo.
- **Parte 4, Presentation e fechamento:** DTOs do Web (com a própria cópia do enum), coluna "Previsão" na tabela de Objetivos, detalhe expansível com os três cenários e as premissas (saldo com a parcela pendente, aporte realizado e planejado, rentabilidade estimada, aviso de baixa confiança), `TotaisObjetivos` sem previsão, formatador testado, roteiro ponta a ponta, documentação, ADR 0021 **Aceita** e ADR 0014 como complementado.

## Fora de escopo

- Inflação, vencimentos futuros e resgates previstos na projeção.
- TIR e rentabilidade ponderada pelo tempo.
- Mediana do aporte realizado.
- Histórico ou gráfico da projeção ao longo do tempo.
- Projeção por Corretora, Tipo de Renda ou qualquer outro agrupamento.
- Persistir a projeção (ela é calculada a cada consulta).
- Qualquer alteração de schema ou de LumiaFoundation.
