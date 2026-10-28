# ADR 0021: Projeção do Objetivo (meses para atingir a Meta)

- Status: Aceita (2026-10-28). Complementa o ADR 0014 e resolve a ressalva do ADR 0020 sobre o `Pendente`.

## Contexto

Um Objetivo tem `Meta` e `AporteMensal`, mas o Vault não diz **em quanto tempo** a Meta será atingida. Calcular isso só com o `AporteMensal` é frágil: ele é um desejo, não uma obrigação, e nem todo mês é cumprido. A estimativa precisa se apoiar no que **de fato** foi aportado e no que os investimentos **de fato** renderam, mantidos esses ritmos. Não precisa ser exata; uma aproximação honesta basta.

Já existem os dados necessários em `Movimento`: `ValorAporte`, `DataInvestimento`, `EhReinvestimento`, `ValorLiquidoAtual`, `DataAtualizacaoValorLiquido` e `EstaAtivo`, mais o motivo de encerramento do ADR 0020 (`Gasto`, `Reinvestido`, `Pendente`), que distingue dinheiro que saiu, dinheiro que foi para outro Movimento e dinheiro que voltou e segue disponível. A rentabilidade por tempo e capital já tem precedente nos ADRs 0013 e 0018.

## Decisão

### Saldo do Objetivo

`Saldo` é a soma de `ValorLiquidoAtual` dos Movimentos do Objetivo que estão **ativos** ou **encerrados como `Pendente`**. `Gasto` não conta (o dinheiro saiu) e `Reinvestido` não conta (o saldo já está nos Movimentos novos, e contar duas vezes inflaria a projeção).

**O mesmo conjunto passa a valer para a tabela de Objetivos da tela inicial.** `Qtde Movimentos`, `Total Aportado`, `Total Líquido` e `Percentual Meta` (ADR 0014) deixam de considerar só Movimentos ativos e passam a considerar **ativos e `Pendente`**. Os quatro usam o **mesmo conjunto**, para que `Total Líquido − Total Aportado` continue sendo um ganho que faça sentido, e para que a `Meta` seja medida e projetada sobre o mesmo número. A linha de totais do ADR 0014 continua somando as linhas exibidas e passa a refletir o conjunto novo. O detalhe da previsão destaca a parcela `Pendente`, porque é dinheiro disponível e ainda sem destino.

A rentabilidade histórica (abaixo) continua calculada **só sobre os ativos**: o rendimento dos encerrados já está realizado e congelado.

### Três cenários

| Cenário | Aporte mensal | Rentabilidade |
| --- | --- | --- |
| **Conservador** | realizado | 0% |
| **Realista** | realizado | histórica |
| **Planejado** | `AporteMensal` do Objetivo | histórica |

A distância entre Realista e Planejado mede a aderência ao plano. O cenário Realista é o destaque da tela.

### Aporte mensal realizado

- Conta o `ValorAporte` dos Movimentos com `EhReinvestimento = false`, **ativos ou encerrados** (qualquer motivo): é dinheiro novo que foi investido, ainda que depois tenha sido gasto. Reinvestimentos ficam de fora, pois já estavam no saldo.
- Janela: até **12 meses completos anteriores ao mês corrente**. O mês corrente é excluído por estar incompleto e puxaria a média para baixo.
- Se o histórico for menor, a janela começa no mês do primeiro Movimento elegível do Objetivo. Meses sem aporte dentro da janela contam como zero.
- `Aporte realizado = soma dos aportes na janela / meses da janela`.
- Sem nenhum mês completo de histórico (primeiro aporte no mês corrente, ou nenhum aporte), o aporte realizado é **indisponível**, e os cenários que dependem dele resultam em "sem previsão".

### Rentabilidade histórica

Calculada **somente sobre os Movimentos ativos**, pela mesma ideia do ADR 0013 (capital e tempo), sem relógio:

