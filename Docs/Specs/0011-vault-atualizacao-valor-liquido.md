# Especificação técnica: Atualização do Valor Líquido (Vault)

- Data: 2026-10-11
- Status: aprovada para implementação
- ADR relacionado: 0011 (texto no Passo 0)
- Repositórios afetados: **LumiaFoundation** (`EFRepository`) e **Vault** (Domain, Persistence, Application, Service, Presentation/Web)

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.

---

## 1. Resumo

Os Movimentos ativos cujo valor líquido não é atualizado há **mais de 30 dias** devem aparecer numa nova tela, **Manutenção Valor Líquido**. Cada linha tem um campo e o botão **Atualizar**, que altera somente `ValorLiquidoAtual`. Para isso, `Movimentos` ganha a coluna `DataAtualizacaoValorLiquido`. Movimentos novos nascem com essa data igual ao dia do cadastro.

## 2. Decisões aprovadas

| Item | Decisão |
|---|---|
| D1 | Coluna `DataAtualizacaoValorLiquido`, `datetime(6) NOT NULL`, UTC, guardando só a data (`.Date`) |
| D2 | Movimentos existentes recebem `DataInvestimento` na migration |
| D3 | `IDateTimeProvider` (`UtcDateTimeProvider`) registrado também na Api |
| D4 | Só `EstaAtivo = true` entra na listagem; objetivo inativo não filtra |
| D5 | Sem paginação, **top 12** (constante no código), do mais antigo para o mais novo |
| D6 | `PATCH api/movimentos/{id}/valor-liquido`, resposta 204 |
| D7 | O `PUT` completo do Movimento **não** renova a data |
| D8 | Campo inline em cada linha da tabela, com o botão Atualizar |
| D9 | Mesmas colunas de Próximos Vencimentos, sem coluna extra e sem regras de cor |
| D10 | Índice `(UserId, EstaAtivo, DataAtualizacaoValorLiquido)` |
| D11 | Command e query aceitam `CancellationToken`; a lib ganha `SaveAsync(CancellationToken)` |
| Extra 1 | Token **obrigatório** (sem `= default`) nas novas assinaturas internas (repositório, query, command) |
| Extra 2 | Escopo da D11: só `SaveAsync(CancellationToken)` na lib e a sobrecarga de `GetAsync` de Movimento. O restante da propagação segue na discussão geral de CancellationToken |
| Extra 3 | `MovimentoModel` e `MovimentoDto` **não** expõem a data por ora |
| Extra 4 | Menu: dropdown **"Manutenção"** com o item **"Manutenção Valor Líquido"** |

Regra de negócio: entra na lista o Movimento com `EstaAtivo = true` e `DataAtualizacaoValorLiquido < (hoje UTC - 30 dias)`. Exatamente 30 dias **não** entra; 31 dias entra.

## 3. Verificações já feitas na lib (não repetir)

- `ApiConnection.SendAsync` recebe um `HttpMethod` qualquer e não valida o método. `HttpMethod.Patch` funciona **sem alterar** `Http.Client`.
- `BaseRepositoryManager.SaveAsync()` não recebe token e chama `SaveChangesAsync()` sem ele. **Precisa alterar** `EFRepository`.

## 4. Atenção antes de começar

1. **Sobrecarga, nunca parâmetro opcional**, ao acrescentar token a métodos já usados em testes. `Verify`/`Setup` do Moq com expression tree não aceitam argumento opcional (erro CS0854), e a mudança seria quebra binária. Por isso os Passos 1 e 3 usam **sobrecargas** e mantêm as assinaturas atuais.
2. **Publicar a lib antes** de o Vault consumi-la (`EFRepository` 0.12.0), como no ADR 0008.
3. **Discrepância no ADR 0010:** ele fala em "15 itens", mas `GetProximosVencimentosQuery` usa `QuantidadeMaxima = 12`. Corrigir no Passo 8.
4. **A migration é gerada pelo mantenedor** (Jeann). O dev prepara o código e indica o comando; a revisão do arquivo gerado é feita junto.

