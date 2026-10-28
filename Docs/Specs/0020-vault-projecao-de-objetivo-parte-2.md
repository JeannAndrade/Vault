# Especificação técnica 0020: Projeção do Objetivo (Vault), parte 2

- Data: 2026-10-27
- Status: **em elaboração (parte 2 de 4: Passo 2, Application)**
- ADR relacionado: [0021](../adr/0021-projecao-de-objetivo.md)
- Parte anterior: [0020-vault-projecao-de-objetivo.md](0020-vault-projecao-de-objetivo.md) (Passos 0 e 1: ADR e Domain)
- Repositório afetado nesta parte: **Application** (e o ajuste de testes que usam o construtor da query). Sem schema, sem migration, sem dependência nova, sem consulta nova.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Ajustes na parte 1

Dois acréscimos feitos depois de ler o código da Application (já aplicados no arquivo da parte 1):

- Novo teste do calculador com **datas padrão** (`DateTime.MinValue`): vários testes existentes da query criam `Movimento` sem preencher datas, então o calculador não pode lançar exceção com esses valores.
- A nota sobre `Meta` zero ficou mais precisa: a criação já exige `Meta` de pelo menos 1, então o `PercentualMeta` só precisa de uma proteção defensiva para dado legado.

## 2. Resumo

A `GetResumoObjetivosListQuery` passa a ler "hoje" do `IDateTimeProvider` (uma vez por execução) e a montar, para cada Objetivo ativo, o resumo com o **saldo único** (ativos e `Pendente`) e a **projeção** do Domain. **Nenhuma consulta nova** ao banco: a query já traz os Movimentos de todos os Objetivos, inclusive os encerrados, pelo `Include` do `ObjetivoRepository` (leitura sem rastreamento).

## 3. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D12 | O Application ganha modelos próprios para a projeção (`ProjecaoObjetivoModel` e `CenarioProjecaoModel`), mapeados a partir dos records do Domain, como os demais modelos. O enum `SituacaoProjecao` do Domain é **reutilizado** (sem cópia), como o `MotivoEncerramento` na spec 0019 |
| D13 | `ResumoObjetivoModel.FromDomain` passa a receber `hoje`: `FromDomain(Objetivo objetivo, DateTime hoje)`. Continua **estático e puro** |
| D14 | `QtdeMovimentos`, `TotalAportado`, `TotalLiquido` e `PercentualMeta` passam a usar `Movimento.CompoeSaldoDoObjetivo` (ativos e `Pendente`), e **só** essa propriedade define o conjunto |
| D15 | O mapeamento de `Movimento` para `MovimentoParaProjecao` fica no próprio modelo (método privado); `CompoeSaldo` vem de `CompoeSaldoDoObjetivo`, sem repetir a condição |
| D16 | A query recebe `IDateTimeProvider` (já registrado na Api desde o ADR 0011) e lê o relógio **uma vez**, passando a mesma data a todos os Objetivos |
| D17 | `PercentualMeta` com `Meta <= 0` vale **0** (sem divisão por zero); a projeção correspondente é "sem previsão" |
| D18 | O saldo do `ResultadoProjecao` **não** é copiado para o modelo: o saldo é o `TotalLiquido`. Um teste garante que os dois números coincidem |
| D19 | `CancellationToken` já é obrigatório na query e **não muda**; o cálculo é síncrono e sem I/O, então não recebe token. O tratamento de erro (log e relançamento, exceto cancelamento) fica como está |
| D20 | Apenas Objetivos **ativos** são calculados (o filtro de hoje continua antes do mapeamento) |

## 4. Atenção antes de começar

1. **Ordem de uso do relógio:** `IDateTimeProvider.GetDateTime()` é chamado **uma única vez** por `ExecuteAsync`, mesmo com vários Objetivos. Nada de `DateTime.UtcNow`.
2. **Multi-tenant:** nada muda. O `ownerId` já entra em `GetAllWithRelatedEntitiesAsync`, e a projeção só enxerga os Movimentos que a consulta devolveu.
3. **Sem consulta nova e sem `Include` novo.** As colunas de encerramento (spec 0019) chegam com a entidade.
4. **Fixtures legados:** vários testes atuais criam `Movimento { EstaAtivo = false }` **sem** motivo (estado que a invariante do ADR 0020 não produz em dados reais). Para `CompoeSaldoDoObjetivo`, esse Movimento **não** compõe o saldo (não está ativo e o motivo não é `Pendente`), então os resultados desses testes **não mudam**. Os testes **novos** devem montar encerrados com `Encerrar(...)`, nunca com `EstaAtivo = false` solto.
5. **Registro de DI por convenção:** `GetResumoObjetivosListQuery` ganha uma dependência, que já existe no contêiner. O `RegistroDeServicosTests` (Service.Test) já registra `IDateTimeProvider` como mock; nada a mudar lá, só conferir que ele continua verde.
6. **A mudança de significado dos campos do resumo chega à Api e ao Web nas partes 3 e 4:** entre elas, o Web mostra `Total Líquido` e `% Meta` **com** os `Pendente` mas ainda sem a coluna "Previsão". Não publicar nesse intervalo.
7. **Sem dependência nova.**

