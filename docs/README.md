# Tech Trend Emporium — Project Documentation

This repository follows a Trunk-Based Development strategy: all work branches off from main and merges back exclusively via Pull Requests.
Pull Requests must pass automated checks and be reviewed before merging.

## Index

Architecture

Branching Strategy (Trunk-Based)

CI/CD Pipelines

Decision Records (ADRs)

Feature Specifications

Releases

Contributing to Documentation

## Key Policies

Functional changes require updated documentation
Every change in functionality must include corresponding documentation updates, or a justification in the Pull Request.

All significant decisions are logged as ADRs
Use lightweight Architecture Decision Records (ADRs) in docs/decisions/ to capture the “why” behind decisions.

Every release is documented
Add each release to RELEASES.md, including a reference to the Git commit and the corresponding Docker image tag.

## Complementary Guidelines

Code → Docs Alignment
Keep /docs aligned with the actual code and APIs. If an endpoint, data model, or workflow changes, update both the OpenAPI spec and the relevant documentation section.

## Branch Protection Rules

main branch is protected.

Direct pushes are forbidden.

Pull Requests require at least 2 approvals.

All CI checks must pass before merge.

## CI/CD Overview

Feature branches: run build, tests, and Docker image build with tag = commit SHA.

main merges: trigger automatic deployment to AWS (ECS Fargate), with rollout status notifications.

Docs lint ensures /docs and Wiki remain up to date.

## Documentation Style

Diagrams as code: PlantUML or Mermaid.

Decision log style: ADRs (short, focused, one per decision).

Feature specs: placed under ./features, matching acceptance criteria.

## Contributions

Follow the contribution guide in CONTRIBUTING.md
.

Open PRs with linked issues when possible.

Add ADRs for decisions that impact architecture, security, or team practices.