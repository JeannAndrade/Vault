# Especificação técnica 0019: Encerramento de Movimento (Vault), parte 2

- Data: 2026-10-27
- Status: **em elaboração (parte 2 de 4: Passo 3, Application)**
- ADR relacionado: [0020](../adr/0020-encerramento-de-movimento.md)
- Parte anterior: [0019-vault-encerramento-de-movimento.md](0019-vault-encerramento-de-movimento.md) (Passos 0 a 2)
- Repositório afetado nesta parte: **Application** (e um ajuste mínimo de compilação no Service). Sem schema, sem migration, sem dependência nova.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Confirmações da parte 1

Decididas em 2026-10-27, e que passam a valer para todas as partes:

| Item | Decisão |
| --- | --- |
| D3 | **Opção A:** `EstaAtivo` mantém o `set` público; o código de produção deixa de escrevê-lo e usa só `Encerrar` e `Reativar` |
| D4 | **`CHECK` incluído** (`CK_Movimentos_Encerramento`), na mesma migration, depois do `UPDATE` |

Ajuste na parte 1: a tabela da seção 2 e o Passo 2.4 passam de "a confirmar" para "aprovado"; a frase sobre o `CHECK` no Passo 0 entra no ADR.

## 2. Resumo

A Application ganha os dois comandos do ciclo de encerramento (**encerrar/reclassificar** e **reativar**), expõe motivo e data no `MovimentoModel`, aceita encerramento opcional na criação e **deixa de gravar `EstaAtivo`** na edição. As regras de data (não futura, não anterior ao investimento) são validadas aqui, com mensagem amigável, **antes** de chamar o Domain, que as repete como proteção.

## 3. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D5 (parte 1) | Regras da data aplicadas na Application com `CommandValidationException` e, no Domain, com exceção de programação |
| D11 | Dois comandos novos: `EncerrarMovimentoCommand` (encerra **e** reclassifica) e `ReativarMovimentoCommand`; ambos retornam sem corpo (o Service responde 204) |
| D12 | As regras de data ficam num helper estático e puro, `EncerramentoValidador`, usado pelo comando de encerramento **e** pelo de criação, para a mensagem e a regra não divergirem |
| D13 | `MovimentoModelForCreation` perde `EstaAtivo` e ganha `MotivoEncerramento?` e `DataEncerramento?`: ou os dois, ou nenhum. Sem motivo, o Movimento nasce **ativo** |
| D14 | `MovimentoModelForUpdate` perde `EstaAtivo`; a edição **não toca** no encerramento |
| D15 | **Edição de `DataInvestimento` em Movimento encerrado:** se a nova data for posterior à `DataEncerramento`, a edição é rejeitada, pois violaria a regra "encerramento não anterior ao investimento". Consequência direta do ADR 0020, sem decisão nova de produto |
| D16 | `ValorLiquidoAtual` continua editável pelo `PUT` completo num encerrado (correção manual); o encerramento em si nunca o altera |
| D17 | `CancellationToken` **obrigatório** e último parâmetro nas assinaturas novas (ADR 0017) |

## 4. Atenção antes de começar

1. **Ordem de uso do relógio:** "hoje" vem de `IDateTimeProvider` (UTC), lido **uma vez** por execução e passado ao Domain. Nenhum `DateTime.UtcNow` direto.
2. **Multi-tenant:** o `UserId` do token entra sempre em `GetAsync(ownerId, ...)`. Movimento de outro usuário vira `EntityNotFoundException` ("Movimento não encontrado"), igual aos comandos atuais, sem distinguir "não existe" de "não é seu".
3. **Não engolir exceções:** a Application **não** captura `InvalidOperationException` do Domain para traduzir. Ela valida antes; se o Domain ainda lançar, é bug e deve estourar.
4. **Validação em duas camadas, com testes nas duas:** se alguém alterar a regra do Domain sem o `EncerramentoValidador` (ou o contrário), os testes de ambos precisam acusar.
5. **Compilação do Service:** remover `EstaAtivo` dos modelos quebra os DTOs do Service que o repassam. Este passo inclui apenas o **ajuste mínimo** (parar de repassar `EstaAtivo` em `MovimentoForCreationDto` e `MovimentoForUpdateDto`, e remover o que os testes do Service atribuíam). A propriedade continua nos DTOs, **ignorada**, até a parte 3 mudar o contrato. **Não fazer merge entre as partes 2 e 4:** nesse intervalo o Web ainda envia `EstaAtivo` e o comportamento fica incoerente.
6. **Registro de DI:** é por convenção (`AddServicesFromAssembly` no `Program.cs`), então os dois comandos novos são descobertos sozinhos. O `RegistroDeServicosTests` (Service.Test) tem uma lista de interfaces a resolver: acrescentar `IEncerrarMovimentoCommand` e `IReativarMovimentoCommand` (a parte 3 cobre isso).
7. **Sem log nos comandos:** os comandos atuais não logam; o tratamento fica no pipeline da Api. Manter o padrão.

