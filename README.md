# Race Engineering Debrief Tool

A full-stack internal tool for reviewing race session data: import lap-by-lap
data from a session (CSV), then browse it by driver and stint - lap times,
tyre compound, tyre age, and predicted vs actual pace when a prediction is
available.

**Live demo:** [VERCEL](https://race-engineering-tool.vercel.app) (API docs at
`https://race-engineering-tool.onrender.com/swagger/`)

> The demo runs on free-tier hosting: the backend spins down after 15
> minutes of inactivity, so the first request after a while can take
> 30-60s to wake it up. The demo database also resets periodically since
> the free tier has no persistent disk - import the sample CSV below if
> the session list looks empty.

This is deliberately built as a "tool for engineers", not a public app: the
target user is someone who wants to open a session after a race and quickly
answer "which stint was actually quick, and where did we lose time" - the
same kind of question a race engineer asks on Sunday evening.

## Why this project

It reuses the CSV outputs from two other projects in this portfolio -
[ac-lap-coach](../ac-lap-coach) and the pit-stop predictor - as its data
source, so it doubles as an integration layer: the backend exposes an API
over data that other tools already produce, rather than being a standalone
demo with its own fake dataset.

## Stack

- **Backend:** C# / ASP.NET Core 8 Minimal API, Entity Framework Core, SQLite
- **Frontend:** TypeScript, React, Vite, Recharts
- **Deploy:** Backend on Render (Docker), frontend on Vercel

## Architecture

```
race-engineering-tool/
├── backend/
│   └── RaceEngineeringApi/
│       ├── Models/            # Session, Lap, Stint entities
│       ├── Data/              # EF Core DbContext
│       ├── Services/          # CSV import logic (reads ac-lap-coach / pit-stop-predictor exports)
│       ├── Endpoints/         # Minimal API route groups (sessions, laps, import)
│       └── Program.cs         # App wiring, DI, CORS, endpoint mapping
└── frontend/
    └── src/
        ├── api/                # Typed fetch client for the backend API
        ├── components/         # StintChart, LapTable, SessionPicker
        ├── pages/              # Dashboard, SessionDetail
        └── App.tsx
```

## Data model

- **Session** - one race/practice session (track, date, source)
- **Stint** - a continuous run on one set of tyres within a session
- **Lap** - belongs to a stint: lap number, lap time, tyre age, compound,
  and (optionally) a predicted lap time if the CSV came from a model

## Running it

### Backend

```bash
cd backend/RaceEngineeringApi
dotnet restore
dotnet run
# API available at http://localhost:5080, Swagger UI at /swagger
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# App available at http://localhost:5173
```

The frontend expects the API at `http://localhost:5080` - change
`VITE_API_URL` in `frontend/.env` if you run the backend elsewhere.

### Importing data

The API requires an `X-Api-Key` header on every `/api/*` request (see
[Auth](#auth) below).

```bash
curl -F "file=@../ac-lap-coach/laps.csv" -F "sessionName=Silverstone" -F "source=ac-lap-coach" \
  -H "X-Api-Key: your-team-key" \
  http://localhost:5080/api/sessions/import
```

## Auth

A shared API key protects all `/api/*` endpoints - set via the `ApiKey`
setting or the `RACE_ENGINEERING_API_KEY` environment variable on the
backend, and `VITE_API_KEY` on the frontend (must match). If no key is
configured, auth is skipped (useful for local dev). This is a
deliberately simple scheme for a small internal team, not per-user auth -
see the code comments in `Middleware/ApiKeyMiddleware.cs` for the reasoning.

## Roadmap

- [x] Data model + EF Core SQLite persistence
- [x] CSV import endpoint
- [x] Session / stint / lap read API
- [x] React dashboard: session list, stint comparison chart, lap table
- [x] Auth (even a simple API key) so this could realistically run for a team
- [x] Deploy to a free-tier cloud host (Render backend + Vercel frontend)
- [ ] Import directly from the F1 strategy predictor's cached FastF1 data
- [ ] Move off SQLite to a hosted Postgres for persistent demo data

## Note on tooling

Built and verified with real compilers: `dotnet build` for the backend and
`npm run build` for the frontend both pass cleanly. Deployed live via
Docker (backend, on Render) and Vercel (frontend).