- `A_i` = `ValorAporte`, `V_i` = `ValorLiquidoAtual` e `t_i` = `(DataAtualizacaoValorLiquido − DataInvestimento).Days / 365`, mínimo zero. O período termina na data de atualização do valor, e não em "hoje", pelo mesmo motivo do ADR 0018: um valor parado há semanas não pode parecer render menos.
- `T = Σ(A_i × t_i) / ΣA_i` (tempo médio ponderado pelo capital, em anos).
- Taxa anual efetiva: `r = (ΣV_i / ΣA_i)^(1 / T) − 1`.
- **Limites:** `T` abaixo de **90 dias** (ou nenhum Movimento ativo com aporte) resulta em taxa **0%** e sinaliza **baixa confiança**. A taxa é limitada ao intervalo de **0% até um teto anual** (constante nomeada, **30% ao ano**): rentabilidade negativa vira 0%, pois projetar prejuízo contínuo não ajuda a decidir, e o teto impede que um período curto de sorte prometa o impossível.
- Taxa mensal: `i = (1 + r)^(1/12) − 1`.

### Fórmula do prazo

Com `S` = saldo, `M` = `Meta`, `A` = aporte mensal do cenário e `i` = taxa mensal, o número de meses `n` para que `S` atinja `M`, com aportes e rendimento mensais, é:

- `i > 0`: `n = ln((M·i + A) / (S·i + A)) / ln(1 + i)`.
- `i = 0`: `n = (M − S) / A`.

A forma fechada evita simular mês a mês e é fácil de testar. O resultado é arredondado **para cima**, em meses inteiros.

### Situações

Cada cenário devolve uma situação, nunca um número inventado:

| Situação | Quando |
| --- | --- |
| **Atingida** | `S ≥ M` (vale para os três cenários) |
| **Prevista** | prazo calculado e até 100 anos (1.200 meses), com a data prevista (mês e ano) |
| **Mais de 100 anos** | prazo calculado acima de 1.200 meses |
| **Sem previsão** | aporte do cenário indisponível ou menor ou igual a zero com taxa zero; `S·i + A ≤ 0`; ou `Meta` menor ou igual a zero |

### Camadas

- **Domain:** serviço puro e estático, `ProjecaoObjetivoCalculador` (pasta própria dentro de `Domain/Objetivos`), no estilo do `RentabilidadeCalculador` e do `DistribuicaoValorLiquidoCalculador`: sem I/O e **sem relógio**; recebe os Movimentos do Objetivo, `Meta`, `AporteMensal` e a data de hoje. Dinheiro em `decimal`; `double` apenas nos logaritmos e potências, com verificação de valor finito.
- **Application:** a `GetResumoObjetivosListQuery` já carrega os Movimentos de todos os Objetivos (`Include`), inclusive os encerrados, então a projeção sai **sem consulta nova**. Passa a receber `IDateTimeProvider` (UTC) para ler "hoje" **uma vez** por execução. Os campos novos entram no `ResumoObjetivoModel`, e `QtdeMovimentos`, `TotalAportado`, `TotalLiquido` e `PercentualMeta` passam a usar o conjunto do saldo (ativos e `Pendente`).
- **Service:** campos **aditivos** em `GET api/objetivos/resumo` (o Web é o único consumidor). Os campos atuais mantêm nome e tipo, mas `QtdeMovimentos`, `TotalAportado`, `TotalLiquido` e `PercentualMeta` passam a incluir os `Pendente`. Um objeto `Projecao` com: a parcela pendente do saldo (o saldo em si é o `TotalLiquido`), aporte realizado e aporte planejado, taxa mensal histórica, indicador de baixa confiança e os três cenários, cada um com situação, meses e data prevista. Enum de situação como **texto**, por atributo nas propriedades dos DTOs, como no ADR 0020.
- **Web:** nova coluna **Previsão** na tabela de Objetivos da tela inicial, com o cenário Realista (por exemplo, "18 meses (mar/2028)", "Meta atingida", "Sem previsão", "Mais de 100 anos"). O detalhe expansível, no padrão da lista de Movimentos, mostra os três cenários e as premissas: saldo considerado (com a parcela pendente), aporte realizado e planejado, rentabilidade estimada e, se houver, o aviso de **baixa confiança**. A linha de totais (ADR 0014) não ganha previsão.
- **Sem schema, sem migration, sem índice e sem dependência nova.** O contrato do LumiaFoundation não muda.

### Limitações registradas

