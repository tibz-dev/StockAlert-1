export interface Supplier {
  id: string;
  companyName: string;
  contactEmail: string | null;
  productCount: number;
  lowStockProductCount: number;
}
