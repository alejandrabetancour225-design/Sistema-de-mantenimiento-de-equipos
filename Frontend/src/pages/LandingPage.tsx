import { useRef } from "react";
import { Link } from "react-router-dom";
import {
  AlertTriangle,
  ArrowRight,
  ArrowUp,
  Boxes,
  Brain,
  ClipboardList,
  FileBarChart,
  Package,
  ShieldCheck,
  UserCheck,
  Users,
  Wrench,
} from "lucide-react";

// Opcional: pega aquí la URL del repositorio para mostrar el ícono de GitHub en el pie.
const GITHUB_URL = "";

const FEATURES = [
  { icon: Boxes, title: "Inventario de equipos", text: "Registra cada equipo con su serie, ubicación, garantía y estado, siempre actualizado." },
  { icon: UserCheck, title: "Asignaciones", text: "Sabe en todo momento quién tiene cada equipo y libéralo cuando lo devuelva." },
  { icon: AlertTriangle, title: "Incidencias", text: "Los empleados reportan fallas de sus equipos y el área técnica les da seguimiento." },
  { icon: Wrench, title: "Mantenimientos", text: "Preventivos y correctivos con técnico, trabajo realizado, repuestos y costos." },
  { icon: Package, title: "Repuestos y costos", text: "Catálogo de repuestos con costo unitario y costo acumulado por equipo." },
  { icon: FileBarChart, title: "Reportes", text: "Informes gerenciales de equipos, mantenimientos y costos en PDF o Excel." },
  { icon: Brain, title: "Diagnóstico con IA", text: "Sugerencias de causas probables y alertas de equipos con riesgo de falla." },
  { icon: ShieldCheck, title: "Auditoría", text: "Registro de quién hizo qué y cuándo, con control de acceso por rol." },
];

const ROLES = [
  { name: "Administrador", text: "Controla usuarios, equipos, mantenimientos, reportes y la auditoría del sistema." },
  { name: "Técnico", text: "Registra y cierra mantenimientos, gestiona repuestos y cambia el estado de los equipos." },
  { name: "Empleado", text: "Consulta los equipos que tiene asignados y reporta daños o fallas." },
  { name: "Gerente / Cliente", text: "Consulta el dashboard, el historial de mantenimientos, los costos y los reportes." },
];

const STEPS = [
  { n: "1", title: "Registra tus equipos", text: "Da de alta el inventario y asigna cada equipo a quien lo usa." },
  { n: "2", title: "Reporta y atiende", text: "Las fallas se reportan, y el técnico las convierte en mantenimientos con su costo." },
  { n: "3", title: "Analiza y decide", text: "Consulta el historial y los costos para decidir si reparar o reemplazar." },
];

// lucide-react ya no trae íconos de marcas: se dibuja el logo de GitHub a mano.
function GithubMark() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
      <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82a7.6 7.6 0 0 1 4 0c1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8z" />
    </svg>
  );
}

const footerLink = "text-sm text-slate-400 underline decoration-white/20 underline-offset-4 transition hover:text-white hover:decoration-white/60";

function FooterColumn({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="font-medium text-white">{title}</p>
      <ul className="mt-4 space-y-2.5">{children}</ul>
    </div>
  );
}

function FooterLink({ to, children }: { to: string; children: React.ReactNode }) {
  return (
    <li>
      <Link to={to} className={footerLink}>
        {children}
      </Link>
    </li>
  );
}