- A `Meta` é **nominal**: não há inflação.
- `ValorLiquidoAtual` desatualizado subestima o saldo e a taxa; a manutenção do valor líquido (ADRs 0011, 0013 e 0016) continua sendo responsabilidade do usuário.
- Rentabilidade passada não garante a futura, e a taxa mistura Movimentos de renda fixa e de ações.
- Um aporte isolado muito grande distorce a média do ritmo realizado. A mediana seria menos sensível, mas descartaria aportes legítimos e esporádicos.
- Quando vários Movimentos encerrados são reunidos em um novo (`EhReinvestimento = true`), a parcela de dinheiro realmente novo dentro dele não conta como aporte realizado.
- A projeção não considera datas de vencimento nem resgates futuros.

## Alternativas consideradas

- **Só o cenário planejado:** descartado, é o ponto frágil que motivou o ADR.
- **Só o cenário realista:** perde a leitura da aderência ao plano, que é a informação mais útil para o usuário ajustar o `AporteMensal`.
- **Campo de rentabilidade esperada no Objetivo:** mais um dado para o usuário manter e que envelhece. Derivar do histórico não exige nada dele.
- **Simulação mês a mês no lugar da forma fechada:** permitiria aportes variáveis, mas é mais código, mais difícil de testar e desnecessário para uma aproximação.
- **TIR ou rentabilidade ponderada pelo tempo para a taxa:** mais precisas, porém exigem fluxo de caixa completo, que o Vault não guarda. A ponderação por capital e tempo do ADR 0013 já é aceita pelo usuário.
- **Mediana no lugar da média do aporte:** menos sensível a picos, porém subestima o ritmo de quem investe de forma irregular.
- **Contar o mês corrente na janela:** usaria o dado mais recente, mas um mês incompleto subestimaria o ritmo na maior parte do mês.
- **Calcular a projeção na Application ou em SQL:** a regra é pura e merece teste isolado; o Domain a mantém sem banco e sem relógio.
- **Endpoint próprio de projeção por Objetivo:** uma segunda consulta, com risco de divergir do resumo e sem ganho, já que os dados chegam na consulta que a tela inicial usa.
- **Manter a tabela só com ativos e expor o saldo da projeção à parte:** não alteraria o ADR 0014, mas a tela mostraria dois patrimônios para o mesmo Objetivo, e o `% Meta` e a previsão usariam bases diferentes.
- **ADR próprio para a mudança da tabela:** descartado, porque a definição de saldo é única e a projeção depende dela; separar criaria duas decisões acopladas.
- **Ignorar `Pendente` no saldo da projeção:** coerente com a tabela atual, porém subestima o saldo de quem tem dinheiro devolvido ainda sem destino.

## Consequências

- Viabilizada pelo ADR 0020: sem o motivo de encerramento não haveria como separar dinheiro gasto, reinvestido e pendente.
- O contrato de `GET api/objetivos/resumo` ganha campos aditivos, e os campos atuais **mudam de significado** (passam a incluir `Pendente`) sem mudar nome nem tipo. Api e Web são publicados juntos. O ADR 0014 passa a constar como complementado por este, e a ressalva do ADR 0020 sobre o `Pendente` fica resolvida.
- Uma devolução ainda não classificada continua contando para a Meta. Ao classificá-la como `Gasto`, o saldo e o `% Meta` caem, o que é o comportamento esperado.
- Movimentos `Pendente` esquecidos inflam o saldo, o `% Meta` e a previsão; classificá-los (`Gasto` ou `Reinvestido`) mantém os números honestos.
- A projeção é uma estimativa com premissas explícitas na tela; o aviso de baixa confiança evita leitura precisa de um histórico curto.
- Constantes nomeadas e ajustáveis em um só lugar: janela de 12 meses, mínimo de 90 dias, teto de 30% ao ano e limite de 100 anos.
- A `GetResumoObjetivosListQuery` ganha dependência de `IDateTimeProvider` e passa a depender dos testes do calculador para as regras numéricas.
- Testes: do `ResumoObjetivoModel` (ativos e `Pendente` dentro dos quatro campos; `Gasto` e `Reinvestido` fora), unitários do calculador (valores conhecidos da fórmula, taxa zero, saldo já acima da meta, limites de 90 dias, do teto e dos 100 anos, janela e meses sem aporte, reinvestimento fora do ritmo, `Gasto` e `Reinvestido` fora do saldo, `Pendente` dentro), da query e do contrato JSON.
