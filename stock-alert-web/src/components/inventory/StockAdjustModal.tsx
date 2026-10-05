'use client';

import { FormEvent, useState } from 'react';
import { Boxes, Loader2, X } from 'lucide-react';
import api from '@/lib/api';
import type { Product } from '@/types/inventory';

interface StockAdjustModalProps {
  product: Product;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
}

export default function StockAdjustModal({
  product,
  onClose,
  onSaved,
}: StockAdjustModalProps) {
  const [quantityChange, setQuantityChange] = useState('');
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const parsedChange = Number(quantityChange);
  const projectedStock =
    Number.isInteger(parsedChange) && quantityChange !== ''
      ? product.stockQuantity + parsedChange
      : product.stockQuantity;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');

    if (!Number.isInteger(parsedChange) || parsedChange === 0) {
      setError('Enter a non-zero whole-number adjustment.');
      return;
    }

    if (projectedStock < 0) {
      setError('This adjustment would reduce stock below zero.');
      return;
    }

    if (!reason.trim()) {
      setError('An adjustment reason is required.');
      return;
    }

    try {
      setSaving(true);

      await api.post(`/products/${product.id}/stock-adjustments`, {
        quantityChange: parsedChange,
        reason: reason.trim(),
      });

      await onSaved();
      onClose();
    } catch {
      setError('Unable to adjust stock.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">Inventory movement</p>
            <h2 className="mt-1 flex items-center gap-2 text-xl font-bold text-gray-900">
              <Boxes size={21} />
              Adjust Stock
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

          <div className="rounded-lg bg-gray-50 p-4">
            <p className="font-semibold text-gray-900">{product.name}</p>
            <div className="mt-2 flex justify-between text-sm text-gray-600">
              <span>Current stock</span>
              <strong>{product.stockQuantity}</strong>
            </div>
            <div className="mt-1 flex justify-between text-sm text-gray-600">
              <span>Projected stock</span>
              <strong className={projectedStock < 0 ? 'text-red-600' : 'text-gray-900'}>
                {projectedStock}
              </strong>
            </div>
          </div>

          <label className="block">
            <span className="mb-2 block text-sm font-medium text-gray-700">
              Quantity change
            </span>
            <input
              required
              type="number"
              step="1"
              value={quantityChange}
              onChange={(event) => setQuantityChange(event.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              placeholder="Use 10 to add, -3 to remove"
            />
            <span className="mt-1 block text-xs text-gray-500">
              Positive numbers add stock. Negative numbers remove stock.
            </span>
          </label>

          <label className="block">
            <span className="mb-2 block text-sm font-medium text-gray-700">
              Reason
            </span>
            <textarea
              required
              rows={3}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              className="w-full resize-none rounded-lg border border-gray-300 px-3 py-2.5 text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              placeholder="e.g. New supplier delivery, damaged stock, physical count correction"
            />
          </label>

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
              className="flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:bg-blue-400"
            >
              {saving && <Loader2 className="animate-spin" size={17} />}
              {saving ? 'Saving...' : 'Apply Adjustment'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
