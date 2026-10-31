# Especificação técnica 0021: Consolidado de Movimentos (Vault), parte 2 de 3

- Data: 2026-10-30
- Status: **em elaboração (parte 2 de 3: Passo 3, Service/Api)**
- ADR relacionado: [0022](../adr/0022-consolidado-de-movimentos.md)
- Parte anterior: [0021-vault-consolidado.md](0021-vault-consolidado.md) (Passos 0 a 2)
- Repositório afetado nesta parte: **Service (Api)**. Sem schema, sem migration, sem dependência nova, sem Domain e sem alteração no LumiaFoundation.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Ajustes à parte 1 e ao ADR 0022 (aprovados ao seguir para esta parte)

Os pontos P1 e P2 ficam **fechados com as recomendações**:

| Item | Decisão |
| --- | --- |
| P1 | O percentual é só sobre o **Total Líquido**, em uma coluna |
| P2 | O resultado é exibido quando há **ao menos um filtro ou um agrupamento** escolhido. Sem nenhum dos dois, a Api responde 422 e o Web nem a chama |

Textos a corrigir antes de implementar:

1. **ADR 0022**, seção "Quando há resultado": trocar por "A tela abre sem resultado, com uma orientação. O resultado é exibido quando há ao menos um filtro **ou** um agrupamento. A Api responde 422 quando não há nenhum dos dois."
2. **Spec parte 1, Passo 2.2, item 1:** a regra passa a ser "sem nenhum filtro **e** sem `AgruparPor`": `CommandValidationException("Informe ao menos um filtro ou um agrupamento.")`. O teste correspondente da Application passa a cobrir: sem filtro e sem agrupamento (422, sem consulta), só agrupamento (aceito) e só filtro (aceito).
3. **Spec parte 1, "Partes 2 e 3":** a resposta **não** traz `agruparPor`; o Web já sabe o que enviou.
4. **Percentual do total:** a linha de total tem `PercentualLiquido` igual a 1 (100%) quando o líquido do conjunto é diferente de zero, e 0 caso contrário (já definido na parte 1).

## 2. Resumo

A Api expõe `GET api/dashboards/consolidado`, no `DashboardsController` existente (sem controller novo). A ação recebe os quatro filtros e o agrupamento opcional por query string, chama a `GetConsolidadoQuery` e devolve o total e, quando há agrupamento, as linhas com o percentual.

## 3. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D12 | Ação nova no `DashboardsController` (D9 do ADR 0019 e precedente da rentabilidade); o controller passa a ter **quatro** dependências |
| D13 | Entrada por DTO `[FromQuery] ConsolidadoFiltroDto`, como `RentabilidadeFiltroDto`; sem validação no DTO (a Application valida) |
| D14 | `AgruparPor` usa o enum `DimensaoConsolidado` do Persistence, por precedente (o Service já usa `MotivoEncerramento` do Domain). O binder aceita o nome sem diferenciar maiúsculas (`corretora`, `tipoRenda`) e também o número; um número fora do enum cai no `[EnumDataType]` da Application (422) |
| D15 | Resposta: objeto `{ total, grupos }`, não array, porque o total é parte do contrato |
| D16 | `Id` e `Nome` são nulos na linha de total; `grupos` é `[]` sem agrupamento (nunca nulo) |
| D17 | Percentual como **fração** em `decimal` (0,325 = 32,5%), como as demais razões da Api |
| D18 | Erros: GUID malformado ou `agruparPor` ilegível → 422 pelo `[RequestValidation]`; sem filtro e sem agrupamento → 422 pela Application; ID desconhecido → 200 com zeros |
| D19 | `UserId` só do token (`GetCurrentUserId()`); `CancellationToken` obrigatório na ação (ADR 0017) |

## 4. Atenção antes de começar

1. **`GET` com `[FromQuery]` e `[RequestValidation]`:** o filtro da lib já cobre `ModelState` inválido em `GET`. Não escrever checagem de `ModelState` no controller.
2. **Construtor do controller muda.** Todos os testes que instanciam `DashboardsController` (`DashboardsControllerTests`) precisam passar o novo mock; usar um método auxiliar de criação para não repetir.
3. **Sem lógica no controller nem nos DTOs:** só mapeamento. Percentual, total e ordem vêm da Application.
4. **`decimal` e `JSON`:** valores monetários e percentual saem como número JSON `decimal`; não converter para `double`.
5. **Registro por convenção:** a query nova é descoberta pelo `AddServicesFromAssembly`. Acrescentar a interface ao `RegistroDeServicosTests`.
6. **O teste de arquitetura do ADR 0017** descobre a ação sozinho e exige o `CancellationToken` obrigatório.
7. **Sem dependência nova** e `Program.cs` inalterado.

