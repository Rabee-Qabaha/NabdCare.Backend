<script setup lang="ts">
  import { computed } from 'vue';
  import AppMenuItem from './AppMenuItem.vue';
  import Button from 'primevue/button';
  import { useRouter } from 'vue-router';
  import { useAuthStore } from '@/stores/authStore';
  import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
  import { useToastService } from '@/service/toastService';

  const router = useRouter();
  const authStore = useAuthStore();
  const { handleErrorAndNotify } = useErrorHandler();
  const toast = useToastService();

  const handleLogout = async () => {
    try {
      await authStore.logout();
      await router.push({ name: 'login' });
      toast.success('Logged out successfully');
    } catch (error) {
      await handleErrorAndNotify(error);
    }
  };

  // SuperAdmin Menu
  const superAdminMenu = [
    {
      label: 'Home',
      items: [{ label: 'Dashboard', icon: 'pi pi-fw pi-home', to: '/superadmin' }],
    },
    {
      label: 'Management',
      items: [{ label: 'Users', icon: 'pi pi-fw pi-users', to: '/superadmin/users' }],
    },
  ];

  // Client Menu
  const clientMenu = [
    {
      label: 'Home',
      items: [{ label: 'Dashboard', icon: 'pi pi-fw pi-home', to: '/client' }],
    },
    {
      label: 'Patients',
      items: [
        { label: 'Patients', icon: 'pi pi-fw pi-address-book', to: '/client/patients' },
        { label: 'Payments', icon: 'pi pi-fw pi-wallet', to: '/client/payments' },
      ],
    },
    {
      label: 'Users',
      items: [{ label: 'Users', icon: 'pi pi-fw pi-users', to: '/client/users' }],
    },
  ];

  const model = computed(() =>
    authStore.isSuperAdmin ? superAdminMenu : clientMenu
  );
</script>

<template>
  <div class="flex flex-col h-full">
    <!-- Main menu items -->
    <ul class="layout-menu flex-grow overflow-y-auto">
      <template v-for="(item, i) in model" :key="item.label">
        <app-menu-item v-if="!item.separator" :item="item" :index="i"></app-menu-item>
        <li v-if="item.separator" class="menu-separator"></li>
      </template>
    </ul>

    <!-- Logout button at bottom (always visible) -->
    <div class="border-t border-surface-border p-3">
      <Button
        label="Log out"
        icon="pi pi-power-off"
        severity="secondary"
        outlined
        class="w-full"
        @click="handleLogout"
      />
    </div>
  </div>
</template>