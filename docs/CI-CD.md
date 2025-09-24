# CI/CD

This project uses **GitHub Actions** pipelines to enforce Continuous Integration (CI) on every branch and Continuous Deployment (CD) on the protected `main` branch.  
The pipelines ensure that every Docker image can be traced back to the exact Git commit that produced it.

---

## Continuous Integration on `feature/*`, `bugfix/*`, `hotfix/*` branches (F02)

- **Build & Test:**  
  - Compile and run all unit/integration tests (.NET 8).  

- **Docker Image Build:**  
  - Build a Docker image tagged with the commit SHA (`<service>:<sha>`).  
  - Example: `ghcr.io/<owner>/<repo>:abc1234`.  

- **Image Publish:**  
  - Push image to GitHub Container Registry (GHCR).  
  - Push to Amazon ECR if AWS integration is enabled.  

- **CI Summary:**  
  - Post build/test results in the *GitHub Step Summary*.  
  - Notifications can be extended to Teams (optional).  

---

## Continuous Integration on Pull Requests to `main`

- Every PR targeting `main` repeats the **build + tests** pipeline.  
- Acts as a **required status check** before merge.  
- Ensures that no code is merged without passing CI.  

---

## Continuous Deployment on `main` (F03)

Triggered automatically on a **push to `main`** (which only occurs via approved PR merge):

1. **Rebuild & Publish Image:**  
   - Build Docker image tagged with the commit SHA.  
   - Push image to GHCR/ECR.  

2. **Deploy to AWS:**  
   - Update the ECS Fargate service with the new image.  
   - Use forced deployment to replace old tasks with new ones.  
   - Wait until ECS reports service stability.  

3. **Publish Results:**  
   - Deployment status is shown in GitHub Actions logs.  
   - Optionally notify team channels (email).  

---

## Conventions

- **Image Tagging:**  
  - `image = <sha>` ensures a 1:1 trace between code, Docker image, and deployment.  
  - This guarantees reproducibility and rollback accuracy.  

- **Environment Variables / Secrets:**  
  - `GITHUB_TOKEN` → for GHCR authentication (no extra secrets required).  
  - `AWS_ROLE_TO_ASSUME`, `AWS_REGION` → for AWS deployment via OIDC.  
  - Optional: `SLACK_WEBHOOK_URL` or similar for notifications.  

---

## Observability & Traceability

- **Pipeline Logs:**  
  - All logs available in the *Actions* tab of GitHub.  
  - Failed steps show detailed error messages.  

- **Releases Tracking:**  
  - Each deployment is recorded in [`RELEASES.md`](./RELEASES.md).  
  - Documented with the commit SHA and corresponding Docker image tag.  

- **Monitoring:**  
  - ECS service stability checked during deployment.  
  - Application logs and metrics available in AWS CloudWatch/X-Ray.  

---

## Future Improvements

- Add **security scans** .  
- Add **end-to-end test stage** for staging before production rollout.    
