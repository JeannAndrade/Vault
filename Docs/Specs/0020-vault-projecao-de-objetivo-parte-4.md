# Especificação técnica 0020: Projeção do Objetivo (Vault), parte 4

- Data: 2026-10-28
- Status: **implementada (2026-10-28)**
- ADR relacionado: [0021](../adr/0021-projecao-de-objetivo.md)
- Partes anteriores: [parte 1](0020-vault-projecao-de-objetivo.md) (Passos 0 e 1), [parte 2](0020-vault-projecao-de-objetivo-parte-2.md) (Passo 2), [parte 3](0020-vault-projecao-de-objetivo-parte-3.md) (Passo 3)
- Pré-requisito: spec 0019 (Encerramento de Movimento) implementada, e as partes 1 a 3 desta spec.
- Repositório afetado nesta parte: **Presentation (Web)**. Sem schema, sem migration, sem dependência nova e sem alteração no LumiaFoundation.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Ajuste na parte 3

Nenhum. A parte 3 já deixou o contrato da Api fechado (`projecao` dentro de cada item de `GET api/objetivos/resumo`, enum como texto). Esta parte só **consome** esse contrato.

## 2. Resumo

O Web passa a ler o objeto `projecao` e a exibi-lo na tela inicial:

- coluna **Previsão** na tabela de Objetivos, com o cenário **Realista** como destaque;
- detalhe expansível por Objetivo (mesmo padrão de colapso da lista de Movimentos) com os três cenários e as premissas;
- a linha de **Totais** não ganha previsão (ADR 0014 e ADR 0021).

O Web continua sem referenciar Domain, Application ou Service: tem seus próprios DTOs e sua própria cópia do enum `SituacaoProjecao`, ligados à Api só pelo contrato JSON, guardado por testes.

## 3. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D29 | O Web tem a **própria cópia** do enum `SituacaoProjecao` (`Presentation.Objetivos`), mesmos nomes da Api; `[JsonConverter(typeof(JsonStringEnumConverter<SituacaoProjecao>))]` na propriedade do DTO, como no `MotivoEncerramento` (spec 0019, D29) |
| D30 | `ResumoObjetivoDto` (record posicional) ganha o parâmetro **final** `ProjecaoObjetivoDto? Projecao = null`. Anulável com padrão nulo mantém compiláveis os usos atuais (`IndexModelTests`, `TotaisObjetivosTests`) e deixa o Web tolerante a uma Api que ainda não devolva o objeto. A tela trata nulo como "sem previsão" |
| D31 | Novos `ProjecaoObjetivoDto` e `CenarioProjecaoDto`, records posicionais `sealed`, no padrão do `ResumoObjetivoDto`; mesmos nomes e tipos da parte 3 (D22 e D23) |
| D32 | **Texto num formatador estático e testado**, `ProjecaoFormatter` (`Presentation/Objetivos/`), no padrão do `FluxoMensalFormatter`; a view só chama o formatador, sem regra de texto no `.cshtml` |
| D33 | Meses abreviados por **tabela fixa** em português (`jan`, `fev`, `mar`, `abr`, `mai`, `jun`, `jul`, `ago`, `set`, `out`, `nov`, `dez`), não por `CultureInfo`: a abreviação da cultura `pt-BR` varia com a versão do ICU (`mar` ou `mar.`) e quebraria testes entre ambientes |
| D34 | Coluna **Previsão** mostra só o **Realista**, em destaque (`fw-semibold`). Um ícone de alerta com `title` aparece ao lado quando `BaixaConfianca` for verdadeiro. Conservador e Planejado ficam no detalhe |
| D35 | Detalhe expansível por **linha de Objetivo**, no mesmo padrão do `Movimentos/Index.cshtml` (botão com chevron, `data-bs-toggle="collapse"`, `aria-controls`, `aria-expanded`), com id `projecao-{id}`. Sem JavaScript novo |
| D36 | `Projecao` nula: a célula mostra "—" e **não** há botão de detalhe. Evita linha de detalhe vazia |
| D37 | Linha de totais: apenas ajusta o `colspan` à coluna nova; a célula de Previsão fica vazia. Sem previsão agregada (somar cenários de Objetivos distintos não tem sentido) |
| D38 | Pontuação do texto "1 mês" no singular e "N meses" no plural; "Mais de 100 anos" (e não "1200 meses") para `MaisDeCemAnos` |

