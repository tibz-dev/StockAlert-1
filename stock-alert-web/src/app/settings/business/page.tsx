'use client';

import { useEffect, useState } from 'react';
import {
  Building2,
  CreditCard,
  Loader2,
  MapPin,
  Save,
  Settings2,
} from 'lucide-react';
import api from '@/lib/api';
import type { BusinessProfile } from '@/types/business';

export default function BusinessSettingsPage() {
  const [profile, setProfile] = useState<BusinessProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const response = await api.get<BusinessProfile>('/business-profile');
        setProfile(response.data);
      } catch {
        setError('Unable to load business settings.');
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  const update = <K extends keyof BusinessProfile>(
    key: K,
    value: BusinessProfile[K],
  ) => {
    setProfile((current) =>
      current ? { ...current, [key]: value } : current,
    );
  };

  const save = async () => {
    if (!profile) return;

    try {
      setSaving(true);
      setError('');
      setSuccess('');

      const response = await api.put<BusinessProfile>(
        '/business-profile',
        profile,
      );

      setProfile(response.data);
      setSuccess('Business information saved.');
      window.setTimeout(() => setSuccess(''), 3000);
    } catch {
      setError('Unable to save business settings.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading business settings...
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="p-8 text-sm text-red-600">
        {error || 'Business settings are unavailable.'}
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-6xl">
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium text-blue-600">Configuration</p>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
              <Settings2 className="text-blue-600" />
              Business Settings
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              These details appear on receipts, quotes and customer communications.
            </p>
          </div>

          <button
            onClick={() => void save()}
            disabled={saving}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:bg-blue-400"
          >
            {saving ? <Loader2 className="animate-spin" size={17} /> : <Save size={17} />}
            {saving ? 'Saving...' : 'Save Settings'}
          </button>
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

        <div className="space-y-6">
          <Section
            title="Business identity"
            description="Legal and trading information shown on documents."
            icon={<Building2 size={19} />}
          >
            <div className="grid gap-4 md:grid-cols-2">
              <TextField
                label="Business name"
                value={profile.businessName}
                required
                onChange={(value) => update('businessName', value)}
              />
              <TextField
                label="Trading name"
                value={profile.tradingName ?? ''}
                onChange={(value) => update('tradingName', value || null)}
              />
              <TextField
                label="Registration number"
                value={profile.registrationNumber ?? ''}
                onChange={(value) => update('registrationNumber', value || null)}
              />
              <TextField
                label="VAT number"
                value={profile.vatNumber ?? ''}
                onChange={(value) => update('vatNumber', value || null)}
              />
              <label className="flex items-center gap-3 rounded-lg border border-gray-200 p-4">
                <input
                  type="checkbox"
                  checked={profile.isVatRegistered}
                  onChange={(event) =>
                    update('isVatRegistered', event.target.checked)
                  }
                  className="h-4 w-4"
                />
                <span className="text-sm font-medium text-gray-700">
                  VAT registered
                </span>
              </label>
              <NumberField
                label="Default VAT rate (%)"
                value={profile.defaultVatRate}
                min={0}
                max={100}
                step={0.01}
                onChange={(value) => update('defaultVatRate', value)}
              />
              <TextField
                label="Branch name"
                value={profile.branchName ?? ''}
                onChange={(value) => update('branchName', value || null)}
              />
              <TextField
                label="Branch number"
                value={profile.branchNumber ?? ''}
                onChange={(value) => update('branchNumber', value || null)}
              />
              <div className="md:col-span-2">
                <TextField
                  label="Logo URL"
                  value={profile.logoUrl ?? ''}
                  onChange={(value) => update('logoUrl', value || null)}
                />
              </div>
            </div>
          </Section>

          <Section
            title="Contact & address"
            description="Used on receipts, quotes and customer messages."
            icon={<MapPin size={19} />}
          >
            <div className="grid gap-4 md:grid-cols-2">
              <TextField
                label="Email"
                type="email"
                value={profile.email ?? ''}
                onChange={(value) => update('email', value || null)}
              />
              <TextField
                label="Phone"
                value={profile.phoneNumber ?? ''}
                onChange={(value) => update('phoneNumber', value || null)}
              />
              <TextField
                label="WhatsApp number"
                value={profile.whatsAppNumber ?? ''}
                onChange={(value) => update('whatsAppNumber', value || null)}
              />
              <TextField
                label="Website"
                value={profile.website ?? ''}
                onChange={(value) => update('website', value || null)}
              />
              <TextField
                label="Address line 1"
                value={profile.addressLine1 ?? ''}
                onChange={(value) => update('addressLine1', value || null)}
              />
              <TextField
                label="Address line 2"
                value={profile.addressLine2 ?? ''}
                onChange={(value) => update('addressLine2', value || null)}
              />
              <TextField
                label="City"
                value={profile.city ?? ''}
                onChange={(value) => update('city', value || null)}
              />
              <TextField
                label="Province"
                value={profile.province ?? ''}
                onChange={(value) => update('province', value || null)}
              />
              <TextField
                label="Postal code"
                value={profile.postalCode ?? ''}
                onChange={(value) => update('postalCode', value || null)}
              />
              <TextField
                label="Country"
                value={profile.country}
                onChange={(value) => update('country', value)}
              />
            </div>
          </Section>

          <Section
            title="Banking details"
            description="Included on quotes so customers know where to pay."
            icon={<CreditCard size={19} />}
          >
            <div className="grid gap-4 md:grid-cols-2">
              <TextField
                label="Bank"
                value={profile.bankName ?? ''}
                onChange={(value) => update('bankName', value || null)}
              />
              <TextField
                label="Account name"
                value={profile.bankAccountName ?? ''}
                onChange={(value) => update('bankAccountName', value || null)}
              />
              <TextField
                label="Account number"
                value={profile.bankAccountNumber ?? ''}
                onChange={(value) => update('bankAccountNumber', value || null)}
              />
              <TextField
                label="Branch code"
                value={profile.bankBranchCode ?? ''}
                onChange={(value) => update('bankBranchCode', value || null)}
              />
              <TextField
                label="Account type"
                value={profile.bankAccountType ?? ''}
                onChange={(value) => update('bankAccountType', value || null)}
              />
            </div>
          </Section>

          <Section
            title="Document defaults"
            description="Defaults used when creating quotes and receipts."
            icon={<Settings2 size={19} />}
          >
            <div className="grid gap-4 md:grid-cols-2">
              <TextField
                label="Currency code"
                value={profile.currencyCode}
                onChange={(value) => update('currencyCode', value.toUpperCase())}
              />
              <NumberField
                label="Quote validity (days)"
                value={profile.quoteValidityDays}
                min={1}
                max={365}
                step={1}
                onChange={(value) => update('quoteValidityDays', value)}
              />
              <div className="md:col-span-2">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-gray-700">
                    Receipt footer
                  </span>
                  <textarea
                    rows={3}
                    value={profile.receiptFooter ?? ''}
                    onChange={(event) =>
                      update('receiptFooter', event.target.value || null)
                    }
                    className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                    placeholder="Thank you for your business."
                  />
                </label>
              </div>
            </div>
          </Section>
        </div>
      </div>
    </div>
  );
}

function Section({
  title,
  description,
  icon,
  children,
}: {
  title: string;
  description: string;
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm">
      <div className="mb-5 flex items-start gap-3">
        <div className="rounded-lg bg-blue-50 p-2 text-blue-600">{icon}</div>
        <div>
          <h2 className="font-semibold text-gray-900">{title}</h2>
          <p className="mt-1 text-sm text-gray-500">{description}</p>
        </div>
      </div>
      {children}
    </section>
  );
}

function TextField({
  label,
  value,
  onChange,
  type = 'text',
  required = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-gray-700">
        {label}
      </span>
      <input
        type={type}
        required={required}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}

function NumberField({
  label,
  value,
  min,
  max,
  step,
  onChange,
}: {
  label: string;
  value: number;
  min: number;
  max: number;
  step: number;
  onChange: (value: number) => void;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-gray-700">
        {label}
      </span>
      <input
        type="number"
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
        className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}
