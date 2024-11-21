# Base runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_SECRETS=C:\Users\human being\AppData\Roaming\Microsoft\UserSecrets\ce367f95-58c1-4a16-b146-e1f6a0015c05
WORKDIR /src
COPY ["MoodzApi.csproj", "."]
RUN dotnet restore "./MoodzApi.csproj"
COPY . .
RUN dotnet build "./MoodzApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MoodzApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish

# Final runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "MoodzApi.dll"]