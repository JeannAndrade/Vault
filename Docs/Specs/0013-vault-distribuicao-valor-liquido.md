# Especificação técnica 0013: Distribuição do Valor Líquido por capital e tempo (Vault)

- Data: 2026-10-14
- Status: aprovada para implementação
- ADR relacionado: [0013](../adr/0013-distribuicao-do-valor-liquido.md)
- Repositório afetado: **Vault** (Domain, Persistence, Application, Service, Presentation/Web). **Sem alteração no LumiaFoundation, no schema e sem migration.**

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.
> O documento é entregue por camadas. Partes entregues até agora: cabeçalho, Passo 0 (ADR), Passo 1 (Domain), Passo 2 (Persistence), Passo 3 (Application) e Passo 4 (Service).

---

## 1. Resumo

O usuário escolhe um título (Tipo de Renda, Corretora, Produto e Emissor, os quatro obrigatórios) e informa o valor líquido total que ele tem hoje. O sistema distribui esse valor entre os Movimentos **ativos** do título, ponderando o valor de cada aporte e o tempo que ele ficou investido, e mostra um **preview**. Ao confirmar, a Api recalcula a distribuição e grava `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` de todos os aportes do lote, em uma única transação. A soma dos valores fecha exatamente no total informado.

## 2. Decisões aprovadas

| Item | Decisão |
|---|---|
| D1 | Algoritmo: taxa efetiva média, ganho projetado por aporte e normalização do ganho (seção do Passo 1) |
| D2 | Tempo em dias corridos: `(dataReferência − DataInvestimento).Days / 365`; `t` negativo é tratado como zero |
| D3 | Data de referência via `IDateTimeProvider`, só a data (UTC) |
| D4 | Quatro filtros obrigatórios (`TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId`); tipo de filtro próprio, sem reutilizar `MovimentoFiltro` |
| D5 | Só `EstaAtivo = true`, escopado por `UserId`; objetivo inativo não filtra |
| D6 | Sem paginação e sem limite de itens |
| D7 | Preview antes de gravar; na gravação a Api **recalcula** (sem carimbo do preview) |
| D8 | Controller novo, `DistribuicaoValorLiquidoController`, em `api/movimentos/distribuicao-valor-liquido` |
| D9 | Cálculo em serviço de domínio puro (sem I/O e sem relógio) |
| D10 | O aporte **mais recente** (ordem `DataInvestimento`, `Id`) absorve os centavos de arredondamento |
| D11 | Erros de regra viram 422: sem aportes na gravação, todos com `t = 0` e ganho diferente de zero, e valor negativo em algum aporte |
| D12 | Sem schema, sem migration, sem índice novo (gatilho: `EXPLAIN` em volume realista) |
| D13 | `DataAtualizacaoValorLiquido` = hoje (só a data) em todos os aportes do lote |
| D14 | `CancellationToken` **obrigatório** (sem `= default`) nas novas assinaturas |
| D15 | Tela nova, `/Movimentos/Distribuicao`, no menu "Manutenção" |

Regras de negócio adicionais: lucro negativo é permitido; `EhReinvestimento = true` entra normalmente; perda total (`V = 0`) zera todos os valores; `G = 0` zera todos os ganhos.

## 3. Atenção antes de começar

1. **`CancellationToken` obrigatório** nas novas assinaturas de repositório, query e command, e propagado de ponta a ponta. `OperationCanceledException` não é logada como erro (padrão de `GetMovimentosDesatualizadosQuery`).
2. **Dinheiro em `decimal`.** O `double` é usado só nos pesos do cálculo (`Math.Pow` não existe para `decimal`).
3. **Dependências entre camadas:** Service → Application → Persistence → Domain. O Domain não referencia nenhuma outra camada.
4. **Multi-tenant:** o `UserId` é sempre aplicado no repositório. Nenhum caminho pode ler ou gravar Movimentos de outro usuário.
5. **A Api nunca recebe valores calculados pelo Web.** Ela recalcula a partir do banco.
6. **Sem engolir exceções:** falhas são logadas e relançadas, como nas queries atuais. Erros de regra de negócio ficam **fora do `try`** (são erros do cliente, 422, não falha do serviço).
7. **Independente do ADR 0012:** a feature não usa a paginação nem o filtro da listagem. Pode ser implementada antes, depois ou em paralelo.
8. **Todo código, comentários, commits e documentação em português.**
9. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo.

