# Especificação técnica 0015: Atualização do Valor Líquido por cotação (Vault), parte 2 de 3

- Data: 2026-10-17
- Status: **em elaboração (parte 2 de 3: Passos 4 e 5)**
- ADR relacionado: 0016
- Esta parte continua a parte 1 (Passos 0 a 3). Os itens 2 e 3 da seção "Ajustes à parte 1" valem a partir de agora.

---

## Ajustes à parte 1 e ao ADR 0016

1. **Limpeza aprovada:** remover `GetAtivosPorTituloAsync` e `TituloFiltro` (Passo 3.3) deixa de ser opcional. Continua em commit separado, antes do restante do Passo 3.
2. **Errata do ADR 0016 (aplicar no texto):**
   - O upsert vale **somente na tela de atualização por cotação**. No CRUD de Cotação, `POST` com `(Papel, Data)` já existente responde **422** ("Já existe cotação para este papel nesta data. Edite o registro existente."), e `PUT` que leve o registro a colidir com outro também responde 422. Motivo: o CRUD distingue criar de editar, e substituir silenciosamente em um `POST` surpreenderia quem chama a Api.
   - O `POST api/movimentos/atualizacao-por-cotacao` responde **200 com corpo** (`movimentosAtualizados`, `quantidadeMovimentos`), e não 204, para a tela informar se houve recálculo ou só histórico (regra D8).
3. **Validação:** `CommandValidator` usa `Validator.TryValidateObject(..., validateAllProperties: true)`. O `IValidatableObject` só é executado quando os atributos das propriedades passam; portanto, a mensagem de "Quantidade obrigatória com a flag" aparece depois que os demais erros forem corrigidos. Aceito e coberto em teste.
4. **Nenhuma alteração na LumiaFoundation** (`SaveAsync(ct)` e `ToPagedListAsync` já existem).

---

## Passo 4: Application

Todas as novas assinaturas recebem `CancellationToken` **sem valor padrão**. A data de hoje vem do `IDateTimeProvider`, lida **uma única vez** por execução (`GetDateTime().Date`). Mensagens em português. `OperationCanceledException` não é logada como erro nas queries.

### 4.1 Movimento: flag e validação (arquivos existentes)

- `MovimentoModelForCreation` e `MovimentoModelForUpdate`:
  - nova propriedade `bool AtualizaPorCotacao`;
  - implementar `IValidatableObject`: se `AtualizaPorCotacao` e `Quantidade` for nula ou menor ou igual a zero, devolver um `ValidationResult` com a mensagem "Quantidade é obrigatória e deve ser maior que zero quando o valor varia por cotação" e `MemberNames = [nameof(Quantidade)]`;
  - `ToDomain` e `UpdateDomain` copiam a flag. `UpdateDomain` continua **sem** tocar em `DataAtualizacaoValorLiquido` (ADR 0011).
- `MovimentoModel`: expor `AtualizaPorCotacao` em `FromDomain`.
- `CreateMovimentoCommand` e `UpdateMovimentoCommand`: sem mudança de código. A validação já ocorre em `CommandValidator.Validate` **antes** do validador de referências, então flag sem quantidade falha sem consultar o banco.

### 4.2 Cotações (CRUD)

Pasta `Application/Cotacoes/`, no estilo de `Corretoras`.

**Modelos**

- `CotacaoModel`: `Id`, `UserId`, `Papel`, `Valor`, `Data`, com `FromDomain(Cotacao)`.
- `CotacaoPontoModel` (record): `Data`, `Valor`. Usado só na série do gráfico (evita trafegar campos inúteis).
- `CotacaoModelForCreation` (record): `Papel` (`[Required]`, `[MaxLength(60)]`), `Valor` (`[Range(typeof(decimal), "0.0001", "99999999999999.9999")]`, mensagem "O valor da cotação deve ser maior que zero"; o teto é o máximo de `decimal(18,4)`) e `Data` (`[Required]`). `ToDomain(Guid userId)` faz `Trim` no papel e usa `Data.Date`.
- `CotacaoModelForUpdate`: mesmos campos e regras; `UpdateDomain(Cotacao)`.

**Commands** (`Commands/CreateCotacao`, `UpdateCotacao`, `DeleteCotacao`; interface e implementação por pasta)

- `CreateCotacaoCommand(IRepositoryManager, IDateTimeProvider)`: `ExecuteAsync(CotacaoModelForCreation, Guid userId, CancellationToken)` retorna `CotacaoModel`.
  1. `CommandValidator.Validate`.
  2. Data posterior a hoje: `CommandValidationException` ("A data da cotação não pode ser futura.").
  3. `GetPorPapelEDataAsync` (sem tracking): se existir, `CommandValidationException` com a mensagem da errata.
  4. `Create` e `SaveAsync(ct)`.
