# Especificação técnica 0019: Encerramento de Movimento (Vault)

- Data: 2026-10-27
- Status: **em elaboração (parte 1 de 4: Passos 0 a 2), decisões D3 e D4 aprovadas em 2026-10-27**
- ADR relacionado: [0020](../adr/0020-encerramento-de-movimento.md)
- Repositório afetado: **Vault** (Domain, Persistence, Application, Service, Presentation/Web). **Com alteração de schema e migration.** Sem alteração no LumiaFoundation.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.
> **Partes:** 1 = Passos 0 a 2 (este arquivo); 2 = Passo 3 (Application); 3 = Passo 4 (Service); 4 = Passos 5 e 6 (Web, validação ponta a ponta e fechamento).

---

## 1. Resumo

O encerramento de um Movimento deixa de ser o desmarcar de `EstaAtivo` e passa a ter **motivo** (`Gasto`, `Reinvestido` ou `Pendente`) e **data própria** (`DataEncerramento`), separada do vencimento contratado (ADR 0020). `EstaAtivo` continua como coluna e passa a ser mantido pelo Domain, com a invariante `EstaAtivo = true` se e somente se `MotivoEncerramento` é nulo.

Esta parte entrega a base: o ADR como "Proposta", o modelo de domínio com os métodos `Encerrar` e `Reativar`, o mapeamento e a migration (incluindo a conversão dos Movimentos inativos existentes).

## 2. Decisões aprovadas

| Item | Decisão |
| --- | --- |
| D1 | Duas colunas anuláveis em `Movimentos`: `MotivoEncerramento` (enum gravado como texto, `varchar(20)`) e `DataEncerramento` (`date`) |
| D2 | `EstaAtivo` permanece como coluna; invariante `EstaAtivo = true` ⇔ `MotivoEncerramento` nulo, garantida por `Encerrar` e `Reativar` |
| D3 | **Opção A (aprovada):** `EstaAtivo` mantém o `set` público; o código de produção deixa de escrevê-lo e usa só `Encerrar` e `Reativar` (ver seção 3) |
| D4 | **Aprovado:** `CHECK` no banco para a invariante (ver Passo 2.4) |
| D5 | `DataEncerramento` obrigatória com motivo, não futura (data UTC de hoje, recebida como parâmetro) e não anterior à `DataInvestimento` |
| D6 | Reativar limpa motivo e data; `ValorLiquidoAtual` fica **congelado** no encerramento e não é tocado por nenhum dos dois métodos |
| D7 | O Domain **não usa relógio**: `Encerrar` recebe "hoje" como parâmetro, como `AplicarCotacao` |
| D8 | Migration gerada pelo mantenedor: `AddColumn` ×2 e `UPDATE` dos inativos para `Pendente`, com a data vinda de `DataVencimento` ou `DataAtualizacaoValorLiquido` |
| D9 | Sem índice novo (gatilho: `EXPLAIN` em volume realista) |
| D10 | `CancellationToken` **obrigatório** em toda assinatura nova (a partir da parte 2) |

## 3. Atenção antes de começar

1. **Ponto D3 (decidido: opção A).** Hoje `Movimento.EstaAtivo` tem `set` público e **48 linhas de teste** (mais os modelos de criação e edição) o atribuem direto. Opções:
   - **A (escolhida):** manter o `set` público de `EstaAtivo`; as duas colunas novas já nascem com `private set`. O código de produção deixa de escrever `EstaAtivo` (a Application passa a usar `Encerrar` e `Reativar`, partes 2 e 3) e os testes antigos continuam compilando. Risco: nada impede, em código novo, uma atribuição direta que quebre a invariante.
   - **B (não adotada; fica como evolução possível):** `private set` também em `EstaAtivo`, forçando todos os caminhos pelos métodos. É o desenho mais seguro, mas obriga ajustar as 48 linhas de teste e a criação de Movimentos (um método de fábrica ou o `Encerrar` logo após o `new`). Pode ser feito num passo posterior, isolado, sem retrabalho das demais partes.
   - Em qualquer opção, o D4 (`CHECK` no banco) cobre o risco da opção A.
