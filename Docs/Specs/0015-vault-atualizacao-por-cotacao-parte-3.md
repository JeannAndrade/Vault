# Especificação técnica 0015: Atualização do Valor Líquido por cotação (Vault), parte 3 de 3

- Data: 2026-10-18
- Status: **em elaboração (parte 3 de 3: Passos L, L2, 6 e 7). Com esta parte a especificação fica completa para revisão.**
- ADR relacionado: 0016 (texto revisado entregue junto; substitui o das partes 1 e 2)
- Repositórios afetados: **LumiaFoundation** (`AspNetCore`) e **Vault**.

> Especificação para outro dev implementar. Sem código de implementação.
> Todo código, comentário, commit e documentação em português.

---

## E. Errata consolidada das partes 1 e 2

Valem sobre os textos anteriores:

| # | Onde | Mudança |
|---|---|---|
| E1 | Parte 2, 5.2 | **Removido** o `ControllerBaseExtensions.ValidarCorpo`. A guarda de corpo e de `ModelState` passa a ser o `RequestValidationAttribute` da lib (Passo L) |
| E2 | Parte 2, 5.3 e 5.4 | Os controllers novos (`CotacoesController` e `AtualizacaoPorCotacaoController`) levam `[RequestValidation]` no **nível da classe**. A checagem inline de `ModelState` na ação de listagem e a chamada a `ValidarCorpo` não existem |
| E3 | Parte 2, 5.6 | Os testes de controller **não** testam 422 por corpo nulo ou `ModelState` inválido (o filtro não roda em teste unitário de ação). Cada controller novo ganha um teste de reflexão afirmando que o atributo está aplicado; o comportamento é coberto pelos testes da lib e pela validação manual |
| E4 | Parte 1, seção 4 | Ordem atualizada (tabela abaixo): entram os Passos L e L2 |
| E5 | Parte 1, 3.3 | Remover `GetAtivosPorTituloAsync` e `TituloFiltro` (e o método correspondente em `FakeMovimentoRepository`) é **obrigatório**, em commit separado e antes do restante do Passo 3 |
| E6 | Parte 2, errata do início | Mantida: upsert só na tela de atualização; `POST` de atualização responde 200 com corpo |
| E7 | ADR | O texto do ADR 0016 entregue com esta parte substitui o anterior |

### Ordem de implementação atualizada

| # | Passo | Projeto |
|---|---|---|
| 0 | ADR 0016 como "Proposta" | Docs |
| L | `RequestValidationAttribute` | LumiaFoundation |
| L2 | Consumir o filtro e refatorar os controllers atuais | Vault |
| 1 | Saneamento do `IdentityRole` | Persistence |
| 2 | Domain | Domain |
| 3 | Persistence e migration | Persistence |
| 4 | Application | Application |
| 5 | Service (Api) | Service |
| 6 | Presentation (Web), gráfico e menu | Presentation |
| 7 | Documentação final e fechamento | Docs |

Cada passo termina com build e testes verdes. Os Passos 1 a 3 não dependem de L e L2 e podem andar em paralelo.

---

## Passo L: `RequestValidationAttribute` na LumiaFoundation (0.28.0 → 0.29.0)

Repositório: `JeannAndrade/Lumia.Foundation`, projeto `LumiaFoundation.AspNetCore`. Mudança **aditiva**; nenhum contrato público existente é alterado (o `DtoNotEmptyValidationAttribute` permanece como está).

### L.1 O que fazer

Nova classe `RequestValidationAttribute` em `LumiaFoundation.AspNetCore.ActionFilters`:

- Selada, herda de `ActionFilterAttribute`, **sem dependências** (aplicada com `[RequestValidation]`, sem registro no DI nem `ServiceFilter`). `AttributeUsage` para classe e método.
- `OnActionExecuting`, nesta ordem:
  1. **Corpo.** Para cada parâmetro de `context.ActionDescriptor.Parameters` cujo `BindingInfo?.BindingSource` seja `BindingSource.Body`, se o argumento correspondente em `context.ActionArguments` não existir ou for nulo, lançar `HttpBaseException` com status 422 e a mensagem "Corpo da requisição inválido.".
  2. **ModelState.** Se `!context.ModelState.IsValid`, lançar `HttpBaseException` 422 com "Parâmetros inválidos: {chaves}.", listando **apenas as chaves** com erro (nunca os valores enviados).
