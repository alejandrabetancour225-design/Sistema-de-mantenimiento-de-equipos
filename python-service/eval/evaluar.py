"""Evaluación comparativa de modelos para el diagnóstico en español (Sprint 6, fase 2).

No es una suite de pruebas: corre el banco de síntomas contra cada modelo con el MISMO prompt y
el MISMO JSON Schema que usa el servicio, y genera una tabla comparativa.

Uso (desde python-service/, con Ollama levantado en localhost:11434):
    python3 -m eval.evaluar qwen2.5:3b llama3.2:3b qwen3.5:4b gemma3:4b

Criterios de corte acordados: 100 % JSON válido, siempre en español, causas razonables y
<= 180 s por respuesta en CPU, con el contenedor de Ollama limitado a ~3,5 GB de RAM.
"""
import json
import statistics
import sys
import time
from pathlib import Path

from app.main import DiagnosisRequest, DiagnosisResponse, EquipmentContext
from app.ollama_cliente import ErrorDeModelo, chat_estructurado
from app.prompts import mensajes_diagnostico
from app.texto import aplicar_glosario, normalizar, proporcion_espanol

RAIZ = Path(__file__).resolve().parent
GLOSARIO = json.loads((RAIZ.parents[1] / "src/Backend.API/Data/Seed/glosario_colombia.json").read_text("utf-8"))
CASOS = json.loads((RAIZ / "sintomas.json").read_text("utf-8"))
SALIDA = RAIZ / "resultados"

PALABRAS_CATEGORIA = {
    "energía": ["fuente", "alimentaci", "electric", "bateria", "cargador", "tarjeta madre", "placa", "corto"],
    "temperatura": ["temperatura", "sobrecalent", "ventilador", "pasta termica", "polvo", "disipador"],
    "rendimiento": ["memoria", "ram", "disco", "proces", "malware", "virus", "inicio", "almacenamiento", "cpu"],
    "sistema": ["controlador", "driver", "ram", "memoria", "disco", "actualiz", "windows", "sistema operativo"],
    "pantalla": ["pantalla", "panel", "cable", "flex", "video", "grafic", "lcd", "monitor", "display"],
    "batería": ["bateria", "cargador", "carga", "puerto", "adaptador"],
    "red": ["wifi", "red", "inalambr", "controlador", "adaptador", "router", "conexion"],
    "físico": ["liquido", "derrame", "desconect", "humedad", "teclado", "secar"],
    "ruido": ["ventilador", "disco", "rodamiento", "polvo"],
    "arranque": ["ram", "memoria", "bios", "video", "grafic", "pitido"],
    "audio": ["audio", "sonido", "controlador", "parlante", "altavoz", "volumen", "dispositivo"],
    "seguridad": ["malware", "adware", "virus", "antivirus", "extension", "navegador"],
    "impresión": ["cartucho", "tinta", "cabezal", "toner", "papel", "rodillo", "bandeja", "impresora"],
    "periféricos": ["teclado", "tecla", "controlador", "conexion", "cable"],
}
RIESGO_ELECTRICO = {"físico", "energía"}


def memoria_ollama(modelo: str) -> str:
    """Memoria que ocupa el modelo cargado, según /api/ps de Ollama."""
    import httpx
    from app.ollama_cliente import OLLAMA_HOST
    try:
        for m in httpx.get(f"{OLLAMA_HOST}/api/ps", timeout=10).json().get("models", []):
            if m.get("name") == modelo or m.get("model") == modelo:
                return f"{m.get('size', 0) / 1024**3:.2f} GB"
    except Exception:
        pass
    return "?"


