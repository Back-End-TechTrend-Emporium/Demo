# Architecture

## General

* **.NET 8 Web API** (single deployable; layered architecture).
* Deployed on **Azure Container Apps** (or Azure App Service—documented in decisions).
* **Azure SQL Database** as the primary datastore.
* **Docker images versioned by commit `SHA`** (labels include `org.opencontainers.image.revision`).
* **GitHub Actions** pipelines for CI/CD with **OIDC** to Azure.
* Images stored in **Azure Container Registry (ACR)**.

## Key Decisions

See `docs/decisions/` for ADRs covering:

* Monolith-first layered architecture vs. microservices.
* Azure Container Apps vs. App Service.
* ACR vs. other registries.
* Trunk-based development and branch protection.
* JWT strategy, session recording, and approval workflow.

## System Overview

Mermaid at-a-glance:

```mermaid
flowchart LR
  U[Users] --> UI[Web UI]
  UI -->|JWT| API[Backend API (Layered .NET)]
  
  subgraph Application Layer (Use Cases)
    AUTH[Auth & Accounts Service<br/>- Signup/Login/Logout<br/>- Roles & Sessions]
    CAT[Catalog Service<br/>- Products/Categories CRUD<br/>- List/Detail/Filters]
    SHOP[Shopping Service<br/>- Wishlist<br/>- Cart + Coupons]
    APPROVE[Approval Service<br/>- Jobs Queue<br/>- Approve/Decline]
    SEED[Seed/Sync Service<br/>- FakeStore ingest]
  end

  API --> AUTH
  API --> CAT
  API --> SHOP
  API --> APPROVE

  SEED -->|Fetch| FAKE[FakeStore API]

  subgraph Infrastructure
    SQL[(Azure SQL Database)]
    REDIS[(Azure Cache for Redis):::opt]
    MON[Azure Monitor / App Insights]
  end

  classDef opt fill:#f6f6f6,stroke:#999,color:#333

  AUTH --> SQL
  CAT --> SQL
  SHOP --> SQL
  APPROVE --> SQL

  API --> MON
  API -. optional caching .-> REDIS

  FAKE --> SEED
```

**Key components:** React/Next (Web UI), .NET 8 Web API, Azure Container Apps, Azure SQL Database, Azure Cache for Redis (optional), Azure Monitor/App Insights, GitHub Actions CI/CD, ACR.

---

## Architecture & Components

### Layers & Modules (single deployable)

| Layer / Module              | Purpose                                                                               | Tech                          | Deployable                  |
| --------------------------- | ------------------------------------------------------------------------------------- | ----------------------------- | --------------------------- |
| **API Layer**               | REST controllers, input validation, DTOs, pagination, authn/z middleware              | ASP.NET Core                  | **Included in Backend API** |
| **Auth & Accounts Service** | Signup (admin/shopper), login, logout, JWT, session recording, role checks            | .NET 8                        | Included                    |
| **Catalog Service**         | Products/Categories CRUD, list/detail, category filters, product states               | .NET 8                        | Included                    |
| **Shopping Service**        | Wishlist CRUD, Cart preview/calculation, coupon application                           | .NET 8                        | Included                    |
| **Approval Service**        | Approval jobs (create/update/delete for products/categories), superadmin actions      | .NET 8                        | Included                    |
| **Seed/Sync Service**       | One-shot or scheduled ingest from **FakeStore API** to seed categories/products       | .NET 8 Worker (HostedService) | Included                    |
| **Infrastructure**          | Repositories (EF Core/Dapper), JWT provider, password hashing, notifications, caching | .NET 8                        | Included                    |

**Runtime (Azure):** Ingress → **Azure Container Apps** (single container app), image from **ACR**, data in **Azure SQL**, optional **Azure Cache for Redis**, metrics/logs in **App Insights**, config/secrets in **Azure Key Vault**.

### Branching & CI/CD (summary)

