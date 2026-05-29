# Distributed Job Queue System

A distributed job queue system built with C# .NET 9 and Docker. Features priority-based job distribution across Worker containers using the Least-Loaded algorithm, with heartbeat monitoring, auto-scaling, and fault tolerance.

## Architecture
Browser → Queue Server (C#) → Worker Containers (Docker) → Result → Browser

## Features
- ✅ Priority-based job queue (1-10)
- ✅ Least-Loaded worker distribution algorithm
- ✅ Heartbeat monitoring every 5s
- ✅ Dead worker detection & automatic job reassignment
- ✅ Docker auto-scaling (creates workers when queue > 5 jobs)
- ✅ Real-time frontend dashboard with live status updates
- ✅ Python & C# code execution with 10s timeout
- ✅ Fault tolerance - no jobs lost
- ✅ Stress tested with 30 concurrent jobs

## Tech Stack
- **Queue Server:** C# ASP.NET Core 9
- **Workers:** Docker containers (.NET 9 + Python 3)
- **Frontend:** HTML/CSS/JavaScript
- **Orchestration:** Docker Compose

## Project Structure
distributed-job-queue/
├── QueueServer/          # ASP.NET Core Web API
│   ├── Controllers/      # JobsController
│   ├── Models/           # Job, WorkerInfo
│   ├── Services/         # JobQueueService, DispatcherService
│   └── wwwroot/          # Frontend (index.html)
├── Worker/               # Worker Service
│   ├── Controllers/      # ExecuteController
│   ├── Models/           # Job
│   └── Services/         # CodeExecutor, HeartbeatService, JobProcessorService
├── Dockerfile.worker
├── docker-compose.yml
└── distributed-job-queue.sln

## Requirements
- Docker Desktop
- .NET 9 SDK (for local development)

## Setup & Run
```bash
# Clone the repository
git clone https://github.com/USERNAME/distributed-job-queue.git
cd distributed-job-queue

# Build and run
docker-compose build
docker-compose up

# Open browser
http://localhost:5000
```

## How It Works
1. User submits a job (code + language + priority) from the frontend
2. Queue Server stores the job in a priority queue
3. Dispatcher selects the least-loaded worker (Least-Loaded algorithm)
4. Worker executes the code with a 10s timeout
5. Result is sent back to Queue Server
6. Frontend updates in real-time

## Fault Tolerance
- Workers send heartbeat every 5s to Queue Server
- If a worker dies (no heartbeat for 15s), jobs are automatically reassigned
- Auto-scaling creates new worker containers when queue exceeds 5 pending jobs