2. **O Domain não referencia nenhuma outra camada** e não lê relógio.
3. **Dinheiro em `decimal`.** Esta feature não cria nem altera valores monetários.
4. **A migration é gerada pelo mantenedor.** O dev prepara o código e informa o comando.
5. **Os testes de Persistence usam InMemory:** não validam o tipo de coluna, o `UPDATE` da migration nem o `CHECK`. A validação manual contra MariaDB (Passo 2.6) é obrigatória.
6. **Dependências entre camadas:** Service → Application → Persistence → Domain.
7. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo (parte 4).
8. **A mudança de contrato do `PUT api/movimentos/{id}` só chega na parte 3.** Até lá, o `UpdateMovimentoCommand` continua gravando `EstaAtivo` direto; a Application e o Service são ajustados juntos, para a solution compilar e os testes passarem em cada passo.

## 4. Ordem de implementação e entrega desta spec

Cada passo termina com `dotnet build` e `dotnet test` verdes antes do seguinte.

| # | Passo | Projeto | Entrega |
| --- | --- | --- | --- |
| 0 | ADR 0020 como "Proposta" | Docs | **parte 1 (este arquivo)** |
| 1 | `MotivoEncerramento`, `Encerrar` e `Reativar` | Domain | **parte 1 (este arquivo)** |
| 2 | Mapeamento e migration | Persistence | **parte 1 (este arquivo)** |
| 3 | Comandos de encerramento, reativação e criação; modelos | Application | parte 2 |
| 4 | Endpoints, DTOs e mudança do `PUT` | Service (Api) | parte 3 |
| 5 | Página de encerramento, listagem e cliente da Api | Presentation (Web) | parte 4 |
| 6 | Validação ponta a ponta, documentação final e fechamento do ADR | Docs | parte 4 |

---

## Passo 0: ADR 0020 como "Proposta"

- Criar `Docs/adr/0020-encerramento-de-movimento.md` com o texto entregue anteriormente, **Status: Proposta (2026-10-26)**. A numeração já está acertada: o 0019 é o Fluxo Mensal e a projeção do Objetivo passa a ser o **ADR 0021**.
- Acrescentar uma frase à seção **Persistência** do ADR: *"Na conversão, a data de encerramento é limitada ao intervalo entre a `DataInvestimento` e hoje (UTC), para que nenhum Movimento migrado fique com encerramento futuro ou anterior ao investimento."* (refinamento definido nesta spec, Passo 2.3.)
- Acrescentar à seção **Modelo** uma frase sobre o `CHECK` (`CK_Movimentos_Encerramento`) que protege a invariante no banco (D4 aprovado).

**Critério de pronto:** arquivo criado no padrão dos ADRs anteriores, status "Proposta", referências ao 0021 corretas.

---

## Passo 1: Domain

Pasta: `Domain/Movimentos/`. Sem pacote novo.

### 1.1 `MotivoEncerramento`

- Enum em `Domain/Movimentos/MotivoEncerramento.cs`, com os valores **`Gasto`, `Reinvestido` e `Pendente`** (nesta ordem, sem valores numéricos explícitos).
- **Os nomes são contrato de persistência:** a coluna guarda o texto. Renomear um valor exige migration de dados. Comentário curto no enum explicando isso, e o significado de cada valor (ADR 0020).
- A propriedade da entidade tem o mesmo nome do tipo (`MotivoEncerramento? MotivoEncerramento`), o que o C# permite. Se isso incomodar na leitura, o mantenedor decide o nome do tipo; a spec segue o ADR.

### 1.2 `Movimento`

Novas propriedades (ambas com **`private set`**, pois só os métodos abaixo as alteram; o EF Core mapeia propriedades com setter privado por convenção):

- `MotivoEncerramento? MotivoEncerramento { get; private set; }`
- `DateTime? DataEncerramento { get; private set; }`

Novo método **`Encerrar(MotivoEncerramento motivo, DateTime data, DateTime hoje)`**:

1. `motivo` fora do enum (`Enum.IsDefined` falso): `ArgumentOutOfRangeException`.
2. `data.Date` maior que `hoje.Date`: `InvalidOperationException` com mensagem em português ("A data de encerramento não pode ser futura.").
3. `data.Date` menor que `DataInvestimento.Date`: `InvalidOperationException` ("A data de encerramento não pode ser anterior à data do investimento.").
4. Só depois das três validações, e **sem gravar nada antes delas**: `EstaAtivo = false`, `MotivoEncerramento = motivo`, `DataEncerramento = data.Date`.
5. Chamar `Encerrar` num Movimento **já encerrado reclassifica**: troca motivo e data. Não é erro.
6. Não altera nenhum outro campo. Em particular, `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` ficam como estavam (valor congelado, D6).

Novo método **`Reativar()`**:

