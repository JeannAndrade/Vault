# Especificação técnica 0018: Fluxo Mensal (Vault), parte 4 de 4

- Data: 2026-10-24
- Status: implementada (fechamento em 2026-10-24)
- ADR relacionado: [0019](../adr/0019-fluxo-mensal.md)
- Esta parte fecha a feature, continuando as partes [1](0018-vault-fluxo-mensal.md) (ADR e Domain), [2](0018-vault-fluxo-mensal-parte-2.md) (Persistence e Application) e [3](0018-vault-fluxo-mensal-parte-3.md) (Service). As decisões D1 a D14 e as "Atenções" da parte 1 continuam valendo.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e como validar.
> Todo código, comentário, commit e documentação em português.

---

## Escolhas desta parte (a confirmar)

| Item | Escolha |
| --- | --- |
| E1 | O Web tem **tipos próprios** (DTOs em `Presentation/Dashboards/`). O `Presentation.csproj` não referencia Application nem Service e **assim continua** (contrato só por HTTP). Os nomes espelham o contrato da parte 3 |
| E2 | **Um cliente** `IFluxoMensalApi` com dois métodos (um por endpoint), no estilo de `RentabilidadeApi`. A página faz as duas chamadas **em sequência**, cada uma independente da outra |
| E3 | **Erro isolado por seção** (D11, ADR 0010): `ApiException` de uma chamada vira alerta só naquela seção e a outra continua carregando. A exceção de não autorizado **não** é engolida |
| E4 | O rótulo do mês no Web é **`MM/AAAA`** (`2026-10` vira `10/2026`), montado por formatter estático, sem depender de ICU ou cultura. Entrada fora do formato volta **inalterada**, sem exceção |
| E5 | Gráficos com o **Chart.js já existente** (ADR 0016, `wwwroot/lib/chart.js`), sem dependência nova. Dados em atributo `data-*` como JSON e script em **arquivo estático** (`wwwroot/js`), sem script inline, igual ao gráfico de cotações |
| E6 | Retorno: barras simples. Investido: barras **empilhadas** (Novo e Reinvestimento) com legenda. As cores ficam no script e a diferença não depende só de cor (a legenda e a tabela trazem o nome) |
| E7 | Cada tabela tem uma **linha de total** (soma dos 12 meses), calculada no modelo da página |
| E8 | **Notas fixas** sob cada título, para a regra ficar visível: o Retorno exclui Movimentos sem vencimento, soma vencidos ainda ativos no mês atual e usa o Valor Líquido Atual (sem rendimento futuro); o Investido inclui Movimentos já encerrados (ADR 0019) |
| E9 | Um selo **"mês atual"** marca a primeira linha do Retorno e a última do Investido. Depende do contrato de ordem cronológica crescente (D2); um teste da página trava isso |
| E10 | Os valores trafegam como `decimal` até a tabela. Só no JSON do gráfico viram número JSON (`double` no navegador), o que é aceitável para exibição |
| E11 | A página **não recebe parâmetros** (sem query string): os 12 meses são decididos pelo servidor (E2 da parte 3) |
| E12 | Cada `<canvas>` tem `role="img"` e `aria-label`; a tabela ao lado serve de alternativa textual |
| E13 | Menu: item **Fluxo Mensal** no dropdown **Dashboards**, logo abaixo de **Rentabilidade** |

---

## Passo 5: Presentation (Web)

### 5.1 Tipos e cliente da Api (pasta `Presentation/Dashboards/`, namespace `Presentation.Dashboards`)

