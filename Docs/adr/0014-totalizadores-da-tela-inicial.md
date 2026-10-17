# ADR 0014: Totalizadores na tela inicial

- Status: Proposta (2026-10-16)

## Contexto

A tela inicial exibe a tabela "Meus objetivos" (`GET api/objetivos/resumo`), com Meta, Percentual Meta, Qtde Movimentos, Total Aportado e Total Líquido por objetivo. O usuário precisa ver, abaixo da tabela, a soma das três últimas colunas: Qtde de Movimentos ativos, Total Aportado e Total Líquido.

A lista já chega à tela somente com Objetivos ativos e, dentro deles, somente Movimentos ativos (`GetResumoObjetivosListQuery` e `ResumoObjetivoModel`). Ela não é paginada.

## Decisão

- Os totais são a **soma exata das linhas exibidas**: só Objetivos ativos, e Movimentos ativos dentro deles. O total sempre bate com a tabela.
- A soma é feita **no Web**, sobre a lista `ResumoObjetivoDto` já recebida da Api. Não há mudança na Api, no contrato HTTP, no banco ou no LumiaFoundation.
- A agregação fica em um tipo próprio e testável (`TotaisObjetivos`, record em `Presentation/Objetivos`), com `FromResumo(IEnumerable<ResumoObjetivoDto>)`, exposto pelo `IndexModel`. A regra não fica na view.
- Layout: linha `<tfoot>` na mesma tabela, com "Total" na primeira coluna e os valores alinhados sob Qtde Movimentos, Total Aportado e Total Líquido. As células de Meta e Percentual Meta ficam vazias; não se calcula percentual global.
- Formatação idêntica à da tabela (moeda em `pt-BR`; quantidade como inteiro).
- Sem objetivos, a tabela não aparece e os totais também não (sem linha de zeros).
- Decisão de tela, sem relógio nem estado: nada a configurar e nenhum novo registro de serviço.

## Alternativas consideradas

- **Totais de todos os Movimentos ativos do usuário (inclusive de Objetivos inativos):** o total poderia divergir da tabela, pois um Movimento ativo de um Objetivo inativo contaria no total sem aparecer em nenhuma linha.
- **Api devolvendo envelope `{ itens, totais }`:** quebra o contrato de `GET api/objetivos/resumo` (hoje um array), alterando Api e Web para somar três números.
- **Endpoint novo `GET api/objetivos/resumo/totais`:** segunda consulta ao banco, com risco de divergência entre as duas respostas sob escrita concorrente. Só se justificaria se a lista passasse a ser paginada.
- **Bloco separado (cards) em vez de `<tfoot>`:** os valores ficariam desalinhados das colunas que somam.

## Consequências

- Mudança restrita à Presentation: sem migration, sem pacote novo, sem alteração de contrato.
- A soma é agregação de apresentação sobre dados já entregues; a Api continua entregando somente dados, como no ADR 0010.
- O total é, por construção, consistente com a tabela exibida.
- **Gatilho de reavaliação:** se a lista de Objetivos passar a ser paginada ou filtrada na Api, a soma no Web deixará de representar o conjunto completo; nesse caso, abrir novo ADR avaliando um endpoint de totais (alternativa C).
- Soma de `decimal` no Web: sem risco de arredondamento acumulado, pois os valores já chegam arredondados pela Api.
