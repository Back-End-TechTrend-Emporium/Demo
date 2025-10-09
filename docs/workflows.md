# CI/CD Workflows 


---

## 1) General Overview (end-to-end)

```mermaid
flowchart LR

  %% Classes
  classDef ok fill:#e7f7ee,stroke:#2f855a,color:#1b4332,stroke-width:1px
  classDef warn fill:#fff4e6,stroke:#d97706,color:#7c2d12,stroke-width:1px
  classDef fail fill:#fde8e8,stroke:#dc2626,color:#7f1d1d,stroke-width:1px
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px

  %% CI for branches
  A[Push to feature, feat, bugfix, hotfix] --> B[Workflow: ci-feature]
  B --> C[Build and Test .NET with coverage]
  C --> D{Dockerfile in root?}
  D -- Yes --> E[Setup Buildx and Login GHCR]
  E --> F[Build Docker image tag = commit SHA]
  F --> G[Push to GHCR repo:SHA]
  G --> H[Publish summary]
  D -- No --> X[Error: Dockerfile not found]

  %% CI for PR to main
  I[Pull Request to main] --> J[Workflow: ci]
  J --> K[Checkout and setup .NET]
  K --> L[Restore]
  L --> M[Build Release]
  M --> N[Test with coverage]

  %% CD after merge
  O[PR closed] --> P{Merged?}
  P -- No --> R[Ends]
  P -- Yes --> Q[Workflow: cd-on-merge]
  Q --> S[Checkout merge commit or PR head]
  S --> T[Restore Build Test]
  T --> U[Publish to zip]
  U --> V[Azure login OIDC]
  V --> W[Ensure webapp extension]
  W --> Y[App settings Prod and migrations]
  Y --> Z[Zip Deploy to Azure Web App]
  Z --> AA[Health check /health]
  AA --> AB[Summary]

  %% Class assignment
  class A,I ok
  class O warn
  class X fail
  class B,C,D,E,F,G,H,J,K,L,M,N,Q,S,T,U,V,W,Y,Z,AA,AB step
```

---

## 2) Detail: ci-feature (feature/hotfix/bugfix branches)

```mermaid
flowchart TD
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px
  classDef io fill:#e7f7ee,stroke:#2f855a,color:#1b4332,stroke-width:1px
  classDef fail fill:#fde8e8,stroke:#dc2626,color:#7f1d1d,stroke-width:1px

  A[Push to work branches] --> B[Checkout fetch-depth 0]
  B --> C[setup-dotnet 8.0.x]
  C --> D[dotnet restore]
  D --> E[dotnet build Release]
  E --> F[dotnet test with coverage]
  F --> G{Dockerfile exists?}
  G -- No --> X[Error]
  G -- Yes --> H[setup-buildx]
  H --> I[login ghcr.io]
  I --> J[build image tag = SHA]
  J --> K[publish to ghcr.io owner repo SHA]
  K --> L[Summary in GITHUB_STEP_SUMMARY]

  class A io
  class B,C,D,E,F,G,H,I,J,K,L step
  class X fail
```

---

## 3) Detail: ci (PR to main)

```mermaid
flowchart TD
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px
  classDef io fill:#e7f7ee,stroke:#2f855a,color:#1b4332,stroke-width:1px

  A[Pull Request to main] --> B[Checkout]
  B --> C[setup-dotnet 8.0.x]
  C --> D[dotnet restore]
  D --> E[dotnet build Release]
  E --> F[dotnet test with coverage]

  class A io
  class B,C,D,E,F step
```

---

## 4) Detail: cd-on-merge (deploy to Azure after merge)

```mermaid
flowchart TD
  classDef step fill:#eef2ff,stroke:#3730a3,color:#111827,stroke-width:1px
  classDef warn fill:#fff4e6,stroke:#d97706,color:#7c2d12,stroke-width:1px

  A[PR closed] --> B{Merged == true?}
  B -- No --> R[Ends]
  B -- Yes --> C[Checkout merge commit or PR head]
  C --> D[setup-dotnet 8.0.x]
  D --> E[restore build test]
  E --> F[dotnet publish output zip]
  F --> G[azure login OIDC]
  G --> H[az extension add webapp]
  H --> I[Configure app settings]
  I --> J[az webapp deploy type zip]
  J --> K[curl health with retries]
  K --> L[Summary]

  class A warn
  class B,C,D,E,F,G,H,I,J,K,L step
```

---

## 5) Sequence (actors and artifacts)

```mermaid
sequenceDiagram
  autonumber
  participant Dev as Developer
  participant GA as GitHub Actions
  participant GHCR as GH Container Registry
  participant Azure as Azure Web App

  Dev->>GA: Push to feature branches
  GA->>GA: ci-feature build test
  GA->>GHCR: Docker push with SHA tag

  Dev->>GA: Open PR to main
  GA->>GA: ci build test

  Dev->>GA: Merge PR
  GA->>Azure: OIDC login and Zip Deploy
  Azure-->>GA: OK health response
