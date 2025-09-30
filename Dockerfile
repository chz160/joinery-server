# Dockerfile for production
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
WORKDIR /app
EXPOSE 8080

# Install curl for health check and create non-root user
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/* \
    && groupadd -r joinery && useradd -r -g joinery joinery

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Copy project file and restore dependencies (better layer caching)
COPY ["JoineryServer.csproj", "./"]
RUN dotnet restore "JoineryServer.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "JoineryServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JoineryServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R joinery:joinery /app

# Switch to non-root user
USER joinery

# Set environment variables  
# Use HTTP_PORTS for modern Docker compatibility (ASP.NET Core 8.0+)
ENV HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "JoineryServer.dll"]