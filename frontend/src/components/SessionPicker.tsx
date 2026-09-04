import type { SessionSummary } from "../api/client";

interface Props {
  sessions: SessionSummary[];
  selectedId: number | null;
  onSelect: (id: number) => void;
}

export function SessionPicker({ sessions, selectedId, onSelect }: Props) {
  if (sessions.length === 0) {
    return <p>No sessions imported yet — use the API's /api/sessions/import endpoint to add one.</p>;
  }

  return (
    <select
      value={selectedId ?? ""}
      onChange={(e) => onSelect(Number(e.target.value))}
      style={{ padding: "6px 10px", fontSize: 14 }}
    >
      <option value="" disabled>
        Select a session…
      </option>
      {sessions.map((s) => (
        <option key={s.id} value={s.id}>
          {s.name} ({s.source})
        </option>
      ))}
    </select>
  );
}
