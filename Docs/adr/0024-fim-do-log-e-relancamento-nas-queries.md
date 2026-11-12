# ADR 0024: Fim do "loga e relança" nas queries da Application

- Status: Proposta (2026-11-10). **Parcial, a completar na próxima interação.**

## Contexto

Várias queries da Application (por exemplo, `GetMovimentosListQuery`, `GetEmissoresListQuery`, `GetRentabilidadesPorTipoRendaQuery` e `GetConsolidadoQuery`) envolvem o corpo em `try/catch`, registram o erro com `_logger.LogError($"Algo deu errado no método de serviço {nameof(X)}: {ex}")` e relançam. O padrão nasceu no ADR 0017, que exigiu `catch (Exception ex) when (ex is not OperationCanceledException)` para o cancelamento não gerar log de erro falso.

Verificado no código da LumiaFoundation (`UnhandledExceptionHandler`, branch `main`):

- Toda exceção que **não** seja `HttpBaseException` é registrada pelo handler com `logger.LogError(exception, $"Ocorreu um erro desconhecido: {exception}")` e vira resposta 500 (`ErrorDetails` com o tipo da exceção). O objeto da exceção, com stack trace, já é passado ao logger.
- `HttpBaseException` faz o handler retornar `false`, deixando a exceção para os demais handlers registrados.
- O cancelamento com a requisição abortada é tratado antes pelo middleware do ASP.NET Core (499, log em Debug), conforme o ADR 0017.

Consequências do padrão atual:

1. **Log duplicado:** uma falha inesperada em query gera uma entrada na query e outra no handler.
2. **Fora do padrão estruturado:** a mensagem interpola `{ex}` como texto, em vez de passar a exceção ao logger.
3. **Acoplamento:** a Application depende de `ILoggerManager` (pacote `Lumia.Foundation.Logger`) apenas para isso, o que atrapalha o isolamento previsto no ADR 0023.
4. **Repetição:** o mesmo `try/catch` em 15 queries da Application (todas com `catch (Exception ex) when (ex is not OperationCanceledException)` e um único `LogError`), mais os testes de "loga e relança" em 4 classes de teste.

## Decisão (proposta)

- Queries e commands da Application **não** capturam exceções para registrar e relançar. O registro de falhas inesperadas fica num único ponto: o `UnhandledExceptionHandler` da Api.
- `ILoggerManager` deixa de ser dependência das queries.
- Sem `catch`, o `OperationCanceledException` propaga e o middleware responde 499 sem log de erro; o filtro `when (ex is not OperationCanceledException)` deixa de ser necessário.
- Supera parcialmente o ADR 0017 **somente** na regra do `catch ... when`. Permanecem o token obrigatório, último parâmetro e propagado ponta a ponta.

## Alternativas consideradas

- **Manter o `try/catch` com log estruturado** (`LogError(ex, "...")`): resolve o item 2, mas mantém o log duplicado e o acoplamento ao Logger.
- **Manter só em queries "críticas":** cria dois padrões no mesmo projeto, contra o objetivo de consistência.
- **Enriquecer o contexto no handler** (nome da ação ou rota): mantém um único ponto de log e recupera o que se perde com a remoção. Evolução possível na LumiaFoundation.

## Consequências (parciais)

- Perde-se o nome da query na mensagem; a stack trace registrada pelo handler a identifica.
- Saem os testes de "loga e relança"; permanecem os de propagação do token e de cancelamento, que passam a afirmar que a query não engole nem registra a exceção.
- A dívida de log estruturado se desloca para a lib: o próprio handler passa o objeto **e** interpola `{exception}` na mensagem. Não é contrato público, mas deve ser tratado em mudança própria na LumiaFoundation, com sinalização de impacto em Web e Api.

## Verificação dos handlers (`DomainExceptionHandler`, branch `main`)

O `DomainExceptionHandler` converte para `HttpBaseException` as exceções de domínio e as registra por faixa de status:

| Exceção | Status | Log no handler |
|---|---|---|
| `EntityNotFoundException` | 404 | Warn |
| `EntityInUseException` | 422 | Warn |
| `CommandValidationException` | 422 | Warn |
| `HttpBaseException` (qualquer) | o próprio | Warn (4xx) ou Error (5xx) |
| Demais exceções | 500 | Error, no `UnhandledExceptionHandler` |

Conclusões:

- **Nenhum caminho fica sem log** após remover o `try/catch` das queries: toda exceção termina em Warn (4xx) ou Error (5xx/inesperada) num dos dois handlers.
- **Validação já fica fora do `try`:** em `GetMovimentosListQuery`, `CommandValidator.Validate` vem antes do `try` (comentário no código: erro do cliente não é falha de serviço). Se isso valer para as demais queries, o log de Error da query cobre só falhas do repositório; a duplicidade com o handler continua, mas sem inversão de severidade. Falhas esperadas lançadas pelo repositório, se houver, seguem como ponto de atenção do dev ao aplicar a mudança.
- **Mesma dívida de estrutura:** `LogWarn("Atenção: {0}", exception)` usa placeholder posicional e passa a exceção como argumento, não como objeto de exceção. Entra na mesma mudança de LumiaFoundation já citada nas consequências.
- O cancelamento não passa por nenhum dos dois handlers quando a requisição foi abortada (middleware do ASP.NET Core responde antes, conforme ADR 0017).

