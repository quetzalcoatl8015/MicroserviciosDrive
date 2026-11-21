# Validación realizada

## Compilación y modelo

- API compilada y publicada en Release con SDK .NET 10 dentro de Docker: sin errores ni advertencias en la compilación final.
- Interfaz compilada con Vite (`npm run build`): correcta.
- Revisión de React con `npm run lint`: correcta, sin advertencias.
- `dotnet ef migrations has-pending-model-changes`: sin cambios pendientes respecto a la migración incluida.
- Instalación con Docker Compose: MySQL 8.4, API y Nginx con la interfaz.

## Pruebas HTTP

`tests/integracion.py`: **150 respuestas HTTP verificadas** en cada una de dos ejecuciones completas, incluida una instalación nueva desde cero mediante `tests/ejecutar.sh`. Comprueba:

- Creación de equipos, usuarios normales y administradores de ambos niveles.
- Listados de usuarios y equipos limitados al equipo permitido.
- Bloqueo de consultas, descargas, escrituras, subidas y borrados mediante IDs de otros equipos.
- Protección de cuentas de nivel 2 frente a administradores de nivel 1, incluso dentro del mismo equipo.
- Bloqueo de promoción a nivel 2 o cambio de equipo por parte del nivel 1.
- Carpetas con el mismo nombre en equipos distintos y rechazo de duplicados en el mismo nivel/equipo.
- Reglas anteriores de propiedad, eliminación de carpetas no vacías, extensión y límite de 10 MB.
- Compartición de archivos individuales y carpetas con descendientes actuales y futuros.
- El contenido compartido es de lectura, sin acceso a padres ni hermanos.
- Revocación de accesos y limpieza de concesiones al eliminar contenido.
- Degradación de rol, cambio de equipo y desactivación efectivos con JWT emitidos previamente.
- Autoregistro sin equipo, pendiente de activación por un administrador y sin posibilidad de asignarse privilegios.
- Rotación de token de refresco, rechazo de reutilización y cierre de sesión.
- Eliminación recursiva de carpetas y protección de cuentas con contenido.

## Migración con datos anteriores

`tests/migracion.py` crea una base temporal independiente, instala el esquema anterior e inserta tres usuarios, dos carpetas en jerarquía, un archivo y una sesión. Aplica el SQL de la migración nueva y verifica:

- IDs, contraseñas, sesión, jerarquía y referencia física del archivo conservados.
- Todos los usuarios y carpetas asignados al Equipo principal.
- Usuario normal con nivel 0, administrador más antiguo activo con nivel 2 y segundo administrador con nivel 1.
- Retiro del código de equipo y pertenencia opcional (cuentas sin equipo).
- Registro correcto de las tres migraciones en el historial.

La base temporal se elimina al finalizar. No se modifica una instalación anterior del usuario.

## Comprobación en navegador

- Inicio de sesión de nivel 2.
- Creación de equipo y administrador de nivel 1 desde los formularios.
- Creación de carpeta, subida y compartición de un archivo.
- Inicio de sesión como usuario normal del equipo receptor: selector de equipo bloqueado, sin controles de administración.
- Descarga del archivo compartido desde la interfaz, con el nombre original.
- Flujo funcional sin errores JavaScript ni advertencias de React. También se comprobó que un error esperado HTTP 400 por contraseñas distintas se muestra dentro del formulario.

## Alcance

No se recibió el PDF ni una copia de la base de datos y archivos reales del usuario. Se validó la conservación de datos con una instalación representativa del esquema recibido. Las pruebas cubren comportamiento funcional y aislamiento, no son una prueba de carga.
