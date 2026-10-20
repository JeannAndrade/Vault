# Especificação técnica 0017: Dashboard de Rentabilidade (Vault), parte 4 de 4

- Data: 2026-10-20
- Status: **implementada (fechamento em 2026-10-20)**
- ADR relacionado: [0018](../adr/0018-dashboard-de-rentabilidade.md)
- Esta parte fecha a feature, continuando as partes [1](0017-vault-dashboard-rentabilidade-parte-1.md) (ADR e Domain), [2](0017-vault-dashboard-rentabilidade-parte-2.md) (Persistence e Application) e [3](0017-vault-dashboard-rentabilidade-parte-3.md) (Service). As decisões D1 a D17 e as "Atenções" da parte 1 continuam valendo.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e como validar.
> Todo código, comentário, commit e documentação em português.

---

## Escolhas desta parte (a confirmar)

| Item | Escolha |
|---|---|
| E1 | O Web tem **tipos próprios** (enum `OrdemRentabilidade` e DTOs em `Presentation/Dashboards/`). O `Presentation.csproj` não referencia Application nem Service e **assim continua** (Web e Api desacoplados por contrato HTTP) |
| E2 | O cliente envia `ordem` em **texto minúsculo** (`maiores`/`menores`), como já faz com `estaAtivo`; nunca o número do enum |
| E3 | URL digitada à mão com `top` fora da faixa é **normalizada na página** (clamp em 1 a 100), seguindo o precedente de `pagina` no Index de Movimentos. Os limites ficam em constantes do Web que espelham a Api; a Api continua sendo a autoridade (responde 422 se o Web divergir) |
| E4 | Casas decimais: acumulada e anualizada com **2**; diária com **4** (com 2 casas a diária ficaria em "0,03%" para quase tudo e não diferenciaria os investimentos). A confirmar contra o formato da sua planilha |
| E5 | A coluna anualizada recebe cor pelo sinal: `text-success` (positiva) e `text-danger` (negativa). O sinal é avaliado **após o arredondamento**, para nunca aparecer "-0,00%" |
| E6 | O detalhe expansível mostra Objetivo, Corretora e Valor do aporte (como pedido) e **também "Dias investidos"**, que ajuda a julgar a anualizada de aportes recentes (ADR 0018, Consequências) |
| E7 | Nota fixa sob o formulário explicando a regra de elegibilidade ("30 dias ou mais"). O número é uma constante do Web que espelha o Domain, com comentário apontando o ADR 0018 |
| E8 | Menu: novo dropdown **Dashboards** entre "Movimentos" e "Cadastros", com o item **Rentabilidade** |
| E9 | Falha da Api não derruba a página: `ApiException` vira alerta na própria tela (como em `Desatualizados` e na seção de vencimentos da tela inicial). A exceção de não autorizado **não** é engolida |

---

## Passo 5: Presentation (Web)

### 5.1 Tipos e cliente da Api (pasta `Presentation/Dashboards/`, namespace `Presentation.Dashboards`)

- **`OrdemRentabilidade`** (enum): `Maiores` e `Menores`.
- **`RentabilidadeItemDto`** (`sealed record`, propriedades `init`, como `MovimentoDesatualizadoDto`): `Guid Id`, `string? ProdutoNome`, `string? EmissorNome`, `string? ObjetivoNome`, `string? CorretoraNome`, `decimal ValorAporte`, `decimal ValorLiquidoAtual`, `int DiasInvestido`, `double Acumulada`, `double Diaria`, `double Anualizada` (frações, D14).
- **`RentabilidadeGrupoDto`** (`sealed record`): `Guid TipoRendaId`, `string? TipoRendaNome`, `List<RentabilidadeItemDto> Itens` (inicializada vazia).
- **`IRentabilidadeApi`** e **`RentabilidadeApi`** (`sealed`, construtor com `IApiConnection`, no estilo de `DistribuicaoValorLiquidoApi`):
  - `Task<List<RentabilidadeGrupoDto>> GetRentabilidadesAsync(int top, OrdemRentabilidade ordem, CancellationToken cancellationToken)`;
  - `BasePath = "api/dashboards/rentabilidade"`; GET com a query montada com `FormattableString.Invariant`, **nesta ordem fixa**: `?top={top}&ordem={ordem em minúsculo}` (E2);
  - corpo nulo da Api vira lista vazia (`?? []`), como em `GetDesatualizadosAsync`.

