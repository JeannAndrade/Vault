# Especificação técnica 0016: Propagação do CancellationToken (Vault e LumiaFoundation)

- Data: 2026-10-19
- Status: proposta
- ADR relacionado: [0017](../adr/0017-propagacao-do-cancellationtoken.md)
- Repositórios afetados: **Vault** (Persistence, Application, Service, Presentation/Web, testes) e **LumiaFoundation** (somente `Lumia.Foundation.Http.Client`, `BearerTokenHandler`). **Sem alteração de schema e sem migration.**

> Este documento é uma especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português. Nomes de tipos e membros seguem o padrão do repositório.

---

## 1. Resumo

Propagar o `CancellationToken` da requisição HTTP até o banco em todo o código que ainda não o faz, trocando as assinaturas diretamente (token obrigatório, último parâmetro, sem `= default` e sem sobrecarga). A propagação é feita em fatias verticais por agregado, protegida por um teste de reflexão com allowlist decrescente e pela regra `CA2016`. Em paralelo, corrige-se na lib o `BearerTokenHandler`, que hoje limpa a sessão do usuário quando o refresh é cancelado.

## 2. Decisões aprovadas

| Item | Decisão |
| --- | --- |
| D1 | Estratégia: fatias verticais por agregado, trocando a assinatura direto (sem sobrecarga) |
| D2 | Em Application e Persistence: token obrigatório, último parâmetro; testes e fakes ajustados no mesmo commit |
| D3 | Controllers: parâmetro `CancellationToken` na ação, sem `= default`; substitui `HttpContext.RequestAborted` |
| D4 | Nenhum handler de exceção novo ou alterado: o `ExceptionHandlerMiddleware` já responde 499 e loga em Debug |
| D5 | `AuthenticationService`, `ApiConnection` e handlers da lib: sem alteração |
| D6 | Corrigir o `BearerTokenHandler` (Http.Client 0.5.1): cancelamento no refresh não limpa o `ITokenStore` |
| D7 | Salvaguardas: teste de reflexão com allowlist que encolhe e `CA2016` como warning; sem dependência nova |
| D8 | Controllers de autenticação ficam fora do escopo (o `AuthenticationService` não consegue cancelar) |
| D9 | ADR 0017 começa **Proposta** e vira **Aceita** na Fatia 6 |

## 3. Atenção antes de começar

1. **Último parâmetro, obrigatório, sem `= default`.** O objetivo é que o compilador (e o teste de reflexão) denunciem qualquer chamada que esqueça o token.
2. **Nunca descartar um token disponível.** Chamar `SaveAsync()` ou um método sem token dentro de um método que recebe o token é um defeito (o `CA2016` avisa).
3. **Cancelamento não é erro.** Todo `catch (Exception ex)` de query ou command passa a ser `catch (Exception ex) when (ex is not OperationCanceledException)`. Modelo: `GetMovimentosDesatualizadosQuery`.
4. **Escrita cancelada = rollback.** O `SaveChangesAsync(ct)` cancelado desfaz a transação única do `DbContext`; não há estado parcial. Não criar transações explícitas novas.
5. **Cada fatia termina com `dotnet build` e `dotnet test` verdes** e com a allowlist do teste de reflexão atualizada.
6. **Multi-tenant intacto:** a troca de assinatura não pode afetar o `UserId` aplicado nos repositórios.
7. **Contratos da lib:** só `BearerTokenHandler` muda (correção de bug, sem mudança de assinatura pública).
8. **Todo código, comentários, commits e documentação em português.**

## 4. Ordem de implementação

