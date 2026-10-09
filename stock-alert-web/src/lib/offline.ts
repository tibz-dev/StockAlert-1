export type OfflineSaleStatus = 'pending' | 'conflict';

export interface OfflineSaleQueueItem {
  operationId: string;
  request: Record<string, unknown>;
  productName: string;
  estimatedTotal: number;
  queuedAt: string;
  status: OfflineSaleStatus;
  error: string | null;
}

const DB_NAME = 'stockalert-offline';
const DB_VERSION = 1;
const CACHE_STORE = 'cache';
const SALES_STORE = 'salesQueue';
const DEVICE_KEY = 'stockalert-device-id';

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = window.indexedDB.open(DB_NAME, DB_VERSION);

    request.onupgradeneeded = () => {
      const db = request.result;

      if (!db.objectStoreNames.contains(CACHE_STORE)) {
        db.createObjectStore(CACHE_STORE, { keyPath: 'key' });
      }

      if (!db.objectStoreNames.contains(SALES_STORE)) {
        db.createObjectStore(SALES_STORE, {
          keyPath: 'operationId',
        });
      }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

export function getOrCreateDeviceId() {
  const existing = window.localStorage.getItem(DEVICE_KEY);

  if (existing) {
    return existing;
  }

  const id =
    typeof crypto.randomUUID === 'function'
      ? crypto.randomUUID()
      : createUuid();

  window.localStorage.setItem(DEVICE_KEY, id);
  return id;
}

export async function setOfflineCache<T>(
  key: string,
  value: T,
): Promise<void> {
  const db = await openDb();

  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(CACHE_STORE, 'readwrite');
    tx.objectStore(CACHE_STORE).put({
      key,
      value,
      cachedAt: new Date().toISOString(),
    });
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });

  db.close();
}

export async function getOfflineCache<T>(
  key: string,
): Promise<{ value: T; cachedAt: string } | null> {
  const db = await openDb();

  const result = await new Promise<
    { key: string; value: T; cachedAt: string } | undefined
  >((resolve, reject) => {
    const tx = db.transaction(CACHE_STORE, 'readonly');
    const request = tx.objectStore(CACHE_STORE).get(key);
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });

  db.close();

  return result
    ? { value: result.value, cachedAt: result.cachedAt }
    : null;
}

export async function enqueueOfflineSale(
  item: OfflineSaleQueueItem,
): Promise<void> {
  const db = await openDb();

  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(SALES_STORE, 'readwrite');
    tx.objectStore(SALES_STORE).put(item);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });

  db.close();
  notifyQueueChanged();
}

export async function getOfflineSales(): Promise<
  OfflineSaleQueueItem[]
> {
  const db = await openDb();

  const result = await new Promise<OfflineSaleQueueItem[]>(
    (resolve, reject) => {
      const tx = db.transaction(SALES_STORE, 'readonly');
      const request = tx.objectStore(SALES_STORE).getAll();
      request.onsuccess = () =>
        resolve(
          (request.result as OfflineSaleQueueItem[]).sort(
            (a, b) =>
              new Date(a.queuedAt).getTime() -
              new Date(b.queuedAt).getTime(),
          ),
        );
      request.onerror = () => reject(request.error);
    },
  );

  db.close();
  return result;
}

export async function removeOfflineSale(
  operationId: string,
): Promise<void> {
  const db = await openDb();

  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(SALES_STORE, 'readwrite');
    tx.objectStore(SALES_STORE).delete(operationId);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });

  db.close();
  notifyQueueChanged();
}

export async function markOfflineSaleConflict(
  operationId: string,
  error: string,
): Promise<void> {
  const db = await openDb();

  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(SALES_STORE, 'readwrite');
    const store = tx.objectStore(SALES_STORE);
    const request = store.get(operationId);

    request.onsuccess = () => {
      const item = request.result as OfflineSaleQueueItem | undefined;

      if (!item) return;

      store.put({
        ...item,
        status: 'conflict',
        error,
      });
    };

    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });

  db.close();
  notifyQueueChanged();
}

export async function retryOfflineSale(
  operationId: string,
): Promise<void> {
  const db = await openDb();

  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(SALES_STORE, 'readwrite');
    const store = tx.objectStore(SALES_STORE);
    const request = store.get(operationId);

    request.onsuccess = () => {
      const item = request.result as OfflineSaleQueueItem | undefined;

      if (!item) return;

      store.put({
        ...item,
        status: 'pending',
        error: null,
      });
    };

    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });

  db.close();
  notifyQueueChanged();
}

export function notifyQueueChanged() {
  window.dispatchEvent(
    new CustomEvent('stockalert:offline-queue-changed'),
  );
}

function createUuid() {
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;

  const hex = Array.from(bytes, (byte) =>
    byte.toString(16).padStart(2, '0'),
  ).join('');

  return [
    hex.slice(0, 8),
    hex.slice(8, 12),
    hex.slice(12, 16),
    hex.slice(16, 20),
    hex.slice(20),
  ].join('-');
}
