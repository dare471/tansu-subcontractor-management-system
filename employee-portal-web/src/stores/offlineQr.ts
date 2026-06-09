import { defineStore } from 'pinia';

const DB_NAME = 'tansu-portal';
const STORE = 'qr-cache';

type QrCache = {
  verifyUrl: string;
  qrValidUntil: string;
  passStatus: string;
  employeeBlockStatus: string;
  cachedAt: string;
};

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, 1);
    req.onupgradeneeded = () => req.result.createObjectStore(STORE);
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

export const useOfflineQrStore = defineStore('offlineQr', {
  state: () => ({ dataUrl: null as string | null, fromCache: false }),
  actions: {
    async saveFromDashboard(pass: {
      verifyUrl: string;
      qrValidUntil: string;
      passStatus: string;
      employeeBlockStatus: string;
    }) {
      const db = await openDb();
      const tx = db.transaction(STORE, 'readwrite');
      const payload: QrCache = {
        verifyUrl: pass.verifyUrl,
        qrValidUntil: pass.qrValidUntil,
        passStatus: pass.passStatus,
        employeeBlockStatus: pass.employeeBlockStatus,
        cachedAt: new Date().toISOString()
      };
      tx.objectStore(STORE).put(JSON.stringify(payload), 'current');
      await new Promise<void>((res, rej) => {
        tx.oncomplete = () => res();
        tx.onerror = () => rej(tx.error);
      });
      db.close();
    },
    async loadValid(maxOfflineHours = 12): Promise<string | null> {
      try {
        const db = await openDb();
        const raw = await new Promise<string | undefined>((res, rej) => {
          const tx = db.transaction(STORE, 'readonly');
          const get = tx.objectStore(STORE).get('current');
          get.onsuccess = () => res(get.result as string | undefined);
          get.onerror = () => rej(get.error);
        });
        db.close();
        if (!raw) return null;
        const cache = JSON.parse(raw) as QrCache;
        const cachedAt = new Date(cache.cachedAt).getTime();
        const validUntil = new Date(cache.qrValidUntil).getTime();
        const now = Date.now();
        if (cache.passStatus !== 'active' || cache.employeeBlockStatus === 'blocked') return null;
        if (now > validUntil) return null;
        if (now - cachedAt > maxOfflineHours * 3600_000) return null;
        const QRCode = await import('qrcode');
        const url = await QRCode.toDataURL(cache.verifyUrl, { width: 280, margin: 1 });
        this.dataUrl = url;
        this.fromCache = true;
        return url;
      } catch {
        return null;
      }
    },
    async generateOnline(verifyUrl: string) {
      const QRCode = await import('qrcode');
      const url = await QRCode.toDataURL(verifyUrl, { width: 280, margin: 1 });
      this.dataUrl = url;
      this.fromCache = false;
      return url;
    }
  }
});
