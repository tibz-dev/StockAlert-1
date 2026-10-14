'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  AlertTriangle,
  Barcode,
  CheckCircle2,
  ClipboardCheck,
  Loader2,
  MapPin,
  Plus,
  RefreshCw,
  ShieldCheck,
} from 'lucide-react';
import api from '@/lib/api';
import { getSessionInfo } from '@/lib/auth';
import type { StocktakeSession } from '@/types/stocktake';

export default function StocktakesPage() {
  const router = useRouter();
  const session = getSessionInfo();
  const canManage =
    session.roles.includes('Owner') ||
    session.roles.includes('Manager');

  const [stocktakes, setStocktakes] = useState<StocktakeSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState('');
  const [title, setTitle] = useState('');
  const [location, setLocation] = useState('');
  const [zone, setZone] = useState('');
  const [source, setSource] = useState<'StockAlert' | 'SmartTrade'>(
    'StockAlert',
  );
  const [blindCount, setBlindCount] = useState(true);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError('');
      const response = await api.get<StocktakeSession[]>('/stocktakes');
      setStocktakes(response.data);
    } catch {
      setError('Unable to load stocktakes.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const active = useMemo(
    () =>
      stocktakes.filter(
        (item) =>
          item.status === 'Counting' ||
          item.status === 'Review',
      ).length,
    [stocktakes],
  );

  const totalVarianceItems = stocktakes.reduce(
    (sum, item) => sum + item.varianceItems,
    0,
  );

  const create = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!title.trim()) {
      setError('Give the stocktake a title.');
      return;
    }

    try {
      setCreating(true);
      setError('');

      const response = await api.post<{ id: string }>('/stocktakes', {
        title: title.trim(),
        location: location.trim() || null,
        zone: zone.trim() || null,
        source,
        blindCount,
      });

      router.push(`/stocktakes/${response.data.id}`);
    } catch (requestError: unknown) {
      const message =
        typeof requestError === 'object' &&
        requestError !== null &&
        'response' in requestError
          ? (
              requestError as {
                response?: { data?: { message?: string } };
              }
            ).response?.data?.message
          : undefined;

      setError(message ?? 'Unable to create stocktake.');
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-7 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-sm font-semibold text-blue-600">
              Physical stock control
            </p>
            <h1 className="mt-1 flex items-center gap-2 text-2xl font-bold text-gray-900">
              <ClipboardCheck className="text-blue-600" />
              Stocktakes & Reconciliation
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-gray-500">
              Count physical stock independently from the expected system
              quantity. Variances require recount and manager review instead
              of silently changing inventory.
            </p>
          </div>

          {canManage && (
            <button
              onClick={() => setShowCreate((current) => !current)}
              className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-700"
            >
              <Plus size={17} />
              New Stocktake
            </button>
          )}
        </div>

        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {showCreate && canManage && (
          <form
            onSubmit={create}
            className="mb-6 rounded-2xl border border-gray-200 bg-white p-6 shadow-sm"
          >
            <div className="mb-5">
              <h2 className="font-bold text-gray-900">
                Start stocktake session
              </h2>
              <p className="mt-1 text-sm text-gray-500">
                Use StockAlert as the test source now. SmartTrade becomes
                selectable once the vendor API/CSV/database feed is configured.
              </p>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <label>
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Title
                </span>
                <input
                  required
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="October cycle count"
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
                />
              </label>

              <label>
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Location
                </span>
                <input
                  value={location}
                  onChange={(event) => setLocation(event.target.value)}
                  placeholder="Main Store"
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
                />
              </label>

              <label>
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Zone / aisle
                </span>
                <input
                  value={zone}
                  onChange={(event) => setZone(event.target.value)}
                  placeholder="Paint / Aisle 2"
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
                />
              </label>

              <label>
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Expected-stock source
                </span>
                <select
                  value={source}
                  onChange={(event) =>
                    setSource(
                      event.target.value as
                        | 'StockAlert'
                        | 'SmartTrade',
                    )
                  }
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
                >
                  <option value="StockAlert">
                    StockAlert catalogue
                  </option>
                  <option value="SmartTrade">
                    SmartTrade snapshot
                  </option>
                </select>
              </label>
            </div>

            <label className="mt-5 flex items-start gap-3 rounded-lg border border-blue-100 bg-blue-50 p-4">
              <input
                type="checkbox"
                checked={blindCount}
                onChange={(event) =>
                  setBlindCount(event.target.checked)
                }
                className="mt-1 h-4 w-4"
              />
              <span>
                <span className="block text-sm font-semibold text-blue-900">
                  Blind count
                </span>
                <span className="mt-1 block text-xs leading-5 text-blue-700">
                  Counters do not see the expected quantity until they submit
                  their first count. Owner/Manager can still supervise the
                  expected snapshot.
                </span>
              </span>
            </label>

            {source === 'SmartTrade' && (
              <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
                SmartTrade integration is intentionally not faked. Until the
                hardware store/vendor provides supported API, CSV or database
                access, creation from SmartTrade will be blocked rather than
                inventing quantities.
              </div>
            )}

            <div className="mt-5 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-semibold text-gray-700"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={creating}
                className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white disabled:bg-blue-400"
              >
                {creating ? (
                  <Loader2 className="animate-spin" size={16} />
                ) : (
                  <ClipboardCheck size={16} />
                )}
                Create Session
              </button>
            </div>
          </form>
        )}

        <div className="mb-6 grid gap-4 md:grid-cols-4">
          <Summary
            label="Sessions"
            value={stocktakes.length}
            icon={<ClipboardCheck size={17} />}
          />
          <Summary
            label="Active"
            value={active}
            icon={<RefreshCw size={17} />}
          />
          <Summary
            label="Variance items"
            value={totalVarianceItems}
            icon={<AlertTriangle size={17} />}
          />
          <Summary
            label="Control"
            value="Blind + Recount"
            icon={<ShieldCheck size={17} />}
          />
        </div>

        <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1000px] text-left">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Session
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Location
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Source
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Progress
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Exceptions
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Status
                  </th>
                </tr>
              </thead>
              <tbody>
                {stocktakes.map((item) => {
                  const percent =
                    item.totalItems > 0
                      ? Math.round(
                          (item.countedItems / item.totalItems) *
                            100,
                        )
                      : 0;

                  return (
                    <tr
                      key={item.id}
                      onClick={() =>
                        router.push(`/stocktakes/${item.id}`)
                      }
                      className="cursor-pointer border-b border-gray-100 last:border-0 hover:bg-gray-50"
                    >
                      <td className="p-4">
                        <div className="font-semibold text-gray-900">
                          {item.title}
                        </div>
                        <div className="mt-1 text-xs text-gray-400">
                          {item.referenceNumber}
                        </div>
                      </td>
                      <td className="p-4 text-sm text-gray-600">
                        <div className="flex items-center gap-1">
                          <MapPin size={14} />
                          {item.location ?? 'Not specified'}
                        </div>
                        {item.zone && (
                          <div className="mt-1 text-xs text-gray-400">
                            {item.zone}
                          </div>
                        )}
                      </td>
                      <td className="p-4">
                        <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-700">
                          <Barcode size={12} />
                          {item.source}
                        </span>
                      </td>
                      <td className="p-4">
                        <div className="text-sm font-semibold text-gray-800">
                          {item.countedItems} / {item.totalItems}
                        </div>
                        <div className="mt-2 h-1.5 w-32 overflow-hidden rounded-full bg-gray-100">
                          <div
                            className="h-full rounded-full bg-blue-600"
                            style={{ width: `${percent}%` }}
                          />
                        </div>
                      </td>
                      <td className="p-4 text-sm">
                        <div className="font-semibold text-red-700">
                          {item.varianceItems} variance
                        </div>
                        <div className="mt-1 text-xs text-gray-500">
                          {item.recountRequiredItems} recount ·{' '}
                          {item.reviewRequiredItems} review
                        </div>
                      </td>
                      <td className="p-4">
                        <StatusBadge status={item.status} />
                      </td>
                    </tr>
                  );
                })}

                {!loading && stocktakes.length === 0 && (
                  <tr>
                    <td
                      colSpan={6}
                      className="p-12 text-center text-sm text-gray-500"
                    >
                      No stocktake sessions yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            {loading && (
              <div className="flex items-center justify-center p-12 text-sm text-gray-500">
                <Loader2 className="mr-2 animate-spin" size={18} />
                Loading stocktakes...
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const style =
    status === 'Completed'
      ? 'bg-green-50 text-green-700'
      : status === 'Review'
        ? 'bg-amber-50 text-amber-700'
        : status === 'Cancelled'
          ? 'bg-gray-100 text-gray-600'
          : 'bg-blue-50 text-blue-700';

  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${style}`}>
      {status}
    </span>
  );
}

function Summary({
  label,
  value,
  icon,
}: {
  label: string;
  value: string | number;
  icon: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className="mb-2 flex items-center gap-2 text-sm text-gray-500">
        {icon}
        {label}
      </div>
      <div className="text-2xl font-bold text-gray-900">
        {value}
      </div>
    </div>
  );
}
