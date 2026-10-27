# Especificação técnica 0019: Encerramento de Movimento (Vault), parte 3

- Data: 2026-10-27
- Status: **em elaboração (parte 3 de 4: Passo 4, Service/Api)**
- ADR relacionado: [0020](../adr/0020-encerramento-de-movimento.md)
- Partes anteriores: [parte 1](0019-vault-encerramento-de-movimento.md) (Passos 0 a 2) e [parte 2](0019-vault-encerramento-de-movimento-parte-2.md) (Passo 3)
- Repositório afetado nesta parte: **Service (Api)**. Sem schema, sem migration, sem dependência nova, sem alteração no LumiaFoundation.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Resumo

A Api ganha os endpoints do ciclo de encerramento, `PUT` e `DELETE api/movimentos/{id}/encerramento`, expõe motivo e data no `MovimentoDto`, aceita encerramento opcional no `POST` e **muda o contrato** do `POST` e do `PUT api/movimentos/{id}`: ambos deixam de aceitar `EstaAtivo`. Esta parte também desfaz o "ajuste mínimo" do Passo 3.7 (a propriedade ignorada nos DTOs sai de vez).

## 2. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D18 | **Aprovado (opção B):** o enum trafega como **texto** (`"Gasto"`, `"Reinvestido"`, `"Pendente"`), configurado por atributo nas propriedades dos DTOs (ver seção 3) |
| D19 | **Aprovado (controller próprio):** os dois endpoints novos ficam num **controller próprio**, `EncerramentoMovimentoController` (ver seção 3) |
| D20 | `PUT api/movimentos/{id}/encerramento`: corpo `{ "motivo", "data" }`, resposta **204**. Encerra e reclassifica |
| D21 | `DELETE api/movimentos/{id}/encerramento`: sem corpo, resposta **204**. Reativa; é idempotente |
| D22 | `motivo` e `data` do corpo são **anuláveis no DTO** (correção da parte 2): ausência vira erro de validação, nunca `Gasto` em silêncio |
| D23 | `POST api/movimentos` aceita `motivoEncerramento` e `dataEncerramento` opcionais, **juntos ou nenhum**; perde `estaAtivo` |
| D24 | `PUT api/movimentos/{id}` perde `estaAtivo`; não toca no encerramento |
| D25 | `MovimentoDto` expõe `motivoEncerramento` e `dataEncerramento`; `estaAtivo` continua exposto |
| D26 | Erros no padrão atual: corpo ausente ou JSON inválido → **422** pelo `[RequestValidation]`; regra de dados → **422** (`CommandValidationException`); Movimento inexistente ou de outro usuário → **404** |
| D27 | `CancellationToken` **obrigatório** em todas as ações novas (o teste de arquitetura do ADR 0017 já cobre controllers) |

## 3. Decisões D18 e D19 (aprovadas em 2026-10-27)

### D18: serialização do enum

Hoje o `AddControllers()` não configura conversor de enum e nenhum enum trafega na Api; sem configuração, o enum sairia como **número** (`0`, `1`, `2`), ilegível e frágil à reordenação (o mesmo motivo de gravar texto no banco).

| Opção | Prós | Contras |
| --- | --- | --- |
| **B. Atributo nas propriedades dos DTOs** `[JsonConverter(typeof(JsonStringEnumConverter<MotivoEncerramento>))]` (**escolhida**) | efeito local e explícito; nenhum comportamento global muda; o Domain não ganha atributo de serialização | repetir o atributo em cada propriedade de DTO (3 lugares) |
| A. Conversor global em `AddControllers().AddJsonOptions(...)` | uma linha, vale para todo enum futuro | altera o comportamento de toda a Api de uma vez; qualquer enum futuro passa a trafegar como texto sem ninguém decidir |
| C. DTO com `string` e conversão manual | contrato totalmente independente do Domain | parse e mensagem de erro manuais; duplica o que o conversor já faz |

Efeitos a conhecer, em qualquer opção por conversor: a leitura é **sem diferença de maiúsculas**, e o conversor padrão **aceita também o número** (`"motivo": 1`). Isso é inofensivo, porque um número fora do enum cai no `EnumDataType` da Application (422). Um texto desconhecido (`"Foo"`) falha no binding e vira 422 pelo `[RequestValidation]`.

