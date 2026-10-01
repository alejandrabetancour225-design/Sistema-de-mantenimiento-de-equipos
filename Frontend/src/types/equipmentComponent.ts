export interface EquipmentComponent {
  id: string;
  equipmentId: string;
  equipmentInternalCode?: string | null;
  componentType: string;
  brand: string;
  model: string;
  serialNumber: string;
  specifications?: Record<string, unknown> | null;
  installedAt: string;
}

export interface CreateEquipmentComponentRequest {
  equipmentId: string;
  componentType: string;
  brand: string;
  model: string;
  serialNumber: string;
  specifications?: Record<string, unknown>;
  installedAt?: string;
}

export type UpdateEquipmentComponentRequest = Partial<
  Omit<CreateEquipmentComponentRequest, "equipmentId">
>;
