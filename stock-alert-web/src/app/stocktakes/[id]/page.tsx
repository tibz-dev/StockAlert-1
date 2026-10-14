'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'next/navigation';
import {
  AlertTriangle,
  ArrowLeftRight,
  Barcode,
  CheckCircle2,
  ClipboardCheck,
  EyeOff,
  Loader2,
  Search,
  ShieldCheck,
} from 'lucide-react';
import api from '@/lib/api';
import { getSessionInfo } from '@/lib/auth';
import type {
  StocktakeItem,
  StocktakeSession,
} from '@/types/stocktake';

export default function StocktakeWorkspacePage() {
  const params = useParams<{ id: string }>();
  const id = Array.isArray(params.id) ? params.id[0] : params.id;
  const auth = getSessionInfo();
  const canManage =
    auth.roles.includes('Owner') ||
    auth.roles.includes('Manager');

  const barcodeInputRef = useRef<HTMLInputElement>(null);
  const [session, setSession] = useState<StocktakeSession | null>(null);
  const [selected, setSelected] = useState<StocktakeItem | null>(null);
  const [barcode, setBarcode] = useState('');
  const [quantity, setQuantity] = useState('');
  const [resolutionQuantity, setResolutionQuantity] = useState('');
  const [resolutionReason, setResolutionReason] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const load = useCallback(async () => {
    if (!id) return;

    try {
      setLoading(true);
      setError('');
      const response = await api.get<StocktakeSession>(
        `/stocktakes/${id}`,
      );
      setSession(response.data);

      setSelected((current) => {
        if (!current) return current;

        return (
          response.data.items.find(
            (item) => item.id === current.id,
          ) ?? null
        );
      });
    } catch {
      setError('Unable to load this stocktake.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!loading) {
      barcodeInputRef.current?.focus();
    }
  }, [loading]);

  const filteredItems = useMemo(() => {
    if (!session) return [];

    const query = search.trim().toLowerCase();

    return session.items.filter((item) => {
      const matchesSearch =
        !query ||
        item.productName.toLowerCase().includes(query) ||
        item.barcode?.toLowerCase().includes(query) ||
        item.externalProductId?.toLowerCase().includes(query);

      const matchesStatus =
        !statusFilter || item.status === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [search, session, statusFilter]);

  const selectItem = (item: StocktakeItem) => {
    setSelected(item);
    setQuantity('');
    setResolutionQuantity(
      item.finalCount?.toString() ??
        item.secondCount?.toString() ??
        item.firstCount?.toString() ??
        '',
    );
    setResolutionReason(item.resolutionReason ?? '');
    setError('');
    setSuccess('');
  };

  const scan = async () => {
    if (!id || !barcode.trim()) return;

    try {
      setBusy(true);
      setError('');
      setSuccess('');

      const response = await api.get<StocktakeItem>(
        `/stocktakes/${id}/barcode/${encodeURIComponent(
          barcode.trim(),
        )}`,
      );

      selectItem(response.data);
      setBarcode('');
    } catch (requestError: unknown) {
      setError(
        getMessage(
          requestError,
          'This barcode is not part of the stocktake snapshot.',
        ),
      );
    } finally {
      setBusy(false);
      window.setTimeout(
        () => barcodeInputRef.current?.focus(),
        0,
      );
    }
  };

  const submitCount = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault();

    if (!id || !selected) return;

    const parsed = Number(quantity);

    if (!Number.isInteger(parsed) || parsed < 0) {
      setError('Count must be a whole number of zero or more.');
      return;
    }

    const endpoint =
      selected.status === 'RecountRequired'
        ? 'recount'
        : 'count';

    try {
      setBusy(true);
      setError('');
      setSuccess('');

      const response = await api.post<StocktakeItem>(
        `/stocktakes/${id}/items/${selected.id}/${endpoint}`,
        { quantity: parsed },
      );

      setSelected(response.data);
      setQuantity('');

      if (response.data.status === 'Matched') {
        setSuccess(
          `${response.data.productName} matched the expected stock.`,
        );
      } else if (
        response.data.status === 'RecountRequired'
      ) {
        setSuccess(
          'Variance detected. A different staff member must recount this product.',
        );
      } else if (response.data.status === 'NeedsReview') {
        setSuccess(
          'Recount captured. Manager/Owner review is now required.',
        );
      }

      await load();
    } catch (requestError: unknown) {
      setError(
        getMessage(requestError, 'Unable to submit this count.'),
      );
    } finally {
      setBusy(false);
    }
  };

  const resolve = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault();

    if (!id || !selected) return;

    const finalQuantity = Number(resolutionQuantity);

    if (
      !Number.isInteger(finalQuantity) ||
      finalQuantity < 0
    ) {
      setError(
        'Approved physical quantity must be a whole number of zero or more.',
      );
      return;
    }

    if (!resolutionReason.trim()) {
      setError('A reconciliation reason is required.');
      return;
    }

    try {
      setBusy(true);
      setError('');
      setSuccess('');

      const response = await api.post<StocktakeItem>(
        `/stocktakes/${id}/items/${selected.id}/resolve`,
        {
          finalQuantity,
          reason: resolutionReason.trim(),
        },
      );

      setSelected(response.data);
      setSuccess('Variance approved and retained in the audit trail.');
      await load();
    } catch (requestError: unknown) {
      setError(
        getMessage(
          requestError,
          'Unable to resolve this variance.',
        ),
      );
    } finally {
      setBusy(false);
    }
  };

  const complete = async () => {
    if (!id || !session) return;

    const confirmed = window.confirm(
      'Complete this stocktake? This records the reconciliation as complete. It does not automatically overwrite inventory quantities.',
    );

    if (!confirmed) return;

    try {
      setBusy(true);
      setError('');
      await api.post(`/stocktakes/${id}/complete`);
      setSuccess('Stocktake completed successfully.');
      await load();
    } catch (requestError: unknown) {
      setError(
        getMessage(
          requestError,
          'Unable to complete this stocktake.',
        ),
      );
    } finally {
      setBusy(false);
    }
  };

  if (loading && !session) {
    return (
      <div className="flex min-h-[65vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading stocktake...
      </div>
    );
  }

  if (!session) {
    return (
      <div className="p-8 text-sm text-red-700">
        {error || 'Stocktake not found.'}
      </div>
    );
  }

  const progress =
    session.totalItems > 0
      ? Math.round(
          (session.countedItems / session.totalItems) * 100,
        )
      : 0;

  const unresolved =
    session.totalItems -
    session.matchedItems -
    session.items.filter(
      (item) => item.status === 'Approved',
    ).length;

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-6 flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2 text-sm font-semibold text-blue-600">
              <ClipboardCheck size={16} />
              {session.referenceNumber}
              <span className="text-gray-300">•</span>
              {session.source}
            </div>
            <h1 className="mt-2 text-2xl font-bold text-gray-900">
              {session.title}
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              {session.location ?? 'No location'}
              {session.zone ? ` · ${session.zone}` : ''}
              {' · '}
              Snapshot {new Date(
                session.sourceSnapshotAt,
              ).toLocaleString()}
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {session.blindCount && (
              <span className="inline-flex items-center gap-1 rounded-full bg-violet-50 px-3 py-1.5 text-xs font-bold text-violet-700">
                <EyeOff size={13} />
                Blind count
              </span>
            )}

            <StatusBadge status={session.status} />

            {canManage && session.status !== 'Completed' && (
              <button
                onClick={() => void complete()}
                disabled={busy}
                className="inline-flex items-center gap-2 rounded-lg bg-green-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
              >
                <CheckCircle2 size={16} />
                Complete Stocktake
              </button>
            )}
          </div>
        </div>

        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {success && (
          <div className="mb-5 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
            {success}
          </div>
        )}

        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          <Summary label="Progress" value={`${progress}%`} />
          <Summary
            label="Counted"
            value={`${session.countedItems}/${session.totalItems}`}
          />
          <Summary
            label="Recount"
            value={session.recountRequiredItems}
          />
          <Summary
            label="Manager review"
            value={session.reviewRequiredItems}
          />
          <Summary
            label="Variance exposure"
            value={formatMoney(session.varianceValue)}
          />
        </div>

        <div className="mb-6 grid gap-6 xl:grid-cols-[1.25fr_0.75fr]">
          <section className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
            <div className="mb-4">
              <p className="text-sm font-semibold text-blue-600">
                Counter mode
              </p>
              <h2 className="mt-1 flex items-center gap-2 text-xl font-bold text-gray-900">
                <Barcode size={20} />
                Scan product
              </h2>
            </div>

            <div className="flex gap-2">
              <div className="relative flex-1">
                <Barcode
                  size={18}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
                />
                <input
                  ref={barcodeInputRef}
                  value={barcode}
                  onChange={(event) =>
                    setBarcode(event.target.value)
                  }
                  onKeyDown={(event) => {
                    if (event.key === 'Enter') {
                      event.preventDefault();
                      void scan();
                    }
                  }}
                  placeholder="Scan barcode and press Enter"
                  className="w-full rounded-lg border border-gray-300 py-3 pl-10 pr-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                  disabled={session.status === 'Completed'}
                />
              </div>
              <button
                onClick={() => void scan()}
                disabled={
                  busy ||
                  !barcode.trim() ||
                  session.status === 'Completed'
                }
                className="rounded-lg bg-blue-600 px-5 py-3 text-sm font-semibold text-white disabled:bg-blue-400"
              >
                Scan
              </button>
            </div>

            <p className="mt-2 text-xs leading-5 text-gray-500">
              USB scanners and phone scanners that behave like a keyboard
              work here. Products without barcodes can be selected from the
              table below.
            </p>

            {selected && (
              <div className="mt-6 rounded-xl border border-gray-200 p-5">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
                      Selected product
                    </p>
                    <h3 className="mt-1 text-lg font-bold text-gray-900">
                      {selected.productName}
                    </h3>
                    <p className="mt-1 text-xs text-gray-500">
                      Barcode: {selected.barcode ?? 'No barcode'}
                    </p>
                  </div>
                  <ItemStatus status={selected.status} />
                </div>

                <div className="mt-5 grid gap-3 sm:grid-cols-3">
                  <Metric
                    label="Expected"
                    value={
                      selected.expectedQuantity === null
                        ? 'Hidden'
                        : selected.expectedQuantity
                    }
                    hidden={selected.expectedQuantity === null}
                  />
                  <Metric
                    label="First count"
                    value={selected.firstCount ?? '—'}
                  />
                  <Metric
                    label="Second count"
                    value={selected.secondCount ?? '—'}
                  />
                </div>

                {(selected.status === 'Pending' ||
                  selected.status === 'RecountRequired') &&
                  session.status !== 'Completed' && (
                    <form
                      onSubmit={submitCount}
                      className="mt-5 rounded-lg bg-gray-50 p-4"
                    >
                      <label>
                        <span className="mb-2 block text-sm font-semibold text-gray-700">
                          {selected.status === 'RecountRequired'
                            ? 'Independent recount quantity'
                            : 'Physical count'}
                        </span>
                        <input
                          autoFocus
                          required
                          type="number"
                          min="0"
                          step="1"
                          value={quantity}
                          onChange={(event) =>
                            setQuantity(event.target.value)
                          }
                          className="w-full rounded-lg border border-gray-300 px-3 py-3 text-lg font-bold"
                          placeholder="0"
                        />
                      </label>

                      {selected.status ===
                        'RecountRequired' && (
                        <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs leading-5 text-amber-800">
                          The recount must be submitted by a different
                          signed-in staff member from the first counter.
                        </div>
                      )}

                      <button
                        type="submit"
                        disabled={busy}
                        className="mt-4 inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white disabled:bg-blue-400"
                      >
                        {busy ? (
                          <Loader2
                            className="animate-spin"
                            size={16}
                          />
                        ) : (
                          <ClipboardCheck size={16} />
                        )}
                        {selected.status ===
                        'RecountRequired'
                          ? 'Submit Recount'
                          : 'Submit Count'}
                      </button>
                    </form>
                  )}

                {selected.status === 'NeedsReview' &&
                  canManage &&
                  session.status !== 'Completed' && (
                    <form
                      onSubmit={resolve}
                      className="mt-5 rounded-lg border border-amber-200 bg-amber-50 p-4"
                    >
                      <div className="mb-4 flex items-start gap-2 text-amber-800">
                        <ShieldCheck
                          size={18}
                          className="mt-0.5 shrink-0"
                        />
                        <p className="text-sm leading-6">
                          Manager/Owner must approve the final physical
                          quantity and explain the discrepancy. This does not
                          automatically change the stock system.
                        </p>
                      </div>

                      <div className="grid gap-4 sm:grid-cols-2">
                        <label>
                          <span className="mb-2 block text-sm font-semibold text-gray-700">
                            Approved physical quantity
                          </span>
                          <input
                            required
                            type="number"
                            min="0"
                            step="1"
                            value={resolutionQuantity}
                            onChange={(event) =>
                              setResolutionQuantity(
                                event.target.value,
                              )
                            }
                            className="w-full rounded-lg border border-gray-300 px-3 py-2.5"
                          />
                        </label>

                        <label>
                          <span className="mb-2 block text-sm font-semibold text-gray-700">
                            Reason
                          </span>
                          <input
                            required
                            value={resolutionReason}
                            onChange={(event) =>
                              setResolutionReason(
                                event.target.value,
                              )
                            }
                            placeholder="Missing, damaged, receiving error..."
                            className="w-full rounded-lg border border-gray-300 px-3 py-2.5"
                          />
                        </label>
                      </div>

                      <button
                        type="submit"
                        disabled={busy}
                        className="mt-4 rounded-lg bg-amber-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
                      >
                        Approve Reconciliation
                      </button>
                    </form>
                  )}

                {selected.varianceQuantity !== null && (
                  <div className="mt-5 grid gap-3 sm:grid-cols-2">
                    <Metric
                      label="Variance units"
                      value={
                        selected.varianceQuantity > 0
                          ? `+${selected.varianceQuantity}`
                          : selected.varianceQuantity
                      }
                    />
                    <Metric
                      label="Variance value"
                      value={formatMoney(
                        selected.varianceValue ?? 0,
                      )}
                    />
                  </div>
                )}
              </div>
            )}
          </section>

          <aside className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
            <h2 className="flex items-center gap-2 font-bold text-gray-900">
              <ShieldCheck size={18} className="text-blue-600" />
              Control rules
            </h2>

            <div className="mt-5 space-y-4 text-sm leading-6 text-gray-600">
              <Rule
                number="1"
                text="Counter scans/selects the correct product and enters the physical quantity."
              />
              <Rule
                number="2"
                text="Expected stock is hidden from ordinary counters until the first count is submitted."
              />
              <Rule
                number="3"
                text="Any mismatch automatically requires an independent recount."
              />
              <Rule
                number="4"
                text="The first counter cannot submit the recount using the same account."
              />
              <Rule
                number="5"
                text="Confirmed/disputed variances require Owner or Manager review before completion."
              />
              <Rule
                number="6"
                text="Completion records the evidence; it never silently overwrites stock."
              />
            </div>

            {unresolved > 0 && (
              <div className="mt-6 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                <strong>{unresolved}</strong> item(s) still require action
                before this session can be completed.
              </div>
            )}
          </aside>
        </div>

        <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="border-b border-gray-200 p-4">
            <div className="flex flex-col gap-3 lg:flex-row">
              <label className="relative flex-1">
                <Search
                  size={17}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
                />
                <input
                  value={search}
                  onChange={(event) =>
                    setSearch(event.target.value)
                  }
                  placeholder="Search product, barcode or external ID"
                  className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-3 text-sm"
                />
              </label>

              <select
                value={statusFilter}
                onChange={(event) =>
                  setStatusFilter(event.target.value)
                }
                className="rounded-lg border border-gray-300 px-3 py-2.5 text-sm"
              >
                <option value="">All statuses</option>
                <option value="Pending">Pending</option>
                <option value="Matched">Matched</option>
                <option value="RecountRequired">
                  Recount required
                </option>
                <option value="NeedsReview">
                  Needs review
                </option>
                <option value="Approved">Approved</option>
              </select>
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full min-w-[1120px] text-left">
              <thead className="bg-gray-50">
                <tr>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Product
                  </th>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Barcode
                  </th>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Expected
                  </th>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Count 1
                  </th>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Count 2
                  </th>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Variance
                  </th>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Status
                  </th>
                  <th className="p-3 text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Action
                  </th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr
                    key={item.id}
                    className="border-t border-gray-100 hover:bg-gray-50"
                  >
                    <td className="p-3">
                      <div className="font-semibold text-gray-900">
                        {item.productName}
                      </div>
                      {item.externalProductId && (
                        <div className="mt-1 text-xs text-gray-400">
                          External: {item.externalProductId}
                        </div>
                      )}
                    </td>
                    <td className="p-3 text-sm text-gray-500">
                      {item.barcode ?? '—'}
                    </td>
                    <td className="p-3 text-sm font-semibold text-gray-700">
                      {item.expectedQuantity === null ? (
                        <span className="inline-flex items-center gap-1 text-violet-600">
                          <EyeOff size={13} />
                          Hidden
                        </span>
                      ) : (
                        item.expectedQuantity
                      )}
                    </td>
                    <td className="p-3 text-sm text-gray-700">
                      {item.firstCount ?? '—'}
                    </td>
                    <td className="p-3 text-sm text-gray-700">
                      {item.secondCount ?? '—'}
                    </td>
                    <td className="p-3 text-sm font-bold">
                      {item.varianceQuantity === null ? (
                        '—'
                      ) : item.varianceQuantity === 0 ? (
                        <span className="text-green-700">0</span>
                      ) : (
                        <span className="text-red-700">
                          {item.varianceQuantity > 0 ? '+' : ''}
                          {item.varianceQuantity}
                        </span>
                      )}
                    </td>
                    <td className="p-3">
                      <ItemStatus status={item.status} />
                    </td>
                    <td className="p-3">
                      <button
                        onClick={() => selectItem(item)}
                        className="rounded-md border border-gray-300 px-3 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-50"
                      >
                        {item.status === 'NeedsReview' &&
                        canManage
                          ? 'Review'
                          : item.status === 'RecountRequired'
                            ? 'Recount'
                            : item.status === 'Pending'
                              ? 'Count'
                              : 'View'}
                      </button>
                    </td>
                  </tr>
                ))}

                {filteredItems.length === 0 && (
                  <tr>
                    <td
                      colSpan={8}
                      className="p-10 text-center text-sm text-gray-500"
                    >
                      No stocktake items match the current filters.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>

        <div className="mt-5 rounded-lg border border-slate-200 bg-slate-50 p-4 text-xs leading-5 text-slate-600">
          <strong>Reconciliation safeguard:</strong> this module records
          expected stock, physical counts, recounts and approvals separately.
          It does not automatically update SmartTrade or StockAlert inventory
          quantities. A supported vendor adjustment/import workflow can be
          connected later.
        </div>
      </div>
    </div>
  );
}

