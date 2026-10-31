# Especificação técnica 0021: Consolidado de Movimentos (Vault), parte 3 de 3

- Data: 2026-10-30
- Status: **implementada (fechamento em 2026-10-30)**
- ADR relacionado: [0022](../adr/0022-consolidado-de-movimentos.md)
- Partes anteriores: [parte 1](0021-vault-consolidado.md) (Passos 0 a 2) e [parte 2](0021-vault-consolidado-parte-2.md) (Passo 3)
- Repositório afetado nesta parte: **Presentation (Web)** e documentação. Sem schema, sem migration, sem dependência nova, sem Domain e sem alteração no LumiaFoundation.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Resumo

O Web ganha a página **`/Dashboards/Consolidado`** e o item **Consolidado** no menu **Dashboards**. Quatro selects de filtro (`--` = todos), um select "Agrupar por", botão **Filtrar** e link **Limpar filtro**. Sem filtro e sem agrupamento a tela mostra só uma orientação e **não chama a Api**. Com critério, mostra o total e, com agrupamento, uma linha por grupo com a coluna **Percentual** (fatia do Total Líquido). Notas fixas explicam que `Pendente` fica fora e que Objetivo inativo entra.

O Web continua sem referenciar Domain, Application, Service ou Persistence: tem DTOs e a própria cópia do enum, ligados à Api só pelo contrato JSON.

## 2. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D20 | Cópia própria do enum `DimensaoConsolidado` em `Presentation.Dashboards` (`TipoRenda`, `Corretora`, `Produto`, `Emissor`), como `OrdemRentabilidade`; um teste fixa os nomes |
| D21 | O cliente envia `agruparPor` em **minúsculo** (`corretora`, `tiporenda`), como `ordem` na rentabilidade; a Api aceita sem diferenciar maiúsculas. Nunca o número do enum |
| D22 | URL da Api em **ordem fixa**: `tipoRendaId, corretoraId, produtoId, emissorId, agruparPor`; nulos omitidos; GUID em formato invariante |
| D23 | Cliente separado `IConsolidadoApi` (um método), no estilo de `RentabilidadeApi` e `FluxoMensalApi` |
| D24 | Filtros **só na URL** da página (`?Filtro.CorretoraId=...&agruparPor=corretora`), formulário `GET`, como a listagem de Movimentos e o ADR 0012. Nada é persistido |
| D25 | A página **não chama a Api** quando não há filtro nem agrupamento (regra P2); a Api também rejeitaria com 422, mas a chamada é evitada |
| D26 | Selects reaproveitam `IMovimentoFormLookupData` (cinco listas, inclusive Objetivos, que não são usadas aqui); custo aceito na escala atual, como no ADR 0012 |
| D27 | Falha da Api (`ApiException`) vira alerta na própria tela; não autorizado **não** é engolido. Falha ao carregar os selects segue o padrão de `Movimentos/Index` (propaga) |
| D28 | Percentual exibido com `RentabilidadeFormatter.Percentual` (2 casas, `pt-BR`), reaproveitado depois de converter a fração `decimal` para `double`; sem formatador novo de percentual |
| D29 | Rótulos das dimensões em um formatador estático e testado, `ConsolidadoFormatter` |
| D30 | Sem JavaScript novo e sem dependência nova |

## 3. Atenção antes de começar

1. **Sem lógica de negócio no Web.** Total, percentual, ordem e conjunto de Movimentos vêm da Api; o Web só formata. Nenhuma soma ou razão na página.
2. **O Web não decide o que conta como saldo.** As notas fixas da tela descrevem a regra da Api (ADR 0022); se a regra mudar, as notas mudam junto.
3. **Select vazio vira `null`.** `<option value="">--</option>` é o que faz `Guid?` e `DimensaoConsolidado?` ficarem nulos no binding.
4. **Moeda em `pt-BR`** (espaço não separável U+00A0): nos testes, montar o texto esperado com a mesma cultura.
5. **Api e Web saem juntos.** Antes da parte 3, a Api já responde; o Web simplesmente ainda não tem a tela.
6. **Percentuais de linhas podem não somar 100% exatos por arredondamento.** É esperado e não é erro.
7. **Valor de enum desconhecido** (Api mais nova que o Web) faria a desserialização falhar; como Api e Web saem juntos, é aceito, e o teste de nomes protege.

