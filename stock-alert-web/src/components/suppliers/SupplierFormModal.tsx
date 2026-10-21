'use client';

import { useEffect, useState, type FormEvent } from 'react';
import { Loader2, Save, Truck, X } from 'lucide-react';
import api from '@/lib/api';
import type { Supplier } from '@/types/supplier';

interface SupplierFormModalProps {
  mode: 'create' | 'edit';
  supplier?: Supplier | null;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
}

export default function SupplierFormModal({
  mode,
  supplier,
  onClose,
  onSaved,
}: SupplierFormModalProps) {
  const [companyName, setCompanyName] = useState('');
  const [contactEmail, setContactEmail] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (mode === 'edit' && supplier) {
      setCompanyName(supplier.companyName);
      setContactEmail(supplier.contactEmail ?? '');
      return;
    }

    setCompanyName('');
    setContactEmail('');
  }, [mode, supplier]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');

    if (!companyName.trim()) {
      setError('Supplier company name is required.');
      return;
    }

    try {
      setSaving(true);

      const payload = {
        companyName: companyName.trim(),
        contactEmail: contactEmail.trim() || null,
      };

      if (mode === 'create') {
        await api.post('/suppliers', payload);
      } else if (supplier) {
        await api.put(`/suppliers/${supplier.id}`, payload);
      }

      await onSaved();
      onClose();
    } catch {
      setError(
        mode === 'create'
          ? 'Unable to create supplier. The company name may already exist.'
          : 'Unable to update supplier.',
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">
              {mode === 'create' ? 'New supplier' : 'Supplier details'}
            </p>
            <h2 className="mt-1 flex items-center gap-2 text-xl font-bold text-gray-900">
              <Truck size={21} />
              {mode === 'create' ? 'Add Supplier' : 'Edit Supplier'}
            </h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="rounded-lg p-2 text-gray-400 transition hover:bg-gray-100 hover:text-gray-700"
          >
            <X size={20} />
          </button>
        </div>

        <form onSubmit={submit} className="space-y-5 p-6">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <label className="block">
            <span className="mb-2 block text-sm font-medium text-gray-700">
              Company name
            </span>
            <input
              required
              value={companyName}
              onChange={(event) => setCompanyName(event.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              placeholder="Supplier company"
            />
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-medium text-gray-700">
              Contact email
            </span>
            <input
              type="email"
              value={contactEmail}
              onChange={(event) => setContactEmail(event.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              placeholder="orders@supplier.co.za"
            />
          </label>

          {mode === 'edit' && supplier && supplier.productCount > 0 && (
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
              This supplier is linked to <strong>{supplier.productCount}</strong>{' '}
              product{supplier.productCount === 1 ? '' : 's'} and cannot be deleted
              until those products are reassigned.
            </div>
          )}

          <div className="flex justify-end gap-3 border-t border-gray-100 pt-5">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={saving}
              className="flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:bg-blue-400"
            >
              {saving ? <Loader2 className="animate-spin" size={17} /> : <Save size={17} />}
              {saving ? 'Saving...' : mode === 'create' ? 'Add Supplier' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
