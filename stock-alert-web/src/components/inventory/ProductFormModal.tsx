'use client';

import { useEffect, useState, type FormEvent } from 'react';
import { Loader2, PackagePlus, Save, X } from 'lucide-react';
import api from '@/lib/api';
import type { Product, ProductFormValues } from '@/types/inventory';
import type { Supplier } from '@/types/supplier';

interface ProductFormModalProps {
  mode: 'create' | 'edit';
  product?: Product | null;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
}

const emptyForm: ProductFormValues = {
  name: '',
  description: '',
  price: '',
  stockQuantity: '0',
  categoryName: '',
  supplierName: '',
  supplierEmail: '',
};

export default function ProductFormModal({
  mode,
  product,
  onClose,
  onSaved,
}: ProductFormModalProps) {
  const [form, setForm] = useState<ProductFormValues>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);

  useEffect(() => {
    const loadSuppliers = async () => {
      try {
        const response = await api.get<Supplier[]>('/suppliers');
        setSuppliers(response.data);
      } catch {
        setSuppliers([]);
      }
    };

    void loadSuppliers();
  }, []);

  useEffect(() => {
    if (mode === 'edit' && product) {
      setForm({
        name: product.name,
        description: product.description ?? '',
        price: product.price.toString(),
        stockQuantity: product.stockQuantity.toString(),
        categoryName: product.categoryName,
        supplierName: product.supplierName,
        supplierEmail: product.supplierEmail ?? '',
      });
      return;
    }

    setForm(emptyForm);
  }, [mode, product]);

  const updateField = (field: keyof ProductFormValues, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    const price = Number(form.price);

    if (!Number.isFinite(price) || price <= 0) {
      setError('Price must be greater than zero.');
      setSaving(false);
      return;
    }

    if (mode === 'create') {
      const stockQuantity = Number(form.stockQuantity);

      if (!Number.isInteger(stockQuantity) || stockQuantity < 0) {
        setError('Opening stock must be a whole number of zero or more.');
        setSaving(false);
        return;
      }
    }

    try {
      if (mode === 'create') {
        await api.post('/products', {
          name: form.name.trim(),
          description: form.description.trim() || null,
          price,
          stockQuantity: Number(form.stockQuantity),
          categoryName: form.categoryName.trim(),
          supplierName: form.supplierName.trim(),
          supplierEmail: form.supplierEmail.trim() || null,
        });
      } else if (product) {
        await api.put(`/products/${product.id}`, {
          name: form.name.trim(),
          description: form.description.trim() || null,
          price,
          categoryName: form.categoryName.trim(),
          supplierName: form.supplierName.trim(),
          supplierEmail: form.supplierEmail.trim() || null,
        });
      }

      await onSaved();
      onClose();
    } catch {
      setError(
        mode === 'create'
          ? 'Unable to create the product.'
          : 'Unable to update the product.',
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="max-h-[92vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">
              {mode === 'create' ? 'New inventory item' : 'Product details'}
            </p>
            <h2 className="mt-1 flex items-center gap-2 text-xl font-bold text-gray-900">
              <PackagePlus size={21} />
              {mode === 'create' ? 'Add Product' : 'Edit Product'}
            </h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-gray-400 transition hover:bg-gray-100 hover:text-gray-700"
            aria-label="Close"
          >
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5 p-6">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="grid gap-5 md:grid-cols-2">
            <Field label="Product name">
              <input
                required
                value={form.name}
                onChange={(event) => updateField('name', event.target.value)}
                className="input"
                placeholder="e.g. 2L Full Cream Milk"
              />
            </Field>

            <Field label="Category">
              <input
                required
                value={form.categoryName}
                onChange={(event) => updateField('categoryName', event.target.value)}
                className="input"
                placeholder="e.g. Dairy"
              />
            </Field>

            <Field label="Unit price">
              <input
                required
                type="number"
                min="0.01"
                step="0.01"
                value={form.price}
                onChange={(event) => updateField('price', event.target.value)}
                className="input"
                placeholder="0.00"
              />
            </Field>

            {mode === 'create' && (
              <Field label="Opening stock">
                <input
                  required
                  type="number"
                  min="0"
                  step="1"
                  value={form.stockQuantity}
                  onChange={(event) =>
                    updateField('stockQuantity', event.target.value)
                  }
                  className="input"
                  placeholder="0"
                />
              </Field>
            )}

            <Field label="Supplier">
              <input
                required
                list="supplier-options"
                value={form.supplierName}
                onChange={(event) => {
                  const value = event.target.value;
                  updateField('supplierName', value);

                  const matchedSupplier = suppliers.find(
                    (supplier) =>
                      supplier.companyName.toLowerCase() === value.toLowerCase(),
                  );

                  if (matchedSupplier?.contactEmail) {
                    updateField('supplierEmail', matchedSupplier.contactEmail);
                  }
                }}
                className="input"
                placeholder="Supplier company"
              />
              <datalist id="supplier-options">
                {suppliers.map((supplier) => (
                  <option key={supplier.id} value={supplier.companyName} />
                ))}
              </datalist>
            </Field>

            <Field label="Supplier email">
              <input
                type="email"
                value={form.supplierEmail}
                onChange={(event) => updateField('supplierEmail', event.target.value)}
                className="input"
                placeholder="orders@supplier.co.za"
              />
            </Field>
          </div>

          <Field label="Description">
            <textarea
              rows={3}
              value={form.description}
              onChange={(event) => updateField('description', event.target.value)}
              className="input resize-none"
              placeholder="Optional product notes or description"
            />
          </Field>

          {mode === 'edit' && product && (
            <div className="rounded-lg border border-blue-100 bg-blue-50 p-3 text-sm text-blue-800">
              Current stock is <strong>{product.stockQuantity}</strong>. Use
              <strong> Adjust Stock</strong> from the inventory table to change quantity
              so the adjustment reason is retained in the audit trail.
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
              className="flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-400"
            >
              {saving ? <Loader2 className="animate-spin" size={17} /> : <Save size={17} />}
              {saving
                ? 'Saving...'
                : mode === 'create'
                  ? 'Add Product'
                  : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>

      <style jsx>{`
        .input {
          width: 100%;
          border-radius: 0.5rem;
          border: 1px solid rgb(209 213 219);
          padding: 0.625rem 0.75rem;
          color: rgb(17 24 39);
          outline: none;
          transition: border-color 150ms, box-shadow 150ms;
        }

        .input:focus {
          border-color: rgb(59 130 246);
          box-shadow: 0 0 0 3px rgb(219 234 254);
        }
      `}</style>
    </div>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-gray-700">{label}</span>
      {children}
    </label>
  );
}