## 4. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 4.1 | Enum, DTOs e filtro do cliente | tipos e testes de contrato |
| 4.2 | Cliente da Api e registro | `IConsolidadoApi`, `ConsolidadoApi`, `Program.cs`, testes |
| 4.3 | Formatador e filtro de entrada | `ConsolidadoFormatter`, `ConsolidadoFiltroInput`, testes |
| 4.4 | Página e modelo | `.cshtml`, `.cshtml.cs`, testes do modelo |
| 4.5 | Menu | `_Layout.cshtml` |
| 5 | Ponta a ponta e fechamento | roteiro, documentação, ADR |

---

## Passo 4: Presentation (Web)

### 4.1 Tipos (`Presentation/Dashboards/`, namespace `Presentation.Dashboards`)

- **`DimensaoConsolidado`** (enum): `TipoRenda`, `Corretora`, `Produto`, `Emissor`. Comentário curto: cópia do contrato da Api; os nomes trafegam como texto na URL e não mudam sem alterar Api e Web juntos.
- **`ConsolidadoFiltro`** (`sealed record`, no estilo de `MovimentoFiltro`): `Guid? TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId`.
- **`ConsolidadoLinhaDto`** (`sealed record` posicional): `Guid? Id`, `string? Nome`, `int Quantidade`, `decimal TotalAportado`, `decimal TotalLiquido` e `decimal PercentualLiquido`.
- **`ConsolidadoDto`** (`sealed record` posicional): `ConsolidadoLinhaDto Total` e `List<ConsolidadoLinhaDto> Grupos` (padrão vazio, via propriedade `init` com valor inicial, para uma resposta sem `grupos` não gerar nulo).

### 4.2 Cliente da Api

- **`IConsolidadoApi`**: `Task<ConsolidadoDto> GetConsolidadoAsync(ConsolidadoFiltro filtro, DimensaoConsolidado? agruparPor, CancellationToken cancellationToken)`.
- **`ConsolidadoApi`** (`sealed`, construtor com `IApiConnection`): `BasePath = "api/dashboards/consolidado"`.
  - Monta a query pela lista de pares na ordem fixa do D22, omitindo nulos; GUIDs e texto com cultura invariante (`FormattableString.Invariant`); `agruparPor` em minúsculo (D21). Sem nenhum par, o caminho fica sem `?` (a página nunca chega aí, mas o método não pode produzir `?` vazio).
  - GET sem corpo; corpo nulo da Api vira um `ConsolidadoDto` **zerado** (total com zeros e `Grupos` vazio), sem exceção.
- **`Program.cs`:** `builder.Services.AddApiResourceClient<IConsolidadoApi, ConsolidadoApi>();` junto dos demais. O `using Presentation.Dashboards;` já existe.

### 4.3 Formatador e filtro de entrada

**`ConsolidadoFormatter`** (`Presentation/Dashboards/`, estático e puro):

- `RotuloDimensao(DimensaoConsolidado? dimensao)`: `TipoRenda` → "Tipo de Renda", `Corretora` → "Corretora", `Produto` → "Produto", `Emissor` → "Emissor"; nulo → texto vazio.
- `Percentual(decimal fracao)`: delega a `RentabilidadeFormatter.Percentual((double)fracao)`; não repete a regra de arredondamento.
- `NomeDoGrupo(string? nome)`: nome, ou "—" quando nulo ou vazio.

