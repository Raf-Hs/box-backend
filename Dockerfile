# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY DailyTracker.slnx ./
COPY src/DailyTracker.Domain/DailyTracker.Domain.csproj src/DailyTracker.Domain/
COPY src/DailyTracker.Application/DailyTracker.Application.csproj src/DailyTracker.Application/
COPY src/DailyTracker.Infrastructure/DailyTracker.Infrastructure.csproj src/DailyTracker.Infrastructure/
COPY src/DailyTracker.Api/DailyTracker.Api.csproj src/DailyTracker.Api/
RUN dotnet restore src/DailyTracker.Api/DailyTracker.Api.csproj

# Copy remaining source and build
COPY src/ ./src/
RUN dotnet publish src/DailyTracker.Api/DailyTracker.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create directory for SQLite database
RUN mkdir -p /data

COPY --from=build /app/publish .

# Configure for Render: bind to 0.0.0.0 on PORT env var (fallback to 10000)
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV PORT=10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "DailyTracker.Api.dll"]
