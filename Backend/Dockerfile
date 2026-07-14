# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# SQLite database file will be saved in /app/data/aitalenthub.db to support persistent volume mounting if needed
RUN mkdir -p /app/data
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Health check so Render knows when the container is ready
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "Backend.dll"]