- `EstaAtivo = true`, `MotivoEncerramento = null`, `DataEncerramento = null`.
- Idempotente: num Movimento já ativo não faz nada nem falha.
- Não altera nenhum outro campo.

Por que lançar exceção no Domain e validar de novo na Application: o Domain protege a invariante contra erro de programação (mesmo padrão do `AplicarCotacao`); a Application valida antes, para devolver erro de validação amigável ao usuário (parte 2), sem depender de capturar exceção de domínio.

Por que `hoje` como parâmetro: o Domain não tem relógio (ADR 0011). A Application lê `IDateTimeProvider` e passa a data.

Por que `data.Date`: a coluna é `date`; guardar só a data evita que uma hora residual vire diferença na ordenação ou no teste.

### 1.3 Testes (`Domain.Test`)

Criar `Movimentos/MovimentoEncerramentoTests.cs`, no estilo do `MovimentoAplicarCotacaoTests` (datas fixas, sem relógio), e `MotivoEncerramentoTests.cs`.

`MovimentoEncerramentoTests`:

| Cenário | Esperado |
| --- | --- |
| `Encerrar` com cada um dos 3 motivos (`Theory`) | `EstaAtivo = false`, motivo e `DataEncerramento` gravados |
| Data com hora (`23:59:59`) | `DataEncerramento` gravada sem hora |
| Data igual a `hoje` | aceita (fronteira superior) |
| Data igual à `DataInvestimento` | aceita (fronteira inferior) |
| Data um dia depois de `hoje` | `InvalidOperationException`; **estado anterior intacto** (continua ativo, motivo e data nulos) |
| Data um dia antes da `DataInvestimento` | `InvalidOperationException`; estado intacto |
| Motivo fora do enum (`(MotivoEncerramento)99`) | `ArgumentOutOfRangeException`; estado intacto |
| Reclassificar (`Pendente` → `Gasto`, com data nova) | continua inativo; motivo e data novos |
| Reclassificação com data inválida | exceção; motivo e data **anteriores** preservados |
| Outros campos | `ValorAporte`, `ValorLiquidoAtual`, `DataAtualizacaoValorLiquido`, `Observacao` e `EhReinvestimento` intactos após `Encerrar` e após `Reativar` |
| `Reativar` num encerrado | `EstaAtivo = true`, motivo e data nulos |
| `Reativar` num ativo | sem efeito e sem exceção |
| Ciclo `Encerrar` → `Reativar` → `Encerrar` | segundo encerramento aceito, com os novos valores |
| **Invariante** (`Theory` com sequências de operações) | após cada operação, `EstaAtivo == (MotivoEncerramento is null)` e `(MotivoEncerramento is null) == (DataEncerramento is null)` |

`MotivoEncerramentoTests`:

- O conjunto de nomes do enum é exatamente `{ "Gasto", "Reinvestido", "Pendente" }`. O teste existe porque os nomes são gravados no banco: se falhar, alguém renomeou ou acrescentou um valor sem migration de dados.

**Como validar:** `dotnet test Domain.Test --filter "FullyQualifiedName~Encerramento"`.

**Critério de pronto:** testes verdes, sem dependência nova, sem relógio e sem I/O no Domain, invariante coberta nas sequências de operações.

---

## Passo 2: Persistence

### 2.1 Mapeamento (`MovimentoMapping`)

- `MotivoEncerramento`: **anulável**, `HasConversion<string>()` e `HasMaxLength(20)` (o provider cria `varchar(20)`).
- `DataEncerramento`: **anulável**, `HasColumnType("date")`.
- Comentário no estilo dos demais, referenciando o ADR 0020 e explicando que os nomes do enum são contrato de persistência.
- **Nenhum índice novo** (D9). Os índices `(UserId, EstaAtivo, ...)` seguem como estão; os testes existentes que os verificam continuam valendo sem alteração.
- `EstaAtivo` continua obrigatória e mapeada como hoje.

### 2.2 Repositórios

Nenhuma alteração nesta parte. `GetAsync(ownerId, movimentoId, trackChanges, ct)` já serve aos comandos da parte 2. Registrar aqui apenas que as colunas novas **não** entram em projeções leves existentes (Fluxo Mensal, Rentabilidade, Distribuição); o uso do motivo nas consultas vem com o ADR 0021.

### 2.3 Migration `MovimentoEncerramento`

Gerada pelo mantenedor, nesta pasta e neste padrão:

