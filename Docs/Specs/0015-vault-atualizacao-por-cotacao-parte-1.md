# Especificação técnica 0015: Atualização do Valor Líquido por cotação (Vault)

- Data: 2026-10-17
- Status: **em elaboração (parte 1 de 3: Passos 0 a 3)**
- ADR relacionado: 0016 (Passo 0)
- Repositório afetado: **Vault** (Domain, Persistence, Application, Service, Presentation/Web). Sem alteração no LumiaFoundation (confirmar no Passo 4, parte 2).

> Especificação para outro dev implementar. Sem código de implementação: descreve o que fazer, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.
> **Partes:** 1 = Passos 0 a 3 (esta); 2 = Passos 4 e 5 (Application e Service); 3 = Passos 6 e 7 (Web e fechamento), mais validação ponta a ponta e riscos.

---

## 1. Resumo

Movimentos marcados com a flag `AtualizaPorCotacao` têm o valor líquido derivado de `Quantidade × cotação` do papel (nome do Emissor). Uma tela em "Manutenção" recebe a cotação de cada papel distinto, grava o histórico na nova entidade `Cotacao` (sem vínculo com Emissor) e recalcula os Movimentos ativos do papel. Há CRUD de Cotação e um gráfico de linhas Valor × Data por papel.

## 2. Decisões aprovadas

| Item | Decisão |
|---|---|
| D1 | Flag `AtualizaPorCotacao` em `Movimento` (`bool`, padrão `false`); com ela marcada, `Quantidade` obrigatória e maior que zero no POST/PUT |
| D2 | Cálculo no `Movimento.AplicarCotacao`; arredondamento a 2 casas, `AwayFromZero` |
| D3 | `Cotacao`: `UserId`, `Papel` (`varchar(60)`), `Valor` (`decimal(18,4)`), `Data` (`date`); sem FK nem navegação para Emissor e User |
| D4 | Uma cotação por `(UserId, Papel, Data)`: índice único e upsert |
| D5 | Só Movimentos `EstaAtivo` entram; objetivo inativo não filtra |
| D6 | Papéis distintos por `Emissor.Nome`, comparação por nome (collation do banco) |
| D7 | Data editável (padrão hoje), não futura; valor maior que zero |
| D8 | Recalcula só se a data informada for a mais recente do papel; senão grava só o histórico |
| D9 | `DataAtualizacaoValorLiquido` = hoje (data da atualização, UTC), não a data da cotação |
| D10 | Movimentos com flag ficam fora de "Manutenção Valor Líquido" e da Distribuição |
| D11 | Lista da tela de cotação mostra papel, último valor, data da última cotação e campos de valor e data |
| D12 | Controllers novos: `CotacoesController` e `AtualizacaoPorCotacaoController` |
| D13 | Listagem do CRUD paginada (12 por página, ADR 0008), filtro opcional por papel; série do gráfico sem paginação |
| D14 | Gráfico com Chart.js via LibMan, eixo de categorias, script em arquivo estático |
| D15 | `CancellationToken` **obrigatório** nas novas assinaturas |
| D16 | Antes da feature, saneamento do `IdentityRole` (Passo 1) |

## 3. Atenção antes de começar

1. **Sobrecarga, nunca parâmetro opcional** ao acrescentar token a métodos existentes usados em `Setup/Verify` do Moq (CS0854). Os métodos **novos** já nascem com token obrigatório.
2. **Multi-tenant:** `UserId` aplicado em todo caminho de `Cotacao` e de `Movimento`.
3. **Sem FK para Emissor:** nenhum `Include`, `HasOne` nem navegação entre `Cotacao` e `Emissor`/`User`.
4. **Dinheiro em `decimal`.** Nenhum `double`.
5. **A migration é gerada pelo mantenedor.** O dev prepara o código e informa o comando.
6. **Os testes de Persistence usam InMemory:** não validam tradução SQL nem collation. A validação manual contra MariaDB (parte 3) é obrigatória.
7. **Dependências entre camadas:** Service → Application → Persistence → Domain.

## 4. Ordem de implementação

Cada passo termina com `dotnet build` e `dotnet test` verdes.

