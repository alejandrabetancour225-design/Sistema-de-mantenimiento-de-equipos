"""Llamadas a Ollama con salida estructurada (JSON Schema) y límites para CPU."""
import json
import os

import httpx

OLLAMA_HOST = os.getenv("OLLAMA_HOST", "http://localhost:11434")
MODEL_NAME = os.getenv("OLLAMA_MODEL", "llama3.2:3b")
OLLAMA_TIMEOUT_SECONDS = float(os.getenv("OLLAMA_TIMEOUT_SECONDS", "170"))
NUM_CTX = int(os.getenv("OLLAMA_NUM_CTX", "2048"))
NUM_THREADS = int(os.getenv("OLLAMA_NUM_THREADS", "0"))  # 0 = que decida Ollama


class ErrorDeModelo(Exception):
    pass


def chat_estructurado(mensajes: list[dict], esquema: dict, modelo: str | None = None,
                      max_tokens: int = 700, temperatura: float = 0.0, num_ctx: int | None = None) -> dict:
    """Envía la conversación a Ollama forzando que la respuesta cumpla el JSON Schema.

    Ollama restringe la generación con una gramática derivada del esquema: el modelo no puede
    producir JSON inválido, así que el backend C# siempre recibe la forma que espera.
    """
    modelo = modelo or MODEL_NAME
    opciones = {"temperature": temperatura, "num_ctx": num_ctx or NUM_CTX, "num_predict": max_tokens}
    if NUM_THREADS > 0:
        opciones["num_thread"] = NUM_THREADS
    payload = {
        "model": modelo,
        "messages": mensajes,
        "stream": False,
        "format": esquema,
        "options": opciones,
    }
    # Los modelos qwen3 "piensan" en voz alta por defecto: se desactiva para ahorrar tiempo y RAM.
    if modelo.startswith("qwen3"):
        payload["think"] = False

    try:
        with httpx.Client(timeout=OLLAMA_TIMEOUT_SECONDS) as client:
            respuesta = client.post(f"{OLLAMA_HOST}/api/chat", json=payload)
            respuesta.raise_for_status()
            datos = respuesta.json()
    except httpx.HTTPStatusError as e:
        raise ErrorDeModelo(f"Ollama respondió {e.response.status_code}: {e.response.text[:300]}") from e
    except httpx.HTTPError as e:
        raise ErrorDeModelo(f"No se pudo contactar a Ollama: {e}") from e

    contenido = datos.get("message", {}).get("content", "")
    try:
        return json.loads(contenido)
    except json.JSONDecodeError as e:
        raise ErrorDeModelo(f"El modelo devolvió JSON inválido: {contenido[:300]}") from e
