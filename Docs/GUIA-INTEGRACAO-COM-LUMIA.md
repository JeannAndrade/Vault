# Integração entre `Vault.Presentation`, `Vault.Service` e as libs do Lumia

<!-- TOC start (generated with https://github.com/derlin/bitdowntoc) -->

- [Integração entre `Vault.Presentation`, `Vault.Service` e as libs do Lumia](#integração-entre-vaultpresentation-vaultservice-e-as-libs-do-lumia)
  - [Como ler este guia](#como-ler-este-guia)
  - [1. Integração do `Vault.Presentation` com o Lumia](#1-integração-do-vaultpresentation-com-o-lumia)
    - [1.1. Registro do cliente HTTP e dos clientes de recurso](#11-registro-do-cliente-http-e-dos-clientes-de-recurso)
    - [1.2. Autenticação, sessão, 401 e refresh](#12-autenticação-sessão-401-e-refresh)
    - [1.3. Tratamento de `ApiException`](#13-tratamento-de-apiexception)
    - [1.4. Página de exceção e páginas de status](#14-página-de-exceção-e-páginas-de-status)
    - [1.5. Exibição de `ErrorDetails` e erros por campo](#15-exibição-de-errordetails-e-erros-por-campo)
    - [1.6. Tratamento de exceções na API e persistência](#16-tratamento-de-exceções-na-api-e-persistência)
      - [Quadro exceção → HTTP](#quadro-exceção--http)
    - [1.7. `SaveAsync` e D2-B](#17-saveasync-e-d2-b)
    - [1.8. Origem e dependência do `ApiUnauthorizedExceptionHandler`](#18-origem-e-dependência-do-apiunauthorizedexceptionhandler)
    - [1.9. Convenções dos clientes de recurso](#19-convenções-dos-clientes-de-recurso)
    - [1.10. Testes do lado do consumidor](#110-testes-do-lado-do-consumidor)
    - [1.11. Logging e observabilidade no consumidor](#111-logging-e-observabilidade-no-consumidor)
    - [1.12. Checklist para um novo recurso consumidor](#112-checklist-para-um-novo-recurso-consumidor)
    - [1.13. Pontos a validar](#113-pontos-a-validar)
    - [Resumo por tópico](#resumo-por-tópico)
  - [2. Integração do `Vault.Service` (API) com o Lumia](#2-integração-do-vaultservice-api-com-o-lumia)
    - [2.1. Host da API e contratos do `AspNetCore.Api`](#21-host-da-api-e-contratos-do-aspnetcoreapi)
      - [Composição e pipeline atuais do Vault](#composição-e-pipeline-atuais-do-vault)
      - [OpenAPI](#openapi)
      - [Filtros e registro por convenção](#filtros-e-registro-por-convenção)
      - [Usuário atual e `ErrorDetails`](#usuário-atual-e-errordetails)
    - [2.2. Persistência, provedores, migrations e health checks](#22-persistência-provedores-migrations-e-health-checks)
      - [`AddPersistence` e seleção do banco](#addpersistence-e-seleção-do-banco)
      - [Dois contextos e migrations](#dois-contextos-e-migrations)
      - [Verificação de saúde do banco](#verificação-de-saúde-do-banco)
    - [2.3. Cliente HTTP, autenticação e antiforgery](#23-cliente-http-autenticação-e-antiforgery)
      - [`AddLumiaApiClient`, refresh e métodos HTTP](#addlumiaapiclient-refresh-e-métodos-http)
      - [Store de tokens e 401](#store-de-tokens-e-401)
      - [Cadastro, login, refresh e bloqueio de tentativas](#cadastro-login-refresh-e-bloqueio-de-tentativas)
      - [Cookie e antiforgery no Vault](#cookie-e-antiforgery-no-vault)
    - [2.4. Logging e `Lumia.Foundation.Logger`](#24-logging-e-lumiafoundationlogger)
  - [3. Referências de implementação e implantação](#3-referências-de-implementação-e-implantação)
    - [3.1. Diferenças previstas para o host do Flashcard](#31-diferenças-previstas-para-o-host-do-flashcard)

<!-- TOC end -->

Este documento descreve a integração em ambos os lados da fronteira HTTP:

- como o `Vault.Presentation` usa as bibliotecas do Lumia para chamar a API, manter a sessão e apresentar erros;
- como o `Vault.Service` usa as bibliotecas do Lumia para expor contratos, autenticar requisições, tratar exceções e acessar persistência;
- quais decisões atravessam os dois lados e devem ser preservadas por novos hosts ou recursos.

<!-- TOC --><a name="como-ler-este-guia"></a>

## Como ler este guia

O fluxo principal é: `Vault.Presentation` → cliente HTTP do Lumia → `Vault.Service` → Foundation de API/EF do Lumia. As seções 1 e 2 explicam cada lado dessa integração; as seções finais reúnem decisões, pontos pendentes e referências. O documento também serve de **referência para qualquer novo consumidor da API** (outro host Web, ilhas Blazor, novos recursos).

**Escopo.** Além do comportamento dos hosts, este guia registra os contratos da Foundation que afetam a integração. As referências de código e documentação estão na seção 3.

**Legenda de status** usada neste guia:

- **Implementado**: comportamento verificado no código do Vault.
- **Diretriz**: convenção recomendada para código novo; pode ainda não estar aplicada em todo o código existente.
- **Pendente**: decisão ou verificação em aberto (ver seção 1.13).

<!-- TOC --><a name="1-integração-do-vaultpresentation-com-o-lumia"></a>

## 1. Integração do `Vault.Presentation` com o Lumia

Este bloco explica o lado consumidor: composição do cliente, autenticação, tratamento de respostas, convenções de clientes de recurso, testes e observabilidade.

<!-- TOC --><a name="11-registro-do-cliente-http-e-dos-clientes-de-recurso"></a>

### 1.1. Registro do cliente HTTP e dos clientes de recurso

O registro central fica em `Presentation/Program.cs`:

```csharp
builder.Services.AddLumiaApiClient(
    vaultApiAddress,
    configureAuthentication: options => options.RefreshPath = "/api/token/refresh");
```

O endereço vem de `VaultApi:BaseAddress`. Os clientes de recurso são registrados em `Presentation/Extensions/PresentationExtensions.cs`, no método `AddApiResourceClientService`, por meio de chamadas como:

```csharp
services.AddApiResourceClient<IMovimentoApi, MovimentoApi>();
```

Cada implementação, por exemplo `Presentation/Movimentos/MovimentoApi.cs`, recebe `IApiConnection` por injeção de dependência e concentra as chamadas HTTP daquele recurso. As Razor Pages dependem da interface (`IMovimentoApi`, `IObjetivoApi` etc.), e não de `HttpClient` diretamente.

<!-- TOC --><a name="12-autenticação-sessão-401-e-refresh"></a>

### 1.2. Autenticação, sessão, 401 e refresh

Também em `Presentation/Program.cs`:

- `AddSessionTokenStore()` armazena o token da API em sessão;
- `AddApiSessionCookie()` configura o cookie de autenticação do Web;
- `AddLumiaApiClient(... RefreshPath = "/api/token/refresh")` configura o caminho usado pelo cliente para renovar o token;
- `AddExceptionHandler<ApiUnauthorizedExceptionHandler>()` registra o tratamento de respostas não autorizadas.

As configurações detalhadas da sessão estão em `Presentation/Extensions/PresentationExtensions.cs`: a sessão usa `DistributedMemoryCache`, expira após oito horas e a chave do token é `Vault.ApiToken`. O cookie `Vault.Auth` dura até sete dias, com expiração deslizante desativada.

O pipeline chama `UseSession()` antes de `UseExceptionHandler()` porque o handler de 401 precisa limpar a sessão. Quando a API devolve 401 e o refresh não resolve a autenticação, a exceção de não autorizado não deve ser capturada pelas páginas como um erro comum; ela segue para `ApiUnauthorizedExceptionHandler`, que encerra a sessão e conduz o usuário ao login.

O ADR [0005 — Sessão e chaves do Web](adr/0005-sessao-e-chaves-do-web.md) registra que a sessão e as chaves do Data Protection não são persistidas. Reiniciar o Web exige novo login. O ADR [0006 — Configuração da Api](adr/0006-configuracao-da-api.md) documenta a configuração da API e do JWT.

**Ilhas Blazor (pendente).** O fluxo de 401 descrito acima vale para requisições tratadas pelo pipeline de Razor Pages. Para componentes Blazor hospedados nas páginas (ADR 0014), o caminho do 401 ainda não está descrito neste guia: não está documentado se a exceção chega ao `ApiUnauthorizedExceptionHandler` ou se o componente precisa tratá-la. Ver item 6 da seção 1.13.

<!-- TOC --><a name="13-tratamento-de-apiexception"></a>

### 1.3. Tratamento de `ApiException`

As páginas capturam `ApiException` somente quando conseguem apresentar uma falha localizada sem derrubar a página inteira. Exemplos:

- `Presentation/Pages/Movimentos/Create.cshtml.cs`: erro de gravação no formulário;
- `Presentation/Pages/Movimentos/Distribuicao.cshtml.cs`: erro isolado na simulação ou confirmação;
- `Presentation/Pages/Index.cshtml.cs`: erro isolado de uma seção da página inicial;
- `Presentation/Pages/Dashboards/Rentabilidade.cshtml.cs` e `Consolidado.cshtml.cs`: erro no painel correspondente.

O padrão é preencher uma propriedade como `ErrorMessage` e retornar `Page()`. A view exibe essa propriedade em um alerta Bootstrap, normalmente com `role="alert"`. Assim, a falha vira uma mensagem de tela, formulário ou seção, conforme o contexto.

Em geral, uma falha de API recebe uma mensagem amigável e fixa, por exemplo: “Não foi possível salvar o movimento agora. Tente novamente em instantes.”. Quando a página decide preservar `ApiException.Message`, isso vale para qualquer status HTTP, inclusive 409 e 404, e não apenas para 422. O uso atual dessa mensagem diretamente na tela ainda é seletivo e aparece, por exemplo, em `Movimentos/Create.cshtml.cs` e `Movimentos/Encerrar.cshtml.cs` para 422.

**Origem de `ApiException.Message`.** A `ApiException` recebe sua mensagem do corpo `ErrorDetails` sempre que a resposta contém um JSON reconhecível com `Message`, **em qualquer status**. Se o corpo não for reconhecível (por exemplo, uma página HTML de proxy, um 502/504 ou um corpo vazio), o cliente usa uma mensagem técnica padrão baseada no status. Consequência prática:

- **Diretriz:** só exiba `ApiException.Message` ao usuário quando o status for um dos que a API declara com `ErrorDetails` de negócio (hoje, 422). Para qualquer outro status, use a mensagem amigável e fixa da página.
- **Diretriz:** mesmo no 422, a mensagem só é segura para a tela se a API a escreveu para o usuário final. Mensagens de domínio devem ser redigidas assim; não repasse texto de exceção técnica.
- **Pendente:** o cliente não distingue, na `ApiException`, “mensagem veio do `ErrorDetails`” de “mensagem técnica padrão”. Se isso for necessário, exige uma propriedade ou um tipo de exceção no contrato da Foundation (ver seção 1.13).

Uma `ApiUnauthorizedException` não deve ser capturada por esses `catch` genéricos: ela representa expiração ou ausência de sessão e deve seguir o handler global de autenticação. Quando uma página capturar `ApiException` de forma ampla, deve relançar a exceção se o status for 401 (por exemplo, com um filtro `when`).

As specs que registram exemplos de comportamento são [0013 — Distribuição do valor líquido](Specs/0013-vault-distribuicao-valor-liquido.md), [0015 — Atualização por cotação, parte 3](Specs/0015-vault-atualizacao-por-cotacao-parte-3.md), [0019 — Encerramento de movimento, parte 4](Specs/0019-vault-encerramento-de-movimento-parte-4.md) e [0021 — Consolidado, parte 3](Specs/0021-vault-consolidado-parte-3.md).

<!-- TOC --><a name="14-página-de-exceção-e-páginas-de-status"></a>

### 1.4. Página de exceção e páginas de status

Existe uma página global de exceção em `Presentation/Pages/Error.cshtml` e `Presentation/Pages/Error.cshtml.cs`. Ela é configurada em `Program.cs` por:

```csharp
app.UseExceptionHandler("/Error");
```

Sua mensagem é genérica: “Ocorreu um erro. Não foi possível concluir a operação. Tente novamente em instantes.”. A página aceita GET e POST porque o middleware reexecuta a requisição original no caminho `/Error`.

Não há `UseStatusCodePages`, uma página específica para 404/403 ou um mapeamento local de páginas por código de status documentado no `Vault.Presentation`. Respostas de 401 são tratadas pelo `ApiUnauthorizedExceptionHandler`; erros inesperados passam pela página `/Error`.

<!-- TOC --><a name="15-exibição-de-errordetails-e-erros-por-campo"></a>

### 1.5. Exibição de `ErrorDetails` e erros por campo

Na API, os controllers declaram `ErrorDetails` para respostas de erro, principalmente 404, 422 e 500. Esse contrato é documentado nos controllers de `Service` e nas specs de cada funcionalidade.

No `Vault.Presentation`, porém, o comportamento atual não desserializa `ErrorDetails` em uma coleção de erros por campo. Para erros de validação do formulário, os campos são validados localmente por Data Annotations e `ModelState`, e as mensagens aparecem junto aos campos por meio dos helpers padrão do Razor Pages.

Quando a API devolve 422, algumas páginas usam apenas `ApiException.Message` como mensagem geral no topo do formulário. Portanto, a estratégia atual é:

1. erro conhecido de entrada no Web: `ModelState` e mensagem junto ao campo;
2. erro 422 devolvido pela API: mensagem geral, quando a página opta por preservar `ApiException.Message`;
3. erro de infraestrutura ou outro status: mensagem geral e amigável;
4. 401: fluxo global de autenticação, sem mensagem de validação no formulário.

Não existe atualmente uma estratégia implementada para converter propriedades de `ErrorDetails` em `ModelState` por nome de campo. Se essa experiência for necessária, ela deve ser implementada em um ponto compartilhado do cliente HTTP ou em um helper de página, preservando uma mensagem geral para erros sem campo.

<!-- TOC --><a name="16-tratamento-de-exceções-na-api-e-persistência"></a>

### 1.6. Tratamento de exceções na API e persistência

O `Vault.Service` registra `builder.Services.AddExceptionHandlers()` em [Service/Program.cs](../Service/Program.cs) e executa `app.UseExceptionHandler(...)`. A extensão vem de `Lumia.Foundation.AspNetCore.Api` **0.32.1**, no namespace `LumiaFoundation.AspNetCore.Api.Commons.Extensions`, e registra, nesta ordem, o `DomainExceptionHandler` e o `UnhandledExceptionHandler`. O `Service.csproj` do Vault referencia esse pacote diretamente; não referencia `Lumia.Foundation.AspNetCore`.

O pacote legado `Lumia.Foundation.AspNetCore` **ainda existe** na solução da Foundation, na versão **0.31.0**, mas está marcado como obsoleto e recomenda `Lumia.Foundation.AspNetCore.Api` e `Lumia.Foundation.AspNetCore.Web`. O projeto de teste legado da própria Foundation ainda o referencia; nenhum projeto de produção do Vault o referencia. Portanto, o item 5 da seção 1.13 está resolvido: para o Vault vale o pacote `AspNetCore.Api`, não há uma equivalência pendente a validar.

- Exceções de domínio que herdam de `DomainBaseException` são tratadas pelo `DomainExceptionHandler`. No contrato atual da Foundation, `EntityNotFoundException` resulta em 404, enquanto `EntityInUseException` e `CommandValidationException` resultam em **422**. A resposta é `ErrorDetails`.
- Exceções não mapeadas seguem para `UnhandledExceptionHandler`, são registradas como falhas de servidor e resultam em 500 com `ErrorDetails`.
- `DbUpdateException` não possui tratamento específico no `Vault.Service` nem na estratégia atual da Foundation. Portanto, uma falha de persistência desse tipo segue o caminho de exceção não mapeada e resulta em **500**. A tradução de violação de unicidade (por exemplo, para 422) é uma pendência, não um comportamento implementado.

<!-- TOC --><a name="quadro-exceção-http"></a>

#### Quadro exceção → HTTP

| Exceção | HTTP hoje | Tratada por | Observação |
| --- | --- | --- | --- |
| `EntityNotFoundException` | 404 | `DomainExceptionHandler` | Recurso inexistente **ou** de outro usuário (ver seção 9, escopo por usuário). |
| `EntityInUseException` | **422** | `DomainExceptionHandler` | Não é 409. Qualquer documento que cite 409 como “intenção observada” está desatualizado. |
| `CommandValidationException` | 422 | `DomainExceptionHandler` | Corpo `ErrorDetails` com mensagem geral. |
| `DbUpdateException` | **500** | `UnhandledExceptionHandler` | Sem tradução hoje, inclusive para violação de unicidade. Pendente (item 8 da seção 1.13). |
| Demais exceções | 500 | `UnhandledExceptionHandler` | Registrada como falha de servidor; corpo `ErrorDetails` genérico. |

No Web, o cliente HTTP converte qualquer resposta de erro em `ApiException`; o 401 vira `ApiUnauthorizedException` e segue o handler global nas requisições HTTP do host (seção 1.8). O contrato de `ErrorDetails` e a forma de responder manualmente, por exemplo ao produzir um 429, estão na seção 2.1.

Essa decisão e a ordem dos handlers estão registradas em [ADR 0024 — Fim do “loga e relança” nas queries](adr/0024-fim-do-log-e-relancamento-nas-queries.md). As implementações relevantes da Foundation estão ligadas na seção 3.

<!-- TOC --><a name="17-saveasync-e-d2-b"></a>

### 1.7. `SaveAsync` e D2-B

Na versão da Foundation usada pelo Vault, `LumiaFoundation.EFRepository.Repository.BaseRepositoryManager.SaveAsync(CancellationToken)` é público e **não é `virtual`**. A implementação apenas delega para `RepositoryContext.SaveChangesAsync(cancellationToken)`.

Consequentemente, uma estratégia que dependa de sobrescrever `SaveAsync` (D2-B) não pode ser aplicada por herança direta nessa versão. O contrato usado pelo Vault é `Application/Repositories/IRepositoryManager`, que declara `Task SaveAsync(CancellationToken)`; qualquer comportamento adicional, como a **tradução de erro de banco** (por exemplo, `DbUpdateException` por unicidade em exceção de domínio), precisa ser implementado por composição, decorator, alteração da Foundation ou outro ponto explícito de extensão.

**Pendente (decisão, exige ADR).** Opções a discutir para a tradução de `DbUpdateException`:

1. decorator de `IRepositoryManager` na camada de infraestrutura, que captura `DbUpdateException` em `SaveAsync` e a converte em exceção de domínio;
2. tratamento no caso de uso (Application), com `try/catch` em torno de `SaveAsync` onde a unicidade for relevante;
3. novo `IExceptionHandler` no host da API, que mapeia `DbUpdateException` para 422 (mais simples, mas o handler não conhece a regra de negócio violada);
4. tornar `SaveAsync` `virtual` na Foundation (sinalizar impacto em Web e Api, conforme as restrições do projeto).

<!-- TOC --><a name="18-origem-e-dependência-do-apiunauthorizedexceptionhandler"></a>

### 1.8. Origem e dependência do `ApiUnauthorizedExceptionHandler`

`ApiUnauthorizedExceptionHandler` vem da Foundation, no pacote `Lumia.Foundation.AspNetCore.Web`, namespace `LumiaFoundation.AspNetCore.Web.ClientAuthentication`. Ele não é implementado pelo Vault.

O handler reconhece `ApiException` com status 401, resolve `IApiSignInService` pelo `HttpContext.RequestServices`, chama `SignOutAsync` e redireciona para o caminho configurado (por padrão, o login). Portanto, ele depende da infraestrutura de autenticação/sessão registrada pelo host, embora não acesse `ISession` diretamente.

No Vault, essa dependência é montada por `AddSessionTokenStore()`, `AddApiSessionCookie()`, `AddApiSignInService()` e pelo middleware `UseSession()` antes de `UseExceptionHandler()`. O `SessionTokenStore` da Foundation usa a sessão para guardar o token; o handler encerra essa sessão por meio do `IApiSignInService`. A composição do Vault está em [Presentation/Program.cs](../Presentation/Program.cs) e [Presentation/Extensions/PresentationExtensions.cs](../Presentation/Extensions/PresentationExtensions.cs). `AddApiSignInService()` registra `IApiSignInService` e, por padrão, `ITokenStore` como `SessionTokenStore`; não registra, por si só, a sessão ASP.NET, o cookie nem o middleware. `AddApiSessionCookie()` é uma extensão local do Vault, não uma extensão da Foundation. Um segundo host precisa registrar o cliente, um token store adequado, o esquema de autenticação local e os serviços/middlewares de que esse store depende. A seção 2.3 detalha o contrato e os requisitos.

**Blazor Server:** uma exceção lançada durante um evento de uma ilha interativa não percorre o pipeline HTTP que contém `ApiUnauthorizedExceptionHandler`; o circuito precisa tratar o 401 e iniciar a navegação ao login. O handler continua aplicável ao SSR e a outras requisições HTTP. O [ADR 0014 do Flashcard](https://github.com/JeannAndrade/Flashcard/blob/main/Docs/adr/0014-autenticacao-nas-ilhas-interativas.md) confirma essa distinção e prevê um store compartilhado por `sid`. O store por `sid` é uma mudança planejada na Foundation, não uma implementação disponível hoje. Assim, a diferença entre SSR e ilhas está esclarecida; a validação do novo fluxo e do sign-out por `sid` continua pendente para o spike da fase 2.

<!-- TOC --><a name="19-convenções-dos-clientes-de-recurso"></a>

### 1.9. Convenções dos clientes de recurso

*Diretriz*, derivada do padrão já usado em `MovimentoApi` e nos demais clientes.

- **Um cliente por recurso.** Interface (`IXxxApi`) e implementação (`XxxApi`) ficam juntas na pasta do recurso e são registradas com `AddApiResourceClient<IXxxApi, XxxApi>()` em `AddApiResourceClientService`.
- **Dependência única:** a implementação recebe `IApiConnection`. Páginas, componentes e serviços de apresentação dependem apenas da interface; nenhum `HttpClient` direto fora do cliente.
- **`CancellationToken` em todos os métodos** do cliente, repassado até a chamada HTTP. Nas Razor Pages, use o token da requisição (`HttpContext.RequestAborted`).
- **Contratos explícitos.** Os tipos de requisição e resposta pertencem ao contrato entre Web e API; o Web não referencia entidades de domínio nem de persistência da API.
- **Escopo por usuário é da API.** O usuário é identificado pelo token enviado pelo cliente. O Web não monta filtros de segurança nem envia identificador de usuário como forma de autorização. Um recurso de outro usuário deve aparecer para o Web como 404, nunca como dado.
- **Sem tratamento de erro dentro do cliente de recurso.** O cliente deixa a `ApiException` subir; quem decide a mensagem é a página (seção 3).

<!-- TOC --><a name="110-testes-do-lado-do-consumidor"></a>

### 1.10. Testes do lado do consumidor

*Diretriz.* Toda decisão de cliente deve poder ser testada sem subir a API.

- **Páginas:** testes unitários com um duplo de `IXxxApi` (fake ou mock). Cobrir, no mínimo: sucesso, `ApiException` com status 422 (mensagem preservada), `ApiException` com outro status (mensagem amigável fixa) e 401 (a exceção não pode ser engolida).
- **Clientes de recurso:** testes com um duplo de `IApiConnection`, verificando método, rota, corpo e propagação do `CancellationToken`.
- **Composição (`Program.cs`):** um teste de integração do host que verifique que `IXxxApi` resolve pelo contêiner e que a ordem `UseSession()` → `UseExceptionHandler()` é mantida. Um 401 simulado da API deve resultar em redirecionamento ao login com a sessão limpa.
- **Contrato Web × API:** quando o contrato de um recurso muda, o teste do cliente deve quebrar antes da página; por isso a verificação de rota e corpo acima.

<!-- TOC --><a name="111-logging-e-observabilidade-no-consumidor"></a>

### 1.11. Logging e observabilidade no consumidor

*Diretriz (a confirmar contra o código atual).*

- Falhas de API capturadas localmente pela página devem ser registradas em nível adequado (aviso para 4xx esperados, erro para 5xx), com status e identificador da operação, **sem** token, cabeçalhos de autorização nem corpo completo da requisição.
- Exceções não capturadas chegam ao handler global, que registra e exibe `/Error`; a página não deve registrar de novo o que vai relançar (mesmo princípio do ADR 0024: sem “loga e relança”).
- Logging estruturado por NLog, com propriedades (`Recurso`, `Operacao`, `StatusHttp`) em vez de texto concatenado.
- Mensagens técnicas (`ApiException.Message` padrão) servem para log, não para a tela.

<!-- TOC --><a name="112-checklist-para-um-novo-recurso-consumidor"></a>

### 1.12. Checklist para um novo recurso consumidor

1. Existe o contrato (requisição e resposta) documentado na spec do recurso, com os status e o `ErrorDetails` declarados nos controllers.
2. Interface `IXxxApi` e implementação `XxxApi` criadas com `IApiConnection`, todos os métodos com `CancellationToken`.
3. Registro adicionado em `AddApiResourceClientService`.
4. Página captura `ApiException` apenas onde há falha localizada, usa mensagem fixa (ou `Message` somente em 422) e **não captura 401**.
5. Exibição do erro com `ErrorMessage` e alerta com `role="alert"`; erros de campo por `ModelState`.
6. Testes da página e do cliente conforme a seção 10.
7. Logging conforme a seção 11, sem dados sensíveis.
8. Spec do recurso atualizada com o comportamento de erro observado.

<!-- TOC --><a name="113-pontos-a-validar"></a>

### 1.13. Pontos a validar

| # | Ponto | Situação |
| --- | --- | --- |
| 1 | Quadro exceção → HTTP: `EntityInUseException` é 422, não 409. | Resolvido neste guia. |
| 2 | `ApiException.Message` técnica quando o corpo não é reconhecível; diferenciar a origem da mensagem no cliente. | Aberto (seção 3). |
| 3 | `SaveAsync` não é `virtual`; tradução de erro de banco exige composição. | Resolvido como fato; decisão de solução aberta (seção 7). |
| 4 | Padrão de consumo de API no `Vault.Presentation` é a referência para novos consumidores. | Resolvido neste guia (seções 9 a 12). |
| 5 | `AddExceptionHandlers()` vem do pacote legado `Lumia.Foundation.AspNetCore`, enquanto a recomendação é `AspNetCore.Api`. | Resolvido: o Vault usa diretamente `Lumia.Foundation.AspNetCore.Api` 0.32.1; o pacote antigo não é dependência de produção do Vault. |
| 6 | Fluxo do 401 nas ilhas Blazor (ADR 0014): a exceção chega ao `ApiUnauthorizedExceptionHandler` ou o componente a trata? | Resolvido como contrato: evento do circuito não chega ao handler HTTP; o circuito trata o 401. Spike e testes continuam pendentes no Flashcard. |
| 7 | Dependência do handler em `IApiSignInService` ao ser validada pelo Flashcard. | Contrato esclarecido na seção 8: o registro isolado não basta; a validação integrada, incluindo store por `sid`, continua pendente no spike. |
| 8 | `DbUpdateException` resulta em 500; definir tradução de violação de unicidade. | Aberto; exige ADR (seção 7). |

<!-- TOC --><a name="resumo-por-tópico"></a>

### Resumo por tópico

| Tópico | Orientação atual |
| --- | --- |
| `ApiException` | Captura local nas Razor Pages; mensagem geral em `ErrorMessage`; `Message` vem de `ErrorDetails` em qualquer status, ou é técnica se o corpo não for reconhecível, então só exibir em 422; 401 segue o handler global. |
| Cliente tipado | `Program.cs` registra `AddLumiaApiClient`; `PresentationExtensions` registra clientes de recurso baseados em `IApiConnection`; um cliente por recurso, com `CancellationToken`. |
| Sessão e refresh | `SessionTokenStore`, cookie `Vault.Auth`, refresh em `/api/token/refresh` e `ApiUnauthorizedExceptionHandler`; `UseSession()` antes de `UseExceptionHandler()`. |
| Exceção/status | Existe `/Error`; não existe `StatusCodePages` nem página específica por status. |
| `ErrorDetails` | Contrato existe na API, mas a Presentation atualmente o apresenta como mensagem geral, não por campo. |
| Exceções da API | 404 (`EntityNotFoundException`), 422 (`EntityInUseException`, `CommandValidationException`); exceções não mapeadas e `DbUpdateException` resultam em 500. |
| Origem dos handlers | `AddExceptionHandlers()` do pacote `Lumia.Foundation.AspNetCore.Api` 0.32.1; o pacote `Lumia.Foundation.AspNetCore` é legado/obsoleto. |
| `SaveAsync` | `BaseRepositoryManager.SaveAsync(CancellationToken)` existe, mas não é `virtual`; extensão por composição. |
| Ilhas Blazor | 401 em evento interativo é tratado no circuito, não pelo handler HTTP; o store por `sid` ainda é trabalho futuro da Foundation. |
| Testes e logging | Fakes de `IXxxApi`/`IApiConnection`; log sem “loga e relança” e sem dados sensíveis. |

<!-- TOC --><a name="2-integração-do-vaultservice-api-com-o-lumia"></a>

## 2. Integração do `Vault.Service` (API) com o Lumia

Este bloco explica o lado servidor: composição do host, contratos HTTP, persistência, autenticação e logging fornecidos ou influenciados pelas bibliotecas do Lumia.

<!-- TOC --><a name="21-host-da-api-e-contratos-do-aspnetcoreapi"></a>

### 2.1. Host da API e contratos do `AspNetCore.Api`

<!-- TOC --><a name="composição-e-pipeline-atuais-do-vault"></a>

#### Composição e pipeline atuais do Vault

[Service/Program.cs](../Service/Program.cs) é a referência de composição atual. Registra persistência, validação, Identity/JWT, serviços da Application, logger, health checks, CORS, controllers, handlers e OpenAPI. O pipeline atual, em ordem, inclui:

1. `UseExceptionHandler`;
2. `UseHsts`;
3. `UseStaticFiles`;
4. `UseForwardedHeaders(ForwardedHeaders.All)`;
5. `UseCors("CorsPolicy")`;
6. `UseAuthentication` e `UseAuthorization`;
7. endpoints OpenAPI/Scalar, controllers e health checks.

O `UseHttpsRedirection` está comentado. O Vault tem um `ConfigureOpenApi("Vault API", "v1")` e, logo depois, outro `AddOpenApi` que define o mesmo título e versão; a segunda configuração é duplicada no código atual.

<!-- TOC --><a name="openapi"></a>

#### OpenAPI

- `ConfigureOpenApi(this IServiceCollection services, string title, string version)` registra o documento OpenAPI e configura seu título/versão.
- `MapOpenApiDevTools(this IEndpointRouteBuilder endpoints, Func<IServiceProvider, bool>? shouldExpose = null)` mapeia o documento OpenAPI e a UI Scalar se `shouldExpose` permitir.
- Se `shouldExpose` não for informado, a própria extensão verifica `IWebHostEnvironment.IsDevelopment()`. Portanto, a chamada sem `if` no `Program.cs` do Vault **já restringe os endpoints ao ambiente Development**. O parâmetro opcional permite substituir essa decisão.

<!-- TOC --><a name="filtros-e-registro-por-convenção"></a>

#### Filtros e registro por convenção

- `AddValidationFilters()` apenas registra `DtoNotEmptyValidationAttribute` como scoped no DI; não o aplica globalmente. O filtro verifica corpo/DTO ausente e `ModelState` inválido (responde 400 ou 422). O controller precisa aplicá-lo, como fazem os endpoints de autenticação por `[ServiceFilter]`. O Vault desativa o filtro automático de `ModelState` de `[ApiController]`.
- `AddServicesFromAssembly(Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)` procura classes concretas, não aninhadas nem geradas, e registra somente a primeira interface do mesmo assembly cujo nome seja `I` + nome da classe. O padrão é scoped. O Vault passa o assembly da Application; classes sem a interface com esse nome convencional ficam de fora.
- `IDateTimeProvider` expõe `GetDateTime()`. O `Service` registra `UtcDateTimeProvider`; a Presentation também registra essa implementação para suas páginas.
- `ConfigureCors()` no Vault registra uma política permissiva (`AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader`), ativada pelo `UseCors`. CORS só é necessário quando um navegador chama a API de outra origem. Para o desenho do Flashcard — navegador → Presentation e Presentation → API servidor a servidor — não é necessário. A spec do host do Flashcard ainda marca essa decisão como pendente de confirmação.

<!-- TOC --><a name="usuário-atual-e-errordetails"></a>

#### Usuário atual e `ErrorDetails`

`BaseApiController.GetCurrentUserId()` **não lê uma claim diretamente**: lê `HttpContext.Items["UserId"]`, converte o valor para `Guid` e lança `InvalidOperationException` se estiver ausente ou inválido. Se escapar da action, o handler não mapeado responde 500. O `RetrieveUserIdFromTokenAttribute`, da Foundation Auth, valida o bearer token e obtém o identificador da claim `ClaimTypes.NameIdentifier` (URI `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`); em seguida, grava o valor em `Items["UserId"]`.

`ErrorDetails` pertence ao namespace `LumiaFoundation.Abstractions.ErrorModel` e contém `StatusCode` (`int`), `Message` (`string?`) e `ExceptionType` (`string` obrigatório). Não tem construtor com parâmetros; pode ser criado com inicializador de objeto. `ToString()` serializa a instância com `JsonSerializer.Serialize` sem opções, então os handlers atuais escrevem as propriedades em PascalCase (`StatusCode`, `Message`, `ExceptionType`). Não há helper próprio que escreva diretamente no `HttpResponse`.

Para responder manualmente, por exemplo a um 429 produzido pelo rate limiter, pode-se usar o mesmo objeto e serialização dos handlers:

```csharp
var error = new ErrorDetails
{
    StatusCode = StatusCodes.Status429TooManyRequests,
    Message = "Muitas requisições. Tente novamente mais tarde.",
    ExceptionType = "RateLimitExceeded"
};

context.Response.StatusCode = error.StatusCode;
context.Response.ContentType = "application/json";
await context.Response.WriteAsync(error.ToString(), cancellationToken);
```

Se usar `WriteAsJsonAsync` ou outro serializador com opções Web, confirme o casing resultante: não é a mesma serialização de `ErrorDetails.ToString()` usada pelos handlers atuais. O rate limiter também deve preencher `Retry-After`, conforme o ADR 0013 do Flashcard.

<!-- TOC --><a name="22-persistência-provedores-migrations-e-health-checks"></a>

### 2.2. Persistência, provedores, migrations e health checks

<!-- TOC --><a name="addpersistence-e-seleção-do-banco"></a>

#### `AddPersistence` e seleção do banco

Em [Persistence/Extensions/ServiceExtensions.cs](../Persistence/Extensions/ServiceExtensions.cs), `AddPersistence(IConfiguration)` chama `ConfigureMariaDbDatabase<TContext>` da Foundation para `VaultDbContext` e `VaultIdentityDbContext`. O primeiro parâmetro genérico é o contexto; os demais são um `DbConnectionHelper` e a assembly de migrations (`"Persistence"`). O Vault usa `ConfigureMariaDbDatabase<VaultDbContext>(...)`, que configura o provider pelo `UseMySql` com `MariaDbServerVersion`.

Não existe uma única chave `ConnectionStrings:*` no Vault. O helper monta a connection string a partir destas chaves obrigatórias de configuração/ambiente:

| Chave | Uso |
| --- | --- |
| `VAULT_DB_HOST` | Host do banco |
| `VAULT_DB_PORT` | Porta |
| `VAULT_DB_USER` | Usuário |
| `VAULT_DB_PASS` | Senha |
| `VAULT_DB_NAME` | Nome do banco |
| `DBMAJORVERSION`, `DBMINORVERSION`, `DBBUILDVERSION` | Versão MariaDB usada para configurar o provider |

As chaves são lidas por `IConfiguration`, portanto podem vir de variáveis de ambiente (`VAULT_DB_HOST`, etc.). No Compose, os serviços `api` e `migrations` recebem as mesmas credenciais e o mesmo nome de banco. Observação: o `DbConnectionHelper` da versão consultada imprime a connection string inteira no console em `Development`; não registre credenciais reais nesse ambiente sem corrigir esse comportamento.

`Lumia.Foundation.EFRepository` **0.14.1** declara providers para MariaDB, Oracle MySQL e PostgreSQL. O provider não é escolhido automaticamente: o host chama explicitamente `ConfigureMariaDbDatabase<T>`, `ConfigureMySqlDbDatabase<T>` ou `ConfigurePostgreSqlDatabase<T>`. A última usa `UseNpgsql`; portanto, PostgreSQL é suportado, embora não seja o provider atual do Vault.

<!-- TOC --><a name="dois-contextos-e-migrations"></a>

#### Dois contextos e migrations

`VaultDbContext` e `VaultIdentityDbContext` recebem a mesma configuração de conexão e apontam ao mesmo banco. O segundo deriva de `LumiaFoundation.Auth.Persistence.IdentityContext`; o primeiro de `RepositoryContext`. As migrations ficam em diretórios/snapshots separados, mas são produzidas pela assembly `Persistence` e compartilham a tabela `__EFMigrationsHistory` padrão.

O Vault gera e executa **um bundle por contexto**, explicitando `--context VaultDbContext` e `--context VaultIdentityDbContext`. O serviço `migrations` executa primeiro o bundle de domínio e depois o de identidade. Consulte [Dockerfile](../Dockerfile), [docker/migrate.sh](../docker/migrate.sh), [ContextFactory](../Persistence/ContextFactory) e [ADR 0001 — Migrations por bundle](adr/0001-migrations-por-bundle.md).

<!-- TOC --><a name="verificação-de-saúde-do-banco"></a>

#### Verificação de saúde do banco

O `EFRepository` fornece `AddDbContextHealthCheck<TContext>(name = "banco-de-dados", failureStatus = null, tags = null, timeout = null)`. A checagem tenta conectar ao banco; o timeout padrão é 5 segundos. A Foundation não atribui tag readiness automaticamente: o host precisa passar `tags: [LumiaHealthCheckTags.Ready]`.

O [Service/Extensions/ServiceExtensions.cs](../Service/Extensions/ServiceExtensions.cs) registra checks com essa tag para `VaultDbContext` (`banco-dominio`) e `VaultIdentityDbContext` (`banco-identidade`). `MapLumiaHealthChecks()` não aceita rotas configuráveis:

| Rota | Política | Resposta |
| --- | --- | --- |
| `/health/live` | Liveness; não executa nenhuma health check. | Anônima; texto simples `Healthy` enquanto o processo responde. |
| `/health/ready` | Readiness; executa somente checks com a tag `ready`. No Vault, verifica os dois contextos. | Anônima; texto simples `Healthy` ou `Unhealthy`, sem detalhes das checks; `Unhealthy` resulta em HTTP 503. |

As rotas são constantes da Foundation (`LumiaHealthCheckPaths.Live` e `.Ready`). Para usar outros caminhos, é preciso mapear health checks próprios; `MapLumiaHealthChecks()` não recebe parâmetros de rota. A readiness verifica conectividade, não se migrations foram aplicadas — isso é garantido no Vault pela ordem do Compose.

<!-- TOC --><a name="23-cliente-http-autenticação-e-antiforgery"></a>

### 2.3. Cliente HTTP, autenticação e antiforgery

<!-- TOC --><a name="addlumiaapiclient-refresh-e-métodos-http"></a>

#### `AddLumiaApiClient`, refresh e métodos HTTP

Assinatura da Foundation:

```csharp
AddLumiaApiClient(
    string baseAddress,
    Action<HttpClient>? configureClient = null,
    Action<AuthenticationClientOptions>? configureAuthentication = null)
```

O método registra `IAuthenticationApi`, `IApiConnection` e o pipeline com `BearerTokenHandler`; **não** registra `ITokenStore`. `AuthenticationClientOptions` possui `LoginPath` (`/api/authentication/login`), `RefreshPath` (`/api/authentication/refresh`) e `RegisterPath` (`/api/authentication`) configuráveis. O Vault sobrescreve `RefreshPath` para `/api/token/refresh`.

O handler lê o token do store, envia o access token como bearer e, ao receber 401, serializa o refresh concorrente por `SemaphoreSlim`, relê o token atual e chama o refresh. Se renovar, atualiza o store e repete a requisição original uma vez. Falha de refresh limpa o store e deixa o 401 original seguir como `ApiException`; cancelamento é propagado. A serialização é pelo handler compartilhado pelo `HttpClientFactory`, não por usuário/sid. Um store customizado compartilhado por usuários precisa manter isolamento próprio.

`IApiConnection` tem duas sobrecargas, ambas `SendAsync(HttpMethod method, string requestUri, object? body = null, CancellationToken ct = default)`: uma retorna `Task<T?>` desserializado de JSON e a outra não retorna corpo. Aceita qualquer `HttpMethod`, incluindo `HttpMethod.Patch`, mas sempre serializa o corpo como JSON e não oferece leitura binária nem `Stream`. Para imagens do ADR 0016, é necessário adicionar um contrato/cliente específico ou ampliar a Foundation.

<!-- TOC --><a name="store-de-tokens-e-401"></a>

#### Store de tokens e 401

O contrato `ITokenStore` é:

```csharp
ValueTask<AuthenticationToken?> GetAsync(CancellationToken cancellationToken = default);
ValueTask SetAsync(AuthenticationToken token, CancellationToken cancellationToken = default);
ValueTask ClearAsync(CancellationToken cancellationToken = default);
```

`AuthenticationToken` contém access e refresh token. As implementações prontas atuais são `InMemoryTokenStore` e `SessionTokenStore`. É possível implementar um store indexado por `sid`, mas o contrato **não recebe o sid como argumento**: o store precisa obtê-lo de um accessor/escopo do host. No Blazor Server, `HttpContext`/`ISession` não são uma fonte confiável dentro do circuito, por isso o `SessionTokenStore` não serve para a ilha. Como `AddApiSignInService()` registra `SessionTokenStore` incondicionalmente, um host que use store próprio precisa substituir esse registro depois da chamada (ou registrar sua própria composição).

`ApiUnauthorizedExceptionHandler` trata apenas `ApiException` 401 em requisições HTTP, resolve `IApiSignInService` a partir do `HttpContext`, executa `SignOutAsync` e redireciona (padrão `/Login?expired=true`). `AddApiSignInService()` registra o serviço scoped e `SessionTokenStore` singleton, mas **não** registra cookie, sessão, `IHttpContextAccessor`, middleware `UseSession`, autenticação do host ou handler de exceção. O Vault faz esses registros explicitamente. Para outro host, cookie e sessão só bastam se essa for a estratégia do host; para Blazor interativo, o ADR 0014 prevê um store por `sid` e tratamento do 401 no circuito. Esse spike ainda não está implementado na Foundation/Flashcard.

<!-- TOC --><a name="cadastro-login-refresh-e-bloqueio-de-tentativas"></a>

#### Cadastro, login, refresh e bloqueio de tentativas

No Vault, as rotas da API são:

| Operação | Rota | Implementação |
| --- | --- | --- |
| Cadastro | `POST /api/authentication` | `AuthenticationController.RegisterUser`; cria via `UserManager` e responde 201 ou 422. |
| Login | `POST /api/authentication/login` | `AuthenticationController.Authenticate`; responde com tokens ou 401. |
| Refresh | `POST /api/token/refresh` | `TokenController.Refresh`; valida par de tokens e emite novo par. |

As páginas Web são `/Register`, `/Login` e `/Logout`. O pacote Auth oferece `IAuthenticationService.RegisterUser`, criação de usuário pelo `UserManager`, atribuição opcional de roles válidas e validação de senha do Identity. O Vault configura senha com mínimo de 10 caracteres, maiúscula, minúscula, dígito, caractere não alfanumérico e email único.

**Bloqueio por conta não está habilitado pelo fluxo atual.** Apesar de o Identity ter campos de lockout, o `AuthenticationService.ValidateUser` chama `UserManager.CheckPasswordAsync`; isso não incrementa tentativas falhas como `SignInManager.PasswordSignInAsync(..., lockoutOnFailure: true)`. Apenas configurar `Lockout.MaxFailedAccessAttempts` não basta. A limitação por IP/cadastro/login/refresh descrita no [ADR 0013 do Flashcard](https://github.com/JeannAndrade/Flashcard/blob/main/Docs/adr/0013-cadastro-aberto-e-limite-de-taxa.md) é separada e ainda não está implementada nos hosts atuais.

<!-- TOC --><a name="cookie-e-antiforgery-no-vault"></a>

#### Cookie e antiforgery no Vault

`AddApiSessionCookie()` é código do Vault. Configura o cookie `Vault.Auth` com `HttpOnly = true`, `SameSite = Lax`, login `/Login`, validade de sete dias e sem expiração deslizante. Não define `SecurePolicy` nem recebe opções/configuração: aplica-se o padrão do ASP.NET Core, `CookieSecurePolicy.SameAsRequest`. `HttpOnly` e `SameSite` estão fixos no método; para configurá-los hoje é preciso alterar o método ou aplicar configuração posterior de `CookieAuthenticationOptions`. A proposta do [ADR 0021 do Flashcard](https://github.com/JeannAndrade/Flashcard/blob/main/Docs/adr/0021-seguranca-de-borda-e-premissa-de-implantacao.md) quer as flags configuráveis, mas isso não descreve a implementação atual do Vault.

Nas Razor Pages, os formulários `method="post"` usam o antiforgery padrão do ASP.NET Core: o Form Tag Helper emite o token e os handlers POST o validam. A página `/Error` é a exceção explícita com `[IgnoreAntiforgeryToken]`; não há desativação geral do antiforgery no Vault.

<!-- TOC --><a name="24-logging-e-lumiafoundationlogger"></a>

### 2.4. Logging e `Lumia.Foundation.Logger`

`ILoggerManager` fica em `LumiaFoundation.Logger.Contracts`. Seus métodos são `LogDebug`, `LogWarn`, `LogInfo` e `LogError`, com overloads que recebem mensagem e parâmetros; `LogWarn` também aceita exceção, e `LogError` aceita exceção com ou sem parâmetros. Não existe `LogWarning`.

Os overloads parametrizados passam os argumentos ao NLog. Uma mensagem-template como `LogInfo("Operação {OperationId}", operationId)` cria a propriedade nomeada no evento; ela aparece no JSON se o layout inclui as propriedades, como o `layout` atual do Vault com `includeEventProperties="true"` e `${all-event-properties}`. Interpolação antecipada (`$"Operação {operationId}"`) produz apenas a mensagem renderizada, sem propriedade estruturada.

Detalhes de versão:

- A Foundation atual declara `Lumia.Foundation.Logger` **0.6.0**, `TargetFramework` **net10.0** e `NLog.Extensions.Logging` **6.3.0**. O restore consultado resolve `NLog` **6.2.2**.
- O assembly para a extensão do NLog é **`LumiaFoundation.Logger`** (sem pontos), por exemplo `<add assembly="LumiaFoundation.Logger" />` dentro de `<extensions>`.
- Os renderizadores `${lumia-traceid}` e `${lumia-ownerid}` foram adicionados no Logger 0.6.0; o segundo lê a tag `lumia.owner_id` da `Activity`. Sem a seção `<extensions>` no `nlog.config`, o NLog não reconhece esses renderizadores.
- O Vault ainda referencia Logger **0.5.1** nos projetos Web. Seu `nlog.config` não declara `<extensions>` nem os renderizadores 0.6.0; portanto, não se deve documentar esses campos como comportamento já ativo no Vault.
- `ConfigureLoggerService()` registra somente `ILoggerManager` como singleton. O Vault chama `LoggerManager.LoadConfigurationFromFile(...)`; não chama `AddNLog()`. `AddNLog()` é a integração do provider com `Microsoft.Extensions.Logging`, prevista nas specs do Flashcard, e não é requisito para o wrapper direto `ILoggerManager`.
- O projeto de teste da biblioteca é `LumiaFoundation.Logger.Test`.

Na proposta do Flashcard, `AddNLog()` integra o NLog aos logs do framework; a spec recomenda carregar o `nlog.config`, limpar os providers padrão e então adicionar o provider NLog, evitando dupla emissão e filtros de nível inconsistentes. Essa integração ainda é parte da especificação do host, não do `Program.cs` atual do Flashcard.

<!-- TOC --><a name="3-referências-de-implementação-e-implantação"></a>

## 3. Referências de implementação e implantação

| Assunto | Arquivos |
| --- | --- |
| Host e pipeline da API | [Service/Program.cs](../Service/Program.cs), [Service/Extensions/ServiceExtensions.cs](../Service/Extensions/ServiceExtensions.cs), [Service.csproj](../Service/Service.csproj) |
| Persistência e conexão | [Persistence/Extensions/ServiceExtensions.cs](../Persistence/Extensions/ServiceExtensions.cs), [VaultDbContext](../Persistence/Context/VaultDbContext.cs), [VaultIdentityDbContext](../Persistence/Context/VaultIdentityDbContext.cs), [fábricas de contexto](../Persistence/ContextFactory) |
| Consumidor Web e auth | [Presentation/Program.cs](../Presentation/Program.cs), [Presentation/Extensions/PresentationExtensions.cs](../Presentation/Extensions/PresentationExtensions.cs), [Login](../Presentation/Pages/Login.cshtml.cs), [Register](../Presentation/Pages/Register.cshtml.cs) |
| Rotas de autenticação da API | [AuthenticationController](../Service/Auth/AuthenticationController.cs), [TokenController](../Service/Auth/TokenController.cs) |
| Logging | [Service/nlog.config](../Service/nlog.config), [Presentation/nlog.config](../Presentation/nlog.config) |
| Build e implantação | [Dockerfile](../Dockerfile) (Dockerfile único multi-stage para API, Web e migrations), [compose.yaml](../compose.yaml), [Docs/Docker.md](Docker.md), [docker/migrate.sh](../docker/migrate.sh), [docker/healthcheck.sh](../docker/healthcheck.sh) |
| Exclusões de arquivos | [.gitignore](../.gitignore), [.dockerignore](../.dockerignore) |
| Foundation: API e EFRepository | [ServiceCollectionExtensions](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.AspNetCore.Api/Commons/Extensions/ServiceCollectionExtensions.cs), [OpenAPI service extensions](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.AspNetCore.Api/Commons/Extensions/OpenApiServiceExtensions.cs), [HealthCheckEndpointExtensions](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.AspNetCore.Api/Commons/Extensions/HealthCheckEndpointExtensions.cs), [OpenAPI endpoints](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.AspNetCore.Api/Commons/Extensions/OpenApiEndpointRouteBuilderExtensions.cs), [BaseApiController](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.AspNetCore.Api/Commons/BaseControllers/BaseApiController.cs), [ErrorDetails](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Abstractions/ErrorModel/ErrorDetails.cs), [EFRepository ServiceExtensions](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.EFRepository/Extensions/ServiceExtensions.cs), [HealthChecksBuilderExtensions](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.EFRepository/Extensions/HealthChecksBuilderExtensions.cs) |
| Foundation: consumidor, auth e logging | [Http.Client ServiceCollectionExtensions](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Http.Client/Extensions/ServiceCollectionExtensions.cs), [IApiConnection](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Http.Client/Services/IApiConnection.cs), [BearerTokenHandler](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Http.Client/Authentication/BearerTokenHandler.cs), [ITokenStore](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Http.Client/Authentication/ITokenStore.cs), [Auth ServiceExtensions](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Auth/Extensions/ServiceExtensions.cs), [ApiUnauthorizedExceptionHandler](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.AspNetCore.Web/ClientAuthentication/ApiUnauthorizedExceptionHandler.cs), [SessionTokenStore](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.AspNetCore.Web/ClientAuthentication/SessionTokenStore.cs), [ILoggerManager](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Logger/Contracts/ILoggerManager.cs), [Logger README](https://github.com/JeannAndrade/Lumia.Foundation/blob/main/LumiaFoundation.Logger/docs/readme.md) |

O `.gitignore` é baseado no modelo .NET e tem, entre outras entradas, `.env` e `/Presentation/wwwroot/lib/` para não versionar segredos locais nem bibliotecas restauradas do front-end. O `.dockerignore` também exclui `.env`, logs, artefatos `bin/obj`, projetos de teste e documentação do contexto de build.

<!-- TOC --><a name="31-diferenças-previstas-para-o-host-do-flashcard"></a>

### 3.1. Diferenças previstas para o host do Flashcard

O [ADR 0021](https://github.com/JeannAndrade/Flashcard/blob/main/Docs/adr/0021-seguranca-de-borda-e-premissa-de-implantacao.md) define implantação HTTP, sem proxy reverso e com a API em rede interna. O host planejado do Flashcard, por isso, não deve usar `UseHsts`, `UseHttpsRedirection` nem `UseForwardedHeaders`; o navegador chama a Presentation, não a API. A ausência de CORS também é proposta para esse desenho, mas ainda aguarda confirmação na spec 0004. Isso difere do `Service/Program.cs` atual do Vault, que habilita HSTS, forwarded headers e uma política CORS permissiva.

Esses pontos devem ser lidos como decisões/specs do Flashcard, não como funcionalidades já implementadas: a [spec 0004 do host Service](https://github.com/JeannAndrade/Flashcard/blob/main/Docs/spec/0004-flashcard-host-do-service.md) ainda marca CORS/OpenAPI como itens a confirmar, e o [Presentation/Program.cs atual do Flashcard](https://github.com/JeannAndrade/Flashcard/blob/main/Presentation/Program.cs) ainda contém `UseHsts()` e `UseHttpsRedirection()`, em desacordo com a premissa do ADR 0021. A especificação do host da Presentation (fase 2) e o spike de autenticação nas ilhas também continuam sendo trabalho futuro.

O [ADR 0022 do Flashcard](https://github.com/JeannAndrade/Flashcard/blob/main/Docs/adr/0022-ordem-de-implementacao.md) ainda lista a spec 0006 do host Presentation como entrega futura; não há `Docs/spec/0006` no checkout consultado.
