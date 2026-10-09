'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Activity,
  ChevronLeft,
  ChevronRight,
  Download,
  History,
  Loader2,
  Printer,
  Search,
  ShieldCheck,
  User,
} from 'lucide-react';
import api from '@/lib/api';

interface AuditLog {
  id: string;
  entityName: string;
  entityId: string | null;
  action: string;
  userId: string;
  summary: string | null;
  ipAddress: string | null;
  timestamp: string;
  changes: string | null;
}

interface AuditResponse {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default function AuditPage() {
  const [response, setResponse] = useState<AuditResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState(false);
  const [error, setError] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [search, setSearch] = useState('');
  const [action, setAction] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 50;

  const params = useMemo(
    () => ({
      ...(fromDate ? { fromDate } : {}),
      ...(toDate ? { toDate } : {}),
      ...(search.trim() ? { search: search.trim() } : {}),
      ...(action ? { action } : {}),
      page,
      pageSize,
    }),
    [action, fromDate, page, search, toDate],
  );

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError('');
      const result = await api.get<AuditResponse>('/audit', { params });
      setResponse(result.data);
    } catch {
      setError('Unable to load the audit trail.');
    } finally {
      setLoading(false);
    }
  }, [params]);

  useEffect(() => {
    void load();
  }, [load]);

  const resetPage = () => setPage(1);

  const exportCsv = async () => {
    try {
      setDownloading(true);
      setError('');

      const result = await api.get('/audit/csv', {
        params: {
          ...(fromDate ? { fromDate } : {}),
          ...(toDate ? { toDate } : {}),
          ...(search.trim() ? { search: search.trim() } : {}),
          ...(action ? { action } : {}),
        },
        responseType: 'blob',
      });

      const url = URL.createObjectURL(new Blob([result.data]));
      const link = document.createElement('a');
      link.href = url;
      link.download = `AuditTrail_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch {
      setError('Unable to export the audit trail.');
    } finally {
      setDownloading(false);
    }
  };

  const logs = response?.items ?? [];

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8 print:bg-white print:p-0">
      <div className="mx-auto max-w-7xl">
        <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between print:mb-4">
          <div>
            <p className="text-sm font-medium text-blue-600 print:hidden">
              Security & accountability
            </p>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
              <History className="text-blue-600" />
              Audit Trail
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              Sales, stock changes, product archives, quotes, payments and configuration activity.
            </p>
          </div>

          <div className="flex flex-wrap gap-2 print:hidden">
            <button
              onClick={() => void exportCsv()}
              disabled={downloading}
              className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-50"
            >
              {downloading ? (
                <Loader2 className="animate-spin" size={16} />
              ) : (
                <Download size={16} />
              )}
              Export CSV
            </button>

            <button
              onClick={() => window.print()}
              className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-700"
            >
              <Printer size={16} />
              Print / Save PDF
            </button>
          </div>
        </div>

        <section className="mb-5 rounded-xl border border-gray-200 bg-white p-4 shadow-sm print:hidden">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
            <label>
              <span className="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-500">
                From
              </span>
              <input
                type="date"
                value={fromDate}
                max={toDate || undefined}
                onChange={(event) => {
                  setFromDate(event.target.value);
                  resetPage();
                }}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-500">
                To
              </span>
              <input
                type="date"
                value={toDate}
                min={fromDate || undefined}
                onChange={(event) => {
                  setToDate(event.target.value);
                  resetPage();
                }}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-500">
                Action
              </span>
              <select
                value={action}
                onChange={(event) => {
                  setAction(event.target.value);
                  resetPage();
                }}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
              >
                <option value="">All actions</option>
                <option value="Sold">Sold</option>
                <option value="StockAdjusted">Stock adjusted</option>
                <option value="Archived">Archived</option>
                <option value="QuoteCreated">Quote created</option>
                <option value="PaymentRecorded">Payment recorded</option>
                <option value="Modified">Modified</option>
                <option value="Added">Added</option>
                <option value="Deleted">Deleted</option>
              </select>
            </label>

            <label className="relative md:col-span-2">
              <span className="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-500">
                Search
              </span>
              <Search
                size={16}
                className="absolute bottom-3 left-3 text-gray-400"
              />
              <input
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  resetPage();
                }}
                placeholder="Product, user, action, quote, details..."
                className="w-full rounded-lg border border-gray-300 py-2.5 pl-9 pr-3 text-sm"
              />
            </label>
          </div>
        </section>

        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        <div className="mb-4 grid gap-4 sm:grid-cols-3 print:grid-cols-3">
          <Summary
            label="Matching activities"
            value={response?.totalCount ?? 0}
            icon={<Activity size={17} />}
          />
          <Summary
            label="Page"
            value={response ? `${response.page} / ${response.totalPages}` : '—'}
            icon={<ShieldCheck size={17} />}
          />
          <Summary
            label="Rows per page"
            value={pageSize}
            icon={<History size={17} />}
          />
        </div>

        <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm print:border-0 print:shadow-none">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1080px] text-left text-sm print:min-w-0 print:text-[10px]">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="p-3 font-semibold text-gray-600">Timestamp</th>
                  <th className="p-3 font-semibold text-gray-600">User</th>
                  <th className="p-3 font-semibold text-gray-600">IP</th>
                  <th className="p-3 font-semibold text-gray-600">Entity</th>
                  <th className="p-3 font-semibold text-gray-600">Action</th>
                  <th className="p-3 font-semibold text-gray-600">Activity</th>
                </tr>
              </thead>
              <tbody>
                {logs.map((log) => (
                  <tr key={log.id} className="border-b border-gray-100 align-top last:border-0">
                    <td className="p-3 text-gray-500">
                      {new Date(log.timestamp).toLocaleString()}
                    </td>
                    <td className="p-3">
                      <div className="flex items-center gap-2">
                        <div className="rounded-full bg-slate-100 p-1.5 print:hidden">
                          <User size={13} />
                        </div>
                        <span className="font-medium text-gray-800">
                          {log.userId}
                        </span>
                      </div>
                    </td>
                    <td className="p-3 text-gray-500">
                      {log.ipAddress ?? '—'}
                    </td>
                    <td className="p-3">
                      <div className="font-semibold text-gray-800">
                        {log.entityName}
                      </div>
                      {log.entityId && (
                        <div className="mt-1 max-w-40 truncate text-xs text-gray-400">
                          {log.entityId}
                        </div>
                      )}
                    </td>
                    <td className="p-3">
                      <ActionBadge action={log.action} />
                    </td>
                    <td className="p-3 text-gray-600">
                      <div>{log.summary ?? 'No summary available.'}</div>
                      {log.changes && (
                        <details className="mt-2 print:hidden">
                          <summary className="cursor-pointer text-xs font-semibold text-blue-600">
                            Technical details
                          </summary>
                          <pre className="mt-2 max-w-xl overflow-auto whitespace-pre-wrap rounded bg-gray-50 p-2 text-[11px] text-gray-500">
                            {log.changes}
                          </pre>
                        </details>
                      )}
                    </td>
                  </tr>
                ))}

                {!loading && logs.length === 0 && (
                  <tr>
                    <td colSpan={6} className="p-12 text-center text-gray-500">
                      No audit activity matches the selected filters.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            {loading && (
              <div className="flex items-center justify-center p-10 text-sm text-gray-500">
                <Loader2 className="mr-2 animate-spin" size={18} />
                Loading audit trail...
              </div>
            )}
          </div>
        </div>

        <div className="mt-4 flex items-center justify-between print:hidden">
          <p className="text-sm text-gray-500">
            {response?.totalCount ?? 0} matching record
            {(response?.totalCount ?? 0) === 1 ? '' : 's'}
          </p>

          <div className="flex gap-2">
            <button
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              disabled={!response || response.page <= 1}
              className="inline-flex items-center gap-1 rounded-lg border border-gray-300 px-3 py-2 text-sm font-semibold text-gray-700 disabled:opacity-40"
            >
              <ChevronLeft size={16} />
              Previous
            </button>
            <button
              onClick={() =>
                setPage((current) =>
                  response
                    ? Math.min(response.totalPages, current + 1)
                    : current,
                )
              }
              disabled={!response || response.page >= response.totalPages}
              className="inline-flex items-center gap-1 rounded-lg border border-gray-300 px-3 py-2 text-sm font-semibold text-gray-700 disabled:opacity-40"
            >
              Next
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function ActionBadge({ action }: { action: string }) {
  const style =
    action === 'Sold'
      ? 'bg-green-50 text-green-700'
      : action === 'Archived' || action === 'Deleted'
        ? 'bg-red-50 text-red-700'
        : action === 'StockAdjusted'
          ? 'bg-violet-50 text-violet-700'
          : action === 'PaymentRecorded'
            ? 'bg-blue-50 text-blue-700'
            : 'bg-amber-50 text-amber-700';

  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${style}`}>
      {action}
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
    <div className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm print:shadow-none">
      <div className="mb-2 flex items-center gap-2 text-blue-600">
        {icon}
        <span className="text-xs font-semibold uppercase tracking-wide">
          {label}
        </span>
      </div>
      <p className="text-xl font-bold text-gray-900">{value}</p>
    </div>
  );
}
