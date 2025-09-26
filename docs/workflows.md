# Workflows CI/CD 


---

## 1) Vista general (end-to-end)

```mermaid
flowchart LR

  %% Clases
  classDef ok fill:#e7f7ee,stroke:#2f855a,color:#1b4332,stroke-width:1px
  classDef warn fill:#fff4e6,stroke:#d97706,color:#7c2d12,stroke-width:1px
  classDef fail fill:#fde8e8,stroke:#dc2626,color:#7f1d1d,stroke-width:1px
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px

  %% CI para ramas
  A[Push a feature, feat, bugfix, hotfix] --> B[Workflow: ci-feature]
  B --> C[Build y Test .NET con coverage]
  C --> D{Dockerfile en raiz?}
  D -- Si --> E[Setup Buildx y Login GHCR]
  E --> F[Build imagen Docker tag = commit SHA]
  F --> G[Push a GHCR repo:SHA]
  G --> H[Publicar resumen]
  D -- No --> X[Error: Dockerfile no encontrado]

  %% CI para PR a main
  I[Pull Request hacia main] --> J[Workflow: ci]
  J --> K[Checkout y setup .NET]
  K --> L[Restore]
  L --> M[Build Release]
  M --> N[Test con coverage]

  %% CD tras merge
  O[PR cerrado] --> P{Merged?}
  P -- No --> R[Termina]
  P -- Si --> Q[Workflow: cd-on-merge]
  Q --> S[Checkout del commit merge o head del PR]
  S --> T[Restore Build Test]
  T --> U[Publish a zip]
  U --> V[Azure login OIDC]
  V --> W[Asegurar extension webapp]
  W --> Y[App settings Prod y migrations]
  Y --> Z[Zip Deploy a Azure Web App]
  Z --> AA[Health check /health]
  AA --> AB[Summary]

  %% Asignación de clases
  class A,I ok
  class O warn
  class X fail
  class B,C,D,E,F,G,H,J,K,L,M,N,Q,S,T,U,V,W,Y,Z,AA,AB step
```

---

## 2) Detalle: ci-feature (ramas de feature/hotfix/bugfix)

```mermaid
flowchart TD
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px
  classDef io fill:#e7f7ee,stroke:#2f855a,color:#1b4332,stroke-width:1px
  classDef fail fill:#fde8e8,stroke:#dc2626,color:#7f1d1d,stroke-width:1px

  A[Push a ramas de trabajo] --> B[Checkout fetch-depth 0]
  B --> C[setup-dotnet 8.0.x]
  C --> D[dotnet restore]
  D --> E[dotnet build Release]
  E --> F[dotnet test con coverage]
  F --> G{Existe Dockerfile?}
  G -- No --> X[Error]
  G -- Si --> H[setup-buildx]
  H --> I[login ghcr.io]
  I --> J[build image tag = SHA]
  J --> K[publish a ghcr.io owner repo SHA]
  K --> L[Resumen en GITHUB_STEP_SUMMARY]

  class A io
  class B,C,D,E,F,G,H,I,J,K,L step
  class X fail
```

---

## 3) Detalle: ci (PR hacia main)

```mermaid
flowchart TD
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px
  classDef io fill:#e7f7ee,stroke:#2f855a,color:#1b4332,stroke-width:1px

  A[Pull Request hacia main] --> B[Checkout]
  B --> C[setup-dotnet 8.0.x]
  C --> D[dotnet restore]
  D --> E[dotnet build Release]
  E --> F[dotnet test con coverage]

  class A io
  class B,C,D,E,F step
```

---

## 4) Detalle: cd-on-merge (deploy a Azure tras merge)

```mermaid
flowchart TD
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px
  classDef warn fill:#fff4e6,stroke:#d97706,color:#7c2d12,stroke-width:1px

  A[PR cerrado] --> B{Merged == true?}
  B -- No --> R[Termina]
  B -- Si --> C[Checkout merge commit o head del PR]
  C --> D[setup-dotnet 8.0.x]
  D --> E[restore build test]
  E --> F[dotnet publish output zip]
  F --> G[azure login OIDC]
  G --> H[az extension add webapp]
  H --> I[Configurar app settings]
  I --> J[az webapp deploy type zip]
  J --> K[curl health con reintentos]
  K --> L[Summary]

  class A warn
  class B,C,D,E,F,G,H,I,J,K,L step
```

---

## 5) Secuencia (actores y artefactos)

```mermaid
sequenceDiagram
  autonumber
  participant Dev as Developer
  participant GA as GitHub Actions
  participant GHCR as GH Container Registry
  participant Azure as Azure Web App

  Dev->>GA: Push a feature ramas
  GA->>GA: ci-feature build test
  GA->>GHCR: Docker push con tag SHA

  Dev->>GA: Abrir PR a main
  GA->>GA: ci build test

  Dev->>GA: Merge PR
  GA->>Azure: OIDC login y Zip Deploy
  Azure-->>GA: Respuesta OK health
```
