import { defineStore } from "pinia";
import { ref } from "vue";
import { UserService } from "@/service/UserService";
import type {
  UserResponseDto,
  CreateUserRequestDto,
  UpdateUserRequestDto,
  ChangePasswordRequestDto,
  ResetPasswordRequestDto,
} from "@/types/backend";

export const useUserStore = defineStore("user", () => {
  const users = ref<UserResponseDto[]>([]);
  const currentUser = ref<UserResponseDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  // ============ FETCH USERS ============

  async function fetchUsers() {
    loading.value = true;
    error.value = null;
    try {
      users.value = await UserService.getAllUsers();
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function fetchClinicUsers(clinicId: string) {
    loading.value = true;
    error.value = null;
    try {
      users.value = await UserService.getUsersByClinic(clinicId);
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function fetchCurrentUser() {
    loading.value = true;
    error.value = null;
    try {
      currentUser.value = await UserService.getCurrentUser();
      return currentUser.value;
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  // ============ CREATE/UPDATE/DELETE ============

  async function createUser(data: CreateUserRequestDto) {
    loading.value = true;
    error.value = null;
    try {
      const newUser = await UserService.createUser(data);
      users.value.push(newUser);
      return newUser;
    } catch (err: any) {
      error.value = err.message;
      console.error("Error creating user:", err);
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function updateUser(id: string, data: UpdateUserRequestDto) {
    loading.value = true;
    error.value = null;
    try {
      const updated = await UserService.updateUser(id, data);
      users.value = users.value.map((u) => (u.id === id ? updated : u)); // <-- Fix: replace array reference
      return updated;
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function deleteUser(id: string) {
    loading.value = true;
    error.value = null;
    try {
      await UserService.deleteUser(id);
      users.value = users.value.filter((u) => u.id !== id);
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function hardDeleteUser(id: string) {
    loading.value = true;
    error.value = null;
    try {
      await UserService.hardDeleteUser(id);
      users.value = users.value.filter((u) => u.id !== id); // <-- REMOVE FROM USERS LIST
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  // ============ ACTIVATE/DEACTIVATE ============

  async function activateUser(id: string) {
    loading.value = true;
    error.value = null;
    try {
      await UserService.activateUser(id); // Activate (may return partial)
      const fullUser = await UserService.getUserById(id); // Fetch full user data
      users.value = users.value.map((u) => (u.id === id ? fullUser : u));
      return fullUser;
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function deactivateUser(id: string) {
    loading.value = true;
    error.value = null;
    try {
      await UserService.deactivateUser(id); // Deactivate (may return partial)
      const fullUser = await UserService.getUserById(id); // Fetch full user data
      users.value = users.value.map((u) => (u.id === id ? fullUser : u));
      return fullUser;
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  // ============ PASSWORD MANAGEMENT ============

  async function changePassword(id: string, data: ChangePasswordRequestDto) {
    loading.value = true;
    error.value = null;
    try {
      await UserService.changePassword(id, data);
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function resetPassword(id: string, newPassword: string) {
    loading.value = true;
    error.value = null;
    try {
      const data: ResetPasswordRequestDto = { newPassword };
      await UserService.resetPassword(id, data);
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function adminResetPassword(id: string, newPassword: string) {
    loading.value = true;
    error.value = null;
    try {
      const data: ResetPasswordRequestDto = { newPassword };
      await UserService.adminResetPassword(id, data);
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  return {
    users,
    currentUser,
    loading,
    error,
    fetchUsers,
    fetchClinicUsers,
    fetchCurrentUser,
    createUser,
    updateUser,
    deleteUser,
    hardDeleteUser,
    activateUser,
    deactivateUser,
    changePassword,
    resetPassword,
    adminResetPassword,
  };
});
