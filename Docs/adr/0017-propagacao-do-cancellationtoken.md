# ADR 0017: Propagação do CancellationToken

- Status: Proposta (2026-10-19)

## Contexto

As funcionalidades novas (specs 0011, 0013, 0015) já nascem com `CancellationToken` obrigatório, mas o código mais antigo não propaga o token da requisição até o banco: os repositórios, as queries e os commands dos catálogos (Corretora, Emissor, Produto, Tipo de Renda e Objetivo), boa parte de Movimento e quase todas as ações dos controllers da Api ainda não recebem o token.

Sem o token, uma requisição abandonada (aba fechada, navegação interrompida) continua ocupando conexão do pool e CPU até o fim. Com ele, o `OperationCanceledException` interrompe a consulta. Nas escritas, um `SaveChangesAsync(ct)` cancelado faz rollback da transação única do `DbContext`, sem estado parcial; é a mesma semântica de uma falha de rede, já assumida nos ADRs 0011 e 0013.

A propagação cria uma armadilha: seis queries de lista (Corretoras, Emissores, Produtos, Tipos de Renda, Objetivos e Resumo de Objetivos) usam `catch (Exception ex)` sem excluir `OperationCanceledException`. Depois da propagação, cada requisição abortada geraria um log de erro falso.

Foi verificado no código-fonte (LumiaFoundation e ASP.NET Core 10) que:

- O `ExceptionHandlerMiddleware` do ASP.NET Core já trata a requisição abortada **antes** de chamar os `IExceptionHandler`: com `OperationCanceledException` (ou `IOException`) e `RequestAborted` cancelado, loga em Debug e responde 499. Os handlers da lib só veem cancelamentos com a requisição viva (por exemplo, timeout do `HttpClient` entre Web e Api), que continuam 500.
- O `UserManager` e o `RoleManager` do Identity usam `CancellationToken.None` e nenhum método aceita token por chamada. O `AuthenticationService` não usa `DbContext` diretamente.
- O `ApiConnection` já propaga o token no envio, na leitura do corpo de erro e na desserialização, sem `catch`.
- O `BearerTokenHandler` (Http.Client) tem um `catch` vazio no refresh: um cancelamento durante o refresh cai nele, **limpa o `ITokenStore`** e devolve o 401, derrubando o login do usuário.

## Decisão

### Regra para Application e Persistence

- Todo método assíncrono de I/O de `I*Repository`, `I*Query`, `I*Command` e do validator de referências recebe `CancellationToken cancellationToken` como **último parâmetro, obrigatório**: sem `= default` e sem sobrecarga.
- As assinaturas são trocadas diretamente, e os testes e fakes são ajustados no mesmo commit. A regra "sobrecarga, nunca opcional" continua valendo para a LumiaFoundation, que é publicada; Application e Persistence não são bibliotecas publicadas.
- O token é repassado até o `SaveAsync(cancellationToken)` e às chamadas do EF Core. Nenhum método descarta o token disponível.
- Queries e commands que fazem `catch (Exception ex)` passam a usar `catch (Exception ex) when (ex is not OperationCanceledException)`, como `GetMovimentosDesatualizadosQuery`. Cancelamento não é erro e não é logado como erro.

### Controllers da Api

- O token entra como **parâmetro `CancellationToken cancellationToken` da ação**, sem `= default`: o ASP.NET o liga a `RequestAborted`. Isso substitui `HttpContext.RequestAborted` nas ações atuais (`MovimentosController`, `CotacoesController`, `AtualizacaoPorCotacaoController`).
- O parâmetro não aparece no OpenAPI e simplifica os testes (sem montar `HttpContext`).
- Os controllers de autenticação (`AuthenticationController`, `TokenController`) ficam de fora, porque o `AuthenticationService` não consegue cancelar (ver abaixo).

### Cancelamento não é tratado pelos handlers da lib

