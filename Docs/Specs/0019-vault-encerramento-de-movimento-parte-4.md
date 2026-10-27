# Especificação técnica 0019: Encerramento de Movimento (Vault), parte 4

- Data: 2026-10-27
- Status: **implementada (2026-10-27; parte 4 de 4: Passos 5 e 6, Web e fechamento)**
- ADR relacionado: [0020](../adr/0020-encerramento-de-movimento.md)
- Partes anteriores: [parte 1](0019-vault-encerramento-de-movimento.md) (Passos 0 a 2), [parte 2](0019-vault-encerramento-de-movimento-parte-2.md) (Passo 3) e [parte 3](0019-vault-encerramento-de-movimento-parte-3.md) (Passo 4)
- Repositório afetado nesta parte: **Presentation (Web)** e documentação. Sem schema, sem migration, sem dependência nova, sem alteração no LumiaFoundation.

> Especificação para outro dev implementar. Não contém código de implementação: descreve o que fazer em cada classe, a ordem, os testes e a documentação.
> Todo código, comentário, commit e documentação em português.

---

## 1. Resumo

O Web passa a falar o contrato novo da Api: o checkbox "Ativo" sai da edição e da criação, entra a página **`/Movimentos/Encerrar/{id}`** (encerrar, reclassificar e reativar), a listagem mostra o encerramento e o formulário de criação ganha uma seção **opcional** de encerramento, para cadastrar histórico já encerrado. Esta parte também fecha a feature: validação ponta a ponta, documentação e ADR 0020 **Aceita**.

## 2. Decisões desta parte

| Item | Decisão |
| --- | --- |
| D28 | **Aprovado:** o formulário de **criação** ganha uma seção opcional "Encerramento" (ver seção 3) |
| D29 | O Web tem a **sua própria cópia** do enum (`Presentation.Movimentos.MotivoEncerramento`), com os mesmos nomes, porque o Web não referencia Domain nem Application (acoplamento só por contrato). O atributo `JsonStringEnumConverter<T>` entra nas propriedades de DTO, como na Api (D18) |
| D30 | Página `/Movimentos/Encerrar/{id}`: uma só página para **encerrar**, **reclassificar** e **reativar** (como no ADR 0020); preserva página e filtro da listagem, como a edição |
| D31 | Sem motivo pré-selecionado ao encerrar: o usuário escolhe de propósito; a data vem preenchida com **hoje (UTC)**, pelo `IDateTimeProvider`, como em `Cotacoes/Create` |
| D32 | Erro da Api: **422** mostra a **mensagem da Api** (é validação de negócio, em português); qualquer outro erro mostra a mensagem genérica do padrão atual |
| D33 | Listagem: ação **Encerrar** (ativo) ou **Reclassificar** (encerrado) na linha; o detalhe expansível mostra motivo e data de encerramento; o filtro "Status" não muda |
| D34 | Descrição amigável dos motivos num formatador estático e testado (`MotivoEncerramentoFormatter`), no padrão do `FluxoMensalFormatter` |
| D35 | `CancellationToken` **obrigatório** em todo método novo do cliente da Api e das páginas |

## 3. Decisão aprovada

### D28: criação com encerramento opcional

Hoje o formulário de criação tem o checkbox "Ativo" (padrão marcado), usado também para lançar histórico de investimentos que já terminaram. Sem ele, esse caminho some.

| Opção | Prós | Contras |
| --- | --- | --- |
| **Seção opcional "Encerramento" na criação** (recomendada) | mantém o lançamento de histórico em **um** passo; usa o `POST` que o ADR 0020 já prevê para isso | formulário um pouco maior; mais uma validação no Web (motivo e data juntos) |
| Sem encerramento na criação | formulário mais simples | histórico passa a exigir **dois** passos (criar e depois encerrar), o que pesa quando o usuário cadastra vários antigos de uma vez |

A opção de seção opcional foi aprovada e implementada conforme descrito em 5.5.

---

## 4. Atenção antes de começar

