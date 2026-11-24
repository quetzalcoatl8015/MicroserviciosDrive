FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MicroserviciosProyectoApi/Proyecto.csproj", "MicroserviciosProyectoApi/"]
RUN dotnet restore "MicroserviciosProyectoApi/Proyecto.csproj"

COPY . .
WORKDIR "/src/MicroserviciosProyectoApi"
RUN dotnet publish "Proyecto.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

VOLUME ["/app/storage"]

ENTRYPOINT ["dotnet", "Proyecto.dll"]
