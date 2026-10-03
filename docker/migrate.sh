#!/bin/sh
# Aplica as migrations dos dois contextos (domínio e identidade).
# Idempotente: sem migrations pendentes, nada é alterado.
# Qualquer falha encerra o script com código diferente de zero (set -e).
set -eu

echo "[migrations] Aplicando migrations do contexto de domínio (VaultDbContext)..."
/app/efbundle-vault

echo "[migrations] Aplicando migrations do contexto de identidade (VaultIdentityDbContext)..."
/app/efbundle-identity

echo "[migrations] Concluído."
