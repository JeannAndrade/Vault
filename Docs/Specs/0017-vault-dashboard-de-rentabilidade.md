# Especificação técnica 0017: Dashboard de Rentabilidade (Vault)

- Data: 2026-10-20
- Status: implementada (fechamento em 2026-10-20)
- ADR relacionado: [0018](../adr/0018-dashboard-de-rentabilidade.md)
- Repositório afetado: **Vault** (Domain, Persistence, Application, Service, Presentation/Web). **Sem alteração no LumiaFoundation, no schema e sem migration.**

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.

---

## 1. Resumo

Novo menu **Dashboards**, com o submenu **Rentabilidade**. A tela mostra, para cada Tipo de Renda, uma tabela com os top N Movimentos (padrão 25, configurável na tela) ordenados pela rentabilidade anualizada: **Maiores rentabilidades** (padrão, decrescente) ou **Menores rentabilidades** (crescente). As três rentabilidades (acumulada, diária e anualizada) são **calculadas** a partir de `ValorAporte`, `ValorLiquidoAtual`, `DataInvestimento` e `DataAtualizacaoValorLiquido`, com as mesmas fórmulas das macros VBA da planilha do usuário.

## 2. Decisões aprovadas

| Item | Decisão |
| --- | --- |
| D1 | Fórmulas do VBA: acumulada `V/A − 1`; diária `(1+acum)^(1/dias) − 1` (composta); anualizada `(1+acum)^(365/dias) − 1` |
| D2 | `dias = (DataAtualizacaoValorLiquido.Date − DataInvestimento.Date).Days`; o cálculo **não usa "hoje"** nem relógio |
| D3 | Base de tempo: dias corridos por 365 (como o ADR 0013); sem dias úteis/252 |
| D4 | Casos sem cálculo (`null`, não 0): `dias <= 0`, `A <= 0`, `1 + acum < 0` ou resultado não finito |
| D5 | Perda total (`V = 0`, `dias > 0`) é cálculo **válido**: as três rentabilidades valem −100% |
| D6 | Cada linha é um **Movimento** (um aporte); sem agrupar por título |
| D7 | Elegibilidade: `EstaAtivo = true`, `UserId = dono`, `dias >= 30` (`DiasMinimosParaRanking`), cálculo válido; vencidos ativos entram; Objetivo inativo não filtra; Movimentos por cotação entram |
| D8 | Ordem: anualizada decrescente (Maiores, padrão) ou crescente (Menores); desempate por `Id` ascendente; comparação sem arredondar |
| D9 | Top N por Tipo de Renda: padrão 25, faixa 1 a 100, constantes nomeadas; fora da faixa: 422 |
| D10 | Parâmetros só na URL: `?top=25&ordem=maiores | menores`;`ordem` inválida: 422; nada é persistido |
| D11 | Tipo de Renda sem Movimentos elegíveis não aparece; grupos em ordem alfabética do nome |
| D12 | Títulos: "Campeões da renda <nome>" (Maiores) e "Menores rentabilidades da renda <nome>" (Menores) |
| D13 | Cálculo e ranking **em memória**, em serviço de domínio puro; a Api devolve as rentabilidades como **fração** (0,225 = 22,5%) e o Web formata em % |
| D14 | `DashboardsController` novo, `GET api/dashboards/rentabilidade`; não se usa o `MovimentosController` |
| D15 | Sem schema, sem migration, sem índice novo (gatilho: `EXPLAIN` em volume realista) |
| D16 | `CancellationToken` **obrigatório** (sem `= default`) nas novas assinaturas |
| D17 | Detalhe expansível na tela (Objetivo, Corretora e Valor do aporte), como na listagem de Movimentos |

## 3. Atenção antes de começar

1. **`CancellationToken` obrigatório** nas novas assinaturas de repositório, query e command, e propagado de ponta a ponta. `OperationCanceledException` não é logada como erro. O teste de reflexão do ADR 0017 já cobre as novas interfaces.
2. **Dinheiro em `decimal`.** O `double` é usado só no cálculo das razões (`Math.Pow` não existe para `decimal`) e nas rentabilidades resultantes.
3. **Dependências entre camadas:** Service → Application → Persistence → Domain. O Domain não referencia nenhuma outra camada.
4. **Multi-tenant:** o `UserId` é sempre aplicado no repositório. Nenhum caminho pode ler Movimentos de outro usuário.
5. **A tela nunca quebra por causa de uma linha ruim.** Dados corrompidos por edição direta no banco (por exemplo, `ValorAporte` zerado) resultam em "sem cálculo" e a linha sai do ranking; o calculador não lança exceção para dados de entrada.
6. **Sem engolir exceções:** falhas são logadas e relançadas, como nas queries atuais.
7. **Sem dependência nova** e sem alteração de contrato da LumiaFoundation.
8. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo.