## 5. Ordem de implementação

Cada passo termina com build e testes verdes antes do seguinte.

| # | Passo | Projeto |
|---|---|---|
| 0 | ADR 0011 como "Proposta" | Vault (docs) |
| 1 | `SaveAsync(CancellationToken)` | LumiaFoundation |
| 2 | Domain | Vault |
| 3 | Persistence | Vault |
| 4 | Migration | Vault |
| 5 | Application | Vault |
| 6 | Service (Api) | Vault |
| 7 | Presentation (Web) | Vault |
| 8 | Documentação final e fechamento do ADR | Vault + lib |

---

## Passo 0: ADR 0011 como "Proposta"

Criar `Docs/adr/0011-atualizacao-do-valor-liquido.md` no formato dos ADRs existentes, com o texto abaixo e **Status: Proposta (2026-10-10)**. Mudar para "Aceita" no Passo 8.

```markdown
# ADR 0011: Atualização do Valor Líquido

- Status: Proposta (2026-10-10)

## Contexto

O Valor Líquido Atual de cada Movimento é informado manualmente. Sem a data da última atualização, não há como saber quais investimentos estão com o valor defasado. A regra de negócio é: um Movimento ativo precisa de atualização quando passaram mais de 30 dias desde a última atualização do valor líquido.

## Decisão

- Nova coluna `DataAtualizacaoValorLiquido` em `Movimentos` (`datetime(6)`, obrigatória), guardando somente a data, em UTC. Os Movimentos existentes recebem a `DataInvestimento` na migration; os novos recebem a data do cadastro (e não a `DataInvestimento`, que pode ser retroativa).
- Regra: entra na lista o Movimento com `EstaAtivo = true` e `DataAtualizacaoValorLiquido < hoje - 30 dias` (UTC). Objetivo inativo não filtra, como no ADR 0010.
- `GET api/movimentos/desatualizados`: sem paginação, com 12 itens fixos (constante no código), ordenados por `DataAtualizacaoValorLiquido` e `Id` ascendentes (os mais antigos primeiro). Modelo e DTOs dedicados, com os mesmos campos dos próximos vencimentos (ADR 0010).
- `PATCH api/movimentos/{id}/valor-liquido`, corpo apenas com `ValorLiquidoAtual` (maior ou igual a zero), resposta 204. Altera somente `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido`. O `PUT` completo não renova a data.
- `IDateTimeProvider` (`UtcDateTimeProvider`) passa a ser registrado também na Api. O repositório recebe a data limite já calculada e continua sem relógio.
- Índice `IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido` em `(UserId, EstaAtivo, DataAtualizacaoValorLiquido)`, acoplado à ordenação ascendente do repositório (mesma lógica dos ADRs 0009 e 0010). A migration é gerada pelo mantenedor.
- Web: nova tela em `/Movimentos/Desatualizados`, com campo de valor inline em cada linha e o botão Atualizar. No menu, em "Manutenção" > "Manutenção Valor Líquido".
- `CancellationToken`: as novas assinaturas (repositório, query e command) o exigem, sem valor padrão. A lib `EFRepository` 0.12.0 ganha a sobrecarga `SaveAsync(CancellationToken)`, e `IMovimentoRepository` ganha uma sobrecarga de `GetAsync` com token. As demais rotas seguem na discussão geral de propagação do token.

## Alternativas consideradas

- Filtro em `GET api/movimentos`: conflita com a paginação e a ordenação do ADR 0008.
- Relógio só no Web, enviando a data limite à Api: coloca uma regra de negócio no cliente.
- `PUT` completo renovando a data quando o valor muda: mantém a data coerente, mas faz a Edição geral alterar um dado de controle. Descartada por ora.
- Página dedicada ou modal para informar o valor: mais passos por atualização; o campo inline é mais rápido para quem atualiza vários.
- Paginação da lista: a tela é uma fila de trabalho (ao atualizar, o item sai da lista), então basta limitar a quantidade.
- Parâmetro opcional em `SaveAsync`: quebraria a compilação dos testes existentes (`Verify` com expression tree não aceita argumento opcional) e seria uma quebra binária na lib.

## Consequências

- O relógio passa a existir também na Api, o que supera parcialmente o ADR 0010 (que o restringia ao Web).
- Como o relógio é UTC, entre 21h e 24h (BRT) "hoje" já é o dia seguinte; o efeito é de um dia na fronteira dos 30 dias.
- Sem total na tela, o usuário vê só os 12 mais antigos; ao atualizar, os seguintes aparecem.
- O valor pode ser editado pela tela de Edição sem renovar a data, e o Movimento continuará aparecendo como desatualizado. Reavaliar se isso incomodar.
- A tabela passa a ter 13 índices secundários; pequeno custo extra em escrita.
- Mudar a direção ou o critério da ordenação exige reavaliar o índice. Um teste de metadados do modelo garante que ele existe. O uso deve ser confirmado com `EXPLAIN` quando houver volume.
- Exige a nova versão `EFRepository` 0.12.0, publicada antes de o Vault consumi-la. Quem implementa `IBaseRepositoryManager` manualmente (no Vault, só o fake de testes) precisa da sobrecarga nova.
- A carga inicial de Movimentos (seed) precisa preencher `DataAtualizacaoValorLiquido`.
- A migration herda o ruído de `IdentityRole` (`HasData` sem `Id` fixo) das anteriores.
```

