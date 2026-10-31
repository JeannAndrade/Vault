# ADR 0022: Consolidado de Movimentos por filtros e agrupamento

- Status: Proposta (2026-10-30)

## Contexto

O usuário precisa responder perguntas como "quanto tenho em renda fixa?", "quanto tenho na corretora Inter?" e "quanto tenho em renda fixa no Inter?". A tela inicial só consolida por Objetivo (ADR 0014), e a listagem de Movimentos (ADR 0012) filtra, mas não soma. Falta uma visão que consolide **Qtde de Movimentos, Total Aportado e Total Líquido** por Tipo de Renda, Corretora, Produto e Emissor, sozinhos ou combinados, e que também permita comparar as fatias de uma dimensão ("quanto tenho em cada corretora, e quanto isso representa do todo").

O dinheiro de um Movimento encerrado como `Pendente` pode já ter sido levado para outro lugar. Para responder "quanto tenho **nesta** corretora", ele não pode ser presumido ali (diferente da tela de Objetivos, ADR 0021, onde o foco é o patrimônio do Objetivo).

## Decisão

### Conjunto de Movimentos

- Somente Movimentos **ativos** (`EstaAtivo = true`) do usuário (`UserId` vem só do token).
- `Pendente`, `Gasto` e `Reinvestido` **ficam fora**. Nota fixa na tela explica o motivo (o dinheiro pendente pode já estar em outro lugar).
- **Objetivo inativo não filtra** (ADRs 0010, 0011, 0013, 0018 e 0019). Consequência: o total sem filtro pode ser maior que o "Total" da tela inicial, que só lista Objetivos ativos e inclui `Pendente`. A nota da tela também registra isso.

### Filtros

- Quatro filtros opcionais, **um valor por dimensão**, combinados com `E`: `tipoRendaId`, `corretoraId`, `produtoId` e `emissorId`.
- Seleção múltipla dentro de uma dimensão fica fora de escopo.
- A Api **não valida existência** dos IDs: ID inexistente, de outro usuário ou `Guid.Empty` resulta em zeros (200), sem enumeração de IDs (mesma regra do ADR 0012). GUID malformado: 422.

### Agrupamento

- Parâmetro opcional `agruparPor`, com **uma** dimensão: `tipoRenda`, `corretora`, `produto` ou `emissor`. Valor inválido: 422.
- Com agrupamento, a resposta traz uma linha por grupo: Id, nome, Qtde, Total Aportado, Total Líquido e **percentual do Total Líquido do grupo sobre o Total Líquido de todo o conjunto filtrado**.
- Ordem das linhas: Total Líquido decrescente, depois nome e Id ascendentes (determinística).
- O total geral de uma resposta agrupada é a **soma das linhas**: o total e as linhas nunca divergem e a consulta é uma só.
- Total Líquido do conjunto igual a zero: percentual 0 (sem divisão por zero). Os percentuais são só de exibição e podem não somar exatamente 100% por arredondamento.
- Agrupar por uma dimensão que também está filtrada é permitido (resulta em uma linha com 100%).

### Quando há resultado

- A tela abre **sem resultado**, com uma orientação. O resultado é exibido quando há **ao menos um filtro ou um agrupamento**.
- A Api devolve 422 quando não há nenhum dos dois.

### Agregação no banco

- `Count` e `Sum` por `Where` e, quando há agrupamento, `GroupBy` com a chave (Id e Nome da dimensão) **no banco**: o resultado tem poucas linhas, e a regra é simples para o provider traduzir. Isso difere dos ADRs 0018 e 0019 (que agregam em memória) porque aqui não há potência nem agrupamento por data.
- Sem filtro de saldo: o único critério de conjunto é `EstaAtivo`, uma coluna. A propriedade calculada `CompoeSaldoDoObjetivo` **não** é usada.
- Quatro ramos explícitos no repositório (um por dimensão), sem montar expressões dinamicamente.

### Camadas

- **Domain:** sem alteração. Não há regra de negócio nova (só soma e razão).
- **Persistence:** `ConsolidadoFiltro` (quatro `Guid?`), enum `DimensaoConsolidado`, projeções `ConsolidadoAgregado` e `ConsolidadoGrupoItem`, e dois métodos no `IMovimentoRepository` (total e agrupado), ambos com `CancellationToken` obrigatório (ADR 0017).
- **Application:** `GetConsolidadoQuery`, sem relógio. Valida "ao menos um filtro" e a dimensão (`CommandValidationException`, 422) e calcula os percentuais.
- **Service:** `GET api/dashboards/consolidado` no `DashboardsController`, com `[RequestValidation]`; um tipo de filtro por camada, com `ToXxx()` entre elas.
- **Web:** página `/Dashboards/Consolidado`, item **Consolidado** no menu **Dashboards**; formulário `GET` com quatro selects (`--` = todos) e "Agrupar por"; botão **Limpar filtro**; filtros só na URL. Reaproveita `IMovimentoFormLookupData`.
- Sem schema, sem migration, sem índice novo e sem dependência nova.

## Alternativas consideradas

- **Só filtros:** cobre os três exemplos, mas obriga a repetir a consulta para comparar corretoras e não responde "quanto isso representa do todo".
- **Só agrupar (sem filtros):** não responde diretamente "renda fixa no Inter".
- **Incluir `Pendente`** (conjunto do ADR 0021): coerente com a tela de Objetivos, mas afirmaria que o dinheiro ainda está na corretora, o que pode não ser verdade.
- **Incluir todos os Movimentos:** contaria o dinheiro duas vezes (`Reinvestido`) e dinheiro que já saiu (`Gasto`).
- **Tornar `CompoeSaldoDoObjetivo` uma expressão compartilhada:** só seria necessário com `Pendente`; descartado junto com ele.
- **Agregar em memória (padrão dos ADRs 0018 e 0019):** traz todas as linhas filtradas para somar, sem necessidade aqui.
- **Selos multiseleção:** complica a URL e a Api sem necessidade hoje.
- **Total em consulta separada quando há agrupamento:** permitiria divergência sob escrita concorrente; somar as linhas evita isso.
- **Reutilizar `MovimentoFiltro` (ADR 0012):** ele tem Objetivo e `EstaAtivo`, com outro sentido aqui.

## Consequências

- O consolidado representa o dinheiro **ainda investido** e ativo; o total pode diferir do "Total" da tela inicial (que inclui `Pendente` e exclui Objetivos inativos). A nota da tela deixa isso explícito.
- Os testes de Persistence com InMemory não validam a tradução para SQL do `GroupBy`; é preciso rodar uma vez contra o MariaDB (validação manual na spec).
- Gatilho de reavaliação: se o `EXPLAIN` em volume realista mostrar problema, abrir novo ADR avaliando índice composto. Os índices `(UserId, X)` e `(UserId, EstaAtivo, ...)` existentes atendem os filtros.
- Percentual sobre o Total Líquido (não sobre o Aportado): mede a fatia do dinheiro hoje, não do dinheiro investido.
- Complementa os ADRs 0012, 0014, 0018, 0019 e 0021.

Fora de escopo: seleção múltipla, agrupamento por mais de uma dimensão, gráficos, exportação, inclusão de encerrados e persistência do último filtro.