## 4. Ordem de implementação

Cada passo termina com `dotnet build` e `dotnet test` verdes antes do seguinte.

| # | Passo | Projeto |
|---|---|---|
| 0 | ADR 0013 como "Proposta" | Vault (docs) |
| 1 | Calculador de domínio | Domain |
| 2 | Repositório | Persistence |
| 3 | Query de simulação e command de gravação | Application |
| 4 | Controller e DTOs | Service (Api) |
| 5 | Página, cliente da Api e menu | Presentation (Web) |
| 6 | Documentação final e fechamento do ADR | Vault |

---

## Passo 0: ADR 0013 como "Proposta"

- Criar `Docs/adr/0013-distribuicao-do-valor-liquido.md` com o texto do arquivo `0013-distribuicao-do-valor-liquido.md` entregue junto com esta especificação, **Status: Proposta (2026-10-14)**.
- Atualizar o índice de ADRs, se existir.

**Critério de pronto:** arquivo criado, no padrão dos ADRs anteriores, status "Proposta".

---

## Passo 1: Domain

Pasta: `Domain/Movimentos/Distribuicao/`. Sem pacote novo (o Domain já referencia `Lumia.Foundation.Core`).

### 1.1 Tipos

Todos imutáveis (`record` seladas), sem dependência de `Movimento`, para o cálculo ser testável sem montar entidades.

- **`AporteParaDistribuicao`**: `Guid Id`, `decimal ValorAporte`, `DateTime DataInvestimento`.
- **`AporteDistribuido`**: `Guid Id`, `decimal ValorAporte`, `decimal Ganho`, `decimal ValorLiquido`.
- **`ErroDistribuicao`** (enum): `SemAportes`, `SemBaseDeTempo`, `ValorNegativo`, `CalculoInvalido`.
- **`ResultadoDistribuicao`**: resultado com sucesso ou erro.
  - Sucesso: `IReadOnlyList<AporteDistribuido> Aportes` (na ordem `DataInvestimento`, `Id`), `decimal TotalAportado`, `decimal ValorLiquidoTotal`, `decimal GanhoTotal` e `double? TaxaEfetivaAnual`.
  - Erro: `ErroDistribuicao Erro`.
  - Propriedade `Sucesso` e fábricas estáticas (`Ok(...)`, `Falha(erro)`). Quem consome decide como transformar o erro em exceção; o Domain não conhece HTTP.

### 1.2 `DistribuicaoValorLiquidoCalculador`

Classe **estática** (função pura, sem estado nem dependências; evita registro em DI sem ganho de testabilidade). Método:

- `ResultadoDistribuicao Calcular(IReadOnlyList<AporteParaDistribuicao> aportes, decimal valorLiquidoTotal, DateTime dataReferencia)`

**Pré-condições (erro de programação, não de regra):** `valorLiquidoTotal < 0` lança `ArgumentOutOfRangeException`; `aportes` nulo ou algum `ValorAporte <= 0` lança `ArgumentException`. A validação de entrada do usuário é da camada Application.

**Algoritmo, nesta ordem:**

1. `aportes` vazio → `Falha(SemAportes)`.
2. Ordenar por `DataInvestimento` ascendente e depois `Id` ascendente (o último é o mais recente).
3. Para cada aporte: `t_i = max(0, (dataReferencia.Date − DataInvestimento.Date).Days / 365.0)`.
4. `S = ΣA_i` e `G = V − S`.
5. Se `V = 0`: todos com `ValorLiquido = 0` e `Ganho = −A_i`; `TaxaEfetivaAnual = −1`. Fim.
6. Se `G = 0`: todos com `Ganho = 0` e `ValorLiquido = A_i`; `TaxaEfetivaAnual = 0`. Fim.
7. `T = Σ(A_i × t_i) / S`. Se `T = 0` (todos com `t = 0`) e `G ≠ 0` → `Falha(SemBaseDeTempo)`.
8. `r = (V / S)^(1 / T) − 1` (em `double`).
9. `g_i = A_i × ((1 + r)^t_i − 1)` e `Σg`. Se algum valor não for finito ou `Σg = 0` → `Falha(CalculoInvalido)`.
10. `Ganho_i = G × g_i / Σg`, arredondado a 2 casas (`MidpointRounding.AwayFromZero`) para todos **menos o último**. O último recebe `G − Σ(ganhos arredondados dos demais)`, de modo que a soma dos ganhos seja exatamente `G`.
11. `ValorLiquido_i = A_i + Ganho_i`. Se algum for negativo → `Falha(ValorNegativo)`.
12. Retornar sucesso com `TaxaEfetivaAnual = r`.