| # | Passo | Projeto | Parte |
|---|---|---|---|
| 0 | ADR 0016 como "Proposta" | Docs | 1 |
| 1 | Saneamento do `IdentityRole` | Persistence | 1 |
| 2 | Domain | Domain | 1 |
| 3 | Persistence (mapeamentos, repositórios) e migration | Persistence | 1 |
| 4 | Application | Application | 2 |
| 5 | Service (Api) | Service | 2 |
| 6 | Presentation (Web), gráfico e menu | Presentation | 3 |
| 7 | Documentação final e fechamento | Docs | 3 |

---

## Passo 0: ADR 0016 como "Proposta"

Criar `Docs/adr/0016-atualizacao-do-valor-liquido-por-cotacao.md` com o texto entregue junto (Status: Proposta, 2026-10-17). Mudar para "Aceita" no Passo 7.

**Critério de pronto:** arquivo criado no padrão dos ADRs anteriores.

---

## Passo 1: Saneamento do `IdentityRole` (pré-requisito)

### 1.1 Diagnóstico (a confirmar no código)

- `VaultDbContext` chama `ApplyConfigurationsFromAssembly(typeof(VaultDbContext).Assembly)`, que também aplica `RoleConfiguration`. Resultado: `IdentityRole` virou entidade (e tabela) do contexto de **domínio**, o que gera o ruído de `DeleteData`/`InsertData` em toda migration do `VaultDb`.
- `RoleConfiguration` está com o `HasData` **comentado**, mas o snapshot do contexto de **identidade** ainda tem o seed do Administrator. A próxima migration de identidade emitiria `DeleteData` da role e, por cascata, apagaria vínculos em `AspNetUserRoles`.
- Antes de começar, o dev confirma que nada lê a tabela `IdentityRole` do banco de domínio (a identidade usa `AspNetRoles`).

### 1.2 O que fazer

- `VaultDbContext`: aplicar as configurações **excluindo** `RoleConfiguration` (sobrecarga de `ApplyConfigurationsFromAssembly` com predicado de tipo).
- `RoleConfiguration`: **restaurar** o `HasData` (descomentar), mantendo as constantes de Id e ConcurrencyStamp. O `VaultIdentityDbContext` continua aplicando-a explicitamente.
- Migration `RemocaoIdentityRoleDoDominio` no `VaultDb` (gerada pelo mantenedor): deve conter **somente** `DropTable("IdentityRole")` no `Up` e a recriação (tabela e seed fixo) no `Down`. Revisar o arquivo gerado.
- Nenhuma migration nova de identidade: o modelo deve voltar a coincidir com o snapshot.

### 1.3 Testes (`Persistence.Test`)

- `RoleConfigurationTests`: reativar o teste do seed (hoje comentado) para o contexto de identidade; se ele falhar por causa do provider InMemory, registrar o motivo e substituir por verificação do `IDesignTimeModel`.
- Novo teste: o modelo de `VaultDbContext` **não** contém `IdentityRole`.

### 1.4 Como validar

- `dotnet test Persistence.Test`.
- `dotnet ef migrations has-pending-model-changes` para `VaultIdentityDbContext`: sem mudanças pendentes.
- Aplicar a migration local e conferir `SHOW TABLES;` (sem `IdentityRole`) e que `AspNetRoles` mantém o Administrator.

**Critério de pronto:** migration revisada, aplicada e revertida localmente; identidade sem mudanças pendentes.

---

## Passo 2: Domain

### 2.1 `Movimento`

- Nova propriedade `bool AtualizaPorCotacao { get; set; }`.
- Novo método `AplicarCotacao(decimal cotacao, DateTime hoje)`:
  - `Quantidade` nula ou menor ou igual a zero: `InvalidOperationException` com mensagem em português (erro de programação; a regra de entrada é validada na Application);
  - `ValorLiquidoAtual = decimal.Round(Quantidade.Value * cotacao, 2, MidpointRounding.AwayFromZero)`;
  - `DataAtualizacaoValorLiquido = hoje.Date`.
- Não altera nenhum outro campo.

### 2.2 `Cotacao`