- **`RetornoMensalDto`** (`sealed record`, propriedades `init`, como `RentabilidadeItemDto`): `string Mes` (`AAAA-MM`), `decimal Total` e `int Quantidade`. Nomes iguais aos do contrato JSON da parte 3 (o desserializador do cliente casa sem diferenciar maiúsculas).
- **`InvestidoMensalDto`** (`sealed record`): `string Mes`, `decimal Novo`, `decimal Reinvestimento` e `decimal Total`.
- **`IFluxoMensalApi`** e **`FluxoMensalApi`** (`sealed`, construtor com `IApiConnection`, no estilo de `RentabilidadeApi`):
  - `Task<List<RetornoMensalDto>> GetRetornoMensalAsync(CancellationToken cancellationToken)`, caminho `api/dashboards/retorno-mensal`;
  - `Task<List<InvestidoMensalDto>> GetInvestidoMensalAsync(CancellationToken cancellationToken)`, caminho `api/dashboards/investido-mensal`;
  - GET sem corpo e sem query string; corpo nulo da Api vira lista vazia (`?? []`).

### 5.2 Formatação e dados do gráfico (pasta `Presentation/Dashboards/`)

**`FluxoMensalFormatter`** (classe estática e pura, no estilo de `CotacaoFormatter`):

- `string FormatarMes(string? mes)`: `"2026-10"` → `"10/2026"`. Valida o formato `AAAA-MM` (4 dígitos, hífen, mês de 01 a 12) **sem usar `DateTime.Parse`** nem cultura. Qualquer outra entrada (nula, vazia, mês 13, texto) devolve o **valor original** (ou texto vazio se nulo), sem lançar exceção: um rótulo estranho nunca pode quebrar a tela.

**`FluxoMensalGraficoDados`** (classe estática, no estilo de `CotacaoGraficoDados`):

- `string RetornoParaJson(IEnumerable<RetornoMensalDto> meses)` → objeto com `rotulos` (via `FormatarMes`) e `valores` (os `Total`).
- `string InvestidoParaJson(IEnumerable<InvestidoMensalDto> meses)` → objeto com `rotulos`, `novo` e `reinvestimento`.
- Mantém a ordem recebida e usa `System.Text.Json` (`JsonSerializer.Serialize` com objeto anônimo, como a classe de cotações); lista vazia gera arrays vazios.
- O JSON entra no HTML por atributo `data-*` com escape do Razor (o `@` já codifica as aspas); **nunca** em `@Html.Raw`.

### 5.3 Página `/Dashboards/FluxoMensal`

**Arquivos:** `Presentation/Pages/Dashboards/FluxoMensal.cshtml` e `FluxoMensal.cshtml.cs`, modelo `FluxoMensalModel(IFluxoMensalApi api)`.

**Modelo:**

- `List<RetornoMensalDto> Retornos { get; private set; } = []`, `List<InvestidoMensalDto> Investidos { get; private set; } = []`, `string? RetornoErro { get; private set; }` e `string? InvestidoErro { get; private set; }`.
- `OnGetAsync(CancellationToken cancellationToken)`, sem parâmetros de URL:
  1. `try`: `Retornos = await api.GetRetornoMensalAsync(cancellationToken)`; `catch (ApiException)`: `RetornoErro = "Não foi possível carregar o retorno previsto agora. Tente novamente em instantes."` e `Retornos` fica vazio.
  2. `try`: `Investidos = await api.GetInvestidoMensalAsync(cancellationToken)`; `catch (ApiException)`: `InvestidoErro = "Não foi possível carregar o valor investido agora. Tente novamente em instantes."` e `Investidos` fica vazio.
  3. **A segunda chamada ocorre mesmo que a primeira falhe.** Não capturar a exceção de não autorizado (E3): ela interrompe a página e segue para o `ApiUnauthorizedExceptionHandler`.
- **Totais (E7):** `decimal TotalRetorno` (soma dos `Total`), `int TotalQuantidadeRetorno`, `decimal TotalNovo`, `decimal TotalReinvestimento` e `decimal TotalInvestido`. Listas vazias dão 0.
- **Gráficos:** `string RetornoJson` e `string InvestidoJson` (via `FluxoMensalGraficoDados`).
- **Rótulos:** `string RotuloMes(string mes)` delega ao `FluxoMensalFormatter`, para a view não conhecer a regra.
- **Selo do mês atual (E9):** `bool EhMesAtualDoRetorno(int indice) => indice == 0` e `bool EhMesAtualDoInvestido(int indice) => indice == Investidos.Count - 1`, com comentário apontando D2 e o ADR 0019.

