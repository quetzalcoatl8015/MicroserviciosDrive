# ---------------------------------------------------------------------------
# Imagen del frontend (React + Vite servido por nginx).
#
# Se construye en dos etapas por el mismo motivo que la imagen del backend:
# la imagen final no arrastra las herramientas de compilación.
#
#   1) "build"   -> instala dependencias y compila el proyecto con Vite,
#                   dejando los archivos estáticos en /app/dist
#   2) "runtime" -> solo nginx con esos archivos estáticos dentro
#
# Se publica el puerto 80 del contenedor; docker-compose lo expone en el host
# en el 5173, que es el puerto que el usuario espera ver.
# ---------------------------------------------------------------------------

# --- Etapa 1: compilar el proyecto ---
FROM node:22-alpine AS build

WORKDIR /app

# Se copian primero los manifiestos para que la instalación de dependencias
# quede en una capa propia y no se repita cada vez que cambia el código fuente.
COPY package.json package-lock.json ./

# "npm ci" instala exactamente las versiones del package-lock.json, de modo que
# la compilación dentro de Docker dé el mismo resultado en cualquier máquina.
RUN npm ci

# Después se copia el resto del código fuente y se compila.
COPY . .

# ---------------------------------------------------------------------------
# URL de la API que usará el navegador del usuario.
#
# IMPORTANTE: Vite reemplaza las variables de entorno en el momento de COMPILAR,
# no en tiempo de ejecución. Una vez hecho "npm run build", el valor queda
# escrito dentro del JavaScript y ya no se puede cambiar sin volver a compilar.
# Por eso se recibe como argumento de construcción (ARG) y no como variable de
# entorno del contenedor.
#
# Además, la dirección debe ser alcanzable DESDE EL NAVEGADOR del usuario, no
# desde dentro de la red de Docker: por eso el valor por omisión es
# "http://localhost:5239/api" (el puerto que docker-compose publica en la
# máquina) y no "http://api:8080/api", que solo resolvería entre contenedores.
# ---------------------------------------------------------------------------
ARG VITE_API_URL=http://localhost:5239/api
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

# --- Etapa 2: imagen final con nginx ---
FROM nginx:alpine

# Se reemplaza la configuración por omisión de nginx por la del proyecto, que
# incluye el redireccionamiento necesario para que funcionen las rutas de React.
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Se copian únicamente los archivos ya compilados, nada del código fuente.
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

# Se arranca nginx en primer plano para que el contenedor siga vivo y Docker
# pueda controlar el proceso principal.
CMD ["nginx", "-g", "daemon off;"]
