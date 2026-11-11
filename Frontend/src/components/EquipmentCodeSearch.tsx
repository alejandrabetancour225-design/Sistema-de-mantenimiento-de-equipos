import { useRef, useState } from "react";
import { EQUIPMENT_STATUS_LABEL, type Equipment } from "../types/equipment";

// Motivo por el que un equipo no admite un mantenimiento nuevo (null = sí admite).
function blockedReason(eq: Equipment): string | null {
  if (eq.status === "UNDER_MAINTENANCE") return "ya tiene un mantenimiento abierto";
  if (eq.status === "DECOMMISSIONED") return "está dado de baja";
  return null;
}

// Escribe el código (o serie, marca, modelo) y elige de las coincidencias.
// Enter selecciona la primera coincidencia disponible.
export default function EquipmentCodeSearch({
  equipment,
  value,
  onChange,
}: {
  equipment: Equipment[];
  value: string;
  onChange: (equipmentId: string) => void;
}) {
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const selected = equipment.find((e) => e.id === value);

  const term = query.trim().toLowerCase();
  const matches = equipment
    .filter(
      (e) =>
        !term ||
        e.internalCode.toLowerCase().includes(term) ||
        e.serialNumber.toLowerCase().includes(term) ||
        `${e.brand} ${e.model}`.toLowerCase().includes(term)
    )
    // Primero los que empiezan por lo escrito, luego el resto
    .sort((a, b) => {
      const aStarts = a.internalCode.toLowerCase().startsWith(term) ? 0 : 1;
      const bStarts = b.internalCode.toLowerCase().startsWith(term) ? 0 : 1;
      return aStarts - bStarts || a.internalCode.localeCompare(b.internalCode);
    })
    .slice(0, 8);

  function pick(eq: Equipment) {
    if (blockedReason(eq)) return;
    onChange(eq.id);
    setQuery("");
    setOpen(false);
  }

  if (selected) {
    return (
      <div className="flex items-center justify-between gap-2 rounded border bg-blue-50 px-3 py-2 text-sm">
        <span>
          <strong>{selected.internalCode}</strong> — {selected.brand} {selected.model}{" "}
          <span className="text-xs text-gray-600">({EQUIPMENT_STATUS_LABEL[selected.status]})</span>
        </span>
        <button
          type="button"
          onClick={() => {
            onChange("");
            setTimeout(() => inputRef.current?.focus(), 0);
          }}
          className="shrink-0 text-xs text-blue-600 underline"
        >
          Cambiar
        </button>
      </div>
    );
  }

  return (
    <div className="relative">
      <input
        ref={inputRef}
        value={query}
        placeholder="Escribe el código del equipo, por ejemplo EQ-001"
        onChange={(e) => {
          setQuery(e.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setOpen(false)}
        onKeyDown={(e) => {
          if (e.key === "Enter") {
            e.preventDefault();
            const first = matches.find((m) => !blockedReason(m));
            if (first) pick(first);
          }
          if (e.key === "Escape") setOpen(false);
        }}
        className="input w-full"
        autoComplete="off"
      />

      {open && (
        <ul
          // Evita que el input pierda el foco antes de registrar el clic en una opción
          onMouseDown={(e) => e.preventDefault()}
          className="absolute z-20 mt-1 max-h-64 w-full overflow-auto rounded border bg-white shadow-lg"
        >
          {matches.map((eq) => {
            const reason = blockedReason(eq);
            return (
              <li key={eq.id}>
                <button
                  type="button"
                  disabled={!!reason}
                  onClick={() => pick(eq)}
                  className="flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm hover:bg-blue-50 disabled:cursor-not-allowed disabled:bg-gray-50 disabled:text-gray-400 disabled:hover:bg-gray-50"
                >
                  <span>
                    <strong>{eq.internalCode}</strong> — {eq.brand} {eq.model}
                  </span>
                  <span className="shrink-0 text-xs">
                    {reason ? `No disponible: ${reason}` : EQUIPMENT_STATUS_LABEL[eq.status]}
                  </span>
                </button>
              </li>
            );
          })}
          {matches.length === 0 && (
            <li className="px-3 py-2 text-sm text-gray-500">
              No hay equipos con «{query}».
            </li>
          )}
        </ul>
      )}
    </div>
  );
}
