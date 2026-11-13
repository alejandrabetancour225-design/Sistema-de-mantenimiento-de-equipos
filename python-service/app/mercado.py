"""Información de mercado 100 % gratuita (Q6, Q29): búsqueda web sin claves ni cobros.

1. ddgs (metabuscador gratuito, sin clave): prueba varios motores en orden porque alguno
   puede limitar las peticiones en un momento dado.
2. Si ddgs falla y hay una instancia SearXNG configurada (SEARXNG_URL), se usa como respaldo.
3. Si nada responde, se devuelve una lista vacía: el reporte se genera igual con la base de
   conocimiento propia y lo indica.
Cada resultado conserva su fuente (URL) y la fecha de consulta.
"""
import os
from datetime import datetime, timezone

import httpx

SEARXNG_URL = os.getenv("SEARXNG_URL", "").rstrip("/")
_BACKENDS = ("bing", "duckduckgo", "brave", "mojeek", "yahoo")


def _ddgs(consulta: str, max_resultados: int) -> list[dict]:
    from ddgs import DDGS
    for backend in _BACKENDS:
        try:
            resultados = DDGS(timeout=15).text(consulta, region="co-es", backend=backend, max_results=max_resultados)
            if resultados:
                return [{"title": r.get("title", ""), "url": r.get("href", ""), "snippet": r.get("body", ""),
                         "engine": backend} for r in resultados if r.get("href")]
        except Exception:
            continue
    return []


def _searxng(consulta: str, max_resultados: int) -> list[dict]:
    if not SEARXNG_URL:
        return []
    try:
        respuesta = httpx.get(f"{SEARXNG_URL}/search", params={"q": consulta, "format": "json", "language": "es-CO"},
                              timeout=20)
        respuesta.raise_for_status()
        return [{"title": r.get("title", ""), "url": r.get("url", ""), "snippet": r.get("content", ""),
                 "engine": "searxng"} for r in respuesta.json().get("results", [])[:max_resultados] if r.get("url")]
    except Exception:
        return []


def buscar(consultas: list[str], max_resultados: int = 4) -> dict:
    fecha = datetime.now(timezone.utc).isoformat(timespec="seconds")
    salida, vistos = [], set()
    for consulta in consultas[:8]:
        resultados = _ddgs(consulta, max_resultados) or _searxng(consulta, max_resultados)
        for r in resultados:
            if r["url"] in vistos:
                continue
            vistos.add(r["url"])
            salida.append({**r, "query": consulta, "retrieved_at": fecha,
                           "snippet": (r["snippet"] or "")[:400], "title": (r["title"] or "")[:200]})
    return {"results": salida, "retrieved_at": fecha, "available": bool(salida)}
