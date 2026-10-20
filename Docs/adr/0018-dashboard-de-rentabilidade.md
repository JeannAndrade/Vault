# ADR 0018: Dashboard de Rentabilidade (maiores e menores por Tipo de Renda)

- Status: Proposta (2026-10-20)

## Contexto

O usuário quer um menu novo, **Dashboards**, para reunir telas de visualização dos investimentos. A primeira é o submenu **Rentabilidade**, que mostra, para cada Tipo de Renda, os Movimentos com **maior** rentabilidade anualizada (padrão) ou com a **menor** (para identificar investimentos que foram mal e não devem ser repetidos).

Hoje a planilha Excel do usuário calcula três colunas por macro VBA: rentabilidade acumulada, diária e anualizada. O schema não guarda rentabilidade numérica (`RentabilidadeContratada` é texto livre), então os três valores precisam ser **calculados** a partir de `ValorAporte`, `ValorLiquidoAtual`, `DataInvestimento` e `DataAtualizacaoValorLiquido`.

Em um mesmo título com vários aportes, o `ValorLiquidoAtual` individual pode ser uma estimativa da distribuição do ADR 0013; a rentabilidade por aporte herda essa aproximação, e o usuário a aceita.

## Decisão

### Fórmulas (idênticas às macros VBA nos casos válidos)

Notação: `A = ValorAporte`, `V = ValorLiquidoAtual`, `dias = (DataAtualizacaoValorLiquido.Date − DataInvestimento.Date).Days`.

- Acumulada: `V / A − 1`.
- Diária (composta): `(1 + acumulada)^(1 / dias) − 1`.
- Anualizada: `(1 + acumulada)^(365 / dias) − 1`. Base de dias corridos por 365, como no ADR 0013.

Diferenças deliberadas em relação ao VBA, que devolve 0 em `dias <= 0`, `A = 0` e `1 + acumulada <= 0`. No ranking, 0 viraria um falso empate com rentabilidade zero de verdade:

- `dias <= 0`, `A = 0` e `1 + acumulada < 0` (só ocorreria com `V` negativo) resultam em **"sem cálculo"** (`null`).
- **Perda total** (`V = 0`, ou seja, `1 + acumulada = 0`, com `dias > 0`) é cálculo **válido**: acumulada, diária e anualizada = −100% (limite matemático). Assim o pior caso aparece na visão "Menores rentabilidades". A validação do `ValorLiquidoAtual` (não negativo) deve ser confirmada na spec.

O período termina em `DataAtualizacaoValorLiquido`, não em "hoje": um valor parado há 40 dias não pode parecer render menos do que rende. Por isso o cálculo **não usa relógio** (sem `IDateTimeProvider`), ao contrário do ADR 0013.

### Elegibilidade

Entra no ranking o Movimento com:

- `EstaAtivo = true` e `UserId = dono`. Movimentos já vencidos e ainda ativos entram; Objetivo inativo não filtra (ADRs 0010 e 0011).
- `dias >= 30`, constante nomeada `DiasMinimosParaRanking` no domínio. Anualizar poucos dias gera números absurdos que dominariam o ranking.
- Cálculo válido (as três rentabilidades diferentes de `null`).
- Movimentos com `AtualizaPorCotacao` (ADR 0016) **entram**: o valor deles é `Quantidade × cotação`, portanto a rentabilidade é real.

Cada linha é um **Movimento** (um aporte), não um título agrupado: aportes de datas diferentes não podem ser somados sem distorcer o tempo.

### Ranking e ordem

- Agrupamento por Tipo de Renda; os grupos saem em ordem alfabética do nome. Tipo de Renda sem Movimentos elegíveis **não aparece**.
- Dois modos, escolhidos na tela: **Maiores rentabilidades** (padrão, anualizada decrescente) e **Menores rentabilidades** (anualizada crescente).
- Título de cada tabela: "Campeões da renda <nome>" no modo Maiores e "Menores rentabilidades da renda <nome>" no modo Menores.
- Desempate por `Id` ascendente (determinístico, como nos ADRs anteriores). A comparação usa o valor sem arredondamento; o arredondamento é só de exibição.
- Top N **por grupo**: padrão 25, faixa de 1 a 100 (como `PageSizeMaximum`), constantes nomeadas no código. Fora da faixa: 422.
- Os parâmetros ficam **somente na URL** (`?top=25&ordem=maiores`); nada é persistido. `ordem` aceita `maiores` e `menores`; qualquer outro valor: 422.

### Camadas