def evaluar_modelo(modelo: str) -> dict:
    SALIDA.mkdir(exist_ok=True)
    esquema = DiagnosisResponse.model_json_schema()
    filas, tiempos = [], []

    # Carga del modelo en RAM (no cuenta en el tiempo por respuesta).
    t0 = time.time()
    try:
        chat_estructurado([{"role": "user", "content": "Responde OK."}],
                          {"type": "object", "properties": {"ok": {"type": "string"}}, "required": ["ok"]},
                          modelo=modelo, max_tokens=60)
    except ErrorDeModelo as e:
        return {"modelo": modelo, "error": str(e)}
    carga = time.time() - t0

    for caso in CASOS:
        req = DiagnosisRequest(
            equipment_id="eval", symptom=caso["sintoma"],
            equipment_context=EquipmentContext(brand=caso["marca"], model=caso["modelo"]),
            knowledge_context=[], glossary=[],
        )
        req.glossary = GLOSARIO
        inicio = time.time()
        fila = {"id": caso["id"], "sintoma": caso["sintoma"], "categoria": caso["categoria_esperada"]}
        try:
            datos = chat_estructurado(mensajes_diagnostico(req), esquema, modelo=modelo, max_tokens=700)
            respuesta = DiagnosisResponse.model_validate(datos)
            fila["json_valido"] = True
            texto = " ".join([respuesta.reasoning] + [c.cause + " " + c.recommendation for c in respuesta.ranked_causes])
            fila["espanol"] = proporcion_espanol(texto) >= 0.5
            palabras = PALABRAS_CATEGORIA.get(caso["categoria_esperada"])
            fila["relevante"] = True if not palabras else any(p in normalizar(texto) for p in palabras)
            if caso["categoria_esperada"] in RIESGO_ELECTRICO and any(
                    k in normalizar(caso["sintoma"]) for k in ("humo", "cafe", "quemado")):
                fila["seguridad_ok"] = "desconect" in normalizar(respuesta.ranked_causes[0].recommendation + texto)
            fila["respuesta"] = respuesta.model_dump()
        except (ErrorDeModelo, ValueError) as e:
            fila.update(json_valido=False, espanol=False, relevante=False, error=str(e)[:300])
        fila["segundos"] = round(time.time() - inicio, 1)
        tiempos.append(fila["segundos"])
        filas.append(fila)
        print(f"  [{modelo}] caso {caso['id']:>2}: {fila['segundos']:>6}s json={fila['json_valido']} "
              f"es={fila.get('espanol')} relevante={fila.get('relevante')}", flush=True)

    memoria = memoria_ollama(modelo)
    (SALIDA / f"{modelo.replace(':', '_')}.json").write_text(
        json.dumps(filas, ensure_ascii=False, indent=1), "utf-8")

    n = len(filas)
    seguridad = [f["seguridad_ok"] for f in filas if "seguridad_ok" in f]
    return {
        "modelo": modelo,
        "carga_s": round(carga, 1),
        "json_valido": f"{sum(f['json_valido'] for f in filas)}/{n}",
        "espanol": f"{sum(bool(f.get('espanol')) for f in filas)}/{n}",
        "relevante": f"{sum(bool(f.get('relevante')) for f in filas)}/{n}",
        "seguridad": f"{sum(seguridad)}/{len(seguridad)}",
        "mediana_s": statistics.median(tiempos),
        "max_s": max(tiempos),
        "memoria_ollama": memoria,
        "glosario_detectado": f"{sum(bool(aplicar_glosario(c['sintoma'], GLOSARIO)) for c in CASOS)}/{n}",
    }


def main(modelos: list[str]):
    # Se combina con corridas anteriores: repetir un modelo reemplaza solo su fila.
    previo = SALIDA / "resumen.json"
    resumen = json.loads(previo.read_text("utf-8")) if previo.exists() else []
    for modelo in modelos:
        print(f"== {modelo}", flush=True)
        resultado = evaluar_modelo(modelo)
        resumen = [r for r in resumen if r["modelo"] != modelo] + [resultado]
    tabla = ["| Modelo | JSON válido | En español | Causas relevantes | Seguridad | Mediana (s) | Máx (s) | Carga (s) | RAM Ollama |",
             "| --- | --- | --- | --- | --- | --- | --- | --- | --- |"]
    for r in resumen:
        if "error" in r:
            tabla.append(f"| {r['modelo']} | error: {r['error'][:80]} | | | | | | | |")
        else:
            tabla.append(f"| {r['modelo']} | {r['json_valido']} | {r['espanol']} | {r['relevante']} | {r['seguridad']} "
                         f"| {r['mediana_s']} | {r['max_s']} | {r['carga_s']} | {r['memoria_ollama']} |")
    (SALIDA / "resumen.md").write_text("\n".join(tabla) + "\n", "utf-8")
    (SALIDA / "resumen.json").write_text(json.dumps(resumen, ensure_ascii=False, indent=1), "utf-8")
    print("\n".join(tabla))


if __name__ == "__main__":
    main(sys.argv[1:] or ["qwen2.5:3b", "llama3.2:3b", "qwen3.5:4b", "gemma3:4b"])