| # | Fatia | Projeto |
| --- | --- | --- |
| 0 | ADR 0017 "Proposta", teste de reflexão com allowlist e `CA2016` | Vault |
| 1 | Correção do `BearerTokenHandler` e publicação da 0.5.1 | LumiaFoundation |
| 2 | Referências: os 5 `GetX`, `MovimentoReferenciasValidator`, `Create/UpdateMovimentoCommand` e as ações de create/update de Movimento | Persistence, Application, Service |
| 3 | Demais operações dos 5 catálogos (list, get, create, update, delete), com Corretora como modelo | Persistence, Application, Service |
| 4 | Resumo de Objetivos | Persistence, Application, Service |
| 5 | Resto de Movimento (get, delete, próximos vencimentos), Cotações e remoção das sobrecargas antigas | Persistence, Application, Service |
| 6 | Limpeza, verificação no Web e fechamento do ADR | Vault |

Por que as referências vêm primeiro: o `MovimentoReferenciasValidator` chama os 5 `IGetXQuery` dos catálogos. Trocar a assinatura de `IGetCorretoraQuery` (sem sobrecarga) obriga a mexer no validator e nos commands de Movimento na mesma fatia.

---

## Fatia 0: ADR, teste de reflexão e `CA2016`

- Criar `Docs/adr/0017-propagacao-do-cancellationtoken.md` (arquivo entregue junto), **Status: Proposta (2026-10-19)**.
- **`.editorconfig`:** acrescentar `dotnet_diagnostic.CA2016.severity = warning` na seção `[*.cs]`.
- **Teste de reflexão** em `Service.Test/Arquitetura/CancellationTokenTests.cs` (o `Service.Test` já referencia Service, Application e Persistence):
  - Percorre os tipos de interface `I*Query`, `I*Command`, `I*Repository` (e `IMovimentoReferenciasValidator`) dos assemblies Application e Persistence.
  - Para cada método que retorna `Task` ou `Task<T>`, exige `CancellationToken` como **último** parâmetro e sem valor padrão.
  - **Allowlist** de pendências, no próprio arquivo, como lista de strings `Tipo.Metodo`. O teste falha se (a) um método fora da allowlist não está em conformidade, ou (b) um método da allowlist já está em conformidade (força a remoção do item).
  - A allowlist inicial é gerada executando o teste uma vez sem lista e copiando as pendências. Ela **encolhe a cada fatia** e termina vazia na Fatia 6.
- **Atenção:** assinaturas em várias linhas e métodos com `bool trackChanges` antes do token são cobertos pela reflexão (não depender de grep).

**Critério de pronto:** teste verde com a allowlist completa; `dotnet build` sem novos warnings além dos `CA2016` que já existirem (listar os existentes no PR).

---

## Fatia 1: LumiaFoundation, `BearerTokenHandler`

Repositório: Lumia.Foundation, projeto `LumiaFoundation.Http.Client`.

- No `SendAsync` do `BearerTokenHandler`, o `catch` do refresh passa de `catch { ... }` para `catch (Exception ex) when (ex is not OperationCanceledException)`. Assim, um cancelamento durante `RefreshAsync` **propaga** e **não** executa `ClearAsync`.
- No caminho do cancelamento, descartar também o `retryRequest` e o `response` 401 (hoje o `finally` só libera o semáforo; sem isso, a exceção que propaga deixa os dois sem descarte), sem mudar o comportamento dos demais caminhos.
- **Testes** em `BearerTokenHandlerTests`:
  - refresh cancelado propaga `OperationCanceledException` e **o `ITokenStore` continua com o token**;
  - refresh que falha com outra exceção continua limpando o token e devolvendo o 401 original (comportamento atual preservado);
  - `_refreshLock` liberado após o cancelamento (uma segunda chamada não trava).
- Atualizar o `readme.md` do Http.Client (histórico) e subir a versão para **0.5.1** no `.csproj`. Publicar no NuGet **antes** de o Vault consumi-la.
- No Vault, atualizar `Lumia.Foundation.Http.Client` de 0.5.0 para 0.5.1 em `Presentation/Presentation.csproj` (na Fatia 6, ou antes, se preferir fechar logo).

**Critério de pronto:** `dotnet test LumiaFoundation.slnx` verde; pacote 0.5.1 publicado.

---

## Fatia 2: Referências (os 5 `GetX`, validator e Movimento create/update)

