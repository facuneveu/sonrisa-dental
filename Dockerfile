# Imagen para publicar Sonrisa Dental en Render, Azure Container Apps o cualquier hosting con Docker.
# Se construye desde la raíz del repositorio:   docker build -t sonrisa-dental .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Primero solo el .csproj: así la restauración de paquetes queda en caché si no cambiaron las dependencias.
COPY ClinicaDental/ClinicaDental.csproj ClinicaDental/
RUN dotnet restore ClinicaDental/ClinicaDental.csproj
COPY ClinicaDental/ ClinicaDental/
RUN dotnet publish ClinicaDental/ClinicaDental.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Detrás del proxy del hosting (que pone el HTTPS) hay que respetar X-Forwarded-Proto; si no, las cookies y las
# redirecciones creerían que la conexión es HTTP. Si el hosting define PORT, la aplicación lo usa.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080
# Las imágenes oficiales de ASP.NET traen un usuario sin privilegios llamado "app".
USER app
ENTRYPOINT ["dotnet", "ClinicaDental.dll"]
