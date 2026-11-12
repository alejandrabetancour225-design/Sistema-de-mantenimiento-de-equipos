"""Integración con bases de datos de terceros (Sprint 6, fase 6). SOLO LECTURA.

1. Leer registros de la fuente: archivo (CSV, Excel, JSON) o conexión en vivo a PostgreSQL,
   MySQL/MariaDB, SQL Server o MongoDB. Las sesiones SQL se abren en modo de solo lectura y
   solo se ejecuta un SELECT sobre una tabla con nombre validado: nunca se escribe en la fuente.
2. Perfilar los datos (minería de datos ligera, sin pandas para ahorrar RAM): tipo inferido de
   cada columna, nulos, unicidad, ejemplos y patrones.
3. Proponer el mapeo hacia nuestro modelo de equipo (código, serie, tipo, marca, modelo...)
   combinando el nombre de la columna y el contenido. El Administrador lo confirma.
"""
import base64
import csv
import io
import json
import re
from datetime import date, datetime
from typing import Any

from .texto import normalizar

MAX_FILAS = 5000
MAX_FILAS_PERFIL = 2000
_IDENTIFICADOR = re.compile(r"^[A-Za-z_][A-Za-z0-9_]{0,62}(\.[A-Za-z_][A-Za-z0-9_]{0,62})?$")


class ErrorDeFuente(Exception):
    pass


# --------------------------------------------------------------------------- lectura

def leer_registros(fuente: dict, limite: int = MAX_FILAS) -> list[dict]:
    tipo = fuente.get("kind")
    if tipo == "file":
        return _leer_archivo(fuente.get("file_name", ""), fuente.get("file_base64", ""), limite)
    if tipo in ("postgres", "mysql", "sqlserver"):
        return _leer_sql(tipo, fuente.get("connection", {}), fuente.get("table", ""), limite)
    if tipo == "mongodb":
        return _leer_mongo(fuente.get("connection", {}), fuente.get("table", ""), limite)
    raise ErrorDeFuente(f"Tipo de fuente no soportado: {tipo}")


def _leer_archivo(nombre: str, contenido_b64: str, limite: int) -> list[dict]:
    try:
        datos = base64.b64decode(contenido_b64)
    except ValueError as e:
        raise ErrorDeFuente("El archivo no se pudo decodificar.") from e
    extension = nombre.lower().rsplit(".", 1)[-1] if "." in nombre else ""

    if extension in ("csv", "txt"):
        texto = _decodificar(datos)
        try:
            dialecto = csv.Sniffer().sniff(texto[:4096], delimiters=",;\t|")
        except csv.Error:
            dialecto = csv.excel
        return [dict(fila) for _, fila in zip(range(limite), csv.DictReader(io.StringIO(texto), dialect=dialecto))]

    if extension in ("xlsx", "xlsm"):
        from openpyxl import load_workbook
        libro = load_workbook(io.BytesIO(datos), read_only=True, data_only=True)
        hoja = libro.worksheets[0]
        filas = hoja.iter_rows(values_only=True)
        encabezados = [str(c).strip() if c is not None else f"columna_{i + 1}" for i, c in enumerate(next(filas, []))]
        registros = []
        for fila in filas:
            if len(registros) >= limite:
                break
            if fila and any(v is not None for v in fila):
                registros.append({encabezados[i]: v for i, v in enumerate(fila) if i < len(encabezados)})
        return registros

    if extension == "json":
        contenido = json.loads(_decodificar(datos))
        if isinstance(contenido, dict):
            # {"equipos": [...]} o similar: se toma la primera lista de objetos.
            contenido = next((v for v in contenido.values() if isinstance(v, list)), [contenido])
        if not isinstance(contenido, list):
            raise ErrorDeFuente("El JSON debe ser una lista de objetos.")
        return [_aplanar(r) for r in contenido[:limite] if isinstance(r, dict)]

    raise ErrorDeFuente("Formato de archivo no soportado. Usa CSV, Excel (.xlsx) o JSON.")


