# Especificação técnica 0018: Fluxo Mensal (Vault), parte 3 de 4

- Data: 2026-10-24
- Status: em andamento (passo 4)
- ADR relacionado: [0019](../adr/0019-fluxo-mensal.md)
- Esta parte continua as partes [1](0018-vault-fluxo-mensal.md) (ADR e Domain) e [2](0018-vault-fluxo-mensal-parte-2.md) (Persistence e Application). As decisões D1 a D14 e as "Atenções" da parte 1 continuam valendo.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e como validar.
> Todo código, comentário, commit e documentação em português.

---

## Resolução do ponto em aberto da parte 2 (registro das queries)

A parte 2 deixou para esta parte confirmar se as queries novas são registradas sem mexer no `Program.cs`. **São:** o `AddServicesFromAssembly` registra por convenção (interface e implementação no mesmo assembly), como aconteceu com `GetRentabilidadesPorTipoRendaQuery` na 0017. **Nenhuma alteração no `Program.cs`.** A confirmação fica no teste de registro (Passo 4.4).

---

## Escolhas desta parte (a confirmar)

| Item | Escolha |
| --- | --- |
| E1 | As duas ações entram no `DashboardsController` **existente** (D9), que passa a receber mais duas queries no construtor |
| E2 | Rotas `GET api/dashboards/retorno-mensal` e `GET api/dashboards/investido-mensal`, **sem parâmetros de entrada**: nada de `[FromQuery]`, nada de filtro. Os 12 meses são decididos pelo servidor |
| E3 | Resposta é a **lista de 12 meses** direto (array), sem envelope, como `rentabilidade` e `proximos-vencimentos` |
| E4 | O contrato do mês é uma `string` no formato **`AAAA-MM`** (propriedade `Mes`), gerada no DTO com `CultureInfo.InvariantCulture`. Alternativa descartada: expor `ano` e `mes` separados, que empurra a montagem do rótulo para o Web sem ganho |
| E5 | Todos os DTOs da feature em um arquivo, `Service/Dashboards/DTOs/FluxoMensalDtos.cs`, como `RentabilidadeDtos.cs` |
| E6 | `ProducesResponseType`: 200 e 500. **Sem 422**, porque não há entrada a rejeitar (diferente da rentabilidade); 401 já é coberto pelo `[Authorize]` |
| E7 | Valores monetários trafegam como `decimal` (número JSON). O Web desserializa em `decimal` (parte 4); nenhuma conversão para `double` no caminho |

---

## Passo 4: Service (Api)

### 4.1 DTOs (`Service/Dashboards/DTOs/FluxoMensalDtos.cs`, namespace `Service.Dashboards.DTOs`)

`record` posicionais, no estilo de `RentabilidadeItemDto`:

- **`RetornoMensalDto`**: `string Mes` (`AAAA-MM`), `decimal Total` e `int Quantidade`.
  - Fábricas estáticas `FromApplication(RetornoMensalModel)` e `FromApplication(List<RetornoMensalModel>)`.
- **`InvestidoMensalDto`**: `string Mes` (`AAAA-MM`), `decimal Novo`, `decimal Reinvestimento` e `decimal Total`.
  - Fábricas estáticas equivalentes, com `InvestidoMensalModel`. O `Total` é copiado do modelo (que já garante `Novo + Reinvestimento`); o DTO **não recalcula**.
- A montagem do `Mes` fica num único método auxiliar privado reutilizado pelos dois DTOs (ou numa classe interna `static` do mesmo arquivo): `Ano` com 4 dígitos, `Mes` com 2 dígitos e zero à esquerda (`2026-01`), sempre com `CultureInfo.InvariantCulture`, para o rótulo não depender da cultura do servidor.
- A ordem da lista **não é alterada** no mapeamento: a Application já a entrega cronológica crescente (D2).

### 4.2 `DashboardsController` (`Service/Dashboards/DashboardsController.cs`)

- Atributos de classe e rota base **inalterados** (`[ApiController]`, `[Authorize]`, `[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]`, `[RequestValidation]`, `[Route("api/dashboards")]`).
- Construtor: acrescentar `IGetRetornoMensalQuery` e `IGetInvestidoMensalQuery` à dependência já existente (`IGetRentabilidadesPorTipoRendaQuery`).
- Duas ações novas:
  - `GET retorno-mensal`: `Task<ActionResult<List<RetornoMensalDto>>> GetRetornoMensal(CancellationToken cancellationToken)`.
  - `GET investido-mensal`: `Task<ActionResult<List<InvestidoMensalDto>>> GetInvestidoMensal(CancellationToken cancellationToken)`.
- Em ambas: `GetCurrentUserId()` para o `ownerId` (nunca vem da requisição); chamar a query com `ownerId` e `cancellationToken`; devolver `Ok(Dto.FromApplication(...))`. O `CancellationToken` é obrigatório e sem valor padrão (D13).
- `ProducesResponseType`: 200 (lista do DTO) e 500 (`ErrorDetails`) (E6).
- Sem `try/catch`: falhas inesperadas viram 500 pelo tratamento já configurado.

### 4.3 `Program.cs`

**Sem alteração** (ver a resolução no topo).

### 4.4 Testes (`Service.Test`)

