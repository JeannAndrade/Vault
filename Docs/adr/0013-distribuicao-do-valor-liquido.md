# ADR 0013: Distribuição do Valor Líquido entre aportes por capital e tempo

- Status: Aceita (2026-10-15). Complementada pelo ADR 0016 (Movimentos por cotação ficam fora da distribuição).

## Contexto

Um mesmo título (mesmo Tipo de Renda, Corretora, Produto e Emissor) pode ter vários aportes ao longo do tempo, cada um registrado como um Movimento. A corretora informa o valor líquido do título como um todo, mas o Vault guarda `ValorLiquidoAtual` por Movimento. Não há preço unitário nem histórico de PUs, então não é possível calcular o valor de cada aporte diretamente.

Distribuir o valor total apenas proporcionalmente ao valor investido seria incorreto, porque ignora o tempo: o dinheiro aplicado há dois anos rendeu mais do que o aplicado há um mês. A precisão exata não é exigida; basta o critério ser conceitualmente aceitável e o resultado fechar exatamente no total informado.

A feature complementa a atualização individual do ADR 0011: ela passa a ser feita em lote, por título, com o valor distribuído pelo sistema.

## Decisão

### Algoritmo (ponderação por capital e tempo)

Notação: `A_i` é o valor do aporte; `t_i` é o tempo em anos, `(hoje − DataInvestimento).Days / 365`; `V` é o valor líquido total informado; `S = ΣA_i`; `G = V − S` (lucro total, pode ser negativo).

1. Tempo médio ponderado pelo capital: `T = Σ(A_i × t_i) / S`.
2. Taxa efetiva média: `r = (V / S)^(1 / T) − 1`.
3. Ganho projetado de cada aporte: `g_i = A_i × ((1 + r)^t_i − 1)`.
4. Normalização: `Ganho_i = G × g_i / Σg`. A soma dos ganhos fecha em `G` e um aporte com `t_i = 0` fica com ganho zero.
5. `Valor_i = A_i + Ganho_i`, arredondado a 2 casas.
6. O aporte **mais recente** (ordenação por `DataInvestimento` e depois `Id`, o último da lista) absorve a diferença de arredondamento, de modo que `ΣValor_i = V` exatamente.

Regras:

- Tempo em dias corridos; a data de hoje vem do `IDateTimeProvider` (UTC, só a data). Um `t_i` negativo (data futura, que não deveria existir) é tratado como zero.
- Aportes com `EhReinvestimento = true` entram normalmente.
- Lucro negativo é permitido (`V < S`).
- O `double` é usado somente nos pesos (`Math.Pow`); todos os valores monetários ficam em `decimal`.

### Casos degenerados

| Caso | Tratamento |
| --- | --- |
| `G = 0` | Todos os ganhos são zero (evita divisão por zero em `Σg`) |
| `V = 0` (perda total) | Todos os valores finais são zero |
| Aporte único | Recebe todo o ganho |
| Todos os aportes com `t = 0` e `G ≠ 0` | Rejeitado com 422: não há base de tempo para distribuir |
| Algum `Valor_i` resultante negativo (prejuízo extremo) | Bloqueado: o preview exibe aviso e a gravação é rejeitada com 422 |

### Filtro e escopo

- Os quatro filtros (`TipoRendaId`, `CorretoraId`, `ProdutoId`, `EmissorId`) são **obrigatórios** (`[NotEmptyGuid]`). O conjunto-alvo é sempre `UserId = dono` e `EstaAtivo = true`.
- Sem paginação e sem limite de itens: o lote é o conjunto inteiro do título.
- Tipo de filtro próprio de quatro campos, em cada camada. Não se reutiliza `MovimentoFiltro` (ADR 0012), que é opcional, tem seis campos e é paginado.
- Filtro sem aportes ativos: a simulação responde 200 com lista vazia (o Web desabilita a confirmação); a gravação responde 422 com mensagem clara.
- Objetivo inativo não filtra (como nos ADRs 0010 e 0011).

### Camadas

