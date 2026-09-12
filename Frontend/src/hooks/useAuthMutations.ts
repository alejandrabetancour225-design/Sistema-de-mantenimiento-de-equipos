import { useMutation } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { login, register } from "../services/authServices";
import { useAuthContext } from "../context/authContext";

export function useLogin() {
  const { setSession } = useAuthContext();
  return useMutation({
    mutationFn: login,
    onSuccess: setSession,
  });
}

export function useRegister() {
  const { setSession } = useAuthContext();
  return useMutation({
    mutationFn: register,
    onSuccess: setSession,
  });
}

// HU-01: mensaje genérico, no revela cuál dato falló
export function getAuthErrorMessage(error: unknown): string {
  if (isAxiosError(error) && error.response?.data?.message) {
    return error.response.data.message;
  }
  return "Ocurrió un error. Intenta de nuevo.";
}