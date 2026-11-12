# Flujos por rol – Sistema de Mantenimiento de Equipos

Qué puede hacer cada tipo de usuario, dónde lo hace (pantalla del menú) y para qué sirve.
Las reglas se aplican en la API: si un rol no puede hacer algo, la API lo rechaza aunque alguien
intente llamarla directamente.

## Roles

| Rol | Quién es | Alcance |
| --- | --- | --- |
| Visitante | Persona sin sesión | Portada, inicio de sesión y recuperación de contraseña |
| SuperAdmin | Administrador de la plataforma | Empresas (organizaciones) y sus planes. **No ve datos de las empresas** |
| Administrador | Responsable de TI de una empresa | Todo dentro de su empresa |
| Técnico | Personal de soporte | Equipos, mantenimientos, repuestos, diagnóstico con IA |
| Empleado | Usuario final de los equipos | Solo sus equipos asignados: consultar y reportar fallas |
| Gerente (antes Cliente) | Directivo | Dashboard gerencial, reportes, historial y alquileres (consulta) |

Cada empresa solo ve sus propios datos. Su **plan** define qué módulos tiene:
- **Básico**: todo lo operativo y el diagnóstico con IA.
- **Integraciones**: lo anterior más bases de datos de terceros, reportes con IA e información de mercado.

---

## Flujos comunes a todos los usuarios con sesión

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Iniciar sesión | Portada → Iniciar sesión | Entrar con correo y contraseña. Tras 5 intentos fallidos la cuenta se bloquea 15 minutos. |
| Cambiar la contraseña temporal | Pantalla obligatoria al primer ingreso | Las cuentas creadas o restablecidas por un Administrador traen contraseña temporal; mientras no se cambie, no se puede hacer nada más. |
| Recuperar la contraseña | Inicio de sesión → "¿Olvidaste tu contraseña?" | Recibir por correo un código de un solo uso (15 minutos) y poner una contraseña nueva. |
| Cambiar la contraseña | Menú → Cambiar contraseña | Cambio voluntario; cierra las demás sesiones. |
| Ver notificaciones | Campana del menú | Avisos de mantenimientos programados (7 días antes, 1 día antes y vencidos), contratos por vencer y reportes listos. Al hacer clic se va a la pantalla relacionada. |
| Ver el calendario | Menú → Calendario | Mantenimientos preventivos programados y sus repeticiones, mantenimientos en curso y completados y vencimientos de contratos. |
| Cerrar sesión | Menú → Cerrar sesión | Invalida todas las sesiones del usuario. |
| Actualización en tiempo real | Automático | Cuando alguien de la empresa cambia datos, el dashboard, el calendario y las listas se refrescan solos. |

---

## SuperAdmin (plataforma)

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Ver organizaciones | Organizaciones | Lista de empresas con su plan, estado y cuántos usuarios y equipos tienen (sin ver sus datos). |
| Crear organización | Organizaciones → formulario | Da de alta una empresa con su plan y su **primer Administrador**, que recibe una contraseña temporal. |
| Cambiar el plan | Organizaciones → selector de plan | Activa o quita módulos (Básico ⇄ Integraciones). Sin pagos: es una asignación manual. |
| Activar o desactivar una empresa | Organizaciones → botón de estado | Al desactivarla, sus usuarios no pueden entrar y sus sesiones se cierran al instante. |

---

## Administrador

Tiene todo lo del **Técnico** y lo del **Gerente**, además de lo siguiente:

### Usuarios y control

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Crear usuarios | Usuarios → formulario | Alta de Administradores, Técnicos, Empleados o Gerentes de su empresa con contraseña temporal (no hay registro público). |
| Cambiar rol o activar/desactivar | Usuarios → tabla | Ajustar permisos. No puede quitarse el rol a sí mismo ni dejar la empresa sin Administrador activo. |
| Restablecer contraseña | Usuarios → "Restablecer contraseña" | Para quien la olvidó y no tiene acceso al correo; queda como temporal. |
| Consultar la auditoría | Auditoría | Quién hizo qué y cuándo, desde qué IP, con el **valor anterior y el nuevo** de cada campo cambiado. |

### Acciones exclusivas sobre equipos y mantenimientos

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Dar de baja un equipo propio | Equipos → "Dar de baja" | Retirarlo definitivamente conservando su historial. Solo si está Disponible o Fuera de servicio, sin asignación ni mantenimiento abierto. |
| Corregir código interno o número de serie | Editar equipo | Arreglar identificadores mal digitados (el Técnico no puede). |
| Anular un mantenimiento cerrado | Mantenimientos → detalle → "Anular" | Si un mantenimiento cerrado tenía errores: queda en el historial con el motivo, fuera de costos y reportes, y se registra uno nuevo correcto. |
| Devolver equipos alquilados | Detalle del equipo / Alquiler → "Devolver todo" | Entregar al arrendador un equipo o todos los de un contrato. |
| Revisar recordatorios ya | API `POST /api/notifications/RunReminders` | Forzar la revisión de avisos sin esperar la pasada de cada hora (útil en demostraciones). |