Observação (decisão P1 do ADR): como o aporte mais recente pode ter `t = 0`, ele pode receber ou perder alguns centavos de ganho por causa da compensação. É esperado e aceito.

### 1.3 Testes (`Domain.Test`)

Remover o `UnitTest1.cs` de exemplo. Criar `Movimentos/Distribuicao/DistribuicaoValorLiquidoCalculadorTests.cs`, com data de referência fixa em todos os testes (por exemplo, `2026-10-14`) e aportes criados em dias exatos antes dela (365 dias = 1 ano).

- **Cenário do ADR** (aportes 1.200 com 730 dias, 2.000 com 365 dias e 800 com 0 dia; `V = 5.000`): a soma dos `ValorLiquido` é exatamente `5.000`; a soma dos ganhos é exatamente `1.000`; o aporte de 730 dias tem ganho maior que o de 365 dias; o ganho do aporte mais recente fica entre `-0,02` e `0,02`. Não fixar valores decimais exatos, só as propriedades.
- **Duas datas e valores iguais, `G = 1.000,01`:** os dois aportes têm a mesma ponderação; o primeiro fica com `500,01` e o último com `500,00` (compensação de centavos).
- **Aporte único:** recebe todo o ganho e `ValorLiquido = V`.
- **`G = 0`:** todos os ganhos zero; sem exceção.
- **`V = 0`:** todos os valores zero e ganhos `−A_i`.
- **Lucro negativo moderado** (por exemplo, `V = 0,9 × S`, com datas distintas): sucesso, todos os ganhos negativos e a soma exata.
- **Todos com `t = 0` e `G ≠ 0`:** `Falha(SemBaseDeTempo)`. Com `G = 0`: sucesso com ganhos zero.
- **Data de investimento futura:** tratada como `t = 0` (equivale a aporte de hoje).
- **Prejuízo extremo → `ValorNegativo`:** aportes de 100 com 1.095 dias e de 900 com 4 dias, `V = 50`. O resultado esperado é `Falha(ValorNegativo)`. Rodar e conferir; se um ajuste de dias mudar o resultado, ajustar `V` mantendo a intenção.
- **Ordem de entrada irrelevante:** a mesma lista embaralhada produz o mesmo resultado, na ordem `DataInvestimento`, `Id`.
- **Lista vazia:** `Falha(SemAportes)`.
- **Pré-condições:** `V` negativo → `ArgumentOutOfRangeException`; `ValorAporte <= 0` → `ArgumentException`.

**Como validar:** `dotnet test Domain.Test`.

**Critério de pronto:** testes verdes, sem dependência nova, sem uso de relógio dentro do calculador.

---

## Passo 2: Persistence

**Sem alteração de schema.** Não há coluna, índice, migration nem mudança no `MovimentoMapping`. A consulta usa as FKs já indexadas em `(UserId, X)` e o volume por título é pequeno (D12).

### 2.1 Filtro do título

Criar `MovimentoTituloFiltro` em `Persistence/Movimentos/` (ao lado de `MovimentoFiltro`, mas **sem relação com ele**):

- Record selado e imutável, com `TipoRendaId`, `CorretoraId`, `ProdutoId` e `EmissorId`, todos `Guid` **não anuláveis** e `required init` (os quatro são obrigatórios, D4).
- Sem valor "vazio": não existe consulta sem título.
- O repositório não valida `Guid.Empty`; isso é da Application (Passo 3). Se chegar um `Guid.Empty`, a consulta simplesmente não encontra nada.

