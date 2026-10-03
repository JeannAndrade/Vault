# ADR 0006: Configuração da Api via builder.Configuration

- Status: Aceita (2026-10-03)

## Contexto

A Api montava a configuração só com variáveis de ambiente, ignorando o `appsettings.json` (emissor, audiência e expiração do JWT caíam nos padrões da lib).

## Decisão

Usar `builder.Configuration`: `appsettings.json`, `appsettings.{Ambiente}.json` e variáveis de ambiente, com o ambiente prevalecendo. `JWT_SECRET` continua apenas por ambiente.

## Alternativas consideradas

- Declarar `JwtSettings__*` no compose: manteria a Api ignorando o `appsettings.json`, com configuração em dois lugares.

## Consequências

- Tokens passam a durar 15 minutos (antes, 0 mais a tolerância de relógio).
- Emissor e audiência passam a ser os do `appsettings.json`; tokens emitidos antes da troca deixam de valer e exigem novo login.
