# Application

Casos de uso do Vault (commands e queries) e as **portas** que eles precisam da infraestrutura. Não conhece banco, EF Core, ASP.NET Core nem logger. Decisões: [ADR 0023](../Docs/adr/0023-inversao-da-persistencia-e-pacotes.md) e [ADR 0024](../Docs/adr/0024-fim-do-log-e-relancamento-nas-queries.md).

## Dependências permitidas

- Projeto: `Domain`.
- Pacote: somente `Lumia.Foundation.Core` (`CommandValidator`, `PagedList`, exceções de domínio).
- `Persistence` e `Service` dependem da Application, nunca o contrário. O teste `Service.Test/Arquitetura/DependenciasEntreCamadasTests` falha se isso mudar.

## Estrutura

- `<Agregado>/Commands/<Caso>` e `<Agregado>/Queries/<Caso>`: uma interface `I<Caso>` e a classe `<Caso>` no mesmo assembly. O Service registra todas por convenção (`AddServicesFromAssembly`).
- `Repositories/`: portas implementadas pela Persistence (`IRepositoryManager`, `I<Entidade>Repository`) e os tipos de filtro e projeção que elas usam.
- `Pagination/`: `PaginationParameters`.

## Regras

- **Multi-tenant:** todo caso de uso recebe `ownerId` e o repassa ao repositório. Nenhum acesso sem escopo de usuário.
- **Validação primeiro:** `CommandValidator.Validate(...)` antes de qualquer acesso a dados; erro do cliente vira 422.
- **`CancellationToken`:** obrigatório, último parâmetro e repassado até o `SaveAsync` e o EF Core (ADR 0017).
- **Sem `try/catch` para logar e relançar:** o registro de falhas fica nos handlers da Api (ADR 0024). Capture exceção só para tratá-la de fato.
- **Acesso a dados só por `IRepositoryManager`:** não injete repositórios isolados.

## Como adicionar um caso de uso

1. Crie `I<Caso>` e `<Caso>` em `<Agregado>/Commands` ou `<Agregado>/Queries`, recebendo `IRepositoryManager` no construtor.
2. Se faltar operação no repositório, declare-a na interface em `Repositories/` e implemente na Persistence (ver o [README da Persistence](../Persistence/README.md)).
3. Escreva os testes em `Application.Test`, com `Moq` ou o `FakeRepositoryManager`, cobrindo o caminho feliz, a validação, a propagação de exceção e o cancelamento.
4. Não é preciso registrar nada no contêiner: a convenção `I<Classe>` cuida disso.

## Como testar

`dotnet test Application.Test`
