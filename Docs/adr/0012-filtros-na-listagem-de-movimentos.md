# ADR 0012 — Filtros na listagem de Movimentos

**Status:** Proposta (2026-10-12)

## Contexto

A listagem de Movimentos (`/Movimentos`) exibe todos os movimentos do usuário, paginados por offset (ADR 0008) e ordenados por `DataInvestimento DESC, Id DESC` (ADR 0009). Com o crescimento do volume, o usuário precisa restringir a listagem por:

- Objetivo
- Tipo de Renda
- Corretora
- Produto
- Emissor
- Ativo (situação do movimento: ativo/inativo)

Requisitos funcionais:

- A tela inicial não muda: ao abrir, exibe todos os movimentos.
- Cada filtro é uma lista de seleção com o item `--`, que significa "filtro não utilizado".
- Botão **Filtrar** aplica os filtros selecionados (combinados com `E`).
- Botão **Limpar filtro** volta à listagem sem filtro (todos os selects em `--`).
- A listagem continua paginada, agora sobre o conjunto filtrado.

Restrições herdadas do projeto:

- Clean Architecture: Service → Application → Persistence → Domain; Persistence não enxerga Application.
- Web nunca acessa o banco; só a Api, por contrato HTTP.
- Multi-tenant: todo acesso é escopado por `UserId`.
- Sem mudança de schema nem de LumiaFoundation sem discussão prévia.
- Regra da spec 0011: ao acrescentar argumentos a métodos usados em `Setup/Verify` do Moq, criar **sobrecarga**, nunca parâmetro opcional (CS0854 / quebra binária).

## Decisão

### Contrato da Api (D1, D3, D7)

`GET api/movimentos` passa a aceitar, via query string, os parâmetros opcionais `objetivoId`, `tipoRendaId`, `corretoraId`, `produtoId`, `emissorId` (GUIDs) e `estaAtivo` (booleano), além de `page` e `pageSize`. Parâmetro ausente significa "sem filtro". Sem nenhum filtro, o contrato HTTP é idêntico ao atual.

- Os filtros chegam ao controller como objeto `[FromQuery] MovimentoFiltroDto filtro`.
- GUID malformado invalida o `ModelState` e é respondido com **422** pela checagem já existente em `GetMovimentos` (`SuppressModelStateInvalidFilter` está ativo).
- A Api **não valida a existência** dos IDs: apenas filtra, sempre escopada por `UserId`. ID inexistente (ou de outro usuário, ou `Guid.Empty`) resulta em lista vazia. Isso evita enumeração de IDs.
- Os filtros combinam com `E`. `TotalCount`/`TotalPages` refletem o conjunto filtrado.

### Um tipo de filtro por camada (D6)

| Camada | Tipo | Observação |
|---|---|---|
| Service | `MovimentoFiltroDto` | Binding da query string |
| Application | `MovimentoFiltroParameters` | Segue o padrão de `PaginationParameters` |
| Persistence | `MovimentoFiltro` | Consumido pelo repositório |
| Presentation (Web) | `MovimentoFiltro` (record selado, próprio) | Usado por `IMovimentoApi` |

Conversões explícitas com métodos `ToXxx()` entre camadas. Nenhuma camada referencia o tipo de outra camada "para fora".

### Sobrecargas, nunca parâmetros opcionais (D5, D6, D10)

- `IMovimentoRepository.GetPagedWithRelatedEntitiesAsync(Guid ownerId, MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken)`.
- `IGetMovimentosListQuery.ExecuteAsync(Guid ownerId, MovimentoFiltroParameters filtro, PaginationParameters paginacao, CancellationToken cancellationToken)`.
- `IMovimentoApi.GetPagedAsync(MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken)`.

Nas novas assinaturas o `CancellationToken` é **obrigatório** (sem valor padrão). As assinaturas antigas permanecem. A action do controller não pode ser sobrecarregada; sua assinatura muda (recebe o DTO de filtro) sem alterar o contrato HTTP.

### Persistência (D4)

O `Where` dos filtros é aplicado **antes** da ordenação e da paginação; a contagem total também é filtrada. Não há novo índice nem migration: já existem índices `(UserId, X)` para cada FK e `(UserId, DataInvestimento)` para a ordenação. Não se espera alteração em `EFRepository` (LumiaFoundation); o desenvolvedor deve confirmar durante a implementação.

