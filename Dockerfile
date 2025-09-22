# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY src/TechTrendEmporium.Api/*.csproj ./src/TechTrendEmporium.Api/
RUN dotnet restore src/TechTrendEmporium.Api/TechTrendEmporium.Api.csproj

# Copy everything else and build
COPY . .
WORKDIR /app/src/TechTrendEmporium.Api
RUN dotnet publish -c Release -o out

# Use the official .NET 8 runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/src/TechTrendEmporium.Api/out ./

# Expose port 8080 (default for .NET apps in containers)
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "TechTrendEmporium.Api.dll"]