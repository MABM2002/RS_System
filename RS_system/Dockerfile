# Runtime — with Chromium installed via apt
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base

# Install Chromium browser directly via apt-get (system package manager)
RUN apt-get update && apt-get install -y --no-install-recommends \
    chromium \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore
COPY RS_system/RS_system.csproj RS_system/
COPY Reports.PdfEngine/Reports.PdfEngine.csproj Reports.PdfEngine/
RUN dotnet restore "RS_system/RS_system.csproj"

# Copy everything and build
COPY . .
RUN dotnet build "RS_system/RS_system.csproj" -c $BUILD_CONFIGURATION --no-restore

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RS_system/RS_system.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RS_system.dll"]