## 5. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 3.1 | `MovimentoModel` expõe motivo e data | modelo e teste |
| 3.2 | `EncerramentoValidador` | helper e testes |
| 3.3 | `EncerrarMovimentoCommand` e modelo | comando e testes |
| 3.4 | `ReativarMovimentoCommand` | comando e testes |
| 3.5 | Criação com encerramento opcional | modelo, comando e testes |
| 3.6 | Edição sem `EstaAtivo` e com a regra D15 | modelo, comando e testes |
| 3.7 | Ajuste mínimo de compilação no Service | DTOs e testes do Service |

---

## Passo 3: Application

### 3.1 `MovimentoModel`

- Acrescentar `MotivoEncerramento? MotivoEncerramento` e `DateTime? DataEncerramento`.
- `FromDomain` preenche os dois. `EstaAtivo` continua exposto como hoje.
- Alteração **aditiva**: nenhum consumidor quebra.

### 3.2 `EncerramentoValidador`

Pasta `Application/Movimentos/Validation/`. Classe **estática** e pura (sem estado, sem relógio, sem dependências).

- Método `Validar(DateTime dataInvestimento, DateTime dataEncerramento, DateTime hoje)`.
- Regras, comparando sempre `.Date`:
  1. `dataEncerramento` posterior a `hoje`: `CommandValidationException("A data de encerramento não pode ser futura.")`.
  2. `dataEncerramento` anterior a `dataInvestimento`: `CommandValidationException("A data de encerramento não pode ser anterior à data do investimento.")`.
- As mensagens são **as mesmas** do Domain (parte 1, Passo 1.2), mas o tipo de exceção é outro: aqui é erro de validação do usuário (vira 400 na Api); no Domain é erro de programação.
- A verificação do motivo (valor válido do enum) **não** entra aqui: fica na anotação do modelo (3.3).

Por que um helper estático e não um serviço injetado: a regra é pura, não depende de repositório nem de relógio, e é usada por dois comandos. Um serviço com interface só acrescentaria um mock sem ganho.

### 3.3 `EncerrarMovimentoCommand`

Pasta `Application/Movimentos/Commands/EncerrarMovimento/`, no padrão dos demais comandos (`IEncerrarMovimentoCommand`, `EncerrarMovimentoCommand`, `MovimentoModelForEncerramento`).

**Modelo** `MovimentoModelForEncerramento` (record):

- `MotivoEncerramento? Motivo`: **anulável com `[Required]`** e dentro do enum (`EnumDataType`). Mensagens em português ("Motivo do encerramento é obrigatório" / "Motivo do encerramento inválido").
- `DateTime? Data`: **anulável com `[Required]`** ("Data do encerramento é obrigatória").
- Por que anuláveis: o primeiro valor do enum (`Gasto`) é o `default`, e `DateTime` não anulável vira `0001-01-01`. Com tipo não anulável, um corpo sem o campo `motivo` seria aceito **silenciosamente como `Gasto`**, e `[Required]` nunca dispararia. Sendo anulável, a ausência vira erro de validação.
- Depois da validação, o comando usa `Motivo!.Value` e `Data!.Value.Date`.

**Comando** `ExecuteAsync(MovimentoModelForEncerramento model, Guid ownerId, Guid movimentoId, CancellationToken cancellationToken)`, sem retorno de dados:

1. `CommandValidator.Validate(model)`.
2. `GetAsync(ownerId, movimentoId, trackChanges: true, cancellationToken)`; nulo: `EntityNotFoundException("Movimento não encontrado")`.
3. `hoje = IDateTimeProvider.GetDateTime().Date`.
4. `EncerramentoValidador.Validar(movimento.DataInvestimento, model.Data, hoje)`.
5. `movimento.Encerrar(model.Motivo, model.Data, hoje)`.
6. `Update(movimento)` e `SaveAsync(cancellationToken)`.

Comportamentos a garantir:

- Serve para **encerrar** um ativo e para **reclassificar** um já encerrado (troca motivo e data). Não há erro "já está encerrado".
- Nada é gravado se qualquer validação falhar.
- `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` **não** mudam (D16).

### 3.4 `ReativarMovimentoCommand`

