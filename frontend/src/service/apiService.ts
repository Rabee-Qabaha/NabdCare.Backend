import axios, {
  type AxiosInstance,
  type AxiosError,
  type InternalAxiosRequestConfig,
} from "axios";
import { AuthService } from "@/service/AuthService";
import { tokenManager } from "@/utils/tokenManager";

interface QueuedRequest {
  resolve: (value: string) => void;
  reject: (error: unknown) => void;
}

class ApiService {
  private api: AxiosInstance;
  private baseURL: string;
  private isRefreshing = false;
  private failedQueue: QueuedRequest[] = [];

  constructor() {
    this.baseURL =
      import.meta.env.VITE_API_BASE_URL || "http://localhost:5175/api";

    this.api = axios.create({
      baseURL: this.baseURL,
      headers: { "Content-Type": "application/json" },
      timeout: 30000,
      withCredentials: true, // ✅ CRITICAL: Enable cookie support for HttpOnly refresh token
    });

    this.setupInterceptors();
  }

  // ----------------------
  // Interceptors
  // ----------------------
  private setupInterceptors(): void {
    // ✅ Request Interceptor: Attach access token
    this.api.interceptors.request.use(
      (config) => {
        const token = AuthService.getAccessToken();
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // ✅ Response Interceptor: Handle 401 + refresh token
    this.api.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & {
          _retry?: boolean;
        };

        // Handle 401 errors (token expired)
        if (error.response?.status === 401 && !originalRequest._retry) {
          // Queue requests while refreshing
          if (this.isRefreshing) {
            return new Promise((resolve, reject) => {
              this.failedQueue.push({ resolve, reject });
            })
              .then((token) => {
                if (originalRequest.headers) {
                  originalRequest.headers.Authorization = `Bearer ${token}`;
                }
                return this.api(originalRequest);
              })
              .catch((err) => Promise.reject(err));
          }

          originalRequest._retry = true;
          this.isRefreshing = true;

          try {
            // ✅ Refresh using HttpOnly cookie
            const newAccessToken = await tokenManager.refreshAccessToken();

            if (!newAccessToken) {
              throw new Error("Token refresh failed");
            }

            // Process queued requests with new token
            this.processQueue(null, newAccessToken);

            // Retry original request with new token
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
            }
            return this.api(originalRequest);
          } catch (err) {
            // Refresh failed - clear tokens and redirect to login
            this.processQueue(err, null);
            AuthService.clearTokens();
            this.redirectToLogin();
            return Promise.reject(err);
          } finally {
            this.isRefreshing = false;
          }
        }

        return Promise.reject(error);
      }
    );
  }

  private processQueue(error: unknown, token: string | null): void {
    this.failedQueue.forEach((p) => {
      if (error) p.reject(error);
      else if (token) p.resolve(token);
    });
    this.failedQueue = [];
  }

  private redirectToLogin(): void {
    if (!window.location.pathname.includes("/auth/login")) {
      const redirect = encodeURIComponent(window.location.pathname);
      window.location.href = `/auth/login?redirect=${redirect}`;
    }
  }

  // ----------------------
  // CRUD METHODS
  // ----------------------
  async get<T>(url: string, config?: Record<string, unknown>): Promise<T> {
    const response = await this.api.get<T>(url, config);
    return response.data;
  }

  async post<T>(
    url: string,
    data?: unknown,
    config?: Record<string, unknown>
  ): Promise<T> {
    const response = await this.api.post<T>(url, data, config);
    return response.data;
  }

  async put<T>(
    url: string,
    data?: unknown,
    config?: Record<string, unknown>
  ): Promise<T> {
    const response = await this.api.put<T>(url, data, config);
    return response.data;
  }

  async patch<T>(
    url: string,
    data?: unknown,
    config?: Record<string, unknown>
  ): Promise<T> {
    const response = await this.api.patch<T>(url, data, config);
    return response.data;
  }

  async delete<T>(url: string, config?: Record<string, unknown>): Promise<T> {
    const response = await this.api.delete<T>(url, config);
    return response.data;
  }

  getBaseURL(): string {
    return this.baseURL;
  }
}

export const apiService = new ApiService();
