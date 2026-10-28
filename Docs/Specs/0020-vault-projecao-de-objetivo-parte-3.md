# Especificação técnica 0020: Projeção do Objetivo (Vault), parte 3

- Data: 2026-10-27
- Status: **em elaboração (parte 3 de 4: Passo 3, Service/Api)**
- ADR relacionado: [0021](../adr/0021-projecao-de-objetivo.md)
- Partes anteriores: [parte 1](0020-vault-projecao-de-objetivo.md) (Passos 0 e 1), [parte 2](0020-vault-projecao-de-objetivo-parte-2.md) (Passo 2)
- Repositório afetado nesta parte: **Service (Api)**. Sem schema, sem migration, sem dependência nova, **sem endpoint novo** e sem alteração no LumiaFoundation.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Ajuste na parte 2

O `ResumoObjetivoModel` do Application tem `Projecao` **nunca nula**, com padrão `ProjecaoObjetivoModel.Vazia` (todos os cenários em `SemPrevisao`). Motivo: o teste atual do controller (`GetObjetivosComValores_ReturnsMappedDtosForCurrentUser`) cria `ResumoObjetivoModel` por inicializador **sem** `Projecao`; sem esse padrão, o mapeamento para o DTO lançaria `NullReferenceException`. Já aplicado no arquivo da parte 2.

## 2. Resumo

A Api expõe a projeção como um objeto novo, `projecao`, dentro de cada item de `GET api/objetivos/resumo`. Nenhum campo existente muda de **nome ou tipo**; `qtdeMovimentos`, `totalAportado`, `totalLiquido` e `percentualMeta` passam a refletir o saldo único (ativos e `Pendente`), efeito da parte 2. Não há ação, rota ou controller novos: só os DTOs e seus testes.

## 3. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D21 | `ResumoObjetivoDto` ganha `Projecao` (`ProjecaoObjetivoDto`), **sempre presente** (nunca nulo) em cada item |
| D22 | `ProjecaoObjetivoDto` traz: `SaldoPendente`, `AporteRealizadoMensal` (anulável), `MesesObservados`, `AportePlanejadoMensal`, `TaxaAnualEstimada`, `TaxaMensalEstimada`, `BaixaConfianca` e os três cenários `Conservador`, `Realista` e `Planejado` |
| D23 | `CenarioProjecaoDto` traz `Situacao`, `Meses` (anulável) e `DataPrevista` (anulável) |
| D24 | `Situacao` trafega como **texto** (`Prevista`, `Atingida`, `MaisDeCemAnos`, `SemPrevisao`), por atributo `JsonStringEnumConverter<SituacaoProjecao>` na propriedade do DTO, como no ADR 0020; sem conversor global |
| D25 | Dinheiro em `decimal`; as taxas em `double` (número JSON), pois são razões, não valores monetários, e já são `double` no Domain |
| D26 | `DataPrevista` é o primeiro dia do mês previsto, como data **sem fuso** (mesmo tratamento de `DataVencimento` nos DTOs de Movimento); a tela mostra só mês e ano |
| D27 | Nomes em `PascalCase` nas propriedades (a Api serializa em `camelCase` pelo padrão do ASP.NET); os nomes dos valores do enum são **contrato** com o Web |
| D28 | Nenhuma mudança no `ObjetivosController`: a ação atual devolve o DTO; o contrato novo é guardado por testes de DTO e de JSON |

## 4. Atenção antes de começar

1. **Campos atuais intocados em nome e tipo.** Quem lê o JSON hoje (o Web) continua funcionando; o objeto `projecao` é aditivo.
2. **O significado de `totalLiquido`, `totalAportado`, `qtdeMovimentos` e `percentualMeta` mudou** (inclui `Pendente`, ADR 0021). Registrar isso num comentário curto no `ResumoObjetivoDto`, para quem ler o DTO entender.
3. **`double` e JSON:** o `System.Text.Json` **falha** ao serializar `NaN` ou infinito. O Domain garante valores finitos (parte 1), e o teste de contrato JSON desta parte serializa um resumo completo para pegar qualquer regressão cedo.
4. **Sem lógica no DTO.** O DTO só copia campos; nada de recalcular situação, data ou arredondar aqui.
5. **Multi-tenant:** nada muda; o `userId` continua vindo do token em `GetCurrentUserId()`.
6. **`CancellationToken`:** a ação existente já recebe o token (ADR 0017); não há ação nova.
7. **Publicação:** Api e Web precisam sair **juntos** (parte 4 cuida da leitura do objeto novo e da coluna "Previsão"). Entre as partes 3 e 4 a Api já devolve `projecao`, e o Web ainda a ignora, o que é inofensivo.
8. **Sem dependência nova.** `JsonStringEnumConverter<T>` faz parte do `System.Text.Json`.

