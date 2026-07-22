# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ApparelShop.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create directory for SQLite data persistence
RUN mkdir -p /app/data

COPY --from=build /app/publish .

# Let the platform set the PORT env var, fallback to 80
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 80

ENTRYPOINT ["dotnet", "ApparelShop.dll"]