### D19: onde ficam os endpoints novos

O `MovimentosController` já recebe 8 dependências; acrescentar 2 vai a 10, e o arquivo de testes dele já é o maior do Service.

| Opção | Prós | Contras |
| --- | --- | --- |
| **Controller próprio** `EncerramentoMovimentoController`, com `[Route("api/movimentos/{id:guid}/encerramento")]` (**escolhida**) | segue o precedente de `DistribuicaoValorLiquidoController` e `AtualizacaoPorCotacaoController`; coesão; testes menores; o `MovimentosController` não cresce | a rota `api/movimentos/...` passa a ser servida por mais um controller (convivem sem conflito: `{id:guid}/encerramento` não colide com `{id:guid}`) |
| No `MovimentosController` | tudo de Movimento num só lugar | 10 dependências no construtor; o teste existente precisa mudar de assinatura |

A spec abaixo descreve a opção escolhida.

---

## 4. Atenção antes de começar

1. **Os DTOs do Service são o contrato público da Api.** O Web é o único consumidor e só é ajustado na parte 4; por isso **não publicar** o Service e o Web em versões diferentes entre as partes 3 e 4.
2. **Campo removido não dá erro.** O `System.Text.Json` ignora propriedades desconhecidas: um cliente antigo que ainda mande `estaAtivo: false` terá o campo **ignorado** e o Movimento nascerá **ativo**. Como o Web é o único cliente e será atualizado na parte 4, isso é aceito, mas é a razão do aviso do item 1.
3. **Multi-tenant:** o `UserId` vem só do token (`GetCurrentUserId()`), como nas demais ações; nenhum identificador de usuário no corpo ou na rota.
4. **Sem lógica no controller:** a ação só mapeia DTO para modelo, chama o comando e devolve 204. Regras ficam na Application.
5. **Padrão de DTO do repositório:** `record` com `init`, método `ToXxxCommand()` para o modelo da Application e `FromApplication` para o caminho de volta.
6. **Sem dependência nova:** `JsonStringEnumConverter<T>` faz parte do `System.Text.Json`.

## 5. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 4.1 | `MovimentoDto` com motivo e data | DTO e teste |
| 4.2 | DTO de encerramento e controller | DTO, controller e testes |
| 4.3 | Criação com encerramento opcional; fim do `EstaAtivo` nos DTOs de criação e edição | DTOs e testes |
| 4.4 | Registro e arquitetura | testes de registro e de arquitetura |
| 4.5 | Teste de serialização JSON (contrato) | testes |

---

## Passo 4: Service (Api)

### 4.1 `MovimentoDto`

- Acrescentar `MotivoEncerramento? MotivoEncerramento` e `DateTime? DataEncerramento`.
- A propriedade do enum leva o atributo do D18 (`JsonStringEnumConverter<MotivoEncerramento>`).
- `FromApplication` mapeia os dois campos. `EstaAtivo` continua exposto.
- Aditivo: nenhum consumidor quebra.

### 4.2 Encerramento e reativação

**DTO** `MovimentoForEncerramentoDto` (record, `Service/Movimentos/DTOs/`):

- `MotivoEncerramento? Motivo`, com o atributo do D18, e `DateTime? Data`. Ambos **anuláveis** (D22).
- `ToEncerrarMovimentoCommand()` devolve o `MovimentoModelForEncerramento` da parte 2, repassando os dois campos como estão (a validação de obrigatoriedade é do modelo da Application).

**Controller** `EncerramentoMovimentoController` (D19), mesmo cabeçalho dos demais: `[ApiController]`, `[Authorize]`, `[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]`, `[RequestValidation]` e `[Route("api/movimentos/{id:guid}/encerramento")]`; herda `BaseApiController`; recebe `IEncerrarMovimentoCommand` e `IReativarMovimentoCommand`.