def _decodificar(datos: bytes) -> str:
    for codificacion in ("utf-8-sig", "latin-1"):
        try:
            return datos.decode(codificacion)
        except UnicodeDecodeError:
            continue
    raise ErrorDeFuente("No se reconoce la codificación del archivo.")


def _aplanar(registro: dict, prefijo: str = "") -> dict:
    """{"a": {"b": 1}} -> {"a.b": 1}: los documentos anidados se vuelven columnas."""
    plano = {}
    for clave, valor in registro.items():
        nombre = f"{prefijo}{clave}"
        if isinstance(valor, dict):
            plano.update(_aplanar(valor, nombre + "."))
        elif isinstance(valor, list):
            plano[nombre] = json.dumps(valor, ensure_ascii=False, default=str)
        else:
            plano[nombre] = valor
    return plano


def _validar_tabla(tabla: str) -> str:
    if not _IDENTIFICADOR.match(tabla or ""):
        raise ErrorDeFuente("Nombre de tabla inválido (usa letras, números y _; opcionalmente esquema.tabla).")
    return tabla


def _leer_sql(tipo: str, con: dict, tabla: str, limite: int) -> list[dict]:
    tabla = _validar_tabla(tabla)
    host, puerto = con.get("host"), con.get("port")
    base, usuario, clave = con.get("database"), con.get("user"), con.get("password")
    try:
        if tipo == "postgres":
            import psycopg2
            conexion = psycopg2.connect(host=host, port=puerto or 5432, dbname=base, user=usuario, password=clave,
                                        connect_timeout=10, sslmode=con.get("sslmode") or "prefer",
                                        options="-c default_transaction_read_only=on -c statement_timeout=30000")
            conexion.set_session(readonly=True, autocommit=False)
            consulta = f'SELECT * FROM {".".join(chr(34) + p + chr(34) for p in tabla.split("."))} LIMIT {int(limite)}'
        elif tipo == "mysql":
            import pymysql
            conexion = pymysql.connect(host=host, port=int(puerto or 3306), database=base, user=usuario, password=clave,
                                       connect_timeout=10, read_timeout=30)
            with conexion.cursor() as c:
                c.execute("SET SESSION TRANSACTION READ ONLY")
            consulta = f'SELECT * FROM {".".join("`" + p + "`" for p in tabla.split("."))} LIMIT {int(limite)}'
        else:  # sqlserver
            import pymssql
            conexion = pymssql.connect(server=host, port=str(puerto or 1433), database=base, user=usuario,
                                       password=clave, login_timeout=10, timeout=30)
            consulta = f'SELECT TOP {int(limite)} * FROM {".".join("[" + p + "]" for p in tabla.split("."))}'
    except ErrorDeFuente:
        raise
    except Exception as e:  # el mensaje del driver ayuda al administrador a corregir la conexión
        raise ErrorDeFuente(f"No se pudo conectar a la base de datos: {e}") from e

    try:
        cursor = conexion.cursor()
        cursor.execute(consulta)
        columnas = [d[0] for d in cursor.description]
        return [dict(zip(columnas, fila)) for fila in cursor.fetchall()]
    except Exception as e:
        raise ErrorDeFuente(f"No se pudo leer la tabla {tabla}: {e}") from e
    finally:
        try:
            conexion.rollback()  # nunca se confirma nada
        finally:
            conexion.close()


def _leer_mongo(con: dict, coleccion: str, limite: int) -> list[dict]:
    from pymongo import MongoClient
    _validar_tabla(coleccion)
    try:
        cliente = MongoClient(con.get("uri"), serverSelectionTimeoutMS=10000)
        base = cliente.get_database(con.get("database")) if con.get("database") else cliente.get_default_database()
        documentos = list(base[coleccion].find({}, limit=int(limite)))  # find = solo lectura
    except Exception as e:
        raise ErrorDeFuente(f"No se pudo leer MongoDB: {e}") from e
    finally:
        try:
            cliente.close()
        except Exception:
            pass
    return [_aplanar({k: (str(v) if k == "_id" else v) for k, v in d.items()}) for d in documentos]


