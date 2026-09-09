"""
Import a session directly from FastF1's cached timing data into the
Race Engineering Debrief Tool API — no manual CSV export step needed.

FastF1 is a Python library, and the API backend is C#, so this script is
the bridge between the two: it reads the session via FastF1, reshapes it
into the same CSV format CsvImportService.cs already expects, and POSTs it
straight to /api/sessions/import.

Usage:
    python import_from_fastf1.py --year 2024 --event Silverstone --session R \
        --api-url https://your-api.onrender.com --api-key YOUR_KEY

    # List available events for a year, if you're not sure of the name:
    python import_from_fastf1.py --year 2024 --list-events

Requires: fastf1, pandas, requests (see requirements.txt)
"""

import argparse
import io
import os
import sys

import fastf1
import pandas as pd
import requests


def list_events(year: int) -> None:
    schedule = fastf1.get_event_schedule(year)
    print(f"Available events in {year}:")
    for _, row in schedule.iterrows():
        print(f"  {row['RoundNumber']:>2}  {row['EventName']}")


def build_dataframe(year: int, event: str, session_type: str) -> pd.DataFrame:
    """Load the session from FastF1 and reshape it into the LapRow CSV shape
    the backend's CsvImportService expects: Driver, StintNumber, Compound,
    LapNumber, LapTimeSeconds, TyreLife, PredictedLapTimeSeconds."""

    session = fastf1.get_session(year, event, session_type)
    session.load(telemetry=False, weather=False, messages=False)

    laps = session.laps

    # Drop laps with no recorded lap time (out-laps, red-flag laps, etc.) —
    # they'd otherwise show up as garbage rows with a missing LapTimeSeconds.
    laps = laps[laps["LapTime"].notna()].copy()

    rows = pd.DataFrame({
        "Driver": laps["Driver"],
        "StintNumber": laps["Stint"].astype(int),
        "Compound": laps["Compound"].fillna("UNKNOWN"),
        "LapNumber": laps["LapNumber"].astype(int),
        "LapTimeSeconds": laps["LapTime"].dt.total_seconds().round(3),
        "TyreLife": laps["TyreLife"].fillna(0).astype(int),
        # FastF1 has no built-in lap-time prediction model — this column
        # only gets populated for CSVs coming from the pit-stop-predictor
        # project. Left blank here on purpose.
        "PredictedLapTimeSeconds": "",
    })

    return rows.sort_values(["Driver", "LapNumber"]).reset_index(drop=True)


def upload_csv(df: pd.DataFrame, session_name: str, api_url: str, api_key: str) -> dict:
    buffer = io.StringIO()
    df.to_csv(buffer, index=False)
    buffer.seek(0)

    response = requests.post(
        f"{api_url.rstrip('/')}/api/sessions/import",
        headers={"X-Api-Key": api_key} if api_key else {},
        files={"file": ("fastf1-session.csv", buffer.getvalue(), "text/csv")},
        data={"sessionName": session_name, "source": "fastf1"},
        timeout=60,
    )
    response.raise_for_status()
    return response.json()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--year", type=int, required=True, help="e.g. 2024")
    parser.add_argument("--event", type=str, help="event name or round number, e.g. Silverstone or 10")
    parser.add_argument("--session", type=str, default="R",
                         help="session type: R (race), Q (qualifying), FP1/FP2/FP3, S (sprint). Default: R")
    parser.add_argument("--api-url", type=str, help="your API URL, e.g. https://your-api.onrender.com")
    parser.add_argument("--api-key", type=str, default="", help="your RACE_ENGINEERING_API_KEY")
    parser.add_argument("--session-name", type=str, default=None,
                         help="name to give the imported session (default: auto-generated)")
    parser.add_argument("--cache-dir", type=str, default="./fastf1_cache",
                         help="local folder where FastF1 stores downloaded data (default: ./fastf1_cache)")
    parser.add_argument("--list-events", action="store_true", help="list --year's events and exit")
    parser.add_argument("--dry-run", action="store_true",
                         help="build the CSV locally and print a summary, without sending it to the API")

    args = parser.parse_args()

    # fastf1.Cache.enable_cache() requires the directory to already exist —
    # create it first so a fresh checkout doesn't fail on the first run.
    os.makedirs(args.cache_dir, exist_ok=True)
    fastf1.Cache.enable_cache(args.cache_dir)

    if args.list_events:
        list_events(args.year)
        return

    if not args.event:
        parser.error("--event is required (unless you use --list-events)")

    print(f"Loading {args.year} {args.event} ({args.session}) from FastF1...")
    df = build_dataframe(args.year, args.event, args.session)

    if df.empty:
        print("No valid laps found in this session — nothing to import.", file=sys.stderr)
        sys.exit(1)

    session_name = args.session_name or f"{args.event} {args.year} - {args.session}"
    print(f"{len(df)} laps found, {df['Driver'].nunique()} drivers. Session: \"{session_name}\"")

    if args.dry_run:
        print(df.head(10).to_string(index=False))
        print("(--dry-run: nothing was sent to the API)")
        return

    if not args.api_url:
        parser.error("--api-url is required to upload data (or use --dry-run)")

    result = upload_csv(df, session_name, args.api_url, args.api_key)
    print(f"Imported successfully — session id {result['id']} (\"{result['name']}\")")


if __name__ == "__main__":
    main()