# ADR 0004: Health checks e ordem de subida

- Status: Aceita (2026-10-03)

## Contexto

A Api só deve subir com o banco apto e o Web só com a Api apta a receber requisições.

## Decisão

A Api expõe `/health/live` (sem checks) e `/health/ready` (conectividade dos dois contextos), mapeados por `MapLumiaHealthChecks`, anônimos e com resposta em texto puro. A imagem da Api tem `HEALTHCHECK` com `docker/healthcheck.sh` (bash e `/dev/tcp`, sem `curl`). O compose usa `condition: service_healthy`. O Web não tem health check.

## Alternativas consideradas

- Sondar apenas a porta TCP: não prova que o banco responde.
- Instalar `curl` na imagem: mais superfície de ataque.

## Consequências

- `depends_on` só garante a ordem na primeira subida; após um reboot, vale `restart: unless-stopped` e a readiness que se recupera sozinha.
- Os endpoints são anônimos: ficam seguros porque a Api não é publicada.
- A readiness prova conectividade, não migrations aplicadas (isso vem da ordem do compose).
