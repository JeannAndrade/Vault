# Integração entre `Vault.Presentation` e a API

Este documento descreve como o `Vault.Presentation` consome a API, mantém a sessão e apresenta erros ao usuário. Ele também serve de **referência para qualquer novo consumidor da API** (outro host Web, ilhas Blazor, novos recursos): mostra como o Vault usa os clientes do Lumia.Foundation e quais convenções seguir.

**Escopo.** O funcionamento interno das bibliotecas Lumia (como o cliente HTTP renova tokens, como os handlers são montados etc.) está documentado em `LumiaFoundation.AspNetCore/docs/readme.md`. Aqui ficam apenas os aspectos específicos de quem **consome** essas bibliotecas.

**Legenda de status** usada neste guia:

- **Implementado**: comportamento verificado no código do Vault.
- **Diretriz**: convenção recomendada para código novo; pode ainda não estar aplicada em todo o código existente.
- **Pendente**: decisão ou verificação em aberto (ver seção 13).

## 1. Registro do cliente HTTP e dos clientes de recurso

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

## 2. Autenticação, sessão, 401 e refresh

Também em `Presentation/Program.cs`:

- `AddSessionTokenStore()` armazena o token da API em sessão;
- `AddApiSessionCookie()` configura o cookie de autenticação do Web;
- `AddLumiaApiClient(... RefreshPath = "/api/token/refresh")` configura o caminho usado pelo cliente para renovar o token;
- `AddExceptionHandler<ApiUnauthorizedExceptionHandler>()` registra o tratamento de respostas não autorizadas.

As configurações detalhadas da sessão estão em `Presentation/Extensions/PresentationExtensions.cs`: a sessão usa `DistributedMemoryCache`, expira após oito horas e a chave do token é `Vault.ApiToken`. O cookie `Vault.Auth` dura até sete dias, com expiração deslizante desativada.

O pipeline chama `UseSession()` antes de `UseExceptionHandler()` porque o handler de 401 precisa limpar a sessão. Quando a API devolve 401 e o refresh não resolve a autenticação, a exceção de não autorizado não deve ser capturada pelas páginas como um erro comum; ela segue para `ApiUnauthorizedExceptionHandler`, que encerra a sessão e conduz o usuário ao login.

O ADR [0005 — Sessão e chaves do Web](adr/0005-sessao-e-chaves-do-web.md) registra que a sessão e as chaves do Data Protection não são persistidas. Reiniciar o Web exige novo login. O ADR [0006 — Configuração da Api](adr/0006-configuracao-da-api.md) documenta a configuração da API e do JWT.

**Ilhas Blazor (pendente).** O fluxo de 401 descrito acima vale para requisições tratadas pelo pipeline de Razor Pages. Para componentes Blazor hospedados nas páginas (ADR 0014), o caminho do 401 ainda não está descrito neste guia: não está documentado se a exceção chega ao `ApiUnauthorizedExceptionHandler` ou se o componente precisa tratá-la. Ver item 6 da seção 13.

## 3. Tratamento de `ApiException`

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
- **Pendente:** o cliente não distingue, na `ApiException`, “mensagem veio do `ErrorDetails`” de “mensagem técnica padrão”. Se isso for necessário, exige uma propriedade ou um tipo de exceção no contrato da Foundation (ver seção 13).

Uma `ApiUnauthorizedException` não deve ser capturada por esses `catch` genéricos: ela representa expiração ou ausência de sessão e deve seguir o handler global de autenticação. Quando uma página capturar `ApiException` de forma ampla, deve relançar a exceção se o status for 401 (por exemplo, com um filtro `when`).

As specs que registram exemplos de comportamento são [0013 — Distribuição do valor líquido](Specs/0013-vault-distribuicao-valor-liquido.md), [0015 — Atualização por cotação, parte 3](Specs/0015-vault-atualizacao-por-cotacao-parte-3.md), [0019 — Encerramento de movimento, parte 4](Specs/0019-vault-encerramento-de-movimento-parte-4.md) e [0021 — Consolidado, parte 3](Specs/0021-vault-consolidado-parte-3.md).

## 4. Página de exceção e páginas de status

Existe uma página global de exceção em `Presentation/Pages/Error.cshtml` e `Presentation/Pages/Error.cshtml.cs`. Ela é configurada em `Program.cs` por:

```csharp
app.UseExceptionHandler("/Error");
```

Sua mensagem é genérica: “Ocorreu um erro. Não foi possível concluir a operação. Tente novamente em instantes.”. A página aceita GET e POST porque o middleware reexecuta a requisição original no caminho `/Error`.

Não há `UseStatusCodePages`, uma página específica para 404/403 ou um mapeamento local de páginas por código de status documentado no `Vault.Presentation`. Respostas de 401 são tratadas pelo `ApiUnauthorizedExceptionHandler`; erros inesperados passam pela página `/Error`.

