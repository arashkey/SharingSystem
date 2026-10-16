# =============================================================================
#  Runtime-only Dockerfile -- the .NET build happens on the HOST.
# -----------------------------------------------------------------------------
#  make-docker.bat runs "dotnet publish" on the host using the local NuGet
#  cache, so there is no SDK stage and no "dotnet restore" here: the image build
#  itself never needs NuGet or the network.
#
#  Expects one thing in the build context:
#    * publish/  -> output of "dotnet publish -c Release" produced by make-docker.bat
#
#  Base image: Alpine "-extra" (~165 MB vs ~230 MB for the Debian image).
#    * The plain Alpine image runs in invariant globalization mode (no ICU),
#      which breaks the Persian culture and the Solar Hijri calendar used by the
#      UI. The "-extra" variant ships ICU and tzdata, so globalization works.
#    * Alpine uses musl, so make-docker.bat keeps the linux-musl-x64 native
#      SQLite library in publish/ instead of linux-x64.
#    * Chiseled images are not used: they have no shell/tar, which the backup
#      and restore scripts run inside this image.
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra AS runtime

# Release version, passed by docker-compose.yml from <Version> in SharingSystem.Web.csproj.
ARG APP_VERSION=latest
LABEL org.opencontainers.image.title="Sharing System" \
      org.opencontainers.image.version="${APP_VERSION}"

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__DataPath=/app/data

# The data folder holds the SQLite database, uploaded files and data protection keys.
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
VOLUME /app/data

# --- app payload (published & trimmed on the host by make-docker.bat) --------
COPY publish/ ./

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SharingSystem.Web.dll"]