**Critério de pronto:** arquivo criado, no padrão dos ADRs anteriores, status "Proposta".

---

## Passo 1: Lib `LumiaFoundation.EFRepository` (0.11.0 → 0.12.0)

Repositório: `JeannAndrade/Lumia.Foundation`.

**O que fazer**

- `IBaseRepositoryManager`: acrescentar `SaveAsync(CancellationToken cancellationToken)`. **Manter** o `SaveAsync()` atual.
- `BaseRepositoryManager`: implementar a nova sobrecarga repassando o token a `SaveChangesAsync(cancellationToken)`. O método sem parâmetro continua como está.
- Versão do pacote: 0.12.0 (mudança de contrato público, só aditiva).

**Impacto para Web/Api (sinalizar no PR)**

- `RepositoryManager` do Vault herda da base e **não muda**.
- Implementações manuais de `IRepositoryManager` quebram. No Vault, só o `FakeRepositoryManager` (Application.Test), tratado no Passo 5.
- Testes com Moq que verificam `SaveAsync()` continuam válidos.

**Testes** (`LumiaFoundation.EFRepository.Test`)

- A sobrecarga persiste as alterações normalmente.
- Com token já cancelado, lança `OperationCanceledException` e não persiste.

**Documentação** (`LumiaFoundation.EFRepository/docs/readme.md`)

- Seção "4. Salve as alterações": mostrar também o uso com `CancellationToken`.
- Histórico de versões: entrada `0.12.0` descrevendo a sobrecarga (aditiva, sem quebra).
- Tabela do `readme.md` da raiz: atualizar a versão do pacote.

**Como validar:** `dotnet test LumiaFoundation.slnx`. Publicar o pacote no NuGet **antes** do Passo 3.

**Critério de pronto:** testes verdes e pacote 0.12.0 publicado.

---

## Passo 2: Domain

- `Movimento`: nova propriedade `DateTime DataAtualizacaoValorLiquido { get; set; }`, **sem valor padrão** (quem cria é responsável por preenchê-la).

**Critério de pronto:** a solution compila. Os testes existentes que montam `Movimento` continuam compilando (`DateTime` assume `default`); ajustar a data nos testes dos Passos 3 e 5 onde ela importar.

---

## Passo 3: Persistence

**Pacote:** `Persistence.csproj` sobe `Lumia.Foundation.EFRepository` para **0.12.0**.

**`MovimentoMapping`**