## 5. Exibição de `ErrorDetails` e erros por campo

Na API, os controllers declaram `ErrorDetails` para respostas de erro, principalmente 404, 422 e 500. Esse contrato é documentado nos controllers de `Service` e nas specs de cada funcionalidade.

No `Vault.Presentation`, porém, o comportamento atual não desserializa `ErrorDetails` em uma coleção de erros por campo. Para erros de validação do formulário, os campos são validados localmente por Data Annotations e `ModelState`, e as mensagens aparecem junto aos campos por meio dos helpers padrão do Razor Pages.

Quando a API devolve 422, algumas páginas usam apenas `ApiException.Message` como mensagem geral no topo do formulário. Portanto, a estratégia atual é:

1. erro conhecido de entrada no Web: `ModelState` e mensagem junto ao campo;
2. erro 422 devolvido pela API: mensagem geral, quando a página opta por preservar `ApiException.Message`;
3. erro de infraestrutura ou outro status: mensagem geral e amigável;
4. 401: fluxo global de autenticação, sem mensagem de validação no formulário.

Não existe atualmente uma estratégia implementada para converter propriedades de `ErrorDetails` em `ModelState` por nome de campo. Se essa experiência for necessária, ela deve ser implementada em um ponto compartilhado do cliente HTTP ou em um helper de página, preservando uma mensagem geral para erros sem campo.

## 6. Tratamento de exceções na API e persistência

O `Vault.Service` registra `builder.Services.AddExceptionHandlers()` em `Service/Program.cs` e executa `app.UseExceptionHandler(...)`. No Vault, essa extensão vem do pacote **legado** `Lumia.Foundation.AspNetCore` e registra, nesta ordem, o `DomainExceptionHandler` e o `UnhandledExceptionHandler`. A equivalência de comportamento com o pacote `AspNetCore.Api` da Foundation ainda precisa ser confirmada (item 5 da seção 13).

- Exceções de domínio que herdam de `DomainBaseException` são tratadas pelo `DomainExceptionHandler`. No contrato atual da Foundation, `EntityNotFoundException` resulta em 404, enquanto `EntityInUseException` e `CommandValidationException` resultam em **422**. A resposta é `ErrorDetails`.
- Exceções não mapeadas seguem para `UnhandledExceptionHandler`, são registradas como falhas de servidor e resultam em 500 com `ErrorDetails`.
- `DbUpdateException` não possui tratamento específico no `Vault.Service` nem na estratégia atual da Foundation. Portanto, uma falha de persistência desse tipo segue o caminho de exceção não mapeada e resulta em **500**. A tradução de violação de unicidade (por exemplo, para 422) é uma pendência, não um comportamento implementado.

### Quadro exceção → HTTP

| Exceção | HTTP hoje | Tratada por | Observação |
| --- | --- | --- | --- |
| `EntityNotFoundException` | 404 | `DomainExceptionHandler` | Recurso inexistente **ou** de outro usuário (ver seção 9, escopo por usuário). |
| `EntityInUseException` | **422** | `DomainExceptionHandler` | Não é 409. Qualquer documento que cite 409 como “intenção observada” está desatualizado. |
| `CommandValidationException` | 422 | `DomainExceptionHandler` | Corpo `ErrorDetails` com mensagem geral. |
| `DbUpdateException` | **500** | `UnhandledExceptionHandler` | Sem tradução hoje, inclusive para violação de unicidade. Pendente (item 8 da seção 13). |
| Demais exceções | 500 | `UnhandledExceptionHandler` | Registrada como falha de servidor; corpo `ErrorDetails` genérico. |

No Web, o cliente HTTP converte qualquer resposta de erro em `ApiException`; o 401 vira `ApiUnauthorizedException` e segue o handler global (seção 8).

Essa decisão e a ordem dos handlers estão registradas em [ADR 0024 — Fim do “loga e relança” nas queries](adr/0024-fim-do-log-e-relancamento-nas-queries.md). A documentação geral da Foundation está em `LumiaFoundation.AspNetCore/docs/readme.md`.

## 7. `SaveAsync` e D2-B

Na versão da Foundation usada pelo Vault, `LumiaFoundation.EFRepository.Repository.BaseRepositoryManager.SaveAsync(CancellationToken)` é público e **não é `virtual`**. A implementação apenas delega para `RepositoryContext.SaveChangesAsync(cancellationToken)`.

