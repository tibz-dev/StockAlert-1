'use client';

import { useEffect, useState } from 'react';
import {
  Building2,
  CalendarClock,
  CheckCircle2,
  CreditCard,
  Loader2,
  Mail,
  MapPin,
  LockKeyhole,
  MessageCircle,
  Save,
  Settings2,
  Smartphone,
  TriangleAlert,
} from 'lucide-react';
import api from '@/lib/api';
import type { BusinessProfile } from '@/types/business';

interface CommunicationStatus {
  emailConfigured: boolean;
  smsConfigured: boolean;
  whatsAppConfigured: boolean;
}

interface OwnerReportSettings {
  recipientEmail: string;
  dailyEnabled: boolean;
  weeklyEnabled: boolean;
  monthlyEnabled: boolean;
  yearlyEnabled: boolean;
  sendHourLocal: number;
  weeklyDay: number;
  monthlyDay: number;
  timeZoneId: string;
  lastDailySentAt: string | null;
  lastWeeklySentAt: string | null;
  lastMonthlySentAt: string | null;
  lastYearlySentAt: string | null;
}

export default function BusinessSettingsPage() {
  const [profile, setProfile] = useState<BusinessProfile | null>(null);
  const [communicationStatus, setCommunicationStatus] =
    useState<CommunicationStatus | null>(null);
  const [ownerReports, setOwnerReports] =
    useState<OwnerReportSettings | null>(null);
  const [savingOwnerReports, setSavingOwnerReports] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const [profileResponse, communicationResponse] = await Promise.all([
          api.get<BusinessProfile>('/business-profile'),
          api.get<CommunicationStatus>('/communications/status'),
        ]);

        setProfile(profileResponse.data);
        setCommunicationStatus(communicationResponse.data);

        try {
          const ownerReportResponse =
            await api.get<OwnerReportSettings>('/owner-report-settings');
          setOwnerReports(ownerReportResponse.data);
        } catch {
          // Non-owner users intentionally cannot read or modify owner reporting.
          setOwnerReports(null);
        }
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

  const updateOwnerReport = <K extends keyof OwnerReportSettings>(
    key: K,
    value: OwnerReportSettings[K],
  ) => {
    setOwnerReports((current) =>
      current ? { ...current, [key]: value } : current,
    );
  };

  const saveOwnerReports = async () => {
    if (!ownerReports) return;

    try {
      setSavingOwnerReports(true);
      setError('');
      setSuccess('');

      const response = await api.put<OwnerReportSettings>(
        '/owner-report-settings',
        {
          recipientEmail: ownerReports.recipientEmail,
          dailyEnabled: ownerReports.dailyEnabled,
          weeklyEnabled: ownerReports.weeklyEnabled,
          monthlyEnabled: ownerReports.monthlyEnabled,
          yearlyEnabled: ownerReports.yearlyEnabled,
          sendHourLocal: ownerReports.sendHourLocal,
          weeklyDay: ownerReports.weeklyDay,
          monthlyDay: ownerReports.monthlyDay,
          timeZoneId: ownerReports.timeZoneId,
        },
      );

      setOwnerReports(response.data);
      setSuccess('Owner report schedule saved.');
      window.setTimeout(() => setSuccess(''), 3000);
    } catch {
      setError('Unable to save owner report schedule.');
    } finally {
      setSavingOwnerReports(false);
    }
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
            title="Automated communications"
            description="Provider readiness for automatic customer delivery. Credentials stay on the server."
            icon={<MessageCircle size={19} />}
          >
            <div className="grid gap-4 md:grid-cols-3">
              <ProviderStatus
                label="Email"
                description="SMTP"
                icon={<Mail size={18} />}
                configured={communicationStatus?.emailConfigured ?? false}
              />
              <ProviderStatus
                label="SMS"
                description="Twilio Messaging"
                icon={<Smartphone size={18} />}
                configured={communicationStatus?.smsConfigured ?? false}
              />
              <ProviderStatus
                label="WhatsApp"
                description="Twilio WhatsApp"
                icon={<MessageCircle size={18} />}
                configured={communicationStatus?.whatsAppConfigured ?? false}
              />
            </div>

            <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm leading-6 text-amber-800">
              Provider credentials are configured through environment variables or server configuration,
              not stored in Business Settings. This keeps SMTP and Twilio secrets out of the browser.
            </div>
          </Section>

          {ownerReports && (
            <Section
              title="Owner operational reports"
              description="Protected server-side reporting for the business owner. Staff cannot modify this schedule."
              icon={<CalendarClock size={19} />}
            >
              <div className="mb-5 flex items-start gap-3 rounded-lg border border-blue-200 bg-blue-50 p-4">
                <LockKeyhole size={18} className="mt-0.5 shrink-0 text-blue-700" />
                <div className="text-sm leading-6 text-blue-800">
                  These settings are available only to the Owner role. Reports contain operational
                  metrics plus an audit-trail digest and are generated by the API even when no browser is open.
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <TextField
                  label="Owner report email"
                  type="email"
                  value={ownerReports.recipientEmail}
                  onChange={(value) =>
                    updateOwnerReport('recipientEmail', value)
                  }
                />

                <TextField
                  label="Time zone"
                  value={ownerReports.timeZoneId}
                  onChange={(value) =>
                    updateOwnerReport('timeZoneId', value)
                  }
                />

                <NumberField
                  label="Send hour (local 0-23)"
                  value={ownerReports.sendHourLocal}
                  min={0}
                  max={23}
                  step={1}
                  onChange={(value) =>
                    updateOwnerReport('sendHourLocal', value)
                  }
                />

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-gray-700">
                    Weekly report day
                  </span>
                  <select
                    value={ownerReports.weeklyDay}
                    onChange={(event) =>
                      updateOwnerReport(
                        'weeklyDay',
                        Number(event.target.value),
                      )
                    }
                    className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900"
                  >
                    {[
                      'Sunday',
                      'Monday',
                      'Tuesday',
                      'Wednesday',
                      'Thursday',
                      'Friday',
                      'Saturday',
                    ].map((day, index) => (
                      <option key={day} value={index}>
                        {day}
                      </option>
                    ))}
                  </select>
                </label>

                <NumberField
                  label="Monthly report day"
                  value={ownerReports.monthlyDay}
                  min={1}
                  max={28}
                  step={1}
                  onChange={(value) =>
                    updateOwnerReport('monthlyDay', value)
                  }
                />

                <div className="md:col-span-2 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                  <ReportToggle
                    label="Daily"
                    checked={ownerReports.dailyEnabled}
                    onChange={(value) =>
                      updateOwnerReport('dailyEnabled', value)
                    }
                  />
                  <ReportToggle
                    label="Weekly"
                    checked={ownerReports.weeklyEnabled}
                    onChange={(value) =>
                      updateOwnerReport('weeklyEnabled', value)
                    }
                  />
                  <ReportToggle
                    label="Monthly"
                    checked={ownerReports.monthlyEnabled}
                    onChange={(value) =>
                      updateOwnerReport('monthlyEnabled', value)
                    }
                  />
                  <ReportToggle
                    label="Yearly"
                    checked={ownerReports.yearlyEnabled}
                    onChange={(value) =>
                      updateOwnerReport('yearlyEnabled', value)
                    }
                  />
                </div>
              </div>

              <div className="mt-5 flex items-center justify-between gap-4 border-t border-gray-100 pt-5">
                <div className="text-xs leading-5 text-gray-500">
                  Daily reports cover yesterday. Weekly covers the previous 7 days.
                  Monthly and yearly reports cover the previous completed calendar period.
                </div>
                <button
                  onClick={() => void saveOwnerReports()}
                  disabled={savingOwnerReports}
                  className="inline-flex shrink-0 items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white disabled:bg-blue-400"
                >
                  {savingOwnerReports ? (
                    <Loader2 className="animate-spin" size={16} />
                  ) : (
                    <Save size={16} />
                  )}
                  Save Owner Reports
                </button>
              </div>
            </Section>
          )}

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

function ReportToggle({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <label className="flex cursor-pointer items-center justify-between rounded-lg border border-gray-200 p-4">
      <span className="text-sm font-semibold text-gray-700">{label}</span>
      <input
        type="checkbox"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
        className="h-4 w-4"
      />
    </label>
  );
}

function ProviderStatus({
  label,
  description,
  icon,
  configured,
}: {
  label: string;
  description: string;
  icon: React.ReactNode;
  configured: boolean;
}) {
  return (
    <div className="rounded-xl border border-gray-200 p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="rounded-lg bg-gray-50 p-2 text-gray-600">{icon}</div>
        <span
          className={
            configured
              ? 'inline-flex items-center gap-1 rounded-full bg-green-50 px-2.5 py-1 text-xs font-bold text-green-700'
              : 'inline-flex items-center gap-1 rounded-full bg-amber-50 px-2.5 py-1 text-xs font-bold text-amber-700'
          }
        >
          {configured ? <CheckCircle2 size={12} /> : <TriangleAlert size={12} />}
          {configured ? 'Configured' : 'Setup required'}
        </span>
      </div>
      <p className="mt-3 font-semibold text-gray-900">{label}</p>
      <p className="mt-1 text-xs text-gray-500">{description}</p>
    </div>
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
