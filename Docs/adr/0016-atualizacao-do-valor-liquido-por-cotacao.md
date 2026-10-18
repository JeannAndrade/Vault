# ADR 0016: Atualização do Valor Líquido por cotação

- Status: Proposta (2026-10-17, revisada em 2026-10-18)

## Contexto

Alguns Movimentos (por exemplo, ações) têm o valor líquido determinado pela cotação do ativo no dia: `Quantidade × cotação`. O ativo é identificado pelo nome do Emissor (PETR4, VALE3 etc.). Hoje o `ValorLiquidoAtual` é digitado Movimento a Movimento (ADR 0011) ou distribuído por título (ADR 0013), o que não serve para esses casos: um mesmo papel pode estar em várias corretoras e o valor de cada posição é uma multiplicação exata.

Além do cálculo, o usuário quer um histórico de cotações que **sobreviva** ao Movimento e ao Emissor: ele pode vender o papel e continuar monitorando o preço para decidir uma recompra.

## Decisão

### Movimento

- Nova coluna `AtualizaPorCotacao` em `Movimentos` (`tinyint(1)`, obrigatória, padrão `false`). Os Movimentos existentes nascem `false`; não há carga de dados.
- Com a flag marcada, `Quantidade` passa a ser **obrigatória e maior que zero** no `POST` e no `PUT` do Movimento (Api e Web). Na Api a regra é um `IValidatableObject` nos modelos de criação e atualização, executado pelo `CommandValidator`.
- `MovimentoModel` e `MovimentoDto` (Api e Web) expõem a flag. Create e Edit ganham um checkbox.
- Regra de cálculo no próprio `Movimento` (`AplicarCotacao`): `ValorLiquidoAtual = Quantidade × cotação`, arredondado a 2 casas (`MidpointRounding.AwayFromZero`), e `DataAtualizacaoValorLiquido = hoje` (data da atualização, só a data, UTC; não a data da cotação).
- O `PUT` completo continua **sem** renovar `DataAtualizacaoValorLiquido` (regra do ADR 0011).

### Entidade Cotação

- Nova entidade `Cotacao` (tabela `Cotacoes`): `Id` (coluna `CotacaoId`), `UserId`, `Papel` (`varchar(60)`), `Valor` (`decimal(18,4)`) e `Data` (`date`).
- **Sem chave estrangeira e sem navegação** para `Emissor` e para `User`. O papel é guardado como texto; renomear ou excluir o Emissor não altera o histórico, e o histórico não impede excluir o Emissor.
- **Uma cotação por papel por dia**: índice único `IX_Cotacoes_UserId_Papel_Data` em `(UserId, Papel, Data)`. O índice também atende o gráfico e o "último valor".
- O texto do papel é gravado com `Trim`. A comparação entre `Cotacao.Papel` e `Emissor.Nome` é por nome; no MariaDB a collation `utf8mb4` é insensível a maiúsculas, então `PETR4` e `petr4` são o mesmo papel (os testes com provider InMemory são sensíveis; ver Consequências).
- `Valor` deve ser maior que zero (teto de `decimal(18,4)`). `Data` não pode ser futura (comparada à data UTC de hoje).
- CRUD completo de Cotação, escopado por `UserId` em todos os caminhos. No CRUD, `POST` com `(Papel, Data)` já existente responde **422** e `PUT` que leve o registro a colidir com outro também. O **upsert** vale apenas na tela de atualização por cotação.

### Atualização por cotação (tela em "Manutenção")

- A lista traz os **papéis distintos** (`Emissor.Nome`) dos Movimentos **ativos** com a flag marcada, cada um com a última cotação registrada (valor e data) ou "sem cotação". Há um campo de valor e outro de data (padrão: hoje, editável).
- Confirmar um papel executa, em **uma única transação**: upsert da `Cotacao` e, se a data informada for a **mais recente registrada** para o papel, o recálculo de todos os Movimentos ativos com flag daquele papel. Se a data for anterior à mais recente (registro retroativo), grava-se só o histórico.
- `POST api/movimentos/atualizacao-por-cotacao` responde **200** com `movimentosAtualizados` e `quantidadeMovimentos`, para a tela informar se houve recálculo.
- Movimentos ativos com flag e `Quantidade` nula (só possível por edição direta no banco) são ignorados no recálculo.
- Movimentos com a flag **não aparecem** em "Manutenção Valor Líquido" (ADR 0011/0015) nem entram na Distribuição (ADR 0013). Para não perder o sinal de defasagem, a tela de cotação exibe também a data da última cotação de cada papel.

### Gráfico

- Página com seleção do papel (papéis distintos de `Cotacoes`, inclusive os sem Movimento) e gráfico de linhas Valor × Data com **todos** os registros, sem filtro de período.
- Biblioteca: **Chart.js**, baixada por LibMan (`libman.json`, como Bootstrap) e servida localmente. Eixo de categorias (datas como rótulos), sem adaptador de datas, evitando uma segunda dependência. Os dados vêm da Api e são entregues à página como JSON em atributo `data-*`; o script fica em arquivo estático (`wwwroot/js`), sem script inline.

