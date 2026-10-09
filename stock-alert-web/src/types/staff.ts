export interface StaffMember {
  id: string;
  fullName: string;
  email: string | null;
  phoneNumber: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
}
