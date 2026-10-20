# Especificação técnica 0017: Dashboard de Rentabilidade (Vault), parte 3 de 4

- Data: 2026-10-20
- Status: **em elaboração (parte 3 de 4: Passo 4)**
- ADR relacionado: [0018](../adr/0018-dashboard-de-rentabilidade.md)
- Esta parte continua as partes [1](0017-vault-dashboard-rentabilidade-parte-1.md) (ADR e Domain) e [2](0017-vault-dashboard-rentabilidade-parte-2.md) (Persistence e Application). As decisões D1 a D17 e as "Atenções" da parte 1 continuam valendo.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e como validar.
> Todo código, comentário, commit e documentação em português.

---

## Resolução do ponto em aberto da parte 2 (`ordem` inválida: 400 ou 422?)

A dúvida era se um `ordem` fora de `maiores`/`menores` responderia 400 no *model binding*. **Não responde**, e **não é preciso código extra**:

- O `Program.cs` configura `SuppressModelStateInvalidFilter = true`, então o `[ApiController]` **não** devolve 400 automático para `ModelState` inválido.
- O controller leva `[RequestValidation]` (LumiaFoundation, spec 0015 parte 3, Passo L). O filtro lança `HttpBaseException` **422** quando `ModelState` é inválido, com a mensagem "Parâmetros inválidos: {chaves}." (só os nomes dos campos, nunca os valores enviados).
- Resultado por caso:

| Requisição | Quem rejeita | Resposta |
| --- | --- | --- |
| `ordem=abc` ou `top=abc` (não converte) | `ModelState` + `[RequestValidation]` | 422 |
| `ordem=7` (número fora do enum; o binder aceita) | `[EnumDataType]` em `RentabilidadeParameters`, via `CommandValidator` na query | 422 |
| `top=0` ou `top=101` | `[Range]` em `RentabilidadeParameters`, via `CommandValidator` na query | 422 |
| `top` e `ordem` ausentes | valores padrão (25 e `maiores`) | 200 |

A confirmação fica nos testes unitários dos DTOs (padrões) e na validação manual do Passo 4.5, porque o comportamento do filtro é da biblioteca.

---

## Escolhas desta parte (a confirmar)

| Item | Escolha |
| --- | --- |
| E1 | Filtro de entrada como **DTO** `[FromQuery] RentabilidadeFiltroDto` (`Top` e `Ordem`, com padrões), espelhando `MovimentoFiltroDto`, em vez de parâmetros soltos na ação |
| E2 | `Ordem` usa o enum `OrdemRentabilidade` do Application. O binder aceita `maiores`/`menores` **sem diferenciar maiúsculas** e também `0`/`1`. Aceito por simplicidade; a alternativa é um `string` com `[RegularExpression]` e mapeamento manual, mais código para o mesmo efeito |
| E3 | Resposta é a **lista de grupos** direto (array), sem envelope, como `proximos-vencimentos` |
| E4 | Controller e DTOs em `Service/Dashboards/` (e `Service/Dashboards/DTOs/`), com todos os DTOs desta feature em um arquivo, como `DistribuicaoValorLiquidoDto.cs` |
| E5 | Sem alteração em `Program.cs`: a query nova é registrada pelo `AddServicesFromAssembly`, e o teste de registro passa a cobri-la |
| E6 | Rota `GET api/dashboards/rentabilidade` (D13), reservando `api/dashboards/...` para os próximos dashboards |

---

## Passo 4: Service (Api)

### 4.1 DTOs (`Service/Dashboards/DTOs/RentabilidadeDtos.cs`, namespace `Service.Dashboards.DTOs`)

- **`RentabilidadeFiltroDto`** (`record`, no estilo de `MovimentoFiltroDto`):
  - `int Top { get; init; } = RentabilidadeParameters.TopPadrao`;
  - `OrdemRentabilidade Ordem { get; init; } = OrdemRentabilidade.Maiores`;
  - `ToParameters()` devolve `RentabilidadeParameters` com os dois valores. **Sem validação aqui:** quem valida é o Application (faixa e enum), para a regra ficar num lugar só.
- **`RentabilidadeItemDto`** (`record` posicional, no estilo de `AporteDistribuidoDto`): `Guid Id`, `string? ProdutoNome`, `string? EmissorNome`, `string? ObjetivoNome`, `string? CorretoraNome`, `decimal ValorAporte`, `decimal ValorLiquidoAtual`, `int DiasInvestido`, `double Acumulada`, `double Diaria`, `double Anualizada`. As três rentabilidades são **fração** (0,225 = 22,5%), D14.
- **`RentabilidadeGrupoDto`** (`record` posicional): `Guid TipoRendaId`, `string? TipoRendaNome`, `List<RentabilidadeItemDto> Itens`.
- Fábricas estáticas `FromApplication`: uma para o grupo (mapeando os itens) e uma para a lista de grupos, como em `ProximoVencimentoDto`.

### 4.2 `DashboardsController` (`Service/Dashboards/DashboardsController.cs`)

