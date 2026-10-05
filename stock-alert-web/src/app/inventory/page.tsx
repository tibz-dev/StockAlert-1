'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Download,
  Loader2,
  Mail,
  Package,
  PackagePlus,
  Pencil,
  Search,
  SlidersHorizontal,
  Trash2,
} from 'lucide-react';
import api from '@/lib/api';
import ProductFormModal from '@/components/inventory/ProductFormModal';
import StockAdjustModal from '@/components/inventory/StockAdjustModal';
import type { Product } from '@/types/inventory';

const PAGE_SIZE = 10;

type StatusFilter = 'all' | 'low' | 'healthy';

export default function InventoryPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [page, setPage] = useState(1);
  const [productModal, setProductModal] = useState<{
    mode: 'create' | 'edit';
    product?: Product;
  } | null>(null);
  const [adjustProduct, setAdjustProduct] = useState<Product | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const fetchProducts = useCallback(async () => {
    try {
      setError('');
      const response = await api.get<Product[]>('/products');
      setProducts(response.data);
    } catch {
      setError('Unable to load inventory.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchProducts();
  }, [fetchProducts]);

  useEffect(() => {
    setPage(1);
  }, [search, statusFilter, categoryFilter]);

  const categories = useMemo(
    () =>
      Array.from(new Set(products.map((product) => product.categoryName)))
        .filter(Boolean)
        .sort((a, b) => a.localeCompare(b)),
    [products],
  );

  const filteredProducts = useMemo(() => {
    const query = search.trim().toLowerCase();

    return products.filter((product) => {
      const matchesSearch =
        !query ||
        product.name.toLowerCase().includes(query) ||
        product.categoryName.toLowerCase().includes(query) ||
        product.supplierName.toLowerCase().includes(query);

      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'low' && product.isLowStock) ||
        (statusFilter === 'healthy' && !product.isLowStock);

      const matchesCategory =
        categoryFilter === 'all' || product.categoryName === categoryFilter;

      return matchesSearch && matchesStatus && matchesCategory;
    });
  }, [products, search, statusFilter, categoryFilter]);

  const totalPages = Math.max(1, Math.ceil(filteredProducts.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const visibleProducts = filteredProducts.slice(
    (safePage - 1) * PAGE_SIZE,
    safePage * PAGE_SIZE,
  );

  const lowStockCount = products.filter((product) => product.isLowStock).length;

  const refreshWithMessage = async (message: string) => {
    await fetchProducts();
    setSuccessMessage(message);
    window.setTimeout(() => setSuccessMessage(''), 3000);
  };

  const handleDelete = async (product: Product) => {
    const confirmed = window.confirm(
      `Delete "${product.name}"? Products with sales history cannot be deleted.`,
    );

    if (!confirmed) {
      return;
    }

    try {
      setDeletingId(product.id);
      setError('');
      await api.delete(`/products/${product.id}`);
      await refreshWithMessage('Product deleted.');
    } catch {
      setError(
        'Unable to delete this product. Products with existing sales history are protected.',
      );
    } finally {
      setDeletingId(null);
    }
  };

  const handleDownloadReport = async () => {
    try {
      const response = await api.get('/products/report/csv', {
        responseType: 'blob',
      });

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.download = `Inventory_Report_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch {
      setError('Failed to generate the inventory report.');
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading inventory...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mb-6 flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div>
          <p className="text-sm font-medium text-blue-600">Inventory management</p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <Package className="text-blue-600" />
            Stock Inventory
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            {products.length} products · {lowStockCount} low-stock alerts
          </p>
        </div>

        <div className="flex flex-col gap-2 sm:flex-row">
          <button
            onClick={handleDownloadReport}
            className="flex items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50"
          >
            <Download size={18} />
            Export CSV
          </button>

          <button
            onClick={() => setProductModal({ mode: 'create' })}
            className="flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700"
          >
            <PackagePlus size={18} />
            Add Product
          </button>
        </div>
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

      <div className="mb-5 grid gap-3 rounded-xl border border-gray-200 bg-white p-4 shadow-sm lg:grid-cols-[1fr_220px_220px]">
        <label className="relative">
          <Search
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-3 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            placeholder="Search product, category or supplier"
          />
        </label>

        <label className="relative">
          <SlidersHorizontal
            size={16}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value as StatusFilter)}
            className="w-full appearance-none rounded-lg border border-gray-300 py-2.5 pl-9 pr-3 text-sm text-gray-900 outline-none focus:border-blue-500"
          >
            <option value="all">All stock statuses</option>
            <option value="low">Low stock</option>
            <option value="healthy">Healthy stock</option>
          </select>
        </label>

        <select
          value={categoryFilter}
          onChange={(event) => setCategoryFilter(event.target.value)}
          className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500"
        >
          <option value="all">All categories</option>
          {categories.map((category) => (
            <option key={category} value={category}>
              {category}
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[1100px] border-collapse text-left">
            <thead className="border-b border-gray-200 bg-gray-50">
              <tr>
                <th className="p-4 text-sm font-semibold text-gray-600">Product</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Category</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Supplier</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Stock</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Unit Price</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Status</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Actions</th>
              </tr>
            </thead>
            <tbody>
              {visibleProducts.map((product) => (
                <tr
                  key={product.id}
                  className="border-b border-gray-100 align-top transition last:border-b-0 hover:bg-gray-50"
                >
                  <td className="p-4">
                    <div className="font-medium text-gray-900">{product.name}</div>
                    {product.description && (
                      <div className="mt-1 max-w-xs truncate text-xs text-gray-500">
                        {product.description}
                      </div>
                    )}
                  </td>
                  <td className="p-4 text-sm text-gray-500">{product.categoryName}</td>
                  <td className="p-4">
                    <div className="text-sm text-gray-700">{product.supplierName}</div>
                    {product.supplierEmail && (
                      <div className="mt-1 text-xs text-gray-400">
                        {product.supplierEmail}
                      </div>
                    )}
                  </td>
                  <td className="p-4 font-mono text-sm font-semibold text-gray-800">
                    {product.stockQuantity}
                  </td>
                  <td className="p-4 text-sm text-gray-800">
                    {product.price.toFixed(2)}
                  </td>
                  <td className="p-4">
                    {product.isLowStock ? (
                      <span className="flex w-fit items-center gap-1 rounded-full bg-red-50 px-2 py-1 text-xs font-bold text-red-600">
                        <AlertCircle size={14} />
                        Low Stock
                      </span>
                    ) : (
                      <span className="flex w-fit items-center gap-1 rounded-full bg-green-50 px-2 py-1 text-xs font-bold text-green-600">
                        <CheckCircle2 size={14} />
                        Healthy
                      </span>
                    )}
                  </td>
                  <td className="p-4">
                    <div className="flex flex-wrap gap-2">
                      <button
                        onClick={() =>
                          setProductModal({ mode: 'edit', product })
                        }
                        className="inline-flex items-center gap-1 rounded-md border border-gray-300 bg-white px-2.5 py-1.5 text-xs font-semibold text-gray-700 transition hover:bg-gray-50"
                      >
                        <Pencil size={13} />
                        Edit
                      </button>

                      <button
                        onClick={() => setAdjustProduct(product)}
                        className="inline-flex items-center gap-1 rounded-md border border-blue-200 bg-blue-50 px-2.5 py-1.5 text-xs font-semibold text-blue-700 transition hover:bg-blue-100"
                      >
                        <SlidersHorizontal size={13} />
                        Adjust
                      </button>

                      {product.isLowStock && product.supplierEmail && (
                        <a
                          href={`mailto:${product.supplierEmail}?subject=${encodeURIComponent(
                            `Stock reorder request: ${product.name}`,
                          )}`}
                          className="inline-flex items-center gap-1 rounded-md border border-green-200 bg-green-50 px-2.5 py-1.5 text-xs font-semibold text-green-700 transition hover:bg-green-100"
                        >
                          <Mail size={13} />
                          Supplier
                        </a>
                      )}

                      <button
                        onClick={() => void handleDelete(product)}
                        disabled={deletingId === product.id}
                        className="inline-flex items-center gap-1 rounded-md border border-red-200 bg-red-50 px-2.5 py-1.5 text-xs font-semibold text-red-700 transition hover:bg-red-100 disabled:opacity-50"
                      >
                        {deletingId === product.id ? (
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

              {visibleProducts.length === 0 && (
                <tr>
                  <td colSpan={7} className="p-10 text-center text-sm text-gray-500">
                    No products match the current filters.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="flex flex-col gap-3 border-t border-gray-200 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-gray-500">
            Showing {visibleProducts.length === 0 ? 0 : (safePage - 1) * PAGE_SIZE + 1}
            {'–'}
            {Math.min(safePage * PAGE_SIZE, filteredProducts.length)} of{' '}
            {filteredProducts.length}
          </p>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              disabled={safePage === 1}
              className="rounded-lg border border-gray-300 p-2 text-gray-600 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40"
              aria-label="Previous page"
            >
              <ChevronLeft size={17} />
            </button>

            <span className="text-sm font-medium text-gray-700">
              Page {safePage} of {totalPages}
            </span>

            <button
              onClick={() =>
                setPage((current) => Math.min(totalPages, current + 1))
              }
              disabled={safePage === totalPages}
              className="rounded-lg border border-gray-300 p-2 text-gray-600 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40"
              aria-label="Next page"
            >
              <ChevronRight size={17} />
            </button>
          </div>
        </div>
      </div>

      {productModal && (
        <ProductFormModal
          mode={productModal.mode}
          product={productModal.product}
          onClose={() => setProductModal(null)}
          onSaved={() =>
            refreshWithMessage(
              productModal.mode === 'create'
                ? 'Product added successfully.'
                : 'Product updated successfully.',
            )
          }
        />
      )}

      {adjustProduct && (
        <StockAdjustModal
          product={adjustProduct}
          onClose={() => setAdjustProduct(null)}
          onSaved={() => refreshWithMessage('Stock adjusted successfully.')}
        />
      )}
    </div>
  );
}
