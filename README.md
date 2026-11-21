# Microservicios Drive — equipos y niveles de administración

Código completo de la API ASP.NET Core 10 + Entity Framework Core/MySQL y la aplicación React. Se tomó como base `MicroserviciosDrive.zip`, la versión más completa de los tres ZIP recibidos. Los otros dos contienen versiones anteriores. No se recibió el PDF del enunciado; se mantuvieron las reglas implementadas por la API existente.

## Arranque con Docker

Requiere Docker con Docker Compose. Desde esta carpeta:

```sh
cp .env.example .env
docker compose up --build -d
```

Abre **http://localhost:8081**. La API está en **http://localhost:5239/api** y, en desarrollo, la documentación de prueba en **http://localhost:5239/scalar**.

En una instalación NUEVA, el administrador inicial es de **nivel 2**:

- Correo: `admin@proyectom.com`
- Contraseña: `password`

Puedes establecer otras credenciales antes del primer arranque con `ADMIN_EMAIL` y `ADMIN_PASSWORD` en `.env`. Modifica también `MYSQL_ROOT_PASSWORD` y `JWT_SECRET` si vas a utilizar el sistema fuera de una demostración local. La configuración inicial no sobrescribe cuentas existentes.

La primera compilación descarga las imágenes y dependencias. Consulta `docker compose logs api` si aún está arrancando. Los puertos se pueden cambiar con `WEB_PORT`, `API_PORT` y `MYSQL_PORT` en `.env`.

## Permisos

| Acción | Usuario normal | Administrador nivel 1 | Administrador nivel 2 |
| --- | --- | --- | --- |
| Consultar y descargar contenido de su equipo | Sí | Sí | Sí |
| Crear carpetas y subir archivos a su equipo | Sí | Sí | Sí |
| Renombrar/eliminar contenido propio | Sí; carpetas solo vacías | Sí | Sí |
| Administrar contenido de otros miembros de su equipo | No | Sí | Sí |
| Crear, activar, editar y desactivar usuarios de su equipo | No | Sí, salvo cuentas de nivel 2 | Sí |
| Asignar nivel 2 o cambiar un usuario a otro equipo | No | No | Sí |
| Crear/renombrar equipos y navegar por todos ellos | No | No | Sí |
| Compartir archivos/carpetas con otro equipo y revocar accesos | No | No | Sí |
| Leer contenido que nivel 2 haya compartido con su equipo | Sí | Sí | Sí |

Cada usuario pertenece a un solo equipo. Los archivos pertenecen al equipo de su carpeta; la pertenencia de las carpetas es independiente del equipo actual de su creador. Cambiar de equipo a una persona **no mueve su contenido anterior** y le retira el acceso a ese contenido salvo que exista una concesión compartida.

Los permisos se comprueban en el servidor en cada petición, incluyendo consultas por ID, descarga, renombrado, borrado, subida, usuarios y tokens de sesión. Cambios de rol, equipo o estado se reflejan en la siguiente petición, incluso con un JWT emitido anteriormente.

Se mantiene la compatibilidad del campo `esAdmin`: un cliente anterior que mande `esAdmin: true` sin nivel explícito crea un administrador de nivel 1. Los clientes nuevos deben enviar `nivelAdmin: 0`, `1` o `2`, y `esAdmin` coherente con ese nivel.

## Cómo utilizarlo

1. Inicia sesión con un administrador de nivel 2.
2. En **Equipos**, crea los equipos necesarios.
3. Selecciona un equipo y abre **Usuarios** para crear usuarios normales y administradores de nivel 1. El nivel 2 puede asignar cualquier nivel.
4. Los usuarios también pueden autoregistrarse con sus datos personales. El registro siempre crea una cuenta normal, inactiva y **sin equipo**: un administrador debe asignarle un equipo y activarla desde **Usuarios**.
5. En **Archivos**, crea una carpeta y ábrela para subir archivos. Los archivos conservan el límite de 10 MB y no se admiten en la raíz.
6. Para compartir, un administrador de nivel 2 abre el equipo de origen, pulsa **Compartir** en el archivo o carpeta y elige el equipo destino.
7. Los destinatarios acceden desde **Compartido con este equipo**. El administrador de nivel 2 puede **Revocar** desde el mismo diálogo o desde esa lista.

Compartir concede **lectura y descarga**, sin escritura, renombrado, eliminación ni permiso para volver a compartir. Compartir una carpeta incluye todo su subárbol y los archivos que se añadan después; no abre sus carpetas superiores ni sus hermanas. Compartir un archivo individual tampoco abre su carpeta. El recurso permanece en el equipo original y sus cambios son visibles para los destinatarios.

Si existen dos concesiones que cubran un mismo recurso (por ejemplo, la carpeta y un archivo suyo), revocar una no cancela la otra. El diálogo muestra concesiones directas y avisa de que puede existir acceso heredado.

No puedes cambiar tu propio equipo/nivel ni eliminar o desactivar tu propia cuenta. Una cuenta con contenido debe desactivarse o limpiar su contenido antes de eliminarla, para no borrar involuntariamente trabajo de otros usuarios dentro de sus carpetas.

