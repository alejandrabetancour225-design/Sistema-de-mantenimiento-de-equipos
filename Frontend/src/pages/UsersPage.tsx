import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useUsers } from "../hooks/useUsers";
import { updateUser, assignRole, createUser, resetUserPassword } from "../services/usersServices";
import type { Role } from "../types/auth";
import { getApiErrorMessage } from "../lib/errors";
import { PASSWORD_RULE_MESSAGE, strongPassword } from "../schemas/authSchemas";
import type { CreateUserRequest } from "../types/user";

const EMPTY_USER: CreateUserRequest = { fullName: "", email: "", role: "Empleado", temporaryPassword: "" };

const ROLES: Role[] = ["Administrador", "Técnico", "Empleado", "Gerente"];

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

  // Alta de usuarios (reemplaza al registro público): contraseña temporal que el usuario cambia al entrar.
  const [newUser, setNewUser] = useState<CreateUserRequest>(EMPTY_USER);
  const [formError, setFormError] = useState("");
  const createMutation = useMutation({
    mutationFn: () => createUser(newUser),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      setNewUser(EMPTY_USER);
    },
  });
  const resetMutation = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) => resetUserPassword(id, password),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
  });

  function submitNewUser(e: React.FormEvent) {
    e.preventDefault();
    setFormError("");
    if (!strongPassword.safeParse(newUser.temporaryPassword).success) {
      setFormError(PASSWORD_RULE_MESSAGE);
      return;
    }
    createMutation.mutate();
  }

  function resetPassword(id: string, name: string) {
    const password = prompt(`Nueva contraseña temporal para ${name} (${PASSWORD_RULE_MESSAGE})`);
    if (!password) return;
    if (!strongPassword.safeParse(password).success) {
      alert(PASSWORD_RULE_MESSAGE);
      return;
    }
    resetMutation.mutate({ id, password });
  }

  if (isLoading) return <p className="p-8">Cargando usuarios...</p>;
  if (error) return <p className="p-8 text-red-600">Error al cargar los usuarios.</p>;

  return (
    <div className="p-8 space-y-4">
      <h1 className="text-xl font-semibold">Gestión de usuarios</h1>

      <form onSubmit={submitNewUser} className="grid gap-2 rounded border border-white/10 p-3 md:grid-cols-5">
        <input
          value={newUser.fullName}
          onChange={(e) => setNewUser({ ...newUser, fullName: e.target.value })}
          placeholder="Nombre completo"
          className="input"
          required
        />
        <input
          type="email"
          value={newUser.email}
          onChange={(e) => setNewUser({ ...newUser, email: e.target.value })}
          placeholder="Correo"
          className="input"
          required
        />
        <select
          value={newUser.role}
          onChange={(e) => setNewUser({ ...newUser, role: e.target.value as Role })}
          className="input"
        >
          {ROLES.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </select>
        <input
          value={newUser.temporaryPassword}
          onChange={(e) => setNewUser({ ...newUser, temporaryPassword: e.target.value })}
          placeholder="Contraseña temporal"
          className="input"
          required
        />
        <button
          type="submit"
          disabled={createMutation.isPending}
          className="rounded bg-blue-600 px-3 py-2 text-white disabled:opacity-50"
        >
          Crear usuario
        </button>
        <p className="text-xs text-gray-400 md:col-span-5">
          El usuario deberá cambiar la contraseña temporal la primera vez que inicie sesión. Compártela por un
          canal privado.
        </p>
        {(formError || createMutation.error) && (
          <p className="text-sm text-red-500 md:col-span-5">
            {formError || getApiErrorMessage(createMutation.error)}
          </p>
        )}
      </form>

      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2">Nombre</th>
            <th>Correo</th>
            <th>Rol</th>
            <th>Estado</th>
            <th>Intentos fallidos</th>
            <th></th>
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
              <td className="text-right text-xs">
                {user.mustChangePassword && <span className="mr-2 text-amber-500">Clave temporal</span>}
                <button onClick={() => resetPassword(user.id, user.fullName)} className="text-blue-500">
                  Restablecer contraseña
                </button>
              </td>
            </tr>
          ))}
          {data?.length === 0 && (
            <tr>
              <td colSpan={6} className="py-4 text-center text-gray-500">
                No hay usuarios registrados.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}