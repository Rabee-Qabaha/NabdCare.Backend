<script setup lang="ts">
import { ref, computed } from "vue";
import Menu from "primevue/menu";
import Avatar from "primevue/avatar";
import { useToast } from "primevue/usetoast";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import { useUserStore } from "@/stores/userStore";
import ChangePasswordDialog from "@/components/User/ChangePasswordDialog.vue";

const toast = useToast();
const router = useRouter();
const authStore = useAuthStore();
const userStore = useUserStore();

const profileMenu = ref<InstanceType<typeof Menu> | null>(null);
const changePasswordDialog = ref(false);
const currentUser = computed(() => authStore.currentUser);

// Normalize role display
const userRoleDisplay = computed(() => currentUser.value?.role);

const userName = computed(() => currentUser.value?.fullName);

const profileMenuItems = [
  {
    label: "Change Password",
    icon: "pi pi-key",
    command: () => {
      changePasswordDialog.value = true;
    },
  },
  {
    label: "Log Out",
    icon: "pi pi-power-off",
    command: async () => {
      try {
        await authStore.logout();
        router.push("/auth/login");
        toast.add({
          severity: "info",
          summary: "Logged out",
          detail: "You have been successfully logged out",
          life: 3000,
        });
      } catch (err: any) {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: err.message || "Failed to log out",
          life: 3000,
        });
      }
    },
  },
];

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
      detail: "Please fill all fields correctly",
      life: 3000,
    });
    return;
  }

  const targetUid = currentUser.value?.clinicId;
  if (!targetUid) return;

  try {
    await userStore.changePassword(targetUid, passwords.newPassword);
    toast.add({
      severity: "success",
      summary: "Success",
      detail: "Password changed successfully",
      life: 3000,
    });
    changePasswordDialog.value = false;
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed to change password",
      life: 3000,
    });
  }
}
</script>

<template>
  <div
    class="user-profile-menu flex items-center cursor-pointer"
    @click="toggleProfileMenu"
  >
    <Avatar
      icon="pi pi-user"
      shape="circle"
      size="large"
      class="p-avatar-icon mr-2"
    />
    <div class="flex flex-col text-left">
      <span
        class="text-surface-600 dark:text-surface-0 font-semibold text-[12px]"
        >{{ userName }}</span
      >
      <span class="text-sm text-surface-500 dark:text-surface-0">{{
        userRoleDisplay
      }}</span>
    </div>
    <i class="pi pi-angle-down ml-2 mb-4 text-gray-600"></i>
    <Menu ref="profileMenu" :model="profileMenuItems" :popup="true" />
  </div>

  <!-- <ChangePasswordDialog
    v-model:visible="changePasswordDialog"
    :user="currentUser"
    @save="handleChangePassword"
  /> -->
</template>