## Actualizar una instalación existente

1. Respalda MySQL y el volumen de archivos antes de actualizar.
2. Conserva el **mismo nombre de proyecto de Compose**, `.env` y volúmenes `db_data` y `files_data` de tu instalación. Si descomprimes en otra carpeta, usa `docker compose -p NOMBRE_ANTERIOR ...` para seguir utilizando los mismos volúmenes.
3. Detén la versión anterior para liberar sus puertos y arranca esta versión con `docker compose up --build -d` usando ese mismo nombre de proyecto. No uses `down -v` sobre datos que quieras conservar.
4. Al arrancar, la API aplica automáticamente la migración nueva, sin recrear las tablas originales:
   - Crea **Equipo principal** y asigna a él todos los usuarios y carpetas existentes.
   - Conserva IDs, contraseñas, tokens, jerarquía y nombres físicos de los archivos.
   - Los administradores existentes conservan nivel 1; el administrador **activo con menor ID** pasa a nivel 2 para poder configurar los equipos.
   - En una base vacía se crea el administrador inicial de nivel 2.
5. Inicia sesión de nuevo y crea los equipos. Distribuye usuarios desde **Usuarios**. El contenido anterior permanece en Equipo principal: el nivel 2 puede compartirlo y crear las nuevas carpetas de cada equipo.

Si una base anterior ya tiene usuarios pero **ningún administrador activo**, el sembrado respeta sus cuentas; tendrás que restaurar una cuenta administrativa desde tu procedimiento de recuperación antes de usar la nueva administración.

Las migraciones son incrementales. No reviertas a la versión anterior después de crear equipos: el esquema antiguo no representa la separación de equipos ni sus permisos. Los archivos `tests/esquema_inicial.sql` y `tests/migracion_equipos.sql` son scripts generados por EF para la prueba de migración, **no deben ejecutarse adicionalmente cuando la API ya aplicó sus migraciones**.

## Desarrollo sin contenerizar la interfaz

```sh
# Base de datos
# Desde la raíz del proyecto:
docker compose up -d mysql

# API; requiere SDK .NET 10:
cd MicroserviciosProyectoApi
dotnet restore
dotnet run

# En otra terminal; requiere Node 24 y npm:
cd MicroserviciosProyectoApp
npm ci
npm run dev
```

Abre `http://localhost:5173`. Vite reenvía `/api` a `http://localhost:5239`. Para un servidor diferente puede configurarse `VITE_API_URL` al compilar o arrancar la interfaz. En Docker, Nginx reenvía `/api` a la API por la red interna; el navegador utiliza el mismo origen de la interfaz.

## Rutas añadidas

- `GET /api/autenticacion/yo`: identidad y permisos actuales.
- `GET /api/equipos`: solo el equipo propio; nivel 2 obtiene todos.
- `POST /api/equipos`, `PUT /api/equipos/{id}`: crear y renombrar; solo nivel 2.
- `GET /api/compartidos?equipoId=...`: contenido recibido por un equipo.
- `POST /api/compartidos`: `{ "equipoDestinoId": 2, "directorioId": 10 }` o `{ "equipoDestinoId": 2, "archivoId": 15 }`; solo nivel 2.
- `GET /api/compartidos/recurso?directorioId=10` o `?archivoId=15`: concesiones directas; solo nivel 2.
- `DELETE /api/compartidos/{id}`: revocar; solo nivel 2.

Se conservan las rutas existentes de autenticación, usuarios, directorios y archivos. Las consultas de usuarios y raíces aceptan `equipoId`; el servidor verifica que el solicitante pueda elegir ese equipo.

## Validación

Consulta `VALIDACION.md`. Las pruebas incluidas usan Python 3 estándar y Docker, sin paquetes Python adicionales. `integracion.py` crea datos con identificadores aleatorios: ejecútala contra una instalación desechable, no contra una base de uso cotidiano.

```sh
# Suite completa: crea una instalación desechable propia y la elimina al finalizar.
# Requiere libres los puertos 15339, 13316 y 15173.
./tests/ejecutar.sh

# Ejemplo con una instalación de prueba que exponga la API en el puerto 15339:
API_URL=http://localhost:15339/api ADMIN_PASSWORD='AdminPruebas2026!' python3 tests/integracion.py

# Prueba de migración dentro del MySQL de esa instalación:
MYSQL_CONTAINER=microservicios-equipos-pruebas-mysql-1 python3 tests/migracion.py

# Compilación de la interfaz:
cd MicroserviciosProyectoApp
npm run build
npm run lint

# Comprobación del modelo EF, desde la raíz, con SDK .NET 10:
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project MicroserviciosProyectoApi
```

La suite permite cambiar los puertos con `TEST_API_PORT`, `TEST_MYSQL_PORT` y `TEST_WEB_PORT`.

La migración y la instantánea del modelo se generaron con EF Core y se revisó su SQL siguiendo la documentación oficial de [gestión de migraciones de Microsoft](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing).
