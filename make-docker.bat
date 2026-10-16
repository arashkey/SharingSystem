@echo off
REM ============================================================================
REM  make-docker.bat  --  offline docker build for Sharing System
REM ----------------------------------------------------------------------------
REM  1) reads the release version from <Version> in SharingSystem.Web.csproj
REM  2) publishes the app on the HOST (uses your local NuGet cache -> no internet)
REM  3) builds a runtime-only docker image that just copies the published output
REM     (see Dockerfile) -> the image build never touches the network
REM  4) brings the stack up and exports the image to sharing-system-<version>.docker
REM     (gzip compressed; "docker load -i" reads it directly)
REM
REM  PREREQUISITES:
REM    * a .env file next to this script. Copy .env.example and fill it.
REM    * the base image mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra in the
REM      local docker cache (pull it once while online).
REM ============================================================================
setlocal
cd /d "%~dp0"

date /t
time /t

REM --- sanity check: compose needs .env for the required secrets --------------
if not exist ".env" (
  echo.
  echo [make-docker] ERROR: .env not found.
  echo [make-docker] copy ".env.example" to ".env" and fill in the values first.
  exit /b 1
)

set PUBLISH_DIR=publish
set WEB_PROJ=src\SharingSystem.Web\SharingSystem.Web.csproj
set IMAGE_NAME=sharing-system
REM  The base image is Alpine (musl), so only the musl native libraries are kept.
set TARGET_RID=linux-musl-x64

echo.
echo [make-docker] (1/4) reading version from %WEB_PROJ% ...
set APP_VERSION=
for /f "usebackq delims=" %%V in (`dotnet msbuild "%WEB_PROJ%" -nologo -getProperty:Version`) do set APP_VERSION=%%V
if "%APP_VERSION%"=="" (
  echo [make-docker] ERROR: could not read ^<Version^> from %WEB_PROJ%.
  exit /b 1
)
echo [make-docker]      version: %APP_VERSION%
set EXPORT_FILE=%IMAGE_NAME%-%APP_VERSION%.docker
set EXPORT_TAR=%IMAGE_NAME%-%APP_VERSION%.tmp.tar

echo.
echo [make-docker] (2/4) publishing %WEB_PROJ% on host ...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"

dotnet publish "%WEB_PROJ%" -c Release -o "%PUBLISH_DIR%" /p:UseAppHost=false
if errorlevel 1 (
  echo [make-docker] ERROR: dotnet publish failed.
  exit /b 1
)

REM --- trim publish output: the Alpine image only runs on linux-musl-x64, so drop
REM     the native runtimes (SQLite) for every other RID and the .pdb files.
echo [make-docker]      trimming non-%TARGET_RID% runtimes from publish ...
for /d %%R in ("%PUBLISH_DIR%\runtimes\*") do (
  if /i not "%%~nxR"=="%TARGET_RID%" rmdir /s /q "%%R"
)
del /q "%PUBLISH_DIR%\*.pdb" 2>nul

echo.
echo [make-docker] (3/4) building docker image %IMAGE_NAME%:%APP_VERSION% ...
REM  APP_VERSION is read by docker-compose.yml for the image tag and build arg.
REM  "down" without -v so the data volume (database, files, keys) survives a rebuild.
docker compose down
docker compose up -d --build
if errorlevel 1 (
  echo [make-docker] ERROR: docker compose build/up failed.
  exit /b 1
)
docker tag %IMAGE_NAME%:%APP_VERSION% %IMAGE_NAME%:latest
if errorlevel 1 (
  echo [make-docker] ERROR: docker tag failed.
  exit /b 1
)

echo.
echo [make-docker] (4/4) exporting image to %EXPORT_FILE% ...
if exist "%EXPORT_FILE%" move /y "%EXPORT_FILE%" "%IMAGE_NAME%-%APP_VERSION%.bak.docker"
docker save -o "%EXPORT_TAR%" %IMAGE_NAME%:%APP_VERSION% %IMAGE_NAME%:latest
if errorlevel 1 (
  echo [make-docker] ERROR: docker save failed.
  if exist "%EXPORT_TAR%" del /q "%EXPORT_TAR%"
  exit /b 1
)

REM  docker save writes uncompressed layers; gzip shrinks the file to roughly a
REM  third. "docker load -i" detects gzip by itself, so loading is unchanged.
echo [make-docker]      compressing export with gzip ...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "$source = [IO.File]::OpenRead('%CD%\%EXPORT_TAR%');" ^
  "try {" ^
  "  $target = [IO.File]::Create('%CD%\%EXPORT_FILE%');" ^
  "  try {" ^
  "    $gzip = New-Object IO.Compression.GZipStream($target, [IO.Compression.CompressionLevel]::Optimal);" ^
  "    try { $source.CopyTo($gzip) } finally { $gzip.Dispose() }" ^
  "  } finally { $target.Dispose() }" ^
  "} finally { $source.Dispose() }"
if errorlevel 1 (
  echo [make-docker] ERROR: compressing the export failed.
  del /q "%EXPORT_TAR%"
  if exist "%EXPORT_FILE%" del /q "%EXPORT_FILE%"
  exit /b 1
)
del /q "%EXPORT_TAR%"

echo.
echo [make-docker] done. image exported: %EXPORT_FILE%  (tags: %APP_VERSION%, latest)
endlocal
time /t