### 2.2 `IMovimentoRepository` (dois métodos novos)

Ambos com `CancellationToken` **obrigatório** (D14) e o `ownerId` sempre aplicado:

- `Task<List<Movimento>> GetAtivosDoTituloAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken)`: para a **simulação**. Sem tracking, com o `Objetivo` carregado (o preview exibe o nome do objetivo).
- `Task<List<Movimento>> GetAtivosDoTituloParaAtualizacaoAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken)`: para a **gravação**. **Com tracking** e sem `Include`, porque o command só altera valores.

Por que dois métodos: o `QueryWithRelatedEntities()` usa `AsNoTracking()`, então não serve para quem vai gravar, e carregar o Objetivo na gravação seria custo sem uso. Cada método declara a sua intenção.

### 2.3 `MovimentoRepository`

Os dois métodos aplicam os mesmos critérios:

- Filtros: `UserId == ownerId`, `EstaAtivo`, `TipoRendaId`, `CorretoraId`, `ProdutoId` e `EmissorId` iguais aos do título.
- Ordenação: `DataInvestimento` ascendente e depois `Id` ascendente. É a mesma ordem usada pelo calculador (o último é o mais recente, D10), então a lista já sai na ordem do cálculo.
- `ToListAsync(cancellationToken)`.
- Simulação: partir de `FindByCondition(..., trackChanges: false)` com `.Include(m => m.Objetivo)`, ou de `RepositoryContext.Set<Movimento>().AsNoTracking().Include(...)`, mantendo o estilo do repositório.
- Gravação: partir de `FindByCondition(..., trackChanges: true)`.
- Evitar duplicar o `Where`: extrair um método privado que monta a consulta base filtrada e ordenada, parametrizado por `trackChanges`.
- Comentário curto indicando que a ordenação acompanha o calculador de domínio (ADR 0013) e que, com volume por título pequeno, não se justifica índice.

### 2.4 Ajustes em outros projetos

- `Application.Test/Movimentos/TestDoubles/FakeMovimentoRepository.cs`: implementar os dois métodos novos (pode lançar `NotSupportedException`, como os demais), apenas para a solution compilar. Os testes do Passo 3 usarão `Mock<IMovimentoRepository>`.

### 2.5 Testes (`Persistence.Test/Movimentos/MovimentoRepositoryTests.cs`)

O helper `AdicionarMovimento` atual cria entidades novas (Objetivo, Tipo de Renda, Corretora, Produto, Emissor) a cada chamada e deixa `EstaAtivo = false`. Para estes testes, criar um helper próprio (por exemplo, `AdicionarMovimentoDoTitulo`) que receba as quatro entidades do título já criadas, o `userId`, a `DataInvestimento` e `EstaAtivo`, e devolva o `Movimento`.

`GetAtivosDoTituloAsync`:

- Retorna só os aportes **ativos** do título e do usuário.
- Exclui, cada um em seu caso: movimento **inativo**; de outro **Tipo de Renda**; de outra **Corretora**; de outro **Produto**; de outro **Emissor** (um `Theory` ou um teste por campo, para provar que **cada** filtro é aplicado).
- Isolamento: um movimento de **outro usuário** com os mesmos IDs de título não aparece.
- Ordem: `DataInvestimento` ascendente, com desempate por `Id` ascendente (datas iguais).
- Carrega o nome do Objetivo.
- Sem tracking: as entidades ficam `Detached` (mesmo padrão do teste de `GetAsync` com token).
- Título sem aportes: lista vazia.

`GetAtivosDoTituloParaAtualizacaoAsync`:

- Mesmos critérios de filtro e isolamento (um teste que cubra inativo, outro título e outro usuário).
- Mesma ordem.
- Com tracking: as entidades ficam `Unchanged` no contexto, e alterar `ValorLiquidoAtual` seguido de `SaveChangesAsync` persiste a mudança (confirma que o command conseguirá gravar).

`MovimentoMappingTests`: sem alteração (nenhum índice novo).

### 2.6 Como validar

