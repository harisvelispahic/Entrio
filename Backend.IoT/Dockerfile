# syntax=docker/dockerfile:1

# =============================================================================
# Entrio API — multi-stage build: SDK compiles, slim runtime ships.
# Build context is Backend.IoT/ (see docker-compose.yml).
# Every tag is pinned to a concrete patch; never :latest.
# =============================================================================

# --- Build stage --------------------------------------------------------------
# 8.0.425 is the newest .NET 8 SDK patch (verified against the MCR tag list).
FROM mcr.microsoft.com/dotnet/sdk:8.0.425-bookworm-slim AS build
WORKDIR /src

# Copy ONLY the project files first. Restore depends on these alone, so this
# layer stays cached across source edits and NuGet is not re-downloaded on
# every build — the single biggest win in .NET image build times.
COPY IoT.Domain/IoT.Domain.csproj                 IoT.Domain/
COPY IoT.Application/IoT.Application.csproj       IoT.Application/
COPY IoT.Infrastructure/IoT.Infrastructure.csproj IoT.Infrastructure/
COPY IoT.API/IoT.API.csproj                       IoT.API/
RUN dotnet restore IoT.API/IoT.API.csproj

# Now the sources. Edits below only invalidate from here down.
COPY . .
RUN dotnet publish IoT.API/IoT.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Runtime stage ------------------------------------------------------------
# 8.0.31 is the newest ASP.NET Core 8 runtime patch. bookworm-slim (not alpine)
# on purpose: it ships bash, which the compose healthcheck's /dev/tcp probe needs.
FROM mcr.microsoft.com/dotnet/aspnet:8.0.31-bookworm-slim AS final
WORKDIR /app

# Kestrel listens on 8080 inside the container; compose publishes it on 5263.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

# Run as the non-root user the base image already provides (uid 1654).
# Anything written at runtime must be chowned first or it fails at runtime,
# often silently: DOTNET_ generated files and the logs directory.
RUN mkdir -p /app/logs /home/app/.aspnet \
 && chown -R app:app /app/logs /home/app
USER app

ENTRYPOINT ["dotnet", "IoT.API.dll"]