1. **Não fazer merge entre as partes 2 e 4**, nem publicar Service e Web em versões diferentes: depois desta parte, o Web deixa de enviar `estaAtivo`, que a Api (parte 3) também deixou de aceitar.
2. **Nomes do enum são contrato** entre Web e Api e com o banco. O Web não referencia o Domain; a cópia precisa ter exatamente `Gasto`, `Reinvestido` e `Pendente`. Um teste fixa os nomes.
3. **Sem lógica de negócio no Web.** As validações do Web são só conveniência de formulário (campo obrigatório, data não futura); a Api continua sendo a autoridade e o Web exibe a mensagem dela em caso de 422.
4. **Multi-tenant:** o Web nunca trata o `UserId`; a Api o aplica pelo token. Abrir `/Movimentos/Encerrar/{id}` de outro usuário resulta em 404 da Api, tratado como na edição.
5. **Padrão de página do repositório:** `PageModel` com construtor primário, `[BindProperty(SupportsGet = true)]` para `Id`, `Pagina` e `Filtro`, `ErrorMessage` com `private set`, redirecionamento para `Index` com `Filtro.ToRouteValues(Pagina)`.
6. **`DateTime.Today` não** nas páginas novas: o relógio vem de `IDateTimeProvider` (UTC), para a data padrão e a regra "não futura" baterem com a da Api.
7. **Textos em português**, mesma grafia das mensagens atuais.

## 5. Ordem de implementação

Cada subpasso termina com `dotnet build` e `dotnet test` verdes.

| # | Subpasso | Entrega |
| --- | --- | --- |
| 5.1 | Contratos e cliente da Api | enum, DTOs, `IMovimentoApi`/`MovimentoApi`, testes |
| 5.2 | Formatador e validador de entrada | `MotivoEncerramentoFormatter`, `EncerramentoInput`, `EncerramentoInputValidador`, testes |
| 5.3 | Página de encerramento | `Encerrar.cshtml` e `.cs`, testes |
| 5.4 | Edição sem `EstaAtivo` | `Edit`, `InputModel`, testes |
| 5.5 | Criação com encerramento opcional | `Create`, testes |
| 5.6 | Listagem | `Index.cshtml` |
| 6 | Validação ponta a ponta, documentação e fechamento | roteiro, docs, ADR |

---

## Passo 5: Presentation (Web)

### 5.1 Contratos e cliente da Api (`Presentation/Movimentos/`)

**Enum** `MotivoEncerramento` (arquivo próprio): `Gasto`, `Reinvestido`, `Pendente`, com comentário curto: cópia do contrato da Api, e os nomes são gravados no banco.

**DTOs:**

- `MovimentoDto`: acrescentar `MotivoEncerramento? MotivoEncerramento` (com `[JsonConverter(typeof(JsonStringEnumConverter<MotivoEncerramento>))]`) e `DateTime? DataEncerramento`. `EstaAtivo` continua.
- `MovimentoForCreationDto`: **remover** `EstaAtivo`; **acrescentar** `MotivoEncerramento?` (com o atributo) e `DateTime? DataEncerramento`.
- `MovimentoForUpdateDto`: **remover** `EstaAtivo`.
- **Novo** `MovimentoForEncerramentoDto` (record, `init`): `MotivoEncerramento Motivo` (com o atributo) e `DateTime Data`. Não anuláveis: o Web só monta este DTO depois de validar o formulário.

**Cliente** (`IMovimentoApi` e `MovimentoApi`):

- `Task EncerrarAsync(Guid id, MovimentoForEncerramentoDto encerramento, CancellationToken cancellationToken)`: `PUT api/movimentos/{id}/encerramento` com o DTO no corpo.
- `Task ReativarAsync(Guid id, CancellationToken cancellationToken)`: `DELETE api/movimentos/{id}/encerramento`, sem corpo.
- Mesmo padrão dos métodos atuais (`connection.SendAsync(...)`, sem lógica adicional).

**Testes** (`Presentation.Test/Movimentos/`):

