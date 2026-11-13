"""Normalización de texto y aplicación del glosario de modismos (español de Colombia)."""
import re
import unicodedata

_ES = set("""de la el que en los del se las por un una para con no es su al lo como mas o pero sus le ya
este si porque esta entre cuando muy sin sobre tambien me hasta hay donde desde equipo revisar puede
causa falla posible recomendacion tecnico verificar cambiar limpiar fuente pantalla bateria""".split())
_EN = set("""the of and to in is it that for on with as this be check if or are may could power failure
replace inspect should cause device supply screen battery issue""".split())


def normalizar(texto: str) -> str:
    """Minúsculas, sin tildes y con espacios simples, para comparar frases."""
    sin_tildes = unicodedata.normalize("NFKD", texto or "").encode("ascii", "ignore").decode("ascii")
    return re.sub(r"\s+", " ", re.sub(r"[^a-z0-9ñ ]", " ", sin_tildes.lower())).strip()


def aplicar_glosario(texto: str, glosario: list[dict]) -> list[dict]:
    """Expresiones del glosario que aparecen en el texto (frase completa, sin importar tildes)."""
    base = f" {normalizar(texto)} "
    encontradas = []
    for entrada in glosario:
        expresion = normalizar(entrada.get("expresion", ""))
        if expresion and f" {expresion} " in base:
            encontradas.append(entrada)
    # Las expresiones más largas primero ("se apaga solo" antes que "apaga").
    return sorted(encontradas, key=lambda e: -len(e.get("expresion", "")))[:8]


def proporcion_espanol(texto: str) -> float:
    """Estimación simple de qué tan en español está un texto (0 a 1)."""
    palabras = normalizar(texto).split()
    es = sum(p in _ES for p in palabras)
    en = sum(p in _EN for p in palabras)
    return 1.0 if es + en == 0 else es / (es + en)
