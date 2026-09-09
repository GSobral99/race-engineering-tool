# Race Engineering Debrief Tool

A full-stack internal tool for reviewing race session data: import lap-by-lap
data from a session (CSV), then browse it by driver and stint - lap times,
tyre compound, tyre age, and predicted vs actual pace when a prediction is
available.

**Live demo:** [VERCEL](https://race-engineering-tool.vercel.app) (API docs at
`https://race-engineering-tool.onrender.com/swagger/`)

> The backend runs on Render's free tier, so it spins down after 15
> minutes of inactivity - the first request after a while can take
> 30-60s to wake it up. The database is a free Neon Postgres instance,
> so data persists normally across restarts and deploys (no reset).

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

- **Backend:** C# / ASP.NET Core 8 Minimal API, Entity Framework Core
- **Database:** PostgreSQL (Neon, free tier) in production; SQLite for local dev
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
│       ├── Middleware/        # API key auth
│       └── Program.cs         # App wiring, DI, CORS, DB provider selection, endpoint mapping
├── frontend/
│   └── src/
│       ├── api/                # Typed fetch client for the backend API
│       ├── components/         # StintChart, LapTable, SessionPicker, ImportForm
│       ├── pages/              # Dashboard
│       └── App.tsx
└── scripts/
    └── import_from_fastf1.py   # Pulls a real F1 session from FastF1 and pushes it to the API
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

You can import a session two ways:

1. **Through the dashboard** - the "Upload new session" form on the main page accepts a CSV directly, no terminal needed.
2. **Via the API directly** (requires the `X-Api-Key` header, see [Auth](#auth)):

```bash
curl -F "file=@../ac-lap-coach/laps.csv" -F "sessionName=Silverstone" -F "source=ac-lap-coach" \
  -H "X-Api-Key: your-team-key" \
  http://localhost:5080/api/sessions/import
```

### Importing a real F1 session (FastF1)

`scripts/import_from_fastf1.py` pulls a real session straight from FastF1's
cached timing data and pushes it to the API - no manual CSV step. This is a
one-off script that can run locally, not a feature
exposed in the web UI (see [Why there's no "Import from FastF1" button](#why-theres-no-import-from-fastf1-button)).

```bash
cd scripts
pip install -r requirements.txt

# See what events exist for a year
python import_from_fastf1.py --year 2024 --list-events

# Import a real session
python import_from_fastf1.py --year 2024 --event Silverstone --session R \
  --api-url https://your-api.onrender.com --api-key your-team-key
```

## Auth

A shared API key protects all `/api/*` endpoints - set via the `ApiKey`
setting or the `RACE_ENGINEERING_API_KEY` environment variable on the
backend, and `VITE_API_KEY` on the frontend (must match). If no key is
configured, auth is skipped (useful for local dev). This is a
deliberately simple scheme for a small internal team, not per-user auth -
see the code comments in `Middleware/ApiKeyMiddleware.cs` for the reasoning.

## Database

Local development uses SQLite by default (zero setup). Production uses a
free [Neon](https://neon.tech) Postgres instance - `Program.cs` picks
whichever one is available: if a `DATABASE_URL` environment variable is
set, it connects to Postgres; otherwise it falls back to the local SQLite
file. Both providers use `EnsureCreated()` rather than EF Core migrations,
which is fine for a project this size but would need to change if the
schema needs to evolve without losing existing data.

## Why there's no "Import from FastF1" button

FastF1 is Python-only; the backend is C#. Wiring a button in the web UI
would mean either shelling out to a Python subprocess from the .NET
backend (fragile, and means bundling Python into the Docker image) or
standing up a second microservice just for this. On top of that, a
session's first FastF1 fetch can take tens of seconds - long enough to
risk a timeout on Render's free tier, and the free tier's cache doesn't
persist across restarts anyway, so most imports would hit that slow path
every time. So I decided to only let the script run locally to avoid all of this for
very little cost.

## Roadmap

- [x] Data model + EF Core persistence
- [x] CSV import endpoint
- [x] Session / stint / lap read API
- [x] React dashboard: session list, stint comparison chart, lap table, CSV upload form
- [x] Auth (even a simple API key) so this could realistically run for a team
- [x] Deploy to a free-tier cloud host (Render backend + Vercel frontend)
- [x] Import directly from FastF1's cached data (via a local script, see above)
- [x] Move off SQLite to a hosted Postgres for persistent demo data (Neon)
- [ ] Swap `EnsureCreated()` for real EF Core migrations, if the schema ever needs to change safely
- [ ] Per-driver / per-team filtering on the dashboard for sessions with a full grid

## Note on tooling

Built and verified with real compilers: `dotnet build` for the backend and
`npm run build` for the frontend both pass cleanly. Deployed live via
Docker (backend, on Render) and Vercel (frontend).