- Sem corpo declarado e `ModelState` válido: nada acontece (serve para `GET`).
- A exceção é tratada pelo `DomainExceptionHandler` já registrado, que a devolve como `ErrorDetails` (confirmar no teste de integração descrito abaixo).

### L.2 Testes (`LumiaFoundation.AspNetCore.Test`)

Usar `ActionExecutingContext` com `ControllerActionDescriptor` e parâmetros com `BindingInfo`:

- Corpo declarado com argumento nulo: `HttpBaseException` 422 com a mensagem do corpo.
- Corpo declarado com argumento presente e `ModelState` válido: sem exceção e `Result` nulo.
- `ModelState` inválido sem corpo (`GET`): 422 com as chaves na mensagem; a mensagem **não** contém o valor enviado.
- Corpo nulo **e** `ModelState` inválido: prevalece a mensagem do corpo.
- Ação com `Guid` de rota mais corpo preenchido: sem exceção (garante que não há heurística por nome de tipo).
- Ação sem nenhum parâmetro: sem exceção.
- Integração (se o projeto de teste já tiver `TestServer`/`WebApplicationFactory`; caso contrário, registrar como validação manual no L2): o 422 sai como `ErrorDetails` pelo handler.

### L.3 Documentação e versão

- `LumiaFoundation.AspNetCore/docs/readme.md`: nova seção "Validação de requisição" (uso, mensagens, diferença para o `DtoNotEmptyValidationAttribute`) e entrada `0.29.0` no histórico.
- `readme.md` da raiz: a tabela de pacotes está defasada (por exemplo, AspNetCore 0.20.0 e Core 0.11.0). Alinhar todas as versões às dos `.csproj`.
- Publicar o pacote no NuGet **antes** do Passo L2.

**Como validar:** `dotnet test LumiaFoundation.slnx`.

**Critério de pronto:** testes verdes, docs atualizadas e pacote 0.29.0 publicado.

---

## Passo L2: Vault consome o filtro e refatora os controllers

### L2.1 Pacote

`Application.csproj`: `Lumia.Foundation.AspNetCore` para **0.29.0** (o Service herda transitivamente). `Presentation.csproj` pode ficar na 0.28.0 ou ser alinhada; não usa o filtro.

### L2.2 Refatorações (commit separado)

- `DistribuicaoValorLiquidoController`: aplicar `[RequestValidation]` na classe; **remover** o `ValidarCorpo`, os `ModelState`/`HttpBaseException` que só ele usava e os `?` e `!` do DTO nas duas ações (o filtro garante corpo não nulo).
- `MovimentosController`: aplicar `[RequestValidation]` na classe; **remover** o bloco inline de `ModelState` do `GetMovimentos` (e o `using` de `HttpBaseException` se ficar sem uso). Efeito colateral desejado: `POST` e `PUT` de Movimento com corpo nulo ou JSON malformado passam a responder 422 em vez de falhar com erro 500 por referência nula.
- Status continua 422; a mensagem de parâmetros inválidos do `GetMovimentos` passa a ser a genérica da lib.

### L2.3 Testes

- `MovimentosControllerTests`: remover `GetMovimentos_ComParametroNaoNumerico_Lanca422SemConsultar` (comportamento agora é da lib); remover `using`s sem uso; acrescentar um teste de reflexão: o tipo `MovimentosController` possui `RequestValidationAttribute`.
- `DistribuicaoValorLiquidoControllerTests`: remover os testes de corpo nulo e `ModelState` inválido; acrescentar o mesmo teste de reflexão.
- Os demais testes desses controllers seguem iguais.

### L2.4 Como validar

- `dotnet test Service.Test`.
- Manual, com a Api no ar e token válido: `GET api/movimentos?page=abc` responde 422 em `ErrorDetails`; `POST api/movimentos` sem corpo e com JSON malformado respondem 422; `POST .../distribuicao-valor-liquido/simulacao` sem corpo responde 422; uma chamada válida de cada rota continua funcionando.

**Critério de pronto:** testes verdes, nenhuma validação de corpo ou `ModelState` escrita à mão nos controllers do Vault (exceto os controllers de autenticação, que seguem no filtro antigo).

---

## Passo 6: Presentation (Web)

Sem pacote .NET novo. Uma dependência de front-end: Chart.js (LibMan). A Web não referencia a Api: usa DTOs próprios.

### 6.1 DTOs e clientes (`Presentation/Cotacoes/` e `Presentation/Movimentos/`)