```bash
dotnet ef migrations add MovimentoEncerramento \
  --project Persistence/Persistence.csproj \
  --startup-project Persistence/Persistence.csproj \
  --context VaultDbContext \
  --output-dir Migrations/VaultDb
```

Conteúdo esperado do arquivo gerado, **a revisar antes de aplicar**:

- `Up`: `AddColumn` de `MotivoEncerramento` (`varchar(20)`, anulável, com `CharSet utf8mb4`), `AddColumn` de `DataEncerramento` (`date`, anulável) e `AddCheckConstraint` do `CK_Movimentos_Encerramento` (D4). Nada mais. Se aparecer `DeleteData`/`InsertData`, `DropIndex` ou alteração de outra coluna, o modelo está diferente do snapshot: parar e investigar.
- **Ordem obrigatória:** o EF gera o `AddCheckConstraint` logo depois dos `AddColumn`, mas o `UPDATE` precisa rodar **antes** dele, senão o `CHECK` falha nos Movimentos inativos existentes. Então: **acrescentar à mão** um `migrationBuilder.Sql(...)` com o `UPDATE` de dados **entre** os `AddColumn` e o `AddCheckConstraint`. O `UPDATE` roda **uma vez**, no momento da migration:
  - alvo: `Movimentos` com `EstaAtivo = 0`;
  - `MotivoEncerramento = 'Pendente'` (o motivo mais conservador: o dinheiro não some do saldo até o usuário reclassificar);
  - `DataEncerramento` = a primeira data disponível entre `DataVencimento` e `DataAtualizacaoValorLiquido` (`COALESCE`, convertendo para `DATE`), **limitada ao intervalo** entre `DATE(DataInvestimento)` e a data UTC de hoje (`UTC_DATE()`), com `GREATEST` e `LEAST`.
  - Motivo do limite: um Movimento resgatado antes do vencimento pode ter mantido o vencimento original (futuro) em `DataVencimento`, e a data do encerramento nunca pode ser futura nem anterior ao investimento (D5).
  - Movimentos com `EstaAtivo = 1` não são tocados e ficam com as duas colunas nulas.
- `Down`: remover o `CHECK` e depois as duas colunas, na ordem inversa. **Perde o motivo e a data** (nenhuma outra tabela depende deles); registrar isso num comentário no `Down`.
- O `UPDATE` não precisa de `Down` próprio: as colunas são removidas.

### 2.4 `CHECK` da invariante no banco (D4, aprovado)

**Decisão:** `HasCheckConstraint("CK_Movimentos_Encerramento", ...)` no mapeamento, com a regra: ativo tem motivo e data nulos; inativo tem motivo e data preenchidos. Entra na mesma migration, **depois** do `UPDATE`.

| | Prós | Contras |
| --- | --- | --- |
| **Incluir** (escolhido) | garante a invariante mesmo contra bug futuro, SQL manual ou a opção A do D3; erro explícito em vez de dado inconsistente | mais um objeto de schema para manter; manutenção manual por SQL fica mais restrita; o InMemory dos testes não o aplica |
| **Não incluir** | schema mais enxuto; consistência apenas pelo Domain | a invariante depende só de disciplina de código |

O `CHECK` exige que os Movimentos inativos já tenham motivo e data, o que o `UPDATE` acima garante; por isso ele entra **depois** do `UPDATE`.

### 2.5 Testes (`Persistence.Test`)

`MovimentoMappingTests` (acrescentar):

- `MotivoEncerramento`: anulável, `MaxLength` 20 e conversor para `string` configurado.
- `DataEncerramento`: anulável e com tipo de coluna `date` (conferir pela anotação do modelo, sem depender do provider).
- **Nenhum índice novo**: os índices do `Movimento` continuam exatamente os de hoje (nenhum contém `MotivoEncerramento` ou `DataEncerramento`).
- O `CHECK` `CK_Movimentos_Encerramento` existe no modelo.

`MovimentoRepositoryTests` (acrescentar):

- **Ida e volta:** gravar um Movimento encerrado (usando `Encerrar` na montagem do teste) e ler de volta com `GetAsync`: `EstaAtivo`, motivo e `DataEncerramento` preservados.
- **Ida e volta de um Movimento ativo:** as duas colunas voltam nulas.
- **Reativação persistida:** carregar com tracking, `Reativar`, salvar e reler: motivo e data nulos, `EstaAtivo` verdadeiro.
- **Isolamento por usuário:** nenhum método novo de repositório existe nesta parte, então basta a regressão dos testes atuais.