- **Domain:** `RentabilidadeCalculador` estático e puro, sem I/O e sem relógio. Entrada: aporte, valor líquido, data inicial e data final. Saída: acumulada, diária e anualizada, ou "sem cálculo". Dinheiro em `decimal`; `double` só na potência e no resultado (razões).
- **Persistence:** `GetAtivosParaRankingAsync(ownerId, cancellationToken)` no `IMovimentoRepository`, com `Include` de Tipo de Renda, Produto, Emissor, Corretora e Objetivo, sem tracking.
- **Application:** `GetRentabilidadesPorTipoRendaQuery` (calcula, filtra, agrupa, ordena e aplica o top N em memória). `CancellationToken` obrigatório; `catch (Exception ex) when (ex is not OperationCanceledException)`.
- **Service:** `DashboardsController` novo, `GET api/dashboards/rentabilidade?top=25&ordem=maiores`, com `[RequestValidation]`. Resposta: lista de grupos `{ tipoRendaId, tipoRendaNome, itens[] }`; cada item traz Produto, Emissor, Objetivo, Corretora, `ValorAporte`, `ValorLiquidoAtual` e as três rentabilidades como **fração** (0,225 = 22,5%), como a `TaxaEfetivaAnual` do ADR 0013.
- **Web:** página `/Dashboards/Rentabilidade`, com formulário `GET` (campo `top` e opção Maiores/Menores) e uma tabela por Tipo de Renda. Colunas visíveis: Produto, Emissor, Valor Líquido Atual, Rentabilidade acumulada, diária e anualizada (em %, `pt-BR`). Objetivo, Corretora e Valor do aporte ficam no detalhe expansível, reaproveitando o padrão da listagem de Movimentos. O menu ganha o dropdown **Dashboards**, com o item **Rentabilidade**.
- **Salvaguardas:** `UserId` aplicado em toda consulta; `CancellationToken` obrigatório e último parâmetro (o teste de reflexão do ADR 0017 já cobre as novas interfaces); sem dependência nova.

### Persistência

Sem alteração de schema nem migration. O filtro `(UserId, EstaAtivo)` é atendido pelo prefixo do índice `IX_Movimentos_UserId_EstaAtivo_DataVencimento` (ADR 0010). Gatilho de reavaliação: se o `EXPLAIN` e o tempo de resposta em volume realista mostrarem problema, abrir novo ADR.

## Alternativas consideradas

- **Calcular e ranquear no banco (SQL/EF):** traz menos linhas, mas `POW` sobre colunas `decimal` no Pomelo/MariaDB é frágil, a regra ficaria no SQL e os testes com InMemory não a validariam.
- **Rota em `MovimentosController`:** mistura leitura analítica com CRUD, conflita com o ADR 0010 (sem filtros ali) e o controller já tem muitas dependências.
- **Agrupar por título (Produto + Emissor + Corretora):** esconde a diferença de data entre aportes e distorce o tempo.
- **Período até "hoje":** valores parados parecem render menos do que rendem.
- **Dias úteis/252:** exige calendário de feriados, que o Vault não tem.
- **Manter o 0 do VBA nos casos inválidos:** falso empate com rentabilidade zero real.
- **Perda total como "sem cálculo" (guarda `1 + acumulada <= 0` do VBA):** tiraria da visão "Menores" justamente o pior caso.
- **Mostrar todos, com "—" na anualizada, em vez de excluir os recentes:** poluiria a lista, que é um ranking.
- **Dois endpoints, um para maiores e outro para menores:** duplica contrato e testes para uma mudança de direção de ordenação.
- **Persistir o `top` (preferência do usuário):** exige armazenamento e tela de configuração sem necessidade hoje.

## Consequências

- A rentabilidade por aporte é uma **estimativa** quando o valor veio da distribuição do ADR 0013 (ou de atualização manual); só nos Movimentos por cotação é exata.
- Anualizar 30 a poucos meses ainda amplifica variações; o ranking compara prazos diferentes, e a anualizada de um aporte recente é menos estável que a de um antigo.
- O ranking depende de `DataAtualizacaoValorLiquido`: Movimentos com valor desatualizado entram com o período até a última atualização (ADR 0011), sem aviso na tela.
- A query carrega todos os Movimentos ativos do usuário; aceitável na escala de controle pessoal.
- Serviço de domínio puro, testável sem mocks, com testes para cada caso inválido, para a equivalência com as fórmulas do VBA e para o desempate.
- Mudança sem schema, sem migration e sem pacote novo; nenhum contrato da LumiaFoundation é alterado.
- Complementa os ADRs 0010, 0011, 0013, 0016 e 0017.
