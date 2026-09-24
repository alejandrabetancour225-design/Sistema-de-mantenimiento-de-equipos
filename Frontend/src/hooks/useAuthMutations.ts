import { useState } from "react";
import { isAxiosError } from "axios";
import { login, register } from "../services/authServices";
import { useAuth } from "../context/authContext";

export function useLogin() {
  const { setSession } = useAuth();
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const mutate = async (credentials: Parameters<typeof login>[0]) => {
    setIsPending(true);
    setError(null);
    try {
      const data = await login(credentials);
      setSession(data);
      return data;
    } catch (err) {
      setError(err);
      throw err;
    } finally {
      setIsPending(false);
    }
  };

  return { mutate, isPending, error };
}

export function useRegister() {
  const { setSession } = useAuth();
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const mutate = async (data: Parameters<typeof register>[0]) => {
    setIsPending(true);
    setError(null);
    try {
      const result = await register(data);
      setSession(result);
      return result;
    } catch (err) {
      setError(err);
      throw err;
    } finally {
      setIsPending(false);
    }
  };

  return { mutate, isPending, error };
}

// HU-01: mensaje genérico, no revela cuál dato falló
export function getAuthErrorMessage(error: unknown): string {
  if (isAxiosError(error) && error.response?.data?.message) {
    return error.response.data.message;
  }
  return "Ocurrió un error. Intenta de nuevo.";
}