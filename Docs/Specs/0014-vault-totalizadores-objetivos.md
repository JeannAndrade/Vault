# Especificação técnica 0014: Totalizadores na tela inicial (Vault)

- Data: 2026-10-16
- Status: aprovada para implementação
- ADR relacionado: [0014](../adr/0014-totalizadores-da-tela-inicial.md)
- Repositório afetado: **Vault** (somente Presentation/Web). **Sem alteração na Api, no LumiaFoundation, no schema e sem migration.**

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.
> Documento entregue em partes: esta contém os Passos 0 e 1.

---

## 1. Resumo

Abaixo da tabela "Meus objetivos" da tela inicial, uma linha de totais soma as três últimas colunas da tabela: **Qtde Movimentos**, **Total Aportado** e **Total Líquido**. A soma é feita no Web, sobre a lista `ResumoObjetivoDto` já recebida da Api.

## 2. Decisões aprovadas

| Item | Decisão |
| --- | --- |
| D1 | Os totais são a soma exata das linhas exibidas (só Objetivos ativos, Movimentos ativos dentro deles) |
| D2 | Soma no Web, sem mudança na Api, no contrato HTTP, no banco ou na lib |
| D3 | Agregação em tipo próprio e testável, `TotaisObjetivos` (record), em `Presentation/Objetivos`, exposto pelo `IndexModel`; a regra não fica na view |
| D4 | Layout: `<tfoot>` na mesma tabela, com "Total" na primeira coluna; células de Meta e Percentual Meta vazias, sem percentual global |
| D5 | Formatação igual à da tabela: moeda em `pt-BR`, quantidade como inteiro |
| D6 | Sem objetivos, a tabela não aparece e os totais também não |

## 3. Atenção antes de começar

1. **Nenhuma consulta nova e nenhuma I/O nova.** Por isso não há `CancellationToken` a propagar nesta feature: a soma é feita em memória, depois da chamada à Api que já recebe o token.
2. **A soma usa somente os três campos da tabela** (`QtdeMovimentos`, `TotalAportado`, `TotalLiquido`). `Meta` e `PercentualMeta` não entram.
3. **Dinheiro em `decimal`.** Nenhum `double` na soma.
4. **A Web não referencia a Api.** O tipo novo usa só `ResumoObjetivoDto`, que já existe na Presentation.
5. **Gatilho de reavaliação (ADR 0014):** se a lista de Objetivos passar a ser paginada ou filtrada na Api, a soma no Web deixa de representar o conjunto completo. Nesse caso, abrir novo ADR.
6. **Todo código, comentários, commits e documentação em português.**
7. **Status do ADR:** começa **Proposta**; muda para **Aceita** no último passo.

## 4. Ordem de implementação

Cada passo termina com `dotnet build` e `dotnet test` verdes antes do seguinte.

| # | Passo | Projeto |
| --- | --- | --- |
| 0 | ADR 0014 como "Proposta" | Vault (docs) |
| 1 | `TotaisObjetivos` e `IndexModel` | Presentation |
| 2 | View (`Index.cshtml`) | Presentation |
| 3 | Documentação final e fechamento do ADR | Vault |

---

## Passo 0: ADR 0014 como "Proposta"

- Criar `Docs/adr/0014-totalizadores-da-tela-inicial.md` com o texto do ADR aprovado, **Status: Proposta (2026-10-16)**.
- Atualizar o índice de ADRs, se existir.

**Critério de pronto:** arquivo criado, no padrão dos ADRs anteriores, status "Proposta".

---

## Passo 1: Presentation, lógica e testes

Sem pacote novo.

### 1.1 `TotaisObjetivos`

Arquivo: `Presentation/Objetivos/TotaisObjetivos.cs`, namespace `Presentation.Objetivos` (ao lado de `ResumoObjetivoDto`).

- Record **selado** e imutável, no estilo dos DTOs da pasta, com:
  - `int QtdeMovimentos`
  - `decimal TotalAportado`
  - `decimal TotalLiquido`