## 5. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 3.1 | DTOs | `ConsolidadoFiltroDto`, `ConsolidadoLinhaDto`, `ConsolidadoDto` e testes |
| 3.2 | Ação no controller | ação e testes |
| 3.3 | Registro e contrato JSON | testes |

---

## Passo 3: Service (Api)

### 3.1 DTOs (`Service/Dashboards/DTOs/ConsolidadoDtos.cs`, namespace `Service.Dashboards.DTOs`)

Todos em um arquivo, no estilo de `RentabilidadeDtos.cs` e `FluxoMensalDtos.cs`.

- **`ConsolidadoFiltroDto`** (`record`, propriedades `init`, todas opcionais): `Guid? TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId` e `DimensaoConsolidado? AgruparPor`.
  - `ToParameters()` devolve o `ConsolidadoParameters` da parte 1, copiando os cinco valores.
  - Nenhum valor padrão diferente de nulo e nenhuma validação.
- **`ConsolidadoLinhaDto`** (`record` posicional): `Guid? Id`, `string? Nome`, `int Quantidade`, `decimal TotalAportado`, `decimal TotalLiquido` e `decimal PercentualLiquido`.
  - `FromApplication(ConsolidadoLinhaModel)` e a versão para lista, preservando a ordem.
- **`ConsolidadoDto`** (`record` posicional): `ConsolidadoLinhaDto Total` e `List<ConsolidadoLinhaDto> Grupos`.
  - `FromApplication(ConsolidadoModel)`; `Grupos` nunca nulo.
- O DTO **não recalcula nada** (nem total, nem percentual).

### 3.2 `DashboardsController`

- Atributos de classe e rota base **inalterados**.
- Construtor: acrescentar `IGetConsolidadoQuery` às dependências existentes.
- Ação nova `GET consolidado`: `Task<ActionResult<ConsolidadoDto>> GetConsolidado([FromQuery] ConsolidadoFiltroDto filtro, CancellationToken cancellationToken)`.
  - `filtro ??= new ConsolidadoFiltroDto()`, como em `GetRentabilidade`.
  - Chama a query com `GetCurrentUserId()`, `filtro.ToParameters()` e o token, e devolve `Ok(ConsolidadoDto.FromApplication(...))`.
  - `ProducesResponseType`: 200 (`ConsolidadoDto`), 422 e 500 (`ErrorDetails`).
  - Sem `try/catch`: `CommandValidationException` vira 422 e falhas inesperadas viram 500 pelos handlers.

### 3.3 `Program.cs`

Sem alteração. Confirmar o registro pelo teste de registro.

---

## 6. Testes (`Service.Test`)

Estilo dos testes atuais (Moq, `DefaultHttpContext` com `Items["UserId"]`, `CancellationTokenSource`).

### `DashboardsControllerTests` (arquivo existente)

- Ajustar a criação do controller para o quarto mock; os testes da rentabilidade, do retorno e do investido continuam verdes sem mudança de comportamento.
- `GetConsolidado_RepassaUsuarioParametrosETokenEMapeia`: `Setup` da query com o `ownerId` do contexto, `It.Is<ConsolidadoParameters>` conferindo os cinco campos e o token; resposta `OkObjectResult` com `ConsolidadoDto` (total e grupos mapeados, inclusive percentual).
- `GetConsolidado_SemFiltro_RepassaParametrosVazios`: filtro nulo ou `new ConsolidadoFiltroDto()` chega à query com tudo nulo (a validação de "ao menos um" é da Application).
- `GetConsolidado_UsaOUsuarioDoToken`: o `ownerId` é o de `Items["UserId"]`.
- `GetConsolidado_ExcecaoDaQueryNaoEhEngolida`: `CommandValidationException` da query propaga.
- `GetConsolidado_ChamaSomenteAQueryDeConsolidado`: as outras queries do controller não são chamadas (`Times.Never`).
- `PossuiFiltroDeValidacaoDeRequisicao` já existente cobre a ação nova.

### `ConsolidadoDtosTests` (novo, `Service.Test/Dashboards/`)

- `ConsolidadoFiltroDto`: padrão com tudo nulo; `ToParameters` copia os quatro IDs e o `AgruparPor`.
- `FromApplication` da linha: mapeia todos os campos, inclusive `Id` e `Nome` nulos na linha de total.
- `FromApplication` do consolidado: `Grupos` vazio vira `[]` (nunca nulo), a ordem das linhas é preservada e o DTO **não** altera total nem percentual (valores copiados idênticos).

### `ConsolidadoContratoJsonTests` (novo)

Com `JsonSerializer` e `JsonSerializerOptions(JsonSerializerDefaults.Web)`, a convenção da Api:

