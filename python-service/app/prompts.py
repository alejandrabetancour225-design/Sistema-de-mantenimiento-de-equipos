"""Prompts en español para diagnóstico (técnico) y triaje conversacional (empleado)."""
from .texto import aplicar_glosario

REGLA_IDIOMA = (
    "Responde SIEMPRE en español (Colombia), con frases cortas y claras. "
    "Nunca respondas en inglés, aunque los nombres de los campos JSON estén en inglés."
)


def _bloque_equipo(ctx) -> str:
    componentes = ", ".join(ctx.components) if ctx.components else "no registrados"
    return (
        f"- Marca: {ctx.brand}\n- Modelo: {ctx.model}\n"
        f"- Sistema operativo: {ctx.os_info or 'no disponible'}\n- Componentes: {componentes}"
    )


def _bloque_glosario(texto: str, glosario: list[dict]) -> str:
    coincidencias = aplicar_glosario(texto, glosario)
    if not coincidencias:
        return "No se detectaron expresiones coloquiales conocidas."
    return "\n".join(f'- "{e["expresion"]}" significa: {e["significado"]}' for e in coincidencias)


def mensajes_diagnostico(req) -> list[dict]:
    biblioteca = "\n".join(
        f"- Síntoma: {k.get('symptom')} | Causas: {k.get('possibleCauses')} | Solución: {k.get('suggestedSolution')}"
        for k in req.knowledge_context[:5]
    ) or "No hay errores documentados parecidos."

    sistema = (
        "Eres un técnico experto en soporte y mantenimiento de computadores e impresoras en Colombia. "
        f"{REGLA_IDIOMA} "
        "Los usuarios describen las fallas con palabras informales; usa la interpretación del glosario "
        "para entenderlas. Propón exactamente 3 causas probables, ordenadas de mayor a menor probabilidad, "
        "con probabilidades entre 0 y 1 que sumen aproximadamente 1. Cada causa es una FALLA TÉCNICA concreta "
        "(un componente o el software que falla, p. ej. 'Fuente de poder dañada'), nunca una acción ni un consejo. "
        "Cada recomendación es una acción concreta para el técnico. Solo si el usuario menciona humo, chispas, "
        "olor a quemado o líquido derramado, la recomendación de la primera causa empieza por desconectar el equipo. "
        "No inventes datos del equipo que no se den."
    )
    usuario = (
        f"DATOS DEL EQUIPO:\n{_bloque_equipo(req.equipment_context)}\n\n"
        f"SÍNTOMA REPORTADO (palabras del usuario):\n{req.symptom}\n\n"
        f"INTERPRETACIÓN DE EXPRESIONES COLOQUIALES:\n{_bloque_glosario(req.symptom, req.glossary)}\n\n"
        f"BIBLIOTECA DE ERRORES CONOCIDOS:\n{biblioteca}\n\n"
        "Devuelve el diagnóstico en español."
    )
    return [{"role": "system", "content": sistema}, {"role": "user", "content": usuario}]


def mensajes_triaje(req) -> list[dict]:
    texto_empleado = " ".join(m.content for m in req.messages if m.role == "empleado")
    puede_preguntar = req.questions_asked < req.max_questions

    regla_pregunta = (
        f"Si con lo que sabes no puedes entender bien el problema, haz UNA sola pregunta corta y sencilla "
        f"(sin tecnicismos) en 'question' y pon 'needs_more_info' en true. Ya hiciste {req.questions_asked} "
        f"de máximo {req.max_questions} preguntas."
        if puede_preguntar
        else "Ya no puedes hacer más preguntas: pon 'needs_more_info' en false y 'question' vacía."
    )
    sistema = (
        "Eres el asistente de soporte técnico de la empresa. Hablas con un empleado que NO es técnico y "
        f"describe la falla de su equipo con sus propias palabras. {REGLA_IDIOMA} Trátalo SIEMPRE de usted "
        "(revise, conecte, reinicie), con amabilidad. En 'understanding' NO hagas preguntas: solo resume lo que entendiste. "
        f"{regla_pregunta} "
        "En 'safe_tips' da como máximo 3 consejos SEGUROS que el empleado pueda hacer solo, sin abrir el equipo "
        "ni desarmarlo (por ejemplo: revisar que el cable esté conectado, reiniciar, cerrar programas). "
        "Si hay humo, chispas, olor a quemado o líquido derramado, el primer consejo es desconectarlo y no "
        "volver a encenderlo. En 'technical_summary' escribe un resumen técnico breve para el técnico "
        "(qué falla, desde cuándo, qué ya se intentó). No des diagnósticos definitivos: eso lo hace el técnico."
    )
    contexto = (
        f"EQUIPO DEL EMPLEADO:\n{_bloque_equipo(req.equipment_context)}\n\n"
        f"INTERPRETACIÓN DE EXPRESIONES COLOQUIALES:\n{_bloque_glosario(texto_empleado, req.glossary)}"
    )
    mensajes = [{"role": "system", "content": sistema}, {"role": "system", "content": contexto}]
    for m in req.messages[-8:]:
        mensajes.append({"role": "user" if m.role == "empleado" else "assistant", "content": m.content})
    return mensajes
