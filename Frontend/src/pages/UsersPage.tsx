import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useUsers } from "../hooks/useUsers";
import { updateUser, assignRole } from "../services/usersServices";
import type { Role } from "../types/auth";

const ROLES: Role[] = ["Administrador", "Técnico", "Empleado", "Cliente"];

export default function UsersPage() {
  const { data, isLoading, error } = useUsers();
  const queryClient = useQueryClient();

  const roleMutation = useMutation({
    mutationFn: ({ id, role }: { id: string; role: Role }) => assignRole(id, role),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
  });

  const activeMutation = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      updateUser(id, { active }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
  });

  if (isLoading) return <p className="p-8">Cargando usuarios...</p>;
  if (error) return <p className="p-8 text-red-600">Error al cargar los usuarios.</p>;

  return (
    <div className="p-8 space-y-4">
      <h1 className="text-xl font-semibold">Gestión de usuarios</h1>

      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2">Nombre</th>
            <th>Correo</th>
            <th>Rol</th>
            <th>Estado</th>
            <th>Intentos fallidos</th>
          </tr>
        </thead>
        <tbody>
          {data?.map((user) => (
            <tr key={user.id} className="border-b">
              <td className="py-2">{user.fullName}</td>
              <td>{user.email}</td>
              <td>
                <select
                  value={user.role ?? ""}
                  onChange={(e) =>
                    roleMutation.mutate({ id: user.id, role: e.target.value as Role })
                  }
                  disabled={roleMutation.isPending}
                  className="rounded border px-2 py-1"
                >
                  {ROLES.map((r) => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </select>
              </td>
              <td>
                <button
                  onClick={() =>
                    activeMutation.mutate({ id: user.id, active: !user.active })
                  }
                  disabled={activeMutation.isPending}
                  className={`rounded px-3 py-1 text-xs font-medium ${
                    user.active
                      ? "bg-green-100 text-green-800"
                      : "bg-gray-200 text-gray-600"
                  }`}
                >
                  {user.active ? "Activo" : "Inactivo"}
                </button>
              </td>
              <td>{user.failedAttempts}</td>
            </tr>
          ))}
          {data?.length === 0 && (
            <tr>
              <td colSpan={5} className="py-4 text-center text-gray-500">
                No hay usuarios registrados.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}