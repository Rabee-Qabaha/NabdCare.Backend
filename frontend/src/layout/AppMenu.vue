<script setup>
    import { ref } from 'vue';
    import AppMenuItem from './AppMenuItem.vue';
    import { useRouter } from 'vue-router';
    import { AuthService } from '@/service/AuthService';

    const router = useRouter();

    const model = ref([
        {
            label: 'Home',
            items: [{ label: 'Dashboard', icon: 'pi pi-fw pi-home', to: '/' }]
        },
        {
            label: 'Patients',
            items: [
                { label: 'Patients', icon: 'pi pi-fw pi-address-book', to: '/pages/patients' },
                { label: 'Payments', icon: 'pi pi-fw pi-wallet', to: '/pages/payments' }
            ]
        },
        {
            label: 'Users',
            items: [
                { label: 'Users', icon: 'pi pi-fw pi-users', to: '/pages/users' },
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
        // {
        //     label: 'Get Started',
        //     items: [
        //         {
        //             label: 'Documentation',
        //             icon: 'pi pi-fw pi-book',
        //             to: '/documentation'
        //         },
        //         {
        //             label: 'Sakai Vue TS Source',
        //             icon: 'pi pi-fw pi-github',
        //             url: 'https://github.com/mrevjd/sakai-vue-ts',
        //             target: '_blank'
        //         },
        //         {
        //             label: 'Original Source',
        //             icon: 'pi pi-fw pi-github',
        //             url: 'https://github.com/primefaces/sakai-vue',
        //             target: '_blank'
        //         }
        //     ]
        // }
    ]);
</script>

<template>
    <ul class="layout-menu">
        <template v-for="(item, i) in model" :key="item">
            <app-menu-item v-if="!item.separator" :item="item" :index="i"></app-menu-item>
            <li v-if="item.separator" class="menu-separator"></li>
        </template>
    </ul>
</template>

<style lang="scss" scoped></style>