### Integraciones (plan Integraciones)

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Analizar una fuente | Integraciones → 1. Fuente de datos | Subir un CSV, Excel o JSON, o conectar (solo lectura) a PostgreSQL, MySQL/MariaDB, SQL Server o MongoDB. El sistema perfila las columnas: tipo, vacíos, unicidad y ejemplos. |
| Confirmar el mapeo | Integraciones → 3. Confirma el mapeo | Revisar qué columna corresponde a código, serie, tipo, marca, etc. Viene propuesto con un porcentaje de confianza. |
| Importar | "Guardar e importar" | Crea los equipos. Las columnas sin equivalente quedan como "atributos adicionales". Informa nuevos, actualizados y omitidos con su motivo (por ejemplo, fecha futura o código repetido). |
| Sincronizar | Fuentes conectadas → "Sincronizar" o automático cada N horas | Traer los cambios de la base de la empresa; actualiza sin duplicar. |
| Corregir datos importados | En la base de origen | Los campos sincronizados se ven bloqueados en la app: la verdad está en la fuente. |

### Reportar fallas como un empleado

El Administrador también puede usar "Reportar una falla" y reportar incidencias, igual que el Empleado.

---

## Técnico

### Equipos

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Consultar y filtrar | Equipos | Buscar por código, serie, marca o modelo y filtrar por estado, tipo y ubicación. |
| Registrar un equipo | Equipos → "+ Nuevo equipo" | Alta con datos, características (RAM, disco…) y modalidad **Propio** o **Alquilado** (con su contrato). La fecha de compra no puede ser futura ni la garantía anterior a la compra. |
| Corregir datos | Equipos → Editar | Cualquier dato, incluso vaciar fechas o precio. Todo queda auditado. |
| Cambiar el estado a mano | Clic sobre el estado | Solo **Fuera de servicio ⇄ en servicio**. Los demás estados los ponen las asignaciones, los mantenimientos, la baja y la devolución. |
| Componentes | Detalle del equipo → Componentes | Registrar partes internas (RAM, disco…); la IA las usa para diagnosticar. |
| Historial | Detalle → historial | Línea de tiempo de mantenimientos e incidencias del equipo. |
| Mercado y guías (plan Integraciones) | Detalle → "Buscar precio y guías" | Precio de reemplazo en Colombia y guías de mantenimiento o reparación, con fuente y fecha. |

### Asignaciones

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Asignar | Asignaciones → "Asignar equipo" | Entregar un equipo Disponible a un empleado (pasa a **En uso**). No se asigna a Gerentes. |
| Liberar | Asignaciones → liberar | Recibir el equipo de vuelta (pasa a **Disponible**). |

### Mantenimientos

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Registrar | Mantenimientos → nuevo | Preventivo o correctivo, con técnico, problema, costos de mano de obra y otros. El equipo pasa a **En mantenimiento**. |
| Atender una incidencia | Detalle del equipo → incidencia → crear mantenimiento | Abre un correctivo ligado a la incidencia reportada. |
| Agregar repuestos | Detalle del mantenimiento | Descuenta el costo unitario vigente del catálogo. |
| Editar o cancelar | Detalle → "Editar / cambiar estado" | Ajustar datos o cancelar; cancelar devuelve el equipo a servicio y reabre la incidencia. |
| Cerrar | Detalle → "Cerrar mantenimiento" | Registrar el trabajo realizado y si el equipo vuelve a servicio o queda Fuera de servicio. Si es preventivo y hay plan, calcula la próxima fecha. |
| Equipo alquilado | — | El correctivo **no** se hace internamente: se envía un reclamo al arrendador. El preventivo, solo si el contrato lo permite. |

### Cronograma

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Programar un plan | Detalle del equipo → "Programar" | Preventivo cada N semanas, con responsable y primera fecha. |
| Pausar o reactivar | Detalle del equipo → plan | Suspender temporalmente el plan. |
| Reprogramar | Calendario → arrastrar el evento | Mover la próxima fecha. |
| Iniciar desde el calendario | Calendario → clic → "Iniciar mantenimiento" | Abre el preventivo ligado al plan. |

### Repuestos

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Catálogo | Repuestos | Crear, editar, desactivar o reactivar repuestos con su costo unitario. |

