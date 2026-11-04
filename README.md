# MicroserviciosDrive - Repositorio de Archivos Compartido

Sistema de gestión de archivos en la nube con autenticación de usuarios, organización en directorios jerárquicos y control de permisos.

## Estado Actual del Proyecto

**Nota importante**: El proyecto actualmente está implementado como una arquitectura monolítica, aunque las carpetas tienen el prefijo "Microservicios". Estamos en proceso de transformación a una arquitectura de microservicios real.

### Arquitectura Actual
- **Backend**: ASP.NET Core 10.0 (API monolítica, sin interfaz gráfica)
- **Frontend**: React + Vite, servido por nginx dentro de Docker
- **Base de datos**: MySQL 8.4
- **Orquestación**: Docker Compose

> **Sobre la carpeta `Services/`**: contiene el trabajo en curso de la transición a
> microservicios. Sus servicios están marcados con un *profile* de Docker Compose, así que
> **no se levantan con el comando normal** y no son necesarios para usar la aplicación.
> Para arrancarlos hay que pedirlos de forma explícita:
> `docker compose --profile microservicios up -d`

## Requisitos del Sistema

### Software Necesario

Para ejecutar el proyecto localmente, necesitas tener instalado:

#### Opción 1: Con Docker (Recomendada)
- **Docker Desktop** 4.0+ o Docker Engine
- **Docker Compose** 2.0+ (incluido en Docker Desktop)
- **Git** (para clonar el repositorio)

#### Opción 2: Sin Docker (Desarrollo Local)
- **.NET SDK** 10.0 o superior
- **Node.js** 20.x o superior
- **MySQL Server** 8.4 o superior
- **Git**

### Requisitos de Hardware
- Mínimo 4 GB de RAM
- 10 GB de espacio en disco libre
- Procesador de 64 bits

## Instalación y Configuración

### 1. Clonar el Repositorio

```bash
git clone git@github.com:quetzalcoatl8015/MicroserviciosDrive.git
cd MicroserviciosDrive
```

### 2. Configurar Variables de Entorno (Opcional)

El proyecto arranca sin configuración adicional usando los valores predeterminados. Si
necesitas cambiar algo (puertos, contraseñas), copia la plantilla:

```bash
cp .env.example .env
```

Las variables más importantes:

```env
# Base de datos MySQL
MYSQL_ROOT_PASSWORD=password
MYSQL_DATABASE=ProyectoM
MYSQL_PORT=3306

# API
API_PORT=5239
JWT_CLAVE_SECRETA=koNGJ89L4gn9w@0ifAsmf#opdZsgmx_O
ASPNETCORE_ENVIRONMENT=Development

# Frontend
FRONTEND_PORT=5173
VITE_API_URL=http://localhost:5239/api
FRONTEND_ORIGIN=http://localhost:5173
```

> `.env` no se versiona en Git porque contiene contraseñas y la clave con la que se firman
> los JWT. `.env.example` sí se versiona y documenta qué hace cada variable.

> `VITE_API_URL` y `FRONTEND_ORIGIN` son la misma dirección vista desde dos lados: el
> navegador (a dónde llama) y la API (a quién deja entrar). Si cambias el puerto del
> frontend, cambia las dos.

### 3. Levantar el Proyecto con Docker

Un solo comando levanta la base de datos, la API y el frontend:

```bash
docker compose up -d

# Para ver los logs
docker compose logs -f

# Para detener todo
docker compose down
```

**Comandos útiles:**

```bash
# Levantar solo la base de datos (para desarrollo con "dotnet run" y "npm run dev")
docker compose up -d mysql

# Reconstruir las imágenes después de cambiar código
docker compose up -d --build

# Reconstruir solo el frontend (necesario al cambiar VITE_API_URL)
docker compose build frontend

# Eliminar todo (incluyendo volúmenes de datos)
docker compose down -v

# Levantar además los microservicios de la carpeta Services/
docker compose --profile microservicios up -d
```

### 4. Acceso a los Servicios

Una vez levantado el proyecto:

- **Aplicación web (frontend)**: http://localhost:5173
- **API Backend**: http://localhost:5239
- **Base de datos MySQL**: localhost:3306

El frontend es la única interfaz de la aplicación: se abre en el navegador y es lo que
usa el usuario final. La API es una API pura, sin ninguna página para probarla, tal como
exige el proyecto.

### 5. Usuario Administrador Inicial

El sistema crea automáticamente un administrador la primera vez que arranca sobre una base
de datos vacía (aplica las migraciones y siembra la cuenta):

- **Correo**: `admin@proyectom.com`
- **Contraseña**: `password`

En el módulo de usuarios se pueden crear más cuentas y activar a quienes se registren por
su cuenta.

## Desarrollo Local (Sin Docker)

Si prefieres desarrollar sin Docker, levanta solo la base de datos en Docker y ejecuta las
dos partes por separado:

```bash
docker compose up -d mysql
```

### Backend

```bash
cd MicroserviciosProyectoApi
dotnet restore
dotnet run
```

La API estará disponible en: http://localhost:5239

### Frontend

```bash
cd MicroserviciosProyectoApp
npm install
npm run dev
```

El frontend estará disponible en: http://localhost:5173

En este modo el frontend usa su dirección por omisión (`http://localhost:5239/api`) y la
API acepta el origen `http://localhost:5173`, así que no hay que configurar nada.

