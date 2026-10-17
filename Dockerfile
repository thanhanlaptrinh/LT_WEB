# Build ASP.NET Core MVC / Razor + EF Core SQLite.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Cache NuGet restore separately from source changes.
COPY ["LTW09.csproj", "./"]
RUN dotnet restore "LTW09.csproj"

COPY . .
RUN dotnet publish "LTW09.csproj" \
    --configuration Release \
    --no-restore \
    --output /out \
    /p:UseAppHost=false

# Runtime includes ASP.NET Core and native dependencies for SQLite.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ASPNETCORE_HTTPS_PORT=443 \
    ConnectionStrings__FlowerShop="Data Source=/app/App_Data/FlowerShop.db;Foreign Keys=True" \
    DATA_PROTECTION_KEYS_PATH=/app/App_Data/keys

COPY --from=build /out .
RUN mkdir -p /app/App_Data/keys

# Render provides PORT at runtime. Local docker run defaults to 8080.
EXPOSE 8080
CMD ["/bin/sh", "-c", "exec dotnet LTW09.dll --urls \"http://0.0.0.0:${PORT:-8080}\""]
