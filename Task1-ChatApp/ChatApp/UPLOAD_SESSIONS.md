# How to Upload Session from Folder

This guide explains how to upload chat session data from a folder to your Docker containers or registry.

## Prerequisites

- Docker Desktop installed and running
- `docker-compose.yml` in your project root
- Session data folder ready (JSON, CSV, logs, or any format)

---

## Method 1: Copy Session Data Into Running Container

### Using docker cp (for a running container)

```bash
# Copy a session folder into the running chat-app container
docker cp ./my-sessions chatapp-instance:/app/sessions

# Verify it was copied
docker exec chatapp-instance ls -la /app/sessions
```

**Example:**
```bash
# Copy a single file
docker cp ./session.json chatapp-instance:/app/data/session.json

# Copy an entire directory
docker cp ./chat-logs chatapp-instance:/app/logs
```

---

## Method 2: Mount Session Folder with docker-compose

Add a bind mount to your `docker-compose.yml` to make session data available:

### Updated docker-compose.yml

```yaml
version: '3.9'

services:
  rabbitmq:
    image: rabbitmq:3.13-management
    container_name: chatapp-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq

  chat-app:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: chatapp-instance
    environment:
      - RABBITMQ_HOST=rabbitmq
      - RABBITMQ_PORT=5672
      - RABBITMQ_USER=guest
      - RABBITMQ_PASS=guest
    depends_on:
      rabbitmq:
        condition: service_healthy
    stdin_open: true
    tty: true
    command: Tia rabbitmq general
    # ADD THIS: Mount your session folder
    volumes:
      - ./my-sessions:/app/sessions
      - ./logs:/app/logs

volumes:
  rabbitmq-data:
```

### Start with mounted volume

```bash
# Create the session folder if it doesn't exist
mkdir -p my-sessions logs

# Start compose with the mounted folders
docker compose up

# Inside the container, session data is accessible at /app/sessions
docker exec chatapp-instance ls -la /app/sessions
```

---

## Method 3: Build Session Data into Docker Image

Create a Dockerfile that includes session data:

### Dockerfile with Sessions

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

COPY ChatApp.csproj .
RUN dotnet restore ChatApp.csproj

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine

WORKDIR /app

COPY --from=builder /app/publish .

# Copy session data into the image
COPY ./my-sessions /app/sessions

# Optional: Set environment variable pointing to sessions
ENV SESSIONS_PATH=/app/sessions

ENTRYPOINT ["dotnet", "ChatApp.dll"]
```

### Build and run

```bash
docker build -t chatapp:with-sessions .
docker run -it chatapp:with-sessions Tia rabbitmq general
```

---

## Method 4: Upload to Docker Hub/Registry with Sessions

If you want to persist session data in your registry:

### Push to Docker Hub

```bash
# Build the image with sessions
docker build -t yourusername/chatapp:v1-with-sessions .

# Push to Docker Hub
docker login
docker push yourusername/chatapp:v1-with-sessions

# Pull and run on another machine
docker pull yourusername/chatapp:v1-with-sessions
docker run -it yourusername/chatapp:v1-with-sessions Tia rabbitmq general
```

---

## Method 5: Load Session Data via Volume

### Create a volume and populate it

```bash
# Create a named volume
docker volume create chat-sessions-volume

# Find where Docker stores the volume
docker volume inspect chat-sessions-volume

# Copy files into the volume (using a temporary container)
docker run -v chat-sessions-volume:/data -v ./my-sessions:/source busybox cp -r /source/. /data/

# Use the volume in docker-compose
```

### Updated docker-compose.yml with named volume

```yaml
services:
  chat-app:
    volumes:
      - chat-sessions-volume:/app/sessions

volumes:
  chat-sessions-volume:
```

---

## Method 6: Upload Sessions via GitHub Actions

If using CI/CD, add sessions to your GitHub repository:

```bash
# 1. Add session folder to git
mkdir -p sessions
echo "session data" > sessions/session-001.json
git add sessions/
git commit -m "Add chat sessions"
git push

# 2. Sessions are automatically available in the Docker image when built
# because the Dockerfile COPY command includes them
```

---

## File Structure Examples

### Typical session folder structure

```
my-sessions/
├── session-001.json
├── session-002.json
├── chat-logs/
│   ├── 2026-03-04.log
│   └── 2026-03-05.log
└── metadata.yml
```

### In container

```
/app/
├── ChatApp.dll
├── sessions/
│   ├── session-001.json
│   ├── session-002.json
│   └── chat-logs/
└── logs/
    └── (runtime logs)
```

---

## Accessing Session Data in .NET Code

If you want your C# app to read sessions:

```csharp
// Program.cs
string sessionsPath = Environment.GetEnvironmentVariable("SESSIONS_PATH") ?? "/app/sessions";

if (Directory.Exists(sessionsPath))
{
    var sessions = Directory.GetFiles(sessionsPath, "*.json");
    Console.WriteLine($"Found {sessions.Length} session files in {sessionsPath}");
    
    foreach (var session in sessions)
    {
        Console.WriteLine($"Loading: {session}");
        // Parse and load session data
    }
}
```

---

## Quick Reference Commands

```bash
# Copy folder into running container
docker cp ./my-sessions chatapp-instance:/app/

# Mount folder via docker-compose
docker compose up  # (with volumes: in compose file)

# View files in container
docker exec chatapp-instance ls -la /app/sessions

# Download files from container
docker cp chatapp-instance:/app/sessions ./downloaded-sessions

# Check volume location
docker volume inspect chat-sessions-volume

# Clean up
docker compose down -v  # Remove volumes
```

---

## Best Practices

✅ **Do:**
- Use bind mounts (`./folder:/app/folder`) for local development
- Use named volumes for persistent data in production
- Keep session folder organized with subfolders
- Set `SESSIONS_PATH` environment variable in docker-compose
- Document session file format

❌ **Don't:**
- Store large session files in the image (increases size)
- Mix development and production sessions
- Commit sensitive data (credentials, tokens) to git
- Use relative paths in production containers

---

## Troubleshooting

### Sessions not visible in container

```bash
# Check if volume is mounted
docker inspect chatapp-instance | grep -A 5 Mounts

# Verify path exists
docker exec chatapp-instance ls -la /app/sessions
```

### Permission denied errors

```bash
# Fix permissions on host folder
chmod -R 755 ./my-sessions

# In container, check permissions
docker exec chatapp-instance ls -la /app/sessions
```

### Data lost after container restart

Use **named volumes** or **bind mounts** in `docker-compose.yml`:
```yaml
volumes:
  - ./my-sessions:/app/sessions  # Bind mount (persists)
  - chat-data:/app/data          # Named volume (persists)
```

---

## Summary

| Method | Use Case | Persistence |
|--------|----------|------------|
| `docker cp` | Quick manual uploads | Only while container running |
| Bind mount | Development, local files | Permanent (on host) |
| Named volume | Production, Docker storage | Permanent (Docker managed) |
| Build into image | Static sessions | In image, portable to registry |
| Docker Hub | Share sessions across machines | Until image is deleted |

Choose the method based on your needs. For **local development**, use bind mounts. For **production**, use named volumes or registries.