### Alquiler

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Arrendadores | Alquiler → Arrendadores | Empresas que alquilan equipos, con el correo de contacto adonde llegan los reclamos. |
| Contratos | Alquiler → Contratos | Número, vigencia, tarifa mensual por tipo de equipo y si permite preventivo interno. Muestra el costo mensual y avisa cuando vence. |
| Reclamo por falla | Detalle del equipo alquilado → "Enviar reclamo" | Comunicar la falla al arrendador por correo y hacerle seguimiento: Abierto → Respondido → Resuelto. |
| Equipo de reemplazo | Detalle del equipo devuelto → "Registrar equipo de reemplazo" | Dar de alta el equipo que envía el arrendador, vinculado al devuelto. |

### IA

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Diagnóstico con IA | Diagnóstico IA | Escribir el síntoma (también en lenguaje informal) y recibir 3 causas probables en español con probabilidad y recomendación. **La IA solo sugiere: no cambia nada.** |
| Retroalimentar a la IA | Resultado del diagnóstico → feedback | Confirmar la causa correcta o escribir la real. Alimenta la biblioteca y las métricas (aprendizaje continuo). |
| Biblioteca de errores | Biblioteca IA | Errores conocidos (síntoma, causas, solución, severidad) que la IA consulta. |
| Glosario de modismos | Glosario de modismos | Agregar expresiones de la empresa ("se emputó" → "no responde"). Las 102 base son de solo lectura. |
| Métricas de la IA | Métricas IA | Porcentaje de acierto y retroalimentación reciente. |
| Riesgo de falla | Dashboard | Equipos con mayor riesgo según su historial; desde ahí se crea un preventivo. |

---

## Empleado

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Ver su dashboard | Dashboard | Sus equipos asignados y sus próximos mantenimientos. |
| Ver sus equipos | Equipos | **Solo** los que tiene asignados, con su detalle, componentes e historial. |
| Ver sus asignaciones | Asignaciones | Qué tiene a su cargo y desde cuándo. |
| Reportar una falla con sus palabras | Reportar una falla | Chat que entiende lenguaje cotidiano ("se fundió", "se pega"), hace hasta 3 preguntas, da **consejos seguros** (sin abrir el equipo) y arma un resumen técnico. Con "Enviar reporte al técnico" se crea la incidencia: la decide el empleado, no la IA. |
| Reportar una incidencia directa | Detalle del equipo → incidentes | Describir la falla sin el asistente. |
| Editar o cerrar su incidencia | Detalle del equipo | Solo las suyas, mientras estén abiertas y sin mantenimiento. |
| Ver mantenimientos y calendario | Mantenimientos / Calendario | Solo los de sus equipos. |
| Consultar glosario y biblioteca | Glosario / Biblioteca IA | Solo lectura. |
| Recibir avisos | Campana y correo | Cuando su equipo tiene un mantenimiento programado. |

---

## Gerente

| Acción | Dónde | Para qué |
| --- | --- | --- |
| Dashboard gerencial | Dashboard | Equipos por estado, incidencias abiertas y próximos mantenimientos de toda la empresa. |
| Reportes | Reportes | Equipos, mantenimientos y costos filtrables por fechas, tipo y estado; descarga en PDF o Excel. |
| Reportes con IA (plan Integraciones) | Reportes con IA | Generar y leer el reporte ejecutivo redactado por IA (cifras, hallazgos, recomendaciones, alertas e información de mercado con fuentes); descargar el PDF. El del mes anterior llega solo el día 1. |
| Historial de mantenimientos | Mantenimientos | Consulta de todo el historial y sus costos. |
| Alquileres | Alquiler | Consultar contratos, costos mensuales y reclamos. |
| Calendario | Calendario | Ver la programación. |
| Mercado (plan Integraciones) | API `/api/market/...` | Precio de reemplazo de un modelo (en la interfaz está en el detalle del equipo, que el Gerente no abre). |

---

## Procesos automáticos del sistema

| Proceso | Cuándo | Para qué |
| --- | --- | --- |
| Recordatorios de mantenimiento | Cada hora (minuto 15) y al arrancar | Avisos a 7 días, a 1 día y vencidos, para el técnico responsable, los administradores y el empleado del equipo (campana y correo). |
| Aviso de contratos | Igual | 30 días antes del vencimiento, a los administradores. |
| Sincronización de integraciones | Cada hora (minuto 40) | Sincroniza las fuentes cuyo intervalo ya se cumplió. |
| Reporte ejecutivo mensual | Día 1 a las 6:00 | Reporte del mes anterior para cada empresa con plan Integraciones. |
| Próxima fecha del plan | Al cerrar un preventivo | Fecha de cierre + N semanas. |
| Estado del equipo | Al asignar, liberar, abrir o cerrar un mantenimiento, dar de baja o devolver | Mantiene el estado siempre coherente (regla única R4). |
| Auditoría | Cada cambio | Registro de quién, cuándo, qué y valores anteriores y nuevos. |