- `dotnet test Persistence.Test`.
- Conferir que o modelo não mudou: `dotnet ef migrations has-pending-model-changes --project Persistence --startup-project Persistence --context VaultDbContext` deve informar que não há mudanças pendentes.

**Critério de pronto:** testes verdes, nenhuma migration nova e solution compilando (incluindo o `FakeMovimentoRepository`).

---

## Passo 3: Application

Sem pacote novo. Todas as novas assinaturas recebem `CancellationToken` **sem valor padrão** (D14). A data de hoje vem do `IDateTimeProvider` (já registrado na Api desde o ADR 0011), lida **uma única vez** por execução.

### 3.0 Verificação prévia (somente leitura, antes de codar)

O Domain devolve erros de regra como valor (`ErroDistribuicao`). A Application precisa convertê-los em uma exceção que a Api responda com **422** e com mensagem legível.

- Na LumiaFoundation, conferir como o `DomainExceptionHandler` mapeia as exceções e qual aceita uma mensagem livre. A candidata natural é `CommandValidationException` (é a que `CommandValidator` lança e que já resulta em 422 nos testes atuais): ver o construtor.
- **Não** usar `EntityInUseException` (tem outro significado) nem criar exceção própria sem mapeamento no handler (ela viraria 500).
- **Se nenhuma exceção existente servir, parar e sinalizar.** Qualquer mudança na lib é alteração de contrato público e exige discussão prévia.

Nos itens abaixo, "exceção de regra" é a exceção escolhida nessa verificação.

### 3.1 Tipos compartilhados

Pasta: `Application/Movimentos/DistribuicaoValorLiquido/` (compartilhada pela query e pelo command).

- **`DistribuicaoValorLiquidoParameters`** (record, no estilo de `MovimentoFiltroParameters`):
  - `TipoRendaId`, `CorretoraId`, `ProdutoId` e `EmissorId`: `Guid` com `[NotEmptyGuid]` (`LumiaFoundation.Core.ValidationAttributes`).
  - `ValorLiquidoTotal`: `decimal` com `[Required]` e `[Range(0, double.MaxValue)]`, mensagem "O valor líquido total não pode ser negativo".
  - `ToTituloFiltro()` devolve `MovimentoTituloFiltro` (Passo 2).
- **`AporteDistribuidoModel`** (record): `Id`, `ObjetivoNome`, `DataInvestimento`, `ValorAporte`, `ValorLiquidoAtual` (o valor gravado hoje), `NovoValorLiquido` e `Ganho`.
- **`DistribuicaoValorLiquidoModel`** (record):
  - `List<AporteDistribuidoModel> Aportes`, `TotalAportado`, `GanhoTotal`, `ValorLiquidoTotal` (o informado), `double? TaxaEfetivaAnual` (fração, não percentual) e `string? Aviso`.
  - `PodeConfirmar`: verdadeiro somente se `Aviso` é nulo e há aportes.
- **Mensagens de erro:** um único ponto (método estático interno, por exemplo `MensagemDoErro(ErroDistribuicao)`), usado tanto como `Aviso` do preview quanto como mensagem da exceção de regra. Textos propostos:
  - `SemAportes`: "Não há aportes ativos para o título selecionado."
  - `SemBaseDeTempo`: "Todos os aportes do título foram feitos hoje; não há tempo de aplicação para distribuir o rendimento."
  - `ValorNegativo`: "A distribuição resultaria em valor negativo para algum aporte. Atualize os movimentos individualmente."
  - `CalculoInvalido`: "Não foi possível distribuir o valor informado para este título."
- **Auxiliar interno estático** (para não duplicar entre query e command): converte `Movimento` em `AporteParaDistribuicao` e devolve o dicionário por `Id`.

### 3.2 Query de simulação

Pasta: `Application/Movimentos/Queries/SimularDistribuicaoValorLiquido/`.

