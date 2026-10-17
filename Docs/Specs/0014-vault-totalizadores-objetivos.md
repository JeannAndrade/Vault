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

---

## Passo 2: Presentation, view

Arquivo: `Presentation/Pages/Index.cshtml`. Sem pacote novo, sem JavaScript.

### 2.1 Onde alterar

Somente o bloco da tabela de objetivos, dentro do `else` de `Model.Objetivos.Count == 0` (D6). Nada muda:

- na seção "Próximos vencimentos";
- no alerta de erro;
- no estado vazio ("Nenhum objetivo cadastrado.").

Como a linha de totais fica dentro do `else`, ela some junto com a tabela quando não há objetivos, sem condição extra.

### 2.2 Marcação

Acrescentar um `<tfoot>` na mesma `<table>`, depois do `</tbody>`:

- Uma única `<tr>` com:
  - `<th>` com o texto **Total** na primeira coluna (Nome);
  - uma `<td colspan="2"></td>` cobrindo **Meta** e **Percentual Meta** (D4: sem percentual global);
  - `<td class="text-end">` com `Model.Totais.QtdeMovimentos`, igual ao tratamento da coluna na tabela (inteiro, sem formatação especial);
  - `<td class="text-end">` com `Model.Totais.TotalAportado.ToString("C", culturaMoeda)`;
  - `<td class="text-end">` com `Model.Totais.TotalLiquido.ToString("C", culturaMoeda)`.
- Reutilizar a variável `culturaMoeda` já declarada no topo da view. **Não** criar outra instância de `CultureInfo`.
- Estilo só com classes Bootstrap já presentes, sem CSS novo:
  - `table-group-divider` no `<tfoot>` (linha de separação entre as linhas e o total);
  - `fw-semibold` na `<tr>` para destacar o total.
- A soma de colunas exige que a ordem das `<td>` do rodapé acompanhe a do cabeçalho: Nome, Meta, Percentual Meta, Qtde Movimentos, Total Aportado, Total Líquido. O `colspan="2"` cobre exatamente Meta e Percentual Meta; se uma coluna for acrescentada à tabela, o rodapé precisa ser revisado junto.

### 2.3 Cuidados

- **Não calcular na view.** Ela só lê `Model.Totais`; a soma vive em `TotaisObjetivos` (D3).
- **Encoding:** o Razor já escapa as expressões `@`; nenhum `Html.Raw`.
- **Acessibilidade:** o texto "Total" fica em `<th>`, e não em `<td>`, para leitores de tela associarem a linha ao rótulo.
- **Tabela estriada:** `table-striped` pinta apenas o `<tbody>`, então o `<tfoot>` não entra na alternância de cores.

### 2.4 Testes

A marcação Razor não tem teste unitário no projeto (as views são compiladas no build). A cobertura vem de três lugares:

- Passo 1: a soma e o `IndexModel` já estão testados.
- `dotnet build`: erros de Razor (propriedade inexistente, tag mal fechada) aparecem na compilação.
- Validação manual da seção 5 abaixo.

### 2.5 Como validar

- `dotnet build` e `dotnet test` da solution inteira, tudo verde.
- Subir Api e Web, abrir `/` e conferir a linha de totais abaixo da tabela, alinhada às três últimas colunas (roteiro completo na seção 5).

**Critério de pronto:** a view compila, o `<tfoot>` aparece só quando há objetivos, os valores batem com a soma das linhas e nenhum outro bloco da página mudou.

---

## Passo 3: Documentação final e fechamento

- `Docs/adr/0014-totalizadores-da-tela-inicial.md`: mudar o status para **Aceita**, com a data de fechamento.
- **READMEs do Vault:** se algum listar telas ou funcionalidades da tela inicial, incluir os totalizadores. (Nenhum README do Vault foi analisado; o dev verifica.)
- **Índice de ADRs e de especificações:** atualizar, se existir.
- **Esta especificação:** atualizar o status para **implementada**, com a data de fechamento.
- `Docs/Docker.md`, `Docs/MariaDb.md` e `compose.yaml`: sem mudança (não há migration, variável nem serviço novo).
- **Registro da implementação** no ADR (seção opcional, como no ADR 0013): confirmar que a feature não alterou Api, banco nem lib, e que o gatilho de reavaliação continua descrito.

**Critério de pronto:** ADR aceito, documentação atualizada e validação manual da seção 5 concluída.

---

## 5. Validação ponta a ponta (manual)

Com Api e Web no ar e um usuário com massa de dados variada (alguns objetivos com vários movimentos, ao menos um objetivo inativo e um objetivo sem movimentos ativos):

1. Abrir `/`. Abaixo da tabela de objetivos aparece a linha **Total**, com a Qtde de Movimentos, o Total Aportado e o Total Líquido alinhados sob as respectivas colunas.
2. **Conferência manual:** somar à mão (ou em planilha) as três colunas visíveis. Os totais devem ser exatamente iguais.
3. **Objetivo inativo:** marcar um objetivo com movimentos ativos como inativo (tela de Objetivos) e recarregar. Ele sai da tabela e os totais diminuem na mesma proporção. O total continua igual à soma das linhas visíveis.
4. **Movimento inativo:** inativar um movimento de um objetivo ativo. A Qtde e os valores da linha diminuem, e os totais acompanham.
5. **Objetivo sem movimentos ativos:** a linha aparece com `0` e `R$ 0,00` e não altera os totais.
6. **Sem objetivos ativos:** inativar todos os objetivos (em um usuário de teste). Aparece "Nenhum objetivo cadastrado." e **nenhuma** linha de totais.
7. **Formatação:** os valores usam o mesmo padrão de moeda `pt-BR` das linhas (`R$ 1.234,56`); a quantidade é inteira.
8. **Regressão visual:** a seção "Próximos vencimentos" continua igual, e a tabela de objetivos mantém as listras e o cabeçalho.
9. **Isolamento entre usuários:** com dois usuários distintos, cada um vê somente os seus totais.
10. **Docker:** `docker compose up --build -d` e repetir os passos 1 e 2 em `http://localhost:5000`. Não há migration nova, então nada muda na ordem de subida.

## 6. Riscos e pontos de atenção

| Risco | Mitigação |
| --- | --- |
| A soma no Web deixa de representar o conjunto completo se a lista de Objetivos virar paginada ou filtrada na Api | Gatilho de reavaliação registrado no ADR 0014 e na seção 3; nesse caso, abrir novo ADR com endpoint de totais |
| Alguém acrescentar ou reordenar colunas da tabela e esquecer o rodapé | Comentário na view indicando o acoplamento com a ordem das colunas (2.2) e revisão de PR |
| O total divergir de uma soma feita "por fora" (por exemplo, incluindo objetivos inativos) | Decisão D1 documentada no ADR: o total é a soma das linhas exibidas |
| Esquecer o `Setup` de `GetProximosVencimentosAsync` nos testes do `IndexModel` e receber `NullReferenceException` | Já indicado no Passo 1.3 |
| Percentual de meta global ser cobrado depois | Fora de escopo agora; se pedido, nova decisão (cálculo sobre a soma das metas) |

## 7. Fora de escopo

- Qualquer alteração na Api, no banco ou no LumiaFoundation.
- Percentual de meta global ou outros indicadores agregados.
- Totais da listagem de Movimentos (`/Movimentos`) ou dos Próximos vencimentos.
- Exportar, imprimir ou paginar a tabela de objetivos.
- Mudanças visuais além do `<tfoot>` (cores por faixa, gráficos).
