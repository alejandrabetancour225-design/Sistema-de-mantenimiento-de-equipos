"""Servicio Python del sistema de mantenimiento: diagnóstico y triaje con IA local (Ollama).

R5: la IA solo sugiere. Nunca cambia estados ni crea registros; eso lo decide una persona en la API.
"""
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
