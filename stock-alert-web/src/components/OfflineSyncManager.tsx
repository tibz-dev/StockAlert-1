'use client';

import { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import {
  AlertTriangle,
  CloudOff,
  Loader2,
  RefreshCw,
  Wifi,
} from 'lucide-react';
import api from '@/lib/api';
import {
  getOfflineSales,
  markOfflineSaleConflict,
  removeOfflineSale,
  type OfflineSaleQueueItem,
} from '@/lib/offline';

export default function OfflineSyncManager() {
  const [online, setOnline] = useState(true);
  const [queue, setQueue] = useState<OfflineSaleQueueItem[]>([]);
  const [syncing, setSyncing] = useState(false);

  const refresh = useCallback(async () => {
    try {
      setQueue(await getOfflineSales());
    } catch {
      setQueue([]);
    }
  }, []);

  const sync = useCallback(async () => {
    if (
      typeof navigator === 'undefined' ||
      !navigator.onLine ||
      syncing
    ) {
      return;
    }

    const current = await getOfflineSales();
    const pending = current.filter(
      (item) => item.status === 'pending',
    );

    if (pending.length === 0) {
      await refresh();
      return;
    }

    setSyncing(true);

    try {
      for (const item of pending) {
        try {
          await api.post('/sales', item.request);
          await removeOfflineSale(item.operationId);
        } catch (error: unknown) {
          const status =
            typeof error === 'object' &&
            error !== null &&
            'response' in error
              ? (
                  error as {
                    response?: {
                      status?: number;
                      data?: { message?: string };
                    };
                  }
                ).response
              : undefined;

          if (!status) {
            break;
          }

          const message =
            status.data?.message ??
            'This offline sale requires reconciliation.';

          if (
            status.status === 400 ||
            status.status === 404 ||
            status.status === 409
          ) {
            await markOfflineSaleConflict(
              item.operationId,
              message,
            );
            continue;
          }

          break;
        }
      }
    } finally {
      setSyncing(false);
      await refresh();
      window.dispatchEvent(
        new CustomEvent('stockalert:sync-complete'),
      );
    }
  }, [refresh, syncing]);

  useEffect(() => {
    setOnline(navigator.onLine);
    void refresh();

    const handleOnline = () => {
      setOnline(true);
      void sync();
    };

    const handleOffline = () => setOnline(false);
    const handleQueue = () => void refresh();

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    window.addEventListener(
      'stockalert:offline-queue-changed',
      handleQueue,
    );

    if (navigator.onLine) {
      void sync();
    }

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
      window.removeEventListener(
        'stockalert:offline-queue-changed',
        handleQueue,
      );
    };
  }, [refresh, sync]);

  const conflicts = queue.filter(
    (item) => item.status === 'conflict',
  ).length;
  const pending = queue.length - conflicts;

  if (online && queue.length === 0) {
    return null;
  }

  return (
    <div
      className={
        !online
          ? 'flex flex-wrap items-center justify-between gap-3 border-b border-amber-200 bg-amber-50 px-4 py-2.5 text-sm text-amber-900'
          : conflicts > 0
            ? 'flex flex-wrap items-center justify-between gap-3 border-b border-red-200 bg-red-50 px-4 py-2.5 text-sm text-red-900'
            : 'flex flex-wrap items-center justify-between gap-3 border-b border-blue-200 bg-blue-50 px-4 py-2.5 text-sm text-blue-900'
      }
    >
      <div className="flex items-center gap-2">
        {!online ? (
          <CloudOff size={16} />
        ) : conflicts > 0 ? (
          <AlertTriangle size={16} />
        ) : (
          <Wifi size={16} />
        )}

        <span className="font-semibold">
          {!online
            ? 'Offline mode'
            : conflicts > 0
              ? 'Sync needs attention'
              : syncing
                ? 'Syncing queued sales'
                : 'Sales pending sync'}
        </span>

        <span>
          {pending > 0
            ? `${pending} pending`
            : ''}
          {pending > 0 && conflicts > 0 ? ' · ' : ''}
          {conflicts > 0
            ? `${conflicts} conflict${conflicts === 1 ? '' : 's'}`
            : ''}
        </span>
      </div>

      <div className="flex items-center gap-2">
        {online && pending > 0 && (
          <button
            onClick={() => void sync()}
            disabled={syncing}
            className="inline-flex items-center gap-1 rounded-md border border-current/20 bg-white/70 px-2.5 py-1 text-xs font-bold disabled:opacity-50"
          >
            {syncing ? (
              <Loader2 className="animate-spin" size={13} />
            ) : (
              <RefreshCw size={13} />
            )}
            Sync now
          </button>
        )}

        <Link
          href="/offline"
          className="rounded-md border border-current/20 bg-white/70 px-2.5 py-1 text-xs font-bold"
        >
          View queue
        </Link>
      </div>
    </div>
  );
}
