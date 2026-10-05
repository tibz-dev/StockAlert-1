export interface Product {
  id: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  categoryName: string;
  isLowStock: boolean;
  supplierName: string;
  supplierEmail: string | null;
  externalId: string | null;
}

export interface ProductFormValues {
  name: string;
  description: string;
  price: string;
  stockQuantity: string;
  categoryName: string;
  supplierName: string;
  supplierEmail: string;
}
