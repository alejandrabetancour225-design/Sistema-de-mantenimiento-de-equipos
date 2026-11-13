"""Servicio Python del sistema de mantenimiento: diagnóstico y triaje con IA local (Ollama).

R5: la IA solo sugiere. Nunca cambia estados ni crea registros; eso lo decide una persona en la API.
"""
import json
from typing import List, Literal

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from . import ollama_cliente
from .ollama_cliente import ErrorDeModelo, chat_estructurado
from .prompts import mensajes_diagnostico, mensajes_triaje
from .texto import aplicar_glosario, proporcion_espanol

app = FastAPI(title="Python Service - Sistema de mantenimiento de equipos")

# Palabras que delatan un "consejo" inseguro para alguien sin formación técnica.
_CONSEJOS_PROHIBIDOS = ("abrir", "abra ", "destap", "desarm", "desatornill", "soldar", "carcasa", "tarjeta madre")


# ---------- Modelos compartidos ----------
class EquipmentContext(BaseModel):
    brand: str
    model: str
    os_info: str | None = None
    components: List[str] = []


class GlossaryEntry(BaseModel):
    expresion: str
    significado: str
    categoria: str | None = None


# ---------- Diagnóstico (técnico) ----------
class DiagnosisRequest(BaseModel):
    equipment_id: str
    symptom: str
    equipment_context: EquipmentContext
    knowledge_context: List[dict] = []
    glossary: List[GlossaryEntry] = []


class RankedCause(BaseModel):
    cause: str = Field(description="Causa probable, en español")
    probability: float = Field(ge=0, le=1, description="Probabilidad entre 0 y 1")
    recommendation: str = Field(description="Acción concreta para el técnico, en español")


class DiagnosisResponse(BaseModel):
    ranked_causes: List[RankedCause] = Field(min_length=1, max_length=3,
                                             description="Exactamente 3 causas, de la más a la menos probable")
    requires_technician: bool = Field(description="true si requiere intervención física de un técnico")
    reasoning: str = Field(description="Explicación breve del razonamiento, en español")


class DiagnosisApiResponse(DiagnosisResponse):
    # Lo agrega el servicio (no el modelo).
    model: str
    detected_expressions: List[str] = []


# ---------- Triaje conversacional (empleado) ----------
class ChatMessage(BaseModel):
    role: Literal["empleado", "asistente"]
    content: str = Field(max_length=2000)


class TriageRequest(BaseModel):
    equipment_context: EquipmentContext
    messages: List[ChatMessage] = Field(min_length=1)
    questions_asked: int = 0
    max_questions: int = 3
    glossary: List[GlossaryEntry] = []


class TriageModelResponse(BaseModel):
    understanding: str = Field(description="Lo que entendiste del problema, en palabras sencillas, en español")
    needs_more_info: bool = Field(description="true si necesitas hacer una pregunta más")
    question: str = Field(description="Una sola pregunta corta y sencilla en español, o vacía")
    safe_tips: List[str] = Field(max_length=3, description="Hasta 3 consejos seguros, en español")
    technical_summary: str = Field(description="Resumen técnico breve para el técnico, en español")
    urgency: Literal["baja", "media", "alta"] = Field(description="Urgencia estimada")
    requires_technician: bool = Field(description="true si probablemente necesita un técnico")


class TriageApiResponse(TriageModelResponse):
    model: str
    detected_expressions: List[str] = []


def _llamar(mensajes, esquema_modelo, max_tokens):
    try:
        datos = chat_estructurado(mensajes, esquema_modelo.model_json_schema(), max_tokens=max_tokens)
        return esquema_modelo.model_validate(datos)
    except ErrorDeModelo as e:
        raise HTTPException(status_code=503, detail=str(e)) from e
    except ValueError as e:
        raise HTTPException(status_code=503, detail=f"Respuesta del modelo fuera del esquema: {e}") from e


def _parece_ingles(*textos: str) -> bool:
    return proporcion_espanol(" ".join(textos)) < 0.5


@app.get("/health")
def health():
    return {"status": "ok", "model": ollama_cliente.MODEL_NAME}


