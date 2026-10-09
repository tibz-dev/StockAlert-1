'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  Loader2,
  Mail,
  Package,
  Eye,
  Pencil,
  Plus,
  Search,
  Trash2,
  Truck,
} from 'lucide-react';
import api from '@/lib/api';
import SupplierFormModal from '@/components/suppliers/SupplierFormModal';
import type { Supplier, SupplierProduct } from '@/types/supplier';

export default function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [search, setSearch] = useState('');
  const [modal, setModal] = useState<{
    mode: 'create' | 'edit';
    supplier?: Supplier;
  } | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [viewingSupplier, setViewingSupplier] = useState<Supplier | null>(null);

  const loadSuppliers = useCallback(async () => {
    try {
      setError('');
      const response = await api.get<Supplier[]>('/suppliers');
      setSuppliers(response.data);
    } catch {
      setError('Unable to load suppliers.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadSuppliers();
  }, [loadSuppliers]);

  const filteredSuppliers = useMemo(() => {
    const query = search.trim().toLowerCase();

    if (!query) {
      return suppliers;
    }

    return suppliers.filter(
      (supplier) =>
        supplier.companyName.toLowerCase().includes(query) ||
        supplier.contactEmail?.toLowerCase().includes(query) ||
        supplier.productNames.some((name) =>
          name.toLowerCase().includes(query),
        ),
    );
  }, [search, suppliers]);

  const totalLinkedProducts = suppliers.reduce(
    (sum, supplier) => sum + supplier.productCount,
    0,
  );

  const totalLowStock = suppliers.reduce(
    (sum, supplier) => sum + supplier.lowStockProductCount,
    0,
  );

  const refreshWithMessage = async (message: string) => {
    await loadSuppliers();
    setSuccessMessage(message);
    window.setTimeout(() => setSuccessMessage(''), 3000);
  };

  const handleDelete = async (supplier: Supplier) => {
    const confirmed = window.confirm(
      `Delete supplier "${supplier.companyName}"? Suppliers linked to products cannot be deleted.`,
    );

    if (!confirmed) {
      return;
    }

    try {
      setDeletingId(supplier.id);
      setError('');
      await api.delete(`/suppliers/${supplier.id}`);
      await refreshWithMessage('Supplier deleted.');
    } catch {
      setError(
        'Unable to delete this supplier. Reassign linked products before deleting it.',
      );
    } finally {
      setDeletingId(null);
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading suppliers...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-sm font-medium text-blue-600">Procurement contacts</p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <Truck className="text-blue-600" />
            Suppliers
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            Manage supplier contacts and see which suppliers have low-stock products.
          </p>
        </div>

        <button
          onClick={() => setModal({ mode: 'create' })}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700"
        >
          <Plus size={18} />
          Add Supplier
        </button>
      </div>

      {error && (
        <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {successMessage && (
        <div className="mb-5 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
          {successMessage}
        </div>
      )}

      <div className="mb-5 grid gap-4 md:grid-cols-3">
        <SummaryCard
          label="Suppliers"
          value={suppliers.length}
          icon={<Truck size={18} />}
        />
        <SummaryCard
          label="Linked products"
          value={totalLinkedProducts}
          icon={<Package size={18} />}
        />
        <SummaryCard
          label="Low-stock products"
          value={totalLowStock}
          icon={<AlertTriangle size={18} />}
        />
      </div>

      <div className="mb-5 rounded-xl border border-gray-200 bg-white p-4 shadow-sm">
        <label className="relative block">
          <Search
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-3 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            placeholder="Search supplier, email or product name"
          />
        </label>
      </div>

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[820px] text-left">
            <thead className="border-b border-gray-200 bg-gray-50">
              <tr>
                <th className="p-4 text-sm font-semibold text-gray-600">Supplier</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Contact</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Products</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Low Stock</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredSuppliers.map((supplier) => (
                <tr
                  key={supplier.id}
                  className="border-b border-gray-100 last:border-0 hover:bg-gray-50"
                >
                  <td className="p-4 font-medium text-gray-900">
                    {supplier.companyName}
                  </td>
                  <td className="p-4 text-sm text-gray-600">
                    {supplier.contactEmail ? (
                      <a
                        href={`mailto:${supplier.contactEmail}`}
                        className="inline-flex items-center gap-1.5 text-blue-600 hover:underline"
                      >
                        <Mail size={14} />
                        {supplier.contactEmail}
                      </a>
                    ) : (
                      <span className="text-gray-400">No email</span>
                    )}
                  </td>
                  <td className="p-4 text-sm font-semibold text-gray-700">
                    {supplier.productCount}
                  </td>
                  <td className="p-4">
                    {supplier.lowStockProductCount > 0 ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-1 text-xs font-bold text-red-700">
                        <AlertTriangle size={13} />
                        {supplier.lowStockProductCount}
                      </span>
                    ) : (
                      <span className="rounded-full bg-green-50 px-2 py-1 text-xs font-bold text-green-700">
                        0
                      </span>
                    )}
                  </td>
                  <td className="p-4">
                    <div className="flex gap-2">
                      <button
                        onClick={() => setViewingSupplier(supplier)}
                        className="inline-flex items-center gap-1 rounded-md border border-blue-200 bg-blue-50 px-2.5 py-1.5 text-xs font-semibold text-blue-700 transition hover:bg-blue-100"
                      >
                        <Eye size={13} />
                        View Products
                      </button>

                      <button
                        onClick={() => setModal({ mode: 'edit', supplier })}
                        className="inline-flex items-center gap-1 rounded-md border border-gray-300 bg-white px-2.5 py-1.5 text-xs font-semibold text-gray-700 transition hover:bg-gray-50"
                      >
                        <Pencil size={13} />
                        Edit
                      </button>

                      <button
                        onClick={() => void handleDelete(supplier)}
                        disabled={deletingId === supplier.id}
                        className="inline-flex items-center gap-1 rounded-md border border-red-200 bg-red-50 px-2.5 py-1.5 text-xs font-semibold text-red-700 transition hover:bg-red-100 disabled:opacity-50"
                      >
                        {deletingId === supplier.id ? (
                          <Loader2 className="animate-spin" size={13} />
                        ) : (
                          <Trash2 size={13} />
                        )}
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}

              {filteredSuppliers.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-12 text-center text-sm text-gray-500">
                    No suppliers match the current search.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {viewingSupplier && (
        <SupplierProductsModal
          supplier={viewingSupplier}
          onClose={() => setViewingSupplier(null)}
        />
      )}

      {modal && (
        <SupplierFormModal
          mode={modal.mode}
          supplier={modal.supplier}
          onClose={() => setModal(null)}
          onSaved={() =>
            refreshWithMessage(
              modal.mode === 'create'
                ? 'Supplier added successfully.'
                : 'Supplier updated successfully.',
            )
          }
        />
      )}
    </div>
  );
}

function SupplierProductsModal({
  supplier,
  onClose,
}: {
  supplier: Supplier;
  onClose: () => void;
}) {
  const [products, setProducts] = useState<SupplierProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const response = await api.get<SupplierProduct[]>(
          `/suppliers/${supplier.id}/products`,
        );
        setProducts(response.data);
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [supplier.id]);

  const filtered = useMemo(() => {
    const query = search.trim().toLowerCase();

    if (!query) return products;

    return products.filter(
      (product) =>
        product.name.toLowerCase().includes(query) ||
        product.barcode?.includes(query),
    );
  }, [products, search]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="max-h-[90vh] w-full max-w-4xl overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="flex items-start justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">Supplier catalogue</p>
            <h2 className="mt-1 text-xl font-bold text-gray-900">
              {supplier.companyName}
            </h2>
            <p className="mt-1 text-sm text-gray-500">
              {supplier.productCount} active product
              {supplier.productCount === 1 ? '' : 's'} linked to this supplier.
            </p>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-2 text-gray-400 hover:bg-gray-100"
          >
            ×
          </button>
        </div>

        <div className="p-6">
          <label className="relative mb-4 block">
            <Search
              size={17}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
            />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search product name or barcode"
              className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-3 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </label>

          <div className="max-h-[58vh] overflow-auto rounded-lg border border-gray-200">
            <table className="w-full min-w-[720px] text-left">
              <thead className="sticky top-0 bg-gray-50 text-xs uppercase tracking-wide text-gray-500">
                <tr>
                  <th className="p-3">Product</th>
                  <th className="p-3">Barcode</th>
                  <th className="p-3 text-right">Price</th>
                  <th className="p-3 text-right">On hand</th>
                  <th className="p-3 text-right">Reserved</th>
                  <th className="p-3 text-right">Available</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((product) => (
                  <tr key={product.id} className="border-t border-gray-100">
                    <td className="p-3 font-medium text-gray-900">
                      {product.name}
                    </td>
                    <td className="p-3 text-sm text-gray-500">
                      {product.barcode ?? '—'}
                    </td>
                    <td className="p-3 text-right text-sm text-gray-700">
                      {product.price.toFixed(2)}
                    </td>
                    <td className="p-3 text-right text-sm text-gray-700">
                      {product.onHand}
                    </td>
                    <td className="p-3 text-right text-sm text-violet-700">
                      {product.reserved}
                    </td>
                    <td className="p-3 text-right text-sm font-semibold text-gray-900">
                      {product.available}
                    </td>
                  </tr>
                ))}

                {!loading && filtered.length === 0 && (
                  <tr>
                    <td colSpan={6} className="p-10 text-center text-sm text-gray-500">
                      No products match this search.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            {loading && (
              <div className="flex items-center justify-center p-10 text-sm text-gray-500">
                <Loader2 className="mr-2 animate-spin" size={17} />
                Loading supplier products...
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function SummaryCard({
  label,
  value,
  icon,
}: {
  label: string;
  value: number;
  icon: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className="mb-3 w-fit rounded-lg bg-blue-50 p-2 text-blue-600">{icon}</div>
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}
