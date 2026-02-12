# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0a506ab0c8aa077361af42f82569d364ab1b8741e967955d883e3f23683d473a AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/Relate.Smtp.Core/Relate.Smtp.Core.csproj src/Relate.Smtp.Core/
COPY src/Relate.Smtp.Infrastructure/Relate.Smtp.Infrastructure.csproj src/Relate.Smtp.Infrastructure/
COPY src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj src/Relate.Smtp.ImapHost/
RUN dotnet restore src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj

# Copy source and build
COPY src/Relate.Smtp.Core/ src/Relate.Smtp.Core/
COPY src/Relate.Smtp.Infrastructure/ src/Relate.Smtp.Infrastructure/
COPY src/Relate.Smtp.ImapHost/ src/Relate.Smtp.ImapHost/
RUN dotnet publish src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:258b939d6d684ff05ad7ea16782b4bee55260de4acc6f99bec897fd11de7640c AS runtime
WORKDIR /app
COPY --from=build /app .

# OCI Labels
LABEL org.opencontainers.image.title="Relate IMAP Server"
LABEL org.opencontainers.image.description="IMAP4rev2 server for retrieving emails"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

# IMAP ports: 143 (plain), 993 (SSL/TLS)
EXPOSE 143 993

ENTRYPOINT ["dotnet", "Relate.Smtp.ImapHost.dll"]
