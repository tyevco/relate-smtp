# syntax=docker/dockerfile:1

# Node.js build stage for web frontend
FROM node:22-bookworm-slim@sha256:80fdb3f57c815e1b638d221f30a826823467c4a56c8f6a8d7aa091cd9b1675ea AS node-build
WORKDIR /src

# Copy web frontend sources
COPY web web/
COPY packages packages/
COPY package.json package-lock.json ./

# Build web frontend
RUN npm ci && npm run build:web

# .NET build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:f061e5a7532b36fa1d1b684857fe1f504ba92115b9934f154643266613c44c62 AS build
WORKDIR /src

# Copy Directory.Build.props, csproj files, and lock files for restore
COPY api/Directory.Build.props api/Directory.Build.props
COPY api/src/Relate.Smtp.Core/Relate.Smtp.Core.csproj api/src/Relate.Smtp.Core/
COPY api/src/Relate.Smtp.Infrastructure/Relate.Smtp.Infrastructure.csproj api/src/Relate.Smtp.Infrastructure/
COPY api/src/Relate.Smtp.Api/Relate.Smtp.Api.csproj api/src/Relate.Smtp.Api/
COPY api/src/Relate.Smtp.SmtpHost/Relate.Smtp.SmtpHost.csproj api/src/Relate.Smtp.SmtpHost/
COPY api/src/Relate.Smtp.Pop3Host/Relate.Smtp.Pop3Host.csproj api/src/Relate.Smtp.Pop3Host/
COPY api/src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj api/src/Relate.Smtp.ImapHost/
COPY api/src/Relate.Smtp.Core/packages.lock.json api/src/Relate.Smtp.Core/
COPY api/src/Relate.Smtp.Infrastructure/packages.lock.json api/src/Relate.Smtp.Infrastructure/
COPY api/src/Relate.Smtp.Api/packages.lock.json api/src/Relate.Smtp.Api/
COPY api/src/Relate.Smtp.SmtpHost/packages.lock.json api/src/Relate.Smtp.SmtpHost/
COPY api/src/Relate.Smtp.Pop3Host/packages.lock.json api/src/Relate.Smtp.Pop3Host/
COPY api/src/Relate.Smtp.ImapHost/packages.lock.json api/src/Relate.Smtp.ImapHost/
RUN dotnet restore --locked-mode api/src/Relate.Smtp.Api/Relate.Smtp.Api.csproj
RUN dotnet restore --locked-mode api/src/Relate.Smtp.SmtpHost/Relate.Smtp.SmtpHost.csproj
RUN dotnet restore --locked-mode api/src/Relate.Smtp.Pop3Host/Relate.Smtp.Pop3Host.csproj
RUN dotnet restore --locked-mode api/src/Relate.Smtp.ImapHost/Relate.Smtp.ImapHost.csproj

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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:8c7671a6f0f984d0c102ee70d61e8010857de032b320561dea97cc5781aea5f8 AS api
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:8c7671a6f0f984d0c102ee70d61e8010857de032b320561dea97cc5781aea5f8 AS smtp
WORKDIR /app

COPY --from=build /app/smtp .

# Switch to built-in non-root user (UID 1654 in .NET 8+ images)
USER app

# OCI Labels
LABEL org.opencontainers.image.title="Relate Mail SMTP Server"
LABEL org.opencontainers.image.description="SMTP server for receiving emails"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

EXPOSE 25 587 465 8081
ENV HealthCheck__Url=http://+:8081
ENTRYPOINT ["dotnet", "Relate.Smtp.SmtpHost.dll"]

# POP3 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:8c7671a6f0f984d0c102ee70d61e8010857de032b320561dea97cc5781aea5f8 AS pop3
WORKDIR /app

COPY --from=build /app/pop3 .

# Switch to built-in non-root user (UID 1654 in .NET 8+ images)
USER app

# OCI Labels
LABEL org.opencontainers.image.title="Relate Mail POP3 Server"
LABEL org.opencontainers.image.description="POP3 server for retrieving emails"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

EXPOSE 110 995 8082
ENV HealthCheck__Url=http://+:8082
ENTRYPOINT ["dotnet", "Relate.Smtp.Pop3Host.dll"]

# IMAP runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:8c7671a6f0f984d0c102ee70d61e8010857de032b320561dea97cc5781aea5f8 AS imap
WORKDIR /app

COPY --from=build /app/imap .

# Switch to built-in non-root user (UID 1654 in .NET 8+ images)
USER app

# OCI Labels
LABEL org.opencontainers.image.title="Relate Mail IMAP Server"
LABEL org.opencontainers.image.description="IMAP server for retrieving emails"
LABEL org.opencontainers.image.vendor="Relate Mail"
LABEL org.opencontainers.image.source="https://github.com/four-robots/relate-mail"

EXPOSE 143 993 8083
ENV HealthCheck__Url=http://+:8083
ENTRYPOINT ["dotnet", "Relate.Smtp.ImapHost.dll"]
