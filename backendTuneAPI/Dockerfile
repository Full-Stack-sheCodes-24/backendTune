# Base runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG APP_UID=1001
RUN adduser --disabled-password --uid $APP_UID appuser
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MoodzApi.csproj", "."]
RUN dotnet restore "./MoodzApi.csproj"
COPY . .
RUN dotnet build "./MoodzApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MoodzApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "MoodzApi.dll"]