# ChatApp Container Commands

## Quick Start (Recommended)

```bash
docker compose up
```

---

## Individual Commands

### Build the Docker Image

```bash
docker build -t chatapp .
```

### Start RabbitMQ Only

```bash
docker run -d --name chatapp-rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:3.13-management
```

### Run ChatApp Container (Single Instance)

```bash
docker run -it --name chatapp-instance \
  --link chatapp-rabbitmq:rabbitmq \
  -e RABBITMQ_HOST=rabbitmq \
  -e RABBITMQ_PORT=5672 \
  -e RABBITMQ_USER=guest \
  -e RABBITMQ_PASS=guest \
  chatapp Tia rabbitmq general
```

### Run ChatApp with Multiple Users (Separate Containers)

**User 1 (Alice):**
```bash
docker run -it --name chatapp-alice \
  --link chatapp-rabbitmq:rabbitmq \
  -e RABBITMQ_HOST=rabbitmq \
  -e RABBITMQ_PORT=5672 \
  -e RABBITMQ_USER=guest \
  -e RABBITMQ_PASS=guest \
  chatapp Alice rabbitmq general
```

**User 2 (Bob):**
```bash
docker run -it --name chatapp-bob \
  --link chatapp-rabbitmq:rabbitmq \
  -e RABBITMQ_HOST=rabbitmq \
  -e RABBITMQ_PORT=5672 \
  -e RABBITMQ_USER=guest \
  -e RABBITMQ_PASS=guest \
  chatapp Bob rabbitmq general
```

**User 3 (Charlie):**
```bash
docker run -it --name chatapp-charlie \
  --link chatapp-rabbitmq:rabbitmq \
  -e RABBITMQ_HOST=rabbitmq \
  -e RABBITMQ_PORT=5672 \
  -e RABBITMQ_USER=guest \
  -e RABBITMQ_PASS=guest \
  chatapp Charlie rabbitmq general
```

---

## Docker Compose Commands

### Start All Services (Build + Run)

```bash
docker compose up
```

### Start in Detached Mode (Background)

```bash
docker compose up -d
```

### Rebuild Images and Start

```bash
docker compose up --build
```

### Stop All Services

```bash
docker compose stop
```

### Stop and Remove All Services

```bash
docker compose down
```

### Stop and Remove All Services + Volumes

```bash
docker compose down -v
```

### View Logs

```bash
docker compose logs -f
```

### View Logs for Specific Service

```bash
docker compose logs -f chat-app
docker compose logs -f rabbitmq
```

---

## Container Management Commands

### List Running Containers

```bash
docker ps
```

### List All Containers (Including Stopped)

```bash
docker ps -a
```

### View Container Logs

```bash
docker logs chatapp-instance
docker logs chatapp-rabbitmq
```

### Follow Container Logs in Real-Time

```bash
docker logs -f chatapp-instance
```

### Execute Command in Running Container

```bash
docker exec -it chatapp-instance /bin/sh
```

### Stop a Running Container

```bash
docker stop chatapp-instance
```

### Remove a Container

```bash
docker rm chatapp-instance
```

### Remove All Stopped Containers

```bash
docker container prune
```

---

## Image Management Commands

### List Images

```bash
docker images
```

### Remove an Image

```bash
docker rmi chatapp
```

### Remove Unused Images

```bash
docker image prune
```

### Tag an Image

```bash
docker tag chatapp:latest myregistry/chatapp:v1.0
```

### Push Image to Registry

```bash
docker push myregistry/chatapp:v1.0
```

---

## Network Commands

### List Docker Networks

```bash
docker network ls
```

### Inspect a Network

```bash
docker network inspect bridge
```

### Create a Custom Network

```bash
docker network create chatapp-network
```

---

## Troubleshooting Commands

### Check Docker System Status

```bash
docker system df
```

### Clean Up Unused Resources

```bash
docker system prune
```

### Inspect Container Details

```bash
docker inspect chatapp-instance
```

### Check Container Stats (CPU, Memory)

```bash
docker stats
```

### Check RabbitMQ Management UI

Open in browser: `http://localhost:15672`
- Username: `guest`
- Password: `guest`

---

## Full Workflow Example

```bash
# 1. Build the image
docker build -t chatapp .

# 2. Start RabbitMQ
docker run -d --name chatapp-rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:3.13-management

# 3. Wait a few seconds for RabbitMQ to be healthy

# 4. Start ChatApp instances
docker run -it --name chatapp-alice \
  --link chatapp-rabbitmq:rabbitmq \
  -e RABBITMQ_HOST=rabbitmq \
  -e RABBITMQ_PORT=5672 \
  -e RABBITMQ_USER=guest \
  -e RABBITMQ_PASS=guest \
  chatapp Alice rabbitmq general

# 5. In another terminal, start another user
docker run -it --name chatapp-bob \
  --link chatapp-rabbitmq:rabbitmq \
  -e RABBITMQ_HOST=rabbitmq \
  -e RABBITMQ_PORT=5672 \
  -e RABBITMQ_USER=guest \
  -e RABBITMQ_PASS=guest \
  chatapp Bob rabbitmq general

# 6. Chat between terminals

# 7. Clean up (Ctrl+C to exit chat, then run):
docker stop chatapp-alice chatapp-bob chatapp-rabbitmq
docker rm chatapp-alice chatapp-bob chatapp-rabbitmq
```
