#!/bin/sh
# Crea y elimina únicamente su propia instalación desechable, con un nombre único.
set -eu
cd "$(dirname "$0")/.."
proyecto="microservicios-equipos-test-$$"
entorno=$(mktemp)
limpiar() {
    docker compose -p "$proyecto" --env-file "$entorno" down -v >/dev/null 2>&1 || true
    rm -f "$entorno"
}
trap limpiar EXIT
trap 'exit 130' INT TERM
cat > "$entorno" <<CONF
MYSQL_ROOT_PASSWORD=PruebasEquiposLocal2026
MYSQL_DATABASE=ProyectoM
MYSQL_PORT=${TEST_MYSQL_PORT:-13316}
API_PORT=${TEST_API_PORT:-15339}
WEB_PORT=${TEST_WEB_PORT:-15173}
ADMIN_PASSWORD=AdminPruebas2026!
CONF
docker compose -p "$proyecto" --env-file "$entorno" up --build -d
export API_URL="http://localhost:${TEST_API_PORT:-15339}/api"
export ADMIN_PASSWORD='AdminPruebas2026!'
export MYSQL_CONTAINER="${proyecto}-mysql-1"
python3 - <<'PY'
import json
import os
import time
from urllib.request import Request, urlopen
for intento in range(60):
    try:
        req = Request(os.environ['API_URL'] + '/autenticacion/iniciar-sesion',
                      data=json.dumps({'correo': 'admin@proyectom.com', 'contrasena': os.environ['ADMIN_PASSWORD']}).encode(),
                      headers={'Content-Type': 'application/json'}, method='POST')
        with urlopen(req, timeout=2) as r:
            if r.status == 200:
                break
    except Exception:
        time.sleep(1)
else:
    raise SystemExit('La API de prueba no arrancó a tiempo. Revisa los logs de Docker.')
PY
python3 tests/integracion.py
python3 tests/migracion.py