- Propriedade `DataAtualizacaoValorLiquido` obrigatória (`datetime(6)`).
- Índice composto `(UserId, EstaAtivo, DataAtualizacaoValorLiquido)`, com nome `IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido` (57 caracteres, abaixo do limite de 64 do MariaDB).
- Comentário no mesmo estilo dos índices anteriores: indicar o acoplamento com a ordenação do repositório e referenciar o ADR 0011.

**`IMovimentoRepository`** (dois métodos novos, ambos com token **obrigatório**)

- `Task<List<Movimento>> GetDesatualizadosAsync(Guid ownerId, DateTime dataLimite, int quantidade, CancellationToken cancellationToken)`
- Sobrecarga `Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges, CancellationToken cancellationToken)`. É **sobrecarga** (o `GetAsync` sem token continua existindo), pelo motivo da seção 4.

**`MovimentoRepository`**

- `GetDesatualizadosAsync`:
  - Parte de `QueryWithRelatedEntities()` (carrega Objetivo, Corretora, Produto, Emissor e tipo, como as demais consultas).
  - Filtros: `UserId == ownerId`, `EstaAtivo` e `DataAtualizacaoValorLiquido < dataLimite` (estritamente menor).
  - Comparar a **coluna direto** com o limite, sem `.Date`, para o índice ser utilizável.
  - Ordenação: `DataAtualizacaoValorLiquido` ascendente e depois `Id` ascendente.
  - `Take(quantidade)` e `ToListAsync(cancellationToken)`; sem tracking.
- Sobrecarga de `GetAsync`: mesma consulta do `GetAsync` atual, terminando em `SingleOrDefaultAsync(cancellationToken)`. Manter o filtro por `ownerId` (isolamento multi-tenant).

**Testes** (`Persistence.Test`)

- `MovimentoMappingTests`: o índice composto existe, com as três colunas na ordem certa.
- `MovimentoRepositoryTests`, para `GetDesatualizadosAsync`:
  - isola por usuário (movimento de outro usuário não aparece);
  - ignora inativos;
  - fronteira: data igual ao limite **não** entra; um dia antes entra;
  - ordem ascendente, com desempate por `Id`;
  - respeita `quantidade`;
  - carrega os nomes das entidades relacionadas.
- `MovimentoRepositoryTests`, para a sobrecarga de `GetAsync`: não devolve movimento de outro usuário; respeita `trackChanges`.

**Critério de pronto:** `dotnet test` verde em Persistence.

---

## Passo 4: Migration `MovimentoDataAtualizacaoValorLiquido`

Gerada pelo **mantenedor**, depois dos Passos 2 e 3. O dev deixa o código pronto e informa o comando.

**Conteúdo esperado (revisar o arquivo gerado)**

- `Up`, nesta ordem:
  1. `AddColumn` de `DataAtualizacaoValorLiquido` (`datetime(6)`, `NOT NULL`, com padrão temporário para as linhas existentes);
  2. `migrationBuilder.Sql` com `UPDATE Movimentos SET DataAtualizacaoValorLiquido = DataInvestimento`;
  3. `CreateIndex` com o nome do Passo 3.
- `Down`: remove o índice e a coluna.
- **Remover o ruído de `IdentityRole`** (`DeleteData`/`InsertData`) que o EF gera por causa do `HasData` sem `Id` fixo (ver a discussão em andamento sobre o assunto). Não deixar isso entrar na migration.
- A aplicação no Docker já é coberta pelos bundles; sem mudança no compose.

**Como validar**

- Aplicar no banco local e rodar `DESCRIBE Movimentos;` e `SHOW INDEX FROM Movimentos;`.
- Conferir que registros antigos têm a data igual à `DataInvestimento`.
- Testar o `Down` num banco descartável.

**Critério de pronto:** migration revisada, aplicada e revertida com sucesso localmente.

---

## Passo 5: Application

Todas as novas assinaturas recebem `CancellationToken` **sem valor padrão**.

### 5.1 Query de movimentos desatualizados

