# Service / Api

`GET api/dashboards/consolidado` recebe `tipoRendaId`, `corretoraId`, `produtoId`, `emissorId` e `agruparPor` opcional. Retorna `{ total, grupos }`, com valores monetários e `percentualLiquido` como `decimal`; `grupos` é vazio quando não há agrupamento. A requisição exige ao menos um filtro ou agrupamento.

`GET api/objetivos/resumo` retorna os totais do Objetivo e o objeto `projecao`, com os cenários Conservador, Realista e Planejado. Os campos `totalLiquido`, `totalAportado`, `qtdeMovimentos` e `percentualMeta` incluem movimentos encerrados como `Pendente`.
