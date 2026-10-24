# ADR 0019: Fluxo Mensal (retorno previsto e investido por mês)

- Status: Proposta (2026-10-24)

## Contexto

O menu **Dashboards** (ADR 0018) ganha uma segunda tela, **Fluxo Mensal**, com duas visões de 12 meses:

1. **Retorno previsto:** quanto dinheiro será devolvido no mês atual e nos 11 seguintes (soma dos investimentos que vencem em cada mês). O objetivo é saber quanto estará disponível nos próximos meses para planejar novos investimentos.
2. **Investido:** quanto foi investido no mês atual e nos 11 anteriores, separando **investimento novo** de **reinvestimento** (`EhReinvestimento`). O objetivo é avaliar a constância dos aportes.

Os dados já existem em `Movimento`: `DataVencimento`, `DataInvestimento`, `ValorAporte`, `ValorLiquidoAtual`, `EhReinvestimento` e `EstaAtivo`. Não há necessidade de schema novo.

## Decisão

### Janelas e relógio

- "Mês" é o mês-calendário em **UTC**, com `IDateTimeProvider` (ADR 0011), como nas telas anteriores. O relógio é injetado na Application; o serviço de domínio recebe "hoje" como parâmetro e **não** usa relógio.
- Os 12 meses estão **sempre presentes**, inclusive os zerados, em **ordem cronológica crescente** nas duas visões.
- Retorno previsto: do mês atual até o mês atual + 11.
- Investido: do mês atual − 11 até o mês atual.
- Serviço de domínio puro e estático (`JanelaMensal`, nome a confirmar na spec) que devolve os 12 intervalos `[início, fim)` a partir de uma data. Testável sem mocks, como o `RentabilidadeCalculador`.

### Retorno previsto

- Elegibilidade: `UserId = dono`, `EstaAtivo = true` e `DataVencimento` não nula. **Objetivo inativo não filtra** (ADRs 0010, 0011, 0013 e 0018).
- Valor somado: `ValorLiquidoAtual`. Não projeta rendimento até o vencimento, então tende a **subestimar** o que voltará; e depende de o valor estar atualizado (ADR 0011).
- **Vencidos ainda ativos** (`DataVencimento` anterior ao mês atual, `EstaAtivo = true`) são **somados ao mês atual**, pois é dinheiro que já deveria estar disponível.
- Vencimentos além do mês atual + 11 ficam de fora. Movimentos sem vencimento (por exemplo, ações) ficam de fora, e a tela exibe uma nota informando isso.
- Cada mês traz o total e a quantidade de Movimentos.

### Investido

- Elegibilidade: `UserId = dono` e `DataInvestimento` dentro da janela. **Não filtra `EstaAtivo`:** o fato "investi em maio" não muda quando o Movimento é resgatado; filtrar apagaria o histórico e subestimaria a constância passada. **Objetivo inativo não filtra**, como nos ADRs anteriores.
- Valor somado: `ValorAporte`, separado por `EhReinvestimento`: **novo** (`false`), **reinvestimento** (`true`) e total.
- Esta é uma **exceção deliberada** à regra "somente investimentos ativos" e deve ficar visível na tela (nota curta), para não gerar a leitura de que os números são inconsistentes com o Retorno previsto.

### Agregação

O repositório filtra a janela **no banco** e devolve só uma **projeção leve**; a Application agrupa por mês em memória e preenche os 12 meses.

- Retorno: `(DataVencimento, ValorLiquidoAtual)`.
- Investido: `(DataInvestimento, ValorAporte, EhReinvestimento)`.

### Camadas