- Propriedade estática `Vazio`, com os três valores zero (usada como estado inicial do `IndexModel`, para a view nunca lidar com nulo).
- Método estático `FromResumo(IEnumerable<ResumoObjetivoDto> objetivos)`:
  - lança `ArgumentNullException` se `objetivos` for nulo (erro de programação, não de regra);
  - soma `QtdeMovimentos`, `TotalAportado` e `TotalLiquido` de todos os itens;
  - lista vazia devolve valores zero (equivalente a `Vazio`);
  - **não** usa `Meta` nem `PercentualMeta`.
- Comentário curto na classe indicando que é agregação de apresentação sobre dados já entregues pela Api e referenciando o ADR 0014 e o gatilho de reavaliação.

### 1.2 `IndexModel` (`Presentation/Pages/Index.cshtml.cs`)

- Nova propriedade `TotaisObjetivos Totais { get; private set; }`, inicializada com `TotaisObjetivos.Vazio`.
- Em `OnGetAsync`, logo depois de carregar `Objetivos` (`GetAllComValoresAsync`), atribuir `Totais = TotaisObjetivos.FromResumo(Objetivos)`.
- **Calcular uma vez**, no handler, e não numa propriedade calculada a cada acesso: a view lê o valor três vezes.
- Nada mais muda: o carregamento dos próximos vencimentos, o tratamento de `ApiException` e os métodos de cor ficam como estão. A construção do modelo (`IObjetivoApi`, `IMovimentoApi`, `IDateTimeProvider`) não muda.

### 1.3 Testes (`Presentation.Test`)

**`Objetivos/TotaisObjetivosTests.cs`** (estilo dos demais testes de DTO/Api, `[Fact]` e AAA):

- Vários objetivos: soma correta dos três campos (valores diferentes em cada linha, para detectar campo trocado).
- Objetivo único: totais iguais aos da linha.
- Lista vazia: três valores zero e igual a `TotaisObjetivos.Vazio`.
- Precisão decimal: somar valores com centavos (por exemplo, `0,10` e `0,20`) resulta exatamente em `0,30`.
- `Meta` e `PercentualMeta` não influenciam: duas listas iguais nos três campos somados e diferentes em `Meta`/`PercentualMeta` produzem o mesmo total.
- Lista nula: `ArgumentNullException`.

**`Pages/IndexModelTests.cs`** (novo; o namespace `Presentation.Test.Pages` já existe; `using Presentation.Pages;` para o `IndexModel` raiz, sem conflito com os `IndexModelTests` das subpastas):

- Usar `Mock<IObjetivoApi>`, `Mock<IMovimentoApi>` e `Mock<IDateTimeProvider>`.
- No `Mock<IMovimentoApi>`, configurar `GetProximosVencimentosAsync` para devolver lista vazia: o `OnGetAsync` o chama sempre, e um mock sem `Setup` pode devolver nulo.
- `OnGetAsync_CalculaTotaisAPartirDosObjetivos`: a Api devolve dois `ResumoObjetivoDto`; `Totais` reflete a soma; `Objetivos` é a mesma lista devolvida.
- `OnGetAsync_SemObjetivos_TotaisZerados`: a Api devolve lista vazia; `Totais` igual a `TotaisObjetivos.Vazio`.
- `Totais_AntesDoOnGet_EhVazio`: modelo recém-criado já tem `Totais` zerado (a view nunca vê nulo).
- Se o projeto já tiver testes do `IndexModel` raiz fora deste caminho, estender o arquivo existente em vez de criar um novo.

### 1.4 Como validar

- `dotnet test Presentation.Test` (ou `dotnet test Presentation.Test --filter "TotaisObjetivos|IndexModelTests"`).
- `dotnet build` da solution (a view ainda não usa `Totais`, então nada muda na tela neste passo).

**Critério de pronto:** testes verdes, `TotaisObjetivos` sem dependência nova, `IndexModel` expondo `Totais` calculado uma única vez em `OnGetAsync`, nenhuma alteração fora da Presentation.