- **`ISimularDistribuicaoValorLiquidoQuery`** e **`SimularDistribuicaoValorLiquidoQuery`**: dependências `IRepositoryManager`, `ILoggerManager` e `IDateTimeProvider`.
- `ExecuteAsync(Guid ownerId, DistribuicaoValorLiquidoParameters parametros, CancellationToken cancellationToken)` retorna `DistribuicaoValorLiquidoModel`.
- Fluxo:
  1. `CommandValidator.Validate(parametros)` **fora do `try`** (erro do cliente, 422).
  2. `hoje = GetDateTime().Date`.
  3. `GetAtivosDoTituloAsync(ownerId, parametros.ToTituloFiltro(), cancellationToken)`.
  4. `DistribuicaoValorLiquidoCalculador.Calcular(aportes, parametros.ValorLiquidoTotal, hoje)`.
  5. Sucesso: montar o modelo juntando o resultado do Domain com `ObjetivoNome`, `DataInvestimento` e `ValorLiquidoAtual` do `Movimento` (pelo `Id`).
  6. Falha (qualquer `ErroDistribuicao`): **não lançar**. Devolver o modelo com `Aportes` vazio, `Aviso` preenchido e `PodeConfirmar` falso (D11 e ADR: o preview exibe o aviso).
- `catch (Exception ex) when (ex is not OperationCanceledException)` registra com `_logger.LogError` e relança, como em `GetMovimentosDesatualizadosQuery`.

### 3.3 Command de gravação

Pasta: `Application/Movimentos/Commands/DistribuirValorLiquido/`.

- **`IDistribuirValorLiquidoCommand`** e **`DistribuirValorLiquidoCommand`**: dependências `IRepositoryManager` e `IDateTimeProvider`.
- `ExecuteAsync(DistribuicaoValorLiquidoParameters parametros, Guid ownerId, CancellationToken cancellationToken)` retorna `Task`.
- Fluxo (a Api **recalcula**, sem confiar no Web, D7):
  1. `CommandValidator.Validate(parametros)`.
  2. `hoje = GetDateTime().Date`.
  3. `GetAtivosDoTituloParaAtualizacaoAsync(...)` (com tracking).
  4. `Calcular(...)`. Se falhar (**inclusive `SemAportes`**, que na gravação não é preview vazio): lançar a exceção de regra com a mensagem do erro, **antes de alterar qualquer entidade**.
  5. Para cada aporte distribuído, localizar o `Movimento` pelo `Id` e atualizar `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido = hoje`. Reutilizar `MovimentoModelForUpdateValorLiquido.UpdateDomain(movimento, hoje)` (ADR 0011), para existir uma só regra de "atualizar valor líquido". Chamar `_repositoryManager.Movimento.Update(movimento)`, como nos demais commands.
  6. Um único `SaveAsync(cancellationToken)` (sobrecarga do ADR 0011): uma transação, tudo ou nada.
- Sem `try/catch` com log, como nos commands existentes (a Api trata e registra).

### 3.4 Alterações em arquivos existentes

Nenhuma. A Query e o Command novos seguem a convenção de registro do `AddServicesFromAssembly` (confirmar no Passo 4).

### 3.5 Testes (`Application.Test`)

Seguir o estilo de `GetMovimentosDesatualizadosQueryTests` e `UpdateValorLiquidoMovimentoCommandTests` (`Mock<IMovimentoRepository>`, `Mock<IDateTimeProvider>` com data fixa). Nos `Movimento` de teste, preencher `EstaAtivo`, `ValorAporte`, `DataInvestimento`, `ValorLiquidoAtual` e `Objetivo = new Objetivo { Nome = ... }` (a navegação nasce nula).

**Query** (`Queries/SimularDistribuicaoValorLiquido/`):

- Sucesso: repassa `ownerId`, os quatro IDs e o token ao repositório; o modelo junta nome do objetivo, data e valor atual; a soma dos `NovoValorLiquido` é exatamente `ValorLiquidoTotal`; `PodeConfirmar` verdadeiro e `TaxaEfetivaAnual` preenchida.
- Título sem aportes: modelo com lista vazia, `Aviso` preenchido, `PodeConfirmar` falso, **sem exceção**.
- Todos os aportes de hoje com `G ≠ 0`: `Aviso` de `SemBaseDeTempo`, lista vazia.
- Prejuízo extremo (valor negativo): `Aviso` de `ValorNegativo`, lista vazia.
- Parâmetros inválidos (`Guid.Empty` em cada um dos quatro IDs; valor negativo), em `Theory`: exceção de validação, **sem consultar** o repositório.
- Falha do repositório é logada e relançada; `OperationCanceledException` não é logada.

