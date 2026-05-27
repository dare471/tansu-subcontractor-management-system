import axios from 'axios';

// Пустой baseURL = same-origin через HTTPS proxy Vite (камера с других устройств).
const baseURL = import.meta.env.VITE_VERIFY_API_BASE_URL || '';

const client = axios.create({ baseURL, timeout: 30_000 });

export type PassLookup = {
  employeeId: string;
  fullName: string;
  position: string;
  subcontractorName: string;
  projectName: string | null;
  hasReferencePhoto: boolean;
  issuedAt: string;
  isActive: boolean;
};

export type VerifyFaceResult = {
  matched: boolean;
  confidence: number;
  message: string;
  employee: PassLookup;
  siteVisitRecorded?: boolean;
  siteVisit?: {
    id: string;
    employeeId: string;
    employeeFullName: string;
    projectName: string | null;
    checkedInAt: string;
    faceConfidence: number | null;
    verificationMethod: string;
  };
};

function apiErrorDetail(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { detail?: string } | undefined;
    if (data?.detail) return data.detail;
    if (err.code === 'ERR_NETWORK') {
      return `Нет связи с Verify API (${baseURL}). Проверьте, что сервис запущен.`;
    }
    return err.message;
  }
  return 'Неизвестная ошибка';
}

export const verifyApi = {
  scan: async (token: string): Promise<PassLookup> => {
    try {
      const res = await client.get<PassLookup>(`/api/scan/${encodeURIComponent(token)}`);
      return res.data;
    } catch (e) {
      throw new Error(apiErrorDetail(e));
    }
  },

  verifyFace: async (token: string, livePhoto: Blob): Promise<VerifyFaceResult> => {
    const fd = new FormData();
    fd.append('token', token);
    fd.append('livePhoto', livePhoto, 'live.jpg');
    try {
      const res = await client.post<VerifyFaceResult>('/api/verify/face', fd);
      return res.data;
    } catch (e) {
      throw new Error(apiErrorDetail(e));
    }
  }
};
