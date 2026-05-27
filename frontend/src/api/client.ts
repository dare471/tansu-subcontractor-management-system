import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

export const apiClient: AxiosInstance = axios.create({
  baseURL,
  timeout: 30_000
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const url = String(config.url ?? '');
  const isAnonymousAuth =
    url.includes('/api/auth/login') ||
    url.includes('/api/auth/dev-login');

  if (!isAnonymousAuth) {
    const token = localStorage.getItem('tansu.token');
    if (token) {
      config.headers = config.headers ?? {};
      config.headers['Authorization'] = `Bearer ${token}`;
    }
  }
  return config;
});

export type ApiError = {
  code: string;
  detail: string;
  status: number;
};

export function toApiError(err: unknown): ApiError {
  const ax = err as AxiosError<{ code?: string; detail?: string; title?: string; status?: number }>;
  const data = ax?.response?.data;
  return {
    code: data?.code || data?.title || 'unknown',
    detail: data?.detail || ax?.message || 'Неизвестная ошибка',
    status: ax?.response?.status ?? 0
  };
}

apiClient.interceptors.response.use(
  (r) => r,
  (error) => {
    const status = error?.response?.status;
    const requestUrl = String(error?.config?.url ?? '');
    const isAuthRequest =
      requestUrl.includes('/api/auth/login') ||
      requestUrl.includes('/api/auth/dev-login');

    if (status === 401 && !isAuthRequest) {
      localStorage.removeItem('tansu.token');
      if (!window.location.pathname.startsWith('/login')) {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);