### 5.2 Formatação e cor (`Presentation/Dashboards/RentabilidadeFormatter.cs`)

Classe estática e pura, no estilo de `CotacaoFormatter`, para a regra ficar testável fora da view (ADR 0010):

- `string Percentual(double fracao, int casas = 2)`:
  - arredonda a fração a `casas + 2` casas (afastando do zero), converte `−0` em `0` e formata com `CultureInfo("pt-BR")` no formato percentual com `casas` casas ("0,225" vira "22,50%");
  - nunca devolve "-0,00%": um valor que arredonda para zero sai "0,00%".
- `string ClasseDaRentabilidade(double fracao)`: `"text-success"` se o valor **arredondado** (2 casas percentuais) for maior que zero, `"text-danger"` se for menor, `""` se for zero.
- O arredondamento usado para classe e para texto é o mesmo, para a cor nunca contradizer o número exibido.

### 5.3 Página `/Dashboards/Rentabilidade`

**Arquivos:** `Presentation/Pages/Dashboards/Rentabilidade.cshtml` e `Rentabilidade.cshtml.cs`, modelo `RentabilidadeModel(IRentabilidadeApi api)`.

**Modelo (`RentabilidadeModel`):**

- Constantes públicas: `TopPadrao = 25`, `TopMaximo = 100` e `DiasMinimosParaRanking = 30`, com comentário "espelha a Api e o Domain (ADR 0018)".
- `[BindProperty(SupportsGet = true, Name = "top")] int Top { get; set; } = TopPadrao` e `[BindProperty(SupportsGet = true, Name = "ordem")] OrdemRentabilidade Ordem { get; set; } = OrdemRentabilidade.Maiores`. Os nomes curtos mantêm a URL limpa (`?top=25&ordem=menores`), como o `pagina` do Index. O binder aceita `maiores`/`menores` sem diferenciar maiúsculas; um texto inválido deixa o padrão e a página **ignora** o `ModelState` (volta ao padrão, sem erro).
- `List<RentabilidadeGrupoDto> Grupos { get; private set; } = []` e `string? ErrorMessage { get; private set; }`.
- `OnGetAsync(CancellationToken cancellationToken)`:
  1. Normalizar: `Top = Math.Clamp(Top, 1, TopMaximo)` (E3). O campo exibido no formulário reflete o valor normalizado.
  2. `try`: `Grupos = await api.GetRentabilidadesAsync(Top, Ordem, cancellationToken)`.
  3. `catch (ApiException)`: `ErrorMessage = "Não foi possível carregar as rentabilidades agora. Tente novamente em instantes."` e `Grupos` fica vazio. Não capturar a exceção de não autorizado (E9).
- `string ObterTitulo(RentabilidadeGrupoDto grupo)`: devolve "Campeões da renda {nome}" no modo `Maiores` e "Menores rentabilidades da renda {nome}" no modo `Menores` (D15). Nome nulo vira "(sem nome)".
- `bool EhModoMenores` (conveniência para a view), se necessário.

**View (`Rentabilidade.cshtml`):**

- `ViewData["Title"] = "Rentabilidade"` e `<h1 class="h3 mb-3">Rentabilidade</h1>`.
- **Formulário `method="get"`** no padrão do filtro de Movimentos (`row g-3 mb-4 align-items-end`):
  - `<select name="ordem" class="form-select">` com duas opções, `value="maiores"` ("Maiores rentabilidades", padrão) e `value="menores"` ("Menores rentabilidades"), marcando `selected` conforme `Model.Ordem`;
  - `<input type="number" name="top" min="1" max="100" class="form-control" value="@Model.Top">` com o rótulo "Quantidade por renda";
  - botão "Aplicar".