* **Trunk-based**: `main` protected (PRs only, **2 approvals**, required checks).
* \**CI (feature/* pushes)\*\*: Build, test, SAST/dependency scan, image tagged **by SHA** and pushed to **ACR**, status summary.
* **CD (merge to main)**: Build & tag (`main-<shortSHA>`), push to ACR, deploy to **Container Apps**, run DB migrations, smoke test, notify (e.g., Teams webhook or GitHub Checks).

---

## Data Model (logical)

**Entities**: `User`, `Session`, `Category`, `Product`, `Review`, `WishlistItem`, `Cart`, `CartItem`, `Coupon`, `ApprovalJob`, `ExternalImport`.

**Highlights**

* Unique **email** and **username**.
* Product ratings: `rating_rate` + `rating_count`.
* Inventory: `inv_total`, `inv_available`.
* Product/Category states: `APPROVED`, `PENDING_CREATE`, `PENDING_DELETE`, `DELETED`.
* One active cart per user enforced in logic.
* JSON payloads stored as `nvarchar(max)` with SQL Server JSON functions.

**ER (PlantUML, SQL Server types)**

```plantuml
@startuml
hide methods
skinparam linetype ortho
skinparam class {
  BackgroundColor White
  ArrowColor #3C6
  BorderColor #333
}

class User {
  +id: uniqueidentifier [PK]
  +email: nvarchar(256) [UQ]
  +username: nvarchar(64) [UQ]
  password_hash: nvarchar(255)
  role: Role
  status: UserStatus
  created_at: datetime2
  updated_at: datetime2
}

enum Role {
  SUPERADMIN
  ADMINISTRATOR
  EMPLOYEE
  SHOPPER
}

enum UserStatus {
  ACTIVE
  SUSPENDED
  DELETED
}

class Session {
  +id: uniqueidentifier [PK]
  +user_id: uniqueidentifier [FK -> User.id]
  login_at: datetime2
  logout_at: datetime2
  jwt_id: nvarchar(64)
  ip: nvarchar(45)
  user_agent: nvarchar(256)
  status: SessionStatus
}

enum SessionStatus {
  ACTIVE
  LOGGED_OUT
  EXPIRED
}

class Category {
  +id: uniqueidentifier [PK]
  name: nvarchar(100) [UQ]
  state: ApprovalState
  created_by: uniqueidentifier [FK -> User.id]
  approved_by: uniqueidentifier [FK -> User.id]?
  created_at: datetime2
  updated_at: datetime2
}

class Product {
  +id: uniqueidentifier [PK]
  title: nvarchar(200)
  description: nvarchar(max)
  price: decimal(10,2)
  image_url: nvarchar(500)
  category_id: uniqueidentifier [FK -> Category.id]
  rating_rate: decimal(3,2)
  rating_count: int
  inv_total: int
  inv_available: int
  state: ApprovalState
  created_by: uniqueidentifier [FK -> User.id]
  approved_by: uniqueidentifier [FK -> User.id]?
  created_at: datetime2
  updated_at: datetime2
}

enum ApprovalState {
  APPROVED
  PENDING_CREATE
  PENDING_DELETE
  DELETED
}

class Review {
  +id: uniqueidentifier [PK]
  product_id: uniqueidentifier [FK -> Product.id]
  user_id: uniqueidentifier [FK -> User.id]
  rating: smallint  ' 1..5 validated in logic
  comment: nvarchar(max)
  status: ReviewStatus
  created_at: datetime2
}

enum ReviewStatus {
  VISIBLE
  HIDDEN
  PENDING
}

class WishlistItem {
  +user_id: uniqueidentifier [FK -> User.id]
  +product_id: uniqueidentifier [FK -> Product.id]
  created_at: datetime2
}

class Cart {
  +id: uniqueidentifier [PK]
  user_id: uniqueidentifier [FK -> User.id]
  status: CartStatus
  coupon_id: uniqueidentifier [FK -> Coupon.id]?
  total_before_discount: decimal(12,2)
  discount_amount: decimal(12,2)
  shipping_cost: decimal(12,2)
  final_total: decimal(12,2)
  created_at: datetime2
  updated_at: datetime2
}

enum CartStatus {
  ACTIVE
  CHECKED_OUT
  ABANDONED
}

class CartItem {
  +id: uniqueidentifier [PK]
  cart_id: uniqueidentifier [FK -> Cart.id]
  product_id: uniqueidentifier [FK -> Product.id]
  quantity: int
  unit_price_snapshot: decimal(10,2)
  title_snapshot: nvarchar(200)
  image_snapshot: nvarchar(500)
  created_at: datetime2
}

class Coupon {
  +id: uniqueidentifier [PK]
  code: nvarchar(50) [UQ]
  discount_percent: smallint
  active: bit
  valid_from: date?
  valid_to: date?
}

class ApprovalJob {
  +id: uniqueidentifier [PK]
  type: JobType
  operation: JobOperation
  target_id: uniqueidentifier?
  payload: nvarchar(max)  ' JSON
  status: JobStatus
  submitted_by: uniqueidentifier [FK -> User.id]
  decided_by: uniqueidentifier [FK -> User.id]?
  created_at: datetime2
  decided_at: datetime2?
  message: nvarchar(500)?
}

enum JobType { PRODUCT, CATEGORY }
enum JobOperation { CREATE, UPDATE, DELETE }
enum JobStatus { PENDING, APPROVED, DECLINED }

class ExternalImport {
  +id: uniqueidentifier [PK]
  source: nvarchar(50)  ' fakestoreapi
  entity_type: nvarchar(50)  ' product/category
  external_id: nvarchar(100)
  mapped_product_id: uniqueidentifier?
  payload: nvarchar(max)  ' JSON
  imported_at: datetime2
}

' Relationships
User "1" -- "0..*" Session
User "1" -- "0..*" Review
Product "1" -- "0..*" Review
User "1" -- "0..*" WishlistItem
Product "1" -- "0..*" WishlistItem
User "1" -- "0..*" Cart
Cart "1" -- "1..*" CartItem
Product "1" -- "0..*" CartItem
Coupon "1" -- "0..*" Cart
Category "1" -- "0..*" Product
User "1" -- "0..*" ApprovalJob : submitted_by
User "1" -- "0..*" ApprovalJob : decided_by
Product "0..1" -- "0..*" ExternalImport : mapped_product_id
@enduml
```

---

## Integrations & Dependencies

* **Inbound**: **FakeStore API** via `Seed/Sync Service` (HostedService). Trigger on demand (admin endpoint), or scheduled with **Azure Container Apps Jobs** (documented in ADR).
* **Outbound**: Notifications (GitHub Checks summary, optional Microsoft Teams webhook) for CI/CD and deployments.
* **Secrets/Config**: **Azure Key Vault** + managed identity (no long-lived secrets in CI). Connection strings and JWT secrets pulled at startup.
* **Observability**: **App Insights** traces/metrics; health probes; structured logging (Serilog).

---

## Operational Guardrails

* **Branch protection** on `main`: PRs only, **2 approvals**, required checks (build, tests, SAST).
* **Docs freshness**: PR template requires updates to Wiki/`CHANGELOG.md`/`docs/decisions/` where applicable; CI enforces basic drift rules.
* **Migrations**: EF Core migrations executed during CD; roll-forward only in `prod`, rollback playbook documented.

---