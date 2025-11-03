import { useMutationWithInvalidate } from "@/composables/query/helpers/useMutationWithInvalidate";
import { authApi } from "@/api/modules/auth";
import type { LoginRequestDto } from "@/types/backend/index";

/* 🔹 Query keys — for consistency */
export const authKeys = {
  base: ["auth"] as const,
  me: ["auth", "me"] as const, // if you later add /auth/me endpoint
};

/* ✅ LOGIN */
export function useLogin() {
  return useMutationWithInvalidate({
    mutationKey: ["auth", "login"],
    mutationFn: (payload: LoginRequestDto) => authApi.login(payload),
    successMessage: "Logged in successfully!",
    errorMessage: "Invalid email or password.",
  });
}

/* ✅ REFRESH TOKEN */
export function useRefreshToken() {
  return useMutationWithInvalidate({
    mutationKey: ["auth", "refresh"],
    mutationFn: () => authApi.refresh(),
    successMessage: "Session refreshed.",
    errorMessage: "Session expired, please log in again.",
  });
}

/* ✅ LOGOUT */
export function useLogout() {
  return useMutationWithInvalidate({
    mutationKey: ["auth", "logout"],
    mutationFn: () => authApi.logout(),
    successMessage: "Logged out successfully.",
    errorMessage: "Failed to log out.",
    invalidateKeys: [authKeys.me],
  });
}
