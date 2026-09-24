import { Monitor, Wrench, Users, ShieldCheck, Settings } from "lucide-react";

const FEATURES = [
  {
    icon: Monitor,
    title: "Inventario centralizado",
    text: "Registra y consulta todos tus equipos tecnológicos en un solo lugar.",
  },
  {
    icon: Wrench,
    title: "Mantenimiento al día",
    text: "Historial de mantenimientos, fallas y repuestos siempre a la mano.",
  },
  {
    icon: Users,
    title: "Asignaciones claras",
    text: "Sabe en todo momento quién tiene cada equipo y desde cuándo.",
  },
  {
    icon: ShieldCheck,
    title: "Acceso por roles",
    text: "Administrador, Técnico, Empleado y Cliente, cada uno con lo que necesita.",
  },
];

export default function LoginFeatures() {
  return (
    <div className="flex max-w-[450px] flex-col gap-6">
      <div className="hidden items-center gap-2 md:flex">
        <Settings className="h-6 w-6 text-blue-400" />
        <span className="text-lg font-semibold text-white">Sistema de Mantenimiento</span>
      </div>

      {FEATURES.map((f) => (
        <div key={f.title} className="flex gap-3">
          <f.icon className="h-5 w-5 flex-none text-gray-400" />
          <div>
            <p className="font-medium text-white">{f.title}</p>
            <p className="text-sm text-gray-400">{f.text}</p>
          </div>
        </div>
      ))}
    </div>
  );
}