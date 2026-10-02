export interface SparePart {
  id: string;
  name: string;
  description?: string | null;
  unitCost: number;
  active: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSparePartRequest {
  name: string;
  description?: string;
  unitCost: number;
}

export interface UpdateSparePartRequest {
  name?: string;
  description?: string;
  unitCost?: number;
  active?: boolean;
}