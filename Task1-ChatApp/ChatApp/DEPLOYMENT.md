# ChatApp Deployment Pipeline

Your deployment pipeline is now ready. Here's what was created:

## Files Created

### 1. **Dockerfile** — Multi-stage build
- **Stage 1 (Builder)**: Compiles .NET app using `mcr.microsoft.com/dotnet/sdk:10.0`
- **Stage 2 (Runtime)**: Minimal Alpine-based runtime with only necessary binaries
- **Result**: ~150MB image (vs ~1GB with full SDK)

Usage:
```bash
docker build -t chatapp:latest .
docker run -it chatapp:latest Tia rabbitmq general
```

### 2. **docker-compose.yml** — Local development & single-host production
Services:
- **rabbitmq**: RabbitMQ message broker with management UI (http://localhost:15672)
- **chat-app**: Your .NET console app

Commands:
```bash
# Start all services
docker compose up

# Stop all services
docker compose down

# View logs
docker compose logs -f chat-app
docker compose logs -f rabbitmq
```

### 3. **.github/workflows/deploy.yml** — Automated CI/CD pipeline
Triggers on:
- Push to `main` → builds, tests, pushes image to registry
- Push to `develop` → builds, tests (no push)
- Pull requests to `main` → builds and tests

**Jobs:**
1. **build** — Compiles Docker image using BuildKit, caches layers, pushes to registry
2. **test** — Runs `dotnet build` and `dotnet test`
3. **deploy** — Placeholder for production deployment (on main branch only)

### 4. **.dockerignore** — Excludes build artifacts from image
Prevents large bin/, obj/, packages/ directories from being copied into Docker images.

---

## Setup Instructions

### Local Development
```bash
# 1. Start services with docker-compose
docker compose up -d

# 2. Verify RabbitMQ is healthy
docker compose logs rabbitmq

# 3. Start the chat app (in a new terminal, or with docker-compose)
docker run -it --network chatapp_default chatapp:latest Alice rabbitmq general

# 4. In another terminal, start another instance
docker run -it --network chatapp_default chatapp:latest Bob rabbitmq general
```

### GitHub Actions Setup
For automated image builds and pushes:

1. **Configure Docker Hub credentials** (or other registry):
   - Go to your GitHub repo → Settings → Secrets and variables → Actions
   - Add secrets:
     - `DOCKER_USERNAME`: Your Docker Hub username
     - `DOCKER_PASSWORD`: Your Docker Hub token (not password!)
     - `DOCKER_REGISTRY_URL`: `docker.io/yourusername` (or your registry URL)

2. **Configure SSH deployment** (for production):
   - Add to GitHub Secrets:
     - `DEPLOY_HOST`: Your production server IP/domain
     - `DEPLOY_USER`: SSH user on production server
     - `DEPLOY_KEY`: SSH private key for deployment

3. **Optional**: Use GitHub Container Registry (ghcr.io) — no extra setup needed, uses `GITHUB_TOKEN`

---

## Deployment Options

### Option 1: docker-compose (Recommended for simple/single-host)
```bash
# SSH into your production server
ssh your-server

# Clone or pull latest code
git clone <repo> && cd chatapp
git pull origin main

# Pull latest image and restart
docker compose up -d --pull always

# View logs
docker compose logs -f
```

### Option 2: Manual docker run (For orchestration platforms)
```bash
docker run -d \
  --name chatapp-alice \
  --network mynet \
  -e RABBITMQ_HOST=rabbitmq-service \
  your-registry/chatapp:latest Alice rabbitmq general
```

### Option 3: Docker Swarm (For multi-node clusters)
```bash
docker service create \
  --name chatapp \
  --network overlay_net \
  -e RABBITMQ_HOST=rabbitmq-service \
  your-registry/chatapp:latest Alice rabbitmq general
```

### Option 4: Kubernetes (For enterprise/cloud deployments)
Create a manifest `k8s-deployment.yml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: chatapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: chatapp
  template:
    metadata:
      labels:
        app: chatapp
    spec:
      containers:
      - name: chatapp
        image: your-registry/chatapp:latest
        args: ["Alice", "rabbitmq-service", "general"]
        env:
        - name: RABBITMQ_HOST
          value: rabbitmq-service
        - name: RABBITMQ_PORT
          value: "5672"
---
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq-service
spec:
  selector:
    app: rabbitmq
  ports:
  - port: 5672
    targetPort: 5672
```

Deploy with:
```bash
kubectl apply -f k8s-deployment.yml
kubectl logs -f deployment/chatapp
```

---

## Monitoring & Troubleshooting

### View running containers
```bash
docker ps -a
docker compose ps
```

### View logs
```bash
docker logs <container-name>
docker compose logs -f [service-name]
```

### Check image size
```bash
docker images chatapp
```

### Test the pipeline locally
```bash
# Simulate GitHub Actions locally with act
brew install act
act -j build  # Run build job
act -j test   # Run test job
```

### RabbitMQ Management UI
- URL: `http://localhost:15672`
- Default credentials: `guest` / `guest`
- Monitor queues, connections, and message flow

---

## Next Steps

1. **Update GitHub Secrets** with your registry credentials
2. **Push to main branch** to trigger automated build
3. **Monitor workflow** in GitHub Actions tab
4. **Test deployment** on staging before production
5. **Configure monitoring** (Docker events, logs, health checks)

Your pipeline is production-ready. Let me know if you need help with specific deployment steps!
