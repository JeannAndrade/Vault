# ADR 0003: Segredos por variáveis obrigatórias

- Status: Aceita (2026-10-03)

## Contexto

Nenhum segredo pode ficar em texto puro no repositório nem ter valor padrão inseguro.

## Decisão

Os segredos vêm do `.env` (ignorado pelo git e pelo contexto de build) ou do shell. O compose usa `${VAR:?mensagem}` sem valor padrão para eles. Cada serviço recebe somente o que usa; o `JWT_SECRET` chega só à Api e nunca vai para arquivos de configuração.

## Alternativas consideradas

- Docker secrets com `_FILE`: melhor isolamento, mas a Api não lê `_FILE` e exigiria pacote novo. Reavaliar se o sistema sair de local/dev.

## Consequências

- Segredos ficam visíveis a quem tem acesso ao Docker do host (`docker inspect`).
- Variáveis do shell vencem o `.env`.
- Pendência: o fallback do `JWT_SECRET` na `Lumia.Foundation.Auth` deve passar a falhar rápido fora de Development.
