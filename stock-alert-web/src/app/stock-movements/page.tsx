'use client';

import Link from 'next/link';
import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ArrowDown,
  ArrowLeft,
  ArrowUp,
  Boxes,
  Filter,
  Loader2,
  Search,
} from 'lucide-react';
import api from '@/lib/api';
import type { StockMovement } from '@/types/inventory';

type MovementFilter = 'all' | 'in' | 'out';

export default function StockMovementsPage() {
  const [movements, setMovements] = useState<StockMovement[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [movementFilter, setMovementFilter] = useState<MovementFilter>('all');
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);

  const loadMovements = useCallback(async (productId?: string | null) => {
    try {
      setLoading(true);
      setError('');

      const response = await api.get<StockMovement[]>('/stock-movements', {
        params: {
          ...(productId ? { productId } : {}),
          take: 500,
        },
      });

      setMovements(response.data);
    } catch {
      setError('Unable to load stock movement history.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const productId =
      typeof window !== 'undefined'
        ? new URLSearchParams(window.location.search).get('productId')
        : null;

    setSelectedProductId(productId);
    void loadMovements(productId);
  }, [loadMovements]);

  const filteredMovements = useMemo(() => {
    const query = search.trim().toLowerCase();

    return movements.filter((movement) => {
      const matchesSearch =
        !query ||
        movement.productName.toLowerCase().includes(query) ||
        movement.reason.toLowerCase().includes(query) ||
        movement.performedBy.toLowerCase().includes(query);

      const matchesDirection =
        movementFilter === 'all' ||
        (movementFilter === 'in' && movement.quantityChange > 0) ||
        (movementFilter === 'out' && movement.quantityChange < 0);

      return matchesSearch && matchesDirection;
    });
  }, [movements, movementFilter, search]);

  const unitsAdded = movements
    .filter((movement) => movement.quantityChange > 0)
    .reduce((sum, movement) => sum + movement.quantityChange, 0);

  const unitsRemoved = Math.abs(
    movements
      .filter((movement) => movement.quantityChange < 0)
      .reduce((sum, movement) => sum + movement.quantityChange, 0),
  );

  const clearProductFilter = async () => {
    window.history.replaceState({}, '', '/stock-movements');
    setSelectedProductId(null);
    await loadMovements(null);
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <p className="text-sm font-medium text-blue-600">Inventory traceability</p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <Boxes className="text-blue-600" />
            Stock Movements
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            Every manual stock adjustment with before/after quantities and reason.
          </p>
        </div>

        <Link
          href="/inventory"
          className="inline-flex items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50"
        >
          <ArrowLeft size={17} />
          Back to Inventory
        </Link>
      </div>

      {selectedProductId && (
        <div className="mb-5 flex flex-col gap-3 rounded-lg border border-violet-200 bg-violet-50 p-4 text-sm text-violet-800 sm:flex-row sm:items-center sm:justify-between">
          <span>Showing movement history for the selected product.</span>
          <button
            onClick={() => void clearProductFilter()}
            className="font-semibold text-violet-700 underline underline-offset-2"
          >
            Show all movements
          </button>
        </div>
      )}

      {error && (
        <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <div className="mb-5 grid gap-4 md:grid-cols-3">
        <SummaryCard
          label="Recorded movements"
          value={movements.length}
          icon={<Boxes size={18} />}
        />
        <SummaryCard
          label="Units added"
          value={unitsAdded}
          icon={<ArrowUp size={18} />}
        />
        <SummaryCard
          label="Units removed"
          value={unitsRemoved}
          icon={<ArrowDown size={18} />}
        />
      </div>

      <div className="mb-5 grid gap-3 rounded-xl border border-gray-200 bg-white p-4 shadow-sm md:grid-cols-[1fr_220px]">
        <label className="relative">
          <Search
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-3 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            placeholder="Search product, reason or user"
          />
        </label>

        <label className="relative">
          <Filter
            size={16}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <select
            value={movementFilter}
            onChange={(event) =>
              setMovementFilter(event.target.value as MovementFilter)
            }
            className="w-full appearance-none rounded-lg border border-gray-300 py-2.5 pl-9 pr-3 text-sm text-gray-900 outline-none focus:border-blue-500"
          >
            <option value="all">All movements</option>
            <option value="in">Stock added</option>
            <option value="out">Stock removed</option>
          </select>
        </label>
      </div>

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        {loading ? (
          <div className="flex min-h-72 items-center justify-center text-gray-500">
            <Loader2 className="mr-2 animate-spin" size={20} />
            Loading stock movements...
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[980px] text-left">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="p-4 text-sm font-semibold text-gray-600">Date</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Product</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Change</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Stock</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Reason</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Performed by</th>
                </tr>
              </thead>
              <tbody>
                {filteredMovements.map((movement) => (
                  <tr
                    key={movement.id}
                    className="border-b border-gray-100 align-top last:border-0 hover:bg-gray-50"
                  >
                    <td className="p-4 text-sm text-gray-500">
                      {new Date(movement.createdAt).toLocaleString()}
                    </td>
                    <td className="p-4 font-medium text-gray-900">
                      {movement.productName}
                    </td>
                    <td className="p-4">
                      <span
                        className={
                          movement.quantityChange > 0
                            ? 'inline-flex items-center gap-1 rounded-full bg-green-50 px-2 py-1 text-xs font-bold text-green-700'
                            : 'inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-1 text-xs font-bold text-red-700'
                        }
                      >
                        {movement.quantityChange > 0 ? (
                          <ArrowUp size={13} />
                        ) : (
                          <ArrowDown size={13} />
                        )}
                        {movement.quantityChange > 0 ? '+' : ''}
                        {movement.quantityChange}
                      </span>
                    </td>
                    <td className="p-4 text-sm font-medium text-gray-700">
                      {movement.previousQuantity} → {movement.newQuantity}
                    </td>
                    <td className="max-w-sm p-4 text-sm text-gray-600">
                      {movement.reason}
                    </td>
                    <td className="p-4 text-sm text-gray-600">
                      {movement.performedBy}
                    </td>
                  </tr>
                ))}

                {filteredMovements.length === 0 && (
                  <tr>
                    <td
                      colSpan={6}
                      className="p-12 text-center text-sm text-gray-500"
                    >
                      No stock movements match the current view.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
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
      <div className="mb-3 w-fit rounded-lg bg-blue-50 p-2 text-blue-600">
        {icon}
      </div>
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}