- **Domain:** serviço de domínio puro `DistribuicaoValorLiquidoCalculador`, sem I/O e sem relógio. Recebe os aportes (valor e data), o valor total e a data de referência; devolve a distribuição ou um resultado de erro de regra.
- **Persistence:** novo método de repositório que devolve os Movimentos ativos do título, com as entidades relacionadas, ordenados por `DataInvestimento` e `Id` ascendentes, com `CancellationToken` obrigatório. Para gravar, uma variante com `trackChanges`.
- **Application:** uma query de simulação e um command de gravação, ambos com `CancellationToken` obrigatório (sem valor padrão) e o `IDateTimeProvider` (já registrado na Api pelo ADR 0011). A query não grava nada.
- **Service:** **controller novo**, `DistribuicaoValorLiquidoController`, em `api/movimentos/distribuicao-valor-liquido`, separado do `MovimentosController`, que já tem muitas dependências.
  - `POST api/movimentos/distribuicao-valor-liquido/simulacao`: corpo com os quatro filtros e `ValorLiquidoTotal` (maior ou igual a zero); resposta 200 com a distribuição por aporte.
  - `POST api/movimentos/distribuicao-valor-liquido`: mesmo corpo; resposta 204.
  - Os dois são `POST` porque são ações de cálculo com corpo, não recursos.
- **Gravação:** a Api **recalcula** a distribuição a partir do banco e nunca recebe valores calculados pelo Web. Atualiza `ValorLiquidoAtual` e `DataAtualizacaoValorLiquido` (hoje, só a data) de todos os aportes do lote em uma única transação (`SaveAsync(cancellationToken)`).
- **Web:** nova página `/Movimentos/Distribuicao`, no menu "Manutenção". Quatro selects obrigatórios (sem a opção `--`) e o campo de valor total. O handler **Simular** exibe o preview (aporte, data, valor atual, ganho e novo valor, com o total e a taxa efetiva anual); o handler **Confirmar** grava e redireciona (PRG).

### Persistência

Sem alteração de schema nem migration. Os filtros usam as FKs já indexadas em `(UserId, X)` e o volume por título é pequeno. Gatilho de reavaliação: se o `EXPLAIN` em volume realista mostrar varredura relevante, abrir novo ADR.

## Alternativas consideradas

- **Solver numérico para uma taxa única que feche o total:** mais rigoroso, mas desnecessário, já que a precisão exata não é exigida, e traria código numérico a testar e manter.
- **Distribuir só proporcional ao capital:** descarta o tempo; contraria o requisito.
- **Normalizar sobre o valor final (`Valor_i = F_i × V / ΣF`):** nunca gera valor negativo, mas faria um aporte de hoje receber lucro, violando a regra de que ele contribui zero.
- **Limitar valores em zero e redistribuir o resíduo:** mais complexo e difícil de explicar no preview. O bloqueio com aviso é mais simples e honesto.
- **Web calcula e envia os valores por aporte:** coloca regra de negócio no cliente e permite gravar valores arbitrários.
- **Carimbo (hash) do preview e 409 se a lista mudou:** mais seguro, porém mais peças; o risco é baixo num controle pessoal. Pode ser adotado em ADR futuro.
- **Reutilizar `MovimentoFiltro` do ADR 0012:** o contrato prometeria filtros ignorados e traria paginação indevida.
- **Acrescentar as ações ao `MovimentosController`:** aumenta ainda mais as dependências do controller.
- **Colocar o cálculo na LumiaFoundation:** é regra financeira específica do Vault e alteraria o contrato público da lib.

## Consequências

- O resultado é uma **estimativa conceitual**, não a rentabilidade real: se um dia houver PUs reais, a distribuição pode ser substituída por valores exatos.
- Os valores dependem da data de hoje (UTC). Entre 21h e 24h (BRT) "hoje" já é o dia seguinte; o efeito é de um dia no tempo de cada aporte.
- Como a Api recalcula na gravação, se algum aporte do título mudar entre o preview e a confirmação, os valores gravados podem diferir dos exibidos.
- A gravação sobrescreve os `ValorLiquidoAtual` anteriores, sem histórico e sem desfazer.
- Os aportes do lote passam a ter `DataAtualizacaoValorLiquido` igual a hoje e saem da lista de desatualizados (ADR 0011).
- Prejuízos muito grandes podem ser bloqueados pela regra de valor negativo; o usuário deve usar a atualização individual nesses casos.
- Sem limite de itens, um título com muitos aportes processa todos de uma vez; aceitável na escala atual.
- Serviço de domínio puro, testável sem mocks, com testes cobrindo cada caso degenerado e a soma exata.

## Implementação e fechamento

- Os erros de regra da distribuição são convertidos em `CommandValidationException`, preservando a resposta HTTP 422 já utilizada pela aplicação.
- A query e o command são descobertos e registrados pelo `AddServicesFromAssembly`; não foi necessário registro explícito no `Program.cs`.
- A feature não altera schema, migration ou índice e não há README ou índice de ADRs separado no repositório para atualizar.
