import axios from "axios";

export function getApiErrorMessage(error: unknown, fallback = "Ocurrió un error."): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;
    if (data && typeof data === "object" && "message" in data) {
      const msg = (data as { message?: unknown }).message;
      if (typeof msg === "string") return msg;
    }
  }
  return fallback;
}