- `MovimentoApiTests`: `EncerrarAsync` envia `PUT` na URL e com o corpo esperados e propaga o token; `ReativarAsync` envia `DELETE` na URL esperada, sem corpo, com o token.
- `MovimentoContratoJsonTests` (novo), com `JsonSerializerDefaults.Web` (o mesmo que o `ApiConnection` usa):
  - `MovimentoForEncerramentoDto` serializa `"motivo":"Gasto"` (texto, não número) e `data`.
  - `MovimentoDto` desserializa `"motivoEncerramento":"Pendente"` e `"dataEncerramento"`; ausente ou `null` vira `null`.
  - `MovimentoForCreationDto` serializa o motivo como texto e **não contém** `estaAtivo` no JSON.
  - **Os nomes do enum são exatamente `Gasto`, `Reinvestido` e `Pendente`** (guarda do contrato com a Api).
  - `MovimentoForCreationDto` e `MovimentoForUpdateDto` não têm propriedade `EstaAtivo` (reflexão).

### 5.2 Formatador e validação de entrada

**`MotivoEncerramentoFormatter`** (`Presentation/Movimentos/`, estático e puro):

- `Descricao(MotivoEncerramento? motivo)`: `Gasto` → "Gasto", `Reinvestido` → "Reinvestido", `Pendente` → "Pendente (sem destino definido)"; nulo → texto vazio.
- Teste: cada valor, nulo, e que **todo valor do enum tem descrição** (acusa um valor novo sem texto).

**`EncerramentoInput`** (`Presentation/Pages/Movimentos/`): classe simples, `MotivoEncerramento? Motivo` e `DateTime? Data`, sem atributos de validação (as regras dependem do contexto, abaixo).

**`EncerramentoInputValidador`** (estático e puro, ao lado do input): `Validar(EncerramentoInput input, DateTime hoje, bool obrigatorio)` devolve a lista de erros `(campo, mensagem)`:

| Situação | Erro |
| --- | --- |
| `obrigatorio` e motivo nulo | em `Motivo`: "Selecione o motivo do encerramento." |
| `obrigatorio` e data nula | em `Data`: "Informe a data do encerramento." |
| `obrigatorio` falso, motivo preenchido e data nula | em `Data`: "Informe a data do encerramento." |
| `obrigatorio` falso, data preenchida e motivo nulo | em `Motivo`: "Selecione o motivo do encerramento." |
| `obrigatorio` falso, os dois nulos | nenhum |
| data posterior a `hoje` (comparando `.Date`) | em `Data`: "A data do encerramento não pode ser futura." |

A comparação com a data do investimento **não** é feita no Web (exigiria carregar o Movimento na criação); a Api responde 422 e o Web mostra a mensagem (D32).

Testes do validador: cada linha da tabela; data igual a `hoje` passa; horas são ignoradas.

### 5.3 Página `/Movimentos/Encerrar/{id}`

Arquivos `Pages/Movimentos/Encerrar.cshtml` e `Encerrar.cshtml.cs`, classe `EncerrarModel(IMovimentoApi movimentoApi, IDateTimeProvider clock)`.

**Estado:**

- `[BindProperty(SupportsGet = true)] Guid Id`, `Pagina` (nome `pagina`) e `Filtro` (`MovimentoFiltroInput`), como na edição.
- `[BindProperty] EncerramentoInput Input`.
- `MovimentoDto Movimento` (`private set`), para o resumo exibido, e `ErrorMessage` (`private set`).
- `bool EstaEncerrado => !Movimento.EstaAtivo`.

**`OnGetAsync`:**

1. Carrega o Movimento (`GetByIdAsync`; erro da Api sobe como na edição).
2. Se estiver **ativo**: `Input.Data = clock.GetDateTime().Date`, `Input.Motivo = null` (D31).
3. Se estiver **encerrado**: `Input` vem preenchido com o motivo e a data atuais (é a tela de reclassificar).

**`OnPostAsync` (encerrar ou reclassificar):**

1. Recarrega o Movimento (a página precisa dele para reexibir o resumo em caso de erro).
2. `EncerramentoInputValidador.Validar(Input, hoje, obrigatorio: true)`: erros viram `ModelState.AddModelError("Input.Motivo" / "Input.Data", mensagem)`; se houver erro, devolve `Page()` **sem chamar a Api**.
3. Chama `EncerrarAsync(Id, new MovimentoForEncerramentoDto { Motivo, Data }, cancellationToken)`.
4. Sucesso: `RedirectToPage("Index", Filtro.ToRouteValues(Pagina))`.
5. `ApiException` com `StatusCode` 422: `ErrorMessage` = **mensagem da Api** (D32). Qualquer outra `ApiException`: "Não foi possível salvar o encerramento agora. Tente novamente em instantes." Em ambos, devolve `Page()`.