**`ConsolidadoFiltroInput`** (`Presentation/Pages/Dashboards/`, classe de binding, no estilo de `MovimentoFiltroInput`): os quatro `Guid?`, `bool HasValue` e `ToFiltro()` para `ConsolidadoFiltro`.

### 4.4 Página `/Dashboards/Consolidado`

Arquivos `Pages/Dashboards/Consolidado.cshtml` e `.cshtml.cs`, modelo `ConsolidadoModel(IConsolidadoApi api, IMovimentoFormLookupData lookupData)`.

**Estado do modelo:**

- `[BindProperty(SupportsGet = true)] ConsolidadoFiltroInput Filtro` (inicializado, nunca nulo).
- `[BindProperty(SupportsGet = true, Name = "agruparPor")] DimensaoConsolidado? AgruparPor`.
- `MovimentoFormLookupData Lookup` (`private set`), `ConsolidadoDto? Resultado` (`private set`) e `string? ErrorMessage` (`private set`).
- `bool TemCriterio => Filtro.HasValue || AgruparPor.HasValue` (P2).
- `bool TemGrupos => Resultado is { Grupos.Count: > 0 }`.

**`OnGetAsync(CancellationToken cancellationToken)`:**

1. Carrega `Lookup`.
2. Sem critério: `Resultado` fica nulo e **não chama a Api** (D25).
3. Com critério: `try` `Resultado = await api.GetConsolidadoAsync(Filtro.ToFiltro(), AgruparPor, cancellationToken)`; `catch (ApiException)`: `ErrorMessage = "Não foi possível carregar o consolidado agora. Tente novamente em instantes."` e `Resultado` nulo. A exceção de não autorizado não é capturada (D27).

**View (`Consolidado.cshtml`):**

- `ViewData["Title"] = "Consolidado"` e `<h1 class="h3 mb-3">Consolidado</h1>`.
- **Formulário `method="get"`** (`row g-3 mb-3 align-items-end`, padrão do filtro de Movimentos): selects de Tipo de Renda, Corretora, Produto e Emissor com `asp-for="Filtro.X"` e `asp-items` do `Lookup`, primeira opção `<option value="">--</option>`; select "Agrupar por" com `<option value="">Sem agrupamento</option>` e as quatro dimensões (valores `TipoRenda`, `Corretora`, `Produto`, `Emissor`, rótulos do formatador), `selected` conforme `Model.AgruparPor`; botões **Filtrar** (submit) e **Limpar filtro** (link para `/Dashboards/Consolidado` sem parâmetros).
- **Notas fixas** (`text-muted small`):
  - "Considera apenas Movimentos ativos. Movimentos encerrados como Pendente não entram: o dinheiro pode já ter sido levado para outro lugar."
  - "Movimentos de Objetivos inativos entram. Por isso o total pode ser diferente do total da tela inicial, que lista só Objetivos ativos e inclui Pendente."
- `ErrorMessage` não nulo: `alert alert-danger`.
- **Sem critério:** `<p class="text-muted">Escolha ao menos um filtro ou um agrupamento e clique em Filtrar.</p>`.
- **Com resultado e `Total.Quantidade == 0`:** `<p class="text-muted">Nenhum movimento ativo encontrado para os critérios selecionados.</p>`, sem tabela.
- **Sem agrupamento (resultado com total):** tabela de uma linha, colunas **Qtde Movimentos**, **Total Aportado** e **Total Líquido**, com `text-end`, moeda `pt-BR` (`ToString("C", CultureInfo.GetCultureInfo("pt-BR"))`) e quantidade inteira.
- **Com agrupamento:** tabela `table table-hover align-middle` com colunas: rótulo da dimensão (`ConsolidadoFormatter.RotuloDimensao`), **Qtde Movimentos**, **Total Aportado**, **Total Líquido** e **Percentual do Total Líquido**; uma linha por grupo, na ordem recebida; `<tfoot>` com **Total** (quantidade, aportado, líquido e percentual do total). Percentual via `ConsolidadoFormatter.Percentual`. A ordem das células do `<tfoot>` acompanha a do cabeçalho (comentário Razor avisando, como na tela inicial).
- Valores monetários e percentuais alinhados à direita; nenhum cálculo na view.