### Api

Controllers separados, para não inflar o `MovimentosController`:

- `CotacoesController` (`api/cotacoes`): CRUD, listagem paginada (ADR 0008) com filtro opcional `papel`, `GET api/cotacoes/papeis` e `GET api/cotacoes/serie?papel=` (série completa, `Data` ascendente, para o gráfico).
- `AtualizacaoPorCotacaoController` (`api/movimentos/atualizacao-por-cotacao`): `GET` (lista da tela) e `POST` (confirmação, 200 com resultado).
- Todas as novas assinaturas de repositório, query e command recebem `CancellationToken` **obrigatório**.

### Validação de requisição (LumiaFoundation)

Com `SuppressModelStateInvalidFilter` ligado, corpo nulo ou `ModelState` inválido não são barrados pelo framework. O filtro existente da lib (`DtoNotEmptyValidationAttribute`) localiza o DTO pelo texto do `ToString()`, responde 400 em texto puro e inglês e 422 com o `ModelState` cru, formatos diferentes do `ErrorDetails` do resto da Api, e não serve para `GET`. Por isso:

- Novo filtro aditivo em `Lumia.Foundation.AspNetCore` 0.29.0, `RequestValidationAttribute`: detecta o corpo pelo `BindingSource.Body` do descritor da ação, e lança `HttpBaseException` 422 (saída em `ErrorDetails`) para corpo nulo ou `ModelState` inválido, inclusive em `GET`.
- O `DtoNotEmptyValidationAttribute` permanece inalterado.
- Os controllers novos usam `[RequestValidation]`. O `DistribuicaoValorLiquidoController` (que tinha um `ValidarCorpo` próprio) e o `GetMovimentos` (bloco inline de `ModelState`) são refatorados para usá-lo.

### Persistência

Mudança de schema: coluna `AtualizaPorCotacao` e tabela `Cotacoes`. Migration gerada pelo mantenedor, **depois** da migration de saneamento do `IdentityRole` (spec 0015, Passo 1).

## Alternativas consideradas

- **Calcular o valor na leitura (sem gravar `ValorLiquidoAtual`):** o resumo de objetivos, a listagem e as demais telas leem `ValorLiquidoAtual`; haveria duas fontes de verdade.
- **Chave estrangeira de `Cotacao` para `Emissor`:** contraria o requisito de monitorar papéis que o usuário já não possui.
- **Cálculo em serviço de domínio estático (como na 0013):** o cálculo é uma multiplicação sobre dados do próprio Movimento; um método na entidade basta.
- **Várias cotações por papel por dia:** o gráfico e o "último valor" ficariam ambíguos.
- **Upsert também no `POST` do CRUD:** substituir em silêncio surpreenderia quem chama a Api; o CRUD distingue criar de editar.
- **Gráfico em SVG gerado no servidor:** sem dependência, mas reinventa eixos, escala e tooltip.
- **Componente Blazor:** decisão de front-end maior, fora desta feature.
- **Reaproveitar `TituloFiltro`/`GetAtivosPorTituloAsync`:** sem uso e filtram por título, não por papel; foram removidos.
- **Usar o `DtoNotEmptyValidationAttribute` como está, ou criar `ValidarCorpo` local no Vault:** o primeiro herda a heurística e os formatos inconsistentes; o segundo duplica responsabilidade da lib.
- **Alterar o `DtoNotEmptyValidationAttribute`:** mudaria status e corpo de resposta de um contrato público já usado pelos controllers de autenticação.

## Consequências

- Movimentos com flag ficam fora da Manutenção Valor Líquido e da Distribuição; a defasagem deles é vista na tela de cotação, pela data da última cotação.
- Quantidade obrigatória com a flag muda a validação de criação e edição de Movimento.
- Nova dependência de front-end (Chart.js) e primeiro JavaScript próprio do Web. Como `wwwroot/lib` não é versionado, o `libman restore` deve ser executado antes do `docker compose build`, como já ocorre com o Bootstrap.
- Nova versão da lib (`AspNetCore` 0.29.0), publicada antes de o Vault consumi-la (como nos ADRs 0008 e 0011).
- O `GetMovimentos` e o `DistribuicaoValorLiquidoController` mudam de mecanismo de validação sem mudar o status (422); a mensagem de parâmetros inválidos do `GetMovimentos` passa a ser a genérica da lib.
- Duas confirmações simultâneas do mesmo papel e da mesma data podem violar o índice único (a segunda falha com erro); aceito em app pessoal.
- A regra de maiúsculas/minúsculas depende da collation do MariaDB; os testes InMemory não a reproduzem. Validar uma vez contra MariaDB (Docker).
- Entre 21h e 24h (BRT) a data UTC de "hoje" já é o dia seguinte; o campo de data da tela pode vir com um dia à frente do calendário local (editável).
- A tabela `Movimentos` ganha uma coluna; `Cotacoes` ganha um índice único. Sem novo índice em `Movimentos`.
- Complementa os ADRs 0011 (excluídos da lista de desatualizados) e 0013 (excluídos da distribuição).