**DTOs** (records selados, propriedades `init`):

- `CotacaoDto` (`Id`, `Papel`, `Valor`, `Data`), `CotacaoForCreationDto`, `CotacaoForUpdateDto` (`Papel`, `Valor`, `Data`), `CotacaoPontoDto` (`Data`, `Valor`).
- `PapelParaCotacaoDto` (`Papel`, `UltimoValor` `decimal?`, `DataUltimaCotacao` `DateTime?`), `AtualizacaoPorCotacaoSolicitacaoDto` (`Papel`, `Valor`, `Data`) e `AtualizacaoPorCotacaoResultadoDto` (`MovimentosAtualizados`, `QuantidadeMovimentos`).
- `MovimentoDto`, `MovimentoForCreationDto` e `MovimentoForUpdateDto`: acrescentar `AtualizaPorCotacao`.

**`ICotacaoApi` / `CotacaoApi`** (`BasePath = "api/cotacoes"`):

- `GetPagedAsync(string? papel, int page, int pageSize, ct)`: URL `api/cotacoes?page=..&pageSize=..` e, **omitindo nulo ou vazio**, `&papel=` com `Uri.EscapeDataString`; cultura invariante (padrão de `MovimentoApi`); resposta nula devolve página vazia.
- `GetPapeisAsync(ct)` (`/papeis`, fallback `[]`), `GetSerieAsync(papel, ct)` (`/serie?papel=`, escapado, fallback `[]`), `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` (padrão das demais APIs).

**`IAtualizacaoPorCotacaoApi` / `AtualizacaoPorCotacaoApi`** (`api/movimentos/atualizacao-por-cotacao`), interface separada, como na Distribuição:

- `GetAsync(ct)`: lista, fallback `[]`.
- `AtualizarAsync(solicitacao, ct)`: `POST` com corpo; resposta nula lança `InvalidOperationException` em português.

**Registro** em `Program.cs`: `AddApiResourceClient` para as duas interfaces.

### 6.2 Formatação

`Presentation/Cotacoes/CotacaoFormatter` (estático, testável): formata cotação em reais `pt-BR` com **2 a 4 casas** (`R$ 40,00`; `R$ 12,3456`). Usado nas três telas, para a view não ter regra.

### 6.3 CRUD de Cotação (`Pages/Cotacoes/`)

Padrão das páginas de Movimentos (filtro e página preservados).

- **Index:** `Pagina` (`pagina`) e `Papel` (`papel`, `SupportsGet`); tamanho de página 12; select de papéis (primeira opção `--`) vindo de `GetPapeisAsync`; formulário `GET` só com o select (sempre página 1) e link **Limpar filtro**; tabela Papel, Data (`dd/MM/yyyy`), Valor (formatter), Ações (Editar, Excluir); `<lumia-pagination>`; redirect para a última página quando a atual deixou de existir; exclusão (`POST`, com `asp-route-pagina` e `asp-route-papel` **na action** do formulário) que preserva página e filtro; estado vazio com e sem filtro; botão **Novo** e link para o gráfico. `RouteValues` exposto pelo modelo.
- **Create e Edit:** `InputModel` com `Papel` (`[Required]`, `[StringLength(60)]`), `Valor` (`[Range]` maior que zero, mensagem em português) e `Data` (`[Required]`, `DataType.Date`; padrão **hoje** via `IDateTimeProvider` no Create). Campo de papel com `<datalist>` dos papéis já cotados. `type="number"` com `step="0.0001"` e `min="0.0001"`. Página e filtro em campos ocultos (padrão de Create/Edit de Movimento); redirect e "Cancelar" preservam ambos. Trim do papel antes de enviar.
- **Erros da Api:** `ApiException` genérica mostra a mensagem padrão ("Não foi possível salvar a cotação agora..."). Para **422** (por exemplo, cotação duplicada), exibir a mensagem devolvida pela Api. **Antes de implementar**, conferir no readme/código do `Http.Client` como `ApiException` expõe o status e a mensagem de `ErrorDetails`; se não expuser a mensagem, mostrar uma frase fixa para 422 ("Já existe cotação para este papel nesta data, ou os dados são inválidos.") e registrar a limitação.

### 6.4 Atualização por cotação (`Pages/Movimentos/AtualizacaoPorCotacao.cshtml` e `.cs`)

