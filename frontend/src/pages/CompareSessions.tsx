import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { SessionDetail, SessionSummary } from "../api/client";
import { SessionPicker } from "../components/SessionPicker";
import { StintChart } from "../components/StintChart";
import { LapTable } from "../components/LapTable";

function SessionColumn({
  sessions,
  selectedId,
  onSelect,
  detail,
}: {
  sessions: SessionSummary[];
  selectedId: number | null;
  onSelect: (id: number) => void;
  detail: SessionDetail | null;
}) {
  return (
    <div style={{ flex: 1, minWidth: 0 }}>
      <SessionPicker sessions={sessions} selectedId={selectedId} onSelect={onSelect} />

      {detail && (
        <div style={{ marginTop: 16 }}>
          <h3 style={{ margin: "0 0 4px" }}>{detail.name}</h3>
          <p style={{ color: "#666", fontSize: 13, margin: "0 0 16px" }}>
            {detail.source} · {new Date(detail.importedAt).toLocaleDateString()}
          </p>

          {detail.stints.map((stint) => (
            <section key={stint.id} style={{ marginBottom: 24 }}>
              <StintChart stint={stint} />
              <LapTable stint={stint} />
            </section>
          ))}
        </div>
      )}
    </div>
  );
}

export function CompareSessions() {
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [idA, setIdA] = useState<number | null>(null);
  const [idB, setIdB] = useState<number | null>(null);
  const [detailA, setDetailA] = useState<SessionDetail | null>(null);
  const [detailB, setDetailB] = useState<SessionDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .listSessions()
      .then(setSessions)
      .catch((err) => setError(err.message));
  }, []);

  useEffect(() => {
    if (idA == null) return;
    api
      .getSession(idA)
      .then(setDetailA)
      .catch((err) => setError(err.message));
  }, [idA]);

  useEffect(() => {
    if (idB == null) return;
    api
      .getSession(idB)
      .then(setDetailB)
      .catch((err) => setError(err.message));
  }, [idB]);

  return (
    <div style={{ maxWidth: 1400, margin: "0 auto", padding: "24px 16px", fontFamily: "sans-serif" }}>
      <h1 style={{ color: "#1F3A5F" }}>Compare Sessions</h1>
      <p style={{ color: "#666", marginTop: -8 }}>
        Pick two sessions to see their stints and lap times side by side — e.g. qualifying vs. race pace,
        or the same driver's pace across two events.
      </p>

      {error && <p style={{ color: "#C0392B" }}>Error: {error}</p>}

      <div style={{ display: "flex", gap: 32, marginTop: 24, flexWrap: "wrap" }}>
        <SessionColumn sessions={sessions} selectedId={idA} onSelect={setIdA} detail={detailA} />
        <SessionColumn sessions={sessions} selectedId={idB} onSelect={setIdB} detail={detailB} />
      </div>
    </div>
  );
}