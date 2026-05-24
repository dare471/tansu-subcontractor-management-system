import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

export const apiClient: AxiosInstance = axios.create({
  baseURL,
  timeout: 30_000
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('tansu.token');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers['Authorization'] = `Bearer ${token}`;
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
    if (status === 401) {
      localStorage.removeItem('tansu.token');
      const path = window.location.pathname;
      if (!path.startsWith('/login')) {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);
