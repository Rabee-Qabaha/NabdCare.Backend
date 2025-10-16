// src/api/apiClientInstance.ts
import { axiosInstance } from "./axiosInstance";
import { setupAuthInterceptors } from "./authInterceptor";
import { API_BASE_URL } from "./apiBaseUrl";
import { createClient } from "@/types/client";

// Setup interceptors once
setupAuthInterceptors(axiosInstance);

// Create single instance of SDK client
export const apiClient = createClient({
  baseURL: API_BASE_URL,
  axios: axiosInstance,
});