- Nomes: `total`, `grupos`, e em cada linha `id`, `nome`, `quantidade`, `totalAportado`, `totalLiquido` e `percentualLiquido`.
- `id` e `nome` do total saem `null`; `grupos` sem agrupamento sai `[]`.
- Valores `decimal` como número JSON (por exemplo, `0.325`), sem virar texto.
- Ida e volta (serializar e desserializar) preserva os valores.
- O conteúdo do exemplo da seção 7 desserializa sem erro (trava o contrato para o Web, que o consome na parte 3).

### `RegistroDeServicosTests`

- Acrescentar `[InlineData(typeof(IGetConsolidadoQuery))]` (e o `using`).

### `CancellationTokenTests`

- Nenhuma alteração: deve continuar verde, pois a ação recebe o token obrigatório.

---

## 7. Como validar

- `dotnet build` e `dotnet test Service.Test --filter "FullyQualifiedName~Dashboards|FullyQualifiedName~Registro|FullyQualifiedName~Arquitetura"`; depois `dotnet test` completo.
- Com a Api no ar e um token válido (`$TOKEN`), ajustando host e porta, e usando IDs reais do usuário:

```
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado?tipoRendaId=<id>"
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado?corretoraId=<id>&agruparPor=produto"
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado?agruparPor=corretora"
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado"                    # 422 (sem filtro e sem agrupamento)
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado?corretoraId=abc"       # 422
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado?agruparPor=xyz"        # 422
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado?agruparPor=7"          # 422
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "http://localhost:<porta>/api/dashboards/consolidado?corretoraId=<guid-inexistente>"   # 200, zeros
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:<porta>/api/dashboards/consolidado?agruparPor=corretora"                                   # 401
```

Exemplo de resposta esperada para `agruparPor=corretora` com dois grupos (600 e 400 de líquido):

```json
{
  "total": { "id": null, "nome": null, "quantidade": 5, "totalAportado": 900.00, "totalLiquido": 1000.00, "percentualLiquido": 1 },
  "grupos": [
    { "id": "…", "nome": "Inter", "quantidade": 3, "totalAportado": 500.00, "totalLiquido": 600.00, "percentualLiquido": 0.6 },
    { "id": "…", "nome": "XP",    "quantidade": 2, "totalAportado": 400.00, "totalLiquido": 400.00, "percentualLiquido": 0.4 }
  ]
}
```

Conferências:

- **Primeira execução real contra o MariaDB** (ponto de atenção da parte 1): conferir no log do EF que as consultas foram traduzidas sem erro. Se houver erro de tradução, voltar à parte 1 (Persistence), sem contornar no controller.
- A soma das linhas de `grupos` confere com `total`, e com um `SELECT COUNT(*), SUM(ValorAporte), SUM(ValorLiquidoAtual) FROM Movimentos WHERE UserId = ? AND EstaAtivo = 1 [AND ...]` feito à mão.
- Movimentos encerrados (`Pendente`, `Gasto`, `Reinvestido`) **não** entram; Movimentos de Objetivo inativo **entram**.
- `tipoRenda`, `corretora`, `produto` e `emissor` em minúsculas e com iniciais maiúsculas funcionam.
- Isolamento: com dois usuários, o segundo usando IDs do primeiro recebe zeros e, no agrupamento, lista vazia.
- OpenAPI: a rota e o parâmetro `agruparPor` aparecem (provavelmente como inteiro com os quatro valores; mesmo ponto de atenção da rentabilidade).

**Critério de pronto:** build e testes verdes, `[RequestValidation]` herdado, ação com `CancellationToken` obrigatório, `UserId` só do token, `Program.cs` inalterado, nenhum pacote novo, e a primeira execução contra o MariaDB sem erro de tradução.

---

## 8. Próxima parte

- **Parte 3, Web e fechamento:** cliente da Api (`IConsolidadoApi`, query montada em ordem fixa, nulos omitidos, minúsculas no `agruparPor`), DTOs próprios do Web, página `/Dashboards/Consolidado` (quatro selects com `--` reaproveitando `IMovimentoFormLookupData`, "Agrupar por", **Limpar filtro**, filtros na URL, tabela do total e das linhas com coluna **Percentual**, notas sobre `Pendente` e Objetivo inativo, orientação quando nada foi escolhido, erro tratado na própria tela), item **Consolidado** no menu **Dashboards**, roteiro ponta a ponta, documentação e ADR 0022 como **Aceita**.

## Fora de escopo

Cliente e telas do Web; multiseleção; agrupamento por mais de uma dimensão; qualquer alteração de schema, Domain ou LumiaFoundation.
