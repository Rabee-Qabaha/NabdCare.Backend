// src/composables/query/users/useUserActions.ts
import { useMutation, useQueryClient } from "@tanstack/vue-query";
import { usersApi } from "@/api/modules/users";
import { useToastService } from "@/composables/useToastService";
import { useErrorHandler } from "@/composables/errorHandling/useErrorHandler";
import type {
  ChangePasswordRequestDto,
  ResetPasswordRequestDto,
  CreateUserRequestDto,
  UpdateUserRequestDto,
} from "@/types/backend";

// -------------------------------------------------------------
// 🔧 Shared helpers for all mutations
// -------------------------------------------------------------

type InvalidateEntry<TPayload> =
  | readonly unknown[]
  | ((variables: TPayload, data: any) => readonly unknown[]);

function createMutationOptions<TPayload>(
  mutationKey: readonly unknown[],
  mutationFn: (payload: TPayload) => Promise<any>,
  successMessage: string,
  invalidate: InvalidateEntry<TPayload>[] = [["users"]],
) {
  const queryClient = useQueryClient();
  const toast = useToastService();
  const { handleErrorAndNotify } = useErrorHandler();

  return {
    mutationKey,
    mutationFn,
    onSuccess: async (data: any, variables: TPayload) => {
      toast.success(successMessage);

      for (const entry of invalidate) {
        const key =
          typeof entry === "function" ? entry(variables, data) : entry;
        await queryClient.invalidateQueries({ queryKey: key });
      }
    },
    onError: (error: any) => handleErrorAndNotify(error),
  };
}

// -------------------------------------------------------------------
// 🆕 Create User
// -------------------------------------------------------------------
export function useCreateUser() {
  return useMutation(
    createMutationOptions<CreateUserRequestDto>(
      ["user", "create"],
      (dto) => usersApi.create(dto),
      "User created successfully",
      [
        ["users"],
        ["users", "infinite"],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// 📝 Update User
// -------------------------------------------------------------------
export function useUpdateUser() {
  return useMutation(
    createMutationOptions<{ id: string; data: UpdateUserRequestDto }>(
      ["user", "update"],
      ({ id, data }) => usersApi.update(id, data),
      "User updated successfully",
      [
        ["users"],
        ["users", "infinite"],
        // invalidate specific user as well
        (vars) => ["user", vars.id],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// 🔒 Change Password (self-service)
// -------------------------------------------------------------------
export function useChangePassword() {
  return useMutation(
    createMutationOptions<{ id: string; data: ChangePasswordRequestDto }>(
      ["user", "change-password"],
      ({ id, data }) => usersApi.changePassword(id, data),
      "Password updated successfully",
      [
        ["user"],
        ["users"],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// 🧩 Reset Password (admin)
// -------------------------------------------------------------------
export function useResetPassword() {
  return useMutation(
    createMutationOptions<{ id: string; data: ResetPasswordRequestDto }>(
      ["user", "reset-password"],
      ({ id, data }) => usersApi.resetPassword(id, data),
      "Password reset successfully",
      [["users"]],
    ),
  );
}

// -------------------------------------------------------------------
// ✅ Activate User
// -------------------------------------------------------------------
export function useActivateUser() {
  return useMutation(
    createMutationOptions<string>(
      ["user", "activate"],
      (id) => usersApi.activate(id),
      "User activated successfully",
      [
        ["users"],
        ["users", "infinite"],
        (id) => ["user", id],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// ⛔ Deactivate User
// -------------------------------------------------------------------
export function useDeactivateUser() {
  return useMutation(
    createMutationOptions<string>(
      ["user", "deactivate"],
      (id) => usersApi.deactivate(id),
      "User deactivated successfully",
      [
        ["users"],
        ["users", "infinite"],
        (id) => ["user", id],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// 🗑 Soft Delete User
// -------------------------------------------------------------------
export function useSoftDeleteUser() {
  return useMutation(
    createMutationOptions<string>(
      ["user", "soft-delete"],
      (id) => usersApi.delete(id),
      "User deleted successfully",
      [
        ["users"],
        ["users", "infinite"],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// 💀 Hard Delete User (SuperAdmin)
// -------------------------------------------------------------------
export function useHardDeleteUser() {
  return useMutation(
    createMutationOptions<string>(
      ["user", "hard-delete"],
      (id) => usersApi.hardDelete(id),
      "User permanently deleted",
      [
        ["users"],
        ["users", "infinite"],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// 👤 Update User Role
// -------------------------------------------------------------------
export function useUpdateUserRole() {
  return useMutation(
    createMutationOptions<{ id: string; roleId: string }>(
      ["user", "update-role"],
      ({ id, roleId }) => usersApi.updateRole(id, roleId),
      "Role updated successfully",
      [
        ["users"],
        ["users", "infinite"],
        (vars) => ["user", vars.id],
      ],
    ),
  );
}

// -------------------------------------------------------------------
// ♻ Restore Soft-Deleted User
// -------------------------------------------------------------------
export function useRestoreUser() {
  return useMutation(
    createMutationOptions<string>(
      ["user", "restore"],
      (id) => usersApi.restore(id),
      "User restored successfully",
      [
        ["users"],
        ["users", "infinite"],
        (id) => ["user", id],
      ],
    ),
  );
}