@app.post("/api/v1/diagnosis", response_model=DiagnosisApiResponse)
def diagnose(request: DiagnosisRequest):
    glosario = [g.model_dump() for g in request.glossary]
    request.glossary = glosario  # los prompts trabajan con dicts
    mensajes = mensajes_diagnostico(request)
    resultado = _llamar(mensajes, DiagnosisResponse, max_tokens=700)

    # Red de seguridad: si aun así respondió en inglés, se repite una vez exigiendo español.
    if _parece_ingles(resultado.reasoning, *(c.cause for c in resultado.ranked_causes)):
        mensajes.append({"role": "user", "content": "Tu respuesta anterior no estaba en español. Repítela completa en español."})
        resultado = _llamar(mensajes, DiagnosisResponse, max_tokens=700)

    causas = sorted(resultado.ranked_causes, key=lambda c: c.probability, reverse=True)
    return DiagnosisApiResponse(
        ranked_causes=causas,
        requires_technician=resultado.requires_technician,
        reasoning=resultado.reasoning,
        model=ollama_cliente.MODEL_NAME,
        detected_expressions=[e["expresion"] for e in aplicar_glosario(request.symptom, glosario)],
    )


@app.post("/api/v1/triage", response_model=TriageApiResponse)
def triage(request: TriageRequest):
    glosario = [g.model_dump() for g in request.glossary]
    request.glossary = glosario
    resultado = _llamar(mensajes_triaje(request), TriageModelResponse, max_tokens=500)

    # Si el modelo dejó su pregunta dentro de "understanding", se rescata como pregunta.
    if not resultado.question.strip() and "¿" in resultado.understanding and request.questions_asked < request.max_questions:
        inicio = resultado.understanding.index("¿")
        fin = resultado.understanding.find("?", inicio)
        if fin > inicio:
            resultado.question = resultado.understanding[inicio:fin + 1]
            resultado.understanding = (resultado.understanding[:inicio] + resultado.understanding[fin + 1:]).strip()
            resultado.needs_more_info = True

    # Límite de preguntas: lo impone el servicio, no la buena voluntad del modelo.
    if request.questions_asked >= request.max_questions or not resultado.question.strip():
        resultado.needs_more_info = False
        resultado.question = ""
    resultado.safe_tips = [
        t for t in resultado.safe_tips if not any(p in t.lower() for p in _CONSEJOS_PROHIBIDOS)
    ][:3]

    texto_empleado = " ".join(m.content for m in request.messages if m.role == "empleado")
    return TriageApiResponse(
        **resultado.model_dump(),
        model=ollama_cliente.MODEL_NAME,
        detected_expressions=[e["expresion"] for e in aplicar_glosario(texto_empleado, glosario)],
    )


# ---------- Integraciones con bases de terceros (solo lectura) ----------
from . import integraciones  # noqa: E402


class ConnectionInfo(BaseModel):
    host: str | None = None
    port: int | None = None
    database: str | None = None
    user: str | None = None
    password: str | None = None
    sslmode: str | None = None
    uri: str | None = None  # MongoDB


class SourceSpec(BaseModel):
    kind: Literal["file", "postgres", "mysql", "sqlserver", "mongodb"]
    file_name: str | None = None
    file_base64: str | None = None
    connection: ConnectionInfo | None = None
    table: str | None = None


def _leer(spec: SourceSpec, limite: int):
    fuente = spec.model_dump()
    fuente["connection"] = (spec.connection.model_dump() if spec.connection else {})
    try:
        return integraciones.leer_registros(fuente, limite)
    except integraciones.ErrorDeFuente as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@app.post("/api/v1/integrations/profile")
def profile_source(spec: SourceSpec):
    """Lee una muestra, perfila las columnas y propone el mapeo hacia el modelo de equipo."""
    registros = _leer(spec, integraciones.MAX_FILAS_PERFIL)
    if not registros:
        raise HTTPException(status_code=400, detail="La fuente no tiene registros.")
    perfil = integraciones.perfilar(registros)
    sugerencia = integraciones.sugerir_mapeo(perfil)
    return {
        "rows_read": len(registros),
        "columns": perfil,
        "suggestion": sugerencia,
        "duplicate_keys": integraciones.duplicados(registros, sugerencia["key_column"]),
        "preview": integraciones.a_json(registros[:5]),
    }


