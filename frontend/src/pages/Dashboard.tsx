import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { SessionDetail, SessionSummary } from "../api/client";
import { SessionPicker } from "../components/SessionPicker";
import { StintChart } from "../components/StintChart";
import { LapTable } from "../components/LapTable";
import { ImportForm } from "../components/ImportForm";
import { DriverTeamFilter } from "../components/DriverTeamFilter";

export function Dashboard() {
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [detail, setDetail] = useState<SessionDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [hiddenDrivers, setHiddenDrivers] = useState<string[]>([]);
  const [hiddenTeams, setHiddenTeams] = useState<string[]>([]);

  function refreshSessions() {
    api
      .listSessions()
      .then(setSessions)
      .catch((err) => setError(err.message));
  }

  useEffect(() => {
    refreshSessions();
  }, []);

  useEffect(() => {
    if (selectedId == null) return;
    setHiddenDrivers([]);
    setHiddenTeams([]);
    api
      .getSession(selectedId)
      .then(setDetail)
      .catch((err) => setError(err.message));
  }, [selectedId]);

  function handleImported(newSessionId: number) {
    refreshSessions();
    setSelectedId(newSessionId);
  }

  const visibleStints =
    detail?.stints.filter(
      (s) => !hiddenDrivers.includes(s.driver) && !(s.team && hiddenTeams.includes(s.team))
    ) ?? [];

  return (
    <div style={{ maxWidth: 960, margin: "0 auto", padding: "24px 16px", fontFamily: "sans-serif" }}>
      <h1 style={{ color: "#1F3A5F" }}>Race Engineering Debrief Tool</h1>

      {error && <p style={{ color: "#C0392B" }}>Error: {error}</p>}

      <ImportForm onImported={handleImported} />

      <SessionPicker sessions={sessions} selectedId={selectedId} onSelect={setSelectedId} />

      {detail && (
        <div style={{ marginTop: 24 }}>
          <h2>{detail.name}</h2>
          <p style={{ color: "#666" }}>
            Source: {detail.source} · Imported {new Date(detail.importedAt).toLocaleString()}
          </p>

          <DriverTeamFilter
            stints={detail.stints}
            hiddenDrivers={hiddenDrivers}
            hiddenTeams={hiddenTeams}
            onHiddenDriversChange={setHiddenDrivers}
            onHiddenTeamsChange={setHiddenTeams}
          />

          {visibleStints.length === 0 && (
            <p style={{ color: "#888" }}>No stints match the current filter.</p>
          )}

          {visibleStints.map((stint) => (
            <section key={stint.id} style={{ marginBottom: 32 }}>
              <StintChart stint={stint} />
              <LapTable stint={stint} />
            </section>
          ))}
        </div>
      )}
    </div>
  );
}