## 5. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 2.1 | Modelos da projeção | `ProjecaoObjetivoModel`, `CenarioProjecaoModel` e testes de mapeamento |
| 2.2 | `ResumoObjetivoModel` com saldo único e projeção | modelo e testes |
| 2.3 | `GetResumoObjetivosListQuery` com relógio | query e ajuste dos testes existentes |
| 2.4 | Testes novos da query | cenários ponta a ponta na Application |

---

## Passo 2: Application

Pasta `Application/Objetivos/Queries/GetResumoObjetivosList/` (onde já está o `ResumoObjetivoModel`).

### 2.1 Modelos da projeção

**`CenarioProjecaoModel`** (record):

- `SituacaoProjecao Situacao`, `int? Meses`, `DateTime? DataPrevista`.
- `FromDomain(CenarioProjecao)` estático.

**`ProjecaoObjetivoModel`** (record):

- `decimal SaldoPendente`;
- `decimal? AporteRealizadoMensal` e `int MesesObservados`;
- `decimal AportePlanejadoMensal`;
- `double TaxaAnualEstimada` e `double TaxaMensalEstimada`;
- `bool BaixaConfianca`;
- `CenarioProjecaoModel Conservador`, `Realista` e `Planejado`.
- `FromDomain(ResultadoProjecao)` estático.
- **`Vazia`** estático: uma projeção neutra (zeros, sem aporte realizado, `BaixaConfianca` verdadeiro e os três cenários em `SemPrevisao`). É o valor **padrão** da propriedade `Projecao` do `ResumoObjetivoModel`, para que um modelo criado sem projeção (como nos testes atuais do Service) nunca tenha `Projecao` nula.
- Sem o campo de saldo (D18).

Testes: o mapeamento leva cada campo do `ResultadoProjecao` ao modelo, incluindo `null` em `AporteRealizadoMensal`, e cada situação com e sem meses e data; `Vazia` traz os três cenários em `SemPrevisao`; um `ResumoObjetivoModel` criado sem `Projecao` tem `Projecao` igual a `Vazia`.

### 2.2 `ResumoObjetivoModel`

- Nova propriedade `ProjecaoObjetivoModel Projecao`, **nunca nula**: o padrão é `ProjecaoObjetivoModel.Vazia`, e `FromDomain` sempre a preenche com o resultado do cálculo.
- `FromDomain(Objetivo objetivo, DateTime hoje)`:
  1. `doSaldo` = Movimentos do Objetivo com `CompoeSaldoDoObjetivo`.
  2. `QtdeMovimentos` = quantidade de `doSaldo`; `TotalAportado` = soma de `ValorAporte`; `TotalLiquido` = soma de `ValorLiquidoAtual`.
  3. `PercentualMeta` = `TotalLiquido / Meta * 100` quando `Meta > 0`; senão **0** (D17).
  4. Mapeia **todos** os Movimentos do Objetivo (não só `doSaldo`: o aporte realizado também usa os encerrados) para `MovimentoParaProjecao`, com `CompoeSaldo` vindo de `CompoeSaldoDoObjetivo` (D15).
  5. Chama `ProjecaoObjetivoCalculador.Calcular(movimentos, objetivo.Meta, objetivo.AporteMensal, hoje)` e mapeia o resultado em `Projecao`.
- Os campos atuais mantêm **nome e tipo**; só o conjunto de Movimentos mudou (ADR 0021).

### 2.3 `GetResumoObjetivosListQuery`

- Construtor: `IRepositoryManager`, `ILoggerManager` e **`IDateTimeProvider`** (D16).
- `ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)`, assinatura **inalterada**:
  1. Busca os Objetivos como hoje (`GetAllWithRelatedEntitiesAsync(ownerId, cancellationToken)`).
  2. Lê `hoje` **uma vez** (`GetDateTime().Date`).
  3. Filtra os Objetivos ativos e mapeia com `ResumoObjetivoModel.FromDomain(objetivo, hoje)`.
- O bloco `try/catch` com log e relançamento (exceto `OperationCanceledException`) permanece como está.

### 2.4 Ajuste dos testes existentes

Os cinco testes de `GetResumoObjetivosListQueryTests` constroem a query com dois argumentos. Passam a receber um `Mock<IDateTimeProvider>` com data fixa (por exemplo, `2026-10-27` em UTC). Resultados esperados **inalterados** (vide atenção 4): o primeiro teste continua dando 2 movimentos, aportado 1.500, líquido 1.575 e 13,125%.

---

## 6. Testes novos (`Application.Test/Objetivos/Queries/GetResumoObjetivosList/`)

Estilo dos testes atuais (Moq para repositório e relógio). Relógio fixo em `2026-10-27`.

### `ResumoObjetivoModelTests` (funções puras, sem Moq)

**Saldo único**