## 4. Ordem de implementação

Cada passo termina com `dotnet build` e `dotnet test` verdes antes do seguinte.

| # | Passo | Projeto | Situação desta spec |
| --- | --- | --- | --- |
| 0 | ADR 0018 como "Proposta" | Vault (docs) | detalhado |
| 1 | Calculador de domínio | Domain | detalhado |
| 2 | Repositório | Persistence | próxima iteração |
| 3 | Query do ranking | Application | próxima iteração |
| 4 | Controller e DTOs | Service (Api) | próxima iteração |
| 5 | Página, cliente da Api e menu | Presentation (Web) | próxima iteração |
| 6 | Documentação final e fechamento do ADR | Vault | próxima iteração |

---

## Passo 0: ADR 0018 como "Proposta"

- Criar `Docs/adr/0018-dashboard-de-rentabilidade.md` com o texto do arquivo `0018-dashboard-de-rentabilidade.md` entregue junto com esta especificação, **Status: Proposta (2026-10-20)**.
- Atualizar o índice de ADRs, se existir (hoje não há).

**Critério de pronto:** arquivo criado, no padrão dos ADRs anteriores, status "Proposta".

---

## Passo 1: Domain

Pasta: `Domain/Movimentos/Rentabilidade/`. Sem pacote novo.

### 1.1 Tipos

- **`ResultadoRentabilidade`**: `record` selada e imutável, sem dependência de `Movimento` (o cálculo é testável sem montar entidades).
  - `double Acumulada`, `double Diaria`, `double Anualizada` (frações: 0,225 = 22,5%) e `int Dias`.
  - Propriedade calculada `bool ElegivelParaRanking`, verdadeira quando `Dias >= RentabilidadeCalculador.DiasMinimosParaRanking`.
- **`RentabilidadeCalculador`**: classe **estática** (função pura, sem estado, sem relógio e sem dependências; como o `DistribuicaoValorLiquidoCalculador`).
  - Constantes públicas: `DiasMinimosParaRanking = 30` e `DiasPorAno = 365`.
  - Método: `ResultadoRentabilidade? Calcular(decimal valorAporte, decimal valorLiquidoAtual, DateTime dataInvestimento, DateTime dataAtualizacaoValorLiquido)`. O retorno `null` significa **"sem cálculo"**.

Por que `null` e não o par `Resultado`/`Erro` do ADR 0013: aqui só existe um desfecho de falha, e quem consome (o ranking) apenas ignora a linha, sem precisar do motivo. Um tipo de erro seria cerimônia sem uso.

Por que o calculador **não lança exceção** para entrada inválida (diferente do 0013): os dados vêm do banco, e a API já os valida na entrada. Se uma linha fugir da regra por edição direta, o dashboard deve omiti-la, não responder 500.

### 1.2 Algoritmo, nesta ordem

1. `dias = (dataAtualizacaoValorLiquido.Date − dataInvestimento.Date).Days`. Se `dias <= 0` → `null`. (Só a data conta; horas e `Kind` são ignorados.)
2. Se `valorAporte <= 0` → `null`.
3. `razao = (double)valorLiquidoAtual / (double)valorAporte`. Se `razao < 0` (só ocorre com `valorLiquidoAtual` negativo) → `null`.
4. `Acumulada = razao − 1`.
5. `Diaria = Math.Pow(razao, 1.0 / dias) − 1`.
6. `Anualizada = Math.Pow(razao, DiasPorAno / (double)dias) − 1`.
7. Se `Acumulada`, `Diaria` ou `Anualizada` não for finito (overflow de `Math.Pow`, por exemplo razão enorme com poucos dias) → `null`.
8. Retornar `ResultadoRentabilidade` com os três valores e `Dias`.

Observações:

- **Perda total:** com `razao = 0` e `dias > 0`, `Math.Pow(0, y)` com `y > 0` devolve 0, então as três rentabilidades dão −1 (−100%) naturalmente, sem tratamento especial (D5). Cobrir com teste para travar o comportamento.
- **Equivalência com o VBA:** nos casos válidos as fórmulas são as mesmas das macros (que usam `Double`). A diferença deliberada está só nos casos de guarda: o VBA devolve 0 e o Vault devolve `null` (e, na perda total, −100% em vez de 0).
- **Sem arredondamento no Domain:** o arredondamento é só de exibição, no Web. O ranking compara os valores completos.
- **Elegibilidade** (`ElegivelParaRanking`) fica separada do cálculo: a rentabilidade de um Movimento com 10 dias continua calculável (útil para outras telas), mas ele não entra no ranking.

