@echo off
REM Generate a self-signed certificate for local Docker development
REM Run this script before starting docker-compose

set CERT_DIR=.\https
set CERT_FILE=%CERT_DIR%\aspnetapp.pfx

if not exist "%CERT_DIR%" mkdir "%CERT_DIR%"

if exist "%CERT_FILE%" (
    echo Certificate already exists at %CERT_FILE%
    echo Delete it and re-run this script to generate a new one.
    exit /b 0
)

echo Generating self-signed certificate...

dotnet dev-certs https --export --no-password --output "%CERT_FILE%"

if %ERRORLEVEL% EQU 0 (
    echo Certificate generated successfully at %CERT_FILE%
) else (
    echo Failed to generate certificate.
    echo Please run: dotnet dev-certs https --trust
    echo Then re-run this script.
)
