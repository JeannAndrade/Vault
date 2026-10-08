# ADR 0009: Índice (UserId, DataInvestimento) em Movimentos

- Status: Aceita (2026-10-08)

## Contexto

A listagem paginada de Movimentos filtra por `UserId` e ordena por `DataInvestimento` decrescente. Os índices existentes começam por `UserId` e atendem o filtro, mas não a ordenação: a cada página, o banco ordenaria em memória todos os movimentos do usuário. Com o volume atual isso é imperceptível. O requisito foi registrado agora para não ficar esquecido.

## Decisão

Criar o índice composto `(UserId, DataInvestimento)` e tornar a ordenação inteiramente decrescente (`DataInvestimento DESC, Id DESC`). O InnoDB acrescenta a chave primária a todo índice secundário; com a ordenação toda no mesmo sentido, o banco percorre o índice de trás para a frente, sem ordenar em memória. Não há migração de dados: o ambiente está em recadastro.

## Alternativas consideradas

- Não criar agora e definir um gatilho de medição: o volume atual não exige, mas o requisito tende a ser esquecido.
- Índice com `DataInvestimento DESC` explícito, mantendo o `Id` ascendente: depende de o provider do EF emitir colunas descendentes para MariaDB, o que não foi validado.

## Consequências

- A tabela passa a ter 11 índices secundários; há um pequeno custo extra em escrita.
- O ganho depende de a ordenação do repositório continuar toda descendente. Mudar a direção ou o critério exige reavaliar o índice. Um teste de metadados do modelo garante que o índice existe.
- Com poucas linhas o otimizador pode ignorar o índice. O uso deve ser confirmado com `EXPLAIN` quando houver volume.
- A migration herda o ruído de `IdentityRole` (`HasData` sem `Id` fixo) das anteriores.