### 1.3 Testes (`Domain.Test`)

Criar `Movimentos/Rentabilidade/RentabilidadeCalculadorTests.cs`, no estilo dos testes do `DistribuicaoValorLiquidoCalculadorTests` (datas fixas, helper para montar as datas a partir de um número de dias). Comparações de `double` com `Assert.Equal(esperado, atual, precision: 12)`.

Valores de referência calculados fora do código (Python):

| Caso | Aporte | Valor líquido | Dias | Acumulada | Diária | Anualizada |
| --- | --- | --- | --- | --- | --- | --- |
| A | 1.000 | 1.100 | 365 | 0,10 | 0,000261157876 | 0,10 |
| B | 1.000 | 1.050 | 100 | 0,05 | 0,000488020685 | 0,194925809336 |
| C | 1.000 | 900 | 365 | −0,10 | −0,000288617289 | −0,10 |
| D | 1.000 | 1.210 | 730 | 0,21 | 0,000261157876 | 0,10 |

Cenários:

- **Casos A, B, C e D:** os três valores batem com a tabela e `Dias` é o esperado. O caso D mostra que 21% em 2 anos equivale a 10% ao ano.
- **Sem variação** (`V = A`, 200 dias): sucesso (não é `null`), com as três rentabilidades iguais a 0.
- **Perda total** (`V = 0`, 100 dias): sucesso, com as três rentabilidades iguais a −1.
- **Acumulada independe do prazo:** o mesmo par aporte/valor com 100 e 400 dias dá a mesma acumulada; a anualizada é maior no prazo menor (para ganho positivo).
- **Mesmo prazo, maior ganho:** valor líquido maior produz anualizada maior (monotonia, base do ranking).
- **`dias = 0`** (datas iguais): `null`.
- **Data de atualização anterior à de investimento:** `null`.
- **Só a data conta:** investimento em `2026-01-01 23:59` e atualização em `2026-01-31 00:01` resultam em `Dias = 30`.
- **`valorAporte = 0`** e **`valorAporte` negativo:** `null`.
- **`valorLiquidoAtual` negativo:** `null`.
- **Overflow:** aporte 1, valor líquido `1e30`, 1 dia: `null` (resultado não finito).
- **Elegibilidade:** `Dias = 29` → `ElegivelParaRanking` falso; `Dias = 30` → verdadeiro; `DiasMinimosParaRanking` vale 30.

**Como validar:** `dotnet test Domain.Test --filter "FullyQualifiedName~Rentabilidade"`.

**Critério de pronto:** testes verdes, sem dependência nova, sem relógio e sem I/O dentro do calculador, e nenhuma exceção para entradas inválidas.

---

## Passos 2 a 6 (a detalhar nas próximas iterações)

Escopo já definido no ADR 0018, para orientar a próxima iteração:

- **Passo 2, Persistence:** `GetAtivosParaRankingAsync(ownerId, cancellationToken)` no `IMovimentoRepository`, com `Include` de Tipo de Renda, Produto, Emissor, Corretora e Objetivo, sem tracking, `UserId` e `EstaAtivo` no filtro.
- **Passo 3, Application:** `GetRentabilidadesPorTipoRendaQuery` com parâmetros `Top` e `Ordem` (enum `OrdemRentabilidade`: `Maiores`, `Menores`); calcula, descarta `null` e inelegíveis, agrupa por Tipo de Renda, ordena, aplica o top N.
- **Passo 4, Service:** `DashboardsController` com `GET api/dashboards/rentabilidade?top=&ordem=`, validação (422) e DTOs com as rentabilidades em fração.
- **Passo 5, Presentation:** cliente da Api, página `/Dashboards/Rentabilidade`, formulário `GET`, tabelas por Tipo de Renda com detalhe expansível, formatação em % (`pt-BR`) e menu **Dashboards**.
- **Passo 6:** documentação final e ADR 0018 para **Aceita**.

## Fora de escopo

- Persistir o `top` como preferência do usuário.
- Rentabilidade por título agrupado, dias úteis/252 e marcação a mercado.
- Gráficos; a primeira versão é só tabela.
- Qualquer alteração de schema ou de LumiaFoundation.
