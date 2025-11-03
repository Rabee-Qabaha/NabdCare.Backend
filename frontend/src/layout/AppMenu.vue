// AppMenu.vue
<script setup type="ts">
import { ref, computed } from 'vue';
import AppMenuItem from './AppMenuItem.vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { AuthService } from '@/service/AuthService';

const router = useRouter();
const authStore = useAuthStore();

// ✅ SuperAdmin Menu
const superAdminMenu = [
    {
        label: 'Home',
        items: [{ label: 'Dashboard', icon: 'pi pi-fw pi-home', to: '/superadmin' }]
    },
    {
        label: 'Management',
        items: [
            { label: 'Users', icon: 'pi pi-fw pi-users', to: '/superadmin/users' }
        ]
    },
    {
        label: 'Account',
        items: [
            {
                label: 'Log out',
                icon: 'pi pi-fw pi-power-off',
                command: async () => {
                    await AuthService.logout();
                    router.push('/auth/login');
                }
            }
        ]
    }
];

// ✅ Client Menu (ClinicAdmin, Doctor, Nurse, Receptionist)
const clientMenu = [
    {
        label: 'Home',
        items: [{ label: 'Dashboard', icon: 'pi pi-fw pi-home', to: '/client' }]
    },
    {
        label: 'Patients',
        items: [
            { label: 'Patients', icon: 'pi pi-fw pi-address-book', to: '/client/patients' },
            { label: 'Payments', icon: 'pi pi-fw pi-wallet', to: '/client/payments' }
        ]
    },
    {
        label: 'Users',
        items: [
            { label: 'Users', icon: 'pi pi-fw pi-users', to: '/client/users' },
            {
                label: 'Log out',
                icon: 'pi pi-fw pi-power-off',
                command: async () => {
                    await AuthService.logout();
                    router.push('/auth/login');
                }
            }
        ]
    }
];

// ✅ Show different menu based on user role
const model = computed(() => {
    if (authStore.isSuperAdmin) {
        return superAdminMenu;
    }
    return clientMenu;
});
</script>

<template>
  <ul class="layout-menu">
    <template v-for="(item, i) in model" :key="item">
      <app-menu-item
        v-if="!item.separator"
        :item="item"
        :index="i"
      ></app-menu-item>
      <li v-if="item.separator" class="menu-separator"></li>
    </template>
  </ul>
</template>

<style lang="scss" scoped></style>