## 4. Atenção antes de começar

1. **`ResumoObjetivoDto` é record posicional.** Adicionar parâmetro no meio quebraria todos os construtores; por isso o parâmetro novo entra **no fim** e com padrão nulo (D30).
2. **Nomes do enum são contrato.** `SituacaoProjecao` no Web precisa ter exatamente `Prevista`, `Atingida`, `MaisDeCemAnos` e `SemPrevisao`. Um teste fixa os nomes, como o do `MotivoEncerramento`.
3. **Desserialização do enum.** Se o valor vier desconhecido (Api mais nova que o Web), o `JsonStringEnumConverter` lança. Como Api e Web saem juntos, isso é aceito; o teste de contrato cobre os quatro valores atuais.
4. **Moeda em `pt-BR`.** O `.NET` formata `R$ 1.000,00` com espaço não separável (U+00A0). Nos testes do formatador, montar o esperado com a **mesma** cultura (`ToString("C", pt-BR)`), em vez de digitar o texto literal.
5. **Significado dos campos antigos mudou** (inclui `Pendente`). A tela deve dizer isso de forma curta, no detalhe e em uma nota abaixo da tabela, para o usuário não estranhar a diferença em relação ao que via antes.
6. **Tabela com `table-striped` e linhas de detalhe** quebram a alternância de cores, igual à lista de Movimentos. Aceito por consistência com o padrão existente.
7. **Sem lógica de cálculo no Web.** O Web nunca recalcula situação, meses ou data; só formata o que a Api entregou.
8. **Sem dependência nova.** Bootstrap, ícones (`bi-*`) e `System.Text.Json` já estão no projeto.
9. **Verificar `_ViewImports.cshtml`**: se `Presentation.Objetivos` não estiver importado nas páginas, usar `@using Presentation.Objetivos` na `Index.cshtml`.

## 5. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 4.1 | Enum e DTOs (`SituacaoProjecao`, `CenarioProjecaoDto`, `ProjecaoObjetivoDto`, `ResumoObjetivoDto`) | tipos e testes de contrato JSON |
| 4.2 | `ProjecaoFormatter` | formatador e testes |
| 4.3 | `Index.cshtml`: coluna, detalhe, totais, nota | view (teste manual) |
| 4.4 | Testes de página e de totais | `IndexModelTests`, `TotaisObjetivosTests` |
| 5 | Ponta a ponta e fechamento | roteiro, documentação, ADRs e specs |

---

## Passo 4: Presentation (Web)

### 4.1 Enum e DTOs (`Presentation/Objetivos/`)

**Enum `SituacaoProjecao`** (arquivo próprio): `Prevista`, `Atingida`, `MaisDeCemAnos`, `SemPrevisao`. Comentário curto: cópia do contrato da Api; os nomes trafegam como texto no JSON e não podem mudar sem alterar Api e Web juntos.

**`CenarioProjecaoDto`** (record posicional `sealed`):

- `Situacao` (`SituacaoProjecao`, com `[JsonConverter(typeof(JsonStringEnumConverter<SituacaoProjecao>))]` na propriedade);
- `Meses` (`int?`);
- `DataPrevista` (`DateTime?`).

**`ProjecaoObjetivoDto`** (record posicional `sealed`):

- `SaldoPendente` (`decimal`);
- `AporteRealizadoMensal` (`decimal?`);
- `MesesObservados` (`int`);
- `AportePlanejadoMensal` (`decimal`);
- `TaxaAnualEstimada` e `TaxaMensalEstimada` (`double`);
- `BaixaConfianca` (`bool`);
- `Conservador`, `Realista` e `Planejado` (`CenarioProjecaoDto`).

**`ResumoObjetivoDto`:** acrescentar, **depois** de `TotalLiquido`, o parâmetro `ProjecaoObjetivoDto? Projecao = null`. Comentário curto no tipo: `QtdeMovimentos`, `TotalAportado`, `TotalLiquido` e `PercentualMeta` incluem Movimentos encerrados como `Pendente` (ADR 0021).

