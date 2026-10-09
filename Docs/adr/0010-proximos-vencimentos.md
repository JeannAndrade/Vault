# ADR 0010: Próximos vencimentos na tela inicial

- Status: Aceita (2026-10-09)

## Contexto

A tela inicial precisa exibir os próximos 15 Movimentos ativos a vencer (Objetivo, Corretora, Produto, Emissor, Quando Vence e Valor Líquido Atual), com duas regras de estilo: fundo verde claro quando o valor líquido atual for maior que R$ 5.000 e fundo amarelo quando faltarem menos de 10 dias corridos para o vencimento (vencidos também ficam amarelos).

## Decisão

- Endpoint dedicado `GET api/movimentos/proximos-vencimentos`, sem paginação, com quantidade fixa de 15 definida como constante no código.
- Filtro: `EstaAtivo = true` e `DataVencimento` não nula. Movimentos já vencidos e ainda ativos entram na lista. Um Movimento ativo aparece mesmo que o Objetivo esteja inativo.
- Ordenação: `DataVencimento ASC, Id ASC`.
- DTOs enxutos: `ProximoVencimentoModel` (Application) e `ProximoVencimentoDto` (Service/Presentation), com Id, ObjetivoNome, CorretoraNome, ProdutoNome, EmissorNome, DataVencimento e ValorLiquidoAtual.
- A Api devolve apenas dados. As regras de cor são de apresentação e ficam no Web, em classe testável, usando classes Bootstrap (`bg-success-subtle`, `bg-warning-subtle`). A comparação considera apenas o dia, com `IDateTimeProvider` (`UtcDateTimeProvider`, de Lumia.Foundation.Core). O relógio existe somente no Web.
- Na tela inicial, as chamadas à Api são sequenciais e o erro da nova seção é isolado: `ApiException` é capturada e exibe um alerta apenas na seção, sem levar a página para `/Error`. A exceção de não autorizado não é engolida (segue para `ApiUnauthorizedExceptionHandler`).
- Índice `IX_Movimentos_UserId_EstaAtivo_DataVencimento` em `(UserId, EstaAtivo, DataVencimento)`, acoplado à ordenação ascendente do repositório (mesma lógica do ADR 0009). A migration é gerada manualmente pelo mantenedor.

## Alternativas consideradas

- Filtros em `GET api/movimentos`: conflita com a paginação e a ordenação definidas no ADR 0008.
- A Api devolver flags de cor: coloca responsabilidade de visão na camada de negócio.
- Filtrar e ordenar no Web: traria todos os movimentos pela rede.
- Reutilizar `MovimentoDto`: carrega campos desnecessários e acopla a tela a um contrato maior.

## Consequências

- O relógio só existe no Web. Como é UTC, entre 21h e 24h (BRT) "hoje" já é o dia seguinte; o efeito é apenas visual (cor do vencimento).
- A tabela passa a ter 12 índices secundários; pequeno custo extra em escrita.
- Mudar a direção ou o critério da ordenação exige reavaliar o índice. Um teste de metadados do modelo garante que ele existe. O uso deve ser confirmado com `EXPLAIN` quando houver volume.
- A migration herda o ruído de `IdentityRole` (`HasData` sem `Id` fixo) das anteriores.
