# Especificação 0012 — Filtros na listagem de Movimentos

**ADR relacionado:** [0012 — Filtros na listagem de Movimentos](../adr/0012-filtros-na-listagem-de-movimentos.md)
**Destinatário:** desenvolvedor responsável pela implementação
**Data:** 2026-10-12

## 1. Resumo

Adicionar filtros (Objetivo, Tipo Renda, Corretora, Produto, Emissor, Ativo) à listagem de Movimentos, executados no banco, combinados com `E`, preservando paginação e ordenação (`DataInvestimento DESC, Id DESC`). Tela inicial inalterada (todos os movimentos). Botões **Filtrar** e **Limpar filtro**; cada lista tem o item `--` ("filtro não usado").

Sem alteração de schema, migration ou LumiaFoundation.

## 2. Decisões (ver ADR 0012)

| # | Decisão |
|---|---|
| D1 | Filtros via query string em `GET api/movimentos`: `objetivoId, tipoRendaId, corretoraId, produtoId, emissorId, estaAtivo`; todos opcionais (ausente = sem filtro). |
| D2 | Web mantém filtros na URL; Filtrar = formulário GET só com os selects (sempre página 1); Limpar = link para `/Movimentos` sem parâmetros. |
| P2 | Select Ativo: `--` (vazio) / `Ativos` (`true`) / `Inativos` (`false`), tipo `bool?`. |
| P3 | Tamanho de página continua 12. |
| P4 | Selects mostram todos os registros (inclusive Objetivos inativos). |
| D3 | Api não valida existência de IDs; só filtra, escopada por `UserId`; sem correspondência = lista vazia; GUID malformado = 422; `Guid.Empty` é ID comum (resultado vazio). |
| D4 | Sem novo índice/migration. `Where` antes de ordenar/paginar; contagem também filtrada. Gatilho de medição (`EXPLAIN`/filesort) registrado no ADR. Sem mudança esperada em `EFRepository` (confirmar). |
| D5 | `MovimentoFiltro` entra por **sobrecarga** no repositório, `CancellationToken` obrigatório; assinatura antiga mantida. |
| D6 | Um tipo de filtro por camada: `MovimentoFiltroDto` (Service), `MovimentoFiltroParameters` (Application), `MovimentoFiltro` (Persistence), mapeados com `ToXxx()`; a query ganha sobrecarga `ExecuteAsync(ownerId, MovimentoFiltroParameters, PaginationParameters, ct)`. |
| D7 | Controller recebe `[FromQuery] MovimentoFiltroDto filtro`; assinatura da action muda, contrato HTTP sem filtros não muda; GUID malformado → chave no `ModelState` (ex.: `ObjetivoId`) → 422 pela checagem existente. |
| D8 | Filtro preservado em todas as ações do Web (paginação, Editar, Novo, Excluir, redirects de Create/Edit/Index, inclusive redirect para a última página). |
| D9 | Web usa objeto aninhado `Filtro` (`MovimentoFiltroInput`) compartilhado por Index/Create/Edit; URL `/Movimentos?Filtro.ObjetivoId=...&Filtro.EstaAtivo=true&pagina=2`; `asp-for="Filtro.X"`; `--` = `<option value="">`. |
| D10 | `IMovimentoApi` ganha sobrecarga `GetPagedAsync(MovimentoFiltro filtro, int page, int pageSize, CancellationToken ct)` com record selado `MovimentoFiltro` próprio da Presentation (5 `Guid?` + `bool?`); ordem fixa da URL: `page, pageSize, objetivoId, tipoRendaId, corretoraId, produtoId, emissorId, estaAtivo`; nulos omitidos; bool minúsculo; cultura invariante; método antigo mantido. |

## 3. Pontos de atenção

1. **Sobrecarga, nunca parâmetro opcional.** Métodos usados em `Setup/Verify` do Moq não podem receber parâmetros opcionais (CS0854 e quebra binária). Nas novas assinaturas o `CancellationToken` é obrigatório (sem `= default`). Assinaturas antigas permanecem.
2. **Actions não admitem sobrecarga.** A action `GetMovimentos` muda de assinatura; ajustar os testes existentes.
3. **Testes existentes a ajustar:**
   - `Service.Test/Movimentos/MovimentosControllerTests.cs` (chama `GetMovimentos(2, 10, cts.Token)`).
   - `Presentation.Test/Pages/Movimentos/IndexModelTests.cs` (Setups de `GetPagedAsync(pagina, IndexModel.TamanhoPagina, ct)`): o Index passará a sempre chamar a nova sobrecarga com filtro (vazio quando não há filtro).
