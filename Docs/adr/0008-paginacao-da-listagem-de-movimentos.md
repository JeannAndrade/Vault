# ADR 0008: Paginação da listagem de Movimentos

- Status: Aceita (2026-10-07)

## Contexto

`GET api/movimentos` devolve todos os Movimentos do usuário, com as cinco entidades relacionadas. O volume só cresce, e a tela do Web renderiza tudo de uma vez.

## Decisão

- Paginação por offset: `page` (base 1, padrão 1) e `pageSize` (padrão 20, máximo 100).
- Resposta com envelope no corpo: `PagedResponse<T>` com `items`, `page`, `pageSize`, `totalCount` e `totalPages`.
- Valores inválidos de `page`/`pageSize` resultam em 422 (`CommandValidator`). Página além da última resulta em 200 com `items` vazio e metadados corretos.
- Ordenação por `DataInvestimento` decrescente, com `Id` como desempate para tornar a paginação determinística.
- Tipos reutilizáveis nas libs: `PagedList<T>` (Core), `ToPagedListAsync` (EFRepository) e `PagedResponse<T>` (Abstractions). Web e Api continuam desacoplados: cada um mapeia para seus próprios DTOs.
- No Web, a página vem de `?pagina=N`, com tamanho fixo de 20, e a exclusão preserva a página atual.
- A conversão para o contrato de transporte (`ToPagedResponse`) e a navegação no Web (`<lumia-pagination>`, um TagHelper) ficam no `LumiaFoundation.AspNetCore`.

## Alternativas consideradas

- Metadados no header `X-Pagination`: o `IApiConnection` só expõe o corpo, e mudar isso alteraria o contrato público do `Http.Client`.
- Paginação por cursor: sem número de página nem total, e desnecessária para o volume esperado.
- Tipos apenas no Vault: duplicação e retrabalho no próximo projeto que usar as libs.

## Consequências

- Quebra o contrato de `GET api/movimentos` (antes um array). O único consumidor é o Web, ajustado no mesmo esforço.
- Total e itens saem de duas consultas; sob escrita concorrente pode haver pequena divergência entre eles.
- Sem filtros nem ordenação por coluna nesta entrega.
- Exige novas versões das libs, publicadas antes de o Vault consumi-las: Abstractions 0.2.0, Core 0.13.0, EFRepository 0.11.0 e AspNetCore 0.27.0.