## 5. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 3.1 | `CenarioProjecaoDto` e `ProjecaoObjetivoDto` | DTOs e testes |
| 3.2 | `ResumoObjetivoDto` com `Projecao` | DTO e testes |
| 3.3 | Teste do controller e contrato JSON | testes |

---

## Passo 3: Service (Api)

Pasta `Service/Objetivos/DTOs/`, ao lado do `ResumoObjetivoDto` e no mesmo padrão: classe com propriedades `{ get; set; }` e `FromApplication` estático.

### 3.1 DTOs da projeção

**`CenarioProjecaoDto`:**

- `SituacaoProjecao Situacao` com o atributo `[JsonConverter(typeof(JsonStringEnumConverter<SituacaoProjecao>))]` (D24).
- `int? Meses` e `DateTime? DataPrevista`.
- `FromApplication(CenarioProjecaoModel)`: copia os três campos, sem lógica.

**`ProjecaoObjetivoDto`:**

- `decimal SaldoPendente`;
- `decimal? AporteRealizadoMensal` e `int MesesObservados`;
- `decimal AportePlanejadoMensal`;
- `double TaxaAnualEstimada` e `double TaxaMensalEstimada`;
- `bool BaixaConfianca`;
- `CenarioProjecaoDto Conservador`, `Realista` e `Planejado`.
- `FromApplication(ProjecaoObjetivoModel)`: copia os campos e mapeia os três cenários.

### 3.2 `ResumoObjetivoDto`

- Nova propriedade `ProjecaoObjetivoDto Projecao`, com valor inicial seguro (um `ProjecaoObjetivoDto` mapeado de `ProjecaoObjetivoModel.Vazia`) para o DTO nunca ter `Projecao` nula, mesmo criado por inicializador.
- `FromApplication(ResumoObjetivoModel)` passa a copiar também `Projecao`, mantendo os campos atuais exatamente como estão.
- A sobrecarga para lista não muda.
- Comentário curto acima dos quatro campos de totais registrando que, desde o ADR 0021, eles incluem Movimentos encerrados como `Pendente`.

### 3.3 Controller

Nenhuma alteração. A ação `GetObjetivosComValores` já devolve `ResumoObjetivoDto.FromApplication(objetivos)`. O `ProducesResponseType` não muda.

---

## 6. Testes (`Service.Test`)

Estilo dos testes atuais.

### `ResumoObjetivoDtoTests` (novo, `Service.Test/Objetivos/`)

- `FromApplication` mantém **todos** os campos atuais (`Id`, `Nome`, `Meta`, `PercentualMeta`, `QtdeMovimentos`, `TotalAportado`, `TotalLiquido`).
- Mapeia `Projecao` por completo, com um modelo que tenha: `SaldoPendente`, aporte realizado, meses observados, aporte planejado, as duas taxas e `BaixaConfianca`.
- Cada cenário:
  - **`Prevista`**: `Meses` e `DataPrevista` preenchidos;
  - **`Atingida`**, **`MaisDeCemAnos`** e **`SemPrevisao`**: `Meses` e `DataPrevista` nulos.
- `AporteRealizadoMensal` nulo chega nulo ao DTO.
- **Modelo sem projeção** (`new ResumoObjetivoModel { ... }` sem `Projecao`): o DTO é criado **sem exceção**, com os três cenários em `SemPrevisao`.
- Sobrecarga para lista: mantém a ordem e a quantidade.

### `ProjecaoObjetivoDtoTests` / `CenarioProjecaoDtoTests`

- Cada campo do modelo chega ao DTO sem alteração, sem arredondar nem recalcular (`double` idêntico, `decimal` idêntico).

### `ObjetivosControllerTests` (acrescentar)

- `GetObjetivosComValores` devolve o DTO com `Projecao` mapeada do modelo (um caso com `Planejado` em `Prevista`).
- O teste atual (modelo **sem** `Projecao`) continua verde **sem alteração**.
- O token recebido chega à query (já coberto; manter).

### `ResumoObjetivoContratoJsonTests` (novo)

Usa `JsonSerializer` com `JsonSerializerOptions(JsonSerializerDefaults.Web)`, a mesma convenção de `camelCase` da Api. Cobre o contrato sem subir servidor:

- **Campos atuais** aparecem com os nomes de sempre: `id`, `nome`, `meta`, `percentualMeta`, `qtdeMovimentos`, `totalAportado`, `totalLiquido`.
- **Objeto novo:** `projecao` com `saldoPendente`, `aporteRealizadoMensal`, `mesesObservados`, `aportePlanejadoMensal`, `taxaAnualEstimada`, `taxaMensalEstimada`, `baixaConfianca`, `conservador`, `realista` e `planejado`.
- **Enum como texto:** `"situacao":"Prevista"` (e os demais valores), **nunca** número.
- **Anuláveis:** `aporteRealizadoMensal`, `meses` e `dataPrevista` aparecem como `null` quando indisponíveis.
- **`dataPrevista`** serializa como `2028-05-01T00:00:00` (sem fuso) para um primeiro dia de mês.
- **Nomes do enum** `SituacaoProjecao` são exatamente `Prevista`, `Atingida`, `MaisDeCemAnos` e `SemPrevisao` (guarda do contrato com o Web, como o teste dos nomes de `MotivoEncerramento`).
- **Resumo completo serializa sem exceção**, inclusive com taxas em zero e no teto (`0,30`), protegendo contra `NaN` e infinito.
- Desserialização de volta para o DTO (ida e volta) preserva os valores, inclusive o enum.

---

## 7. Como validar

- `dotnet build` da solution.
- `dotnet test Service.Test --filter "FullyQualifiedName~Objetivos"`.
- `dotnet test Service.Test` completo (os testes de arquitetura do `CancellationToken` e de registro continuam verdes; nenhuma ação ou serviço novo).
- Com a Api no ar e um token válido: `GET /api/objetivos/resumo`. Com o cenário determinístico da parte 2 (`Meta` 12.000, `AporteMensal` 600, um Movimento ativo de 1.000 investido em 01/10/2026 e líquido 1.000, com "hoje" em 27/10/2026), o item deve ficar assim (abreviado):

```json
{
  "nome": "Reserva",
  "meta": 12000.00,
  "percentualMeta": 8.3333333333333333333333333333,
  "qtdeMovimentos": 1,
  "totalAportado": 1000.00,
  "totalLiquido": 1000.00,
  "projecao": {
    "saldoPendente": 0.00,
    "aporteRealizadoMensal": null,
    "mesesObservados": 0,
    "aportePlanejadoMensal": 600.00,
    "taxaAnualEstimada": 0,
    "taxaMensalEstimada": 0,
    "baixaConfianca": true,
    "conservador": { "situacao": "SemPrevisao", "meses": null, "dataPrevista": null },
    "realista":    { "situacao": "SemPrevisao", "meses": null, "dataPrevista": null },
    "planejado":   { "situacao": "Prevista", "meses": 19, "dataPrevista": "2028-05-01T00:00:00" }
  }
}
```

- Conferir o documento OpenAPI: `situacao` aparece como texto, com os 4 valores. Se aparecer como número, o atributo do D24 não foi aplicado.
- Com um Movimento encerrado como `Pendente` no mesmo Objetivo, conferir que `totalLiquido` sobe e `projecao.saldoPendente` mostra a parcela. Com `Gasto`, nada muda no saldo.
- Usuário B chamando o endpoint não vê Objetivos de A (comportamento atual preservado).

**Critério de pronto:** build e testes verdes; JSON com os campos atuais intactos e o objeto `projecao` completo; enum como texto na Api e no OpenAPI; nenhuma ação, rota ou dependência nova; nenhum `NaN` ou infinito possível na serialização.

---

## 8. Próxima parte

- **Parte 4, Presentation (Web) e fechamento:**
  - DTOs do Web com a própria cópia do enum `SituacaoProjecao` (mesmos nomes, `JsonStringEnumConverter` na propriedade), `ResumoObjetivoDto` do Web com `Projecao`;
  - coluna **Previsão** na tabela de Objetivos da tela inicial (cenário Realista como destaque) e detalhe expansível com os três cenários e as premissas: saldo com a parcela pendente, aporte realizado e planejado, rentabilidade estimada e aviso de baixa confiança;
  - formatador estático e testado para os textos ("18 meses (mar/2028)", "Meta atingida", "Sem previsão", "Mais de 100 anos");
  - `TotaisObjetivos` sem previsão (a linha de totais não muda);
  - roteiro ponta a ponta, documentação, ADR 0021 **Aceita** e ADR 0014 como complementado.

## Fora de escopo

- Endpoint próprio de projeção por Objetivo.
- Parâmetros de entrada para o usuário simular outros aportes ou taxas.
- Qualquer alteração de schema ou de LumiaFoundation.