- **Nota fixa** (`text-muted small`): "Considera Movimentos ativos com 30 dias ou mais entre o investimento e a última atualização do valor líquido. Rentabilidades calculadas a partir do valor do aporte e do valor líquido atual." O "30" vem de `DiasMinimosParaRanking`, não de texto digitado (E7).
- `ErrorMessage` não nulo: `alert alert-danger` (mesmo padrão do Index de Movimentos).
- **Sem grupos e sem erro:** `<p class="text-muted">Nenhum investimento elegível para o ranking.</p>`.
- **Um bloco por grupo:** `<h2 class="h5">` com `Model.ObterTitulo(grupo)` e uma tabela `table table-hover align-middle`, no estilo do Index de Movimentos:
  - colunas: (seta), Produto, Emissor, Valor Líquido Atual, Rentabilidade acumulada (%), Rentabilidade diária (%), Rentabilidade anualizada (%); valores numéricos com `text-end`;
  - **seta para baixo:** mesmo botão `collapse` do Index de Movimentos, com `id` do detalhe `detalhe-{item.Id}` (os `Id` são únicos no documento, pois cada Movimento aparece uma vez);
  - Valor Líquido Atual em moeda `pt-BR` (`ToString("C", new CultureInfo("pt-BR"))`, como no Index);
  - acumulada e diária com `RentabilidadeFormatter.Percentual` (2 e 4 casas, E4); anualizada com 2 casas e a classe de `ClasseDaRentabilidade` (E5);
  - **detalhe expansível** (linha extra com `colspan` igual ao número de colunas e `dl row mb-0`): Objetivo, Corretora, Valor do aporte (moeda) e Dias investidos (E6). Nomes nulos aparecem como "—".
- Nenhum script novo: o `collapse` é do Bootstrap já carregado no layout.

### 5.4 Menu (`Presentation/Pages/Shared/_Layout.cshtml`)

Novo `<li class="nav-item dropdown">` no mesmo padrão dos dropdowns existentes, **entre "Movimentos" e "Cadastros"** (E8): título "Dashboards" e um item `<a class="dropdown-item" asp-page="/Dashboards/Rentabilidade">Rentabilidade</a>`.

### 5.5 Registro (`Presentation/Program.cs`)

Acrescentar `builder.Services.AddApiResourceClient<IRentabilidadeApi, RentabilidadeApi>();` junto dos outros clientes e o `using Presentation.Dashboards;`. Sem outros registros.

### 5.6 Testes (`Presentation.Test`)

- **`Dashboards/RentabilidadeApiTests.cs`** (como `MovimentoApiTests`: `Mock<IApiConnection>`):
  - envia GET para `api/dashboards/rentabilidade?top=25&ordem=maiores` e devolve a lista do mock (mesma instância);
  - com `Top = 10` e `Menores`, envia `?top=10&ordem=menores` (minúsculo);
  - corpo nulo devolve lista vazia, sem exceção;
  - repassa o `CancellationToken`.
- **`Dashboards/RentabilidadeFormatterTests.cs`:**
  - `Percentual(0.225)` = "22,50%"; `Percentual(-0.10)` = "-10,00%"; `Percentual(-1)` = "-100,00%"; `Percentual(0)` = "0,00%";
  - `Percentual(0.000261157876, 4)` = "0,0261%";
  - **zero negativo:** `Percentual(-0.00001)` = "0,00%" (nunca "-0,00%");
  - milhares: `Percentual(12.345)` = "1.234,50%";
  - `ClasseDaRentabilidade`: positiva `text-success`, negativa `text-danger`, zero e `-0.00001` (arredonda para zero) `""`.
  - Se o ambiente de teste não tiver ICU/pt-BR, usar `CultureInfo.GetCultureInfo("pt-BR")` como o `CotacaoFormatter` e conferir que os testes existentes dele passam antes de acrescentar estes.
- **`Pages/Dashboards/RentabilidadeModelTests.cs`** (como `DesatualizadosModelTests`, com `Mock<IRentabilidadeApi>`):
  - **padrões:** sem alterar propriedades, `OnGetAsync` chama a Api com `(25, Maiores, token)` e `Grupos` é a lista devolvida;
  - **repasse:** com `Top = 10` e `Ordem = Menores`, chama `(10, Menores, token)`;
  - **normalização:** `Top = 0` chama com 1; `Top = -5` com 1; `Top = 500` com 100; o `Top` do modelo reflete o valor normalizado;
  - **falha da Api:** `ApiException(HttpStatusCode.InternalServerError, "erro")` deixa `ErrorMessage` preenchida, `Grupos` vazio e **não** lança;
  - **não autorizado não é engolido:** a exceção de não autorizado da lib propaga (usar o mesmo tipo que a tela inicial deixa passar; conferir em `IndexModelTests`);
  - **título:** `ObterTitulo` devolve "Campeões da renda CDB" em `Maiores` e "Menores rentabilidades da renda CDB" em `Menores`;
  - **lista vazia:** `Grupos` vazio e `ErrorMessage` nulo.
- A view não tem teste unitário (não há no repositório); fica coberta pela validação manual.

