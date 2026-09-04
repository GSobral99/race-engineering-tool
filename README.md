# Race Engineering Debrief Tool

A full-stack internal tool for reviewing race session data: import lap-by-lap
data from a session (CSV), then browse it by driver and stint — lap times,
tyre compound, tyre age, and predicted vs actual pace when a prediction is
available.

This is deliberately built as a "tool for engineers", not a public app: the
target user is someone who wants to open a session after a race and quickly
answer "which stint was actually quick, and where did we lose time" — the
same kind of question a race engineer asks on Sunday evening.

## Why this project

It reuses the CSV outputs from two other projects in this portfolio —
[ac-lap-coach](../ac-lap-coach) and the pit-stop predictor — as its data
source, so it doubles as an integration layer: the backend exposes an API
over data that other tools already produce, rather than being a standalone
demo with its own fake dataset.

## Stack

- **Backend:** C# / ASP.NET Core 8 Minimal API, Entity Framework Core, SQLite
- **Frontend:** TypeScript, React, Vite, Recharts

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

- **Session** — one race/practice session (track, date, source)
- **Stint** — a continuous run on one set of tyres within a session
- **Lap** — belongs to a stint: lap number, lap time, tyre age, compound,
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

The frontend expects the API at `http://localhost:5080` — change
`VITE_API_URL` in `frontend/.env` if you run the backend elsewhere.

### Importing data

```bash
curl -F "file=@../ac-lap-coach/laps.csv" -F "source=ac-lap-coach" \
  http://localhost:5080/api/sessions/import
```

## Roadmap

- [x] Data model + EF Core SQLite persistence
- [x] CSV import endpoint
- [x] Session / stint / lap read API
- [x] React dashboard: session list, stint comparison chart, lap table
- [ ] Auth (even a simple API key) so this could realistically run for a team
- [ ] Import directly from the F1 strategy predictor's cached FastF1 data
- [ ] Deploy to a free-tier cloud host (Azure App Service / Render) with the
      SQLite file on persistent storage, or swap to Postgres for that

## Note on tooling

This backend was written targeting .NET 8 / ASP.NET Core Minimal APIs. The
`.NET SDK` was not available in the environment this was drafted in, so the
backend has not been compiled here — review it with `dotnet build` before
relying on it. The frontend has been built and verified with `npm run build`.