4. **Dependências entre camadas:** Service → Application → Persistence → Domain. Persistence não referencia Application; Web não referencia Api (só contrato HTTP).
5. **Multi-tenant:** `UserId` sempre aplicado no repositório, com ou sem filtro. Nenhum caminho pode vazar dados entre usuários.
6. **`CancellationToken`** propagado de ponta a ponta; `OperationCanceledException` não é logada como erro (padrão atual da query).
7. **Sem engolir exceções:** falhas são logadas e relançadas, como em `GetMovimentosListQuery`.
8. **Todo código, comentários, commits e documentação em português**; nomes de tipos/membros seguem o padrão do repositório.
9. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo.
10. **TagHelper `<lumia-pagination>`** (LumiaFoundation.AspNetCore 0.28.0) preserva os demais parâmetros da query e só troca `pagina`; **não** preserva query em POST (ver Passo 6).

## 4. Passos

Executar na ordem. Cada passo termina com `dotnet build` e `dotnet test` verdes.

### Passo 0 — ADR como Proposta

- Adicionar `Docs/adr/0012-filtros-na-listagem-de-movimentos.md` com status **Proposta**.
- Atualizar índice de ADRs, se existir.

### Passo 1 — Persistence

**Criar** `MovimentoFiltro` (Persistence/Movimentos): tipo imutável com `ObjetivoId`, `TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId` (`Guid?`) e `EstaAtivo` (`bool?`). Fornecer valor "vazio" (sem filtros) para uso em chamadas sem filtro.

**Alterar** `IMovimentoRepository` e `MovimentoRepository`:

- Nova sobrecarga `GetPagedWithRelatedEntitiesAsync(Guid ownerId, MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken)`.
- Implementação: partir de `QueryWithRelatedEntities()`, aplicar `Where` de `UserId` e, para cada campo não nulo, o `Where` correspondente (FK ou situação ativo/inativo); depois `OrderByDescending(DataInvestimento).ThenByDescending(Id)` e `ToPagedListAsync(...)`. A contagem total deve refletir o filtro.
- A assinatura antiga delega para a nova com filtro vazio (ou permanece como está, desde que o comportamento seja idêntico). Não alterar `EFRepository`; se for necessário, parar e sinalizar impacto (D4).

**Testes** (`Persistence.Test/Movimentos/MovimentoRepositoryTests.cs`, usando o helper `AdicionarMovimento`):

- Cada um dos 6 filtros isoladamente.
- Combinações de dois ou mais filtros.
- Filtro vazio = comportamento atual.
- Isolamento por usuário com filtro ativo (movimentos de outro usuário nunca retornam).
- `TotalCount`/`TotalPages` refletem o filtro.
- Página 2 filtrada.
- Ordenação `DataInvestimento DESC, Id DESC` mantida.
- ID desconhecido (inclusive `Guid.Empty`) → lista vazia.

### Passo 2 — Application

**Criar** `MovimentoFiltroParameters` (Application/Movimentos), mesmo estilo de `PaginationParameters`, com método de mapeamento `ToMovimentoFiltro()`.

**Alterar** `IGetMovimentosListQuery` / `GetMovimentosListQuery`:

- Nova sobrecarga `ExecuteAsync(Guid ownerId, MovimentoFiltroParameters filtro, PaginationParameters paginacao, CancellationToken cancellationToken)`.
- Mesmo fluxo atual: validar paginação com `CommandValidator`, chamar a nova sobrecarga do repositório com o filtro mapeado, logar falhas (exceto `OperationCanceledException`) e relançar.
- Assinatura antiga mantida, com comportamento equivalente a filtro vazio.

**Testes** (`Application.Test/.../GetMovimentosListQueryTests.cs`):

- A sobrecarga repassa filtro mapeado, paginação e token ao repositório.
- Paginação inválida → erro de validação (422) sem chamar o repositório.
- Falha do repositório é logada e relançada.
- Cancelamento não é logado como erro.
- Mapeamento `MovimentoFiltroParameters` → `MovimentoFiltro`.

### Passo 3 — Service (Api)