- **`PUT`** `Encerrar(Guid id, [FromBody] MovimentoForEncerramentoDto dto, CancellationToken cancellationToken)`:
  - chama `EncerrarMovimentoCommand.ExecuteAsync(dto.ToEncerrarMovimentoCommand(), GetCurrentUserId(), id, cancellationToken)`;
  - responde `NoContent()`;
  - `ProducesResponseType`: 204, 404, 422 e 500 (`ErrorDetails`), como as ações atuais.
- **`DELETE`** `Reativar(Guid id, CancellationToken cancellationToken)`:
  - chama `ReativarMovimentoCommand.ExecuteAsync(GetCurrentUserId(), id, cancellationToken)`;
  - responde `NoContent()`;
  - `ProducesResponseType`: 204, 404 e 500.

Por que 204 e não devolver o Movimento: é o padrão do `PUT` de edição e do `PATCH` de valor líquido; o Web recarrega a listagem de qualquer forma.

Por que `PUT` serve para encerrar **e** reclassificar: o recurso é "o encerramento deste Movimento"; `PUT` o define por inteiro e é idempotente, e o `DELETE` o remove (reativa).

### 4.3 Criação e edição: contrato novo

`MovimentoForCreationDto`:

- **Remover** `EstaAtivo`.
- **Acrescentar** `MotivoEncerramento? MotivoEncerramento` (com o atributo do D18) e `DateTime? DataEncerramento`.
- `ToCreateMovimentoCommand` repassa os dois campos novos. Não há regra aqui: "ambos ou nenhum", futuro e anterior ao investimento são da Application (parte 2, Passo 3.5).

`MovimentoForUpdateDto`:

- **Remover** `EstaAtivo` e o repasse correspondente. Nenhum campo de encerramento no `PUT` de edição.

Isso desfaz o ajuste mínimo do Passo 3.7 da parte 2: a propriedade ignorada sai dos dois DTOs.

### 4.4 Registro e arquitetura

- `RegistroDeServicosTests`: acrescentar `IEncerrarMovimentoCommand` e `IReativarMovimentoCommand` à lista de interfaces resolvidas (o registro em si é por convenção).
- `CancellationTokenTests`: nenhum ajuste esperado (ele varre as ações dos controllers e as interfaces). **Confirmar** que a ação `Encerrar` e a `Reativar` aparecem na varredura e passam.

---

## 6. Testes (`Service.Test`)

Estilo dos testes atuais: Moq para os comandos, `DefaultHttpContext` com `Items["UserId"]`, `CancellationTokenSource` para provar a propagação do token.

### `EncerramentoMovimentoControllerTests` (novo)

- `Encerrar` repassa motivo, data, `userId` do contexto, `id` da rota e o **mesmo** token ao comando, e devolve `NoContentResult`.
- `Encerrar` com `Motivo` e `Data` nulos no DTO: o modelo repassado ao comando traz os dois **nulos** (o controller não decide valor padrão).
- `Reativar` repassa `userId`, `id` e token, e devolve `NoContentResult`.
- Se o comando lançar (`EntityNotFoundException`, `CommandValidationException`), a exceção **não é engolida** no controller (propaga para o `DomainExceptionHandler`).

### `MovimentoForEncerramentoDtoTests` (novo)

- `ToEncerrarMovimentoCommand` mapeia `Motivo` e `Data`.
- Nulos continuam nulos.

### `MovimentoDtoTests` (acrescentar)

- `FromApplication` mapeia `MotivoEncerramento` e `DataEncerramento` de um encerrado, e nulos de um ativo; `EstaAtivo` continua mapeado.

### `MovimentoForCreationAndUpdateDtoTests` (ajustar e acrescentar)

- Criação: mapeia `MotivoEncerramento` e `DataEncerramento`; sem eles, o modelo fica com nulos.
- Os testes que atribuíam `EstaAtivo` nos DTOs de criação e edição **somem** (a propriedade não existe mais).
- Reflexão: `MovimentoForCreationDto` e `MovimentoForUpdateDto` **não** têm propriedade `EstaAtivo` (guarda contra a volta do campo por engano).

### `MovimentosControllerTests`