- `UpdateCotacaoCommand`: `ExecuteAsync(CotacaoModelForUpdate, Guid ownerId, Guid cotacaoId, CancellationToken)`.
  1. Validar modelo e data não futura.
  2. `GetAsync(..., trackChanges: true, ct)`; `null` lança `EntityNotFoundException("Cotação não encontrada")`.
  3. Se papel (após `Trim`) ou data mudaram, procurar colisão com **outro** registro (`Id` diferente) e lançar `CommandValidationException`.
  4. `UpdateDomain`, `Update`, `SaveAsync(ct)`.
- `DeleteCotacaoCommand`: `GetAsync` com tracking, `EntityNotFoundException` se não achar, `Delete`, `SaveAsync(ct)`. Excluir uma cotação não tem restrição de uso.

**Queries** (`Queries/GetCotacao`, `GetCotacaoList`, `GetPapeisCotados`, `GetSerieCotacao`)

- `GetCotacaoQuery`: `ExecuteAsync(Guid ownerId, Guid cotacaoId, CancellationToken)`; `EntityNotFoundException` se não achar.
- `GetCotacaoListQuery`: `ExecuteAsync(Guid ownerId, string? papel, PaginationParameters paginacao, CancellationToken)` retorna `PagedList<CotacaoModel>`. `CommandValidator.Validate(paginacao)` **fora do `try`**; `Trim` no filtro (vazio vira `null`); falhas logadas e relançadas, exceto cancelamento (padrão de `GetMovimentosListQuery`).
- `GetPapeisCotadosQuery`: `ExecuteAsync(Guid ownerId, CancellationToken)` retorna `List<string>`.
- `GetSerieCotacaoQuery`: `ExecuteAsync(Guid ownerId, string papel, CancellationToken)` retorna `List<CotacaoPontoModel>`. Papel vazio: `CommandValidationException`.

### 4.3 Atualização por cotação

Pasta `Application/Movimentos/AtualizacaoPorCotacao/` (tipos compartilhados), `Queries/GetAtualizacaoPorCotacao/` e `Commands/AtualizarPorCotacao/`.

**Tipos**

- `PapelParaCotacaoModel` (record): `Papel`, `UltimoValor` (`decimal?`) e `DataUltimaCotacao` (`DateTime?`).
- `AtualizacaoPorCotacaoParameters` (record): `Papel` (`[Required]`, `[MaxLength(60)]`), `Valor` (mesma regra do CRUD) e `Data` (`[Required]`).
- `AtualizacaoPorCotacaoResultadoModel` (record): `bool MovimentosAtualizados` e `int QuantidadeMovimentos`.

**Query `GetAtualizacaoPorCotacaoQuery`** (`ExecuteAsync(Guid ownerId, CancellationToken)` retorna `List<PapelParaCotacaoModel>`)

1. `Movimento.GetPapeisPorCotacaoAsync`. Lista vazia: devolver vazio sem segunda consulta.
2. `Cotacao.GetUltimasPorPapelAsync(ownerId, papeis, ct)`.
3. Combinar em memória por papel com `StringComparer.OrdinalIgnoreCase`; papel sem cotação fica com `UltimoValor` e `DataUltimaCotacao` nulos. Ordem por papel ascendente.
4. Falhas logadas e relançadas, exceto cancelamento.

**Command `AtualizarPorCotacaoCommand`** (dependências: `IRepositoryManager` e `IDateTimeProvider`). `ExecuteAsync(AtualizacaoPorCotacaoParameters, Guid ownerId, CancellationToken)` retorna `AtualizacaoPorCotacaoResultadoModel`:

1. `CommandValidator.Validate`; `Trim` no papel; `hoje = GetDateTime().Date`; data posterior a hoje: `CommandValidationException`.
2. `existente = Cotacao.GetPorPapelEDataAsync(..., trackChanges: true, ct)`. Se existir, atualizar `Valor` (mantendo o texto do papel já gravado); senão `Create` de nova `Cotacao`.
3. `maisRecente = Cotacao.GetDataMaisRecenteAsync(...)` (consulta feita **antes** do `SaveAsync`, portanto sem a cotação pendente). `recalcula = maisRecente is null || data.Date >= maisRecente.Value.Date` (D8).
4. Se `recalcula`: `Movimento.GetAtivosPorCotacaoDoPapelAsync` (com tracking) e, para cada Movimento, `AplicarCotacao(valor, hoje)` e `Update`.
5. **Um único** `SaveAsync(cancellationToken)`: cotação e movimentos na mesma transação.
6. Retornar `MovimentosAtualizados = recalcula` e `QuantidadeMovimentos` (zero quando só histórico ou quando o papel não tem Movimentos).

Sem `try/catch` com log (como nos commands existentes). A mutação dos Movimentos só ocorre depois das validações.

### 4.4 Registro

