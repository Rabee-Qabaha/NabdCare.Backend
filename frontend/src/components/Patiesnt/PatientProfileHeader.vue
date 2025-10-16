<template>
    <div class="card shadow-2 border-round overflow-hidden p-0">
        <!-- Cover Image -->
        <img src="/images/user-profile-header-bg.png" alt="Cover" class="w-full object-cover" style="height: 200px" />

        <!-- Profile Info -->
        <div class="relative flex flex-col md:flex-row items-start md:items-center px-4 md:px-6 py-6 md:py-10 md:pl-40">
            <!-- Avatar -->
            <div class="absolute md:absolute -top-16 left-4 md:left-6 border-3 border-white border-round overflow-hidden shadow-2" style="width: 130px; height: 150px">
                <img :src="patient?.gender === 'Female' ? '/images/default-Female_Avatar.avif' : '/images/default-Male_Avatar.avif'" alt="Profile" class="w-full h-full object-cover" />
            </div>

            <!-- Details -->
            <div class="mt-20 md:mt-0 md:ml-8 flex-1 w-full">
                <h2 class="m-0 text-lg font-bold">{{ patient?.name }}</h2>
                <div class="flex flex-col sm:flex-row sm:flex-wrap gap-2 text-500 text-sm mt-2">
                    <div class="flex items-center gap-1"><i class="pi pi-phone"></i> {{ formatPhone(patient?.phone) }}</div>
                    <div class="flex items-center gap-1"><i class="pi pi-map-marker"></i> {{ patient?.address }}</div>
                    <div class="flex items-center gap-1"><i class="pi pi-calendar"></i> Joined {{ formatDate(patient?.createdAt) }}</div>
                </div>
            </div>

            <!-- Edit Button -->
            <div class="w-full md:w-auto mt-4 md:mt-0 md:ml-auto">
                <Button label="Edit profile" icon="pi pi-cog" severity="primary" class="w-full md:w-auto" @click="openEditDialog" />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
    import type { Patient } from '@/../../shared/types';
    import { formatDate, formatPhone } from '@/utils/uiHelpers';
    import { toRefs } from 'vue';

    const props = defineProps<{
        patient: Patient | null;
        openEditDialog: () => void;
    }>();

    const { patient, openEditDialog } = toRefs(props);
</script>
