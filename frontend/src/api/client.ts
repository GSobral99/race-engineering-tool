const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5080";

export interface SessionSummary {
  id: number;
  name: string;
  source: string;
  importedAt: string;
}

export interface Lap {
  id: number;
  lapNumber: number;
  lapTimeSeconds: number;
  tyreLife: number;
  predictedLapTimeSeconds: number | null;
}

export interface Stint {
  id: number;
  driver: string;
  stintNumber: number;
  compound: string;
  laps: Lap[];
}

export interface SessionDetail extends SessionSummary {
  stints: Stint[];
}

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${API_URL}${path}`);
  if (!res.ok) {
    throw new Error(`Request to ${path} failed: ${res.status} ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

export const api = {
  listSessions: () => get<SessionSummary[]>("/api/sessions"),
  getSession: (id: number) => get<SessionDetail>(`/api/sessions/${id}`),

  async importCsv(file: File, sessionName: string, source: string): Promise<{ id: number; name: string }> {
    const form = new FormData();
    form.append("file", file);
    form.append("sessionName", sessionName);
    form.append("source", source);

    const res = await fetch(`${API_URL}/api/sessions/import`, {
      method: "POST",
      body: form,
    });
    if (!res.ok) {
      throw new Error(`Import failed: ${res.status} ${res.statusText}`);
    }
    return res.json();
  },
};