> Atenção ao atributo em record posicional: usar `[property: JsonConverter(typeof(JsonStringEnumConverter<SituacaoProjecao>))]` no parâmetro, para que o atributo chegue à propriedade. Conferir o mesmo padrão usado no `MovimentoDto` (spec 0019, parte 4).

### 4.2 `ProjecaoFormatter` (`Presentation/Objetivos/`, estático e puro)

Todos os métodos recebem dados prontos, sem relógio e sem acesso a nada externo.

- **`FormatarCenario(CenarioProjecaoDto? cenario)`**:
  - `Prevista` com `Meses` e `DataPrevista`: `"18 meses (mar/2028)"`; um mês: `"1 mês (nov/2026)"`;
  - `Prevista` sem `Meses` ou sem `DataPrevista` (dado inconsistente): `"Sem previsão"`, sem exceção;
  - `Atingida`: `"Meta atingida"`;
  - `MaisDeCemAnos`: `"Mais de 100 anos"`;
  - `SemPrevisao` ou `null`: `"Sem previsão"`.
- **`FormatarMesAno(DateTime data)`**: `"mar/2028"`, pela tabela fixa de meses (D33).
- **`FormatarTaxa(double taxa)`**: percentual com duas casas e vírgula, sempre em `pt-BR`: `0,1` vira `"10,00%"`. Valor não finito (`NaN`, infinito) vira `"—"`, sem exceção (defesa; o Domain já garante finitos).
- **`FormatarAporteRealizado(decimal? aporte, int mesesObservados)`**:
  - nulo: `"Sem histórico de aportes"`;
  - com valor: `"R$ 500,00 por mês (média de 8 meses)"`; um mês: `"(média de 1 mês)"`.
- **`TextoBaixaConfianca`** (constante): `"Poucos dados: a rentabilidade foi considerada zero ou limitada. Trate a previsão como estimativa grosseira."` (ajustar o texto final com o mantenedor).

### 4.3 `Pages/Index.cshtml`

**Tabela de Objetivos**

- Nova **primeira coluna** estreita com o botão de expandir (D35), seguida das atuais; nova **última coluna** `Previsão`.
- Célula de Previsão (D34, D36): `ProjecaoFormatter.FormatarCenario(objetivo.Projecao?.Realista)` em `fw-semibold`; ícone `bi-exclamation-triangle` com `title` do `TextoBaixaConfianca` quando `BaixaConfianca`; `"—"` quando `Projecao` for nula.
- Botão de expandir **só** quando `Projecao` não for nula.

**Linha de detalhe** (logo abaixo de cada Objetivo com projeção, `colspan` igual ao total de colunas, no padrão `p-0` + `collapse` de Movimentos). Lista `dl` com:

- os **três cenários**, cada um com o rótulo e o texto do formatador:
  - **Conservador**: "mantendo o aporte realizado, sem rendimento";
  - **Realista**: "mantendo o aporte realizado, com o rendimento histórico";
  - **Planejado**: "com o aporte planejado, com o rendimento histórico";
- **Saldo considerado** (`TotalLiquido`), com a parcela pendente quando maior que zero: `"inclui R$ X de movimentos encerrados como Pendente"`;
- **Aporte realizado** (`FormatarAporteRealizado`) e **aporte planejado** (moeda);
- **Rentabilidade estimada** (`FormatarTaxa` anual e mensal);
- **Aviso de baixa confiança** (`alert alert-warning` pequeno) somente quando `BaixaConfianca`.

**Linha de totais**

- Ajustar `colspan` à coluna nova (o comentário Razor que já avisa "a ordem desta linha acompanha as colunas" continua valendo): `Total` ocupa a coluna do botão e a do nome; Meta e Percentual seguem juntos; Qtde, Aportado e Líquido como hoje; **célula final vazia** para a Previsão (D37).

**Nota abaixo da tabela** (`small text-muted`): "Total Líquido e Percentual Meta incluem movimentos encerrados como Pendente. Previsões são estimativas baseadas no seu histórico, não promessas de rentabilidade."

### 4.4 Página e totais

- `IndexModel` **sem alteração** de código: continua carregando a lista e calculando `Totais`.
- `TotaisObjetivos.FromResumo` **sem alteração**; só ganha teste garantindo que a projeção não entra na soma.

