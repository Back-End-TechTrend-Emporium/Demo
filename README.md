# Tech Trend Emporium

**Authors:**
- Andrés Poveda
- Carlos Peña  
- Nicolás Quintana

---

## Description

E-commerce platform where technology meets fashion. Integrates FakeStore API to populate products/categories and offers dynamic authentication, catalog with filters, reviews, wishlist, shopping cart with coupons and a CI/CD flow with Trunk-Based strategy (everything to main via PR + approvals).

## Architecture

**Technology Stack:**
- **.NET 8** - Backend APIs and services
- **Entity Framework Core** - ORM with MySQL
- **Docker** - Containerization
- **AWS ECS Fargate** - Deployment
- **GitHub Actions** - CI/CD

**Key Features:**
- Microservices with logical schema separation
- JWT authentication with sessions
- Approval system for products/categories
- FakeStore API integration for seeding
- Shopping cart with coupon system
- Product reviews and wishlist

## Project Structure

```
├── src/TechTrendEmporium.Api/     # Main API
├── Data/                          # Entities and DbContext
├── Logica/                        # Services and repositories
├── docs/                          # Project documentation
└── Dockerfile                     # Containerization
```

## Quick Start

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- SQL Server/MySQL (local or remote)

### Local Setup

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd TechTrend-Emporium.Backend
   ```

2. **Configure connection string:**
   ```bash
   # In appsettings.Development.json or environment variables
   "ConnectionStrings": {
     "DefaultConnection": "your-connection-string-here"
   }
   ```

3. **Run migrations:**
   ```bash
   dotnet ef database update --project Data
   ```

4. **Run the application:**
   ```bash
   dotnet run --project src/TechTrendEmporium.Api
   ```

5. **Access Swagger UI:**
   - Development: `https://localhost:7089/swagger`
   - Health check: `https://localhost:7089/health`

### Docker

```bash
# Build image
docker build -t techtrend-emporium .

# Run container
docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="your-connection-string" techtrend-emporium
```

## Documentation

Complete project documentation is located in the [`docs/`](./docs/) folder:

### 📋 Main Documentation
- **[Architecture](./docs/ARQUITECTURE.md)** - System design, components and data model
- **[Branch Strategy](./docs/BRANCH.md)** - Trunk-Based Development and PR workflow
- **[CI/CD](./docs/CI-CD.md)** - Integration and continuous deployment pipelines

### 📁 Specialized Documentation
- **[Architecture Decisions (ADRs)](./docs/decisions/)** - Record of important technical decisions
- **[Feature Specifications](./docs/features/)** - Detailed functionality documentation

### 🎯 Quick Links
- [System Overview](./docs/ARQUITECTURE.md#system-overview) - General system diagram
- [Data Model](./docs/ARQUITECTURE.md#data-model-logical) - Entities and relationships
- [Development Workflow](./docs/BRANCH.md#pull-request-workflow) - Contribution process
- [CI/CD Pipelines](./docs/CI-CD.md#continuous-integration-on-feature-bugfix-hotfix-branches-f02) - Automation details

## Development

### Branch Strategy

This project follows **Trunk-Based Development**:

- ✅ Main branch: `main` (protected)
- ✅ Work branches: `feature/*`, `bugfix/*`, `hotfix/*`
- ✅ All changes via Pull Request with 2+ approvals
- ✅ Automatic CI on each push
- ✅ Automatic CD on merge to `main`

See complete details in [BRANCH.md](./docs/BRANCH.md).

### Contributing

1. **Create branch from main:**
   ```bash
   git checkout main && git pull origin main
   git checkout -b feature/my-new-functionality
   ```

2. **Develop and commit changes**

3. **Open Pull Request to `main`:**
   - Include clear description of the change
   - Reference related issues
   - Update documentation if necessary

4. **Review process:** Requires 2+ approvals and green checks

See complete process in [docs/README.md](./docs/README.md#contributions).

## API Endpoints

### Main Endpoints
- `GET /health` - Health check
- `POST /api/users` - Create user
- `GET /api/users` - List users
- `GET /api/users/{id}` - Get user by ID

> 📖 **Complete documentation:** See Swagger UI in development or review [ARQUITECTURE.md](./docs/ARQUITECTURE.md#services--repos) for service details.

## Database

### Main Entities
- **User** - System users (Admin, Employee, Shopper)
- **Product** - Products with approval system
- **Category** - Categories with approval states
- **Cart/CartItem** - Shopping cart with coupons
- **Review** - Product reviews
- **Wishlist** - Wishlist

### Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project Data

# Apply migrations
dotnet ef database update --project Data
```

See complete model in [ARQUITECTURE.md](./docs/ARQUITECTURE.md#data-model-logical).

## Deployment

### CI/CD Pipeline

The project uses **GitHub Actions** for automation:

1. **CI on feature branches:** Build → Test → Docker Image → Push to GHCR
2. **CI on PRs:** Validation before merge
3. **CD on main:** Automatic deploy to AWS ECS Fargate

Each Docker image is tagged with the **commit SHA** for complete traceability.

See details in [CI-CD.md](./docs/CI-CD.md).

### Environments

- **Development:** Local with Swagger enabled
- **Production:** AWS ECS Fargate with health checks

## Architecture Decisions

Important technical decisions are documented as ADRs:

- [**Containerization and Database Choice**](./docs/decisions/ADR.md) - Why Docker + MySQL + commit SHA tagging

## Support

- **Issues:** Use GitHub Issues to report bugs or request features
- **Documentation:** Review the [`docs/`](./docs/) folder for detailed information
- **Contributions:** Follow the [PR workflow](./docs/BRANCH.md#pull-request-workflow)

---

> 📚 **More information:** Review [`docs/README.md`](./docs/README.md) for the complete project documentation index.
