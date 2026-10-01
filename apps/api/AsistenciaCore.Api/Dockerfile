# Imagen base con SDK para compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY ["AsistenciaCore.Api.csproj", "./"]
RUN dotnet restore "AsistenciaCore.Api.csproj"

# Copiar todo el código y publicar en modo Release
COPY . .
RUN dotnet publish "AsistenciaCore.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final liviana solo para ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AsistenciaCore.Api.dll"]