- Atributos de classe, na mesma ordem dos controllers atuais: `[ApiController]`, `[Authorize]`, `[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]`, `[RequestValidation]` e `[Route("api/dashboards")]`. Herda de `BaseApiController`.
- Dependência única: `IGetRentabilidadesPorTipoRendaQuery`.
- Ação: `GET rentabilidade`, com `[FromQuery] RentabilidadeFiltroDto filtro` e `CancellationToken cancellationToken` (sem valor padrão). Retorno `ActionResult<List<RentabilidadeGrupoDto>>`.
  - `filtro ??= new RentabilidadeFiltroDto()`, como em `GetMovimentos`.
  - `GetCurrentUserId()` para o `ownerId` (nunca vem da requisição).
  - Chama a query com `filtro.ToParameters()` e devolve `Ok(RentabilidadeGrupoDto.FromApplication(...))`.
- `ProducesResponseType`: 200 (`List<RentabilidadeGrupoDto>`), 422 e 500 (`ErrorDetails`), como `GetMovimentos`.
- Sem `try/catch`: erros de regra viram 422 pelos handlers já registrados, e falhas inesperadas, 500.

### 4.3 `Program.cs`

**Sem alteração** (E5). Conferir que `AddServicesFromAssembly(typeof(IGetCorretoraQuery).Assembly)` registra a query nova, pela convenção de interface e implementação do mesmo assembly.

### 4.4 Testes (`Service.Test`)

- **`Dashboards/DashboardsControllerTests.cs`**, no estilo de `MovimentosControllerTests` (`Mock` da query, `ControllerContext` com `DefaultHttpContext` e `Items["UserId"]`):
  - `PossuiFiltroDeValidacaoDeRequisicao`: o controller tem `RequestValidationAttribute` (como em `DistribuicaoValorLiquidoControllerTests`).
  - `GetRentabilidade_RepassaUsuarioParametrosETokenEMapeia`: `Setup` da query com `It.Is<RentabilidadeParameters>(p => p.Top == 10 && p.Ordem == OrdemRentabilidade.Menores)` e o `cts.Token`; o retorno é `OkObjectResult` com a lista de DTOs mapeada (grupo, itens e as três rentabilidades).
  - `GetRentabilidade_SemFiltro_UsaPadroes`: filtro nulo ou `new RentabilidadeFiltroDto()` chama a query com `Top = 25` e `Ordem = Maiores`.
  - `GetRentabilidade_ListaVazia_RetornaOkComListaVazia`.
  - `GetRentabilidade_UsaOUsuarioDoToken`: o `ownerId` repassado é o do `Items["UserId"]`, não um valor vindo do filtro.
- **`Dashboards/RentabilidadeDtosTests.cs`** (no estilo de `MovimentoDtoTests`):
  - `RentabilidadeFiltroDto` sem argumentos tem `Top = 25` e `Ordem = Maiores`; `ToParameters` copia os dois valores.
  - `FromApplication` mapeia todos os campos do item (nomes, valores, `DiasInvestido`, rentabilidades) e preserva a ordem de grupos e itens.
- **`Registro/RegistroDeServicosTests.cs`:** acrescentar `[InlineData(typeof(IGetRentabilidadesPorTipoRendaQuery))]` ao `ResolveServicosEsperados` (e o `using` da query).
- **`Arquitetura/CancellationTokenTests.cs`:** nenhuma alteração. O teste descobre o controller novo sozinho e deve continuar verde, porque a ação recebe o token obrigatório. Se ele falhar, o token foi esquecido.

### 4.5 Como validar

```
dotnet build
dotnet test Service.Test --filter "FullyQualifiedName~Dashboards|FullyQualifiedName~Registro|FullyQualifiedName~Arquitetura"
dotnet test
```

Validação manual na Api em execução (com um token válido, ajustando host e porta; `$TOKEN` vem do login):

```
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/rentabilidade"
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/rentabilidade?top=5&ordem=menores"
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/rentabilidade?top=0"      # 422
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/rentabilidade?top=101"    # 422
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/rentabilidade?ordem=abc"  # 422
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/rentabilidade?ordem=7"    # 422
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:<porta>/api/dashboards/rentabilidade"                                             # 401
```

Pontos de atenção:

- Conferir que `ordem=MAIORES` (maiúsculas) responde 200.
- Conferir no OpenAPI (`MapOpenApiDevTools`) como o parâmetro `ordem` aparece; é provável que seja listado como inteiro (E2). Se isso incomodar, abrir decisão sobre a alternativa com `string`.
- Conferir que um usuário só vê os próprios Movimentos (rodar com dois usuários).

**Critério de pronto:** build e testes verdes, `[RequestValidation]` presente, ação com `CancellationToken` obrigatório, `UserId` só do token, nenhum pacote novo e `Program.cs` inalterado.

---

## Fora do escopo desta parte

Cliente da Api no Web, página `/Dashboards/Rentabilidade`, menu **Dashboards**, formatação em % e fechamento do ADR (parte 4).