### Base de Datos MySQL

Si usas tu propia instalación de MySQL:

1. Crea la base de datos `ProyectoM`
2. Configura la cadena de conexión en `MicroserviciosProyectoApi/appsettings.json`
3. Ejecuta las migraciones:
   ```bash
   cd MicroserviciosProyectoApi
   dotnet ef database update
   ```

## Funcionalidades Actuales

### Módulo de Autenticación
- Registro de usuarios (requiere activación por admin)
- Inicio de sesión con JWT (token de acceso + token de refresco)
- Renovación de tokens
- Cierre de sesión

### Módulo de Usuarios (Solo Administradores)
- Listado paginado de usuarios
- Creación de usuarios
- Edición de usuarios
- Activación/desactivación
- Revocación de tokens de sesión
- Eliminación de usuarios

### Módulo de Directorios
- Creación de directorios (jerarquía)
- Listado paginado por nivel
- Renombrado (solo propietario o admin)
- Eliminación (vacío por usuarios comunes, con contenido por admins)

### Módulo de Archivos
- Subida de archivos (máximo 10 MB)
- Descarga de archivos
- Listado paginado por directorio con filtros
- Renombrado (solo propietario o admin)
- Eliminación (solo propietario o admin)

## Usuario Administrador Inicial

El sistema crea automáticamente un usuario administrador al primer inicio:

- **Correo**: admin@proyectom.com
- **Contraseña**: password
- **Estado**: Activo

**Importante**: Cambia esta contraseña en producción.

## Estructura del Proyecto

```
MicroserviciosDrive/
├── MicroserviciosProyectoApi/      # Backend ASP.NET Core (API pura, sin interfaz gráfica)
│   ├── Controllers/                # Controladores de la API
│   ├── Models/                     # Modelos de datos
│   ├── Services/                   # Servicios (JWT, almacenamiento, sembrado)
│   ├── Migrations/                 # Migraciones de EF Core
│   └── Program.cs                  # Punto de entrada
├── MicroserviciosProyectoApp/      # Frontend React
│   ├── src/                        # Código fuente React
│   │   ├── repositorio/            # Pantalla principal y sus ventanas
│   │   ├── usuarios/               # Módulo de usuarios (solo administradores)
│   │   ├── componentes/            # Piezas reutilizables
│   │   └── utils/                  # Cliente HTTP y manejo de la sesión
│   ├── Dockerfile                  # Imagen del frontend (compila con Vite y sirve con nginx)
│   ├── nginx.conf                  # Configuración de nginx, incluye las rutas de React
│   └── package.json                # Dependencias Node.js
├── Services/                       # Microservicios en construcción (perfil aparte)
├── docker-compose.yml              # Orquestación Docker
├── Dockerfile                      # Imagen Docker del backend
├── .env.example                    # Plantilla de variables de entorno
└── README.md                       # Este archivo
```

## Próximos Pasos (Transformación a Microservicios)

El proyecto se transformará a una arquitectura de microservicios real con:

- **Servicio de Autenticación**: Gestión de tokens y sesiones
- **Servicio de Usuarios**: Gestión de cuentas y permisos
- **Servicio de Archivos**: Almacenamiento y gestión de archivos
- **Servicio de Directorios**: Gestión de la jerarquía de carpetas
- **API Gateway**: Punto de entrada único
- **Service Discovery**: Descubrimiento de servicios
- **Message Broker**: Comunicación asíncrona entre servicios

Estos servicios ya existen en la carpeta `Services/` y están en construcción. Se levantan
con `docker compose --profile microservicios up -d`, aparte del arranque normal.

## Solución de Problemas

### El contenedor MySQL no inicia
- Verifica que el puerto 3306 no esté en uso
- Revisa los logs: `docker compose logs mysql`

### La aplicación web no abre
- Revisa los logs: `docker compose logs frontend`
- Comprueba que el contenedor esté arriba: `docker compose ps`
- Si cambiaste `FRONTEND_PORT`, abre la nueva dirección **y** actualiza
  `FRONTEND_ORIGIN` en el `.env`, porque la API solo acepta peticiones de ese origen

### La aplicación abre pero las peticiones fallan
- Casi siempre es CORS: `FRONTEND_ORIGIN` debe coincidir exactamente con la dirección
  desde la que se abre el frontend (protocolo, nombre y puerto, sin barra final)
- Revisa la consola del navegador; el mensaje de error lo indica

### La API no conecta a la base de datos
- Asegúrate de que el contenedor MySQL esté saludable: `docker compose ps`
- Verifica la cadena de conexión en `docker-compose.yml`

### Error de permisos al subir archivos
- Verifica que el volumen `files_data` tenga los permisos correctos
- En Linux/Mac, ajusta los permisos del directorio de almacenamiento

### Cambié código y el contenedor sigue igual
- Las imágenes no se reconstruyen solas. Usa `docker compose up -d --build`
- En el frontend, si cambiaste `VITE_API_URL`, hace falta `docker compose build frontend`:
  la dirección se escribe dentro del JavaScript al compilar, no se lee al arrancar

## Soporte

Para reportar problemas o sugerencias, abre un issue en el repositorio de GitHub.

## Licencia

Este proyecto es parte de un trabajo académico para la materia de Microservicios.