- Objetivo com: ativo (aporte 1.000, líquido 1.100), `Pendente` (aporte 500, líquido 550), `Gasto` (aporte 300, líquido 400) e `Reinvestido` (aporte 700, líquido 800), os três últimos montados com `Encerrar`:
  - `QtdeMovimentos` = 2, `TotalAportado` = 1.500, `TotalLiquido` = 1.650;
  - `PercentualMeta` calculado sobre 1.650;
  - `Gasto` e `Reinvestido` **fora** de todos os quatro campos.
- Só ativos: igual ao comportamento anterior (regressão).
- Sem Movimentos: zeros e `PercentualMeta` 0; `Projecao` presente, sem exceção.

**`PercentualMeta`**

- `Meta` zero (dado legado) e negativa: `PercentualMeta` = 0, sem `DivideByZeroException`, e a projeção em "sem previsão".

**Projeção**

- **Consistência de saldo (D18):** com `Pendente` presente, `TotalLiquido` é igual ao saldo usado na projeção. Verificar de forma indireta: `Meta` igual ao `TotalLiquido` resulta em **`Atingida`** nos três cenários, e `Meta` um centavo acima **não**.
- `SaldoPendente` = soma dos `Pendente` (e zero sem eles).
- **Cenário determinístico** (relógio `2026-10-27`): `Meta` 12.000, `AporteMensal` 600; um Movimento ativo, aporte 1.000, líquido 1.000, investido em `2026-10-01` e atualizado em `2026-10-27` (26 dias):
  - taxa 0 e `BaixaConfianca` verdadeiro;
  - aporte realizado **indisponível** (único aporte no mês corrente), portanto `Conservador` e `Realista` em **`SemPrevisao`**;
  - `Planejado` **`Prevista`** com 19 meses (`(12.000 − 1.000) / 600 = 18,33`, para cima) e `DataPrevista` **2028-05-01**.
- **Reinvestimento fora do ritmo:** um Movimento com `EhReinvestimento = true` e valor alto **não** altera o aporte realizado (comparar com e sem ele).
- **`Gasto` conta como aporte realizado, mas não como saldo:** um aporte novo encerrado como `Gasto` entra em `AporteRealizadoMensal` e não entra em `TotalLiquido`.
- Todas as propriedades do `ResumoObjetivoModel` anteriores à mudança mantêm nome e tipo (reflexão simples: guarda do contrato).

### `GetResumoObjetivosListQueryTests` (acrescentar)

- **Relógio lido uma vez:** com três Objetivos, `GetDateTime` é chamado **uma vez** (`Times.Once`).
- **Só ativos são calculados:** Objetivo inativo não aparece (comportamento atual preservado), com `Pendente` ou não.
- **Datas padrão:** Movimentos sem datas preenchidas (como os fixtures antigos) não geram exceção e produzem `Projecao` com taxa 0.
- **Propagação do token:** o mesmo token chega a `GetAllWithRelatedEntitiesAsync` (já coberto; manter e confirmar que continua).
- **Cancelamento e falha:** os testes atuais de log e relançamento continuam verdes com o construtor novo.

---

## 7. Como validar

- `dotnet build` da solution.
- `dotnet test Application.Test --filter "FullyQualifiedName~Objetivos"`.
- `dotnet test Service.Test --filter "FullyQualifiedName~Registro"` (o contêiner ainda resolve a query com a dependência nova).
- Conferir por busca que **só** `CompoeSaldoDoObjetivo` define o conjunto do saldo: `grep -rn "EstaAtivo" Application/Objetivos` não deve mostrar o antigo filtro por `EstaAtivo` nos totais (o filtro do **Objetivo** ativo na query continua e é outro `EstaAtivo`).
- Ponto de atenção: a Api (parte 3) ainda **não** expõe `Projecao`; só os campos atuais, já com o novo significado.

**Critério de pronto:** build e testes verdes; relógio lido uma vez; saldo definido só por `CompoeSaldoDoObjetivo`; nenhuma consulta ou `Include` novo; `Projecao` sempre preenchida e sem exceção para dado legado.

---

## 8. Próximas partes

- **Parte 3, Service:** `ResumoObjetivoDto` ganha o objeto `Projecao` (com `CenarioProjecaoDto` para os três cenários), enum de situação como texto por atributo nas propriedades (como na spec 0019), mapeamento a partir dos modelos desta parte, testes de DTO e de contrato JSON; os campos atuais mantêm nome e tipo.
- **Parte 4, Presentation e fechamento:** DTOs do Web (com a própria cópia do enum), coluna "Previsão" e detalhe expansível, `TotaisObjetivos` sem previsão, formatador testado, roteiro ponta a ponta, documentação, ADR 0021 **Aceita** e ADR 0014 como complementado.

## Fora de escopo

- Qualquer alteração de endpoint, DTO ou página (partes 3 e 4).
- Cache da projeção ou consulta própria para ela.
- Alterar `Total Líquido` e `% Meta` de outras telas além da tabela de Objetivos da tela inicial.
