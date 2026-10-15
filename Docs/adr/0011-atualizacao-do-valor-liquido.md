# ADR 0011: Atualização do Valor Líquido

- Status: Aceita (2026-10-11). Complementada pelo ADR 0013 (atualização em lote por título).

## Contexto

O Valor Líquido Atual de cada Movimento é informado manualmente. Sem a data da última atualização, não há como saber quais investimentos estão com o valor defasado. A regra de negócio é: um Movimento ativo precisa de atualização quando passaram mais de 30 dias desde a última atualização do valor líquido.

## Decisão

- Nova coluna `DataAtualizacaoValorLiquido` em `Movimentos` (`datetime(6)`, obrigatória), guardando somente a data, em UTC. Os Movimentos existentes recebem a `DataInvestimento` na migration; os novos recebem a data do cadastro (e não a `DataInvestimento`, que pode ser retroativa).
- Regra: entra na lista o Movimento com `EstaAtivo = true` e `DataAtualizacaoValorLiquido < hoje - 30 dias` (UTC). Objetivo inativo não filtra, como no ADR 0010.
- `GET api/movimentos/desatualizados`: sem paginação, com 12 itens fixos (constante no código), ordenados por `DataAtualizacaoValorLiquido` e `Id` ascendentes (os mais antigos primeiro). Modelo e DTOs dedicados, com os mesmos campos dos próximos vencimentos (ADR 0010).
- `PATCH api/movimentos/{id}/valor-liquido`, corpo apenas com `ValorLiquidoAtual` (maior ou igual a zero), resposta 204. Altera somente `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido`. O `PUT` completo não renova a data.
- `IDateTimeProvider` (`UtcDateTimeProvider`) passa a ser registrado também na Api. O repositório recebe a data limite já calculada e continua sem relógio.
- Índice `IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido` em `(UserId, EstaAtivo, DataAtualizacaoValorLiquido)`, acoplado à ordenação ascendente do repositório (mesma lógica dos ADRs 0009 e 0010). A migration é gerada pelo mantenedor.
- Web: nova tela em `/Movimentos/Desatualizados`, com campo de valor inline em cada linha e o botão Atualizar. No menu, em "Manutenção" > "Manutenção Valor Líquido".
- `CancellationToken`: as novas assinaturas (repositório, query e command) o exigem, sem valor padrão. A lib `EFRepository` 0.12.0 ganha a sobrecarga `SaveAsync(CancellationToken)`, e `IMovimentoRepository` ganha uma sobrecarga de `GetAsync` com token. As demais rotas seguem na discussão geral de propagação do token.

## Alternativas consideradas

- Filtro em `GET api/movimentos`: conflita com a paginação e a ordenação do ADR 0008.
- Relógio só no Web, enviando a data limite à Api: coloca uma regra de negócio no cliente.
- `PUT` completo renovando a data quando o valor muda: mantém a data coerente, mas faz a Edição geral alterar um dado de controle. Descartada por ora.
- Página dedicada ou modal para informar o valor: mais passos por atualização; o campo inline é mais rápido para quem atualiza vários.
- Paginação da lista: a tela é uma fila de trabalho (ao atualizar, o item sai da lista), então basta limitar a quantidade.
- Parâmetro opcional em `SaveAsync`: quebraria a compilação dos testes existentes (`Verify` com expression tree não aceita argumento opcional) e seria uma quebra binária na lib.

## Consequências

- O relógio passa a existir também na Api, o que supera parcialmente o ADR 0010 (que o restringia ao Web).
- Como o relógio é UTC, entre 21h e 24h (BRT) "hoje" já é o dia seguinte; o efeito é de um dia na fronteira dos 30 dias.
- Sem total na tela, o usuário vê só os 12 mais antigos; ao atualizar, os seguintes aparecem.
- O valor pode ser editado pela tela de Edição sem renovar a data, e o Movimento continuará aparecendo como desatualizado. Reavaliar se isso incomodar.
- A tabela passa a ter 13 índices secundários; pequeno custo extra em escrita.
- Mudar a direção ou o critério da ordenação exige reavaliar o índice. Um teste de metadados do modelo garante que ele existe. O uso deve ser confirmado com `EXPLAIN` quando houver volume.
- Exige a nova versão `EFRepository` 0.12.0, publicada antes de o Vault consumi-la. Quem implementa `IBaseRepositoryManager` manualmente (no Vault, só o fake de testes) precisa da sobrecarga nova.
- A carga inicial de Movimentos (seed) precisa preencher `DataAtualizacaoValorLiquido`.
- A migration herda o ruído de `IdentityRole` (`HasData` sem `Id` fixo) das anteriores.
