import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { SessionDetail, SessionSummary } from "../api/client";
import { SessionPicker } from "../components/SessionPicker";
import { StintChart } from "../components/StintChart";
import { LapTable } from "../components/LapTable";

export function Dashboard() {
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [detail, setDetail] = useState<SessionDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .listSessions()
      .then(setSessions)
      .catch((err) => setError(err.message));
  }, []);

  useEffect(() => {
    if (selectedId == null) return;
    api
      .getSession(selectedId)
      .then(setDetail)
      .catch((err) => setError(err.message));
  }, [selectedId]);

  return (
    <div style={{ maxWidth: 960, margin: "0 auto", padding: "24px 16px", fontFamily: "sans-serif" }}>
      <h1 style={{ color: "#1F3A5F" }}>Race Engineering Debrief Tool</h1>

      {error && <p style={{ color: "#C0392B" }}>Error: {error}</p>}

      <SessionPicker sessions={sessions} selectedId={selectedId} onSelect={setSelectedId} />

      {detail && (
        <div style={{ marginTop: 24 }}>
          <h2>{detail.name}</h2>
          <p style={{ color: "#666" }}>
            Source: {detail.source} · Imported {new Date(detail.importedAt).toLocaleString()}
          </p>

          {detail.stints.map((stint) => (
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
