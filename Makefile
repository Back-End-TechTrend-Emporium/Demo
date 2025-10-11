# Variables
SOLUTION = TechTrend-Emporium.Backend.sln
PROJECT_API = src/TechTrendEmporium.Api/TechTrendEmporium.Api.csproj
ENV = Development

# Default target
all: build

# Restore NuGet packages
restore:
    dotnet restore $(SOLUTION)

# Build the solution in Development
build:
    dotnet build $(SOLUTION) --configuration Debug

# Run the API project with ASPNETCORE_ENVIRONMENT=Development (compatible con Git Bash)
run:
    ASPNETCORE_ENVIRONMENT=$(ENV) dotnet run --project $(PROJECT_API)

# Run tests (if you have a test project)
test:
    dotnet test --no-build --configuration Debug

# Format code (if dotnet-format is installed)
format:
    dotnet format

# Clean build artifacts
clean:
    dotnet clean $(SOLUTION)

# Watch for changes and run the API (hot reload, compatible con Git Bash)
watch:
    ASPNETCORE_ENVIRONMENT=$(ENV) dotnet watch --project $(PROJECT_API) run

.PHONY: all restore build run test format clean watch