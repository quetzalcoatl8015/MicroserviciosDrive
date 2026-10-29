# MicroserviciosDrive - Repositorio de Archivos Compartido

Sistema de gestión de archivos en la nube con autenticación de usuarios, organización en directorios jerárquicos y control de permisos.

## Estado Actual del Proyecto

⚠️ **Nota Importante**: El proyecto actualmente está implementado como una arquitectura monolítica, aunque las carpetas tienen el prefijo "Microservicios". Estamos en proceso de transformación a una arquitectura de microservicios real.

### Arquitectura Actual
- **Backend**: ASP.NET Core 10.0 (API monolítica)
- **Frontend**: React + Vite
- **Base de datos**: MySQL 8.4
- **Orquestación**: Docker Compose

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

Crea un archivo `.env` en la raíz del proyecto con las siguientes variables (usa los valores predeterminados si no lo creas):

```env
# Base de datos MySQL
MYSQL_ROOT_PASSWORD=password
MYSQL_DATABASE=ProyectoM
MYSQL_PORT=3306

# API Backend
API_PORT=5239
```

### 3. Levantar el Proyecto con Docker

Esta es la forma más sencilla de ejecutar todo el proyecto:

```bash
# Levanta la base de datos y la API
docker compose up -d

# Para ver los logs
docker compose logs -f

# Para detener todo
docker compose down
```

**Comandos útiles:**

```bash
# Levantar solo la base de datos (para desarrollo de la API localmente)
docker compose up -d mysql

# Reconstruir los contenedores después de cambios
docker compose up -d --build

# Eliminar todo (incluyendo volúmenes de datos)
docker compose down -v
```

### 4. Acceso a los Servicios

Una vez levantado el proyecto:

- **API Backend**: http://localhost:5239
- **Documentación de la API (Scalar)**: http://localhost:5239/scalar/v1
- **Base de datos MySQL**: localhost:3306

### 5. Ejecutar el Frontend (Opcional)

El frontend React debe ejecutarse en una terminal separada:

```bash
cd MicroserviciosProyectoApp
npm install
npm run dev
```

El frontend estará disponible en: http://localhost:5173

## Desarrollo Local (Sin Docker)

Si prefieres desarrollar sin Docker:

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
- **Contraseña**: Admin123!
- **Estado**: Activo

⚠️ **Importante**: Cambia esta contraseña en producción.

## Estructura del Proyecto

```
MicroserviciosDrive/
├── MicroserviciosProyectoApi/      # Backend ASP.NET Core
│   ├── Controllers/                # Controladores de la API
│   ├── Models/                     # Modelos de datos
│   ├── Services/                   # Servicios (JWT, almacenamiento)
│   ├── Migrations/                 # Migraciones de EF Core
│   └── Program.cs                  # Punto de entrada
├── MicroserviciosProyectoApp/      # Frontend React
│   ├── src/                        # Código fuente React
│   └── package.json                # Dependencias Node.js
├── docker-compose.yml              # Orquestación Docker
├── Dockerfile                      # Imagen Docker del backend
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

## Solución de Problemas

### El contenedor MySQL no inicia
- Verifica que el puerto 3306 no esté en uso
- Revisa los logs: `docker compose logs mysql`

### La API no conecta a la base de datos
- Asegúrate de que el contenedor MySQL esté saludable: `docker compose ps`
- Verifica la cadena de conexión en `docker-compose.yml`

### Error de permisos al subir archivos
- Verifica que el volumen `files_data` tenga los permisos correctos
- En Linux/Mac, ajusta los permisos del directorio de almacenamiento

## Soporte

Para reportar problemas o sugerencias, abre un issue en el repositorio de GitHub.

## Licencia

Este proyecto es parte de un trabajo académico para la materia de Microservicios.