@app.post("/api/v1/integrations/extract")
def extract_source(spec: SourceSpec):
    """Devuelve los registros de la fuente (máximo 5000) para sincronizarlos en la API."""
    registros = _leer(spec, integraciones.MAX_FILAS)
    return {"rows": integraciones.a_json(registros), "truncated": len(registros) >= integraciones.MAX_FILAS}


# ---------- Información de mercado y reportes generados por IA (plan Integraciones) ----------
from . import mercado  # noqa: E402


class MarketSearchRequest(BaseModel):
    queries: List[str] = Field(min_length=1, max_length=8)
    max_results: int = Field(default=4, ge=1, le=8)


@app.post("/api/v1/market/search")
def market_search(request: MarketSearchRequest):
    return mercado.buscar(request.queries, request.max_results)


class MarketItem(BaseModel):
    title: str
    url: str
    snippet: str = ""
    retrieved_at: str = ""
    query: str = ""


class ExecutiveReportRequest(BaseModel):
    organization: str
    period_from: str
    period_to: str
    stats: dict
    market: List[MarketItem] = []


class Recommendation(BaseModel):
    text: str = Field(description="Recomendación concreta, en español")
    priority: Literal["alta", "media", "baja"]


class MarketNote(BaseModel):
    source_index: int = Field(ge=1, description="Número de la fuente de mercado citada (de la lista dada)")
    note: str = Field(description="Qué dice esa fuente y por qué importa, en español")


class ExecutiveReportModel(BaseModel):
    summary: str = Field(description="Resumen ejecutivo de 3 a 5 frases, en español")
    findings: List[str] = Field(max_length=5, description="Hallazgos con cifras, en español")
    recommendations: List[Recommendation] = Field(max_length=5)
    alerts: List[str] = Field(max_length=4, description="Riesgos que requieren atención, en español")
    market_notes: List[MarketNote] = Field(max_length=5, description="Solo fuentes de la lista dada")


@app.post("/api/v1/reports/executive")
def executive_report(request: ExecutiveReportRequest):
    """Reporte ejecutivo en español a partir de las cifras del periodo y, si hay, fuentes de mercado.
    R5: solo redacta; no cambia datos. Las notas de mercado solo pueden citar las fuentes entregadas."""
    fuentes = "\n".join(
        f"[{i}] {m.title} ({m.url}): {m.snippet[:160]}" for i, m in enumerate(request.market[:6], start=1)
    ) or "No hay fuentes de mercado disponibles: deja market_notes vacío."
    mensajes = [
        {"role": "system", "content": (
            "Eres un analista de gestión de activos de TI que escribe para la gerencia de una empresa colombiana. "
            f"{REGLA_IDIOMA_REPORTE} Usa SOLO las cifras entregadas (no inventes números). Montos en pesos "
            "colombianos (COP). En market_notes cita únicamente fuentes de la lista por su número.")},
        {"role": "user", "content": (
            f"EMPRESA: {request.organization}\nPERIODO: {request.period_from} a {request.period_to}\n\n"
            f"CIFRAS DEL PERIODO (JSON):\n{json.dumps(request.stats, ensure_ascii=False)[:2500]}\n\n"
            f"FUENTES DE MERCADO:\n{fuentes}")},
    ]
    resultado = _llamar_ctx(mensajes, ExecutiveReportModel, max_tokens=900, num_ctx=4096)
    validas = []
    for nota in resultado.market_notes:
        if 1 <= nota.source_index <= min(len(request.market), 6):
            fuente = request.market[nota.source_index - 1]
            validas.append({"note": nota.note, "title": fuente.title, "url": fuente.url,
                            "retrieved_at": fuente.retrieved_at})
    return {**resultado.model_dump(exclude={"market_notes"}), "market_notes": validas, "model": ollama_cliente.MODEL_NAME}


REGLA_IDIOMA_REPORTE = "Escribe SIEMPRE en español (Colombia), en tono profesional y claro."


def _llamar_ctx(mensajes, esquema_modelo, max_tokens, num_ctx):
    try:
        datos = chat_estructurado(mensajes, esquema_modelo.model_json_schema(), max_tokens=max_tokens, num_ctx=num_ctx)
        return esquema_modelo.model_validate(datos)
    except ErrorDeModelo as e:
        raise HTTPException(status_code=503, detail=str(e)) from e
    except ValueError as e:
        raise HTTPException(status_code=503, detail=f"Respuesta del modelo fuera del esquema: {e}") from e
