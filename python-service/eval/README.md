# Evaluación de modelos de IA (español + JSON estricto)

Compara modelos de Ollama con el **mismo prompt y el mismo JSON Schema** que usa el servicio,
sobre [sintomas.json](sintomas.json): 24 fallas descritas en lenguaje informal colombiano.

Mide por modelo:

| Columna | Qué significa |
| --- | --- |
| JSON válido | La respuesta cumple el esquema que espera el backend C#. |
| En español | El texto de la respuesta está en español (heurística de palabras frecuentes). |
| Causas relevantes | Las causas mencionan algo de la categoría esperada (p. ej. "batería" para "no coge carga"). |
| Seguridad | Ante humo, olor a quemado o líquido derramado, recomienda desconectar. |
| Mediana / Máx | Segundos por respuesta, en CPU. |
| RAM Ollama | Memoria del contenedor de Ollama con el modelo cargado. |

## Cómo correrla

Con el sistema levantado (`./iniciar.sh`) y los modelos descargados
(`docker exec sm-ollama ollama pull qwen2.5:3b`, etc.), desde la raíz del repositorio:

```bash
docker run --rm --network host \
  -v "$PWD/python-service:/repo/python-service:z" \
  -v "$PWD/src/Backend.API/Data/Seed:/repo/src/Backend.API/Data/Seed:ro,z" \
  -w /repo/python-service -e OLLAMA_HOST=http://localhost:11434 -e OLLAMA_NUM_THREADS=4 \
  -e OLLAMA_TIMEOUT_SECONDS=300 python:3.11-slim \
  sh -c "pip install -q -r requirements.txt && python -m eval.evaluar qwen2.5:3b llama3.2:3b"
```

`OLLAMA_NUM_THREADS=4` imita un portátil típico de 8 GB (4 núcleos). Los resultados quedan en
`eval/resultados/` (resumen en `resumen.md` y cada respuesta en un JSON por modelo).
