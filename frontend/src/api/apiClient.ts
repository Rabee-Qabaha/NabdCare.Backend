import axios, {
  type AxiosError,
  type AxiosInstance,
  type InternalAxiosRequestConfig,
} from "axios";
import { AuthService } from "@/service/AuthService";
import { tokenManager } from "@/utils/tokenManager";
import { useAuthStore } from "@/stores/authStore";
import { globalErrorHandler } from "@/utils/globalErrorHandler";
import { showToast } from "@/service/toastService"; // ✅ optional global toast helper

let isRefreshing = false;
let failedQueue: {
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}[] = [];

/**
 * Resolve or reject all queued requests waiting for a new token.
 */
function processQueue(error: unknown, token: string | null) {
  failedQueue.forEach((p) => {
    if (error) p.reject(error);
    else if (token) p.resolve(token);
  });
  failedQueue = [];
}

/**
 * Redirect user to login page, preserving their current route.
 */
function redirectToLogin() {
  if (!window.location.pathname.includes("/auth/login")) {
    // Preserve full path + query for accurate restore
    const redirect = encodeURIComponent(
      window.location.pathname + window.location.search
    );

    // Avoid double redirects
    const currentUrl = new URL(window.location.href);
    if (!currentUrl.searchParams.has("redirect")) {
      window.location.href = `/auth/login?redirect=${redirect}`;
    }
  }
}

/**
 * Axios client instance
 */
const api: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5175/api",
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
});

/**
 * ✅ Request Interceptor
 * Attaches the access token to all outgoing requests.
 */
api.interceptors.request.use(
  (config) => {
    const token = AuthService.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

/**
 * ✅ Response Interceptor
 * Handles token refresh and global errors.
 */
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<any>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    // 🔒 Handle 401 Unauthorized (expired or invalid access token)
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // If refresh is in progress, queue this request
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
        // 🔁 Try to refresh the token
        const newToken = await tokenManager.refreshAccessToken();
        if (!newToken) throw new Error("Token refresh failed");

        // Store new token and process queued requests
        AuthService.storeAccessToken(newToken);
        processQueue(null, newToken);

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
        }

        return api(originalRequest);
      } catch (err) {
        // ❌ Refresh failed — clear tokens and log out
        processQueue(err, null);
        AuthService.clearTokens();

        const authStore = useAuthStore();
        await authStore.logout();

        // 🧠 Optional: Show toast to the user
        showToast?.({
          severity: "warn",
          summary: "Session Expired",
          detail: "Your session has expired. Please log in again.",
          life: 4000,
        });

        redirectToLogin();
        return Promise.reject(err);
      } finally {
        isRefreshing = false;
      }
    }

    // 🧠 Handle all other errors globally
    await globalErrorHandler(error);
    return Promise.reject(error);
  }
);

export { api };
