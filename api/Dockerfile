# syntax=docker/dockerfile:1

# Node.js build stage for web frontend
FROM node:22-bookworm-slim@sha256:5373f1906319b3a1f291da5d102f4ce5c77ccbe29eb637f072b6c7b70443fc36 AS node-build
WORKDIR /src

# Copy web frontend sources
COPY web web/
COPY packages packages/
COPY package.json package-lock.json ./

# Build web frontend
RUN npm ci && npm run build:web

# .NET build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0a506ab0c8aa077361af42f82569d364ab1b8741e967955d883e3f23683d473a AS build
WORKDIR /src

# Copy csproj files and restore
COPY api/src/Relate.Smtp.Core/Relate.Smtp.Core.csproj api/src/Relate.Smtp.Core/
COPY api/src/Relate.Smtp.Infrastructure/Relate.Smtp.Infrastructure.csproj api/src/Relate.Smtp.Infrastructure/
COPY api/src/Relate.Smtp.Api/Relate.Smtp.Api.csproj api/src/Relate.Smtp.Api/
COPY api/src/Relate.Smtp.SmtpHost/Relate.Smtp.SmtpHost.csproj api/src/Relate.Smtp.SmtpHost/
COPY api/src/Relate.Smtp.Pop3Host/Relate.Smtp.Pop3Host.csproj api/src/Relate.Smtp.Pop3Host/
COPY api/src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj api/src/Relate.Smtp.ImapHost/
RUN dotnet restore api/src/Relate.Smtp.Api/Relate.Smtp.Api.csproj
RUN dotnet restore api/src/Relate.Smtp.SmtpHost/Relate.Smtp.SmtpHost.csproj
RUN dotnet restore api/src/Relate.Smtp.Pop3Host/Relate.Smtp.Pop3Host.csproj
RUN dotnet restore api/src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj

# Copy backend sources
COPY api api/

# Copy pre-built web folder from node-build stage (includes dist)
# The .csproj skips npm build if dist/index.html exists
COPY --from=node-build /src/web /src/web
COPY --from=node-build /src/packages /src/packages
COPY --from=node-build /src/package.json /src/package.json

# Build backend (API publish will detect existing web/dist and skip npm build)
WORKDIR /src/api
RUN dotnet publish src/Relate.Smtp.Api/Relate.Smtp.Api.csproj -c Release -o /app/api
RUN dotnet publish src/Relate.Smtp.SmtpHost/Relate.Smtp.SmtpHost.csproj -c Release -o /app/smtp
RUN dotnet publish src/Relate.Smtp.Pop3Host/Relate.Smtp.Pop3Host.csproj -c Release -o /app/pop3
RUN dotnet publish src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj -c Release -o /app/imap

# API runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:258b939d6d684ff05ad7ea16782b4bee55260de4acc6f99bec897fd11de7640c AS api
WORKDIR /app

COPY --from=build /app/api .

# Switch to built-in non-root user (UID 1654 in .NET 8+ images)
USER app

# OCI Labels
LABEL org.opencontainers.image.title="Relate Mail API"
LABEL org.opencontainers.image.description="REST API for Relate Mail email management"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Relate.Smtp.Api.dll"]

# SMTP runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:258b939d6d684ff05ad7ea16782b4bee55260de4acc6f99bec897fd11de7640c AS smtp
WORKDIR /app

COPY --from=build /app/smtp .

# Switch to built-in non-root user (UID 1654 in .NET 8+ images)
USER app

# OCI Labels
LABEL org.opencontainers.image.title="Relate Mail SMTP Server"
LABEL org.opencontainers.image.description="SMTP server for receiving emails"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

EXPOSE 25 587 465
ENTRYPOINT ["dotnet", "Relate.Smtp.SmtpHost.dll"]

# POP3 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:258b939d6d684ff05ad7ea16782b4bee55260de4acc6f99bec897fd11de7640c AS pop3
WORKDIR /app

COPY --from=build /app/pop3 .

# Switch to built-in non-root user (UID 1654 in .NET 8+ images)
USER app

# OCI Labels
LABEL org.opencontainers.image.title="Relate Mail POP3 Server"
LABEL org.opencontainers.image.description="POP3 server for retrieving emails"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

EXPOSE 110 995
ENTRYPOINT ["dotnet", "Relate.Smtp.Pop3Host.dll"]

# IMAP runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:258b939d6d684ff05ad7ea16782b4bee55260de4acc6f99bec897fd11de7640c AS imap
WORKDIR /app

COPY --from=build /app/imap .

# Switch to built-in non-root user (UID 1654 in .NET 8+ images)
USER app

# OCI Labels
LABEL org.opencontainers.image.title="Relate Mail IMAP Server"
LABEL org.opencontainers.image.description="IMAP server for retrieving emails"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

EXPOSE 143 993
ENTRYPOINT ["dotnet", "Relate.Smtp.ImapHost.dll"]
