# Runtime — with Chromium headless dependencies
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base

# Install Chromium/Playwright OS-level dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2 \
    libxshmfence1 \
    fonts-liberation \
    fonts-noto-color-emoji \
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
RUN dotnet build "RS_system/RS_system.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RS_system/RS_system.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Install Playwright browsers in the publish stage
RUN dotnet tool install --global Microsoft.Playwright.CLI || true
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN playwright install --with-deps chromium || pwsh /app/publish/.playwright/node/linux/playwright.sh install --with-deps chromium || true

# Final
FROM base AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Copy Playwright browsers from build stage
COPY --from=publish /root/.cache/ms-playwright /root/.cache/ms-playwright

# Set Playwright browsers path
ENV PLAYWRIGHT_BROWSERS_PATH=/root/.cache/ms-playwright

ENTRYPOINT ["dotnet", "RS_system.dll"]