Consequentemente, uma estratégia que dependa de sobrescrever `SaveAsync` (D2-B) não pode ser aplicada por herança direta nessa versão. O contrato usado pelo Vault é `Application/Repositories/IRepositoryManager`, que declara `Task SaveAsync(CancellationToken)`; qualquer comportamento adicional, como a **tradução de erro de banco** (por exemplo, `DbUpdateException` por unicidade em exceção de domínio), precisa ser implementado por composição, decorator, alteração da Foundation ou outro ponto explícito de extensão.

**Pendente (decisão, exige ADR).** Opções a discutir para a tradução de `DbUpdateException`:

1. decorator de `IRepositoryManager` na camada de infraestrutura, que captura `DbUpdateException` em `SaveAsync` e a converte em exceção de domínio;
2. tratamento no caso de uso (Application), com `try/catch` em torno de `SaveAsync` onde a unicidade for relevante;
3. novo `IExceptionHandler` no host da API, que mapeia `DbUpdateException` para 422 (mais simples, mas o handler não conhece a regra de negócio violada);
4. tornar `SaveAsync` `virtual` na Foundation (sinalizar impacto em Web e Api, conforme as restrições do projeto).

## 8. Origem e dependência do `ApiUnauthorizedExceptionHandler`

`ApiUnauthorizedExceptionHandler` vem da Foundation, no pacote `Lumia.Foundation.AspNetCore.Web`, namespace `LumiaFoundation.AspNetCore.Web.ClientAuthentication`. Ele não é implementado pelo Vault.

O handler reconhece `ApiException` com status 401, resolve `IApiSignInService` pelo `HttpContext.RequestServices`, chama `SignOutAsync` e redireciona para o caminho configurado (por padrão, o login). Portanto, ele depende da infraestrutura de autenticação/sessão registrada pelo host, embora não acesse `ISession` diretamente.

No Vault, essa dependência é montada por `AddSessionTokenStore()`, `AddApiSessionCookie()`, `AddApiSignInService()` e pelo middleware `UseSession()` antes de `UseExceptionHandler()`. O `SessionTokenStore` da Foundation usa a sessão para guardar o token; o handler encerra essa sessão por meio do `IApiSignInService`. A documentação correspondente está em `LumiaFoundation.AspNetCore/docs/readme.md`, na seção “Consumindo uma API protegida por Lumia.Auth”.

**Pendente.** A dependência do handler em `IApiSignInService` ainda não foi exercitada por um segundo consumidor. Quando o Flashcard o adotar, confirmar que o registro de `AddApiSignInService()` e do cookie é suficiente nesse host e atualizar este guia (item 7 da seção 13).

## 9. Convenções dos clientes de recurso

*Diretriz*, derivada do padrão já usado em `MovimentoApi` e nos demais clientes.

- **Um cliente por recurso.** Interface (`IXxxApi`) e implementação (`XxxApi`) ficam juntas na pasta do recurso e são registradas com `AddApiResourceClient<IXxxApi, XxxApi>()` em `AddApiResourceClientService`.
- **Dependência única:** a implementação recebe `IApiConnection`. Páginas, componentes e serviços de apresentação dependem apenas da interface; nenhum `HttpClient` direto fora do cliente.
- **`CancellationToken` em todos os métodos** do cliente, repassado até a chamada HTTP. Nas Razor Pages, use o token da requisição (`HttpContext.RequestAborted`).
- **Contratos explícitos.** Os tipos de requisição e resposta pertencem ao contrato entre Web e API; o Web não referencia entidades de domínio nem de persistência da API.
- **Escopo por usuário é da API.** O usuário é identificado pelo token enviado pelo cliente. O Web não monta filtros de segurança nem envia identificador de usuário como forma de autorização. Um recurso de outro usuário deve aparecer para o Web como 404, nunca como dado.
- **Sem tratamento de erro dentro do cliente de recurso.** O cliente deixa a `ApiException` subir; quem decide a mensagem é a página (seção 3).

## 10. Testes do lado do consumidor

*Diretriz.* Toda decisão de cliente deve poder ser testada sem subir a API.

- **Páginas:** testes unitários com um duplo de `IXxxApi` (fake ou mock). Cobrir, no mínimo: sucesso, `ApiException` com status 422 (mensagem preservada), `ApiException` com outro status (mensagem amigável fixa) e 401 (a exceção não pode ser engolida).
- **Clientes de recurso:** testes com um duplo de `IApiConnection`, verificando método, rota, corpo e propagação do `CancellationToken`.
- **Composição (`Program.cs`):** um teste de integração do host que verifique que `IXxxApi` resolve pelo contêiner e que a ordem `UseSession()` → `UseExceptionHandler()` é mantida. Um 401 simulado da API deve resultar em redirecionamento ao login com a sessão limpa.
- **Contrato Web × API:** quando o contrato de um recurso muda, o teste do cliente deve quebrar antes da página; por isso a verificação de rota e corpo acima.

