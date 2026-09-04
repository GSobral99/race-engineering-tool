import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";
import type { Stint } from "../api/client";

interface Props {
  stint: Stint;
}

export function StintChart({ stint }: Props) {
  const data = stint.laps.map((lap) => ({
    lap: lap.lapNumber,
    actual: lap.lapTimeSeconds,
    predicted: lap.predictedLapTimeSeconds ?? undefined,
  }));

  const hasPredictions = data.some((d) => d.predicted !== undefined);

  return (
    <div style={{ width: "100%", height: 260 }}>
      <h4 style={{ margin: "4px 0" }}>
        {stint.driver} — Stint {stint.stintNumber} ({stint.compound})
      </h4>
      <ResponsiveContainer>
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="lap" label={{ value: "Lap", position: "insideBottom", offset: -4 }} />
          <YAxis
            domain={["auto", "auto"]}
            label={{ value: "Lap time (s)", angle: -90, position: "insideLeft" }}
          />
          <Tooltip />
          <Legend />
          <Line type="monotone" dataKey="actual" stroke="#1F3A5F" dot={false} name="Actual" />
          {hasPredictions && (
            <Line
              type="monotone"
              dataKey="predicted"
              stroke="#C0392B"
              strokeDasharray="4 4"
              dot={false}
              name="Predicted"
            />
          )}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