**View (`FluxoMensal.cshtml`):**

- `ViewData["Title"] = "Fluxo Mensal"` e `<h1 class="h3 mb-3">Fluxo Mensal</h1>`.
- **Seção 1, "Retorno previsto"** (`<h2 class="h5">`):
  - Nota fixa (`text-muted small`, E8): "Soma do Valor Líquido Atual dos Movimentos ativos que vencem em cada mês, nos próximos 12 meses. O mês atual inclui vencidos que ainda estão ativos. Movimentos sem vencimento (como ações) não entram. Não projeta rendimento até o vencimento."
  - `RetornoErro` não nulo: `alert alert-danger`. Lista vazia e sem erro: `<p class="text-muted">Nenhum dado disponível.</p>`.
  - Caso contrário: `<canvas id="grafico-retorno" role="img" aria-label="Gráfico de barras do retorno previsto por mês" data-pontos="@Model.RetornoJson">` e a tabela `table table-hover align-middle`: colunas **Mês**, **Movimentos** e **Retorno previsto**; valores numéricos com `text-end`; moeda `pt-BR` (`ToString("C", new CultureInfo("pt-BR"))`, como no Index de Movimentos); o selo "mês atual" (`badge text-bg-secondary`) na linha marcada; **linha de total** no `<tfoot>`.
- **Seção 2, "Investido"** (`<h2 class="h5">`):
  - Nota fixa (E8): "Soma do valor aportado em cada mês, nos últimos 12 meses, separando investimento novo de reinvestimento. Inclui Movimentos já encerrados: o histórico de aportes não some com o resgate."
  - Erro, vazio e canvas como na seção 1 (`id="grafico-investido"`, `aria-label` "Gráfico de barras empilhadas do valor investido por mês, separando novo e reinvestimento").
  - Tabela: colunas **Mês**, **Novo**, **Reinvestimento** e **Total**; selo "mês atual" na última linha; `<tfoot>` com os totais.
- **Scripts (E5):** na `@section Scripts`, `<script src="~/lib/chart.js/chart.umd.min.js">` e `<script src="~/js/fluxo-mensal-grafico.js">`, como `Cotacoes/Grafico.cshtml`.

### 5.4 Script do gráfico (`Presentation/wwwroot/js/fluxo-mensal-grafico.js`)

Arquivo estático, no estilo de `cotacao-grafico.js` (função autoexecutável, sem script inline, sem dependência nova):

- Trata **cada canvas de forma independente**: se `grafico-retorno` ou `grafico-investido` não existir (seção com erro), apenas pula aquela; nunca lança exceção por canvas ausente.
- Lê `canvas.dataset.pontos` com `JSON.parse`.
- **Retorno:** gráfico de barras (`type: 'bar'`), um dataset com o rótulo "Retorno previsto" e os `valores`.
- **Investido:** gráfico de barras com `x.stacked` e `y.stacked` verdadeiros e dois datasets, "Novo" (`novo`) e "Reinvestimento" (`reinvestimento`), com **cores distintas definidas no script** e **legenda visível**.
- Eixo Y e tooltip em moeda `pt-BR` (`toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })`), como no gráfico de cotações.
- `y.beginAtZero` verdadeiro: barras partem de zero (um eixo cortado distorceria a comparação entre meses).
- Sem animações personalizadas nem segunda biblioteca.

### 5.5 Menu (`Presentation/Pages/Shared/_Layout.cshtml`)

