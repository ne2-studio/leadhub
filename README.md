# LeadHub

A lightweight, self-hosted form backend for static websites. Create forms, receive submissions at
a public per-form endpoint, browse them from an authenticated admin panel, and optionally get
notified by email when a new submission arrives. See [`docs/PRD.md`](docs/PRD.md) for the full
product spec.

## What's here

| Directory | Contents |
|-----------|----------|
| `docs/PRD.md` | Product requirements |
| `docs/DESIGN.md` | The Exeal visual design system the admin panel follows |
| `docs/ARCHITECTURE.md` | The architecture standard both services follow |
| `docs/CONTRACT.md` | The application (use-case) contract |
| `docs/API.md` | The HTTP API contract |
| `backend/` | ASP.NET Core (.NET 10) ports & adapters implementation — see [`backend/README.md`](backend/README.md) |
| `frontend/` | React 19 + Vite + Zustand admin panel — see [`frontend/README.md`](frontend/README.md) |
| `.github/workflows/` | Path-filtered CI/CD for each service (build → test → Docker image → registry → deploy webhook) |

## Architecture

Two independently deployable services, no shared code between them:

| Directory | Stack |
|-----------|-------|
| `frontend/` | React 19, TypeScript, Vite, Tailwind CSS v4, Zustand, react-oidc-context |
| `backend/` | ASP.NET Core (.NET 10), PostgreSQL, Serilog |

Auth is OIDC/JWT Bearer end-to-end for the admin panel: the frontend authenticates against an
external OIDC provider and attaches the access token to every admin API call; the backend
validates it via `JwtBearer` middleware. The public form-submission endpoint requires no
authentication. Full conventions and rationale are in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Getting started

### Prerequisites

- Node.js 22+
- .NET 10 SDK
- Docker (for PostgreSQL locally, and for building images)

### Backend

```bash
cd backend
dotnet restore
dotnet run --project LeadHub.Api
```

See [`backend/README.md`](backend/README.md) for running PostgreSQL locally, environment
configuration (including email notifications via Resend), tests, and Docker.

### Frontend

```bash
cd frontend
cp .env.example .env
# Set VITE_API_URL to the backend URL, and the OIDC authority/client_id in src/main.tsx

npm install
npm run dev
```

See [`frontend/README.md`](frontend/README.md) for details.

### Everything via Docker Compose

`docker-compose.yaml` at the repo root spins up Postgres, the backend, and the frontend together,
each service built from its own `Dockerfile`. The frontend image only copies a pre-built `dist/`
(it doesn't run `npm run build` itself), so build the frontend once first:

```bash
cd frontend && cp .env.example .env && npm install && npm run build && cd ..
docker compose up --build
```

Backend: http://localhost:5050 · Frontend: http://localhost:3000 · Postgres: localhost:5432.

## Deployment

Both services are containerized and deploy independently. CI/CD runs on push to `main`
(path-filtered per service), builds a Docker image, pushes it to GitHub Container Registry, and
triggers a Coolify deploy webhook — see `.github/workflows/backend-deploy.yml` and
`frontend-deploy.yml`.

## License

MIT © [Exeal](https://www.exeal.com)
