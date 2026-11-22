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
| `.github/workflows/` | Path-filtered CI/CD for each service (backend: build → test → Docker image → registry → Coolify webhook; frontend: type-check → build → Cloudflare Pages) |

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
- Docker (for PostgreSQL locally, and for building the backend image)

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

`docker-compose.yaml` at the repo root spins up Postgres, the backend, the frontend, and a
`fake-oidc` auth provider together: the backend is built from its `Dockerfile`, the frontend runs
the Vite dev server in a `node:22-alpine` container over the mounted `frontend/` directory, and
fake-oidc is pulled prebuilt:

```bash
docker compose up --build
```

`fake-oidc` replaces the real OIDC provider for local dev only — no external credentials needed,
you're logged in automatically as an "admin" user. Add `127.0.0.1 fake-oidc` to your hosts file
first (see the comment at the top of `docker-compose.yaml` for why).

Backend: http://localhost:5050 · Frontend: http://localhost:3000 · Postgres: localhost:5432 ·
fake-oidc: http://localhost:5000.

## Deployment

The two services deploy independently, path-filtered per service:

- **Backend** — on push to `main`, `.github/workflows/backend-deploy.yml` runs the tests, builds a
  Docker image, pushes it to GitHub Container Registry and triggers a Coolify deploy webhook.
- **Frontend** — `.github/workflows/frontend-deploy.yml` type-checks and builds the Vite app (with
  `VITE_*` from the repo's Actions variables) and deploys `dist/` to the Cloudflare Pages project
  `leadhub-frontend`: push to `main` → production (https://leadhub-frontend.ne2.studio); pull
  request → preview at `<branch>.leadhub-frontend.pages.dev`, linked in a PR comment. With no
  `404.html` in `dist/`, Pages serves `index.html` for unknown paths (SPA routing).

## License

MIT © [Exeal](https://www.exeal.com)