Pasta: `Application/Movimentos/Queries/GetMovimentosDesatualizados/`.

- **`MovimentoDesatualizadoModel`**: `Id`, `ObjetivoNome`, `CorretoraNome`, `ProdutoNome`, `EmissorNome`, `DataVencimento` e `ValorLiquidoAtual`, com `FromDomain(Movimento)`. **Não reaproveitar** `ProximoVencimentoModel` (as telas vão divergir, como no ADR 0010).
- **`IGetMovimentosDesatualizadosQuery`** e **`GetMovimentosDesatualizadosQuery`**:
  - `ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)` retorna `List<MovimentoDesatualizadoModel>`.
  - Constantes nomeadas na classe: `DiasLimite = 30` e `QuantidadeMaxima = 12`, no estilo das constantes de `GetProximosVencimentosQuery`.
  - Calcula o limite com `IDateTimeProvider`: início do dia de hoje (UTC) menos `DiasLimite`.
  - Chama `GetDesatualizadosAsync(ownerId, limite, QuantidadeMaxima, cancellationToken)` e mapeia com `FromDomain`.
  - `catch` registra o erro e relança, **exceto** `OperationCanceledException` (que não é logada), como em `GetMovimentosListQuery`. Não engolir exceções.

### 5.2 Command de atualização do valor líquido

Pasta: `Application/Movimentos/Commands/UpdateValorLiquidoMovimento/`.

- **`MovimentoModelForUpdateValorLiquido`**:
  - Só `ValorLiquidoAtual`, com `[Required]` e `[Range(0, double.MaxValue)]` e a mesma mensagem do update atual.
  - `UpdateDomain(Movimento, DateTime hoje)` altera **exclusivamente** `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` (hoje, só a data).
- **`IUpdateValorLiquidoMovimentoCommand`** e **`UpdateValorLiquidoMovimentoCommand`**:
  - `ExecuteAsync(MovimentoModelForUpdateValorLiquido model, Guid ownerId, Guid movimentoId, CancellationToken cancellationToken)`.
  - Valida com `CommandValidator` (fora do `try`, é erro do cliente).
  - Busca com a sobrecarga `GetAsync(ownerId, movimentoId, trackChanges: true, cancellationToken)`; se `null`, lança `EntityNotFoundException("Movimento não encontrado")`. O `ownerId` na busca garante o isolamento entre usuários.
  - Aplica `UpdateDomain` com a data do `IDateTimeProvider` e chama `SaveAsync(cancellationToken)` (sobrecarga do Passo 1).
  - Mensagens em português.

### 5.3 Alterações em arquivos existentes

- `MovimentoModelForCreation.ToDomain`: passa a receber a data de atualização e preenche `DataAtualizacaoValorLiquido`.
- `CreateMovimentoCommand`: injeta `IDateTimeProvider` e passa a data de hoje (só a data, UTC). A data **não** vem do cliente nem da `DataInvestimento`.
- `MovimentoModelForUpdate` e `UpdateMovimentoCommand`: **não mudam** (D7).
- `MovimentoModel` (e, no Service, `MovimentoDto`): **não** expõem a nova data.

### 5.4 Testes (`Application.Test`)

- Query:
  - o limite é calculado com relógio fake (hoje 10/10 → limite 10/09 00:00);
  - repassa `ownerId`, limite, quantidade e token ao repositório;
  - falha do repositório é logada e relançada;
  - `OperationCanceledException` não é logada.
- Command:
  - altera só `ValorLiquidoAtual` e a data; os demais campos ficam intactos;
  - movimento inexistente ou de outro usuário lança `EntityNotFoundException`;
  - valor negativo lança exceção de validação e **não** consulta nem persiste;
  - o token chega ao `GetAsync` e ao `SaveAsync`.
- `CreateMovimentoCommandTests`: a data nasce igual a hoje, mesmo com `DataInvestimento` retroativa.
- `UpdateMovimentoCommandTests`: teste de regressão de que o `PUT` completo **não** altera a data (D7).
- Fakes:
  - `FakeMovimentoRepository`: implementar os métodos novos de `IMovimentoRepository`;
  - `FakeRepositoryManager`: implementar a sobrecarga `SaveAsync(CancellationToken)`.