function Metric({
  label,
  value,
  hidden = false,
}: {
  label: string;
  value: string | number;
  hidden?: boolean;
}) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-3">
      <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
        {label}
      </p>
      <p
        className={
          hidden
            ? 'mt-1 flex items-center gap-1 text-lg font-bold text-violet-600'
            : 'mt-1 text-lg font-bold text-gray-900'
        }
      >
        {hidden && <EyeOff size={15} />}
        {value}
      </p>
    </div>
  );
}

function Rule({
  number,
  text,
}: {
  number: string;
  text: string;
}) {
  return (
    <div className="flex gap-3">
      <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-blue-50 text-xs font-bold text-blue-700">
        {number}
      </div>
      <p>{text}</p>
    </div>
  );
}

function ItemStatus({ status }: { status: string }) {
  const style =
    status === 'Matched'
      ? 'bg-green-50 text-green-700'
      : status === 'Approved'
        ? 'bg-blue-50 text-blue-700'
        : status === 'RecountRequired'
          ? 'bg-orange-50 text-orange-700'
          : status === 'NeedsReview'
            ? 'bg-red-50 text-red-700'
            : 'bg-gray-100 text-gray-600';

  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${style}`}>
      {status.replace(/([a-z])([A-Z])/g, '$1 $2')}
    </span>
  );
}

function StatusBadge({ status }: { status: string }) {
  const style =
    status === 'Completed'
      ? 'bg-green-50 text-green-700'
      : status === 'Review'
        ? 'bg-amber-50 text-amber-700'
        : 'bg-blue-50 text-blue-700';

  return (
    <span className={`rounded-full px-3 py-1.5 text-xs font-bold ${style}`}>
      {status}
    </span>
  );
}

function Summary({
  label,
  value,
}: {
  label: string;
  value: string | number;
}) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
        {label}
      </p>
      <p className="mt-1 text-xl font-bold text-gray-900">
        {value}
      </p>
    </div>
  );
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-ZA', {
    style: 'currency',
    currency: 'ZAR',
  }).format(value);
}

function getMessage(error: unknown, fallback: string) {
  if (
    typeof error === 'object' &&
    error !== null &&
    'response' in error
  ) {
    return (
      (
        error as {
          response?: { data?: { message?: string } };
        }
      ).response?.data?.message ?? fallback
    );
  }

  return fallback;
}
