import axios, {
  type AxiosInstance,
  AxiosError,
  type InternalAxiosRequestConfig,
  type AxiosResponse,
} from "axios";
import { tokenManager } from "@/utils/tokenManager";
import type { ErrorResponseDto } from "@/types/backend/error-response-dto";
import { handleError } from "@/utils/errorHandler";

let isRefreshing = false;
let queue: Array<{ resolve: (t: string) => void; reject: (e: unknown) => void }> = [];

function processQueue(error: unknown, token: string | null) {
  queue.forEach((p) => (error ? p.reject(error) : p.resolve(token!)));
  queue = [];
}

export const api: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5175/api",
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
  timeout: 20000,
});

// 🔹 request interceptor
api.interceptors.request.use(
  (config) => {
    const token = tokenManager.getAccessToken();
    if (token && config.headers)
      config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (err) => Promise.reject(err),
);

// 🔹 response interceptor (refresh + unified error)
api.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError<ErrorResponseDto>) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    const status = error.response?.status;

    if (status === 401 && !original?._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => queue.push({ resolve, reject }))
          .then((token) => {
            if (original.headers) original.headers.Authorization = `Bearer ${token}`;
            return api(original);
          })
          .catch((err) => Promise.reject(err));
      }

      original._retry = true;
      isRefreshing = true;

      try {
        const newToken = await tokenManager.refreshAccessToken();
        if (!newToken) throw new Error("Token refresh failed");

        processQueue(null, newToken);
        if (original.headers)
          original.headers.Authorization = `Bearer ${newToken}`;

        return api(original);
      } catch (err) {
        processQueue(err, null);
        tokenManager.clearTokens();
        return Promise.reject(err);
      } finally {
        isRefreshing = false;
      }
    }

    // normalize + reject typed error
    const uiError = handleError(error);
    return Promise.reject(uiError);
  },
);