<script setup lang="ts">
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import Menu from "primevue/menu";
import Avatar from "primevue/avatar";
import ChangePasswordDialog from "@/components/User/ChangePasswordDialog.vue";

import { useAuthStore } from "@/stores/authStore";
import { useChangePassword } from "@/composables/query/users/useUserActions";

const toast = useToast();
const router = useRouter();
const authStore = useAuthStore();
const { mutateAsync: changePassword } = useChangePassword();

// ======================
// 🔹 STATE
// ======================
const profileMenu = ref<InstanceType<typeof Menu> | null>(null);
const changePasswordDialog = ref(false);

// ======================
// 🧠 COMPUTED
// ======================
const currentUser = computed(() => authStore.currentUser);
const userName = computed(
  () => currentUser.value?.name || currentUser.value?.email || "User"
);

const userRoleDisplay = computed(() => {
  const role = currentUser.value?.role || "User";
  return role.replace(/([a-z])([A-Z])/g, "$1 $2"); // e.g. "SuperAdmin" → "Super Admin"
});

// ======================
// 📋 MENU
// ======================
const profileMenuItems = [
  {
    label: "Change Password",
    icon: "pi pi-key",
    command: () => (changePasswordDialog.value = true),
  },
  { separator: true },
  {
    label: "Log Out",
    icon: "pi pi-power-off text-red-500",
    command: async () => {
      try {
        await authStore.logout();
        await router.push({ name: "login" });
        toast.add({
          severity: "info",
          summary: "Logged Out",
          detail: "You have been successfully logged out.",
          life: 3000,
        });
      } catch (err: any) {
        toast.add({
          severity: "error",
          summary: "Logout Failed",
          detail: err.message || "An unexpected error occurred.",
          life: 3000,
        });
      }
    },
  },
];

// ======================
// ⚙️ METHODS
// ======================
const toggleProfileMenu = (event: MouseEvent) =>
  profileMenu.value?.toggle(event);

async function handleChangePassword(passwords: {
  currentPassword?: string;
  newPassword: string;
  confirmPassword: string;
}) {
  if (
    !passwords.newPassword ||
    passwords.newPassword !== passwords.confirmPassword
  ) {
    toast.add({
      severity: "warn",
      summary: "Validation",
      detail: "Passwords do not match.",
      life: 3000,
    });
    return;
  }

  const userId = currentUser.value?.sub;
  if (!userId) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "User ID not found.",
      life: 3000,
    });
    return;
  }

  try {
    await changePassword({
      id: userId,
      data: {
        currentPassword: passwords.currentPassword || "",
        newPassword: passwords.newPassword,
      },
    });
    toast.add({
      severity: "success",
      summary: "Password Changed",
      detail: "Your password has been updated successfully.",
      life: 3000,
    });
    changePasswordDialog.value = false;
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed to change password.",
      life: 3000,
    });
  }
}
</script>

<template>
  <div
    class="user-profile-menu flex items-center cursor-pointer select-none"
    @click="toggleProfileMenu"
  >
    <Avatar
      icon="pi pi-user"
      shape="circle"
      size="large"
      class="mr-2 bg-surface-100 dark:bg-surface-800"
    />
    <div class="flex flex-col text-left leading-tight">
      <span
        class="text-surface-900 dark:text-surface-0 font-semibold text-[13px]"
      >
        {{ userName }}
      </span>
      <span class="text-xs text-surface-500 dark:text-surface-300">
        {{ userRoleDisplay }}
      </span>
    </div>
    <i class="pi pi-angle-down ml-2 text-gray-600 dark:text-gray-300"></i>

    <Menu ref="profileMenu" :model="profileMenuItems" :popup="true" />
  </div>

  <!-- 🔒 Change Password Dialog -->
  <ChangePasswordDialog
    v-model:visible="changePasswordDialog"
    :user="currentUser"
    @save="
      () => toast.add({ severity: 'success', summary: 'Password updated' })
    "
  />
</template>

<style scoped>
.user-profile-menu {
  transition: opacity 0.15s ease-in-out;
}
.user-profile-menu:hover {
  opacity: 0.9;
}
</style>