### 2.6 Como validar

- `dotnet build` da solution e `dotnet test Persistence.Test` (e `Domain.Test`).
- Gerar a migration (comando do 2.3) e **abrir o arquivo**: confirmar o conteúdo esperado e acrescentar o `UPDATE` e o `CHECK`.
- Antes de aplicar, no banco local com dados reais, anotar: `SELECT COUNT(*) FROM Movimentos WHERE EstaAtivo = 0;`
- Aplicar com `dotnet ef database update` (mesmos `--project`, `--startup-project` e `--context` do comando acima; as variáveis de ambiente de conexão são as do `Docs/MariaDb.md`).
- Conferir:
  - `DESCRIBE Movimentos;`: `MotivoEncerramento` como `varchar(20)` e `DataEncerramento` como `date`, ambas aceitando `NULL`.
  - Todos os inativos têm `Pendente` e uma data, e a contagem bate com a anotada: `SELECT COUNT(*) FROM Movimentos WHERE EstaAtivo = 0 AND MotivoEncerramento = 'Pendente' AND DataEncerramento IS NOT NULL;`
  - Nenhum ativo foi tocado: `SELECT COUNT(*) FROM Movimentos WHERE EstaAtivo = 1 AND (MotivoEncerramento IS NOT NULL OR DataEncerramento IS NOT NULL);` deve dar **0**.
  - Invariante: `SELECT COUNT(*) FROM Movimentos WHERE (EstaAtivo = 1) <> (MotivoEncerramento IS NULL);` deve dar **0**.
  - Limites de data: `SELECT COUNT(*) FROM Movimentos WHERE DataEncerramento > UTC_DATE() OR DataEncerramento < DATE(DataInvestimento);` deve dar **0**.
  - Amostra legível: `SELECT MovimentoId, EstaAtivo, MotivoEncerramento, DataEncerramento, DataVencimento FROM Movimentos WHERE EstaAtivo = 0 LIMIT 10;` (o motivo aparece como **texto**).
- Reverter para a migration anterior (`dotnet ef database update 20261021202938_ObjetivoAporteMensalPrecisao ...`) e aplicar de novo: o ciclo precisa funcionar nos dois sentidos.
- Tentar, por SQL, `UPDATE Movimentos SET EstaAtivo = 1 WHERE EstaAtivo = 0 LIMIT 1;` deve **falhar** na constraint (executar num banco descartável).

**Critério de pronto:** testes verdes; migration revisada, aplicada, revertida e reaplicada localmente; consultas de conferência com os resultados esperados; nenhum índice novo.

---

## Partes 2 a 4 (a detalhar nas próximas entregas)

Escopo já definido no ADR 0020, para orientar:

- **Parte 2, Application:**
  - `MovimentoModel` expõe `MotivoEncerramento` e `DataEncerramento`.
  - Novos comandos `EncerrarMovimentoCommand` (encerra e reclassifica; valida motivo e data antes de chamar o Domain, com `IDateTimeProvider`) e `ReativarMovimentoCommand`; `UserId` aplicado no repositório, Movimento de outro usuário como "não encontrado".
  - `CreateMovimentoCommand` aceita motivo e data opcionais (sem motivo, nasce ativo) e `UpdateMovimentoCommand` deixa de gravar `EstaAtivo`.
  - Todos com `CancellationToken` obrigatório (ADR 0017).
- **Parte 3, Service:** `PUT api/movimentos/{id}/encerramento` (corpo `{ motivo, data }`, 204), `DELETE api/movimentos/{id}/encerramento` (204), `POST` com motivo e data opcionais, `PUT api/movimentos/{id}` sem `EstaAtivo`, `MovimentoDto` com os dois campos novos e `[RequestValidation]`.
- **Parte 4, Presentation e fechamento:** cliente da Api, remoção do checkbox "Ativo" da edição, página `/Movimentos/Encerrar/{id}` (motivo, data com padrão hoje, reclassificar e "Reativar"), motivo e data no detalhe da listagem, documentação final e ADR 0020 para **Aceita**.

## Fora de escopo

- Qualquer uso do motivo em consultas, dashboards ou na projeção do Objetivo (ADR 0021).
- Vínculo estruturado entre Movimento encerrado e Movimento de reinvestimento (`MovimentoOrigemId`, descartado no ADR 0020).
- Campo `ValorResgatado` e rentabilidade de Movimentos encerrados.
- Índice por motivo.
- Alteração de LumiaFoundation.
