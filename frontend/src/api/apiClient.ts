// src/api/apiClient.ts

import { AuthService } from '@/service/AuthService';
import { showToast } from '@/service/toastService';
import { useAuthStore } from '@/stores/authStore';
import { tokenManager } from '@/utils/tokenManager';
import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import { setupErrorInterceptor } from '@/api/errorInterceptor'; // ✅ ADD THIS

let isRefreshing = false;
let failedQueue: {
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}[] = [];

function processQueue(error: unknown, token: string | null) {
  failedQueue.forEach((p) => {
    if (error) p.reject(error);
    else if (token) p.resolve(token);
  });
  failedQueue = [];
}

function redirectToLogin() {
  if (!window.location.pathname.includes('/auth/login')) {
    const redirect = encodeURIComponent(window.location.pathname + window.location.search);
    const currentUrl = new URL(window.location.href);
    if (!currentUrl.searchParams.has('redirect')) {
      window.location.href = `/auth/login?redirect=${redirect}`;
    }
  }
}

const api: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5175/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

// ✅ Request Interceptor
api.interceptors.request.use(
  (config) => {
    const token = AuthService.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// ✅ Response Interceptor (token refresh)
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<any>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const newToken = await tokenManager.refreshAccessToken();
        if (!newToken) throw new Error('Token refresh failed');

        AuthService.storeAccessToken(newToken);
        processQueue(null, newToken);

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
        }

        return api(originalRequest);
      } catch (err) {
        processQueue(err, null);
        AuthService.clearTokens();

        const authStore = useAuthStore();
        await authStore.logout();

        showToast?.({
          severity: 'warn',
          summary: 'Session Expired',
          detail: 'Your session has expired. Please log in again.',
          life: 4000,
        });

        redirectToLogin();
        return Promise.reject(err);
      } finally {
        isRefreshing = false;
      }
    }

    // ✅ REMOVED: Old global error handler
    // Now using setupErrorInterceptor instead
    return Promise.reject(error);
  },
);

// ✅ ADD ERROR INTERCEPTOR
setupErrorInterceptor(api);

export { api };