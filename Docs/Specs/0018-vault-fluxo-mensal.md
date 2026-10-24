# Especificação técnica 0018: Fluxo Mensal (Vault)

- Data: 2026-10-24
- Status: em andamento (parte 1 de 4: passos 0 e 1)
- ADR relacionado: [0019](../adr/0019-fluxo-mensal.md)
- Repositório afetado: **Vault** (Domain, Persistence, Application, Service, Presentation/Web). **Sem alteração no LumiaFoundation, no schema e sem migration.**

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.

---

## 1. Resumo

Novo submenu **Fluxo Mensal** em **Dashboards** (ADR 0018). A tela tem duas seções de 12 meses:

- **Retorno previsto:** soma de `ValorLiquidoAtual` dos Movimentos ativos que vencem no mês atual e nos 11 seguintes. Vencidos que ainda estão ativos entram no mês atual.
- **Investido:** soma de `ValorAporte` dos Movimentos com `DataInvestimento` no mês atual e nos 11 anteriores, separada em **novo** e **reinvestimento** (`EhReinvestimento`).

Cada seção traz uma tabela com totais e um gráfico de barras (Chart.js); o de Investido é empilhado.

## 2. Decisões aprovadas

| Item | Decisão |
| --- | --- |
| D1 | "Mês" é o mês-calendário em **UTC**, com `IDateTimeProvider` (ADR 0011); a virada de mês às 21h BRT é aceita |
| D2 | Os 12 meses sempre aparecem, inclusive os zerados, em ordem **cronológica crescente** nas duas seções |
| D3 | Retorno: `UserId = dono`, `EstaAtivo = true`, `DataVencimento` não nula; soma `ValorLiquidoAtual` |
| D4 | Vencidos ativos (vencimento anterior ao mês atual) somam no **mês atual**; vencimentos além do mês +11 e Movimentos sem vencimento ficam de fora, com nota na tela |
| D5 | Investido: `UserId = dono` e `DataInvestimento` na janela; soma `ValorAporte`; **não filtra `EstaAtivo`** (exceção deliberada, com nota na tela) |
| D6 | Investido separa **novo** (`EhReinvestimento = false`) e **reinvestimento** (`true`) e traz o total |
| D7 | Objetivo inativo **não filtra** em nenhuma das duas seções (ADRs 0010, 0011, 0013 e 0018) |
| D8 | O banco filtra a janela e devolve **projeção leve**; a Application agrupa por mês em memória e preenche os 12 meses |
| D9 | Dois `GET` no `DashboardsController` existente: `api/dashboards/retorno-mensal` e `api/dashboards/investido-mensal`; sem parâmetros de entrada |
| D10 | Serviço de domínio puro e estático gera os 12 intervalos a partir de "hoje" (recebido como parâmetro, sem relógio) |
| D11 | Erro **isolado por seção** no Web (`ApiException` vira alerta só na seção; não autorizado não é engolido) |
| D12 | Tabela mais gráfico com Chart.js (já no projeto), sem dependência nova |
| D13 | `CancellationToken` **obrigatório** (sem `= default`) e último parâmetro nas novas assinaturas |
| D14 | Sem schema, sem migration, sem índice novo (gatilho: `EXPLAIN` em volume realista) |

## 3. Atenção antes de começar

1. **`CancellationToken` obrigatório** nas novas assinaturas de repositório e query, propagado de ponta a ponta. `OperationCanceledException` não é logada como erro. O teste de reflexão do ADR 0017 já cobre as novas interfaces.
2. **Dinheiro em `decimal`**, também nos totais e na serialização. Não há `double` nesta feature.
3. **Dependências entre camadas:** Service → Application → Persistence → Domain. O Domain não referencia nenhuma outra camada.
4. **Multi-tenant:** o `UserId` vem **somente** do token e é sempre aplicado no repositório.
5. **O Domain não usa relógio.** Quem lê "hoje" é a Application, via `IDateTimeProvider`, e passa a data ao Domain.
6. **Sem engolir exceções:** falhas são logadas e relançadas, como nas queries atuais.
7. **Sem dependência nova** e sem alteração de contrato da LumiaFoundation.
8. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo.

