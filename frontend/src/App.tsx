import { useState } from "react";
import { Dashboard } from "./pages/Dashboard";
import { CompareSessions } from "./pages/CompareSessions";

type View = "dashboard" | "compare";

export function App() {
  const [view, setView] = useState<View>("dashboard");

  return (
    <div>
      <nav
        style={{
          display: "flex",
          gap: 8,
          padding: "12px 16px",
          borderBottom: "1px solid #eee",
          fontFamily: "sans-serif",
        }}
      >
        <button
          onClick={() => setView("dashboard")}
          style={{
            padding: "6px 14px",
            borderRadius: 6,
            border: "none",
            cursor: "pointer",
            background: view === "dashboard" ? "#1F3A5F" : "#eee",
            color: view === "dashboard" ? "white" : "#333",
          }}
        >
          Dashboard
        </button>
        <button
          onClick={() => setView("compare")}
          style={{
            padding: "6px 14px",
            borderRadius: 6,
            border: "none",
            cursor: "pointer",
            background: view === "compare" ? "#1F3A5F" : "#eee",
            color: view === "compare" ? "white" : "#333",
          }}
        >
          Compare Sessions
        </button>
      </nav>

      {view === "dashboard" ? <Dashboard /> : <CompareSessions />}
    </div>
  );
}