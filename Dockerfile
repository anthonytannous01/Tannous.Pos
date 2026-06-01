# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY Tannous.Pos.Domain/*.csproj ./Tannous.Pos.Domain/
COPY Tannous.Pos.Application/*.csproj ./Tannous.Pos.Application/
COPY Tannous.Pos.Infrastructure/*.csproj ./Tannous.Pos.Infrastructure/
COPY Tannous.Pos.WebApi/*.csproj ./Tannous.Pos.WebApi/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish Tannous.Pos.WebApi/Tannous.Pos.WebApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# curl is not in the base aspnet image; docker-compose healthchecks use curl against /health/ready
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .

# Expose port
EXPOSE 80
EXPOSE 443

# Align with compose healthchecks (optional when Compose defines healthcheck)
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -fsS http://localhost/health/ready || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "Tannous.Pos.WebApi.dll"]