Modelo `AtualizacaoPorCotacaoModel(IAtualizacaoPorCotacaoApi api, IDateTimeProvider clock)`, no padrão da tela `Desatualizados`.

- **Estado:** `Papeis`, `[BindProperty] InputModel Input` (`Valor` com `[Required]` e `[Range]` maior que zero; `Data` com `[Required]`), `string? PapelComErro`, `ErrorMessage`, `[TempData] MensagemSucesso` e `Hoje` (`clock.GetDateTime().Date`).
- `OnGetAsync`: carrega a lista; `Input.Data = Hoje`.
- `OnPostAtualizarAsync(string papel)` (`asp-route-papel`): `ModelState` inválido ou `Data` futura (comparada a `Hoje`): recarrega, marca `PapelComErro`, `Page()` sem chamar a Api. Sucesso: `AtualizarAsync`; `MensagemSucesso` conforme o resultado ("{papel}: N movimento(s) atualizado(s)." ou "{papel}: cotação registrada somente no histórico (data anterior à última cotação)."); `RedirectToPage()` (PRG). `ApiException`: mensagem genérica (ou a da Api para 422, conforme 6.3), recarrega e `Page()`.
- **View:** título "Atualização por Cotação"; texto explicando a regra (a cotação mais recente recalcula os movimentos ativos com a flag; data anterior grava só o histórico). Tabela: Papel, Última cotação (formatter) e Data da última cotação (ou "sem cotação"), campo **Nova cotação** (`name="Input.Valor"` manual, `id` único por linha, `step="0.0001"`, `min="0.0001"`, `required`), campo **Data** (`type="date"`, valor `yyyy-MM-dd`, `name="Input.Data"`) e botão **Atualizar**. Mensagem de erro na linha de `PapelComErro`; estado vazio ("Nenhum movimento com valor por cotação.").
- Cuidado de cultura: preencher o campo de data com o formato `yyyy-MM-dd` e qualquer valor numérico pré-preenchido com cultura invariante.

### 6.5 Gráfico (`Pages/Cotacoes/Grafico.cshtml` e `.cs`)

- **Modelo:** `Papel` (`SupportsGet`), lista de papéis (select com `Selecione...`), série carregada de `GetSerieAsync` quando há papel, e `DadosJson`: string produzida por `CotacaoGraficoDados.ToJson(papel, pontos)` (classe estática testável em `Presentation/Cotacoes/`), com `papel`, `rotulos` (datas `dd/MM/yyyy`) e `valores` (números, cultura invariante), via `System.Text.Json`.
- **View:** formulário `GET` com o select e botão **Ver gráfico**; sem papel: orientação; papel sem registros: aviso; com série: `<canvas id="grafico-cotacao" data-pontos="@Model.DadosJson">` (o Razor codifica o atributo). Rodapé com quantidade de registros e período. Link de volta para Cotações.
- **Scripts:** `@section Scripts` referenciando o Chart.js local e `wwwroot/js/cotacao-grafico.js`. O arquivo estático lê `data-pontos`, monta um gráfico de **linhas**, formata o eixo Y e os tooltips em reais `pt-BR` e **não** contém lógica de negócio. Sem script inline.
- **LibMan:** acrescentar a biblioteca em `libman.json` (provider `cdnjs`, destino `wwwroot/lib/chart.js`, apenas o arquivo UMD minificado). **Fixar a versão e o nome do arquivo conferindo no cdnjs/`libman`** (a versão exata não está definida nesta spec) e registrá-los no ADR. Como `wwwroot/lib` está no `.gitignore`, documentar o `libman restore` antes do build local e do `docker compose build`.

### 6.6 Movimento: checkbox e detalhes

- `CreateModel.InputModel` (compartilhado com Edit): propriedade `AtualizaPorCotacao`, mapeada em `ToCreationDto`, `ToUpdateDto` e `MapToInput` do Edit; implementar `IValidatableObject`: flag marcada exige `Quantidade` maior que zero, mensagem em português no campo `Quantidade`.
- `Create.cshtml` e `Edit.cshtml`: checkbox "Valor varia por cotação" e texto de ajuda no campo Quantidade ("Obrigatória quando o valor varia por cotação").
- `Movimentos/Index.cshtml`: no detalhe expandido, linha "Varia por cotação" (Sim/Não).

### 6.7 Menu (`Pages/Shared/_Layout.cshtml`)

- Dropdown **Cadastros**: itens "Cotações" e "Gráfico de Cotações".
- Dropdown **Manutenção**: item "Atualização por Cotação", depois dos atuais.