**Command** (`Commands/DistribuirValorLiquido/`):

- Sucesso: todos os aportes têm `ValorLiquidoAtual` novo e `DataAtualizacaoValorLiquido = hoje`; a soma é exatamente o total informado; `Update` é chamado uma vez por aporte; `SaveAsync(token)` é chamado **uma vez**; os demais campos (`ValorAporte`, `Observacao`) ficam intactos.
- Repassa `ownerId` e o token ao repositório e ao `SaveAsync`.
- Sem aportes: exceção de regra, sem `SaveAsync`.
- `SemBaseDeTempo`: exceção de regra, sem `SaveAsync`, valores originais intactos.
- `ValorNegativo`: exceção de regra, sem `SaveAsync` e **nenhum** movimento alterado (confirma que a mutação só ocorre após o cálculo bem-sucedido).
- Parâmetros inválidos: exceção de validação, sem consultar o repositório.
- Mensagem da exceção em português (`Contains` de um trecho do texto).

**Mensagens:** um `Theory` percorrendo todos os valores de `ErroDistribuicao` e garantindo que cada um tem mensagem não vazia (protege contra um valor novo do enum sem mensagem).

### 3.6 Como validar

`dotnet test Application.Test` e `dotnet build` da solution inteira (o Service ainda não usa os tipos novos).

**Critério de pronto:** testes verdes; erro de regra tratado pela exceção verificada no 3.0; a mutação das entidades só ocorre após cálculo bem-sucedido.

---

## Passo 4: Service (Api)

Sem pacote novo. O `MovimentosController` **não muda** (essa é a vantagem do controller separado, D8).

### 4.1 DTOs

Pasta: `Service/Movimentos/DTOs/`, no estilo dos DTOs de movimento.

- **`DistribuicaoValorLiquidoSolicitacaoDto`** (record com propriedades `init`): `TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId` (`Guid`) e `ValorLiquidoTotal` (`decimal`). Corpo comum às duas ações. Método `ToParameters()` devolve `DistribuicaoValorLiquidoParameters`. Sem DataAnnotations: a validação é do Application (`CommandValidator`, 422).
- **`AporteDistribuidoDto`**: `Id`, `ObjetivoNome`, `DataInvestimento`, `ValorAporte`, `ValorLiquidoAtual`, `NovoValorLiquido` e `Ganho`. Não expõe `UserId`.
- **`DistribuicaoValorLiquidoDto`**: `Aportes` (lista de `AporteDistribuidoDto`), `TotalAportado`, `GanhoTotal`, `ValorLiquidoTotal`, `TaxaEfetivaAnual` (`double?`, fração: 0,225 significa 22,5%), `Aviso` e `PodeConfirmar`. `FromApplication(DistribuicaoValorLiquidoModel)` mapeia tudo, inclusive a lista (inclusive vazia) e `PodeConfirmar`.

### 4.2 `DistribuicaoValorLiquidoController`

Arquivo: `Service/Movimentos/DistribuicaoValorLiquidoController.cs`, namespace `Service.Movimentos`.

- Atributos iguais aos demais: `[ApiController]`, `[Authorize]`, `[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]` e `[Route("api/movimentos/distribuicao-valor-liquido")]`. Herda de `BaseApiController`.
- Construtor com `ISimularDistribuicaoValorLiquidoQuery` e `IDistribuirValorLiquidoCommand`.
- **`POST simulacao`** → 200 com `DistribuicaoValorLiquidoDto`. Chama a query com `GetCurrentUserId()`, `dto.ToParameters()` e `HttpContext.RequestAborted`. `ProducesResponseType`: 200, 422 e 500.
- **`POST`** (sem sufixo) → 204. Chama o command com `dto.ToParameters()`, `GetCurrentUserId()` e `HttpContext.RequestAborted`. `ProducesResponseType`: 204, 422 e 500.
- Os dois são `POST` por serem ações de cálculo com corpo (ADR 0013).
- **Guarda do corpo:** com `SuppressModelStateInvalidFilter` ligado, um corpo malformado (por exemplo, um GUID inválido ou um número maior que o `decimal`) não é barrado pelo framework e o DTO pode chegar nulo. Nas duas ações, antes de qualquer chamada: se `!ModelState.IsValid` ou o DTO for nulo, lançar `HttpBaseException` com 422 e mensagem em português ("Corpo da requisição inválido."), no mesmo padrão já usado em `GetMovimentos`. Isso foge do que as outras ações de escrita fazem hoje; vale registrar como melhoria separada para as demais (fora desta feature).
- **Rotas:** os caminhos `.../simulacao` e `.../distribuicao-valor-liquido` são literais e não colidem com as rotas `{id:guid}` do `MovimentosController` (a restrição `:guid` não casa com texto). Confirmar na validação manual.

