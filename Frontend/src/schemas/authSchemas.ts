import { z } from "zod";

// Misma regla que el backend (StrongPasswordAttribute): 10 a 128 caracteres, al menos una letra y un número.
export const PASSWORD_RULE_MESSAGE =
  "La contraseña debe tener entre 10 y 128 caracteres e incluir al menos una letra y un número.";

export const strongPassword = z
  .string()
  .min(10, PASSWORD_RULE_MESSAGE)
  .max(128, PASSWORD_RULE_MESSAGE)
  .regex(/\p{L}/u, PASSWORD_RULE_MESSAGE)
  .regex(/\p{Nd}/u, PASSWORD_RULE_MESSAGE);

export const loginSchema = z.object({
  email: z.string().email("Correo inválido"),
  password: z.string().min(1, "La contraseña es obligatoria"),
});

export const registerSchema = z.object({
  fullName: z.string().min(1, "El nombre es obligatorio"),
  email: z.string().email("Correo inválido"),
  password: strongPassword,
  phone: z.string()
    .optional()
    .transform((v) => (v?.trim() ? v.trim() : undefined)),
});

export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;