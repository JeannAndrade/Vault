# ADR 0024: Fim do "loga e relança" nas queries da Application

- Status: Aceita (2026-11-11). Supera o ADR 0017 somente na regra do `catch ... when`.

## Contexto

15 queries da Application envolviam o corpo em `try/catch`, registravam o erro com `_logger.LogError($"Algo deu errado no método de serviço {nameof(X)}: {ex}")` e relançavam. O padrão nasceu no ADR 0017, que exigiu `catch (Exception ex) when (ex is not OperationCanceledException)` para o cancelamento não gerar log de erro falso. Todas seguiam o mesmo formato, com um único `LogError` cada.

Verificado na LumiaFoundation (`main`):

- `UnhandledExceptionHandler`: toda exceção que **não** seja `HttpBaseException` é registrada com `logger.LogError(exception, ...)` (objeto e stack trace) e vira 500.
- `DomainExceptionHandler`: converte `EntityNotFoundException` (404), `EntityInUseException` (422) e `CommandValidationException` (422) e registra 4xx como Warn e 5xx como Error.
- `AddExceptionHandlers()` registra o Domain **antes** do Unhandled, ordem necessária porque as exceções de domínio não herdam de `HttpBaseException`; o Service a chama e o pipeline usa `UseExceptionHandler`.
- O cancelamento com requisição abortada é tratado antes, pelo middleware do ASP.NET Core (499, log em Debug), conforme o ADR 0017.

Problemas do padrão:

1. **Log duplicado:** uma falha inesperada gerava uma entrada na query e outra no handler.
2. **Fora do padrão estruturado:** a mensagem interpolava `{ex}` como texto.
3. **Acoplamento:** a Application dependia de `ILoggerManager` (`Lumia.Foundation.Logger`) só para isso, contra o isolamento do ADR 0023.
4. **Repetição:** o mesmo bloco em 15 queries e os testes de "loga e relança" em 4 classes de teste.

## Decisão

- Queries e commands da Application **não** capturam exceções para registrar e relançar. O registro de falhas inesperadas fica num único ponto: os handlers da Api. Nenhum caminho fica sem log: falhas esperadas terminam em Warn (4xx) e inesperadas em Error (5xx).
- `ILoggerManager` deixa de ser dependência da Application; o pacote `Lumia.Foundation.Logger` sai do `Application.csproj` junto com o `Lumia.Foundation.AspNetCore` (ADR 0023), e o Service os declara.
- Sem `catch`, o `OperationCanceledException` propaga e o middleware responde 499 sem log de erro. O filtro `when (ex is not OperationCanceledException)` deixa de ser necessário.
- Do ADR 0017 permanecem o token obrigatório, último parâmetro e propagado até o EF Core.
- A validação de entrada (`CommandValidator.Validate`) continua antes de qualquer acesso a dados, e erro do cliente continua sendo 422.

## Alternativas consideradas

- **Manter o `try/catch` com log estruturado** (`LogError(ex, "...")`): resolve o item 2, mas mantém o log duplicado e o acoplamento ao Logger.
- **Manter só em queries "críticas":** cria dois padrões no mesmo projeto, contra o objetivo de consistência.
- **Enriquecer o contexto no handler** (nome da ação ou rota): mantém um único ponto de log e recupera o que se perde com a remoção. Evolução possível na LumiaFoundation.

## Consequências

- Perde-se o nome da query na mensagem; a stack trace registrada pelo handler a identifica.
- Saíram os testes de "loga e relança". Permanecem, sem o `Verify` do logger, os de propagação (`Assert.Same` na mesma exceção) e de cancelamento (propaga `OperationCanceledException`).
- `RegistroDeServicosTests` deixou de registrar `Mock.Of<ILoggerManager>()`.
- **Dívida na LumiaFoundation** (mudança própria, com sinalização de impacto em Web e Api): `UnhandledExceptionHandler` passa o objeto **e** interpola `{exception}` na mensagem, e `DomainExceptionHandler` usa `LogWarn("Atenção: {0}", exception)`, placeholder posicional sem o objeto da exceção.
- **Fora do escopo:** as páginas do Web (`Presentation/Pages/**`) ainda usam `ILoggerManager`. Um ajuste equivalente no Web pede ADR próprio.
- **Adiado:** teste automatizado da ordem dos handlers; a ordem está fixada em `AddExceptionHandlers()` e, quando feito, o teste mora na LumiaFoundation.

## Execução

Commit `6f702e5`: 15 queries e testes ajustados, com `GetMovimentosListQuery` como modelo. A saída dos pacotes da Application veio no commit `b34895a` (ADR 0023). Build e testes verdes em cada commit.

## Como validar

1. `dotnet build` e `dotnet test` na solução, ambos verdes.
2. `grep -rn "Logger" Application Application.Test`: sem resultados.
3. Falha real (sem código novo): subir a Api com `docker compose up --build`, parar o contêiner do MariaDB e chamar uma listagem autenticada. Esperado: 500 com `ExceptionType` e **uma única** entrada de Error com stack trace em `Service/logs/current.log` e `current.json`.
4. Cancelamento: abortar a requisição no meio (por exemplo, `curl` interrompido). Esperado: nenhuma entrada de Error.
5. Falha esperada: chamar um recurso inexistente. Esperado: 404 e uma entrada de Warn.

## Pendências

- ADR 0017: acrescentar nota de que a regra do `catch ... when` foi superada por este ADR.
- Mudança na LumiaFoundation para os dois pontos de log estruturado acima.
