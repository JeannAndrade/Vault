# ADR 0007: Logs em console e em volume

- Status: Aceita (2026-10-03)

## Contexto

O NLog gravava só em arquivo, inacessível pelo `docker logs` e em pasta não gravável por usuário não-root.

## Decisão

Target de console em JSON (nível Info ou acima) além do arquivo JSON (Debug). As imagens criam `/app/logs` e `/app/internal_logs` com o dono do usuário não-root, e o compose monta um volume por aplicação (`api-logs` e `web-logs`).

## Alternativas consideradas

- Somente console, sem volume: perderia o histórico de Debug ao recriar o container.

## Consequências

- O `docker logs` mistura JSON (NLog) e texto (framework).
- Um volume criado antes do `chown` existir na imagem nasce com dono root e precisa ser recriado.