- **Domain:** `JanelaMensal` estático e puro (12 intervalos mensais a partir de uma data de referência).
- **Persistence:** `GetRetornoPrevistoAsync(ownerId, limiteSuperior, cancellationToken)` e `GetInvestidoAsync(ownerId, inicio, fim, cancellationToken)` no `IMovimentoRepository`, ambos sem tracking e sem `Include`. O retorno usa limite superior apenas (inclui vencidos antigos).
- **Application:** duas queries, `GetRetornoMensalQuery` e `GetInvestidoMensalQuery`. `CancellationToken` obrigatório; `catch (Exception ex) when (ex is not OperationCanceledException)`.
- **Service:** dois `GET` no `DashboardsController` existente, cada um com `[RequestValidation]`: `api/dashboards/retorno-mensal` e `api/dashboards/investido-mensal`. Sem parâmetros de entrada. `UserId` vem **somente** do token. Valores em `decimal`, mês como `AAAA-MM`.
- **Web:** página `/Dashboards/FluxoMensal`, submenu **Fluxo Mensal** em **Dashboards**. Para cada visão: tabela com totais e gráfico de barras com Chart.js (já presente no projeto, sem dependência nova), sendo o de Investido **empilhado** (novo + reinvestimento). Chamadas sequenciais à Api com **erro isolado por seção** (`ApiException` vira alerta só na seção; não autorizado não é engolido), como no ADR 0010.
- **Salvaguardas:** filtro por `UserId` em toda consulta; `CancellationToken` obrigatório e último parâmetro (o teste de reflexão do ADR 0017 cobre as novas interfaces); sem dependência nova.

### Persistência

Sem alteração de schema nem migration.

- Retorno: atendido pelo índice `IX_Movimentos_UserId_EstaAtivo_DataVencimento` (ADR 0010).
- Investido: atendido pelo índice `IX_Movimentos_UserId_DataInvestimento` (ADR 0009).

Gatilho de reavaliação: se o `EXPLAIN` e o tempo de resposta em volume realista mostrarem problema, abrir novo ADR.

## Alternativas consideradas

- **`GROUP BY` ano/mês no EF:** menos linhas, mas a tradução no Pomelo/MariaDB é frágil, a regra ficaria no SQL e os testes com InMemory não a validariam.
- **Calcular tudo no Web:** coloca regra de negócio no cliente e obriga a trafegar todos os Movimentos.
- **Endpoint único com as duas seções:** uma ida só, mas uma falha derruba a tela inteira.
- **Somar `ValorAporte` no retorno:** representa só o principal, não o que volta.
- **Mostrar `ValorAporte` e `ValorLiquidoAtual` lado a lado:** tabela mais larga, sem ganho claro hoje.
- **Vencidos ativos em linha separada ou ignorados:** a linha separada reconcilia melhor o total, mas foi preterida; ignorá-los esconderia dinheiro disponível.
- **Filtrar Objetivo ativo nesta tela:** seria a primeira consulta de Movimentos a divergir dos ADRs 0010, 0011, 0013 e 0018; fica para um eventual alinhamento geral.
- **Só tabela:** menos esforço, mas a constância é difícil de perceber sem o gráfico empilhado.

## Consequências

- O Retorno previsto é uma **estimativa conservadora**: usa o valor líquido de hoje, sem rendimento futuro, e herda a desatualização do valor (ADR 0011).
- O mês atual do Retorno **mistura** vencimentos do mês e vencidos ainda ativos; a tela não os distingue.
- O Investido inclui Movimentos já encerrados, enquanto o Retorno considera só os ativos; a diferença é intencional e sinalizada na tela.
- O relógio é UTC: entre 21h e 24h (BRT) do último dia do mês, "mês atual" já é o seguinte. Antes o efeito era de um dia; aqui é de um mês inteiro, mas por cerca de 3 horas. Aceito.
- Aportes com `DataInvestimento` futura dentro do mês atual entram no mês atual; futuros além dele ficam de fora.
- As queries carregam só a projeção da janela; aceitável na escala de controle pessoal.
- Serviço de domínio puro e agregação em memória, testáveis sem banco, com casos para meses zerados, virada de ano, vencidos ativos, Movimentos sem vencimento e isolamento por usuário.
- Mudança sem schema, sem migration e sem pacote novo; nenhum contrato da LumiaFoundation é alterado.
- Complementa os ADRs 0009, 0010, 0011, 0017 e 0018.

Fora do escopo: média ou meta de aporte mensal, filtros por Objetivo ou Corretora e projeção de rendimento até o vencimento.