### 6.8 Testes (`Presentation.Test`)

- `CotacaoApiTests`: rota, verbo e corpo de cada método; `GetPagedAsync` com e sem papel (nulo/vazio omitido, ordem `page, pageSize, papel`, papel com espaço e acento escapado); fallbacks `[]`/página vazia; `GetSerieAsync` escapa o papel.
- `AtualizacaoPorCotacaoApiTests`: `GetAsync` (rota e fallback) e `AtualizarAsync` (`POST`, corpo, resposta nula lança `InvalidOperationException`).
- `CotacaoFormatterTests`: `40` vira `R$ 40,00`; `12,3456` mantém 4 casas; `0,5` vira `R$ 0,50`; milhar (`R$ 1.234,56`).
- `CotacaoGraficoDadosTests`: rótulos e valores na ordem, cultura invariante (ponto decimal), lista vazia, papel com aspas é serializado corretamente.
- Páginas de Cotações: `IndexModelTests` (carrega página e papéis, filtro repassado, última página preserva o filtro, exclusão preserva página e filtro, `ApiException` na exclusão), `CreateModelTests` e `EditModelTests` (`ModelState` inválido não chama a Api, trim, redirect preserva página e filtro, 422 mostra mensagem), `GraficoModelTests` (sem papel não consulta a série; com papel gera `DadosJson`; série vazia).
- `AtualizacaoPorCotacaoModelTests`: `OnGet` carrega e define a data de hoje; `ModelState` inválido e data futura não chamam a Api e marcam `PapelComErro`; sucesso com recálculo e só histórico (mensagens distintas) e redirect; `ApiException` mostra erro e devolve `Page()`.
- Movimento: `InputModel` com flag e sem quantidade falha na validação; flag com quantidade passa; `ToCreationDto`/`ToUpdateDto`/`MapToInput` carregam a flag.
- Ajustar os testes existentes que constroem os DTOs e as páginas alteradas.

### 6.9 Como validar

`dotnet test Presentation.Test` e `dotnet build` (erros de Razor aparecem no build). Com Api e Web no ar, percorrer as telas conforme a seção 8.

**Critério de pronto:** testes verdes, telas funcionando de ponta a ponta, menu atualizado, Chart.js servido localmente e nenhum script inline.

---

## Passo 7: Documentação final e fechamento

- ADR 0016: status **Aceita**, com a data, e registro da versão do Chart.js.
- ADR 0011: acrescentar ao status "Complementada pelo ADR 0016 (movimentos por cotação ficam fora da lista de desatualizados)". ADR 0013: acrescentar "Movimentos por cotação ficam fora da distribuição (ADR 0016)".
- `Docs/Docker.md` (ou seção equivalente): lembrar do `libman restore` antes do `docker compose build` por causa do Chart.js (e do Bootstrap).
- READMEs do Vault: se algum listar endpoints ou telas, incluir os novos. (Nenhum README do Vault foi analisado; o dev verifica.)
- Docs da lib: feitas no Passo L.
- Esta especificação (partes 1 a 3): status **implementada**, com a data de fechamento.

---

## 8. Validação ponta a ponta

Preparar: dois Emissores cadastrados, "PETR4" (e outro "VALE3"), e Movimentos ativos de PETR4 em duas corretoras (20 e 40 unidades), todos com a flag. Fazer backup do banco antes (`Docs/MariaDb.md`).

