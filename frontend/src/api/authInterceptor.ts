// src/api/authInterceptor.ts
import type { AxiosInstance, AxiosRequestConfig } from "axios";
import { AuthService } from "@/service/AuthService";

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null = null) {
  failedQueue.forEach((prom) => {
    if (error) prom.reject(error);
    else if (token) prom.resolve(token);
  });
  failedQueue = [];
}

export function setupAuthInterceptors(api: AxiosInstance) {
  // ✅ Attach token to each request
  api.interceptors.request.use((config: AxiosRequestConfig) => {
    const token = AuthService.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  // ✅ Handle 401 (unauthorized)
  api.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config;

      if (error.response?.status === 401 && !originalRequest._retry) {
        if (isRefreshing) {
          return new Promise((resolve, reject) => {
            failedQueue.push({
              resolve: (token: string) => {
                originalRequest.headers.Authorization = `Bearer ${token}`;
                resolve(api(originalRequest));
              },
              reject,
            });
          });
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const newTokens = await AuthService.refreshToken();
          AuthService.saveTokens(newTokens.accessToken, newTokens.refreshToken);
          processQueue(null, newTokens.accessToken);
          isRefreshing = false;

          originalRequest.headers.Authorization = `Bearer ${newTokens.accessToken}`;
          return api(originalRequest);
        } catch (refreshError) {
          processQueue(refreshError, null);
          isRefreshing = false;
          AuthService.logout();
          window.location.href = "/auth/login";
          return Promise.reject(refreshError);
        }
      }

      return Promise.reject(error);
    }
  );
}