# --------------------------------------------------------------------------- perfilado

def _texto(valor: Any) -> str:
    if valor is None:
        return ""
    if isinstance(valor, (datetime, date)):
        return valor.isoformat()
    return str(valor).strip()


def es_fecha(valor: Any) -> date | None:
    if isinstance(valor, datetime):
        return valor.date()
    if isinstance(valor, date):
        return valor
    texto = _texto(valor)
    for formato in ("%Y-%m-%d", "%d/%m/%Y", "%d-%m-%Y", "%m/%d/%Y", "%Y/%m/%d", "%Y-%m-%dT%H:%M:%S", "%Y-%m-%d %H:%M:%S"):
        try:
            return datetime.strptime(texto[:19], formato).date()
        except ValueError:
            continue
    return None


def es_numero(valor: Any) -> float | None:
    if isinstance(valor, bool):
        return None
    if isinstance(valor, (int, float)):
        return float(valor)
    texto = _texto(valor).replace("$", "").replace("COP", "").replace(" ", "")
    if not texto:
        return None
    # 1.234.567,89 (Colombia) o 1,234,567.89
    if re.fullmatch(r"-?\d{1,3}(\.\d{3})+(,\d+)?", texto):
        texto = texto.replace(".", "").replace(",", ".")
    elif re.fullmatch(r"-?\d{1,3}(,\d{3})+(\.\d+)?", texto):
        texto = texto.replace(",", "")
    else:
        texto = texto.replace(",", ".")
    try:
        return float(texto)
    except ValueError:
        return None


def perfilar(registros: list[dict]) -> list[dict]:
    muestra = registros[:MAX_FILAS_PERFIL]
    columnas: list[str] = []
    for r in muestra:
        for c in r:
            if c not in columnas:
                columnas.append(c)

    perfil = []
    for col in columnas:
        valores = [r.get(col) for r in muestra]
        no_vacios = [v for v in valores if _texto(v) != ""]
        textos = [_texto(v) for v in no_vacios]
        n = len(no_vacios) or 1
        fechas = sum(es_fecha(v) is not None for v in no_vacios)
        numeros = sum(es_numero(v) is not None for v in no_vacios)
        tipo = "fecha" if fechas / n >= 0.8 else "número" if numeros / n >= 0.8 else "texto"
        perfil.append({
            "column": col,
            "type": tipo,
            "null_ratio": round(1 - len(no_vacios) / max(len(valores), 1), 3),
            "unique_ratio": round(len(set(textos)) / n, 3),
            "distinct": len(set(textos)),
            "avg_length": round(sum(map(len, textos)) / n, 1),
            "has_digits_and_letters": round(sum(bool(re.search(r"\d", t) and re.search(r"[A-Za-z]", t)) for t in textos) / n, 3),
            "examples": list(dict.fromkeys(textos))[:5],
        })
    return perfil


# --------------------------------------------------------------------------- mapeo sugerido

_SINONIMOS = {
    "internalCode": ["codigo", "cod", "code", "codigo interno", "id equipo", "placa", "activo", "asset", "asset tag",
                     "tag", "inventario", "numero inventario", "consecutivo"],
    "serialNumber": ["serial", "serie", "numero serie", "num serie", "sn", "s n", "serial number", "nro serie",
                     "serial no", "no serie"],
    "type": ["tipo", "type", "categoria", "clase", "tipo equipo", "category", "device type", "device"],
    "brand": ["marca", "brand", "fabricante", "manufacturer", "make", "vendor"],
    "model": ["modelo", "model", "referencia", "ref", "model name"],
    "location": ["ubicacion", "location", "sede", "oficina", "area", "lugar", "piso", "departamento", "site"],
    "acquisitionDate": ["fecha compra", "fecha adquisicion", "purchase date", "fecha", "compra", "adquisicion",
                        "fecha ingreso", "acquired", "purchase"],
    "acquisitionPrice": ["precio", "costo", "valor", "price", "cost", "valor compra", "precio compra", "purchase cost"],
    "operatingSystem": ["so", "s o", "sistema operativo", "os", "operating system"],
}
_TIPO_ESPERADO = {"acquisitionDate": "fecha", "acquisitionPrice": "número"}