Pasta `Domain/Cotacoes/`, no estilo de `Corretora` (herda `Entity`): `Guid UserId`, `string Papel` (padrão vazio), `decimal Valor` e `DateTime Data`. Sem coleções e sem navegação.

### 2.3 Testes (`Domain.Test`)

`Movimentos/MovimentoAplicarCotacaoTests.cs`:

- 20 × 40,00 = 800,00 e 40 × 40,00 = 1.600,00 (exemplo do pedido).
- Quantidade fracionária e cotação com 4 casas: arredondamento a 2 casas (`AwayFromZero`; usar um caso de ponto médio).
- Data gravada sem hora (`hoje` com hora é truncado).
- Quantidade nula, zero e negativa: `InvalidOperationException`.
- Outros campos (`ValorAporte`, `Observacao`) intactos.

**Critério de pronto:** testes verdes, sem dependência nova.

---

## Passo 3: Persistence

### 3.1 Mapeamentos

- `MovimentoMapping`: `AtualizaPorCotacao` obrigatória. **Sem** `HasDefaultValue` (o EF já emite `defaultValue: false` na migration de uma coluna `bool` obrigatória).
- Novo `CotacaoMapping`:
  - tabela `Cotacoes`; chave `Id` com coluna `CotacaoId`;
  - `Papel` obrigatório, `HasMaxLength(60)`;
  - `Valor` obrigatório, `HasPrecision(18, 4)`;
  - `Data` obrigatória, `HasColumnType("date")`;
  - `UserId` obrigatório (sem FK);
  - índice **único** `(UserId, Papel, Data)` com o nome `IX_Cotacoes_UserId_Papel_Data`, com comentário no estilo dos demais referenciando o ADR 0016.
- `VaultDbContext`: `DbSet<Cotacao> Cotacoes`.

### 3.2 Repositório de cotações

Pasta `Persistence/Cotacoes/`: `ICotacaoRepository` e `CotacaoRepository` (herda `BaseRepository<Cotacao>`), todos os métodos novos com `CancellationToken` **obrigatório** e `ownerId` sempre aplicado:

- `Task<PagedList<Cotacao>> GetPagedAsync(Guid ownerId, string? papel, int page, int pageSize, CancellationToken ct)`: filtro opcional por papel; ordem `Data DESC, Papel ASC, Id ASC` (determinística); contagem refletindo o filtro; `ToPagedListAsync`. Sem tracking.
- `Task<Cotacao?> GetAsync(Guid ownerId, Guid cotacaoId, bool trackChanges, CancellationToken ct)`.
- `Task<Cotacao?> GetPorPapelEDataAsync(Guid ownerId, string papel, DateTime data, bool trackChanges, CancellationToken ct)`: base do upsert.
- `Task<List<string>> GetPapeisAsync(Guid ownerId, CancellationToken ct)`: papéis distintos, ordem ascendente.
- `Task<List<Cotacao>> GetSerieAsync(Guid ownerId, string papel, CancellationToken ct)`: todas as cotações do papel, `Data` ascendente, sem tracking.
- `Task<List<Cotacao>> GetUltimasPorPapelAsync(Guid ownerId, IReadOnlyCollection<string> papeis, CancellationToken ct)`: a cotação de data mais recente de cada papel da lista. Sugestão: subconsulta correlacionada pela data máxima do papel. **Validar a tradução do provider**; se não traduzir, uma consulta por papel (volume pequeno).
- `Task<DateTime?> GetDataMaisRecenteAsync(Guid ownerId, string papel, CancellationToken ct)`.
- `void Create(Cotacao)`, `void Update(Cotacao)`, `void Delete(Cotacao)` (herdados do padrão do repositório).
- `IRepositoryManager`/`RepositoryManager`: nova propriedade `Cotacao`, com `Lazy` no padrão dos demais.

### 3.3 `IMovimentoRepository` e `MovimentoRepository`

Novos (token obrigatório):

- `Task<List<string>> GetPapeisPorCotacaoAsync(Guid ownerId, CancellationToken ct)`: `Emissor.Nome` distintos dos Movimentos com `EstaAtivo` e `AtualizaPorCotacao`, `Quantidade` não nula, ordem ascendente; projeção, sem carregar entidades.
- `Task<List<Movimento>> GetAtivosPorCotacaoDoPapelAsync(Guid ownerId, string papel, CancellationToken ct)`: **com tracking**, mesmos filtros e `Emissor.Nome == papel`; sem `Include` além do necessário ao filtro.