**Critério de pronto:** `dotnet test` verde em Application.

---

## Passo 6: Service (Api)

**`Program.cs`**

- Registrar `IDateTimeProvider` → `UtcDateTimeProvider`, com o mesmo escopo usado no Web.
- Confirmar que a Query e o Command novos são registrados pela convenção do `AddServicesFromAssembly`; se não forem, registrar explicitamente.

**DTOs** (`Service/Movimentos/DTOs/`)

- `MovimentoDesatualizadoDto`, com `FromApplication` para item e lista.
- `MovimentoForUpdateValorLiquidoDto` (somente `ValorLiquidoAtual`), com `ToUpdateValorLiquidoCommand()`.
- `MovimentoDto`, `MovimentoForCreationDto` e `MovimentoForUpdateDto`: sem mudança.

**`MovimentosController`**

- Duas dependências novas no construtor (query e command).
- `GET api/movimentos/desatualizados` → 200 com a lista. O `userId` vem do token, como nas demais ações.
- `PATCH api/movimentos/{id:guid}/valor-liquido` → 204.
- Ambos recebem o `CancellationToken` da requisição (`HttpContext.RequestAborted`) e o repassam.
- `ProducesResponseType`: 200/204, 404 (PATCH), 422 (PATCH) e 500, para alimentar o OpenAPI.
- Cuidado com a ordem das rotas: `desatualizados` é literal e não deve conflitar com `{id:guid}`.

**Testes** (`Service.Test`)

- Atualizar o construtor em `MovimentosControllerTests`.
- GET: mapeia a lista e repassa `userId` e token.
- PATCH: chama o command com o `userId` do token, o `id` da rota e o token; devolve `NoContentResult`.
- Mapeamento dos DTOs novos.

**Critério de pronto:** `dotnet test` verde em Service.

---

## Passo 7: Presentation (Web)

**`Presentation/Movimentos/`**

- `MovimentoDesatualizadoDto` (record selado, mesmos campos do modelo do Application) e `MovimentoForUpdateValorLiquidoDto`.
- `IMovimentoApi` e `MovimentoApi`:
  - `GetDesatualizadosAsync(CancellationToken)`: GET em `api/movimentos/desatualizados`, com fallback `[]` se vier vazio.
  - `UpdateValorLiquidoAsync(Guid id, MovimentoForUpdateValorLiquidoDto dto, CancellationToken)`: `HttpMethod.Patch` em `api/movimentos/{id}/valor-liquido`.

**`Pages/Movimentos/Desatualizados.cshtml` e `.cs`**

- Modelo: lista de movimentos, `ErrorMessage`, `Guid? IdComErro` e um `InputModel` com `ValorLiquidoAtual` (`Required` e `Range` ≥ 0, mesma mensagem do Web).
- `OnGetAsync`: carrega a lista.
- `OnPostAtualizarAsync(Guid id)` (`asp-page-handler="Atualizar"` e `asp-route-id`):
  - `ModelState` inválido → recarrega a lista, marca `IdComErro`, devolve `Page()`;
  - `ApiException` → mensagem genérica, recarrega e devolve `Page()`;
  - sucesso → `RedirectToPage()` (PRG, evita reenvio ao dar F5).
- Tabela:
  - colunas iguais às de Próximos Vencimentos: Objetivo, Corretora, Produto, Emissor, Vence, Valor Líquido e Ações;
  - sem regras de cor;
  - coluna Ações com mini-formulário: campo numérico (`step="0.01"`, `min="0"`, `required`, placeholder "Novo valor") e botão **Atualizar**;
  - estado vazio: "Nenhum movimento a atualizar.";
  - texto fixo informando que exibe até 12 movimentos, do mais antigo ao mais recente.