**`Dashboards/DashboardsControllerTests.cs`** (arquivo existente). Atenção: o construtor do controller muda, então **todos os testes atuais que instanciam o controller precisam passar os dois novos mocks** (um helper de criação evita repetir). Depois disso, os testes da rentabilidade devem continuar verdes sem alteração de comportamento.

- `GetRetornoMensal_RepassaUsuarioETokenEMapeia`: `Setup` da query com o `ownerId` de `Items["UserId"]` e o `cts.Token`; o retorno é `OkObjectResult` com a lista de DTOs mapeada (mês `AAAA-MM`, total e quantidade).
- `GetRetornoMensal_UsaOUsuarioDoToken`: o `ownerId` repassado é o do `Items["UserId"]`; nenhum parâmetro da requisição o altera (a ação nem recebe parâmetros).
- `GetRetornoMensal_ListaCom12Meses_PreservaOrdem`: 12 modelos entram, 12 DTOs saem, na mesma ordem.
- `GetInvestidoMensal_RepassaUsuarioETokenEMapeia`: idem, com `Novo`, `Reinvestimento` e `Total`.
- `GetInvestidoMensal_UsaOUsuarioDoToken` e `GetInvestidoMensal_ListaCom12Meses_PreservaOrdem`: idem.
- `PossuiFiltroDeValidacaoDeRequisicao`: já existe e continua cobrindo as duas ações novas (o atributo é de classe).
- **Isolamento das seções:** um teste por ação garante que ela chama **somente** a sua query (`Verify(..., Times.Never)` na outra), para uma falha de uma seção nunca depender da outra (D11).

**`Dashboards/FluxoMensalDtosTests.cs`** (novo, no estilo de `RentabilidadeDtosTests`):

- **Formato do mês:** `Ano = 2026, Mes = 1` → `"2026-01"`; `Ano = 2026, Mes = 12` → `"2026-12"`.
- **Cultura não interfere:** o mesmo mapeamento sob `CultureInfo.CurrentCulture` de uma cultura não latina (por exemplo `ar-SA`, restaurando a cultura ao fim do teste) continua devolvendo dígitos ocidentais (`"2026-01"`).
- **Mapeamento completo:** `RetornoMensalDto` copia `Total` e `Quantidade`; `InvestidoMensalDto` copia `Novo`, `Reinvestimento` e `Total`.
- **Total não é recalculado:** o DTO copia o `Total` do modelo (usar valores que respeitam a invariante e conferir a igualdade, sem lógica própria no DTO).
- **Ordem preservada** nas fábricas de lista, incluindo lista de 12 itens.
- **Contrato JSON (trava para o Web):** serializar um DTO com `JsonSerializerDefaults.Web` e conferir os nomes `mes`, `total`, `quantidade` (Retorno) e `mes`, `novo`, `reinvestimento`, `total` (Investido), com os valores `decimal` como número. O teste protege a parte 4, que desserializa esse contrato.

**`Registro/RegistroDeServicosTests.cs`:** acrescentar `[InlineData(typeof(IGetRetornoMensalQuery))]` e `[InlineData(typeof(IGetInvestidoMensalQuery))]` ao `ResolveServicosEsperados` (e os `using` das queries).

**`Arquitetura/CancellationTokenTests.cs`:** nenhuma alteração. O teste descobre as novas ações sozinho e deve continuar verde porque ambas recebem o token obrigatório. Se falhar, o token foi esquecido.

### 4.5 Como validar

```
dotnet build
dotnet test Service.Test --filter "FullyQualifiedName~Dashboards|FullyQualifiedName~Registro|FullyQualifiedName~Arquitetura"
dotnet test
```

Validação manual na Api em execução (com um token válido, ajustando host e porta; `$TOKEN` vem do login):

```
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/retorno-mensal"
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/investido-mensal"
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:<porta>/api/dashboards/retorno-mensal"     # 401
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:<porta>/api/dashboards/investido-mensal"   # 401
```

Pontos de atenção:

- **Esta é a primeira execução real das consultas da parte 2 contra o MariaDB.** Conferir no log do EF que a projeção por construtor é traduzida sem erro e que a consulta usa só o `UserId` e as datas esperadas. Se houver erro de tradução, voltar à parte 2 (não contornar no controller).
- Conferir que cada resposta tem **exatamente 12** itens, em ordem crescente, começando em `AAAA-MM` do mês atual (Retorno) ou 11 meses antes (Investido).
- Conferir um mês com valores conhecidos contra os dados (somar na mão ou por SQL): vencimentos do mês no Retorno e aportes novos/reinvestimentos no Investido.
- Conferir que um **vencido ainda ativo** aparece somado no mês atual do Retorno e que um Movimento **encerrado** (`EstaAtivo = false`) aparece no Investido, mas **não** no Retorno.
- Conferir o isolamento por usuário: rodar com dois usuários e confirmar que cada um vê só os próprios valores.
- Conferir que um Movimento sem `DataVencimento` não entra no Retorno.

**Critério de pronto:** build e testes verdes, `[RequestValidation]` e `[Authorize]` herdados, ações com `CancellationToken` obrigatório, `UserId` só do token, nenhum pacote novo e `Program.cs` inalterado.

---

## Fora do escopo desta parte

Cliente da Api no Web, página `/Dashboards/FluxoMensal`, tabelas, gráficos Chart.js, notas na tela, submenu em **Dashboards**, documentação final e fechamento do ADR (parte 4).
