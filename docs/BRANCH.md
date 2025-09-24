# Branching Strategy – Trunk-Based Development

This project follows a **Trunk-Based Development** approach: all changes integrate into the protected `main` branch exclusively through Pull Requests.

---

## Main Branch
- **Branch:** `main`
- **Protection:**  
  - Direct pushes are forbidden.  
  - Only Pull Requests are allowed.  
  - Requires **at least 2 approvals**.  
  - All required checks (CI build, tests, lint, security scans) must pass.  
  - All review conversations must be resolved.  

---

## Working Branches
- **Feature branches:** `feature/*` — new functionality.  
- **Bugfix branches:** `bugfix/*` — non-critical fixes.  
- **Hotfix branches:** `hotfix/*` — urgent production fixes.  

> Branches should be **short-lived** (days, not weeks) to minimize drift from `main`.  

---

## Continuous Integration
- Every `push` to a `feature/*`, `bugfix/*`, or `hotfix/*` branch triggers CI.  
- CI runs build, unit tests, linting, and security scans.  
- A Docker image is published with the **commit SHA** as its tag (e.g., `service:abc1234`).  

---

## Pull Request Workflow
1. **Create a branch from `main`:**
   ```bash
   git checkout main
   git pull origin main
   git checkout -b feature/short-description
````

2. **Commit and push your changes:**
   CI will automatically run on the branch.

3. **Open a Pull Request targeting `main`:**

   * Fill out the PR checklist.
   * Link to the relevant ADR or feature documentation.

4. **Review process:**

   * Requires **2 approvals**.
   * All checks must be green.
   * All review comments resolved.

5. **Merge:**

   * Allowed strategies: **Squash** or **Merge commit** (depending on repository policy).
   * Never rebase/force push on shared branches.

---

## Notes

* Keep feature branches **focused and small**. Large features should be broken down into incremental PRs.
* Always update or add documentation (`/docs`, ADRs, feature specs) as part of the PR.
* Hotfix branches should target `main` directly and follow the same PR/approval process.