### 4.5 Menu (`Presentation/Pages/Shared/_Layout.cshtml`)

No dropdown **Dashboards**, depois de **Fluxo Mensal**: `<li><a class="dropdown-item" asp-page="/Dashboards/Consolidado">Consolidado</a></li>`.

---

## 5. Testes (`Presentation.Test`)

Estilo dos testes atuais (xUnit, Moq).

### `Dashboards/ConsolidadoApiTests.cs` (`Mock<IApiConnection>`)

- Só um filtro: `api/dashboards/consolidado?corretoraId=<id>`; nenhum outro parâmetro.
- Todos os filtros mais agrupamento: ordem fixa `tipoRendaId, corretoraId, produtoId, emissorId, agruparPor`.
- Só `agruparPor`: `?agruparPor=corretora`; `TipoRenda` sai `tiporenda` (minúsculo).
- Nulos omitidos; GUID em formato invariante.
- Corpo nulo: devolve `ConsolidadoDto` zerado, sem exceção.
- Repassa o `CancellationToken`.

### `Dashboards/ConsolidadoContratoJsonTests.cs`

Com `JsonSerializerOptions(JsonSerializerDefaults.Web)`:

- Desserializa o **JSON de exemplo da parte 2** (Inter e XP): `Total` com `Id` e `Nome` nulos, `Quantidade` 5, dois grupos, `PercentualLiquido` 0,6 e 0,4, valores `decimal` corretos.
- JSON sem `grupos`: `Grupos` vazio (nunca nulo).
- Os nomes de `DimensaoConsolidado` são exatamente `TipoRenda`, `Corretora`, `Produto` e `Emissor` (guarda do contrato com a Api).

### `Dashboards/ConsolidadoFormatterTests.cs`

- `RotuloDimensao`: cada valor e nulo.
- `Percentual`: `0.6m` → "60,00%"; `1m` → "100,00%"; `0m` → "0,00%"; `0.36709m` → "36,71%".
- `NomeDoGrupo`: nome, nulo e vazio ("—").

### `Pages/Dashboards/ConsolidadoFiltroInputTests.cs`

- `HasValue` falso sem campos e verdadeiro com qualquer um; `ToFiltro` copia os quatro valores.

### `Pages/Dashboards/ConsolidadoModelTests.cs` (`Mock<IConsolidadoApi>` e `Mock<IMovimentoFormLookupData>`)

- **Sem critério:** `OnGetAsync` carrega o `Lookup`, `Resultado` nulo e a Api **não** é chamada (`Times.Never`).
- **Só filtro:** chama a Api com o filtro mapeado, `agruparPor` nulo e o token; `Resultado` é o devolvido.
- **Só agrupamento:** chama a Api com filtro vazio e a dimensão.
- **Filtro e agrupamento:** repassa os dois.
- **`TemGrupos`:** falso sem grupos, verdadeiro com grupos.
- **Falha da Api** (`ApiException(HttpStatusCode.InternalServerError, "erro")`): `ErrorMessage` preenchida, `Resultado` nulo, não lança.
- **Não autorizado não é engolido:** a exceção de não autorizado da lib propaga (usar o mesmo tipo que `RentabilidadeModelTests` verifica).
- **Falha 422 da Api** (por exemplo, inconsistência entre Web e Api): vira a mesma mensagem genérica, sem quebrar a página.
- A view não tem teste unitário (não há no repositório); fica coberta pelo roteiro do Passo 5.

---

## Passo 5: Ponta a ponta e fechamento

### 5.1 Roteiro manual

