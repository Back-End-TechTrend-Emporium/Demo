# Arquitectura

## General
- .NET service deployed on AWS (ECS Fargate).
- `SHA` versioned docker images.
- Pipelines GitHub Actions for CI/CD.

## Decisiones clave
- Look for `docs/decisions/` for decision making process (why ECS, why GHCR/ECR, etc.).

## System Overview

Mermaid at-a-glance:

```mermaid
flowchart LR
  U[Users] --> UI[Web UI]
  UI -->|JWT| APIGW(API Gateway/BFF)
  APIGW --> AUTH[Auth Svc]
  APIGW --> CAT[Catalog Svc]
  APIGW --> REV[Review Svc]
  APIGW --> WISH[Wishlist Svc]
  APIGW --> CART[Cart Svc]
  APIGW --> MOD[Moderation/Approvals]
  CAT <-- SNS/SQS --> REV
  ING[Ingestion Svc] -->|FakeStore| CAT
  subgraph Data Stores
    PG[(MySQL per svc schemas)]
    REDIS[(ElastiCache Redis)]
  end
  AUTH --> PG
  CAT --> PG
  REV --> PG
  WISH --> PG
  CART --> PG
  MOD --> PG
  AUTH --> REDIS
```

Key components: React/Next UI, ASP.NET Core APIs, ECS Fargate services, MySQL, ElastiCache Redis, SNS/SQS, API Gateway/ALB, CloudWatch/X-Ray.

---

## Architecture & Components

### Services & Repos

| Component              | Purpose                                                  | Tech          | Deployable |
| ---------------------- | -------------------------------------------------------- | ------------- | ---------- |      |
| API Gateway/BFF        | Route aggregation & authz                                | ASP.NET Core  | Yes        |
| **Auth Service**       | `/api/auth`,`/api/login`,`/api/logout`,`/api/admin/auth` | .NET 8        | Yes        |
| **Catalog Service**    | Products/Categories CRUD, list/detail                    | .NET 8        | Yes        |
| **Review Service**     | Product reviews (list/add)                               | .NET 8        | Yes        |
| **Wishlist Service**   | Wishlist CRUD                                            | .NET 8        | Yes        |
| **Cart Service**       | Cart preview, totals, coupons                            | .NET 8        | Yes        |
| **Moderation Service** | Approval jobs queue and actions                          | .NET 8        | Yes        |
| **Ingestion Service**  | FakeStore seeding/sync                                   | .NET 8 Worker | Yes        |

**Runtime (AWS):** API Gateway (HTTP API) → ALB → ECS Fargate; ECR images; MySQL; ElastiCache Redis; SNS/SQS; EventBridge schedules; Secrets Manager; CloudWatch/X-Ray.

### Data Model (logical)

**Entities**: User, Session, Category, Product, Review, WishlistItem, Cart, CartItem, Coupon, ApprovalJob, ExternalImport.
**Highlights**: Unique `email`/`username`; product `rating_rate` + `rating_count`; inventory (`inv_total`, `inv_available`); approval states (`APPROVED`,`PENDING_CREATE`,`PENDING_DELETE`,`DELETED`); one active cart per user enforced in logic.

**ER (PlantUML)**

```plantuml
@startuml
' ER-style diagram
hide methods
skinparam linetype ortho
skinparam class {
  BackgroundColor White
  ArrowColor #3C6
  BorderColor #333
}

class User {
  +id: uuid [PK]
  +email: text [UQ]
  +username: text [UQ]
  password_hash: text
  role: Role
  status: UserStatus
  created_at: timestamptz
  updated_at: timestamptz
}

enum Role {
  SUPERADMIN
  ADMIN
  EMPLOYEE
  CUSTOMER
}

enum UserStatus {
  ACTIVE
  SUSPENDED
  DELETED
}

class Session {
  +id: uuid [PK]
  +user_id: uuid [FK -> User.id]
  login_at: timestamptz
  logout_at: timestamptz
  jwt_id: text
  ip: inet
  user_agent: text
  status: SessionStatus
}

enum SessionStatus {
  ACTIVE
  LOGGED_OUT
  EXPIRED
}

class Category {
  +id: uuid [PK]
  name: text [UQ]
  state: ApprovalState
  created_by: uuid [FK -> User.id]
  approved_by: uuid [FK -> User.id]?
  created_at: timestamptz
  updated_at: timestamptz
}

class Product {
  +id: uuid [PK]
  title: text
  description: text
  price: numeric(10,2)
  image_url: text
  category_id: uuid [FK -> Category.id]
  rating_rate: numeric(3,2)
  rating_count: int
  inv_total: int
  inv_available: int
  state: ApprovalState
  created_by: uuid [FK -> User.id]
  approved_by: uuid [FK -> User.id]?
  created_at: timestamptz
  updated_at: timestamptz
}

enum ApprovalState {
  APPROVED
  PENDING_CREATE
  PENDING_DELETE
  DELETED
}

class Review {
  +id: uuid [PK]
  product_id: uuid [FK -> Product.id]
  user_id: uuid [FK -> User.id]
  rating: smallint (1..5)
  comment: text
  status: ReviewStatus
  created_at: timestamptz
}

enum ReviewStatus {
  VISIBLE
  HIDDEN
  PENDING
}

class WishlistItem {
  +user_id: uuid [FK -> User.id]
  +product_id: uuid [FK -> Product.id]
  created_at: timestamptz
}

class Cart {
  +id: uuid [PK]
  user_id: uuid [FK -> User.id]
  status: CartStatus
  coupon_id: uuid [FK -> Coupon.id]?
  total_before_discount: numeric(12,2)
  discount_amount: numeric(12,2)
  shipping_cost: numeric(12,2)
  final_total: numeric(12,2)
  created_at: timestamptz
  updated_at: timestamptz
}

enum CartStatus {
  ACTIVE
  CHECKED_OUT
  ABANDONED
}

class CartItem {
  +id: uuid [PK]
  cart_id: uuid [FK -> Cart.id]
  product_id: uuid [FK -> Product.id]
  quantity: int
  unit_price_snapshot: numeric(10,2)
  title_snapshot: text
  image_snapshot: text
  created_at: timestamptz
}

class Coupon {
  +id: uuid [PK]
  code: text [UQ]
  discount_percent: smallint
  active: boolean
  valid_from: date?
  valid_to: date?
}

class ApprovalJob {
  +id: uuid [PK]
  type: JobType
  operation: JobOperation
  target_id: uuid?
  payload: jsonb
  status: JobStatus
  submitted_by: uuid [FK -> User.id]
  decided_by: uuid [FK -> User.id]?
  created_at: timestamptz
  decided_at: timestamptz?
  message: text?
}

enum JobType { 
    PRODUCT  
    CATEGORY }
enum JobOperation { 
    CREATE 
    UPDATE 
    DELETE }
enum JobStatus { 
    PENDING  
    APPROVED  
    DECLINED }

class ExternalImport {
  +id: uuid [PK]
  source: text  ' fakestoreapi
  entity_type: text  ' product/category
  external_id: text
  mapped_product_id: uuid?
  payload: jsonb
  imported_at: timestamptz
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



### Integrations & Dependencies

* **Inbound:** FakeStore API (seed/refresh Products & Categories) via Ingestion Service (EventBridge schedule).
* **Outbound:** Email/Slack webhooks for CI/CD & deployment notifications.
* **Secrets:** AWS Secrets Manager & SSM Parameter Store. No secrets committed.

---