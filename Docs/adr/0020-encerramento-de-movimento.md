# ADR 0019: Encerramento de Movimento (motivo e data)

- Status: Proposta (2026-10-26)

## Contexto

Hoje um Movimento deixa de valer desmarcando `EstaAtivo`. Esse único booleano mistura três situações diferentes: o dinheiro voltou e foi **gasto**, voltou e foi **reinvestido** em outro Movimento (com `EhReinvestimento = true`), ou voltou e **ainda não teve destino**. O Vault não consegue distinguir os casos, e isso prejudica a leitura do patrimônio de cada Objetivo e o cálculo do ritmo real de aporte (ADR 0020).

Além disso, a data em que o Movimento foi encerrado é registrada hoje em `DataVencimento`, o que mistura dois conceitos: o vencimento contratado do título e o dia em que o usuário de fato o encerrou, que pode ser anterior ao vencimento.

Vários Movimentos encerrados costumam ser reunidos em um único Movimento novo, de valor maior. Por isso um vínculo estruturado entre "origem" e "destino" (relação 1 para 1) não representa o uso real.

## Decisão

### Modelo

- Duas novas colunas em `Movimentos`, ambas anuláveis:
  - `MotivoEncerramento`: enum `Gasto`, `Reinvestido` ou `Pendente`, gravado como texto (`varchar(20)`) para a coluna ser legível e imune à reordenação do enum.
  - `DataEncerramento`: `date`, só a data, em UTC.
- Significado dos motivos:
  - `Gasto`: o dinheiro saiu do patrimônio do Objetivo.
  - `Reinvestido`: o dinheiro foi para outro(s) Movimento(s). O saldo já está neles.
  - `Pendente`: o dinheiro voltou e continua disponível, ainda sem destino definido. É também o estado de partida para os Movimentos já inativos.
- `EstaAtivo` **permanece** como coluna e passa a ser mantido pelo domínio, com a invariante: `EstaAtivo = true` se e somente se `MotivoEncerramento` é nulo. Remover a coluna obrigaria a reescrever índices e consultas (ADRs 0009, 0010, 0011, 0016 e 0018) sem ganho.
- A invariante é garantida por dois métodos na entidade `Movimento`, `Encerrar(motivo, data)` e `Reativar()`. Nenhum outro código altera `EstaAtivo` diretamente.

### Regras

- `DataEncerramento` é obrigatória quando há motivo, não pode ser futura (data UTC de hoje, via `IDateTimeProvider`) e não pode ser anterior à `DataInvestimento`.
- Reativar limpa o motivo e a data.
- O `ValorLiquidoAtual` de um Movimento encerrado fica congelado no último valor informado. Ele não é recalculado nem entra nas listas de manutenção (que já filtram `EstaAtivo`).
- Nada impõe que um Movimento `Reinvestido` tenha um Movimento com `EhReinvestimento = true` correspondente. A relação continua descrita no campo Observação.

### Api

- O `PUT api/movimentos/{id}` deixa de alterar `EstaAtivo` e o encerramento. O `PUT` completo passa a ser só edição de dados.
- Nova ação `PUT api/movimentos/{id}/encerramento`, corpo `{ motivo, data }`, resposta 204. Serve para encerrar e também para **reclassificar** (por exemplo, `Pendente` para `Gasto`).
- Nova ação `DELETE api/movimentos/{id}/encerramento`, resposta 204, para reativar.
- `POST api/movimentos` aceita, opcionalmente, motivo e data de encerramento, para cadastrar histórico já encerrado. Sem motivo, o Movimento nasce ativo.
- `MovimentoModel` e `MovimentoDto` passam a expor `MotivoEncerramento` e `DataEncerramento`.
- Todas as assinaturas novas recebem `CancellationToken` obrigatório (ADR 0017). O `UserId` é aplicado no repositório, e Movimento de outro usuário responde 404.

### Web

- A edição deixa de ter o checkbox "Ativo". Nova página `/Movimentos/Encerrar/{id}`, com motivo e data (padrão: hoje), acessível pela listagem. A mesma página reclassifica um Movimento já encerrado, e uma ação "Reativar" desfaz o encerramento.
- A listagem exibe motivo e data de encerramento no detalhe expansível; o filtro "Status" atual continua valendo.

### Persistência

- Migration gerada pelo mantenedor: `AddColumn` das duas colunas e, no mesmo arquivo, `UPDATE` dos Movimentos inativos existentes para `MotivoEncerramento = 'Pendente'` e `DataEncerramento = COALESCE(DataVencimento, DataAtualizacaoValorLiquido)`. O motivo é o mais conservador: o dinheiro não some do saldo até o usuário reclassificar. A data aproveita o hábito atual de registrar o encerramento em `DataVencimento`.
- Sem índice novo. Os índices `(UserId, EstaAtivo, ...)` seguem atendendo as consultas. Gatilho de reavaliação: se a projeção do ADR 0020 mostrar necessidade de filtrar por motivo em volume realista, medir com `EXPLAIN` antes de propor índice.

## Alternativas consideradas

- **Só o motivo, sem data própria:** perde a distinção entre vencimento e encerramento, que é o ponto central do uso atual.
- **Motivo, data e `ValorResgatado`:** daria o ganho realizado exato, mas acrescenta um campo a cada encerramento. O último `ValorLiquidoAtual` já aproxima o resgate. Pode voltar em ADR futuro, se a rentabilidade de Movimentos encerrados for necessária.
- **Vínculo estruturado `MovimentoOrigemId`:** descartado por não representar a fusão de vários encerrados em um novo Movimento.
- **Remover `EstaAtivo` e derivar do motivo:** modelo mais limpo, porém reescreve índices e todas as consultas já estabilizadas, e quebra o filtro de status. O custo não se paga.
- **Booleanos separados (`Gasto`, `Reinvestido`):** permitem estados inconsistentes (ambos verdadeiros). O enum torna o estado inválido irrepresentável.
- **Enum numérico no banco:** mais compacto, mas ilegível em consultas manuais e sensível à ordem dos valores.
- **Manter o encerramento dentro do `PUT` completo:** a edição geral voltaria a alterar um dado de controle, o mesmo problema que o ADR 0011 evitou com a data do valor líquido.

## Consequências

- Muda o contrato do `PUT api/movimentos/{id}` (deixa de aceitar `EstaAtivo`). O único consumidor é o Web, ajustado no mesmo esforço.
- Muda o schema: duas colunas em `Movimentos`, com migration e `UPDATE` de dados.
- `DataVencimento` nos Movimentos já inativos continua guardando o que o usuário digitou, e pode refletir o encerramento e não o vencimento original. Não há correção automática; fica a critério do usuário.
- Quem encerra deve atualizar antes o valor líquido, se quiser que o valor congelado represente o resgate.
- A tela de edição perde o atalho de desmarcar "Ativo": encerrar passa a ser um passo explícito.
- Viabiliza o ADR 0020 (projeção de Objetivo), que usa o motivo para decidir o que compõe o saldo e o ritmo de aporte.
- Os testes de domínio cobrem a invariante entre `EstaAtivo` e `MotivoEncerramento`, evitando que um caminho de código deixe os dois fora de sincronia.
