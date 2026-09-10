import type { Stint } from "../api/client";

interface Props {
  stints: Stint[];
  hiddenDrivers: string[];
  hiddenTeams: string[];
  onHiddenDriversChange: (drivers: string[]) => void;
  onHiddenTeamsChange: (teams: string[]) => void;
}

function toggle(list: string[], value: string): string[] {
  return list.includes(value) ? list.filter((v) => v !== value) : [...list, value];
}

export function DriverTeamFilter({
  stints,
  hiddenDrivers,
  hiddenTeams,
  onHiddenDriversChange,
  onHiddenTeamsChange,
}: Props) {
  const drivers = Array.from(new Set(stints.map((s) => s.driver))).sort();
  const teams = Array.from(new Set(stints.map((s) => s.team).filter((t): t is string => !!t))).sort();

  if (drivers.length === 0) return null;

  return (
    <div
      style={{
        display: "flex",
        gap: 24,
        flexWrap: "wrap",
        padding: 12,
        background: "#f5f5f5",
        borderRadius: 8,
        marginBottom: 16,
        fontSize: 13,
      }}
    >
      <div>
        <strong style={{ display: "block", marginBottom: 4 }}>Drivers</strong>
        <div style={{ display: "flex", gap: 12, flexWrap: "wrap" }}>
          {drivers.map((driver) => (
            <label key={driver} style={{ display: "flex", alignItems: "center", gap: 4 }}>
              <input
                type="checkbox"
                checked={!hiddenDrivers.includes(driver)}
                onChange={() => onHiddenDriversChange(toggle(hiddenDrivers, driver))}
              />
              {driver}
            </label>
          ))}
        </div>
      </div>

      {teams.length > 0 && (
        <div>
          <strong style={{ display: "block", marginBottom: 4 }}>Teams</strong>
          <div style={{ display: "flex", gap: 12, flexWrap: "wrap" }}>
            {teams.map((team) => (
              <label key={team} style={{ display: "flex", alignItems: "center", gap: 4 }}>
                <input
                  type="checkbox"
                  checked={!hiddenTeams.includes(team)}
                  onChange={() => onHiddenTeamsChange(toggle(hiddenTeams, team))}
                />
                {team}
              </label>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}