Ambiente do `Docs/Docker.md` (`libman restore`, `docker compose up --build -d`, Web em `http://localhost:5000`), com o usuário A logado e a validação do EF contra o MariaDB (parte 2) feita.

**Dados de teste** (usuário A; só os campos citados importam):

| Movimento | Tipo de Renda | Corretora | Situação | Aporte | Líquido |
| --- | --- | --- | --- | --- | --- |
| M1 | Fixa | Inter | ativo | 1.000 | 1.100 |
| M2 | Fixa | Inter | ativo | 500 | 520 |
| M3 | Fixa | XP | ativo | 2.000 | 2.100 |
| M4 | Variável | XP | ativo | 800 | 900 |
| M5 | Fixa | Inter | encerrado `Pendente` | 700 | 750 |
| M6 | Fixa | Inter | encerrado `Gasto` | 300 | 320 |
| M7 | Fixa | Inter | ativo, **Objetivo inativo** | 100 | 120 |
| M8 | Fixa | Inter | ativo, **outro usuário** | 9.999 | 9.999 |

**Roteiro:**

1. **Sem critério:** Dashboards > Consolidado abre com a orientação e sem tabela. Nenhuma chamada à Api no log do Web.
2. **Tipo de Renda = Fixa:** Qtde **4**, Aportado **R$ 3.600,00**, Líquido **R$ 3.840,00** (M1, M2, M3, M7). M5, M6 e M8 ficam fora; M7 entra.
3. **Corretora = Inter:** Qtde **3**, Aportado **R$ 1.600,00**, Líquido **R$ 1.740,00**.
4. **Fixa e Inter juntos:** os mesmos valores do item 3.
5. **Só agrupamento, por Corretora:** linhas **XP** (2 movimentos, R$ 2.800,00, R$ 3.000,00, **63,29%**) e **Inter** (3, R$ 1.600,00, R$ 1.740,00, **36,71%**), nessa ordem (líquido decrescente); total **5 / R$ 4.400,00 / R$ 4.740,00 / 100,00%**.
6. **Fixa agrupada por Corretora:** **XP** (1, R$ 2.000,00, R$ 2.100,00, **54,69%**) e **Inter** (3, R$ 1.600,00, R$ 1.740,00, **45,31%**); total **4 / R$ 3.600,00 / R$ 3.840,00**.
7. **Agrupar por Emissor/Produto/Tipo de Renda:** as linhas somam o total e os rótulos da coluna mudam.
8. **Agrupar pela dimensão já filtrada** (Corretora = Inter, agrupar por Corretora): uma linha, 100,00%.
9. **Sem correspondência** (combinação sem Movimentos, como Variável e Inter): "Nenhum movimento ativo encontrado...", sem tabela.
10. **Limpar filtro:** volta à URL sem parâmetros, selects em `--`, só a orientação.
11. **Notas:** as duas notas fixas estão visíveis e corretas. Conferir que o total sem filtro **difere** do "Total" da tela inicial (M5 e M7).
12. **Pendente vs reativar:** reativar M5 e conferir que ele passa a entrar na Corretora Inter; encerrar de novo como `Pendente` e conferir que sai.
13. **Isolamento:** o usuário B não enxerga nenhum valor de A; colando na URL um ID de corretora de A, B recebe zeros. Sem login: redireciona para o login.
14. **URL:** copiar a URL do resultado e abrir em outra aba; reproduz o mesmo resultado (filtros só na URL). Recarregar não perde o estado.
15. **Erro isolado:** `docker compose stop api`, recarregar com filtro: alerta na tela, sem `/Error`. Subir a Api de novo e conferir a recuperação.
16. **Conferência no banco** (somente leitura), para o item 3: `SELECT COUNT(*), SUM(ValorAporte), SUM(ValorLiquidoAtual) FROM Movimentos WHERE UserId = ? AND EstaAtivo = 1 AND CorretoraId = ?` bate com a tela.
17. **Regressão visual:** o menu **Dashboards** continua com Rentabilidade, Fluxo Mensal e agora Consolidado.