## 4. Ordem de implementação e entrega desta spec

Cada passo termina com `dotnet build` e `dotnet test` verdes antes do seguinte.

| # | Passo | Projeto | Entrega |
| --- | --- | --- | --- |
| 0 | ADR 0019 como "Proposta" | Vault (docs) | **parte 1 (este arquivo)** |
| 1 | Janela mensal | Domain | **parte 1 (este arquivo)** |
| 2 | Repositório | Persistence | parte 2 |
| 3 | Queries de retorno e investido | Application | parte 2 |
| 4 | Controller e DTOs | Service (Api) | parte 3 |
| 5 | Página, cliente da Api, gráficos e menu | Presentation (Web) | parte 4 |
| 6 | Documentação final e fechamento do ADR | Vault | parte 4 |

---

## Passo 0: ADR 0019 como "Proposta"

- Criar `Docs/adr/0019-fluxo-mensal.md` com o texto do arquivo `0019-fluxo-mensal.md` entregue anteriormente, **Status: Proposta (2026-10-24)**.
- Atualizar o índice de ADRs, se existir (hoje não há).

**Critério de pronto:** arquivo criado, no padrão dos ADRs anteriores, status "Proposta".

---

## Passo 1: Domain

Pasta: `Domain/Movimentos/FluxoMensal/`. Sem pacote novo.

### 1.1 Tipos

- **`IntervaloMensal`**: `record` selada e imutável.
  - `int Ano`, `int Mes` (1 a 12), `DateTime Inicio` (primeiro dia do mês, 00:00, **inclusivo**) e `DateTime Fim` (primeiro dia do mês seguinte, 00:00, **exclusivo**).
  - Método `bool Contem(DateTime data)`: verdadeiro quando `Inicio <= data < Fim`. Usa o valor completo (data e hora), sem `.Date`, porque o intervalo é semiaberto e isso evita o erro clássico do "último dia do mês às 23:59".
- **`JanelaMensal`**: classe **estática** (função pura, sem estado, sem relógio e sem dependências; como o `RentabilidadeCalculador`).
  - Constante pública: `QuantidadeMeses = 12`.
  - `IReadOnlyList<IntervaloMensal> Proximos(DateTime hoje)`: do mês de `hoje` até `hoje` + 11 meses, em ordem crescente (usada no **Retorno previsto**).
  - `IReadOnlyList<IntervaloMensal> Anteriores(DateTime hoje)`: de `hoje` − 11 meses até o mês de `hoje`, em ordem crescente (usada no **Investido**).

Por que um serviço de domínio e não código solto na query: a regra "quais são os 12 meses" é a parte mais propensa a erro desta feature (virada de ano, fevereiro, fronteira do mês) e testá-la isolada, sem banco nem relógio, é barato. A query fica só com a soma e o agrupamento.

Por que `Fim` exclusivo: a consulta ao banco usa `data >= Inicio && data < Fim`, que funciona igual para `DateTime` com hora e para datas puras, e permite montar os 12 intervalos sem lacunas nem sobreposição (o `Fim` de um mês é o `Inicio` do seguinte).

### 1.2 Algoritmo

1. Normalizar a referência: `primeiroDoMes = new DateTime(hoje.Year, hoje.Month, 1)`. Hora e `Kind` de `hoje` são ignorados; só ano e mês contam.
2. `Proximos`: para `i` de 0 a 11, o intervalo começa em `primeiroDoMes.AddMonths(i)` e termina em `primeiroDoMes.AddMonths(i + 1)`.
3. `Anteriores`: para `i` de −11 a 0, o mesmo cálculo, com `i` negativo para trás.
4. `Ano` e `Mes` vêm de `Inicio`.
5. Os `DateTime` gerados têm `Kind = Unspecified`, para casar com os valores lidos do banco (sem conversão de fuso). **Conferir no passo 2** o `Kind` com que o EF materializa `DataInvestimento` e `DataVencimento`; se for outro, ajustar aqui e nos testes.

Observação: `AddMonths` a partir do **dia 1** evita o problema do dia 31 (por exemplo, `31/01 + 1 mês`). Por isso o ponto de partida é sempre o primeiro dia do mês.