def _separar_palabras(columna: str) -> str:
    """'AssetTag' -> 'Asset Tag', 'numeroSerie' -> 'numero Serie', 'fecha_compra' -> 'fecha compra'."""
    columna = re.sub(r"([a-záéíóúñ0-9])([A-ZÁÉÍÓÚÑ])", r"\1 \2", columna)
    columna = re.sub(r"([A-ZÁÉÍÓÚÑ]+)([A-ZÁÉÍÓÚÑ][a-záéíóúñ])", r"\1 \2", columna)
    return columna.replace("_", " ").replace(".", " ").replace("-", " ")


def _puntaje_nombre(columna: str, campo: str) -> float:
    nombre = normalizar(_separar_palabras(columna))
    mejor = 0.0
    for sinonimo in _SINONIMOS[campo]:
        if nombre == sinonimo:
            return 1.0
        if f" {sinonimo} " in f" {nombre} ":
            mejor = max(mejor, 0.75)
    return mejor


def sugerir_mapeo(perfil: list[dict]) -> dict:
    """Asigna a cada campo nuestro la columna más probable (nombre + contenido). Una columna por campo."""
    candidatos = []
    for campo in _SINONIMOS:
        for col in perfil:
            puntaje = _puntaje_nombre(col["column"], campo)
            esperado = _TIPO_ESPERADO.get(campo)
            if esperado and col["type"] == esperado:
                puntaje += 0.3
            elif esperado and col["type"] != esperado:
                puntaje -= 0.5
            if campo in ("internalCode", "serialNumber"):
                # Identificadores: casi únicos, sin vacíos, suelen mezclar letras y números.
                puntaje += 0.3 * col["unique_ratio"] + 0.1 * col["has_digits_and_letters"] - 0.4 * col["null_ratio"]
                if col["type"] == "número":
                    puntaje -= 0.3  # un id autoincremental rara vez es el código de inventario
            if campo in ("type", "brand") and col["type"] == "texto" and 0 < col["distinct"] <= 30:
                puntaje += 0.1  # pocas categorías
            if puntaje >= 0.5:
                candidatos.append((puntaje, campo, col["column"]))

    mapeo, usadas = {}, set()
    for puntaje, campo, columna in sorted(candidatos, reverse=True):
        if campo not in mapeo and columna not in usadas:
            mapeo[campo] = {"column": columna, "confidence": round(min(puntaje, 1.0), 2)}
            usadas.add(columna)

    # Columna llave (identifica el registro en la fuente): el identificador más único.
    llave = None
    for campo in ("internalCode", "serialNumber"):
        if campo in mapeo:
            llave = mapeo[campo]["column"]
            break
    if llave is None:
        unicas = [c for c in perfil if c["unique_ratio"] >= 0.99 and c["null_ratio"] == 0]
        llave = unicas[0]["column"] if unicas else None
    return {"fields": mapeo, "key_column": llave}


def duplicados(registros: list[dict], columna: str | None) -> list[str]:
    if not columna:
        return []
    vistos, repetidos = set(), []
    for r in registros:
        valor = _texto(r.get(columna)).upper()
        if valor and valor in vistos and valor not in repetidos:
            repetidos.append(valor)
        vistos.add(valor)
    return repetidos[:20]


def a_json(registros: list[dict]) -> list[dict]:
    """Valores serializables (fechas a ISO, Decimal a número)."""
    salida = []
    for r in registros:
        fila = {}
        for k, v in r.items():
            if isinstance(v, (datetime, date)):
                fila[str(k)] = v.isoformat()
            elif isinstance(v, (int, float, str, bool)) or v is None:
                fila[str(k)] = v
            else:
                fila[str(k)] = str(v)
        salida.append(fila)
    return salida
