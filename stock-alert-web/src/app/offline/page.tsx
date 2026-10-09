'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  AlertTriangle,
  CheckCircle2,
  Clock3,
  CloudOff,
  Loader2,
  RefreshCw,
} from 'lucide-react';
import {
  getOfflineSales,
  retryOfflineSale,
  type OfflineSaleQueueItem,
} from '@/lib/offline';

export default function OfflineQueuePage() {
  const [items, setItems] = useState<OfflineSaleQueueItem[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    try {
      setItems(await getOfflineSales());
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();

    const handleChange = () => void load();
    window.addEventListener(
      'stockalert:offline-queue-changed',
      handleChange,
    );
    window.addEventListener(
      'stockalert:sync-complete',
      handleChange,
    );

    return () => {
      window.removeEventListener(
        'stockalert:offline-queue-changed',
        handleChange,
      );
      window.removeEventListener(
        'stockalert:sync-complete',
        handleChange,
      );
    };
  }, [load]);

  const retry = async (operationId: string) => {
    await retryOfflineSale(operationId);

    window.dispatchEvent(
      new CustomEvent('stockalert:request-sync'),
    );

    await load();
  };

  const conflicts = items.filter(
    (item) => item.status === 'conflict',
  ).length;
  const pending = items.length - conflicts;

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-6xl">
        <div className="mb-8">
          <p className="text-sm font-medium text-blue-600">
            Resilience
          </p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <CloudOff className="text-blue-600" />
            Offline Sync Queue
          </h1>
          <p className="mt-1 max-w-3xl text-sm leading-6 text-gray-500">
            Sales captured during connectivity outages remain on this
            device until the API confirms them. Conflicts are never
            silently forced through.
          </p>
        </div>

        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <Summary
            label="Queued"
            value={items.length}
            icon={<Clock3 size={17} />}
          />
          <Summary
            label="Pending sync"
            value={pending}
            icon={<RefreshCw size={17} />}
          />
          <Summary
            label="Conflicts"
            value={conflicts}
            icon={<AlertTriangle size={17} />}
          />
        </div>

        <div className="mb-5 rounded-xl border border-blue-200 bg-blue-50 p-5 text-sm leading-6 text-blue-900">
          <strong>Important:</strong> a conflict means the server stock
          or price changed while this device was disconnected. StockAlert
          keeps the transaction visible here instead of changing the
          customer&apos;s sale or stock figures silently.
        </div>

        <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px] text-left">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Captured
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Product
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Total
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Status
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Details
                  </th>
                  <th className="p-4 text-sm font-semibold text-gray-600">
                    Action
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr
                    key={item.operationId}
                    className="border-b border-gray-100 last:border-0"
                  >
                    <td className="p-4 text-sm text-gray-500">
                      {new Date(item.queuedAt).toLocaleString()}
                    </td>
                    <td className="p-4 font-medium text-gray-900">
                      {item.productName}
                    </td>
                    <td className="p-4 font-semibold text-gray-900">
                      {item.estimatedTotal.toFixed(2)}
                    </td>
                    <td className="p-4">
                      {item.status === 'conflict' ? (
                        <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2.5 py-1 text-xs font-bold text-red-700">
                          <AlertTriangle size={12} />
                          Conflict
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2.5 py-1 text-xs font-bold text-amber-700">
                          <Clock3 size={12} />
                          Pending
                        </span>
                      )}
                    </td>
                    <td className="max-w-sm p-4 text-sm text-gray-600">
                      {item.error ??
                        'Waiting for connectivity and server confirmation.'}
                    </td>
                    <td className="p-4">
                      {item.status === 'conflict' ? (
                        <button
                          onClick={() =>
                            void retry(item.operationId)
                          }
                          className="inline-flex items-center gap-1 rounded-md border border-gray-300 px-3 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-50"
                        >
                          <RefreshCw size={13} />
                          Retry after review
                        </button>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-xs font-semibold text-gray-400">
                          <CheckCircle2 size={13} />
                          Auto-sync enabled
                        </span>
                      )}
                    </td>
                  </tr>
                ))}

                {!loading && items.length === 0 && (
                  <tr>
                    <td
                      colSpan={6}
                      className="p-12 text-center text-sm text-gray-500"
                    >
                      No offline sales are waiting to sync.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            {loading && (
              <div className="flex items-center justify-center p-10 text-sm text-gray-500">
                <Loader2
                  className="mr-2 animate-spin"
                  size={18}
                />
                Loading offline queue...
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function Summary({
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
      <div className="flex items-center gap-2 text-sm text-gray-500">
        {icon}
        {label}
      </div>
      <p className="mt-1 text-2xl font-bold text-gray-900">
        {value}
      </p>
    </div>
  );
}