---

## 6. Testes (`Presentation.Test`)

Estilo dos testes atuais (xUnit, Moq).

### `ProjecaoFormatterTests` (novo, `Presentation.Test/Objetivos/`)

- `FormatarCenario`: `Prevista` 18 meses em mar/2028; 1 mês no singular; `Atingida`; `MaisDeCemAnos`; `SemPrevisao`; `null`; `Prevista` sem `Meses`; `Prevista` sem `DataPrevista`.
- `FormatarMesAno`: os 12 meses (`Theory`); independente da cultura corrente da thread (rodar com `CultureInfo` diferente, restaurando depois).
- `FormatarTaxa`: `0` → `"0,00%"`; `0.3` → `"30,00%"`; `0.12345` → `"12,35%"`; `NaN` e infinito → `"—"`.
- `FormatarAporteRealizado`: nulo; valor com 8 meses; valor com 1 mês (esperado montado com a cultura `pt-BR`, ver atenção 4).

### `ResumoObjetivoDtoContratoJsonTests` (novo, `Presentation.Test/Objetivos/`)

`JsonSerializerOptions(JsonSerializerDefaults.Web)`, a mesma convenção da Api:

- Desserializa o **JSON de exemplo da parte 3** (cenário determinístico) e confere todos os campos, inclusive `Planejado` = `Prevista`, 19 meses, `2028-05-01`, e os dois cenários `SemPrevisao` com `Meses` e `DataPrevista` nulos.
- Cada um dos quatro valores de `situacao` desserializa para o membro certo.
- JSON **sem** o objeto `projecao` (Api antiga) desserializa com `Projecao` nula, sem exceção.
- `aporteRealizadoMensal: null` chega nulo.
- Os **nomes** do enum `SituacaoProjecao` são exatamente `Prevista`, `Atingida`, `MaisDeCemAnos` e `SemPrevisao` (guarda do contrato com a Api).
- Ida e volta (serializar e desserializar) preserva os valores, e a situação serializa como **texto**.

### `TotaisObjetivosTests` e `IndexModelTests` (acrescentar)

- `TotaisObjetivos.FromResumo` com Objetivos que tenham `Projecao` preenchida: mesmos totais de antes (a projeção não entra).
- `IndexModel.OnGetAsync` mantém a mesma lista (`Assert.Same`) quando os itens trazem `Projecao`.
- Os testes atuais continuam verdes **sem alteração** (D30).

### Teste manual da view (sem teste automatizado de Razor no repositório)

Ver roteiro do Passo 5. Não introduzir biblioteca de teste de view nesta spec (dependência nova exigiria aprovação).

---

## Passo 5: Ponta a ponta e fechamento

### 5.1 Roteiro manual

Pré-requisito: spec 0019 e partes 1 a 3 implantadas. Ambiente com Docker (ADR 0001): `docker compose up` com Api, Web e MariaDB, e o usuário A logado.

1. **Sem aportes:** Objetivo com Meta 12.000 e `AporteMensal` 600, nenhum Movimento. Tela: Previsão "Sem previsão" no Realista; detalhe mostra Planejado "Sem previsão" (sem saldo, conforme regra do Domain) ou o valor calculado, de acordo com a tabela da parte 1; "Sem histórico de aportes".
2. **Cenário determinístico da parte 2** (Movimento ativo de 1.000 em 01/10/2026): Planejado "19 meses (mai/2028)"; Conservador e Realista "Sem previsão"; aviso de baixa confiança visível.
3. **Histórico real** (aportes mensais nos últimos meses): Conservador e Realista passam a ter meses; Realista na coluna, em negrito; Conservador ≥ Realista em meses.
4. **Meta atingida:** Movimentos que somem Meta ou mais: "Meta atingida" nos três cenários.
5. **Mais de 100 anos:** Meta alta com aporte realizado muito baixo: "Mais de 100 anos".
6. **Pendente vs Gasto vs Reinvestido** (precisa da spec 0019): encerrar um Movimento de 1.000 como `Gasto` e conferir que `Total Líquido`, `% Meta` e a previsão **não** mudam em relação a antes do encerramento do saldo; encerrar outro como `Reinvestido`: idem; reclassificar um como `Pendente`: `Total Líquido` sobe e o detalhe mostra "inclui R$ 1.000,00 de movimentos encerrados como Pendente".
7. **Reativar** um Movimento encerrado: saldo e previsão voltam ao normal.
8. **Linha de totais:** `Qtde`, `Aportado` e `Líquido` batem com a soma da tabela; sem previsão na linha.
9. **Api antiga ou sem projeção** (simular removendo `projecao` de uma resposta em ambiente de teste): célula "—", sem botão de detalhe e sem erro.
10. **Isolamento multiusuário:** usuário B não vê Objetivos nem números de A.
11. **Acessibilidade e responsivo:** navegação por teclado no botão de expandir (`aria-expanded` alterna); tabela legível em tela estreita.
12. **Logs:** NLog sem erro de desserialização; `/health` verde.

