import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createGlossaryTerm,
  deleteGlossaryTerm,
  getGlossary,
  updateGlossaryTerm,
  type GlossaryTerm,
} from "../services/assistantServices";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";

const EMPTY = { expression: "", meaning: "", category: "", active: true };

// Glosario de modismos: cómo describe la gente las fallas y qué significan técnicamente.
// La IA lo usa para entender a los empleados. Técnico y Administrador lo amplían (aprendizaje continuo).
export default function GlossaryPage() {
  const { role } = useAuth();
  const canEdit = role === "Administrador" || role === "Técnico";
  const queryClient = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["glossary"], queryFn: getGlossary });
  const [search, setSearch] = useState("");
  const [form, setForm] = useState(EMPTY);
  const [editingId, setEditingId] = useState<string | null>(null);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["glossary"] });
  const save = useMutation({
    mutationFn: () => (editingId ? updateGlossaryTerm(editingId, form) : createGlossaryTerm(form)),
    onSuccess: () => {
      invalidate();
      setForm(EMPTY);
      setEditingId(null);
    },
  });
  const remove = useMutation({ mutationFn: deleteGlossaryTerm, onSuccess: invalidate });

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return (data ?? []).filter(
      (t) => !q || t.expression.includes(q) || t.meaning.toLowerCase().includes(q) || t.category.includes(q)
    );
  }, [data, search]);

  function edit(t: GlossaryTerm) {
    setEditingId(t.id);
    setForm({ expression: t.expression, meaning: t.meaning, category: t.category, active: t.active });
  }

  return (
    <div className="space-y-4 p-6">
      <div>
        <h1 className="text-xl font-semibold">Glosario de modismos</h1>
        <p className="text-sm text-gray-400">
          Expresiones con las que la gente describe las fallas y su significado técnico. La IA las usa para
          entender reportes como "se fundió" o "se pega".
        </p>
      </div>

      {canEdit && (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            save.mutate();
          }}
          className="grid gap-2 rounded border border-white/10 p-3 md:grid-cols-[1fr_2fr_1fr_auto]"
        >
          <input
            value={form.expression}
            onChange={(e) => setForm({ ...form, expression: e.target.value })}
            placeholder='Expresión (ej: "se emputó")'
            className="input"
          />
          <input
            value={form.meaning}
            onChange={(e) => setForm({ ...form, meaning: e.target.value })}
            placeholder="Significado técnico"
            className="input"
          />
          <input
            value={form.category}
            onChange={(e) => setForm({ ...form, category: e.target.value })}
            placeholder="Categoría (ej: pantalla)"
            className="input"
          />
          <div className="flex gap-2">
            <button
              type="submit"
              disabled={form.expression.trim().length < 2 || form.meaning.trim().length < 3 || save.isPending}
              className="rounded bg-blue-600 px-3 py-2 text-white disabled:opacity-50"
            >
              {editingId ? "Guardar" : "Agregar"}
            </button>
            {editingId && (
              <button
                type="button"
                onClick={() => {
                  setEditingId(null);
                  setForm(EMPTY);
                }}
                className="text-gray-400"
              >
                Cancelar
              </button>
            )}
          </div>
          {save.error && (
            <p className="text-sm text-red-500 md:col-span-4">{getApiErrorMessage(save.error)}</p>
          )}
        </form>
      )}

      <input
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Buscar expresión, significado o categoría…"
        className="input w-full max-w-md"
      />

      {isLoading ? (
        <p className="text-gray-400">Cargando…</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-white/10 text-left">
              <th className="py-2">Expresión</th>
              <th>Significado técnico</th>
              <th>Categoría</th>
              <th>Origen</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((t) => (
              <tr key={t.id} className={`border-b border-white/5 ${t.active ? "" : "opacity-50"}`}>
                <td className="py-2 font-medium">"{t.expression}"</td>
                <td>{t.meaning}</td>
                <td>{t.category}</td>
                <td className="text-xs text-gray-400">{t.isBase ? "Base" : "Equipo"}</td>
                <td className="space-x-2 text-right text-xs">
                  {canEdit && (
                    <>
                      <button onClick={() => edit(t)} className="text-blue-500">
                        Editar
                      </button>
                      {!t.active && (
                        <button
                          onClick={() =>
                            updateGlossaryTerm(t.id, {
                              expression: t.expression,
                              meaning: t.meaning,
                              category: t.category,
                              active: true,
                            }).then(invalidate)
                          }
                          className="text-green-500"
                        >
                          Reactivar
                        </button>
                      )}
                      {t.active && (
                        <button
                          onClick={() => {
                            if (confirm(t.isBase ? "¿Desactivar esta expresión?" : "¿Eliminar esta expresión?"))
                              remove.mutate(t.id);
                          }}
                          className="text-red-500"
                        >
                          {t.isBase ? "Desactivar" : "Eliminar"}
                        </button>
                      )}
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
