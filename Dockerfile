# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SharingSystem.slnx ./
COPY src/SharingSystem.Domain/SharingSystem.Domain.csproj src/SharingSystem.Domain/
COPY src/SharingSystem.Application/SharingSystem.Application.csproj src/SharingSystem.Application/
COPY src/SharingSystem.Infrastructure/SharingSystem.Infrastructure.csproj src/SharingSystem.Infrastructure/
COPY src/SharingSystem.Web/SharingSystem.Web.csproj src/SharingSystem.Web/
RUN dotnet restore src/SharingSystem.Web/SharingSystem.Web.csproj

COPY src/ src/
RUN dotnet publish src/SharingSystem.Web/SharingSystem.Web.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__DataPath=/app/data

# The data folder holds the SQLite database, uploaded files and data protection keys.
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
VOLUME /app/data

COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SharingSystem.Web.dll"]