**`OnPostReativarAsync`:**

1. Chama `ReativarAsync(Id, cancellationToken)`.
2. Sucesso: redireciona para `Index` como acima.
3. `ApiException`: "Não foi possível reativar o movimento agora. Tente novamente em instantes."; recarrega o Movimento e devolve `Page()`.

**Página (`.cshtml`):**

- Título: "Encerrar movimento" (ativo) ou "Alterar encerramento" (encerrado).
- Resumo somente leitura (`dl`): Objetivo, Tipo de Renda, Corretora, Produto, Emissor, Data do investimento, Valor do aporte e **Valor líquido atual**.
- Aviso (`alert-info`): "O valor líquido atual será mantido como o valor do resgate. Se ele estiver desatualizado, atualize-o antes de encerrar." com link para a edição do próprio Movimento. É a contrapartida da consequência registrada no ADR 0020.
- Formulário: `select` de motivo (opção vazia "Selecione..." e os 3 motivos, rotulados pelo `MotivoEncerramentoFormatter`), campo de data (`type="date"`), `asp-validation-for` nos dois campos e os campos ocultos de `Pagina` e `Filtro`, como na edição.
- Botão principal "Encerrar" (ativo) ou "Salvar encerramento" (encerrado) e "Cancelar" (volta à listagem preservando página e filtro).
- Quando **encerrado**, um segundo botão **"Reativar"** em formulário próprio (`asp-page-handler="Reativar"`) com confirmação (`confirm('Reativar este movimento?')`), como o "Excluir" da listagem. Texto de apoio: "Reativar limpa o motivo e a data do encerramento."

**Testes** (`Presentation.Test/Pages/Movimentos/EncerrarModelTests.cs`, com Moq para a Api e para o relógio):

- `OnGetAsync` de um ativo: data = hoje do relógio, motivo nulo.
- `OnGetAsync` de um encerrado: motivo e data preenchidos com os do Movimento.
- `OnPostAsync` sem motivo, sem data e com data futura: devolve `PageResult`, adiciona o erro no campo certo e **não** chama a Api; o Movimento é recarregado.
- `OnPostAsync` válido: chama `EncerrarAsync` com o `id`, o motivo, a data e **o mesmo token**; redireciona para `Index` com os valores de rota de `Filtro` e `Pagina`.
- `OnPostAsync` com `ApiException` 422: `ErrorMessage` é a mensagem da Api; com outro status: mensagem genérica.
- `OnPostReativarAsync`: sucesso chama `ReativarAsync` com `id` e token e redireciona; falha mostra a mensagem e devolve a página.
- A reclassificação (encerrado com motivo novo) usa o mesmo caminho do encerramento (um teste com `Movimento` já encerrado).

### 5.4 Edição sem `EstaAtivo`

- `CreateModel.InputModel` (usado também pela edição): **remover** `EstaAtivo` e do `ToCreationDto`/`ToUpdateDto` o repasse correspondente.
- `Edit.cshtml.cs`: `MapToInput` deixa de copiar `EstaAtivo`.
- `Edit.cshtml`: **remover** o checkbox "Ativo".
- Nenhum campo de encerramento na edição (D14 da parte 2: a Api não aceita).
- Para o usuário não perder o caminho, a edição **não** ganha botão novo; o acesso ao encerramento é pela listagem (5.6).
- Testes: ajustar `EditModelTests` (retirar `EstaAtivo = true` do Movimento de teste) e, em `OnGetAsync`, nada mais a verificar sobre status.

### 5.5 Criação com encerramento opcional (D28)

- `CreateModel`: nova propriedade `[BindProperty] EncerramentoInput Encerramento`. O `InputModel` compartilhado **não** a recebe (a edição não usa).
- `OnGetAsync`: **remover** `Input.EstaAtivo = true`. `Input.DataInvestimento` continua `DateTime.Today`; o `Encerramento` começa vazio (movimento ativo).
- `OnPostAsync`:
  1. Valida com `EncerramentoInputValidador.Validar(Encerramento, hoje, obrigatorio: false)`. O `hoje` vem do `IDateTimeProvider`, que passa a ser **injetado** no `CreateModel` (construtor primário ganha o relógio).
  2. Erros viram `ModelState` em `Encerramento.Motivo` / `Encerramento.Data`; sem chamar a Api.
  3. `ToCreationDto` do `CreateModel` (ou montagem no handler) preenche `MotivoEncerramento` e `DataEncerramento` do DTO com o que veio do `Encerramento`.
  4. Tratamento de `ApiException` com a mesma regra do D32 (422 mostra a mensagem da Api; outro status, a genérica).