No dropdown **Dashboards** existente, logo após **Rentabilidade**, acrescentar `<li><a class="dropdown-item" asp-page="/Dashboards/FluxoMensal">Fluxo Mensal</a></li>` (E13).

### 5.6 Registro (`Presentation/Program.cs`)

Acrescentar `builder.Services.AddApiResourceClient<IFluxoMensalApi, FluxoMensalApi>();` junto dos outros clientes. O `using Presentation.Dashboards;` já existe.

### 5.7 Testes (`Presentation.Test`)

- **`Dashboards/FluxoMensalApiTests.cs`** (como `RentabilidadeApiTests`: `Mock<IApiConnection>`):
  - `GetRetornoMensalAsync` envia GET para `api/dashboards/retorno-mensal` e devolve a lista do mock (mesma instância); idem para `investido-mensal`;
  - corpo nulo devolve lista vazia, sem exceção, nos dois métodos;
  - repassa o `CancellationToken` nos dois métodos.
- **`Dashboards/FluxoMensalFormatterTests.cs`:**
  - `FormatarMes("2026-10")` = `"10/2026"`; `"2027-01"` = `"01/2027"`; `"2026-12"` = `"12/2026"`;
  - entradas inválidas voltam inalteradas e **sem exceção**: `"abc"`, `"2026-13"`, `"2026-00"`, `"26-10"`, `"2026/10"`, `""`;
  - `null` devolve texto vazio;
  - o resultado não muda com a cultura corrente (rodar sob uma cultura não latina, restaurando-a ao fim do teste).
- **`Dashboards/FluxoMensalGraficoDadosTests.cs`:**
  - `RetornoParaJson` traz `rotulos` formatados (`MM/AAAA`) e `valores` como números, na mesma ordem; um valor `1234.56` mantém as duas casas;
  - `InvestidoParaJson` traz `rotulos`, `novo` e `reinvestimento` alinhados pelo índice;
  - lista vazia gera arrays vazios em todas as propriedades;
  - a ordem recebida é preservada.
- **`Pages/Dashboards/FluxoMensalModelTests.cs`** (como `RentabilidadeModelTests`, com `Mock<IFluxoMensalApi>`):
  - **sucesso nas duas:** `Retornos` e `Investidos` preenchidas, os dois erros nulos, token repassado às duas chamadas, cada método chamado **uma vez**;
  - **falha só no Retorno** (`ApiException(HttpStatusCode.InternalServerError, "erro")`): `RetornoErro` preenchido, `Retornos` vazio, **`Investidos` carregado** e `InvestidoErro` nulo; não lança;
  - **falha só no Investido:** o espelho do anterior;
  - **falha nas duas:** os dois erros preenchidos, as duas listas vazias, não lança;
  - **a segunda chamada ocorre mesmo com a primeira falhando** (`Verify(..., Times.Once)`);
  - **não autorizado não é engolido:** a exceção de não autorizado da LumiaFoundation propaga, tanto se vier do Retorno quanto do Investido (usar o tipo que o `ApiUnauthorizedExceptionHandler` trata; confirmar o nome no pacote e, se ele derivar de `ApiException`, ajustar o `catch` para não capturá-lo);
  - **totais:** `TotalRetorno`, `TotalQuantidadeRetorno`, `TotalNovo`, `TotalReinvestimento` e `TotalInvestido` batem com a soma dos 12 itens; com listas vazias, todos 0; `TotalInvestido == TotalNovo + TotalReinvestimento`;
  - **selo do mês atual:** `EhMesAtualDoRetorno(0)` verdadeiro e `(1)` falso; `EhMesAtualDoInvestido` verdadeiro só para o último índice;
  - **JSON do gráfico:** `RetornoJson` e `InvestidoJson` refletem as listas carregadas e são arrays vazios quando a seção falha.
- A view e o script não têm teste unitário (não há no repositório para views; o script é coberto pela validação manual).

### 5.8 Como validar