**Gatilho de reavaliação:** se a medição (`EXPLAIN`) em volume realista mostrar `filesort`/varredura relevante para combinações de filtros, abrir novo ADR avaliando índices compostos.

### Web (D2, D8, D9, D10)

- Os filtros vivem na **URL da página**. **Filtrar** é um formulário `GET` contendo apenas os selects (sempre volta à página 1). **Limpar filtro** é um link para `/Movimentos` sem parâmetros.
- Modelo de binding: objeto aninhado `Filtro` (`MovimentoFiltroInput`, compartilhado por Index/Create/Edit) com `[BindProperty(SupportsGet = true)]`. URL exemplo: `/Movimentos?Filtro.ObjetivoId=...&Filtro.EstaAtivo=true&pagina=2`. Selects com `asp-for="Filtro.X"`; `--` é `<option value="">` (string vazia vira `null`).
- Select de Ativo: `--` (vazio), `Ativos` (`true`), `Inativos` (`false`), mapeado em `bool?`.
- Os selects exibem **todos** os registros (inclusive Objetivos inativos) e reutilizam `IMovimentoFormLookupData`.
- Tamanho de página permanece 12.
- O filtro é **preservado em todas as ações**: paginação (o `<lumia-pagination>` já preserva os demais parâmetros e troca só `pagina`), Editar, Novo, Excluir (via `asp-route-*` na action do formulário POST, pois o TagHelper não preserva query em POST) e todos os redirects (Create, Edit, Index, inclusive o redirect para a última página quando a página atual deixa de existir). O `IndexModel` expõe um dicionário de route values para isso.
- `MovimentoApi` monta a URL em ordem fixa: `page, pageSize, objetivoId, tipoRendaId, corretoraId, produtoId, emissorId, estaAtivo`; omite nulos; booleano em minúsculas; cultura invariante.

## Alternativas consideradas

1. **Filtro no Web (filtrar após buscar tudo):** rejeitada. Quebra a paginação por offset, trafega dados desnecessários e duplica regra no cliente.
2. **Endpoint novo (`POST api/movimentos/busca`) com corpo JSON:** rejeitada. Consulta é idempotente e cacheável; `GET` com query string é mais simples, linkável e coerente com o contrato atual.
3. **Parâmetros opcionais nos métodos existentes:** rejeitada (CS0854 em expressões Moq e quebra binária); adotadas sobrecargas.
4. **Tipo único de filtro compartilhado entre camadas:** rejeitada. Acoplaria Service/Web à Persistence ou inverteria dependências, contrariando a Clean Architecture.
5. **Validar existência dos IDs na Api (404/422 para ID desconhecido):** rejeitada. Custo extra de consultas e risco de enumeração de IDs; lista vazia é resposta suficiente e segura.
6. **Filtro em estado de sessão/cookie no Web:** rejeitada. URL sem estado é testável, compartilhável e funciona com botão Voltar.
7. **Criar índices compostos agora:** adiada. Sem evidência de necessidade; ver gatilho de reavaliação.
8. **Filtros em `POST` com PRG:** rejeitada para a listagem. `GET` mantém a URL como fonte única do estado.

## Consequências

**Positivas**

- Filtros executados no banco, com paginação e contagem corretas.
- Sem mudança de schema, migration ou LumiaFoundation.
- Isolamento entre usuários preservado (`UserId` sempre aplicado); sem enumeração de IDs.
- Fronteiras entre camadas mantidas; Web e Api seguem desacoplados por contrato HTTP.
- Estado na URL: linkável, testável e compatível com Voltar/recarregar.

**Negativas / custos**

- Cinco chamadas paralelas extras à Api por renderização da listagem (via `IMovimentoFormLookupData`); aceitável na escala atual.
- Três tipos de filtro com mapeamentos explícitos (verbosidade em troca de isolamento de camadas).
- Interfaces ficam com métodos antigo e novo coexistindo; o antigo pode ser removido em ADR futuro.
- Testes existentes precisam de ajuste: `MovimentosControllerTests` (assinatura de `GetMovimentos`) e `IndexModelTests` (o Index passa a sempre chamar a nova sobrecarga de `GetPagedAsync`).
- Combinações de filtros podem não ser cobertas de forma ótima pelos índices atuais; monitorar conforme o gatilho acima.

**Fora de escopo**

Alterações de schema/migration, mudanças no LumiaFoundation, filtros por data ou texto, persistência do último filtro usado, exibição de contagem de filtros ativos.
