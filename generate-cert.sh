#!/bin/bash
# Generate a self-signed certificate for local Docker development
# Run this script before starting docker-compose

CERT_DIR="./https"
CERT_FILE="$CERT_DIR/aspnetapp.pfx"

mkdir -p "$CERT_DIR"

if [ -f "$CERT_FILE" ]; then
    echo "Certificate already exists at $CERT_FILE"
    echo "Delete it and re-run this script to generate a new one."
    exit 0
fi

echo "Generating self-signed certificate..."

# Try PowerShell first (Windows)
if command -v pwsh &> /dev/null; then
    pwsh -Command "
        \$cert = New-SelfSignedCertificate -DnsName 'localhost','apparelshop','127.0.0.1' `
            -CertStoreLocation 'Cert:\CurrentUser\My' `
            -NotAfter (Get-Date).AddYears(5) `
            -KeyAlgorithm RSA `
            -KeyLength 2048 `
            -HashAlgorithm SHA256 `
            -FriendlyName 'ApparelShop Dev Cert'
        \$password = ConvertTo-SecureString -String '' -Force -AsPlainText
        Export-PfxCertificate -Cert \$cert -FilePath '$CERT_FILE' -Password \$password
        Write-Host 'Certificate generated successfully at $CERT_FILE'
    "
elif command -v openssl &> /dev/null; then
    openssl req -x509 -nodes -days 1825 -newkey rsa:2048 \
        -keyout "$CERT_DIR/key.pem" \
        -out "$CERT_FILE" \
        -subj "/C=AE/ST=Dubai/L=Dubai/O=ApparelShop/CN=localhost" \
        -addext "subjectAltName=DNS:localhost,DNS:apparelshop,DNS:127.0.0.1"
    echo "Certificate generated at $CERT_FILE"
else
    echo "Neither PowerShell nor OpenSSL found."
    echo "Please generate a PFX certificate manually and place it at: $CERT_FILE"
    echo ""
    echo "You can also use this command with dotnet CLI:"
    echo "  dotnet dev-certs https --export --no-password --output $CERT_FILE"
    exit 1
fi