**Criar** `MovimentoFiltroDto` (Service/Movimentos) com os cinco `Guid?` e `bool?` nomeados `ObjetivoId`, `TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId`, `EstaAtivo` (a query string usa camelCase por convenção de binding case-insensitive: `objetivoId` etc.). Incluir `ToParameters()` para `MovimentoFiltroParameters`.

**Alterar** `MovimentosController.GetMovimentos`:

- Nova assinatura: acrescentar o parâmetro `[FromQuery] MovimentoFiltroDto filtro` à action, mantendo `page`, `pageSize` e `CancellationToken` exatamente como já estão (mesmos atributos e valores padrão). Atenção à ordem: parâmetros com valor padrão ficam depois dos obrigatórios.
- Manter a checagem de `ModelState` → 422 **antes** de chamar a query. GUID malformado deve cair nela.
- Chamar a nova sobrecarga da query com o filtro mapeado.
- Atualizar a documentação (comentários XML/OpenAPI) dos novos parâmetros.

**Testes** (`Service.Test/Movimentos/MovimentosControllerTests.cs`):

- Controller repassa filtro, paginação e token à query.
- `ModelState` inválido (GUID malformado) → 422 sem chamar a query.
- Mapeamento `MovimentoFiltroDto` → `MovimentoFiltroParameters`.
- Ajustar testes existentes de `GetMovimentos` à nova assinatura (passar `new MovimentoFiltroDto()` onde não há filtro).
- Se houver testes de integração HTTP: requisição sem filtros inalterada; com um filtro; com GUID inválido → 422.

### Passo 4 — Presentation: cliente da Api

**Criar** `MovimentoFiltro` (record selado, namespace de Presentation/Movimentos) com cinco `Guid?` e `bool?`. Tipo **próprio** da Presentation; não referenciar tipos de Service/Application.

**Alterar** `IMovimentoApi` / `MovimentoApi`:

- Nova sobrecarga `GetPagedAsync(MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken)`.
- Montar `api/movimentos?page=..&pageSize=..` e acrescentar, nesta ordem fixa, `objetivoId, tipoRendaId, corretoraId, produtoId, emissorId, estaAtivo`, **omitindo nulos**. Booleano em minúsculas (`true`/`false`). Usar `FormattableString.Invariant`.
- Método antigo mantido.

**Testes** (`Presentation.Test`, `MovimentoApiTests`):

- URL sem filtros idêntica à atual.
- Um filtro; todos os filtros (verifica ordem fixa); `estaAtivo=true` e `false`; nulos omitidos; cultura invariante.

### Passo 5 — Presentation: Index (listagem)

**Criar** `MovimentoFiltroInput` (Presentation/Pages/Movimentos), classe de binding com os cinco `Guid?` e `bool?` e método `ToFiltro()` → `MovimentoFiltro`, além de um método que gera os route values (somente campos não nulos, com prefixo `Filtro.`).

**Alterar** `Index.cshtml.cs`:

- Propriedade `[BindProperty(SupportsGet = true)] MovimentoFiltroInput Filtro` (inicializada, nunca nula).
- `OnGetAsync` chama **sempre** `GetPagedAsync(Filtro.ToFiltro(), Pagina, TamanhoPagina, ct)`.
- Redirect para a última página quando a página não existe mais: manter os valores de filtro nos route values.
- `OnPostDeleteAsync`: após excluir, redirecionar preservando `Filtro` e `pagina` (com a lógica atual de página).
- Expor dicionário de route values (filtro + `pagina`) para uso na view (Editar, Novo, Excluir).
- Carregar opções dos selects via `IMovimentoFormLookupData` (todos os registros, inclusive inativos — P4).

**Alterar** `Index.cshtml`:

- Formulário `method="get"` (sem `pagina`, logo sempre página 1) contendo apenas os cinco selects + select Ativo, com `asp-for="Filtro.X"`; primeira opção `<option value="">--</option>`. Select Ativo: `--` (vazio), `Ativos` (`true`), `Inativos` (`false`).
- Botão **Filtrar** (submit) e link **Limpar filtro** apontando para `/Movimentos` sem parâmetros.
- Links Editar e Novo com os route values (`asp-route-*` de filtro e `pagina`).
- Formulário POST de Excluir: colocar filtro e `pagina` em `asp-route-*` **na action** do form (o TagHelper de paginação não faz isso em POST).
- `<lumia-pagination>` sem alterações (preserva demais parâmetros da query).
- Estado "sem resultados" para filtros sem correspondência, com mensagem clara.