- `Create.cshtml`: **remover** o checkbox "Ativo"; acrescentar, abaixo da Observação, uma seção **"Encerramento (opcional)"** com o `select` de motivo (primeira opção "Em aberto (sem encerramento)", valor vazio), a data e o texto de apoio: "Preencha só para lançar um investimento que já terminou. O valor líquido atual será mantido como o valor do resgate."
- Testes (`CreateModelTests`):
  - Remover o teste que esperava `EstaAtivo = true`; manter o da data de investimento (`DateTime.Today`).
  - Sem encerramento: o DTO enviado tem `MotivoEncerramento` e `DataEncerramento` nulos.
  - Com motivo e data válidos: o DTO enviado traz os dois.
  - Só o motivo, só a data e data futura: `PageResult`, erro no campo certo, **sem** chamar a Api.
  - `ApiException` 422 mostra a mensagem da Api.

### 5.6 Listagem (`Index.cshtml`)

- Coluna **Ações**: ao lado de "Editar", um link para `Encerrar` com os mesmos valores de rota (`id`, `pagina`, filtros) da edição. Texto "Encerrar" quando o Movimento está ativo e "Reclassificar" quando está encerrado.
- **Detalhe expansível:** a linha "Status" mostra "Ativo" ou "Encerrado". Quando encerrado, duas linhas novas: **Motivo do encerramento** (`MotivoEncerramentoFormatter`) e **Data do encerramento** (`dd/MM/yyyy`).
- O estilo `opacity-50` dos inativos e o filtro "Status" (Ativos/Inativos) **não mudam** (ADR 0020).
- `Index.cshtml.cs` não muda. Sem teste de página para o `.cshtml`; cobrem-se o formatador (5.2) e a roteirização manual (Passo 6).

---

## Passo 6: Validação ponta a ponta, documentação e fechamento

### 6.1 Roteiro manual (banco migrado, Api e Web no ar)

Com dois usuários (A e B), para provar também o isolamento:

1. **Criar ativo** (sem encerramento): aparece como "Ativo", sem motivo.
2. **Criar já encerrado** (`Gasto`, data passada): aparece como "Encerrado", com motivo e data no detalhe.
3. **Encerrar** um ativo pela listagem, motivo `Pendente`, data de hoje: some do filtro "Ativos", aparece em "Inativos".
4. **Reclassificar** `Pendente` para `Reinvestido`: motivo e data atualizados no detalhe.
5. **Validações:** data de amanhã (mensagem do Web); data anterior ao investimento (422 da Api, mensagem exibida); motivo vazio; só o motivo na criação.
6. **Reativar:** o Movimento volta a "Ativo", sem motivo e sem data, e **passa a aparecer** em "Manutenção Valor Líquido" (a data do valor líquido ficou antiga).
7. **Edição:** não há mais o checkbox "Ativo"; editar um encerrado mantém o encerramento; mudar a data do investimento para depois da data de encerramento é rejeitado (regra D15 da parte 2).
8. **Isolamento:** o usuário B abrindo `/Movimentos/Encerrar/{id de A}` recebe o mesmo comportamento de um id inexistente na edição (404 da Api); `PUT`/`DELETE` pela Api com o token de B para o Movimento de A: **404**.
9. **Regressão das outras telas** com um Movimento encerrado: ele **não** entra no Retorno previsto, nas Distribuições nem na Manutenção do valor líquido; **entra** no Investido do Fluxo Mensal (exceção deliberada do ADR 0019); os totalizadores da tela inicial seguem somando só ativos.

### 6.2 Conferência no banco