- Nenhuma alteração de construtor (a opção D19 escolhida não mexe no controller). Ajustar apenas o que usava `EstaAtivo` nos DTOs.

### Contrato JSON (novo, `MovimentoContratoJsonTests`)

Usa `JsonSerializer` com `JsonSerializerOptions(JsonSerializerDefaults.Web)` (o mesmo padrão de camelCase da Api). Como o Service não sobe servidor nos testes, este teste cobre o contrato sem HTTP:

- `MovimentoDto` serializa o motivo como **texto** (`"motivoEncerramento":"Gasto"`) e `null` quando ativo.
- `MovimentoForEncerramentoDto` desserializa `{"motivo":"Pendente","data":"2026-10-20"}`.
- Texto em outra caixa (`"gasto"`) é aceito.
- Texto desconhecido (`"Foo"`) lança `JsonException`.
- Corpo **sem** `motivo`: o DTO fica com `Motivo = null` (e **não** `Gasto`). É o teste que protege o D22.
- Corpo antigo de criação com `"estaAtivo": false`: desserializa sem erro e o campo é descartado (documenta o aviso do item 2 da seção 4).

---

## 7. Como validar

- `dotnet build` da solution e `dotnet test Service.Test`; depois `dotnet test` da solution inteira.
- Subir a Api local (com o banco já migrado, parte 1) e, com um token válido, exercitar com `curl` ou o arquivo `.http` do projeto, se houver:
  - `PUT /api/movimentos/{id}/encerramento` com `{ "motivo": "Gasto", "data": "<hoje>" }`: **204**; `GET /api/movimentos/{id}` mostra `estaAtivo: false`, `motivoEncerramento: "Gasto"` e a data.
  - O mesmo `PUT` com `"motivo": "Pendente"`: **204** (reclassificou).
  - `PUT` com data de amanhã: **422**. Com data anterior ao investimento: **422**. Sem `motivo`: **422**. Com `"motivo": "Foo"`: **422**.
  - `PUT` e `DELETE` com o `id` de um Movimento de **outro usuário**: **404**.
  - `DELETE /api/movimentos/{id}/encerramento`: **204**; o `GET` mostra `estaAtivo: true` e os dois campos `null`. Repetir o `DELETE`: **204**.
  - `POST /api/movimentos` com motivo e data: nasce encerrado. Só com o motivo: **422**.
  - `PUT /api/movimentos/{id}` de um encerrado, sem campos de encerramento: continua encerrado.
- Abrir o documento OpenAPI da Api e conferir que o enum aparece como texto, com os 3 valores. Se aparecer como número, o atributo do D18 não foi aplicado em alguma propriedade.
- Ponto de atenção: com a parte 3 pronta e a 4 pendente, o Web ainda envia `estaAtivo`: não publicar essa combinação.

**Critério de pronto:** build e testes verdes; endpoints respondendo como na lista acima; enum como texto no JSON e no OpenAPI; nenhuma ação sem `CancellationToken`; `EstaAtivo` removido dos DTOs de criação e edição.

---

## 8. Próxima parte

- **Parte 4, Presentation (Web) e fechamento** ([parte 4](0019-vault-encerramento-de-movimento-parte-4.md)): contratos do Web para os campos novos (com o mesmo conversor de enum, no padrão do cliente da Api), métodos no `IMovimentoApi`/`MovimentoApi` para encerrar e reativar, remoção do checkbox "Ativo" da edição, página `/Movimentos/Encerrar/{id}` (motivo, data com padrão hoje, reclassificar e "Reativar"), motivo e data no detalhe da listagem, validação ponta a ponta, documentação final e ADR 0020 para **Aceita**.

## Fora de escopo

- Filtro por motivo na listagem (`MovimentoFiltroDto`): o filtro "Status" atual continua como está.
- Uso do motivo em consultas, dashboards e na projeção do Objetivo (ADR 0021).
- Versionamento da Api: o contrato muda no próprio `v1`, com o Web como único consumidor, e esse custo está registrado nas Consequências do ADR 0020.
- Endpoint de encerramento em lote (fundir vários encerrados num novo continua manual, com a observação no campo próprio).
