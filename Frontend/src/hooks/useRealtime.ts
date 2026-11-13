import { useEffect } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useAuth } from "../context/authContext";

// Conexión SignalR mientras hay sesión (R3):
//  - "notificacion": llega un aviso nuevo a la campana.
//  - "datosCambiaron": alguien de la organización modificó datos; se refrescan las consultas abiertas.
export function useRealtime() {
  const { token, role } = useAuth();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!token || role === "SuperAdmin") return;

    const connection = new HubConnectionBuilder()
      .withUrl("/api/hubs/notificaciones", { accessTokenFactory: () => localStorage.getItem("token") ?? "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    let refreshTimer: ReturnType<typeof setTimeout> | undefined;
    connection.on("notificacion", () => queryClient.invalidateQueries({ queryKey: ["notifications"] }));
    connection.on("datosCambiaron", () => {
      // Agrupa ráfagas de cambios en un solo refresco.
      clearTimeout(refreshTimer);
      refreshTimer = setTimeout(() => queryClient.invalidateQueries(), 400);
    });

    connection.start().catch(() => {
      /* sin tiempo real (p. ej. API reiniciando); se reintenta al volver a montar */
    });
    return () => {
      clearTimeout(refreshTimer);
      void connection.stop();
    };
  }, [token, role, queryClient]);
}