**Testes** (`Presentation.Test/Pages/Movimentos/IndexModelTests.cs`):

- Ajustar os Setups existentes para a nova sobrecarga de `GetPagedAsync`.
- Filtro vazio é repassado como "sem filtro".
- Filtro preenchido é convertido e repassado à API (os 6 campos).
- Route values contêm somente campos preenchidos + `pagina`.
- Redirect para a última página preserva o filtro.
- `OnPostDeleteAsync` redireciona preservando filtro e página.
- Opções dos selects carregadas do lookup.

### Passo 6 — Presentation: Create e Edit

**Alterar** `Create.cshtml(.cs)` e `Edit.cshtml(.cs)`:

- Bind de `Filtro` (`[BindProperty(SupportsGet = true)]`, mesmo `MovimentoFiltroInput`), além de `Pagina` já existente.
- Persistir `Filtro` e `Pagina` no GET e no POST (campos ocultos `asp-for="Filtro.X"` ou route values na action do form; seguir o padrão já usado por `Pagina`).
- Redirects e links "Cancelar/Voltar" usam `RedirectToPage("Index", new { pagina = Pagina, ... filtro })` preservando o filtro.

**Testes** (`Presentation.Test`, testes de `CreateModel`/`EditModel`):

- Redirect após salvar preserva `pagina` e filtro.
- Filtro vazio mantém o comportamento atual.

### Passo 7 — Encerramento e documentação

- Mudar o ADR 0012 para **Aceita (data)**.
- Atualizar READMEs de Service (contrato `GET api/movimentos` com novos parâmetros) e de Presentation (filtros da listagem).
- Atualizar índice de ADRs/specs, se existir.
- Rodar `dotnet build` e `dotnet test` na solução completa.

## 5. Validação ponta a ponta (manual)

Com Api e Web no ar, usuário com massa de dados variada:

1. Abrir `/Movimentos` sem parâmetros: todos os movimentos, paginados (12 por página), selects em `--`.
2. Selecionar um filtro e clicar **Filtrar**: lista restrita, volta à página 1, URL contém `Filtro.X=...`.
3. Combinar dois filtros (ex.: Corretora + Ativos): resultado é a interseção.
4. Navegar para a página 2: filtro mantido na URL, selects continuam preenchidos, total de páginas coerente.
5. Editar um movimento a partir da página 2 filtrada, salvar: retorna à página 2 com o mesmo filtro. Repetir com **Cancelar** e com **Novo**.
6. Excluir o último item da última página filtrada: redireciona para a nova última página, mantendo o filtro.
7. Filtro sem correspondência: mensagem de "nenhum resultado", sem erro.
8. Clicar **Limpar filtro**: URL `/Movimentos`, todos os selects em `--`, listagem completa.
9. Chamar a Api diretamente com `objetivoId=abc`: resposta **422**; com ID válido inexistente: 200 e lista vazia.
10. Com dois usuários distintos, confirmar que um filtro com ID de outro usuário retorna lista vazia (sem vazamento).
11. (Recomendado) Em base com volume realista, rodar `EXPLAIN` das consultas filtradas e registrar o resultado; se houver `filesort`/varredura relevante, abrir novo ADR (gatilho do ADR 0012).

## 6. Riscos

| Risco | Mitigação |
|---|---|
| Quebra de testes existentes por mudança de assinatura da action e uso da nova sobrecarga no Index | Ajustes listados no item 3 dos pontos de atenção; sobrecargas em vez de opcionais |
| Perda do filtro em Excluir (POST) | `asp-route-*` na action do formulário; teste dedicado em `IndexModelTests` |
| Combinações de filtros lentas com índices atuais | Gatilho de medição (`EXPLAIN`) no ADR; índice composto só por novo ADR |
| Vazamento entre usuários | `UserId` sempre aplicado + teste de isolamento com filtro |
| Cinco chamadas extras de lookup por renderização | Aceito na escala atual (registrado no ADR); reavaliar com cache se necessário |
| Divergência de URL entre Web e Api | Ordem fixa e testes de montagem de URL em `MovimentoApiTests` |
| Necessidade inesperada de mudar `EFRepository` | Parar e sinalizar impacto em Web/Api antes de alterar contrato da LumiaFoundation |

## 7. Fora de escopo

- Alterações de schema/migration.
- Alterações no LumiaFoundation.
- Filtros por data ou texto.
- Persistir o último filtro usado.
- Exibir contagem de filtros ativos.
