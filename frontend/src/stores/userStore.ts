// src/stores/userStore.ts
import { defineStore } from "pinia";
import { ref } from "vue";
import { UserService } from "@/service/UserService";
import type {
  User,
  CreateUserRequestDto,
  UpdateUserRequestDto,
} from "@/types/backend";

export const useUserStore = defineStore("user", () => {
  const users = ref<User[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

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
      users.value = await UserService.getClinicUsers(clinicId);
    } catch (err: any) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function createUser(data: CreateUserRequestDto) {
    const newUser = await UserService.createUser(data);
    users.value.push(newUser);
    return newUser;
  }

  async function updateUser(id: string, data: UpdateUserRequestDto) {
    const updated = await UserService.updateUser(id, data);
    const idx = users.value.findIndex((u) => u.id === id);
    if (idx !== -1) users.value[idx] = updated;
    return updated;
  }

  async function deleteUser(id: string) {
    await UserService.deleteUser(id);
    users.value = users.value.filter((u) => u.id !== id);
  }

  async function changePassword(id: string, newPassword: string) {
    await UserService.changePassword(id, newPassword);
  }

  return {
    users,
    loading,
    error,
    fetchUsers,
    fetchClinicUsers,
    createUser,
    updateUser,
    deleteUser,
    changePassword,
  };
});