## 11. Logging e observabilidade no consumidor

*Diretriz (a confirmar contra o código atual).*

- Falhas de API capturadas localmente pela página devem ser registradas em nível adequado (aviso para 4xx esperados, erro para 5xx), com status e identificador da operação, **sem** token, cabeçalhos de autorização nem corpo completo da requisição.
- Exceções não capturadas chegam ao handler global, que registra e exibe `/Error`; a página não deve registrar de novo o que vai relançar (mesmo princípio do ADR 0024: sem “loga e relança”).
- Logging estruturado por NLog, com propriedades (`Recurso`, `Operacao`, `StatusHttp`) em vez de texto concatenado.
- Mensagens técnicas (`ApiException.Message` padrão) servem para log, não para a tela.

## 12. Checklist para um novo recurso consumidor

1. Existe o contrato (requisição e resposta) documentado na spec do recurso, com os status e o `ErrorDetails` declarados nos controllers.
2. Interface `IXxxApi` e implementação `XxxApi` criadas com `IApiConnection`, todos os métodos com `CancellationToken`.
3. Registro adicionado em `AddApiResourceClientService`.
4. Página captura `ApiException` apenas onde há falha localizada, usa mensagem fixa (ou `Message` somente em 422) e **não captura 401**.
5. Exibição do erro com `ErrorMessage` e alerta com `role="alert"`; erros de campo por `ModelState`.
6. Testes da página e do cliente conforme a seção 10.
7. Logging conforme a seção 11, sem dados sensíveis.
8. Spec do recurso atualizada com o comportamento de erro observado.

## 13. Pontos a validar

| # | Ponto | Situação |
| --- | --- | --- |
| 1 | Quadro exceção → HTTP: `EntityInUseException` é 422, não 409. | Resolvido neste guia. |
| 2 | `ApiException.Message` técnica quando o corpo não é reconhecível; diferenciar a origem da mensagem no cliente. | Aberto (seção 3). |
| 3 | `SaveAsync` não é `virtual`; tradução de erro de banco exige composição. | Resolvido como fato; decisão de solução aberta (seção 7). |
| 4 | Padrão de consumo de API no `Vault.Presentation` é a referência para novos consumidores. | Resolvido neste guia (seções 9 a 12). |
| 5 | `AddExceptionHandlers()` vem do pacote legado `Lumia.Foundation.AspNetCore`, enquanto a recomendação é `AspNetCore.Api`. Confirmar se os dois se comportam igual (ordem dos handlers, mapeamento de status, formato de `ErrorDetails`). | Aberto. |
| 6 | Fluxo do 401 nas ilhas Blazor (ADR 0014): a exceção chega ao `ApiUnauthorizedExceptionHandler` ou o componente a trata? | Aberto. |
| 7 | Dependência do handler em `IApiSignInService` ao ser validada pelo Flashcard. | Aberto. |
| 8 | `DbUpdateException` resulta em 500; definir tradução de violação de unicidade. | Aberto; exige ADR (seção 7). |

## Resumo por tópico

| Tópico | Orientação atual |
| --- | --- |
| `ApiException` | Captura local nas Razor Pages; mensagem geral em `ErrorMessage`; `Message` vem de `ErrorDetails` em qualquer status, ou é técnica se o corpo não for reconhecível, então só exibir em 422; 401 segue o handler global. |
| Cliente tipado | `Program.cs` registra `AddLumiaApiClient`; `PresentationExtensions` registra clientes de recurso baseados em `IApiConnection`; um cliente por recurso, com `CancellationToken`. |
| Sessão e refresh | `SessionTokenStore`, cookie `Vault.Auth`, refresh em `/api/token/refresh` e `ApiUnauthorizedExceptionHandler`; `UseSession()` antes de `UseExceptionHandler()`. |
| Exceção/status | Existe `/Error`; não existe `StatusCodePages` nem página específica por status. |
| `ErrorDetails` | Contrato existe na API, mas a Presentation atualmente o apresenta como mensagem geral, não por campo. |
| Exceções da API | 404 (`EntityNotFoundException`), 422 (`EntityInUseException`, `CommandValidationException`); exceções não mapeadas e `DbUpdateException` resultam em 500. |
| Origem dos handlers | `AddExceptionHandlers()` do pacote legado `Lumia.Foundation.AspNetCore`; equivalência com `AspNetCore.Api` a confirmar. |
| `SaveAsync` | `BaseRepositoryManager.SaveAsync(CancellationToken)` existe, mas não é `virtual`; extensão por composição. |
| Ilhas Blazor | Fluxo do 401 ainda não descrito (ADR 0014). |
| Testes e logging | Fakes de `IXxxApi`/`IApiConnection`; log sem “loga e relança” e sem dados sensíveis. |