- Cuidados de HTML:
  - `name="Input.ValorLiquidoAtual"` manual e `id` **único por linha** (o `asp-for` repetiria o mesmo `id` em todas as linhas);
  - se o campo for pré-preenchido, formatar com cultura invariante, por causa do `type="number"`;
  - mostrar a mensagem de erro na linha de `IdComErro`.
- `ViewData["Title"]` e `<h1>`: "Manutenção Valor Líquido".

**Menu** (`Pages/Shared/_Layout.cshtml`)

- Novo dropdown **"Manutenção"**, ao lado de "Cadastros", no mesmo padrão de marcação (`nav-item dropdown`).
- Item **"Manutenção Valor Líquido"** apontando para `/Movimentos/Desatualizados`.

**Testes** (`Presentation.Test`)

- `MovimentoApiTests`: rota, query string e verbo dos dois métodos novos (o segundo deve usar PATCH).
- `DesatualizadosModelTests`:
  - `OnGet` carrega a lista;
  - `ModelState` inválido não chama a Api, marca `IdComErro` e recarrega;
  - sucesso chama a Api com o id e o valor e redireciona;
  - `ApiException` mostra a mensagem e devolve `Page()`.

**Critério de pronto:** `dotnet test` verde em Presentation e a tela funciona manualmente (ver seção 6).

---

## Passo 8: Documentação final e fechamento

- `Docs/adr/0011-atualizacao-do-valor-liquido.md`: mudar o status para **Aceita**, com a data.
- `Docs/adr/0010-proximos-vencimentos.md`:
  - acrescentar ao status: "Parcialmente superada pelo ADR 0011 quanto ao relógio (agora também na Api)";
  - corrigir a quantidade de itens (15 vs 12) para o que o código realmente usa.
- Docs da lib (feitas no Passo 1): readme do `EFRepository` e histórico de versões.
- READMEs do Vault: se algum listar endpoints ou telas, incluir o endpoint e a tela novos. (Nenhum README do Vault foi analisado; o dev verifica.)
- `Docs/Docker.md` e `Docs/MariaDb.md`: sem mudança.
- Seed de Movimentos (discussão em andamento sobre a carga inicial): garantir que preencha `DataAtualizacaoValorLiquido`.

---

## 6. Validação ponta a ponta

1. `dotnet test` na lib e depois na solução do Vault, tudo verde.
2. Aplicar a migration e conferir `DESCRIBE Movimentos;` e `SHOW INDEX FROM Movimentos;`. Registros antigos com data igual à `DataInvestimento`.
3. Fronteira (hoje 10/10): atualizado em 10/09 **não** aparece; em 09/09 aparece. Ajustar a data direto no banco para testar.
4. Cadastrar um Movimento com `DataInvestimento` retroativa. A data de atualização deve ser a de hoje.
5. Na tela, informar um valor novo e clicar em Atualizar. Só `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` mudam, e o item sai da lista.
6. Editar um movimento pela tela de Edição. A data de atualização **não** muda (D7).
7. `PATCH` em movimento de outro usuário retorna 404. Valor negativo retorna 422.
8. Com mais de 12 desatualizados, a tela mostra os 12 mais antigos; ao atualizar um, o próximo entra.
9. `EXPLAIN` da consulta quando houver volume, confirmando o uso do índice.

## 7. Riscos e pontos de atenção

- O `MovimentosController` passa a ter mais dependências. Se continuar crescendo, avaliar separar em outro controller.
- A propagação do token fora do escopo desta feature segue na discussão geral de `CancellationToken`.
- O cálculo é em UTC; entre 21h e 24h (BRT) a fronteira dos 30 dias já considera o dia seguinte (efeito só visual, registrado no ADR).
- O valor pode ser editado na Edição sem renovar a data (D7); reavaliar se incomodar.

## 8. Fora de escopo

- Exibir a data de última atualização em tela ou DTO.
- Paginação, filtros ou total de pendentes na tela.
- Propagação de `CancellationToken` nas demais rotas.
- Atualização em lote.