Pasta `Application/Movimentos/Commands/ReativarMovimento/` (`IReativarMovimentoCommand`, `ReativarMovimentoCommand`).

`ExecuteAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken)`:

1. `GetAsync(..., trackChanges: true, ...)`; nulo: `EntityNotFoundException("Movimento não encontrado")`.
2. `movimento.Reativar()`.
3. `Update` e `SaveAsync`.

Sobre o resultado prático: o `ValorLiquidoAtual` volta como estava no encerramento, com a `DataAtualizacaoValorLiquido` antiga. O Movimento reativado, portanto, aparece naturalmente na lista de valores desatualizados (ADR 0011), o que é o comportamento desejado: o usuário atualiza o valor assim que reativa.

Reativar um Movimento que já está ativo **não é erro** (idempotente, como no Domain).

### 3.5 Criação com encerramento opcional

`MovimentoModelForCreation`:

- **Remover** `EstaAtivo`.
- **Acrescentar** `MotivoEncerramento? MotivoEncerramento` (com `EnumDataType`) e `DateTime? DataEncerramento`.
- Em `Validate`: motivo e data são **ambos ou nenhum**. Um sem o outro gera `ValidationResult` apontando o campo que falta ("Informe a data do encerramento" / "Informe o motivo do encerramento").
- `ToDomain`: o Movimento é criado sempre com `EstaAtivo = true`. O encerramento **não** é aplicado dentro do `ToDomain` (que não tem relógio); é aplicado pelo comando.

`CreateMovimentoCommand`, depois de validar o modelo e as referências e de montar o domínio:

1. Se o modelo trouxer motivo e data: `EncerramentoValidador.Validar(movimento.DataInvestimento, data, hoje)` e `movimento.Encerrar(motivo, data, hoje)`, **antes** de `Create`. A `hoje` é a mesma já lida para `DataAtualizacaoValorLiquido`: ler o relógio **uma vez**.
2. Sem motivo, nada muda: o Movimento nasce ativo.
3. `Create` e `SaveAsync` como hoje.

Efeito de contrato: quem enviava `EstaAtivo = false` na criação passa a enviar motivo e data. Esse efeito chega ao Service e ao Web nas partes 3 e 4.

### 3.6 Edição

`MovimentoModelForUpdate`:

- **Remover** `EstaAtivo` do modelo e de `UpdateDomain`. Nenhuma linha da edição escreve `EstaAtivo`, motivo ou data.

`UpdateMovimentoCommand`, depois de carregar o Movimento e antes de aplicar o modelo, acrescenta a regra **D15**:

- Se `movimento.DataEncerramento` não for nula e `model.DataInvestimento.Date` for **posterior** a ela: `CommandValidationException("A data do investimento não pode ser posterior à data de encerramento. Reclassifique o encerramento antes.")`.
- Igual à data de encerramento é aceito (fronteira).
- Movimento ativo não é afetado.
- Nada é gravado quando a regra falha.

### 3.7 Ajuste mínimo de compilação no Service

- `MovimentoForCreationDto` e `MovimentoForUpdateDto`: parar de repassar `EstaAtivo` ao modelo. A propriedade continua nos DTOs, **ignorada**, até a parte 3.
- Ajustar os testes do Service (`MovimentoForCreationAndUpdateDtoTests`, `MovimentosControllerTests`) que verificavam o repasse.
- **Nenhuma** rota nova e nenhum DTO novo aqui: tudo isso é da parte 3.

---

## 6. Testes (`Application.Test`)

Todos no estilo dos testes atuais (Moq para repositório e relógio, `CancellationTokenSource` para provar a propagação do token).

### `EncerramentoValidadorTests`

- Data igual a `hoje`: passa. Um dia depois: `CommandValidationException`.
- Data igual à `dataInvestimento`: passa. Um dia antes: `CommandValidationException`.
- Horas são ignoradas (`23:59:59` no mesmo dia passa).
- As mensagens batem com as do Domain (comparar com as constantes ou textos esperados, para o teste acusar divergência).

### `EncerrarMovimentoCommandTests`