function FooterButton({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return (
    <li>
      <button onClick={onClick} className={footerLink}>
        {children}
      </button>
    </li>
  );
}

// Vista previa de la aplicación (solo decorativa)
function AppPreview() {
  const bars = [60, 85, 40, 70, 30, 55];
  return (
    <div className="relative">
      <div className="absolute -inset-4 rounded-3xl bg-blue-500/20 blur-3xl" />
      <div className="relative overflow-hidden rounded-2xl border border-white/10 bg-[#0b1226] shadow-2xl shadow-blue-900/40">
        <div className="flex items-center gap-1.5 border-b border-white/10 px-4 py-3">
          <span className="h-2.5 w-2.5 rounded-full bg-red-400/70" />
          <span className="h-2.5 w-2.5 rounded-full bg-yellow-400/70" />
          <span className="h-2.5 w-2.5 rounded-full bg-green-400/70" />
          <span className="ml-3 text-xs text-slate-500">Dashboard</span>
        </div>
        <div className="flex">
          <div className="hidden w-32 space-y-2 border-r border-white/10 p-3 text-xs text-slate-400 sm:block">
            <div className="rounded-md bg-white/10 px-2 py-1.5 text-white">Dashboard</div>
            <div className="px-2 py-1.5">Equipos</div>
            <div className="px-2 py-1.5">Asignaciones</div>
            <div className="px-2 py-1.5">Mantenimientos</div>
            <div className="px-2 py-1.5">Reportes</div>
          </div>
          <div className="flex-1 space-y-3 p-4">
            <div className="grid grid-cols-2 gap-2 md:grid-cols-4">
              {[
                ["Equipos", "128", "text-blue-400"],
                ["Disponibles", "74", "text-green-400"],
                ["En mantenimiento", "9", "text-yellow-400"],
                ["Incidencias", "3", "text-red-400"],
              ].map(([l, v, c]) => (
                <div key={l} className="rounded-lg border border-white/10 bg-white/[0.04] p-2.5">
                  <p className="text-[10px] text-slate-400">{l}</p>
                  <p className={`text-lg font-semibold ${c}`}>{v}</p>
                </div>
              ))}
            </div>
            <div className="rounded-lg border border-white/10 bg-white/[0.04] p-3">
              <p className="mb-2 text-[10px] text-slate-400">Equipos por estado</p>
              <div className="flex h-24 items-end gap-2">
                {bars.map((h, i) => (
                  <div
                    key={i}
                    className="flex-1 rounded-t bg-gradient-to-t from-blue-600 to-cyan-400"
                    style={{ height: `${h}%` }}
                  />
                ))}
              </div>
            </div>
            <div className="rounded-lg border border-yellow-500/30 bg-yellow-500/10 p-2.5 text-xs">
              <span className="font-medium text-white">LAP-001</span>
              <span className="text-yellow-300"> · próximo mantenimiento en 5 días</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function LandingPage() {
  const features = useRef<HTMLElement>(null);
  const roles = useRef<HTMLElement>(null);
  const how = useRef<HTMLElement>(null);

  // La app usa HashRouter, así que los anclajes (#...) no sirven: se baja con scroll.
  const go = (ref: React.RefObject<HTMLElement | null>) =>
    ref.current?.scrollIntoView({ behavior: "smooth", block: "start" });

  return (
    <div className="min-h-screen bg-[#050a18] text-slate-200">
      {/* Navegación */}
      <header className="sticky top-0 z-20 px-4 pt-4">
        <nav className="mx-auto flex max-w-6xl items-center justify-between rounded-2xl border border-white/10 bg-[#0a1226]/80 px-5 py-3 backdrop-blur">
          <Link to="/" className="flex items-center gap-2 font-semibold text-white">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-blue-500 to-cyan-400">
              <Wrench size={16} className="text-white" />
            </span>
            Mantenimiento de Equipos
          </Link>
          <div className="hidden items-center gap-1 text-sm md:flex">
            {[
              ["Funciones", features],
              ["Roles", roles],
              ["Cómo funciona", how],
            ].map(([label, ref]) => (
              <button
                key={label as string}
                onClick={() => go(ref as React.RefObject<HTMLElement>)}
                className="rounded-lg px-3 py-2 text-slate-300 transition hover:bg-white/5 hover:text-white"
              >
                {label as string}
              </button>
            ))}
          </div>
          <div className="flex items-center gap-2 text-sm">
            <Link to="/login" className="rounded-lg px-3 py-2 text-slate-200 hover:text-white">
              Iniciar sesión
            </Link>
            <Link
              to="/register"
              className="rounded-lg bg-slate-100 px-4 py-2 font-medium text-slate-900 transition hover:bg-slate-300"
            >
              Crear cuenta
            </Link>
          </div>
        </nav>
      </header>

      {/* Portada */}
      <section className="relative overflow-hidden">
        <div
          className="pointer-events-none absolute inset-0"
          style={{
            backgroundImage:
              "radial-gradient(ellipse 70% 50% at 80% 0%, rgb(37 99 235 / 0.35), transparent 60%), radial-gradient(ellipse 50% 40% at 0% 80%, rgb(14 116 144 / 0.25), transparent 60%)",
          }}
        />
        <div className="relative mx-auto grid max-w-6xl items-center gap-12 px-6 py-20 lg:grid-cols-2 lg:py-28">
          <div>
            <p className="mb-4 text-xs font-semibold tracking-[0.3em] text-blue-400 uppercase">
              Gestión de mantenimiento
            </p>
            <h1 className="text-4xl leading-tight font-semibold text-white sm:text-5xl">
              Todo el mantenimiento de tus equipos,{" "}
              <span className="bg-gradient-to-r from-blue-400 to-cyan-300 bg-clip-text text-transparent">
                en un solo lugar
              </span>
            </h1>
            <p className="mt-5 max-w-xl text-lg leading-relaxed text-slate-400">
              Controla el inventario, las asignaciones, las fallas y los costos de mantenimiento.
              Decide con información cuándo reparar y cuándo reemplazar.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <Link
                to="/login"
                className="inline-flex items-center gap-2 rounded-lg bg-blue-500 px-6 py-3 font-medium text-white transition hover:bg-blue-600"
              >
                Iniciar sesión <ArrowRight size={16} />
              </Link>
              <Link
                to="/register"
                className="rounded-lg border border-white/20 px-6 py-3 font-medium text-white transition hover:bg-white/5"
              >
                Crear cuenta
              </Link>
            </div>
            <dl className="mt-10 flex gap-8 text-sm">
              {[
                ["4", "roles de usuario"],
                ["PDF y Excel", "reportes"],
                ["IA", "diagnóstico"],
              ].map(([v, l]) => (
                <div key={l}>
                  <dt className="text-xl font-semibold text-white">{v}</dt>
                  <dd className="text-slate-500">{l}</dd>
                </div>
              ))}
            </dl>
          </div>
          <AppPreview />
        </div>
      </section>

      {/* Funciones */}
      <section ref={features} className="mx-auto max-w-6xl scroll-mt-24 px-6 py-20">
        <div className="mx-auto max-w-2xl text-center">
          <p className="text-xs font-semibold tracking-[0.3em] text-blue-400 uppercase">Funciones</p>
          <h2 className="mt-3 text-3xl font-semibold text-white">Lo que necesitas para mantener tus equipos</h2>
          <p className="mt-3 text-slate-400">
            Un sistema completo, desde el registro del equipo hasta el análisis de costos.
          </p>
        </div>
        <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {FEATURES.map(({ icon: Icon, title, text }) => (
            <div
              key={title}
              className="rounded-xl border border-white/10 bg-white/[0.03] p-5 transition hover:border-blue-400/40 hover:bg-white/[0.06]"
            >
              <span className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-blue-500/15 text-blue-400">
                <Icon size={20} />
              </span>
              <h3 className="font-medium text-white">{title}</h3>
              <p className="mt-1 text-sm leading-relaxed text-slate-400">{text}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Roles */}
      <section ref={roles} className="scroll-mt-24 border-y border-white/5 bg-white/[0.02]">
        <div className="mx-auto max-w-6xl px-6 py-20">
          <div className="mx-auto max-w-2xl text-center">
            <p className="text-xs font-semibold tracking-[0.3em] text-blue-400 uppercase">Roles</p>
            <h2 className="mt-3 text-3xl font-semibold text-white">Cada persona ve lo que necesita</h2>
          </div>
          <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {ROLES.map((r, i) => (
              <div key={r.name} className="rounded-xl border border-white/10 bg-[#0a1226] p-5">
                <span className="mb-3 flex h-9 w-9 items-center justify-center rounded-full bg-cyan-400/10 text-cyan-300">
                  {i === 0 ? <ShieldCheck size={18} /> : i === 1 ? <Wrench size={18} /> : i === 2 ? <Users size={18} /> : <ClipboardList size={18} />}
                </span>
                <h3 className="font-medium text-white">{r.name}</h3>
                <p className="mt-1 text-sm leading-relaxed text-slate-400">{r.text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Cómo funciona */}
      <section ref={how} className="mx-auto max-w-6xl scroll-mt-24 px-6 py-20">
        <div className="mx-auto max-w-2xl text-center">
          <p className="text-xs font-semibold tracking-[0.3em] text-blue-400 uppercase">Cómo funciona</p>
          <h2 className="mt-3 text-3xl font-semibold text-white">Tres pasos para tener el control</h2>
        </div>
        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {STEPS.map((s) => (
            <div key={s.n} className="flex gap-4">
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-blue-500 font-semibold text-white">
                {s.n}
              </span>
              <div>
                <h3 className="font-medium text-white">{s.title}</h3>
                <p className="mt-1 text-sm leading-relaxed text-slate-400">{s.text}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Llamado final */}
      <section className="px-6 pb-20">
        <div
          className="mx-auto max-w-4xl rounded-3xl border border-white/10 px-8 py-14 text-center"
          style={{
            backgroundImage:
              "radial-gradient(ellipse at 50% 0%, rgb(37 99 235 / 0.35), transparent 70%)",
          }}
        >
          <h2 className="text-3xl font-semibold text-white">Empieza a controlar tus equipos hoy</h2>
          <p className="mx-auto mt-3 max-w-xl text-slate-400">
            Ingresa con tu cuenta o crea una nueva para consultar tus equipos y mantenimientos.
          </p>
          <div className="mt-8 flex justify-center gap-3">
            <Link to="/login" className="rounded-lg bg-blue-500 px-6 py-3 font-medium text-white transition hover:bg-blue-600">
              Iniciar sesión
            </Link>
            <Link to="/register" className="rounded-lg border border-white/20 px-6 py-3 font-medium text-white transition hover:bg-white/5">
              Crear cuenta
            </Link>
          </div>
        </div>
      </section>

      {/* Pie de página */}
      <footer className="border-t border-white/10 px-6 pt-14 pb-8">
        <div className="mx-auto max-w-6xl">
          <div className="grid gap-10 md:grid-cols-[1.4fr_1fr_1fr_1fr]">
            <div>
              <Link to="/" className="flex items-center gap-2 font-semibold text-white">
                <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-blue-500 to-cyan-400">
                  <Wrench size={16} className="text-white" />
                </span>
                Mantenimiento de Equipos
              </Link>
              <p className="mt-5 font-medium text-white">Empieza hoy</p>
              <p className="mt-1 max-w-xs text-sm text-slate-400">
                Ingresa con tu cuenta o crea una nueva para gestionar tus equipos.
              </p>
            </div>

            <FooterColumn title="Producto">
              <FooterButton onClick={() => go(features)}>Funciones</FooterButton>
              <FooterButton onClick={() => go(how)}>Cómo funciona</FooterButton>
              <FooterButton onClick={() => go(roles)}>Roles</FooterButton>
            </FooterColumn>

         

            
          </div>

          <div className="mt-12 flex flex-wrap items-center justify-between gap-4 border-t border-white/10 pt-6 text-sm text-slate-500">
            <p>© {new Date().getFullYear()} Sistema de Mantenimiento de Equipos </p>
            <div className="flex items-center gap-3">
              {GITHUB_URL && (
                <a
                  href={GITHUB_URL}
                  target="_blank"
                  rel="noreferrer"
                  aria-label="Repositorio en GitHub"
                  className="flex h-9 w-9 items-center justify-center rounded-lg border border-white/10 text-slate-300 transition hover:bg-white/5 hover:text-white"
                >
                  <GithubMark />
                </a>
              )}
              <button
                onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}
                className="flex h-9 items-center gap-1.5 rounded-lg border border-white/10 px-3 text-slate-300 transition hover:bg-white/5 hover:text-white"
              >
                <ArrowUp size={14} /> Volver arriba
              </button>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}