- **Nenhum `IExceptionHandler` novo ou alterado**, nem no Vault nem na lib: o middleware do framework já responde 499 e loga em Debug. O Web usa o mesmo middleware (`UseExceptionHandler("/Error")`) e se beneficia igual.
- Cancelamento com a requisição viva (timeout interno) continua tratado como 500 pelo `UnhandledExceptionHandler`.

### LumiaFoundation

- **`BearerTokenHandler` (Http.Client 0.5.1, correção):** o `catch` do refresh passa a ser `catch (Exception ex) when (ex is not OperationCanceledException)`. Cancelar durante o refresh **não** limpa o `ITokenStore` e propaga o cancelamento. Teste de regressão na lib.
- **`AuthenticationService`: sem alteração.** Como o Identity não aceita token por chamada, adicionar `CancellationToken` à interface mudaria o contrato sem cancelar nenhuma consulta. Fazer o cancelamento valer exigiria um `UserManager` derivado que sobrescreva `CancellationToken` e transação no `RegisterUser` (que hoje faz `CreateAsync` e `AddToRolesAsync` sem transação); fica registrado como débito conhecido, sem prazo.
- **`ApiConnection` e handlers de exceção: sem alteração.**

### Salvaguardas contra regressão (sem dependência nova)

- **Teste de reflexão** (xUnit, no `Service.Test`, que já referencia Service, Application e Persistence): exige `CancellationToken` como último parâmetro em todo método assíncrono de `I*Query`, `I*Command`, `I*Repository` e `IMovimentoReferenciasValidator`. Parte de uma **allowlist de pendências que encolhe a cada fatia** e termina vazia; o teste também falha se um item da allowlist já estiver em conformidade (para a lista não apodrecer).
- **`CA2016`** (`Forward the CancellationToken parameter`) com `dotnet_diagnostic.CA2016.severity = warning` no `.editorconfig`, para avisar quando um token disponível não é repassado.

### Execução

Fatias verticais por agregado, cada uma com build e testes verdes (spec 0016). O ADR muda para **Aceita** na última fatia.

## Alternativas consideradas

- **Big-bang em um único PR:** termina rápido, mas o diff é enorme e difícil de revisar.
- **Sobrecargas e depois remover as antigas:** menos testes quebrados por commit, mas dobra a superfície (crítica do ADR 0012) e deixa legado.
- **`HttpContext.RequestAborted` nos controllers:** não muda a assinatura, mas é implícito e exige montar o `HttpContext` nos testes. O parâmetro da ação é o idiomático.
- **Handler de cancelamento local no Vault, ou alteração do `UnhandledExceptionHandler` da lib:** redundantes com o middleware do framework (499 antes dos handlers).
- **Token na interface do `AuthenticationService`:** contrato novo sem efeito real (Identity usa `CancellationToken.None`); um `UserManager` derivado com transação é mudança maior que o ganho.
- **NetArchTest ou ArchUnit para as regras de arquitetura:** dependência nova sem justificativa; o teste de reflexão resolve esta regra.

## Consequências

- Mudança de assinatura em cerca de 50 métodos de repositório, query e command, e nas ações dos controllers, mais os testes e fakes; feita por fatias, sem janela de código quebrado.
- Requisições abortadas passam a interromper a consulta e a não gerar log de erro; a resposta é 499 (middleware) em vez de execução até o fim.
- Escritas canceladas fazem rollback integral; o cliente que cancelou não sabe se a escrita ocorreu (mesma semântica de falha de rede).
- Nova versão de lib só para o `BearerTokenHandler` (Http.Client 0.5.1), publicada antes de o Vault consumi-la.
- O `RegisterUser` da lib continua sem transação e sem cancelamento; o risco de usuário sem roles em falha no meio já existe hoje e não muda.
- O teste de reflexão e o `CA2016` impedem que novos métodos nasçam sem o token.
- Complementa os ADRs 0011, 0013 e 0016 (token obrigatório nas novas assinaturas) estendendo a regra ao código existente.