Alterações em métodos existentes (assinaturas inalteradas, **comportamento muda**):

- `GetDesatualizadosAsync`: acrescentar `!m.AtualizaPorCotacao` ao filtro (D10). Atualizar o comentário e referenciar o ADR 0016.
- `ConsultarAtivosDoTitulo` (usado por `GetAtivosDoTituloAsync` e `GetAtivosDoTituloParaAtualizacaoAsync`): acrescentar `!m.AtualizaPorCotacao`.
- **Limpeza (commit separado, opcional):** remover `GetAtivosPorTituloAsync` e `TituloFiltro`, sem uso confirmado; ajustar `FakeMovimentoRepository`.
- Atualizar `FakeMovimentoRepository` (Application.Test) com os dois métodos novos (`NotSupportedException`) e `FakeRepositoryManager` com a propriedade `Cotacao`.

### 3.4 Migration `MovimentoAtualizaPorCotacaoECotacoes`

Gerada pelo mantenedor **depois** do Passo 1. Conteúdo esperado: `AddColumn` bool (`defaultValue: false`) em `Movimentos`, `CreateTable("Cotacoes")` e `CreateIndex` único. **Nenhum** `DeleteData`/`InsertData` de `IdentityRole` (se aparecer, o Passo 1 não foi concluído). `Down` desfaz tudo.

### 3.5 Testes (`Persistence.Test`)

- `CotacaoMappingTests`: índice único existe, com as colunas na ordem e o nome esperados; `Valor` com precisão (18,4); `Cotacao` sem chave estrangeira e sem navegação; `Movimento` mapeia `AtualizaPorCotacao`.
- `CotacaoRepositoryTests` (helper próprio para criar cotações):
  - `GetPagedAsync`: isolamento por usuário; filtro por papel; ordem `Data DESC, Papel, Id`; contagem filtrada; página 2.
  - `GetPorPapelEDataAsync`: acha o par exato; não acha de outro usuário, outro papel ou outra data; respeita tracking.
  - `GetPapeisAsync`: distintos, ordenados, isolados por usuário.
  - `GetSerieAsync`: só o papel pedido, ordem ascendente, isolada por usuário.
  - `GetUltimasPorPapelAsync`: devolve a mais recente de cada papel; ignora papéis fora da lista e de outro usuário.
  - `GetDataMaisRecenteAsync`: máxima; `null` sem registros.
- `MovimentoRepositoryTests`:
  - `GetPapeisPorCotacaoAsync`: só ativos com flag e quantidade; distintos por nome (dois Emissores com o mesmo nome contam uma vez); isolamento por usuário.
  - `GetAtivosPorCotacaoDoPapelAsync`: filtra por papel, flag e ativo; com tracking (`Unchanged`); isolamento.
  - Regressão: `GetDesatualizadosAsync` e `GetAtivosDoTituloAsync`/`ParaAtualizacaoAsync` **não** devolvem Movimento com flag.
- Ajustar o helper `AdicionarMovimento` apenas se precisar de parâmetro para a flag.

### 3.6 Como validar

- `dotnet test Persistence.Test`; `dotnet build` da solution (a Application ainda não usa os tipos novos).
- Com a migration aplicada: `DESCRIBE Movimentos;`, `DESCRIBE Cotacoes;` e `SHOW INDEX FROM Cotacoes;`. Inserir duas cotações do mesmo papel e data deve falhar no índice único.

**Critério de pronto:** testes verdes, migration revisada, aplicada e revertida localmente, nenhuma FK em `Cotacoes`.

---

## Próximas partes

- **Parte 2:** Application (validação da flag e da quantidade em Create/Update, CRUD de Cotação, query e command de atualização, upsert e recálculo transacional) e Service (controllers e DTOs).
- **Parte 3:** Web (CRUD, tela de atualização, gráfico com Chart.js, menu), documentação, validação ponta a ponta e riscos.