1. `dotnet test` na lib e depois no Vault, tudo verde.
2. Migrations: `DESCRIBE Movimentos;` (coluna `AtualizaPorCotacao`), `DESCRIBE Cotacoes;`, `SHOW INDEX FROM Cotacoes;` (índice único), `SHOW TABLES;` (sem `IdentityRole`). Duas cotações do mesmo papel e data devem falhar no índice.
3. **Flag e quantidade:** criar e editar um Movimento com a flag e sem quantidade: mensagem de erro na tela e 422 na Api. Com quantidade: salva. A flag aparece no detalhe da listagem.
4. **Tela de atualização:** aparece uma linha por papel distinto (PETR4 uma vez, mesmo com dois Movimentos e, se houver, dois Emissores com o mesmo nome). Informar 40,00 com a data de hoje: os Movimentos passam a valer 800,00 e 1.600,00, com `DataAtualizacaoValorLiquido` igual a hoje; mensagem de sucesso com a quantidade (2).
5. **Substituição no mesmo dia:** informar 41,00 no mesmo dia: não surge segunda cotação (uma linha no CRUD) e os valores viram 820,00 e 1.640,00.
6. **Data retroativa:** informar 35,00 com a data de ontem: histórico gravado, valores dos Movimentos **inalterados**, mensagem "somente no histórico". Data futura: erro, nada gravado.
7. **CRUD:** criar, editar e excluir cotações; `POST` duplicado (papel e data iguais) responde 422 e a tela mostra o erro; editar uma cotação para colidir com outra também; filtro por papel, paginação e exclusão preservam página e filtro; excluir o Emissor PETR4 (após inativar/excluir os Movimentos) **não** apaga o histórico, e o papel continua no gráfico.
8. **Gráfico:** selecionar PETR4 mostra a linha com todas as datas em ordem; papel sem registros mostra o aviso; trocar de papel funciona; eixo e tooltip em reais `pt-BR`.
9. **Exclusões entre telas:** os Movimentos com flag **não** aparecem em "Manutenção Valor Líquido" (mesmo com `DataAtualizacaoValorLiquido` com mais de 30 dias, ajustando no banco) e **não** entram na Distribuição (título só com flag mostra o aviso de "sem aportes ativos").
10. **Isolamento entre usuários:** o usuário B não vê, não lista, não edita nem exclui cotações do usuário A (404 por id; listas e série vazias) e não recalcula Movimentos de A.
11. **Maiúsculas/minúsculas (MariaDB):** registrar "petr4" no mesmo dia de "PETR4": deve colidir no índice único (collation insensível) e a lista de papéis da atualização não duplica o papel. É a verificação que os testes InMemory não fazem.
12. **Validação de requisição:** `GET api/cotacoes?page=abc`, `POST api/cotacoes` sem corpo e com JSON malformado respondem 422 em `ErrorDetails`.
13. **`EXPLAIN`** (quando houver volume): consulta de série e "última por papel" usam `IX_Cotacoes_UserId_Papel_Data`.
14. **Docker:** `libman restore`, `docker compose up --build -d` e repetir os passos 4 e 8 em `http://localhost:5000`. Há migrations novas (a ordem de subida já garante que rodem antes da Api).

## 9. Riscos e pontos de atenção

| Risco | Mitigação |
|---|---|
| Publicar a lib (0.29.0) é pré-requisito da Api e bloqueia os passos 5 e L2 | Passos 1 a 3 e a parte de Domain/Persistence andam em paralelo; publicar a lib primeiro |
| A exceção lançada em um filtro de ação não chegar ao `DomainExceptionHandler` | Teste de integração na lib ou validação manual (seção 8, item 12) |
| Colisão de maiúsculas/minúsculas só aparece no MariaDB | Item 11 da validação manual |
| `ApiException` não expor a mensagem da Api para 422 | Verificação prévia no 6.3, com mensagem fixa como plano B |
| Entre 21h e 24h (BRT) o padrão "hoje" do campo de data vem um dia à frente | Campo editável; registrado no ADR |
| Duas confirmações simultâneas do mesmo papel e data | Aceito (app pessoal); a segunda falha por índice único |
| Movimento por cotação sem atualização por muito tempo não aparece em nenhuma lista de defasagem | A tela de atualização exibe a data da última cotação de cada papel |
| Cinco consultas e um `JSON.parse` no gráfico para séries muito longas (anos de histórico) | Aceitável na escala atual (milhares de pontos); reavaliar com agregação se crescer |
| Valor de cotação com mais de 4 casas é rejeitado ou truncado pelo `decimal(18,4)` | Validação `Range` e `step` no formulário; documentado |
| O texto de `libman restore` esquecido quebra o build Docker (sem Chart.js) | Documentado no Passo 7 |

## 10. Fora de escopo

- Integração com fonte externa de cotações (importação automática, APIs de mercado).
- Cotações em outra moeda e cripto.
- Filtro por período e agregações no gráfico; vários papéis no mesmo gráfico; eixo de tempo proporcional (adaptador de datas).
- Atualização de várias cotações em lote.
- Alertas de preço alvo.
- Aplicar `[RequestValidation]` nos demais controllers de CRUD e migrar os controllers de autenticação do `DtoNotEmptyValidationAttribute` (ver decisões pendentes).
- Mudanças em `IdentityRole` além do Passo 1.