### 5.2 Conferências no banco (somente leitura)

- Somatório do saldo de um Objetivo por consulta SQL (`EstaAtivo = 1` ou `MotivoEncerramento = 'Pendente'`) igual ao `Total Líquido` da tela.
- Nenhuma mudança de schema nesta spec (o schema da spec 0019 já está aplicado).

### 5.3 Documentação

- `README` do Presentation: seção curta "Previsão de Objetivos" (o que a coluna mostra, os três cenários e o aviso de baixa confiança).
- `README` do Service/Api: o contrato de `GET api/objetivos/resumo` com o objeto `projecao` (exemplo da parte 3) e a nota de que `totalLiquido` e afins incluem `Pendente`.
- `README` do Domain: `ProjecaoObjetivoCalculador` e as constantes (12 meses, 90 dias, teto de 30% a.a., 1.200 meses).

### 5.4 Fechamento de ADRs e specs

- ADR **0021**: Status de "Proposta" para **Aceita** (data da conclusão).
- ADR **0020**: Status para **Aceita**, se ainda não estiver (a ressalva sobre o `Pendente` está resolvida pelo 0021).
- ADR **0014**: acrescentar a linha de status "Complementada pelo ADR 0021" (o conteúdo permanece; só muda o status).
- Specs **0019** e **0020**: Status para **implementada**, cada uma ao concluir suas quatro partes.
- Atualizar o índice de ADRs e de specs, se existir.

### 5.5 Checklist final

- [ ] `dotnet build` e `dotnet test` da solution inteira verdes.
- [ ] Testes de arquitetura e de `CancellationToken` verdes.
- [ ] Contrato JSON da parte 3 e da parte 4 consistentes (mesmo exemplo desserializa no Web).
- [ ] Nomes do enum idênticos em Domain, Api, Web e coluna do banco (testes que os fixam passando).
- [ ] Nenhuma dependência nova, nenhum segredo em texto puro, nenhuma exceção engolida.
- [ ] Roteiro manual do 5.1 executado.
- [ ] ADRs e specs com status atualizado; READMEs revisados.
- [ ] Publicação **conjunta** de Api e Web.

## 7. Riscos

| Risco | Mitigação |
| --- | --- |
| Api e Web publicados em momentos diferentes | Publicação conjunta (checklist); o Web tolera ausência de `projecao` (D30), mas não valor de enum novo |
| Cópia do enum no Web divergir da Api | Testes que fixam os nomes nos dois lados |
| Esquecer o `Pendente` e o usuário estranhar a mudança no Total Líquido | Nota abaixo da tabela e linha no detalhe (4.3) |
| Previsão lida como promessa | Texto da nota, aviso de baixa confiança e rótulos de premissa |
| Moeda com espaço não separável quebrando testes | Esperados montados com a mesma cultura (atenção 4) |
| Linha de totais desalinhada após a coluna nova | Comentário Razor existente e item do roteiro 5.1 (passo 8) |

## Fora de escopo

- Gráfico da evolução do saldo ou da projeção.
- Simulador (o usuário informar outro aporte ou outra taxa).
- Previsão agregada na linha de totais.
- Testes automatizados de Razor (exigiriam dependência nova, a discutir à parte).
- Qualquer alteração de schema, Api ou LumiaFoundation.
- Notificações ou alertas quando o cenário piorar.