## A completar

1. ~~Confirmar o comportamento dos handlers de `HttpBaseException` e das exceções de domínio.~~ **Resolvido (2026-11-11)**, ver "Verificação dos handlers" abaixo. Resta o item 1b.
   - 1b. ~~Confirmar a ordem de registro dos handlers na Api.~~ **Resolvido (2026-11-11):** o `Program.cs` registra `AddExceptionHandler<DomainExceptionHandler>()` antes de `AddExceptionHandler<UnhandledExceptionHandler>()`, então as exceções de domínio viram 404/422 com Warn e só o restante cai no handler genérico. A ordem é requisito (handlers rodam na ordem de registro) e deve ganhar um teste de integração na Api (item 4). Resta conferir que o pipeline chama `app.UseExceptionHandler()`, sem o qual nenhum handler executa.
2. ~~Inventário exato.~~ **Delegado ao dev (2026-11-11)**, com `GetMovimentosListQuery` e seu teste como modelo. Ponto de partida: `grep -rl ILoggerManager Application Application.Test Service.Test` (15 queries, 4 testes de query e `RegistroDeServicosTests`). Os 10 usos de `_logger` e os 5 de `logger` na Application são todos `LogError` dentro do `catch`.
3. ~~Relação com o ADR 0023.~~ **Resolvido (2026-11-11):** o 0024 executa **antes** do 0023, por ser mudança pequena e mecânica que deixa a Application sem `ILoggerManager`. O pacote `Lumia.Foundation.Logger` sai do `Application.csproj` no mesmo passo em que o último uso de `ILoggerManager` some da Application, junto com a saída do `Foundation.AspNetCore` (passo 3 do plano). Só depois vem a inversão da persistência (ADR 0023).
4. ~~Plano de execução e "Como validar".~~ **Resolvido (2026-11-11)** na seção "Plano de execução e validação". O teste automatizado da ordem dos handlers fica **adiado**: a ordem já está fixada na extensão `AddExceptionHandlers()` da LumiaFoundation (Domain antes de Unhandled), que o Service chama; fica só a verificação manual.
5. Decidir se o Web precisa de ajuste equivalente (fora do escopo por ora).

## Plano de execução e validação (rascunho)

Cada passo termina com `dotnet build` e `dotnet test` na solução, ambos verdes, e vira um commit próprio.

**Passo 1: queries e testes** (escopo do 0024)
- Nas 15 queries da Application: remover o `try/catch`, o `ILoggerManager` e o `using` do Logger, conforme o modelo `GetMovimentosListQuery`.
- Nos 4 testes de query: remover o mock do logger, trocar os testes "loga e relança" por "propaga a mesma exceção" e os de cancelamento por "propaga `OperationCanceledException`".
- Em `Service.Test/Registro/RegistroDeServicosTests`: remover `services.AddSingleton(Mock.Of<ILoggerManager>())`, que só existia para resolver as queries.
- Verificação: `grep -rn "Logger" Application Application.Test` sem resultados; `RegistroDeServicosTests` continua passando com `ValidateOnBuild`.

**Passo 2: pacotes** (junto com o passo 3 do plano geral, ADR 0023)
- O `Lumia.Foundation.Logger` sai do `Application.csproj` **junto** com `Lumia.Foundation.AspNetCore`: remover só o Logger não o tira do grafo, porque o AspNetCore o traz de forma transitiva.
- O `Service.csproj` hoje não declara o Logger nem o AspNetCore (os dois chegam via Application) e usa o Logger em `Program.cs` (`LoggerManager`, `ConfigureLoggerService`). Passa a declarar os dois explicitamente, nas mesmas versões atuais (0.5.1 e 0.31.0), sem dependência nova.
- Verificação: `dotnet list Application package --include-transitive` sem Logger nem AspNetCore; build do Service e `docker compose up --build` sobem.

**Validação manual de uma falha real** (sem código novo)
1. Subir a Api e o banco com `docker compose up --build`.
2. Parar o contêiner do MariaDB e chamar um endpoint de listagem autenticado.
3. Esperado: resposta 500 com `ExceptionType`; em `Service/logs/current.log` (e `current.json`), **uma única** entrada de Error com stack trace, em vez das duas de hoje.
4. Cancelamento: abortar a requisição no meio (por exemplo, `curl` interrompido). Esperado: nenhuma entrada de Error.
5. Falha esperada: chamar um recurso inexistente. Esperado: 404 com uma entrada de Warn.

**Ordem dos handlers: teste automatizado (adiado)**
- A) Só a verificação manual acima. Simples, mas a ordem pode regredir sem aviso.
- B) Teste unitário que chama `AddExceptionHandlers()` e confere a ordem dos `IExceptionHandler` resolvidos (Domain antes de Unhandled). Sem dependência nova. Como o método é da LumiaFoundation, o ideal é o teste morar lá, se a lib tiver projeto de testes.
- C) Teste de integração com `WebApplicationFactory`: o mais fiel (status e log reais), mas exige o pacote `Microsoft.AspNetCore.Mvc.Testing`, hoje ausente do `Service.Test`. Dependência nova, depende de aprovação.
- Recomendação: B. **Decisão (2026-11-11):** adiar. A ordem está fixada em `AddExceptionHandlers()`; o teste B, quando feito, mora na LumiaFoundation.
