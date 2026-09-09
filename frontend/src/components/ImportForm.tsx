import { useRef, useState } from "react";
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
  const fileInputRef = useRef<HTMLInputElement>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!file) {
      setError("Choose a CSV file first.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const result = await api.importCsv(file, sessionName, source || "manual-upload");
      setFile(null);
      setSessionName("");
      setSource("");
      if (fileInputRef.current) fileInputRef.current.value = "";
      onImported(result.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Error importing the file.");
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
      <h3 style={{ margin: "0 0 12px", fontSize: 16, color: "#1F3A5F" }}>Upload new session</h3>

      <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "flex-end" }}>
        <label style={{ display: "flex", flexDirection: "column", fontSize: 13 }}>
          CSV file
          <div style={{ display: "flex", alignItems: "center", gap: 8, marginTop: 4 }}>
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              style={{
                padding: "6px 12px",
                border: "1px solid #ccc",
                borderRadius: 6,
                background: "#eee",
                cursor: "pointer",
                fontSize: 13,
              }}
            >
              Choose file
            </button>
            <span style={{ color: file ? "#333" : "#888", fontSize: 13 }}>
              {file ? file.name : "No file selected"}
            </span>
            {/* The real file input is invisible — the custom button above
                triggers it via the ref, so we control every piece of text
                shown to the user instead of the browser's native (and
                locale-dependent) file-picker label. */}
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              style={{ display: "none" }}
            />
          </div>
        </label>

        <label style={{ display: "flex", flexDirection: "column", fontSize: 13 }}>
          Session name (optional)
          <input
            type="text"
            value={sessionName}
            onChange={(e) => setSessionName(e.target.value)}
            placeholder="e.g. Silverstone Race"
            style={{ marginTop: 4, padding: 6 }}
          />
        </label>

        <label style={{ display: "flex", flexDirection: "column", fontSize: 13 }}>
          Source (optional)
          <input
            type="text"
            value={source}
            onChange={(e) => setSource(e.target.value)}
            placeholder="e.g. ac-lap-coach"
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
          {busy ? "Importing..." : "Import"}
        </button>
      </div>

      {error && <p style={{ color: "#C0392B", marginTop: 8, fontSize: 13 }}>{error}</p>}
    </form>
  );
}