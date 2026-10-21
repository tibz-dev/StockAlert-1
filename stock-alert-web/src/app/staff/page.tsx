'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  BriefcaseBusiness,
  Loader2,
  Pencil,
  Plus,
  Search,
  UserCheck,
  UserX,
  X,
} from 'lucide-react';
import api from '@/lib/api';
import type { StaffMember } from '@/types/staff';

export default function StaffPage() {
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<StaffMember | null>(null);
  const [creating, setCreating] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    try {
      setError('');
      const response = await api.get<StaffMember[]>('/staff');
      setStaff(response.data);
    } catch {
      setError('Unable to load staff.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const filtered = useMemo(() => {
    const query = search.trim().toLowerCase();

    if (!query) return staff;

    return staff.filter((member) =>
      [member.fullName, member.role, member.email, member.phoneNumber]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query)),
    );
  }, [staff, search]);

  const activeCount = staff.filter((member) => member.isActive).length;

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading staff...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-6xl">
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-sm font-medium text-blue-600">Sales team</p>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
              <BriefcaseBusiness className="text-blue-600" />
              Staff
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              Manage the people who can be assigned to quotes and sales.
            </p>
          </div>

          <button
            onClick={() => setCreating(true)}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-700"
          >
            <Plus size={17} />
            Add Staff Member
          </button>
        </div>

        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        <div className="mb-6 grid gap-4 sm:grid-cols-2">
          <Summary label="Staff members" value={staff.length} />
          <Summary label="Active staff" value={activeCount} />
        </div>

        <div className="mb-5 rounded-xl border border-gray-200 bg-white p-4 shadow-sm">
          <label className="relative block">
            <Search
              size={17}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
            />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search name, role, email or phone"
              className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </label>
        </div>

        <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="p-4 text-sm font-semibold text-gray-600">Name</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Role</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Contact</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Status</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Action</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((member) => (
                  <tr key={member.id} className="border-b border-gray-100 last:border-0">
                    <td className="p-4 font-semibold text-gray-900">
                      {member.fullName}
                    </td>
                    <td className="p-4 text-sm text-gray-600">{member.role}</td>
                    <td className="p-4 text-sm text-gray-500">
                      <div>{member.email ?? 'No email'}</div>
                      <div>{member.phoneNumber ?? 'No phone'}</div>
                    </td>
                    <td className="p-4">
                      <span
                        className={
                          member.isActive
                            ? 'inline-flex items-center gap-1 rounded-full bg-green-50 px-2.5 py-1 text-xs font-bold text-green-700'
                            : 'inline-flex items-center gap-1 rounded-full bg-gray-100 px-2.5 py-1 text-xs font-bold text-gray-600'
                        }
                      >
                        {member.isActive ? <UserCheck size={12} /> : <UserX size={12} />}
                        {member.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="p-4">
                      <button
                        onClick={() => setEditing(member)}
                        className="inline-flex items-center gap-1 rounded-md border border-gray-300 px-3 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-50"
                      >
                        <Pencil size={13} />
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}

                {filtered.length === 0 && (
                  <tr>
                    <td colSpan={5} className="p-10 text-center text-sm text-gray-500">
                      No staff members found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {(creating || editing) && (
        <StaffModal
          member={editing}
          onClose={() => {
            setCreating(false);
            setEditing(null);
          }}
          onSaved={async () => {
            setCreating(false);
            setEditing(null);
            await load();
          }}
        />
      )}
    </div>
  );
}

function StaffModal({
  member,
  onClose,
  onSaved,
}: {
  member: StaffMember | null;
  onClose: () => void;
  onSaved: () => Promise<void>;
}) {
  const [fullName, setFullName] = useState(member?.fullName ?? '');
  const [email, setEmail] = useState(member?.email ?? '');
  const [phoneNumber, setPhoneNumber] = useState(member?.phoneNumber ?? '');
  const [role, setRole] = useState(member?.role ?? 'Sales');
  const [isActive, setIsActive] = useState(member?.isActive ?? true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!fullName.trim() || !role.trim()) {
      setError('Name and role are required.');
      return;
    }

    try {
      setSaving(true);
      setError('');

      const payload = {
        fullName: fullName.trim(),
        email: email.trim() || null,
        phoneNumber: phoneNumber.trim() || null,
        role: role.trim(),
        ...(member ? { isActive } : {}),
      };

      if (member) {
        await api.put(`/staff/${member.id}`, payload);
      } else {
        await api.post('/staff', payload);
      }

      await onSaved();
    } catch {
      setError('Unable to save staff member.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">Sales team</p>
            <h2 className="mt-1 text-xl font-bold text-gray-900">
              {member ? 'Edit Staff Member' : 'Add Staff Member'}
            </h2>
          </div>
          <button onClick={onClose} className="rounded-lg p-2 text-gray-400 hover:bg-gray-100">
            <X size={20} />
          </button>
        </div>

        <form onSubmit={submit} className="space-y-4 p-6">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <Field label="Full name" value={fullName} onChange={setFullName} required />
          <Field label="Role / title" value={role} onChange={setRole} required />
          <Field label="Email" type="email" value={email} onChange={setEmail} />
          <Field label="Phone" value={phoneNumber} onChange={setPhoneNumber} />

          {member && (
            <label className="flex items-center gap-3 rounded-lg border border-gray-200 p-4">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(event) => setIsActive(event.target.checked)}
              />
              <span className="text-sm font-medium text-gray-700">
                Active staff member
              </span>
            </label>
          )}

          <button
            type="submit"
            disabled={saving}
            className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-3 text-sm font-semibold text-white disabled:bg-blue-400"
          >
            {saving ? <Loader2 className="animate-spin" size={17} /> : null}
            {saving ? 'Saving...' : 'Save Staff Member'}
          </button>
        </form>
      </div>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  type = 'text',
  required = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-gray-700">{label}</span>
      <input
        type={type}
        required={required}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}

function Summary({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}