Camadas: Persistence, Application, Service. Ordem dentro da fatia: repositório, query/command, controller, testes.

- **Persistence:** `GetAsync` dos 5 repositórios de catálogo (Corretora, Emissor, Produto, TipoRenda, Objetivo) e `ExistemPor*Async` do `MovimentoRepository` ganham o token.
- **Application:**
  - os 5 `IGetXQuery` (`GetCorretoraQuery`, `GetEmissorQuery`, `GetProdutoQuery`, `GetTipoRendaQuery`, `GetObjetivoQuery`);
  - `IMovimentoReferenciasValidator.ValidarAsync` e `MovimentoReferenciasValidator`;
  - `ICreateMovimentoCommand` e `IUpdateMovimentoCommand`, que passam o token ao validator, ao repositório e ao `SaveAsync(cancellationToken)`.
- **Service:** ações de create e update de `MovimentosController`: parâmetro `CancellationToken cancellationToken` (D3), trocando `HttpContext.RequestAborted` onde existir.
- **Testes:** ajustar os testes e fakes dos 5 `GetX`, do validator e dos 2 commands; acrescentar, por agregado, um teste de que o token recebido chega ao repositório (fake que registra o token) e um de que cancelamento propaga `OperationCanceledException` sem log de erro.
- Remover da allowlist os itens concluídos.

**Critério de pronto:** build e testes verdes; allowlist menor; nenhuma chamada a `SaveAsync()` sem token nos 2 commands de Movimento.

---

## Fatia 3: Demais operações dos 5 catálogos

Começar por **Corretora** (modelo) e replicar, de forma mecânica, em Emissor, Produto, TipoRenda e Objetivo.

- **Persistence:** `GetAllAsync` e o que faltar (por exemplo, `GetAllWithRelatedEntitiesAsync` de Objetivo) ganham o token. Incluir os `DeleteAsync` hoje não declarados nas interfaces de `ProdutoRepository` e `ObjetivoRepository`, se existirem como membros concretos usados pelos commands.
- **Application:** queries de lista e `Get`, e commands create, update e delete, com token obrigatório; `SaveAsync(cancellationToken)`.
- **As 5 queries de lista** (`GetCorretorasListQuery`, `GetEmissoresListQuery`, `GetProdutosListQuery`, `GetTiposRendaListQuery`, `GetObjetivosListQuery`): trocar o `catch (Exception ex)` por `catch (Exception ex) when (ex is not OperationCanceledException)`, **na mesma fatia em que a query passa a receber o token**. Teste: cancelamento propaga e não loga erro.
- **Service:** todas as ações dos 5 controllers recebem o parâmetro `CancellationToken cancellationToken`.
- **Testes e fakes:** ajustar; manter os testes de multi-tenant existentes.

**Critério de pronto:** build e testes verdes; Corretora revisada antes de replicar; allowlist sem os catálogos.

---

## Fatia 4: Resumo de Objetivos

- `GetResumoObjetivosListQuery` e o repositório que ela usa: token obrigatório; mesmo `catch` com filtro de `OperationCanceledException`.
- Ação do `ObjetivosController` correspondente com `CancellationToken cancellationToken`.
- Testes e fakes ajustados; remover da allowlist.

**Critério de pronto:** build e testes verdes.

---

## Fatia 5: Resto de Movimento, Cotações e remoção das sobrecargas

- **Movimento:** `GetMovimentoQuery`, `DeleteMovimentoCommand`, `GetProximosVencimentosQuery` (hoje o repositório aceita o token com `= default`, mas a query e o controller não o passam) e `GetAsync` de Movimento (versão de 3 argumentos).
- **Sobrecargas antigas:** remover as sobrecargas e os `= default` que sobraram (`GetMovimentos` e variantes `GetPaged...` com token opcional), deixando uma única assinatura com token obrigatório; ajustar quem as chamava.
- **Cotações, Distribuição, Atualização por cotação e Desatualizados:** já têm o token obrigatório no Application e no Persistence (specs 0011, 0013, 0015). Trocar **somente** o uso de `HttpContext.RequestAborted` pelo parâmetro da ação (`CotacoesController`, `AtualizacaoPorCotacaoController`, `DistribuicaoValorLiquidoController` e as 2 ações de `MovimentosController` que ainda usam `RequestAborted`), conforme D3.
- Testes de controller: simplificar os que montavam `HttpContext` só por causa do token.