### 5.2 Documentação e fechamento

1. Rodar a solução inteira: `dotnet build` e `dotnet test` verdes (inclui os testes de reflexão do ADR 0017 e de registro).
2. **ADR 0022:** status para **Aceita (data do fechamento)**, com a correção do item 1 da parte 2 já aplicada, e seção **"Implementação e fechamento"** registrando só o que diferiu do texto original. Conferir, no mínimo:
   - tipos finais e nomes dos endpoints e parâmetros;
   - a tradução das consultas pelo EF/Pomelo (`GroupBy` com `Sum`) sem ajuste, ou a solução adotada;
   - a regra "filtro ou agrupamento" na Application e no Web;
   - a Api não devolve `agruparPor`;
   - o percentual reaproveita o formatador da rentabilidade;
   - nenhuma alteração de schema, migration, índice, pacote, Domain ou LumiaFoundation;
   - o gatilho de reavaliação (`EXPLAIN` e tempo em volume realista).
3. **Specs 0021 (as três partes):** status para "implementada (fechamento em AAAA-MM-DD)".
4. **ADRs relacionados:** acrescentar "Complementada pelo ADR 0022" ao status dos ADRs 0012 e 0014, se o mantenedor quiser o rastro (o conteúdo não muda).
5. **READMEs:** `Presentation/README.md` (tela de Consolidado e as regras do conjunto) e `Service/README.md` (contrato de `GET api/dashboards/consolidado`).
6. **Registrar fora da feature, sem implementar:**
   - o consolidado não mostra o `Pendente` (decisão do usuário); se houver necessidade, abrir ADR para uma linha "dos quais Pendente";
   - o total pode divergir do da tela inicial por definição; se incomodar, alinhar as telas é decisão futura;
   - agrupar por mais de uma dimensão, multiseleção, gráfico e exportação seguem como evolução.

### 5.3 Checklist final

- [ ] `dotnet build` e `dotnet test` da solution inteira verdes.
- [ ] Consultas do consolidado executadas contra o MariaDB, sem erro de tradução, e números conferidos com SQL manual.
- [ ] Contrato JSON da parte 2 consumido pelo Web (mesmo exemplo desserializa nos dois lados).
- [ ] Nomes do enum idênticos na Api e no Web (testes que os fixam passando).
- [ ] Nenhuma dependência nova, nenhum script inline, nenhum segredo em texto puro, nenhuma exceção engolida (além do `ApiException` isolado na tela).
- [ ] Roteiro do 5.1 executado, inclusive o isolamento entre usuários.
- [ ] ADR 0022 **Aceita** e specs **implementadas**.
- [ ] Publicação **conjunta** de Api e Web.

## 6. Riscos

| Risco | Mitigação |
| --- | --- |
| O usuário compara este total com a tela inicial e estranha a diferença | Notas fixas na tela e consequência registrada no ADR |
| Cópia do enum divergir entre Api e Web | Testes que fixam os nomes nos dois lados e roteiro manual |
| Tradução do `GroupBy` falhar só no MariaDB (InMemory não valida) | Validação manual obrigatória na parte 2 e no item 16 do roteiro |
| Percentuais exibidos não somarem exatamente 100% | Esperado; é só arredondamento de exibição |
| Cinco chamadas de lookup por renderização (uma sem uso) | Aceito na escala atual, como no ADR 0012; reavaliar com cache se necessário |
| Mudança futura da regra do conjunto sem atualizar as notas da tela | Item no checklist de qualquer ADR que altere o conjunto de Movimentos |

## Fora de escopo

Multiseleção por dimensão; agrupamento por mais de uma dimensão; inclusão de `Pendente`; gráficos; exportação; persistir o último filtro; qualquer alteração de schema, Domain ou LumiaFoundation.
