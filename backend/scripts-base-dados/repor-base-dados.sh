#!/usr/bin/env bash
# DoseCerta (G07) - Repõe a base de dados local no estado inicial de demonstração (Issue I030).
# Uso: ./repor-base-dados.sh <servidor> <base-de-dados> <utilizador>
# A palavra-passe é pedida pelo sqlcmd ou lida da variável SQLCMDPASSWORD (nunca versionada).
set -euo pipefail
SERVIDOR="${1:-localhost,1433}"
BASE="${2:-DoseCerta}"
UTILIZADOR="${3:-sa}"
PASTA="$(cd "$(dirname "$0")" && pwd)"

sqlcmd -S "$SERVIDOR" -U "$UTILIZADOR" -C -I -b -f 65001 -Q "IF DB_ID(N'$BASE') IS NULL CREATE DATABASE [$BASE];"
for script in 03-repor-base-dados.sql 01-criar-base-dados.sql 02-dados-iniciais-ficticios.sql; do
  echo "A executar $script"
  FICHEIRO="$PASTA/$script"
  # No Git Bash (Windows), o sqlcmd precisa do caminho no formato do Windows.
  if command -v cygpath >/dev/null 2>&1; then FICHEIRO="$(cygpath -w "$FICHEIRO")"; fi
  sqlcmd -S "$SERVIDOR" -U "$UTILIZADOR" -C -I -b -f 65001 -d "$BASE" -i "$FICHEIRO"
done
echo "Base de dados $BASE reposta."