**Critério de pronto:** allowlist vazia; nenhuma ocorrência de `HttpContext.RequestAborted` nos controllers; nenhum `= default` para `CancellationToken` no Vault.

---

## Fatia 6: Limpeza, Web e fechamento

- Remover o uso de `SaveAsync()` sem token (18 ocorrências em Application, Persistence e Service no momento da escrita; conferir com busca) e os restos nos fakes. A interface `IBaseRepositoryManager` da lib continua com as duas sobrecargas; ver "Questões em aberto".
- **Web:** verificar o comportamento com requisição cancelada (aba fechada durante uma lista lenta): a PageModel recebe `TaskCanceledException`/`OperationCanceledException`, o `UseExceptionHandler("/Error")` responde 499 sem reexecutar a página de erro e o log fica em Debug, sem erro. Registrar o resultado no PR.
- Atualizar `Presentation.csproj` para `Lumia.Foundation.Http.Client` 0.5.1, se ainda não foi feito.
- Atualizar os READMEs e `Docs`: a regra "token obrigatório, último parâmetro" no guia do repositório.
- ADR 0017: **Status: Aceita**.

**Critério de pronto:** `dotnet build` sem warnings `CA2016`, testes verdes, allowlist vazia, ADR "Aceita".

---

## 5. Como validar (ponta a ponta)

1. `dotnet build Vault.slnx` e `dotnet test Vault.slnx`: verdes, sem `CA2016`.
2. O teste de reflexão falha se alguém adicionar um método sem token (verificar acrescentando um método de teste temporário e revertendo).
3. Teste manual na Api (`curl --max-time 0.01` ou aborto no cliente) em uma lista com banco lento: a requisição responde 499 e o log não registra erro; o pool não fica ocupado.
4. Teste manual no Web: abortar uma navegação durante o refresh de token: a sessão do usuário permanece (efeito da Fatia 1).
5. Conferir no log que cancelamentos aparecem apenas em Debug.

## 6. Riscos

- **Fatia grande de testes quebrados:** mitigada por fatias pequenas e allowlist que comprova o avanço.
- **Cancelamento no meio de escrita:** rollback integral; o cliente que cancelou não sabe se gravou (mesma semântica de falha de rede).
- **Conflito com trabalho em paralelo em Movimento:** nenhuma feature em andamento (a 0015 está em produção); novas features devem nascer já com a regra.
- **Allowlist apodrece:** o teste falha quando um item da lista já está em conformidade.

## 7. Fora de escopo

- `AuthenticationService`, `AuthenticationController` e `TokenController` (Identity não cancela; ver ADR 0017).
- Transação no `RegisterUser` e `UserManager` derivado.
- Handlers de exceção da lib e `ApiConnection`.
- Remoção do `SaveAsync()` sem token da lib `EFRepository` (ver abaixo).
- NetArchTest, ArchUnit ou qualquer dependência nova.

## 8. Questões em aberto

1. **`SaveAsync()` sem token na lib EFRepository (0.13.0):** a `IBaseRepositoryManager` mantém `SaveAsync()` e `SaveAsync(CancellationToken)`. Depois da Fatia 6 o Vault não usa mais a primeira. Remover exigiria a versão 0.14.0 da lib (mudança incompatível, só o Vault consome). Decidir se entra nesta spec ou fica para depois.
2. **Teste de reflexão para controllers:** estender a regra às ações públicas assíncronas dos controllers (com os 2 controllers de autenticação na allowlist permanente, justificados no ADR). Hoje a regra aprovada cobre `I*Query`, `I*Command` e `I*Repository`.
