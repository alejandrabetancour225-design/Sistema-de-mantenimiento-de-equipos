import { useState } from "react";
import { Bell } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../services/api";

interface NotificationItem {
  id: string;
  title: string;
  body: string;
  link?: string | null;
  createdAt: string;
  readAt?: string | null;
}

// Campana de notificaciones (recordatorios de mantenimiento, contratos por vencer...).
export default function NotificationBell() {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data } = useQuery({
    queryKey: ["notifications"],
    queryFn: async () => (await api.get<{ unread: number; items: NotificationItem[] }>("/notifications")).data,
    refetchInterval: 120_000, // respaldo por si SignalR se desconecta
  });
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["notifications"] });
  const markRead = useMutation({ mutationFn: (id: string) => api.post(`/notifications/${id}/Read`), onSuccess: invalidate });
  const markAll = useMutation({ mutationFn: () => api.post("/notifications/ReadAll"), onSuccess: invalidate });

  const unread = data?.unread ?? 0;
  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="relative rounded-lg p-2 text-gray-300 hover:bg-white/5 hover:text-white"
        aria-label={`Notificaciones${unread ? ` (${unread} sin leer)` : ""}`}
      >
        <Bell size={18} />
        {unread > 0 && (
          <span className="absolute -right-0.5 -top-0.5 rounded-full bg-red-500 px-1.5 text-[10px] font-semibold text-white">
            {unread > 9 ? "9+" : unread}
          </span>
        )}
      </button>
      {open && (
        <div className="absolute left-0 z-30 mt-2 w-80 rounded-lg border border-white/10 bg-[#111b30] shadow-xl">
          <div className="flex items-center justify-between border-b border-white/10 px-3 py-2">
            <span className="text-sm font-medium text-white">Notificaciones</span>
            {unread > 0 && (
              <button onClick={() => markAll.mutate()} className="text-xs text-blue-400">Marcar todas leídas</button>
            )}
          </div>
          <ul className="max-h-96 overflow-y-auto">
            {(data?.items ?? []).map((n) => (
              <li key={n.id}>
                <button
                  onClick={() => {
                    if (!n.readAt) markRead.mutate(n.id);
                    setOpen(false);
                    if (n.link) navigate(n.link);
                  }}
                  className={`block w-full px-3 py-2 text-left text-xs hover:bg-white/5 ${n.readAt ? "text-gray-400" : "text-white"}`}
                >
                  <span className="block font-medium">{n.title}</span>
                  <span className="block">{n.body}</span>
                  <span className="block text-[10px] text-gray-500">{new Date(n.createdAt).toLocaleString()}</span>
                </button>
              </li>
            ))}
            {data?.items.length === 0 && <li className="px-3 py-4 text-center text-xs text-gray-400">Sin notificaciones.</li>}
          </ul>
        </div>
      )}
    </div>
  );
}
