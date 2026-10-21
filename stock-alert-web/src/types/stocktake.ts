export interface StocktakeItem {
  id: string;
  productId: string | null;
  externalProductId: string | null;
  productName: string;
  barcode: string | null;
  unitValue: number;
  expectedQuantity: number | null;
  firstCount: number | null;
  firstCountedBy: string | null;
  firstCountedAt: string | null;
  secondCount: number | null;
  secondCountedBy: string | null;
  secondCountedAt: string | null;
  finalCount: number | null;
  varianceQuantity: number | null;
  varianceValue: number | null;
  status:
    | 'Pending'
    | 'Matched'
    | 'RecountRequired'
    | 'NeedsReview'
    | 'Approved';
  resolutionReason: string | null;
  approvedBy: string | null;
  approvedAt: string | null;
}

export interface StocktakeSession {
  id: string;
  referenceNumber: string;
  title: string;
  location: string | null;
  zone: string | null;
  source: string;
  status: 'Counting' | 'Review' | 'Completed' | 'Cancelled';
  blindCount: boolean;
  createdBy: string;
  createdAt: string;
  sourceSnapshotAt: string;
  completedAt: string | null;
  completedBy: string | null;
  totalItems: number;
  countedItems: number;
  matchedItems: number;
  varianceItems: number;
  recountRequiredItems: number;
  reviewRequiredItems: number;
  varianceValue: number;
  items: StocktakeItem[];
}
