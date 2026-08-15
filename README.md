# Student Registration Microservices

A microservices demo built to learn and practice correct microservice architecture patterns: independent services, database-per-service, service-to-service communication, resilience, an API Gateway, load balancing, and asynchronous messaging.

## Architecture

```
                          ┌───────────────────┐
                Client ──▶│    API Gateway     │  (YARP, port 5000)
                          │  routes by path    │
                          └─────────┬──────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │                           │                           │
        ▼                           ▼                           ▼
┌───────────────┐          ┌─────────────────┐         ┌──────────────────────┐
│ Student Service│          │  Course Service  │         │  Enrollment Service   │
│ (2 instances,  │          │   (port 5002)    │         │     (port 5003)      │
│ load balanced) │          │                  │         │                      │
│ StudentServiceDb│         │  CourseServiceDb │         │  EnrollmentServiceDb │
└───────▲────────┘          └────────▲─────────┘         └───────────┬──────────┘
        │                            │                               │
        │        GET /students/{id} via gateway   GET /courses/{id} via gateway
        └────────────────────────────┴───────────────────────────────┘
                                                                       │
                                                        publishes "EnrollmentCreated"
                                                                       ▼
                                                              ┌───────────────┐
                                                              │   RabbitMQ    │
                                                              │ enrollment-   │
                                                              │ created queue │
                                                              └───────┬───────┘
                                                                      ▼
                                                          ┌───────────────────────┐
                                                          │  Notification Service  │
                                                          │  (consumes & logs)     │
                                                          └───────────────────────┘
```

**Flow:**
1. Create a student via Student Service
2. Create a course via Course Service
3. Enroll a student in a course via Enrollment Service
4. Enrollment Service calls Student Service (through the gateway) to verify the student exists
5. Enrollment Service calls Course Service (through the gateway) to verify the course exists
6. If both exist, the enrollment is saved and a `200 OK` is returned immediately
7. Enrollment Service publishes an `EnrollmentCreated` event to RabbitMQ (fire-and-forget, doesn't block the response)
8. Notification Service, listening independently, consumes the event and simulates sending a confirmation

All external traffic goes through the API Gateway on port 5000 — internal services are not exposed directly.

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQL Server
- `HttpClient` (via `IHttpClientFactory`) for synchronous service-to-service calls
- Polly — retry and circuit breaker policies for resilient HTTP calls
- YARP (Yet Another Reverse Proxy) — API Gateway with routing, load balancing, and active health checks
- RabbitMQ — asynchronous messaging between Enrollment Service (publisher) and Notification Service (consumer)
- Docker & Docker Compose

## Key Principles Demonstrated

- **Database-per-service** — no shared database, no cross-service foreign keys; services only reference each other by ID
- **Synchronous inter-service communication** — Enrollment Service calls Student/Course Service over HTTP for validation that must happen before responding
- **Asynchronous inter-service communication** — Enrollment Service publishes an event to RabbitMQ after saving, without waiting for or knowing about any consumer
- **Independent deployability** — each service has its own Dockerfile and can be built/run on its own
- **Resilience** — Polly retry (exponential backoff) and circuit breaker policies protect HTTP calls; Notification Service retries its RabbitMQ connection on startup to handle broker startup delays
- **API Gateway** — a single entry point (YARP) hides internal service addresses from clients and centralizes routing
- **Load balancing & health checks** — Student Service runs as two instances behind the gateway; YARP distributes requests (RoundRobin) and stops routing to instances that fail active health checks
- **Decoupled messaging** — Notification Service can be down, slow, or added later without ever changing Enrollment Service

## Project Structure

```
/StudentService
  /Controllers
  /Models
  /Data
  Dockerfile
/CourseService
  /Controllers
  /Models
  /Data
  Dockerfile
/EnrollmentService
  /Controllers
  /Models
  /Data
  /Messaging        (RabbitMQ publisher)
  Dockerfile
/NotificationService
  Worker.cs          (RabbitMQ consumer)
  Dockerfile
/ApiGateway
  appsettings.json   (YARP routes, clusters, health checks)
  Dockerfile
docker-compose.yml
```

Each service uses a flat structure (no Clean Architecture layers) — the goal of this project is learning inter-service communication and infrastructure patterns, not internal architecture.

## Running with Docker (recommended)

```bash
docker-compose up --build
```

This starts SQL Server, RabbitMQ, two Student Service instances, Course Service, Enrollment Service, Notification Service, and the API Gateway — with health checks gating startup order so dependent services wait until SQL Server and RabbitMQ are actually ready. Each service applies its own EF Core migrations automatically on startup. SQL Server data persists across restarts via a named volume.

## Running Locally (without Docker)

Each service needs to be run separately, in its own terminal, with SQL Server LocalDB and a local RabbitMQ instance:

```bash
cd StudentService
dotnet ef database update
dotnet run
```

Repeat for `CourseService`, `EnrollmentService`, `NotificationService`, and `ApiGateway`. All services must be running simultaneously for the full flow to work.

## API Endpoints (via API Gateway, port 5000)

| Method | Endpoint | Routed to |
|--------|----------|-----------|
| POST | `/students` | Student Service (load balanced) |
| GET | `/students/{id}` | Student Service (load balanced) |
| POST | `/courses` | Course Service |
| GET | `/courses/{id}` | Course Service |
| POST | `/enroll` | Enrollment Service (validates both student and course exist, then publishes an event) |

**Example request:**
```json
POST /enroll
{
  "studentId": 1,
  "courseId": 1
}
```

## Messaging Notes

- Enrollment Service publishes to a durable `enrollment-created` queue after saving each enrollment.
- Notification Service consumes with manual acknowledgement (`autoAck: false`) — a message is only removed from the queue after it's successfully processed, so a crash mid-processing doesn't lose data.
- RabbitMQ's management UI is available at `http://localhost:15672` (guest/guest) to inspect queues and messages directly.

## Resilience & Load Balancing Notes

- Enrollment Service's calls to Student/Course Service are wrapped in Polly retry (3 attempts, exponential backoff) and circuit breaker policies.
- The API Gateway load-balances across two Student Service instances (`studentservice1`, `studentservice2`) using RoundRobin, with active health checks polling a dedicated `/health` endpoint every 5 seconds — an unhealthy instance is automatically taken out of rotation.

## Possible Next Steps

- Fanout exchange so multiple independent consumers (e.g. an Analytics Service) can each receive every enrollment event
- Notification Service fetching real student/course names via the gateway and sending an actual email
- Separate migrations from service startup (run as a one-time deployment step instead of on every scaled instance)
