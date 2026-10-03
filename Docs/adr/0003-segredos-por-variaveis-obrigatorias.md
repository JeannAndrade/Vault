# ADR 0003: Segredos por variáveis obrigatórias

- Status: Aceita (2026-10-03)

## Contexto

Nenhum segredo pode ficar em texto puro no repositório nem ter valor padrão inseguro.

## Decisão

Os segredos vêm do ambiente do shell (hoje, exportados no `~/.zshrc`) ou, como alternativa equivalente, de um arquivo `.env` ignorado pelo git e pelo contexto de build; o Docker Compose os resolve nessa ordem. O compose usa `${VAR:?mensagem}` sem valor padrão para os segredos. Cada serviço recebe somente o que usa; o `JWT_SECRET` chega só à Api e nunca vai para arquivos de configuração.

## Alternativas consideradas

- Docker secrets com `_FILE`: melhor isolamento, mas a Api não lê `_FILE` e exigiria pacote novo. Reavaliar se o sistema sair de local/dev.

## Consequências

- Segredos ficam visíveis a quem tem acesso ao Docker do host (`docker inspect`) e, no caso do `~/.zshrc`, a qualquer processo do shell.
- Variáveis do shell vencem o `.env`: usar uma única fonte evita valores divergentes sem aviso.
- Pendência: o fallback do `JWT_SECRET` na `Lumia.Foundation.Auth` deve passar a falhar rápido fora de Development.
