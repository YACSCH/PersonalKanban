#!/bin/sh
set -e

# Crea un rol de aplicación sin superusuario para que la app no use el
# bootstrap superuser de la imagen.
: "${KANBAN_APP_PASSWORD:?Define KANBAN_APP_PASSWORD en .env}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -v app_password="$KANBAN_APP_PASSWORD" <<'SQL'
CREATE ROLE kanban_app LOGIN PASSWORD :'app_password';
ALTER DATABASE :"DBNAME" OWNER TO kanban_app;
SQL
