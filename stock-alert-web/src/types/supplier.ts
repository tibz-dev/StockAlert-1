export interface Supplier {
  id: string;
  companyName: string;
  contactEmail: string | null;
  productCount: number;
  lowStockProductCount: number;
  productNames: string[];
}

export interface SupplierProduct {
  id: string;
  name: string;
  barcode: string | null;
  price: number;
  onHand: number;
  reserved: number;
  available: number;
  isLowStock: boolean;
}
