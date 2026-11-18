import { z } from "zod";

// Fecha de hoy (YYYY-MM-DD) en la zona horaria del negocio (Colombia), igual que el backend.
export function todayInBogota(): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "America/Bogota" }).format(new Date());
}

const requiredText = (max: number) =>
  z.string().trim().min(1, "Requerido").max(max, `Máximo ${max} caracteres`);

// Un input vacío llega como "": se trata como "sin valor".
const optionalDate = z
  .string()
  .optional()
  .transform((v) => (v && v.trim() ? v : undefined));

const optionalPrice = z
  .union([z.string(), z.number()])
  .optional()
  .transform((v, ctx) => {
    if (v === undefined || v === "") return undefined;
    const n = typeof v === "number" ? v : Number(v);
    if (Number.isNaN(n)) {
      ctx.addIssue({ code: "custom", message: "Precio inválido" });
      return z.NEVER;
    }
    return n;
  })
  .refine((v) => v === undefined || (v > 0 && v <= 9_999_999_999), {
    message: "El precio debe ser mayor que cero (déjalo vacío si no se conoce)",
  });

// Igual que la API: combina letras y números; se permiten guiones, puntos, barras y guion bajo.
export const SERIAL_FORMAT = /^(?=.*[A-Z])(?=.*[0-9])[A-Z0-9][A-Z0-9\-_./]{2,99}$/;
export const SERIAL_MESSAGE =
  "Debe combinar letras y números (3 a 100 caracteres; se permiten - _ . /)";

// Reglas de negocio (las mismas que valida la API, que es la fuente de verdad):
//  - la fecha de adquisición no puede ser posterior a hoy;
//  - la garantía no puede terminar antes de la fecha de adquisición.
export const equipmentSchema = z
  .object({
    internalCode: requiredText(50),
    serialNumber: requiredText(100).refine((v) => SERIAL_FORMAT.test(v.toUpperCase()), SERIAL_MESSAGE),
    type: requiredText(100),
    brand: requiredText(100),
    model: requiredText(100),
    location: requiredText(200),
    operatingSystem: z.string().trim().max(100, "Máximo 100 caracteres").optional(),
    acquisitionDate: optionalDate,
    acquisitionPrice: optionalPrice,
    warrantyUntil: optionalDate,
    characteristics: z
      .array(z.object({ key: z.string().trim(), value: z.string().trim() }))
      .default([]),
    // Alquilado: pertenece a un contrato; la fecha de adquisición es el inicio del contrato.
    ownership: z.enum(["OWNED", "RENTED"]).default("OWNED"),
    rentalContractId: z.string().optional(),
  })
  .superRefine((data, ctx) => {
    if (data.ownership === "RENTED" && !data.rentalContractId) {
      ctx.addIssue({ code: "custom", path: ["rentalContractId"], message: "Elige el contrato de alquiler." });
    }
    if (data.ownership === "RENTED") return; // fechas y precio los define el contrato
    if (data.acquisitionDate && data.acquisitionDate > todayInBogota()) {
      ctx.addIssue({
        code: "custom",
        path: ["acquisitionDate"],
        message: "La fecha de adquisición no puede ser posterior a hoy.",
      });
    }
    if (data.acquisitionDate && data.warrantyUntil && data.warrantyUntil < data.acquisitionDate) {
      ctx.addIssue({
        code: "custom",
        path: ["warrantyUntil"],
        message: "La garantía no puede terminar antes de la fecha de adquisición.",
      });
    }
    const keys = data.characteristics.filter((c) => c.key).map((c) => c.key.toLowerCase());
    if (new Set(keys).size !== keys.length) {
      ctx.addIssue({ code: "custom", path: ["characteristics"], message: "Hay características repetidas." });
    }
    data.characteristics.forEach((c, i) => {
      if (!c.key && c.value) {
        ctx.addIssue({ code: "custom", path: ["characteristics", i, "key"], message: "Falta el nombre" });
      }
    });
  });

export type EquipmentFormInput = z.input<typeof equipmentSchema>;
export type EquipmentFormValues = z.output<typeof equipmentSchema>;
