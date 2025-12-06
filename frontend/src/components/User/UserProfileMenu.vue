<script setup lang="ts">
  import Avatar from 'primevue/avatar';
  import Menu from 'primevue/menu';
  import { useToast } from 'primevue/usetoast';
  import { computed, ref } from 'vue';
  import { useRouter } from 'vue-router';

  import ChangePasswordDialog from '@/components/User/ChangePasswordDialog.vue';
  import { useAuthStore } from '@/stores/authStore';

  const toast = useToast();
  const router = useRouter();
  const authStore = useAuthStore();

  // ======================
  // 🔹 STATE
  // ======================
  const profileMenu = ref<InstanceType<typeof Menu> | null>(null);
  const changePasswordDialog = ref(false);

  // ======================
  // 🧠 COMPUTED
  // ======================
  const currentUser = computed(() => authStore.currentUser);

  // Fallback to 'User' if data is missing
  const userName = computed(
    () => currentUser.value?.fullName || currentUser.value?.email || 'User',
  );
  const initials = computed(() => userName.value.charAt(0).toUpperCase());

  const userRoleDisplay = computed(() => {
    const role = currentUser.value?.roleName || 'User';
    // Format CamelCase to Spaced (e.g. "SystemAdmin" -> "System Admin")
    return role.replace(/([a-z])([A-Z])/g, '$1 $2');
  });

  // ======================
  // 📋 MENU ACTIONS
  // ======================
  const profileMenuItems = [
    {
      label: 'Change Password',
      icon: 'pi pi-key',
      command: () => {
        changePasswordDialog.value = true;
      },
    },
    { separator: true },
    {
      label: 'Log Out',
      icon: 'pi pi-power-off text-red-500',
      command: async () => {
        try {
          await authStore.logout();
          await router.push({ name: 'login' });
          toast.add({
            severity: 'secondary',
            summary: 'Logged Out',
            detail: 'See you next time!',
            life: 2000,
          });
        } catch (err: any) {
          console.error(err);
        }
      },
    },
  ];

  const toggleProfileMenu = (event: MouseEvent) => profileMenu.value?.toggle(event);
</script>

<template>
  <div
    class="user-profile-menu flex cursor-pointer select-none items-center gap-2 rounded-lg p-1 hover:bg-surface-100 dark:hover:bg-surface-800 transition-colors duration-200"
    @click="toggleProfileMenu"
    aria-haspopup="true"
    aria-controls="overlay_menu"
  >
    <Avatar
      :label="initials"
      shape="circle"
      class="bg-primary-50 text-primary-600 dark:bg-primary-900/30 dark:text-primary-400 font-bold"
    />

    <div class="flex flex-col text-left leading-tight">
      <span class="text-sm font-semibold text-surface-900 dark:text-surface-0">
        {{ userName }}
      </span>
      <span class="text-xs text-surface-500 dark:text-surface-400">
        {{ userRoleDisplay }}
      </span>
    </div>

    <i class="pi pi-angle-down text-surface-500 dark:text-surface-400 text-xs"></i>

    <Menu ref="profileMenu" :model="profileMenuItems" :popup="true" />
  </div>

  <ChangePasswordDialog
    v-if="currentUser?.id"
    v-model:visible="changePasswordDialog"
    :userId="currentUser.id"
  />
</template>
