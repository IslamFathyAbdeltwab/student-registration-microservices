# Student Registration Microservices

A beginner-friendly microservices demo built to learn and practice correct microservice architecture patterns: independent services, database-per-service, and synchronous service-to-service communication.

## Architecture

Three independent ASP.NET Core Web API services, each with its own database:

```
┌─────────────────┐     ┌─────────────────┐     ┌──────────────────────┐
│  Student Service │     │  Course Service  │     │  Enrollment Service   │
│    (port 5001)   │     │    (port 5002)   │     │     (port 5003)      │
│                  │     │                  │     │                      │
│  StudentServiceDb│     │  CourseServiceDb │     │  EnrollmentServiceDb │
└────────▲─────────┘     └────────▲─────────┘     └───────────┬──────────┘
         │                        │                            │
         │      GET /students/{id}│      GET /courses/{id}    │
         └────────────────────────┴────────────────────────────┘
```

**Flow:**
1. Create a student via Student Service
2. Create a course via Course Service
3. Enroll a student in a course via Enrollment Service
4. Enrollment Service calls Student Service to verify the student exists
5. Enrollment Service calls Course Service to verify the course exists
6. If both exist, the enrollment is saved

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQL Server
- `HttpClient` (via `IHttpClientFactory`) for service-to-service calls
- Docker & Docker Compose

## Key Principles Demonstrated

- **Database-per-service** — no shared database, no cross-service foreign keys; services only reference each other by ID
- **Synchronous inter-service communication** — Enrollment Service calls the other two over HTTP and handles their absence/404s
- **Independent deployability** — each service has its own Dockerfile and can be built/run on its own

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
  Dockerfile
docker-compose.yml
```

Each service uses a flat structure (no Clean Architecture layers) — the goal of this project is learning inter-service communication, not internal architecture.

## Running with Docker (recommended)

```bash
docker-compose up --build
```

This starts a shared SQL Server container plus all three services. Each service applies its own EF Core migrations automatically on startup.

## Running Locally (without Docker)

Each service needs to be run separately, in its own terminal, with SQL Server LocalDB:

```bash
cd StudentService
dotnet ef database update
dotnet run
```

Repeat for `CourseService` and `EnrollmentService`. All three must be running simultaneously for the enrollment flow to work.

## API Endpoints

### Student Service (`:5001`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/students` | Create a student |
| GET | `/students/{id}` | Get a student by id |

### Course Service (`:5002`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/courses` | Create a course |
| GET | `/courses/{id}` | Get a course by id |

### Enrollment Service (`:5003`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/enroll` | Enroll a student in a course (validates both exist first) |

**Example request:**
```json
POST /enroll
{
  "studentId": 1,
  "courseId": 1
}
```

## Possible Next Steps

- API Gateway (YARP or Ocelot) as a single entry point
- Resilience with Polly (retry logic when a dependent service is down)
- Async messaging (RabbitMQ) instead of synchronous HTTP calls
