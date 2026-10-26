#!/usr/bin/env bash
# DoseCerta (G07) - Publica a versão enviada para /srv/dosecerta/app no ambiente de demonstração (Issue I040).
# Corre no servidor, chamado pelo workflow backend-cd depois do rsync de backend/ e deploy/azure/
# e da escrita do .env. Pode também ser executado à mão na VM: bash deploy/azure/publicar.sh
#
# Passos: SQL Server até ficar healthy; base de dados e utilizador da API (init-db.sql, idempotente);
# construção da imagem e arranque da API e do Caddy (a API aplica as migrações ao arrancar);
# limpeza das imagens antigas. A espera pelo /saude e os dados fictícios ficam no workflow.
set -euo pipefail
cd "$(dirname "$0")"

if [ ! -f .env ]; then
  echo "Falta o ficheiro $(pwd)/.env (ver .env.example)." >&2
  exit 1
fi
chmod 600 .env

# A palavra-passe do dosecerta_app passa ao sqlcmd pelo nome da variável (-e), nunca nos argumentos.
set -a
# shellcheck disable=SC1091
. ./.env
set +a

echo "1/4 SQL Server"
docker compose up -d --wait sqlserver

echo "2/4 Base de dados e utilizador da API (init-db.sql)"
docker compose exec -T -e DOSECERTA_APP_PASSWORD sqlserver sh -c \
  'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -I -i /dosecerta/init-db.sql'

echo "3/4 Construção e arranque da API e do Caddy"
docker compose up -d --build --remove-orphans

echo "4/4 Limpeza de imagens antigas"
docker image prune -f

docker compose ps
