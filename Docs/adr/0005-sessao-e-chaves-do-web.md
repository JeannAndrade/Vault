# ADR 0005: Sessão e chaves do Web não persistidas

- Status: Aceita (2026-10-03)

## Contexto

O Web guarda os tokens da Api em sessão na memória e valida o cookie de login contra essa sessão.

## Decisão

Não persistir as chaves do Data Protection nem a sessão. Ao recriar ou reiniciar o Web, o usuário faz login de novo.

## Alternativas consideradas

- Volume só para as chaves: não evita o novo login, pois a sessão em memória continua perdida.
- Sessão distribuída (SQL ou Redis): infraestrutura e dependência novas, fora do escopo atual.

## Consequências

- Novo login após recriar o container e avisos do Data Protection nos logs.
- Reavaliar se o Web sair de local/dev ou tiver mais de uma instância.
