import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createEquipment,
  getEquipmentById,
  updateEquipment,
} from "../services/equipmentServices";

const schema = z.object({
  internalCode: z.string().min(1, "Requerido"),
  serialNumber: z.string().min(1, "Requerido"),
  type: z.string().min(1, "Requerido"),
  brand: z.string().min(1, "Requerido"),
  model: z.string().min(1, "Requerido"),
  location: z.string().min(1, "Requerido"),
  acquisitionDate: z.string().optional(),
  acquisitionPrice: z.coerce.number().optional(),
  warrantyUntil: z.string().optional(),
});

type FormInput = z.input<typeof schema>; // lo que el usuario escribe (acquisitionPrice: string)
type FormValues = z.output<typeof schema>; // lo que sale ya validado (acquisitionPrice: number)

export function EquipoFormPage() {
  const { id } = useParams(); // presente solo en modo edición
  const isEdit = !!id;
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: existing } = useQuery({
    queryKey: ["equipment", id],
    queryFn: () => getEquipmentById(id as string),
    enabled: isEdit,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormInput>({ resolver: zodResolver(schema) });

  useEffect(() => {
    if (existing) {
      reset({
        internalCode: existing.internalCode,
        serialNumber: existing.serialNumber,
        type: existing.type,
        brand: existing.brand,
        model: existing.model,
        location: existing.location,
        acquisitionDate: existing.acquisitionDate ?? undefined,
        acquisitionPrice: existing.acquisitionPrice ?? undefined,
        warrantyUntil: existing.warrantyUntil ?? undefined,
      });
    }
  }, [existing, reset]);

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      isEdit ? updateEquipment(id as string, values) : createEquipment(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
      navigate("/equipos");
    },
  });

  // El backend responde 409 con { message } si el código interno
  // o el número de serie ya existen.
  const conflictMessage = (mutation.error as any)?.response?.data?.message;

  return (
    <form
      onSubmit={handleSubmit((values) => mutation.mutate(values as FormValues))}
      className="mx-auto max-w-lg space-y-4 p-8"
    >
      <h1 className="text-xl font-semibold">
        {isEdit ? "Editar equipo" : "Nuevo equipo"}
      </h1>

      <Field label="Código interno" error={errors.internalCode?.message}>
        <input {...register("internalCode")} className="input" disabled={isEdit} />
      </Field>

      <Field label="Número de serie" error={errors.serialNumber?.message}>
        <input {...register("serialNumber")} className="input" />
      </Field>

      <Field label="Tipo" error={errors.type?.message}>
        <input {...register("type")} className="input" placeholder="Laptop, monitor..." />
      </Field>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Marca" error={errors.brand?.message}>
          <input {...register("brand")} className="input" />
        </Field>
        <Field label="Modelo" error={errors.model?.message}>
          <input {...register("model")} className="input" />
        </Field>
      </div>

      <Field label="Ubicación" error={errors.location?.message}>
        <input {...register("location")} className="input" />
      </Field>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Fecha de adquisición">
          <input type="date" {...register("acquisitionDate")} className="input" />
        </Field>
        <Field label="Precio de adquisición">
          <input
            type="number"
            step="0.01"
            {...register("acquisitionPrice")}
            className="input"
          />
        </Field>
      </div>

      <Field label="Garantía hasta">
        <input type="date" {...register("warrantyUntil")} className="input" />
      </Field>

      {conflictMessage && <p className="text-sm text-red-600">{conflictMessage}</p>}

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full rounded bg-blue-600 py-2 text-white disabled:opacity-50"
      >
        {isEdit ? "Guardar cambios" : "Registrar equipo"}
      </button>
    </form>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-sm font-medium">{label}</label>
      <div className="mt-1">{children}</div>
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  );
}  