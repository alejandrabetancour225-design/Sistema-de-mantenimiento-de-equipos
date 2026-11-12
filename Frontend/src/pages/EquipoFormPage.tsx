import { useEffect } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import {
  createEquipment,
  getEquipmentById,
  updateEquipment,
} from "../services/equipmentServices";
import { getApiErrorMessage } from "../lib/errors";
import { useAuth } from "../context/authContext";
import {
  equipmentSchema,
  todayInBogota,
  type EquipmentFormInput,
  type EquipmentFormValues,
} from "../schemas/equipmentSchemas";
import type { Equipment } from "../types/equipment";

function toCharacteristicRows(characteristics: Equipment["characteristics"]) {
  if (!characteristics) return [];
  return Object.entries(characteristics).map(([key, value]) => ({
    key,
    value: typeof value === "string" ? value : JSON.stringify(value),
  }));
}

function toCharacteristicObject(rows: EquipmentFormValues["characteristics"]) {
  const entries = rows.filter((r) => r.key);
  return entries.length ? Object.fromEntries(entries.map((r) => [r.key, r.value])) : null;
}

export function EquipoFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { role } = useAuth();
  // Código interno y serie solo los corrige un Administrador (la API lo exige igual).
  const canEditIdentity = !isEdit || role === "Administrador";

  const { data: existing } = useQuery({
    queryKey: ["equipment", id],
    queryFn: () => getEquipmentById(id as string),
    enabled: isEdit,
  });

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<EquipmentFormInput, unknown, EquipmentFormValues>({
    resolver: zodResolver(equipmentSchema),
    defaultValues: { characteristics: [] },
  });

  const characteristics = useFieldArray({ control, name: "characteristics" });

  useEffect(() => {
    if (existing) {
      reset({
        internalCode: existing.internalCode,
        serialNumber: existing.serialNumber,
        type: existing.type,
        brand: existing.brand,
        model: existing.model,
        location: existing.location,
        operatingSystem: existing.operatingSystem ?? "",
        acquisitionDate: existing.acquisitionDate ?? "",
        acquisitionPrice: existing.acquisitionPrice ?? "",
        warrantyUntil: existing.warrantyUntil ?? "",
        characteristics: toCharacteristicRows(existing.characteristics),
      });
    }
  }, [existing, reset]);

  const mutation = useMutation({
    mutationFn: (values: EquipmentFormValues) => {
      const payload = {
        internalCode: values.internalCode,
        serialNumber: values.serialNumber,
        type: values.type,
        brand: values.brand,
        model: values.model,
        location: values.location,
        operatingSystem: values.operatingSystem || null,
        acquisitionDate: values.acquisitionDate ?? null,
        acquisitionPrice: values.acquisitionPrice ?? null,
        warrantyUntil: values.warrantyUntil ?? null,
        characteristics: toCharacteristicObject(values.characteristics),
      };
      return isEdit
        ? updateEquipment(id as string, payload)
        : createEquipment({
            ...payload,
            operatingSystem: payload.operatingSystem ?? undefined,
            acquisitionDate: payload.acquisitionDate ?? undefined,
            acquisitionPrice: payload.acquisitionPrice ?? undefined,
            warrantyUntil: payload.warrantyUntil ?? undefined,
            characteristics: payload.characteristics ?? undefined,
          });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
      navigate(isEdit ? `/equipos/${id}` : "/equipos");
    },
  });

  const apiError = mutation.error ? getApiErrorMessage(mutation.error) : undefined;
  const today = todayInBogota();

  return (
    <form
      onSubmit={handleSubmit((values) => mutation.mutate(values))}
      className="mx-auto max-w-lg space-y-4 p-8"
      noValidate
    >
      <h1 className="text-xl font-semibold">{isEdit ? "Editar equipo" : "Nuevo equipo"}</h1>
      {isEdit && (
        <p className="text-xs text-gray-400">
          Cada corrección queda registrada en la auditoría con el valor anterior y el nuevo.
        </p>
      )}

      <div className="grid grid-cols-2 gap-4">
        <Field label="Código interno" error={errors.internalCode?.message}>
          <input
            {...register("internalCode")}
            className="input uppercase"
            disabled={!canEditIdentity}
            title={canEditIdentity ? undefined : "Solo un Administrador puede corregirlo"}
          />
        </Field>
        <Field label="Número de serie" error={errors.serialNumber?.message}>
          <input
            {...register("serialNumber")}
            className="input uppercase"
            disabled={!canEditIdentity}
            title={canEditIdentity ? undefined : "Solo un Administrador puede corregirlo"}
          />
        </Field>
      </div>

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

      <Field label="Sistema operativo" error={errors.operatingSystem?.message}>
        <input
          {...register("operatingSystem")}
          className="input"
          placeholder="Ej: Windows 11, Ubuntu 24.04"
        />
      </Field>

      <Field label="Ubicación" error={errors.location?.message}>
        <input {...register("location")} className="input" />
      </Field>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Fecha de adquisición" error={errors.acquisitionDate?.message}>
          <input type="date" max={today} {...register("acquisitionDate")} className="input" />
        </Field>
        <Field label="Precio de adquisición" error={errors.acquisitionPrice?.message}>
          <input type="number" step="0.01" min="0" {...register("acquisitionPrice")} className="input" />
        </Field>
      </div>

      <Field label="Garantía hasta" error={errors.warrantyUntil?.message}>
        <input type="date" {...register("warrantyUntil")} className="input" />
      </Field>

      <div>
        <div className="flex items-center justify-between">
          <label className="block text-sm font-medium">Características</label>
          <button
            type="button"
            onClick={() => characteristics.append({ key: "", value: "" })}
            className="flex items-center gap-1 text-xs text-blue-400 hover:text-blue-300"
          >
            <Plus size={14} /> Agregar
          </button>
        </div>
        {characteristics.fields.length === 0 && (
          <p className="mt-1 text-xs text-gray-400">Ej: RAM 16 GB, Procesador Core i5, Disco SSD 512 GB.</p>
        )}
        <div className="mt-2 space-y-2">
          {characteristics.fields.map((field, index) => (
            <div key={field.id} className="flex gap-2">
              <input
                {...register(`characteristics.${index}.key`)}
                className="input flex-1"
                placeholder="Nombre (ej: RAM)"
              />
              <input
                {...register(`characteristics.${index}.value`)}
                className="input flex-1"
                placeholder="Valor (ej: 16 GB)"
              />
              <button
                type="button"
                onClick={() => characteristics.remove(index)}
                className="px-2 text-gray-400 hover:text-red-400"
                aria-label="Quitar característica"
              >
                <Trash2 size={16} />
              </button>
            </div>
          ))}
        </div>
        {errors.characteristics?.message && (
          <p className="text-xs text-red-600">{errors.characteristics.message}</p>
        )}
      </div>

      {apiError && <p className="text-sm text-red-600">{apiError}</p>}

      <button
        type="submit"
        disabled={isSubmitting || mutation.isPending}
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