```sql
SELECT EstaAtivo, MotivoEncerramento, COUNT(*) FROM Movimentos GROUP BY EstaAtivo, MotivoEncerramento;
SELECT COUNT(*) FROM Movimentos WHERE (EstaAtivo = 1) <> (MotivoEncerramento IS NULL);   -- 0
```

E, em ambiente descartável, a tentativa de violar o `CHECK` (parte 1, Passo 2.6).

### 6.3 Compose

- `docker compose up --build` com os dois bundles: o container `migrations` aplica a migration nova antes da Api (ADR 0001); conferir no log que `MovimentoEncerramento` foi aplicada e que a Api só sobe depois.

### 6.4 Documentação e fechamento

- **ADR 0020:** status de **Proposta** para **Aceita**, com a data; acrescentar às Consequências: (a) o formulário de criação passa a ter a seção opcional de encerramento (D28); (b) a conversão dos inativos limitou a data de encerramento ao intervalo entre o investimento e hoje; (c) o `CHECK` `CK_Movimentos_Encerramento` protege a invariante; (d) a regra de não permitir `DataInvestimento` posterior à `DataEncerramento` na edição.
- **Specs 0019 (as quatro partes):** `Status` para **implementada**, com a data de fechamento.
- **READMEs e documentos de tela/Api**, se houver menção ao checkbox "Ativo", ao `PUT api/movimentos/{id}` com `EstaAtivo` ou à lista de endpoints: atualizar.
- **ADR 0021** (projeção do Objetivo) **destrava**: registrar, nas Consequências do ADR 0020, que o motivo já está disponível para a próxima feature.
- Commits em português, um por subpasso concluído (sugestão: um por linha das tabelas de ordem de cada parte).

### 6.5 Checklist final

- [x] `dotnet build` e `dotnet test` da solution inteira verdes.
- [ ] Migration aplicada, revertida e reaplicada; consultas de conferência com 0 violações.
- [x] Nenhum código de produção (fora do Domain e do mapeamento) escreve `EstaAtivo` de Movimento.
- [x] Nenhum método novo sem `CancellationToken`.
- [ ] Enum como texto no JSON da Api, no OpenAPI e no Web.
- [ ] Roteiro manual do 6.1 executado, inclusive o isolamento entre usuários.
- [x] ADR 0020 **Aceita** e specs **implementadas**.

### Execução registrada em 2026-10-27

- `dotnet build Vault.slnx` e `dotnet test Vault.slnx` passaram após a conclusão das alterações de Presentation.
- A aplicação, reversão e reaplicação da migration, o Compose e o roteiro autenticado ponta a ponta permanecem pendentes: o MariaDB local não estava acessível em `localhost:3307` durante a tentativa de atualização do banco. Esses passos operacionais não foram considerados validados.
- Não há README de raiz no repositório. As referências de produto e implementação foram atualizadas neste ADR e nas quatro partes da spec.

---

## 7. Riscos e pontos de atenção

- **Versões diferentes de Api e Web:** um dos dois no ar sem o outro quebra o cadastro e a edição (o Web ainda enviaria `estaAtivo`; a Api nova ignoraria e criaria sempre ativo). Publicar juntos.
- **Divergência do enum entre Web e Api:** a cópia do Web não é checada pelo compilador. O teste dos nomes do enum e o roteiro manual são a rede de proteção; se o enum crescer, mudar nos dois lados e no banco (migration de dados, se renomear).
- **Fuso horário:** a regra "não futura" usa a data UTC, e a data padrão da página também. Perto da meia-noite UTC (21h em Brasília), o dia exibido pode diferir do calendário local; é o mesmo comportamento aceito nas demais telas.
- **`DataVencimento` histórica:** nos Movimentos já inativos antes da feature, `DataVencimento` pode guardar a data de encerramento antiga. Não há correção automática (ADR 0020); fica a critério do usuário.
- **Fusão de vários encerrados em um novo:** continua manual, com a observação no campo próprio; nenhum vínculo estruturado (decisão do ADR 0020).

## 8. Fora de escopo

- Uso do motivo em dashboards, totalizadores e na projeção do Objetivo (ADR 0021).
- Filtro por motivo na listagem.
- Encerramento em lote e ação de "reinvestir" que crie o novo Movimento automaticamente.
- Auditoria de encerramentos (quem e quando).
- Componentes Blazor.