### 1.3 Testes (`Domain.Test`)

Criar `Movimentos/FluxoMensal/JanelaMensalTests.cs` e `IntervaloMensalTests.cs`, no estilo dos testes do `RentabilidadeCalculadorTests` (datas fixas, sem relógio).

`JanelaMensalTests`:

| Cenário | `hoje` | Esperado em `Proximos` | Esperado em `Anteriores` |
| --- | --- | --- | --- |
| Meio do ano | 2026-10-24 | 2026-10 a 2027-09 | 2025-11 a 2026-10 |
| Virada de ano (dezembro) | 2026-12-15 | 2026-12 a 2027-11 | 2026-01 a 2026-12 |
| Janeiro | 2026-01-31 | 2026-01 a 2026-12 | 2025-02 a 2026-01 |
| Fevereiro bissexto | 2028-02-29 | 2028-02 a 2029-01 | 2027-03 a 2028-02 |

- Em todos os cenários: lista com **12** itens, em ordem **crescente**, `Ano` e `Mes` batendo com a tabela, e o primeiro/último `Inicio` no dia 1 às 00:00.
- **Sem lacunas:** para cada item exceto o último, `Fim` é igual ao `Inicio` do seguinte.
- **Fevereiro bissexto:** o `Fim` de 2028-02 é 2028-03-01 (29 dias dentro do intervalo).
- **Hora e `Kind` ignorados:** `hoje` às `23:59:59` e às `00:00:00` do mesmo dia dão a mesma janela; `Kind` Utc, Local e Unspecified também.
- **Mês atual é o primeiro em `Proximos` e o último em `Anteriores`.**
- **`QuantidadeMeses` vale 12.**

`IntervaloMensalTests` (`Contem`):

- `Inicio` exato → verdadeiro.
- `Fim` exato → **falso** (pertence ao mês seguinte).
- Último instante do mês (`Fim − 1 tick`, ou dia 31 às 23:59:59) → verdadeiro.
- Um dia antes do `Inicio` → falso.
- Data no meio do mês → verdadeiro.

**Como validar:** `dotnet test Domain.Test --filter "FullyQualifiedName~FluxoMensal"`.

**Critério de pronto:** testes verdes, sem dependência nova, sem relógio e sem I/O no Domain, 12 meses contíguos em todos os cenários.

---

## Partes 2 a 4 (a detalhar nas próximas entregas)

Escopo já definido no ADR 0019, para orientar:

- **Parte 2, Persistence e Application:**
  - `GetRetornoPrevistoAsync(ownerId, limiteSuperior, cancellationToken)` e `GetInvestidoAsync(ownerId, inicio, fim, cancellationToken)` no `IMovimentoRepository`, sem tracking, sem `Include`, devolvendo projeções leves (`(DataVencimento, ValorLiquidoAtual)` e `(DataInvestimento, ValorAporte, EhReinvestimento)`).
  - `GetRetornoMensalQuery` e `GetInvestidoMensalQuery`: lêem "hoje" pelo `IDateTimeProvider`, usam `JanelaMensal`, agrupam em memória, somam os vencidos ativos no mês atual e preenchem os 12 meses.
- **Parte 3, Service:** dois `GET` no `DashboardsController` com `[RequestValidation]`, DTOs com `decimal`, mês como `AAAA-MM` e quantidade de Movimentos por mês no Retorno.
- **Parte 4, Presentation e fechamento:** cliente da Api, página `/Dashboards/FluxoMensal`, tabelas, gráficos (barras no Retorno; empilhado no Investido), notas sobre Movimentos sem vencimento e sobre o Investido incluir encerrados, erro isolado por seção, submenu em **Dashboards**, documentação final e ADR 0019 para **Aceita**.

## Fora de escopo

- Média ou meta de aporte mensal.
- Filtros por Objetivo ou Corretora.
- Projeção de rendimento até o vencimento.
- Distinguir, no mês atual do Retorno, os vencimentos do mês dos vencidos ainda ativos.
- Qualquer alteração de schema ou de LumiaFoundation.
