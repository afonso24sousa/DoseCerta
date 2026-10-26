#!/usr/bin/env bash
# DoseCerta (G07) - Aplica os dados iniciais fictícios no ambiente de demonstração (Issue I040).
# Corre no servidor, na pasta deploy/azure, depois de a API ter aplicado as migrações (/saude Saudavel).
#
# O 02-dados-iniciais-ficticios.sql (Issue I030) só tem INSERT com identificadores fixos, numa única
# transação: numa segunda execução falharia na chave primária. Por isso só é executado quando nenhuma
# das 4 contas fictícias existe (procuradas pelo Id e pelo e-mail). Se existir pelo menos uma, os dados
# já foram aplicados e podem ter sido alterados pelo uso da demonstração: não se faz nada.
# Como o 02 é tudo ou nada, uma aplicação falhada não deixa dados parciais e repete-se na publicação seguinte.
set -euo pipefail
cd "$(dirname "$0")"

# sqlcmd dentro do contentor, com o sa; a palavra-passe vem do ambiente do contentor, não dos argumentos.
sqlcmd_sa() {
  docker compose exec -T sqlserver sh -c \
    'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -I -d DoseCerta "$@"' \
    sqlcmd "$@"
}

CONSULTA="SET NOCOUNT ON;
SELECT COUNT(*) FROM dbo.Utilizador
WHERE Id IN ('00000000-0000-4000-8000-000000000011', '00000000-0000-4000-8000-000000000012',
             '00000000-0000-4000-8000-000000000013', '00000000-0000-4000-8000-000000000014')
   OR Email IN (N'ana@exemplo.pt', N'rui@exemplo.pt', N'joana@exemplo.pt', N'carlos@exemplo.pt');"

if ! RESPOSTA="$(sqlcmd_sa -h -1 -W -Q "$CONSULTA" 2>&1)"; then
  echo "Não foi possível contar as contas fictícias (a API já aplicou as migrações?):" >&2
  echo "$RESPOSTA" >&2
  exit 1
fi
CONTAS="$(echo "$RESPOSTA" | tr -d '[:space:]')"
if ! [[ "$CONTAS" =~ ^[0-9]+$ ]]; then
  echo "Resposta inesperada ao contar as contas fictícias: $CONTAS" >&2
  exit 1
fi

if [ "$CONTAS" -gt 0 ]; then
  echo "Dados fictícios já presentes ($CONTAS de 4 contas); nada a aplicar."
  exit 0
fi

echo "A aplicar 02-dados-iniciais-ficticios.sql"
sqlcmd_sa -i /dosecerta/02-dados-iniciais-ficticios.sql
echo "Dados fictícios aplicados."