- Encerra um ativo com cada motivo (`Theory`): `EstaAtivo = false`, motivo e `DataEncerramento` gravados; `SaveAsync` chamado **uma vez** com o token recebido; `GetAsync` chamado com `trackChanges: true`, o `ownerId` e o token.
- Reclassifica (`Pendente` → `Gasto`): troca motivo e data, continua inativo.
- Data com hora: gravada sem hora.
- Movimento inexistente ou de outro usuário (repositório devolve nulo): `EntityNotFoundException`, sem `SaveAsync`.
- Motivo fora do enum, **motivo ausente** (nulo) e **data ausente** (nula): `CommandValidationException`, **sem consultar** o repositório.
- Data futura e data anterior ao investimento: `CommandValidationException`, sem `SaveAsync`, estado do Movimento intacto.
- `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` inalterados.
- O relógio é lido e usado (data "hoje" vinda do `IDateTimeProvider`, não de `DateTime.UtcNow`).
- Nenhuma `InvalidOperationException` do Domain escapa nos casos de data inválida (prova de que a validação da Application vem antes).

### `ReativarMovimentoCommandTests`

- Reativa um encerrado: `EstaAtivo = true`, motivo e data nulos, `SaveAsync` com o token.
- Reativa um ativo: sem erro e sem alteração.
- Inexistente ou de outro usuário: `EntityNotFoundException`, sem `SaveAsync`.
- `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` intactos.

### `CreateMovimentoCommandTests` (acrescentar e ajustar)

- Ajustar os testes que atribuíam `EstaAtivo` no modelo: o Movimento criado sem motivo é **ativo**, com motivo e data nulos.
- Com motivo e data válidos: nasce encerrado, com os dois preenchidos e `EstaAtivo = false`.
- Só motivo, ou só data: `CommandValidationException`, sem `Create`.
- Data futura e data anterior ao `DataInvestimento`: `CommandValidationException`, sem `Create`.
- O relógio é lido **uma vez** e a mesma "hoje" serve à data de atualização do valor líquido e ao encerramento.

### `UpdateMovimentoCommandTests` (acrescentar e ajustar)

- Ajustar os testes que atribuíam `EstaAtivo` no modelo.
- Editar um Movimento **encerrado** mantém `EstaAtivo = false`, motivo e data.
- Editar um **ativo** mantém `EstaAtivo = true`.
- D15: `DataInvestimento` posterior à `DataEncerramento`: `CommandValidationException`, sem `SaveAsync`; igual: aceito; Movimento ativo com a mesma data: aceito.

### `MovimentoModelTests` (novo ou existente)

- `FromDomain` expõe `MotivoEncerramento` e `DataEncerramento` de um encerrado, e nulos de um ativo.

### Reflexão do ADR 0017

- O teste de reflexão que exige `CancellationToken` já cobre as interfaces novas. **Confirmar** que ele varre `Application` e que os dois comandos novos foram pegos (se ele usar lista fixa, acrescentá-los).

---

## 7. Como validar

- `dotnet build` da solution, e `dotnet test Application.Test` com `--filter "FullyQualifiedName~Movimentos"`.
- `dotnet test Service.Test`, para o ajuste do 3.7.
- Conferir por busca que **nenhum código de produção** (fora do Domain e do mapeamento) escreve `EstaAtivo` de Movimento: `grep -rn "\.EstaAtivo *=" --include=*.cs Application Service Presentation` deve acusar apenas Objetivo (que tem o próprio `EstaAtivo`) e atribuições em testes.
- Ponto de atenção: o intervalo entre as partes 2 e 4 deixa o Web enviando um `EstaAtivo` que a Api ignora; não publicar essa combinação.

**Critério de pronto:** build e testes verdes; nenhum comando novo sem `CancellationToken`; validação da Application coberta nas duas camadas; Service compilando com o ajuste mínimo.

---

## 8. Próximas partes

- **Parte 3, Service:** `PUT api/movimentos/{id}/encerramento` (corpo `{ motivo, data }`, 204) e `DELETE api/movimentos/{id}/encerramento` (204) no `MovimentosController`; `MovimentoForCreationDto` com motivo e data opcionais e **sem** `EstaAtivo`; `MovimentoForUpdateDto` sem `EstaAtivo`; DTO de encerramento e serialização do enum como texto; `MovimentoDto` com os dois campos novos; `[RequestValidation]`; testes de controller e de DTO; testes de contrato com o 404 do outro usuário.
- **Parte 4, Presentation e fechamento:** cliente da Api, remoção do checkbox "Ativo" da edição, página `/Movimentos/Encerrar/{id}`, motivo e data no detalhe da listagem, validação ponta a ponta, documentação final e ADR 0020 para **Aceita**.

## Fora de escopo

- Uso do motivo em consultas, dashboards e na projeção do Objetivo (ADR 0021). O `ResumoObjetivoModel` e as demais consultas que filtram `EstaAtivo` **não mudam** nesta parte.
- Qualquer endpoint, DTO ou página nova.
- Log de auditoria de encerramentos.
