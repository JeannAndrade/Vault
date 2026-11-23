# Integração entre `Vault.Presentation` e a API

Este documento descreve como o `Vault.Presentation` consome a API, mantém a sessão e apresenta erros ao usuário.

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

## 3. Tratamento de `ApiException`

As páginas capturam `ApiException` somente quando conseguem apresentar uma falha localizada sem derrubar a página inteira. Exemplos:

- `Presentation/Pages/Movimentos/Create.cshtml.cs`: erro de gravação no formulário;
- `Presentation/Pages/Movimentos/Distribuicao.cshtml.cs`: erro isolado na simulação ou confirmação;
- `Presentation/Pages/Index.cshtml.cs`: erro isolado de uma seção da página inicial;
- `Presentation/Pages/Dashboards/Rentabilidade.cshtml.cs` e `Consolidado.cshtml.cs`: erro no painel correspondente.

O padrão é preencher uma propriedade como `ErrorMessage` e retornar `Page()`. A view exibe essa propriedade em um alerta Bootstrap, normalmente com `role="alert"`. Assim, a falha vira uma mensagem de tela, formulário ou seção, conforme o contexto.

Em geral, uma falha de API recebe uma mensagem amigável e fixa, por exemplo: “Não foi possível salvar o movimento agora. Tente novamente em instantes.”. Há uma exceção deliberada para respostas 422 em alguns formulários: nesses casos, a mensagem da `ApiException` é exibida quando ela representa a mensagem de validação devolvida pela API. Esse comportamento aparece, por exemplo, em `Movimentos/Create.cshtml.cs` e `Movimentos/Encerrar.cshtml.cs`.

Uma `ApiUnauthorizedException` não deve ser capturada por esses `catch` genéricos: ela representa expiração ou ausência de sessão e deve seguir o handler global de autenticação.

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

## Resumo por tópico

| Tópico | Orientação atual |
| --- | --- |
| `ApiException` | Captura local nas Razor Pages; mensagem geral em `ErrorMessage`; 401 segue o handler global. |
| Cliente tipado | `Program.cs` registra `AddLumiaApiClient`; `PresentationExtensions` registra clientes de recurso baseados em `IApiConnection`. |
| Sessão e refresh | `SessionTokenStore`, cookie `Vault.Auth`, refresh em `/api/token/refresh` e `ApiUnauthorizedExceptionHandler`. |
| Exceção/status | Existe `/Error`; não existe `StatusCodePages` nem página específica por status. |
| `ErrorDetails` | Contrato existe na API, mas a Presentation atualmente o apresenta como mensagem geral, não por campo. |
