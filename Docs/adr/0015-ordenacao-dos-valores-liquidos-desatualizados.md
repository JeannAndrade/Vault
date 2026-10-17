# ADR 0015: Ordenação da lista de valores líquidos desatualizados

- Status: Aceita (2026-10-17)

## Contexto

O ADR 0011 definiu que a lista de "Manutenção Valor Líquido" é ordenada por `DataAtualizacaoValorLiquido` e `Id` ascendentes (os mais antigos primeiro). Na prática, essa ordem não ajuda a atualização: o usuário trabalha por título e precisa ver juntos os movimentos da mesma Corretora, Produto e Emissor.

## Decisão

- A ordenação passa a ser `Corretora.Nome`, `Produto.Nome` e `Emissor.Nome` ascendentes, com `DataAtualizacaoValorLiquido` e `Id` ascendentes como desempate determinístico.
- A ordenação é aplicada **antes** do `Take(12)`: os 12 itens exibidos são os 12 primeiros dessa ordem, e não os 12 mais defasados.
- A regra de entrada na lista (ativo e mais de 30 dias) e o limite de 12 itens não mudam.
- Ordena-se por nome, e não por `Id`: o ganho de desempenho do `Id` é desprezível (a consulta já faz join das entidades e usa filesort nos dois casos) e o GUID geraria uma ordem arbitrária para o usuário.
- O índice `IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido` é mantido, pois continua atendendo o filtro. Não há migration.

## Alternativas consideradas

- Pegar os 12 mais antigos e reordenar por nome só na exibição: preserva a prioridade por defasagem, mas a ordenação passaria a ser feita em memória e um título poderia aparecer incompleto.
- Ordenar por `Id` das FKs: marginalmente mais barato, mas sem ordem legível.
- Incluir o Tipo de Renda na ordenação: não solicitado.

## Consequências

- Supera parcialmente o ADR 0011 quanto à ordenação.
- Um movimento muito defasado pode ficar fora dos 12 se sua corretora vier depois no alfabeto; ele aparece quando os anteriores forem atualizados e saírem da lista.
- A ordenação gera filesort sobre o conjunto filtrado do usuário. Com o volume atual é irrelevante; confirmar com `EXPLAIN` quando houver volume e, se for relevante, abrir novo ADR avaliando um índice.
- A ordem por nome segue a collation da coluna no MariaDB (`utf8mb4`, case-insensitive); o provider InMemory dos testes usa ordem ordinal.
