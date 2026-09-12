import type { Stint } from "../api/client";

interface Props {
  stint: Stint;
}

export function LapTable({ stint }: Props) {
  const fastestLapTime = Math.min(...stint.laps.map((l) => l.lapTimeSeconds));

  return (
    <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
      <thead>
        <tr style={{ textAlign: "left", borderBottom: "2px solid #ccc" }}>
          <th>Lap</th>
          <th>Time (s)</th>
          <th>Tyre life</th>
          <th>Predicted (s)</th>
          <th>Delta</th>
        </tr>
      </thead>
      <tbody>
        {stint.laps.map((lap) => {
          const delta =
            lap.predictedLapTimeSeconds != null
              ? lap.lapTimeSeconds - lap.predictedLapTimeSeconds
              : null;
          const isFastest = lap.lapTimeSeconds === fastestLapTime;
          return (
            <tr key={lap.id} style={{ borderBottom: "1px solid #eee" }}>
              <td>{lap.lapNumber}</td>
              <td style={{ fontWeight: isFastest ? 700 : 400, color: isFastest ? "#6B21A8" : undefined }}>
                {lap.lapTimeSeconds.toFixed(3)}
                {isFastest && (
                  <span title="Fastest lap of this stint" style={{ marginLeft: 4 }}>
                    ⚡
                  </span>
                )}
              </td>
              <td>{lap.tyreLife}</td>
              <td>{lap.predictedLapTimeSeconds?.toFixed(3) ?? "—"}</td>
              <td style={{ color: delta != null && delta > 0 ? "#C0392B" : "#1F7A3D" }}>
                {delta != null ? delta.toFixed(3) : "—"}
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}