# ADR 0002: Redes segmentadas e exposição apenas do Web

- Status: Aceita (2026-10-03)

## Contexto

Somente o Web deve ser acessível de fora; Api e banco não.

## Decisão

Três redes: `edge` (Web, com acesso externo), `app` (interna: Web e Api) e `data` (interna: Api, migrations e banco). Só o Web publica porta, em `127.0.0.1:5000`.

## Alternativas consideradas

- Uma única rede sem `ports` para Api e banco: o isolamento dependeria apenas de uma omissão.
- Publicar tudo com firewall: desnecessário e frágil.

## Consequências

- O Web não alcança o banco e a Api não tem saída para a internet.
- Se a Api precisar de integrações externas no futuro, será preciso dar a ela uma rede com saída.
- Na máquina local, o Web fica fora do alcance da rede local (loopback).
