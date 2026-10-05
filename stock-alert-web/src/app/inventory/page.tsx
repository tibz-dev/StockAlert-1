'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  Download,
  Loader2,
  Mail,
  Package,
} from 'lucide-react';
import api from '@/lib/api';

interface Product {
  id: string;
  name: string;
  price: number;
  stockQuantity: number;
  categoryName: string;
  isLowStock: boolean;
  supplierName: string;
  supplierEmail: string | null;
  externalId: string | null;
}

export default function InventoryPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

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
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-sm font-medium text-blue-600">Products</p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <Package className="text-blue-600" />
            Stock Inventory
          </h1>
        </div>

        <button
          onClick={handleDownloadReport}
          className="flex items-center justify-center gap-2 rounded-lg bg-green-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-green-700"
        >
          <Download size={18} />
          Export CSV
        </button>
      </div>

      {error && (
        <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[900px] border-collapse text-left">
            <thead className="border-b border-gray-200 bg-gray-50">
              <tr>
                <th className="p-4 font-semibold text-gray-600">Product</th>
                <th className="p-4 font-semibold text-gray-600">Category</th>
                <th className="p-4 font-semibold text-gray-600">Supplier</th>
                <th className="p-4 font-semibold text-gray-600">Stock</th>
                <th className="p-4 font-semibold text-gray-600">Unit Price</th>
                <th className="p-4 font-semibold text-gray-600">Status</th>
                <th className="p-4 font-semibold text-gray-600">Action</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr
                  key={product.id}
                  className="border-b border-gray-100 transition last:border-b-0 hover:bg-gray-50"
                >
                  <td className="p-4 font-medium text-gray-900">{product.name}</td>
                  <td className="p-4 text-gray-500">{product.categoryName}</td>
                  <td className="p-4 text-gray-500">{product.supplierName}</td>
                  <td className="p-4 font-mono text-gray-800">{product.stockQuantity}</td>
                  <td className="p-4 text-gray-800">{product.price.toFixed(2)}</td>
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
                    {product.isLowStock && product.supplierEmail ? (
                      <a
                        href={`mailto:${product.supplierEmail}?subject=${encodeURIComponent(
                          `Stock reorder request: ${product.name}`,
                        )}`}
                        className="inline-flex items-center gap-1.5 rounded-md bg-blue-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-blue-700"
                      >
                        <Mail size={14} />
                        Contact supplier
                      </a>
                    ) : (
                      <span className="text-xs text-gray-400">No action</span>
                    )}
                  </td>
                </tr>
              ))}

              {products.length === 0 && (
                <tr>
                  <td colSpan={7} className="p-10 text-center text-sm text-gray-500">
                    No products have been added yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
