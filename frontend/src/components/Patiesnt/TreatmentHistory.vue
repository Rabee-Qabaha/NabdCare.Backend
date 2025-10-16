<template>
    <div class="card h-[465px] flex flex-col overflow-hidden">
        <!-- Header -->
        <div class="text-muted-color font-semibold text-xl mb-6 flex-shrink-0">
            {{ header }}
        </div>

        <!-- Scrollable Timeline -->
        <ScrollPanel style="height: 100%" class="pb-14">
            <Timeline v-if="visits?.length" :value="visits">
                <template #opposite="slotProps">
                    <small>{{ formatDate(slotProps.item.date) }}</small>
                </template>

                <template #content="slotProps">
                    <div class="p-2">
                        <div class="font-medium">{{ slotProps.item.practitioner }}</div>
                        <span :class="getVisitTypeTagClass(slotProps.item.visitType) + ' px-2 py-1 rounded text-xs font-semibold border-0'">
                            {{ slotProps.item.visitType }}
                        </span>
                    </div>
                </template>
            </Timeline>

            <!-- Our custom EmptyState -->
            <EmptyState v-else icon="pi pi-history" title="No Treatment History" description="This patient has no recorded visits yet." containerClass="h-full">
                <!-- optional action slot -->
                <!-- <template #action>
                    <Button label="Add Visit" icon="pi pi-plus" class="p-button-sm" />
                </template> -->
            </EmptyState>
        </ScrollPanel>
    </div>
</template>

<script setup lang="ts">
    import type { PatientVisit } from '../../../../shared/types';
    import { formatDate, getVisitTypeTagClass } from '@/utils/uiHelpers';
    import { toRefs } from 'vue';

    // PrimeVue components
    import Timeline from 'primevue/timeline';
    import ScrollPanel from 'primevue/scrollpanel';

    // Our custom component
    import EmptyState from '@/components/EmptyState.vue';

    const props = defineProps<{
        visits?: PatientVisit[];
        header?: string;
    }>();

    const { visits, header = 'Treatment History' } = toRefs(props);
</script>