### 4.3 `Program.cs`

- `IDateTimeProvider` já está registrado na Api (ADR 0011). Nada a acrescentar.
- A Query e o Command novos devem ser registrados pela convenção do `AddServicesFromAssembly`. **Confirmar**; se a convenção não os pegar, registrá-los explicitamente (`AddScoped`) e registrar o motivo em um comentário.

### 4.4 Testes (`Service.Test`)

**Controller** (`Movimentos/DistribuicaoValorLiquidoControllerTests.cs`), no estilo de `MovimentosControllerTests` (`DefaultHttpContext`, `Items["UserId"]`, `HttpContext.RequestAborted` com `CancellationTokenSource`):

- Simulação: repassa `userId`, os parâmetros mapeados (`It.Is`, conferindo os cinco campos) e o token; devolve `OkObjectResult` com `DistribuicaoValorLiquidoDto` mapeado, incluindo `Aviso` e `PodeConfirmar`.
- Gravação: chama o command com `userId`, parâmetros e token e devolve `NoContentResult`.
- Corpo inválido (`ModelState` com erro) e DTO nulo, nas **duas** ações: `HttpBaseException` com 422 e **sem** chamar a query nem o command.

**DTOs** (`Movimentos/DistribuicaoValorLiquidoDtoTests.cs`):

- `ToParameters()` mapeia os cinco campos.
- `FromApplication` mapeia todos os campos, inclusive os de cada aporte.
- Modelo com lista vazia e `Aviso`: DTO com lista vazia, aviso preservado e `PodeConfirmar` falso.

**Registro** (`Movimentos/DistribuicaoValorLiquidoRegistroTests.cs`, recomendado): montar um `ServiceCollection` com `AddServicesFromAssembly(typeof(IGetCorretoraQuery).Assembly)`, registrar `Mock` para `IRepositoryManager`, `ILoggerManager` e `IDateTimeProvider`, e verificar que `ISimularDistribuicaoValorLiquidoQuery` e `IDistribuirValorLiquidoCommand` são resolvidos. Evita descobrir um registro faltando só em execução.

### 4.5 Como validar

- `dotnet test Service.Test`.
- Subir a Api (`dotnet run` no Service) e conferir no OpenAPI (`MapOpenApiDevTools`) as duas rotas novas e as respostas documentadas.
- Chamadas manuais, com um token válido (exemplos em português nas mensagens esperadas):
  - `POST /api/movimentos/distribuicao-valor-liquido/simulacao` com os quatro IDs de um título e um valor: 200 e `podeConfirmar: true`.
  - O mesmo corpo com IDs de título sem aportes ativos: 200, lista vazia e `aviso` preenchido.
  - `valorLiquidoTotal` negativo, um ID `Guid.Empty` ou um GUID malformado: 422.
  - Sem token: 401.
  - `POST /api/movimentos/distribuicao-valor-liquido` com o mesmo corpo: 204; as rotas `PUT`/`PATCH`/`GET` de `{id}` continuam funcionando (sem colisão).
  - Usuário B com os IDs do título do usuário A: simulação vazia e gravação 422 (isolamento).

**Critério de pronto:** testes verdes, rotas visíveis no OpenAPI, registro por convenção confirmado e nenhuma alteração no `MovimentosController`.

---

_(continua: Passo 5, Presentation)_
