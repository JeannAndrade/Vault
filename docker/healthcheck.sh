#!/usr/bin/env bash
# Sonda de saúde para containers sem curl/wget.
# Uso: healthcheck <porta> <caminho>    (ex.: healthcheck 8080 /health/ready)
# Sai com 0 se o endpoint responder HTTP 200; caso contrário, sai com 1.
set -u

porta="${1:?Informe a porta}"
caminho="${2:?Informe o caminho}"

# Abre uma conexão TCP com o próprio container usando o recurso /dev/tcp do bash
exec 3<>"/dev/tcp/127.0.0.1/${porta}" || exit 1

printf 'GET %s HTTP/1.0\r\nHost: localhost\r\nConnection: close\r\n\r\n' "$caminho" >&3 || exit 1

# Lê apenas a linha de status (ex.: "HTTP/1.1 200 OK"); desiste após 5 segundos
read -r -t 5 linha_status <&3 || exit 1

[[ "$linha_status" == *" 200 "* ]]