```
dotnet build
dotnet test Presentation.Test --filter "FullyQualifiedName~FluxoMensal"
dotnet test
```

**Validação manual** (ambiente do `Docs/Docker.md`: `libman restore` antes do `docker compose up --build -d`, Web em `http://localhost:5000`):

1. **Dados de teste** (supondo hoje em **2026-10-24**; ajustar as datas ao mês real). Os Movimentos podem ser distintos ou reaproveitados; só os campos citados importam.

   *Retorno* (ativos, do usuário de teste):

   | Movimento | Vencimento | Valor líquido | Esperado |
   | --- | --- | --- | --- |
   | R1 | 2026-10-30 | 1.000 | mês atual |
   | R2 | 2026-09-15 (vencido, ainda ativo) | 500 | **soma no mês atual**: 1.500, 2 Movimentos |
   | R3 | 2026-12-10 (Objetivo **inativo**) | 2.000 | 12/2026 (Objetivo inativo não filtra) |
   | R4 | 2026-12-31 23:59 | 300 | 12/2026: total 2.300, 2 Movimentos |
   | R5 | 2027-09-30 | 250 | 09/2027 (último mês) |
   | R6 | 2027-10-01 | 700 | fora |
   | R7 | sem vencimento (ação) | 900 | fora |
   | R8 | 2027-01-05, **inativo** | 400 | fora |
   | R9 | qualquer, de **outro usuário** | 800 | fora |

   *Investido* (do usuário de teste):

   | Movimento | Data do investimento | Valor do aporte | Tipo | Esperado |
   | --- | --- | --- | --- | --- |
   | I1 | 2026-10-02 | 1.000 | novo | 10/2026 |
   | I2 | 2026-10-10 | 500 | reinvestimento | 10/2026 |
   | I3 | 2026-10-29 (futura, dentro do mês) | 100 | novo | 10/2026: novo 1.100, reinvestimento 500, total 1.600 |
   | I4 | 2026-09-30 | 200 | novo | 09/2026 |
   | I5 | 2026-05-15, **encerrado** (`EstaAtivo = false`) | 300 | reinvestimento | 05/2026 (conta) |
   | I6 | 2025-11-01 | 800 | novo | 11/2025 (primeiro mês) |
   | I7 | 2025-10-31 | 999 | novo | fora |
   | I8 | 2026-11-01 | 100 | novo | fora |

2. Menu **Dashboards > Fluxo Mensal**: a página abre com as duas seções, cada uma com **12 linhas**, em ordem crescente, incluindo meses zerados.
3. **Retorno:** 10/2026 = R$ 1.500,00 (2 Movimentos, com o selo "mês atual" na primeira linha); 12/2026 = R$ 2.300,00 (2); 09/2027 = R$ 250,00 (1); demais meses zerados; total geral R$ 4.050,00 (5 Movimentos).
4. **Investido:** 11/2025 = novo R$ 800,00; 05/2026 = reinvestimento R$ 300,00; 09/2026 = novo R$ 200,00; 10/2026 = novo R$ 1.100,00, reinvestimento R$ 500,00, total R$ 1.600,00 (selo "mês atual" na última linha); total geral novo R$ 2.100,00, reinvestimento R$ 800,00 e total R$ 2.900,00.
5. **Gráficos:** o de retorno mostra barras por mês; o de investido mostra barras empilhadas com legenda "Novo" e "Reinvestimento". Eixo e tooltip em reais; eixo Y começando em zero. Sem erros no console do navegador. Com o `libman restore` esquecido, o gráfico não aparece e a tabela continua funcionando.
6. **Isolamento entre usuários:** com um segundo usuário, a tela não mostra nenhum valor do primeiro. Sem login: redireciona para o login.
7. **Erro isolado por seção:** simular falha só do Retorno (por exemplo, bloqueando temporariamente a rota ou forçando um erro em ambiente local) e confirmar que o alerta aparece **só** na seção 1 e a seção 2 carrega normalmente; depois o inverso. Parar a Api inteira (`docker compose stop api`): alertas nas duas seções, sem ir para `/Error`. Subir de novo e conferir a recuperação.
8. **Notas fixas:** os textos das duas seções estão visíveis e dizem o que a regra faz (vencidos no mês atual, sem vencimento fora, Investido inclui encerrados).
9. **Log da Api:** conferir, na primeira carga, que o EF traduziu as duas consultas sem erro (ponto de atenção da parte 2).