A query e os commands novos devem ser descobertos pelo `AddServicesFromAssembly` (confirmar no Passo 5). O `IDateTimeProvider` já está registrado na Api.

### 4.5 Testes (`Application.Test`)

Estilo de `UpdateValorLiquidoMovimentoCommandTests` (`Mock<IRepositoryManager>`, `Mock<IDateTimeProvider>` com data fixa, `Mock<ICotacaoRepository>` e `Mock<IMovimentoRepository>`).

**Movimento (regressão)**

- `MovimentoModelForCreation`/`ForUpdate`: flag `true` com `Quantidade` nula, zero e negativa falha (`Theory`) via `CommandValidator`; flag `true` com quantidade positiva passa; flag `false` com quantidade nula passa.
- `ToDomain`/`UpdateDomain` copiam a flag; `UpdateDomain` não altera `DataAtualizacaoValorLiquido`.
- `CreateMovimentoCommand`: flag sem quantidade lança `CommandValidationException` **sem** chamar o validador de referências nem persistir.
- `MovimentoModel.FromDomain` expõe a flag.

**`CreateCotacaoCommand`**

- Sucesso: grava com papel sem espaços, `Data` só com a data, `UserId` do dono; `SaveAsync(token)` uma vez; devolve o modelo.
- Data futura, papel vazio ou só espaços, valor zero ou negativo (`Theory`): exceção de validação, sem consultar nem persistir.
- Duplicidade `(papel, data)`: `CommandValidationException` com a mensagem esperada, sem `Create`.
- Dono e token propagados.

**`UpdateCotacaoCommand`**

- Sucesso altera os campos e persiste uma vez.
- Inexistente ou de outro usuário: `EntityNotFoundException`.
- Mudança de papel ou data que colide com **outro** registro: exceção; colisão com o **próprio** registro (nada mudou): permitido.
- Data futura: exceção. Token propagado.

**`DeleteCotacaoCommand`**: exclui e persiste; inexistente lança `EntityNotFoundException` sem `SaveAsync`.

**Queries de cotação**

- Lista: repassa dono, papel (`Trim`; vazio vira `null`), página e token; paginação inválida lança exceção **sem** consultar; falha é logada e relançada; cancelamento não é logado.
- Série e papéis: repassam dono, papel e token; papel vazio na série lança exceção.

**`GetAtualizacaoPorCotacaoQuery`**

- Combina papéis dos Movimentos com a última cotação (valor e data); papel sem cotação vem com nulos.
- Combinação ignora diferença de maiúsculas entre `Emissor.Nome` e `Cotacao.Papel`.
- Sem papéis: lista vazia e **não** consulta cotações.
- Falha logada e relançada; cancelamento não logado.

**`AtualizarPorCotacaoCommand`**

- Cotação nova e data mais recente: grava `Cotacao`, recalcula todos os Movimentos do papel (valor = quantidade × cotação, `DataAtualizacaoValorLiquido` = hoje), um único `SaveAsync(token)`; resultado `true` com a quantidade.
- Cotação existente no mesmo papel e data: atualiza o valor em vez de criar outra; recalcula.
- Primeira cotação do papel (sem histórico): recalcula.
- Data **anterior** à mais recente: grava só o histórico; nenhum Movimento consultado nem alterado; resultado `false` e zero.
- Data igual à mais recente: recalcula.
- Data futura, valor inválido, papel vazio: exceção de validação, sem consultar nem persistir.
- Papel sem Movimentos com flag: grava o histórico, resultado `true` e quantidade zero.
- Os demais campos dos Movimentos (`ValorAporte`, `Observacao`) ficam intactos; Movimento de outro papel não é tocado.
- Dono e token chegam aos repositórios e ao `SaveAsync`.

**Registro:** teste com `ServiceCollection` e `AddServicesFromAssembly`, resolvendo as queries e commands novos (padrão de `DistribuicaoValorLiquidoRegistroTests`).

### 4.6 Como validar

`dotnet test Application.Test` e `dotnet build` da solution (o Service ainda não usa os tipos novos, mas `MovimentoModel` mudou).

**Critério de pronto:** testes verdes; validação da flag antes de qualquer consulta; mutação dos Movimentos só depois do cálculo e das validações; um só `SaveAsync` por comando.

---

## Passo 5: Service (Api)

Sem pacote novo.

### 5.1 DTOs (`Service/Cotacoes/DTOs/` e `Service/Movimentos/DTOs/`)

