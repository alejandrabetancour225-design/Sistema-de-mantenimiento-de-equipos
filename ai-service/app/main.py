from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from typing import List
import httpx
import os
import json

app = FastAPI(title="AI Service - Diagnóstico de Equipos")

OLLAMA_HOST = os.getenv("OLLAMA_HOST", "http://localhost:11434")
MODEL_NAME = "llama3.1:8b"


class EquipmentContext(BaseModel):
    brand: str
    model: str
    os_info: str | None = None
    components: List[str] = []


class DiagnosisRequest(BaseModel):
    equipment_id: str
    symptom: str
    equipment_context: EquipmentContext
    knowledge_context: List[dict] = []


class RankedCause(BaseModel):
    cause: str = Field(description="Descripción de la causa probable")
    probability: float = Field(ge=0, le=1, description="Probabilidad entre 0 y 1")
    recommendation: str = Field(description="Qué debe hacer el técnico o usuario")


class DiagnosisResponse(BaseModel):
    ranked_causes: List[RankedCause] = Field(description="Lista de causas ordenadas por probabilidad")
    requires_technician: bool = Field(description="True si requiere intervención física")
    reasoning: str = Field(description="Breve explicación del razonamiento")


@app.get("/health")
def health():
    return {"status": "ok", "model": MODEL_NAME}


@app.post("/api/v1/diagnosis", response_model=DiagnosisResponse)
def diagnose(request: DiagnosisRequest):
    knowledge_text = "\n".join([
        f"- Síntoma: {k.get('symptom')} | Causas: {k.get('possibleCauses')} | Solución: {k.get('suggestedSolution')}"
        for k in request.knowledge_context
    ]) or "No hay errores documentados para este síntoma."

    prompt = f"""You are an expert computer repair technician.
Analyze the following case and return a technical diagnosis.

EQUIPMENT DATA:
- Brand: {request.equipment_context.brand}
- Model: {request.equipment_context.model}
- OS: {request.equipment_context.os_info or 'Not available'}
- Components: {', '.join(request.equipment_context.components) or 'Not registered'}

REPORTED SYMPTOM:
{request.symptom}

KNOWN ERROR LIBRARY:
{knowledge_text}

INSTRUCTIONS:
- Use the equipment data (brand, model, OS, components) to filter compatible causes.
- If OS or components are missing ("dato no disponible"), do not block the diagnosis; work with what is available.
- Return a JSON object that strictly follows this schema. The keys MUST be in English exactly as shown:
{{
  "ranked_causes": [
    {{
      "cause": "string describing the probable cause",
      "probability": 0.0,
      "recommendation": "string with the recommended action"
    }}
  ],
  "requires_technician": true,
  "reasoning": "string with a brief explanation"
}}

RULES:
- The field names MUST be exactly: "ranked_causes", "cause", "probability", "recommendation", "requires_technician", "reasoning".
- Do NOT translate field names to Spanish.
- Return exactly 3 causes in "ranked_causes".
- "probability" must be a number between 0 and 1.
"""

    # Esquema exacto que Ollama debe respetar
    schema = DiagnosisResponse.model_json_schema()

    payload = {
        "model": MODEL_NAME,
        "messages": [{"role": "user", "content": prompt}],
        "stream": False,
        "format": schema,  # <-- Esquema directo a la API REST
        "options": {"temperature": 0}
    }

    try:
        # Llamada HTTP directa a Ollama (sin librería)
        with httpx.Client(timeout=120.0) as client:
            response = client.post(f"{OLLAMA_HOST}/api/chat", json=payload)
            response.raise_for_status()
            data = response.json()

        raw = json.loads(raw_content)

        # Remapeo forzado (funciona sin importar lo que devuelva el modelo)
        normalized = {
            "ranked_causes": [
                {
                    "cause": c.get("cause", c.get("causa", "")),
                    "probability": float(c.get("probability", c.get("probabilidad", 0.0))),
                    "recommendation": c.get("recommendation", c.get("recomendación", c.get("recomendacion", "")))
                }
                for c in raw.get("ranked_causes", raw.get("causas", []))
            ],
            "requires_technician": bool(raw.get("requires_technician", raw.get("requiere_tecnico", True))),
            "reasoning": raw.get("reasoning", raw.get("razonamiento", ""))
        }

        return DiagnosisResponse.model_validate(normalized)

    except httpx.HTTPStatusError as e:
        print(f"ERROR HTTP de Ollama: {e.response.text}")
        raise HTTPException(status_code=503, detail=f"Error de Ollama: {e.response.text}")
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=503, detail=f"Error en el servicio de IA: {str(e)}")