### 5.7 Como validar

```
dotnet build
dotnet test Presentation.Test --filter "FullyQualifiedName~Dashboards|FullyQualifiedName~Rentabilidade"
dotnet test
```

**Validação manual** (ambiente do `Docs/Docker.md`: `docker compose up --build -d`, Web em `http://localhost:5000`):

1. Dados de teste, em um Tipo de Renda "CDB" e com Movimentos ativos: A) aporte 1.000, valor líquido 1.100, 365 dias entre investimento e atualização; B) aporte 1.000, valor líquido 900, 365 dias; C) aporte 1.000, valor líquido 1.010, 30 dias; D) aporte 1.000, valor líquido 1.020, 10 dias; E) aporte 1.000, valor líquido 0, 100 dias.
2. Menu **Dashboards > Rentabilidade**: o título é "Campeões da renda CDB" e as linhas saem em ordem decrescente de anualizada: C (acumulada 1,00%, diária 0,0332%, anualizada 12,87%), A (10,00%, 0,0261%, 10,00%), B (−10,00%, −0,0289%, −10,00%) e E (−100,00% nas três). D **não aparece** (10 dias).
3. Selecionar **Menores rentabilidades** e aplicar: título "Menores rentabilidades da renda CDB"; ordem E (−100,00%), B (−10,00%), A, C. Valores negativos em vermelho e positivos em verde.
4. Alterar `top` para 2: só 2 linhas por Tipo de Renda. `?top=500` na URL: o campo volta com 100. `?ordem=abc`: volta ao modo Maiores, sem erro.
5. Seta para baixo em uma linha: mostra Objetivo, Corretora, Valor do aporte e Dias investidos.
6. Segundo usuário: não vê nenhum Movimento do primeiro. Sem login: redireciona para o login.
7. Parar a Api (`docker compose stop api`) e recarregar a página: alerta de erro na própria tela, sem ir para `/Error`. Subir de novo e conferir a recuperação.
8. Tipo de Renda cujos Movimentos têm todos menos de 30 dias: o título não aparece.

**Critério de pronto:** build e testes verdes, Web sem referência a Application ou Service, nenhum pacote novo, nenhum script novo, `top` e `ordem` apenas na URL.

---

## Passo 6: Documentação final e fechamento

1. Rodar a solução inteira: `dotnet build` e `dotnet test` verdes (inclui o teste de reflexão do ADR 0017).
2. **ADR 0018:** mudar o status para **Aceita (data do fechamento)** e acrescentar a seção "Implementação e fechamento", no estilo do ADR 0013, registrando só o que **diferiu** do texto original. Conferir, no mínimo:
   - os erros de entrada (`top` e `ordem`) viram `CommandValidationException` e a resposta HTTP 422 (e o `ModelState` inválido vira 422 pelo `[RequestValidation]`);
   - a query é registrada pelo `AddServicesFromAssembly` (sem alteração em `Program.cs` da Api);
   - `DiasInvestido` foi acrescentado ao item do ranking e ao detalhe da tela (E5 da parte 2 e E6 desta parte);
   - ordem dos grupos definida pelo repositório (collation do banco) e preservada pela query;
   - a feature não altera schema, migration, índice, pacote nem LumiaFoundation;
   - o gatilho de reavaliação (`EXPLAIN` e tempo de resposta em volume realista) continua valendo.
3. **Specs 0017 (partes 1 a 4):** mudar o status de cada uma para "implementada (fechamento em AAAA-MM-DD)".
4. Não há README ou índice de ADRs separado no repositório para atualizar (como registrado no ADR 0013). Se houver `README` por projeto no momento do fechamento, acrescentar a tela nova à lista de funcionalidades do Web.
5. Registrar fora da feature, sem implementar: a anualização de poucos dias é instável (ADR 0018); se isso incomodar na prática, abrir ADR para uma janela mínima configurável ou para exibir um aviso.

**Critério de pronto:** ADR em "Aceita" com a seção de fechamento, specs marcadas como implementadas, solução inteira verde e a validação manual do Passo 5.7 concluída.

---

## Fora do escopo desta feature

Persistir `top` ou `ordem` por usuário; gráficos; filtros por Corretora, Objetivo ou Produto; agrupar por título; exportar para planilha; novos dashboards (a rota `api/dashboards/...` e o menu **Dashboards** ficam prontos para recebê-los).
