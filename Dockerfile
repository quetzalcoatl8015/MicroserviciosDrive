# ---------------------------------------------------------------------------
# Imagen de la API del repositorio de archivos.
# Se construye en dos etapas para que la imagen final no cargue el SDK completo:
#   1) "build"   -> compila y publica la aplicación
#   2) "runtime" -> solo el runtime de ASP.NET con los archivos publicados
# ---------------------------------------------------------------------------

# --- Etapa 1: compilar y publicar ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copia primero solo el archivo de proyecto para que la restauración de paquetes quede
# en una capa propia y no se repita cada vez que cambia el código.
COPY ["MicroserviciosProyectoApi/Proyecto.csproj", "MicroserviciosProyectoApi/"]
RUN dotnet restore "MicroserviciosProyectoApi/Proyecto.csproj"

# Después se copia el resto del código y se publica en modo Release.
COPY . .
WORKDIR "/src/MicroserviciosProyectoApi"
RUN dotnet publish "Proyecto.csproj" -c Release -o /app/publish --no-restore

# --- Etapa 2: imagen final ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

# El contenedor escucha en el 8080; docker-compose lo publica en el puerto de la API.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Punto de montaje del volumen nombrado con los archivos subidos por los usuarios.
# docker-compose monta "files_data" aquí, de modo que el contenido sobrevive a la
# recreación del contenedor.
VOLUME ["/app/storage"]

ENTRYPOINT ["dotnet", "Proyecto.dll"]