- `CotacaoDto` (`Id`, `Papel`, `Valor`, `Data`; sem `UserId`), com `FromApplication` para item e lista.
- `CotacaoForCreationDto` e `CotacaoForUpdateDto` (records com `init`, sem DataAnnotations; a validação é do Application), com `ToCreateCotacaoCommand()`/`ToUpdateCotacaoCommand()`.
- `CotacaoPontoDto` (`Data`, `Valor`) e `FromApplication` para lista.
- `PapelParaCotacaoDto` e `AtualizacaoPorCotacaoSolicitacaoDto` (com `ToParameters()`) e `AtualizacaoPorCotacaoResultadoDto`.
- `MovimentoDto`, `MovimentoForCreationDto` e `MovimentoForUpdateDto`: acrescentar `AtualizaPorCotacao` e mapear em `FromApplication`/`ToCreateMovimentoCommand`/`ToUpdateMovimentoCommand`.

### 5.2 Guarda do corpo

Com `SuppressModelStateInvalidFilter` ligado, um corpo malformado pode chegar nulo. Criar `Service/Extensions/ControllerBaseExtensions.cs` com um método de extensão interno `ValidarCorpo(this ControllerBase controller, object? dto)` que lança `HttpBaseException` 422 ("Corpo da requisição inválido.") se o DTO for nulo ou o `ModelState` for inválido. Usar nos controllers **novos**. A refatoração do `DistribuicaoValorLiquidoController` para usá-lo fica fora desta feature.

### 5.3 `CotacoesController` (`Service/Cotacoes/CotacoesController.cs`)

Atributos iguais aos demais (`[ApiController]`, `[Authorize]`, `[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]`, `[Route("api/cotacoes")]`). Todas as ações repassam `HttpContext.RequestAborted`.

- `GET` (`papel`, `page`, `pageSize` por query): 200 `PagedResponse<CotacaoDto>`. Mesma checagem de `ModelState` de `GetMovimentos` (422).
- `GET papeis`: 200 lista de strings.
- `GET serie?papel=`: 200 lista de `CotacaoPontoDto`.
- `GET {id:guid}` (`Name = "CotacaoById"`): 200.
- `POST`: 201 com `CreatedAtRoute`.
- `PUT {id:guid}`: 204.
- `DELETE {id:guid}`: 204.
- `ProducesResponseType` completos (200/201/204, 404, 422, 500). As rotas literais `papeis` e `serie` não colidem com `{id:guid}`.

### 5.4 `AtualizacaoPorCotacaoController` (`Service/Movimentos/`)

`[Route("api/movimentos/atualizacao-por-cotacao")]`, mesmos atributos.

- `GET`: 200 lista de `PapelParaCotacaoDto`.
- `POST`: corpo `AtualizacaoPorCotacaoSolicitacaoDto`, `ValidarCorpo`, resposta **200** com `AtualizacaoPorCotacaoResultadoDto`. `ProducesResponseType`: 200, 422, 500.

### 5.5 `Program.cs`

Confirmar o registro por convenção; se a convenção não pegar algum tipo, `AddScoped` explícito com comentário do motivo.

### 5.6 Testes (`Service.Test`)

- `CotacoesControllerTests` (padrão de `MovimentosControllerTests`): lista repassa filtro, paginação e token e mapeia o envelope; `ModelState` inválido (page não numérico) lança 422 sem consultar; papéis e série mapeados; `GET {id}`; `POST` devolve `CreatedAtRouteResult` com `CotacaoDto` sem `UserId`; `PUT`/`DELETE` devolvem `NoContent` e repassam dono, id e token; corpo nulo ou `ModelState` inválido em `POST`/`PUT` lança 422 sem chamar o command.
- `AtualizacaoPorCotacaoControllerTests`: `GET` mapeia a lista (inclusive nulos); `POST` repassa parâmetros, dono e token e devolve o resultado; corpo inválido lança 422.
- Testes de DTO: mapeamentos de ida e volta de cada DTO novo; `MovimentoDto` e os DTOs de entrada com a flag (estender `MovimentoDtoTests` e `MovimentoForCreationAndUpdateDtoTests`).
- Teste de registro das queries e commands novos (se não foi coberto no Application).
- Ajustar os testes existentes que constroem `MovimentoModel`/DTOs, se algum quebrar.

### 5.7 Como validar

- `dotnet test Service.Test`; subir a Api e conferir no OpenAPI as rotas novas.
- Chamadas manuais com token: `POST api/cotacoes` duas vezes com o mesmo papel e data (segunda dá 422); `POST api/movimentos/atualizacao-por-cotacao` com data retroativa (200, `movimentosAtualizados: false`); `PUT` de Movimento com flag e sem quantidade (422); usuário B nunca vê nem altera cotações do usuário A; sem token, 401.

**Critério de pronto:** testes verdes, rotas no OpenAPI, nenhuma alteração no `MovimentosController` além dos DTOs.

---

## Próxima parte

**Parte 3:** Presentation (CRUD de Cotação, tela de atualização, gráfico com Chart.js, checkbox no Create/Edit de Movimento, menu), documentação final, validação ponta a ponta (incluindo a rodada contra MariaDB) e riscos.
