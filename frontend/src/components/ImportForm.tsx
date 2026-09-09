import { useState } from "react";
import type { FormEvent } from "react";
import { api } from "../api/client";

interface Props {
  onImported: (newSessionId: number) => void;
}

export function ImportForm({ onImported }: Props) {
  const [file, setFile] = useState<File | null>(null);
  const [sessionName, setSessionName] = useState("");
  const [source, setSource] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!file) {
      setError("Escolhe um ficheiro CSV primeiro.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const result = await api.importCsv(file, sessionName, source || "manual-upload");
      setFile(null);
      setSessionName("");
      setSource("");
      onImported(result.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao importar o ficheiro.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      style={{
        border: "1px solid #ddd",
        borderRadius: 8,
        padding: 16,
        marginTop: 16,
        marginBottom: 24,
        background: "#fafafa",
      }}
    >
      <h3 style={{ margin: "0 0 12px", fontSize: 16, color: "#1F3A5F" }}>Importar nova sessão</h3>

      <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "flex-end" }}>
        <label style={{ display: "flex", flexDirection: "column", fontSize: 13 }}>
          Ficheiro CSV
          <input
            type="file"
            accept=".csv"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            style={{ marginTop: 4 }}
          />
        </label>

        <label style={{ display: "flex", flexDirection: "column", fontSize: 13 }}>
          Nome da sessão (opcional)
          <input
            type="text"
            value={sessionName}
            onChange={(e) => setSessionName(e.target.value)}
            placeholder="ex: Silverstone Race"
            style={{ marginTop: 4, padding: 6 }}
          />
        </label>

        <label style={{ display: "flex", flexDirection: "column", fontSize: 13 }}>
          Origem (opcional)
          <input
            type="text"
            value={source}
            onChange={(e) => setSource(e.target.value)}
            placeholder="ex: ac-lap-coach"
            style={{ marginTop: 4, padding: 6 }}
          />
        </label>

        <button
          type="submit"
          disabled={busy}
          style={{
            padding: "8px 16px",
            background: "#1F3A5F",
            color: "white",
            border: "none",
            borderRadius: 6,
            cursor: busy ? "not-allowed" : "pointer",
          }}
        >
          {busy ? "A importar..." : "Importar"}
        </button>
      </div>

      {error && <p style={{ color: "#C0392B", marginTop: 8, fontSize: 13 }}>{error}</p>}
    </form>
  );
}