**Critério de pronto:** build e testes verdes, Web sem referência a Application ou Service, nenhum pacote novo (só o Chart.js já existente), nenhum script inline, página sem parâmetros e validação manual concluída.

---

## Passo 6: Documentação final e fechamento

1. Rodar a solução inteira: `dotnet build` e `dotnet test` verdes (inclui o teste de reflexão do ADR 0017 e o de registro de serviços).
2. **ADR 0019:** mudar o status para **Aceita (data do fechamento)** e acrescentar a seção "Implementação e fechamento", no estilo do ADR 0018, registrando só o que **diferiu** do texto original. Conferir, no mínimo:
   - nomes finais dos tipos (`JanelaMensal`, `IntervaloMensal`, projeções do repositório, queries, DTOs) e dos endpoints;
   - `Kind` das datas: o provedor materializa `Unspecified` e a `JanelaMensal` gera `Unspecified`; nenhuma conversão foi necessária;
   - a projeção por construtor foi traduzida pelo EF/Pomelo sem problema (ou, se foi preciso trocar, registrar a solução);
   - as queries são registradas pelo `AddServicesFromAssembly`, sem alterar `Program.cs` da Api;
   - o contrato do mês é `AAAA-MM` e o Web o exibe como `MM/AAAA`;
   - o Chart.js (ADR 0016) foi reaproveitado, sem dependência nova;
   - a feature não alterou schema, migration, índice, pacote nem LumiaFoundation;
   - o gatilho de reavaliação (`EXPLAIN` e tempo de resposta em volume realista) continua valendo, para os dois índices (`IX_Movimentos_UserId_EstaAtivo_DataVencimento` e `IX_Movimentos_UserId_DataInvestimento`).
3. **Specs 0018 (partes 1 a 4):** mudar o status de cada uma para "implementada (fechamento em AAAA-MM-DD)". Na parte 1, trocar o status "em andamento (parte 1 de 4...)" pelo texto final.
4. Não há índice de ADRs separado no repositório para atualizar. Se houver `README` por projeto no momento do fechamento, acrescentar a tela nova à lista de funcionalidades do Web.
5. **Registrar fora da feature, sem implementar:**
   - o Retorno previsto é conservador (usa o valor líquido de hoje, sem rendimento até o vencimento); se isso incomodar, abrir ADR para uma projeção opcional;
   - o mês atual do Retorno mistura vencimentos do mês e vencidos ainda ativos; se for útil distingui-los, abrir ADR para uma linha ou indicador separado;
   - a regra "Objetivo inativo não filtra" segue como nos ADRs 0010, 0011, 0013 e 0018; alinhar todas as telas é uma decisão futura, separada;
   - média ou meta de aporte mensal, e filtros por Objetivo ou Corretora, ficam como evolução.

**Critério de pronto:** ADR 0019 em "Aceita" com a seção de fechamento, as quatro partes da spec marcadas como implementadas, solução inteira verde e a validação manual do Passo 5.8 concluída.

---

## Fora do escopo desta feature

Média ou meta de aporte mensal; filtros por Objetivo ou Corretora; projeção de rendimento até o vencimento; distinguir, no mês atual do Retorno, vencimentos do mês e vencidos ainda ativos; escolher o período (os 12 meses são fixos); exportar para planilha; persistir qualquer preferência do usuário; qualquer alteração de schema ou de LumiaFoundation.
