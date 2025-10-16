<script setup lang="ts">
    import { ref, onMounted } from 'vue';
    import { useToast } from 'primevue/usetoast';
    import { FilterMatchMode, FilterOperator } from '@primevue/core/api';
    import { formatDate } from '@/utils/uiHelpers';
    import { useXRayStore } from '@/stores/xrayStore';
    import XRayDialog from '@/components/Patiesnt/XRayDialog.vue';
    import EmptyState from '@/components/EmptyState.vue';
    import type { XRay } from '../../../../../shared/types';

    // Props
    const props = defineProps<{ patientId: string }>();

    // Stores
    const xrayStore = useXRayStore();
    const toast = useToast();

    // Local state
    const dt = ref<any>(null);
    const xrayDialog = ref(false);
    const selectedXRay = ref<Partial<XRay>>({});
    const selectedXrays = ref<XRay[] | null>(null);
    const deleteDialog = ref(false);
    const deleteManyDialog = ref(false);
    const submitted = ref(false);

    // Filters
    const filters = ref({
        global: { value: null as string | null, matchMode: FilterMatchMode.CONTAINS },
        uploadedAt: { operator: FilterOperator.AND, constraints: [{ value: null as Date | null, matchMode: FilterMatchMode.DATE_IS }] },
        uploadedBy: { operator: FilterOperator.AND, constraints: [{ value: null as string | null, matchMode: FilterMatchMode.CONTAINS }] },
        description: { operator: FilterOperator.AND, constraints: [{ value: null as string | null, matchMode: FilterMatchMode.CONTAINS }] }
    });

    function clearFilters() {
        filters.value = {
            global: { value: null, matchMode: FilterMatchMode.CONTAINS },
            uploadedAt: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
            uploadedBy: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            description: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] }
        };
    }

    // Lifecycle
    onMounted(() => {
        xrayStore.fetchXraysByPatient(props.patientId).catch((err) => toast.add({ severity: 'error', summary: 'Error', detail: err.message, life: 3000 }));
    });

    // Actions
    function openNew() {
        selectedXRay.value = { patientId: props.patientId };
        submitted.value = false;
        xrayDialog.value = true;
    }

    function openEdit(xray: XRay) {
        selectedXRay.value = { ...xray };
        submitted.value = false;
        xrayDialog.value = true;
    }

    function hideDialog() {
        xrayDialog.value = false;
        submitted.value = false;
    }

    async function saveXRay(xrayToSave: Partial<XRay>) {
        try {
            if (xrayToSave.id) {
                await xrayStore.updateXRay(xrayToSave as XRay);
                toast.add({ severity: 'success', summary: 'Updated', detail: 'X-ray updated', life: 3000 });
            } else {
                await xrayStore.createXRay(xrayToSave);
                toast.add({ severity: 'success', summary: 'Created', detail: 'X-ray added', life: 3000 });
            }
            hideDialog();
        } catch (err: any) {
            toast.add({ severity: 'error', summary: 'Error', detail: xrayStore.error || 'Save failed', life: 3000 });
        }
    }

    function confirmDelete(xray: XRay) {
        selectedXRay.value = { ...xray };
        deleteDialog.value = true;
    }

    async function deleteXRay() {
        if (!selectedXRay.value.id) return;
        await xrayStore.deleteXRay(selectedXRay.value.id);
        deleteDialog.value = false;
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'X-ray deleted', life: 3000 });
    }

    function confirmDeleteSelected() {
        deleteManyDialog.value = true;
    }

    async function deleteSelectedXrays() {
        if (!selectedXrays.value?.length) return;
        for (const x of selectedXrays.value) {
            if (x.id) await xrayStore.deleteXRay(x.id);
        }
        deleteManyDialog.value = false;
        selectedXrays.value = null;
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'X-rays deleted', life: 3000 });
    }

    // Utils
    function truncate(text: string, length = 40) {
        if (!text) return '';
        return text.length > length ? text.slice(0, length) + '…' : text;
    }
</script>

<template>
    <div>
        <!-- Toolbar -->
        <Toolbar class="mb-2">
            <template #start>
                <Button label="New" icon="pi pi-plus" severity="secondary" class="mr-2" @click="openNew" />
                <Button label="Delete" icon="pi pi-trash" severity="secondary" @click="confirmDeleteSelected" :disabled="!selectedXrays || selectedXrays.length === 0" />
            </template>

            <template #end>
                <Button type="button" icon="pi pi-filter-slash" label="Clear" class="mr-2" outlined @click="clearFilters" />
                <IconField>
                    <InputIcon><i class="pi pi-search" /></InputIcon>
                    <InputText id="xray-global-search" v-model="filters['global'].value" placeholder="Search..." />
                </IconField>
            </template>
        </Toolbar>

        <!-- DataTable -->
        <DataTable
            ref="dt"
            v-model:selection="selectedXrays"
            :value="xrayStore.getXRayByPatient(props.patientId).value"
            dataKey="id"
            :loading="xrayStore.loading"
            :paginator="true"
            :rows="8"
            size="small"
            v-model:filters="filters"
            filterDisplay="menu"
            :globalFilterFields="['description', 'uploadedBy']"
            paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
            :rowsPerPageOptions="[5, 8, 25]"
            currentPageReportTemplate="Showing {first} to {last} of {totalRecords} X-rays"
        >
            <!-- <Column selectionMode="multiple" style="width: 2rem" /> -->

            <Column field="uploadedAt" header="Uploaded At" sortable filter filterField="uploadedAt" style="min-width: 8rem">
                <template #body="{ data }">{{ formatDate(data.uploadedAt) }}</template>
                <template #filter="{ filterModel }">
                    <DatePicker v-model="filterModel.value" dateFormat="dd/mm/yy" placeholder="dd/mm/yyyy" />
                </template>
            </Column>

            <Column field="description" header="Description" sortable filter filterField="description" style="min-width: 12rem">
                <template #body="{ data }">
                    <span v-tooltip.top="data.description">{{ truncate(data.description) }}</span>
                </template>
                <template #filter="{ filterModel }">
                    <InputText v-model="filterModel.value" placeholder="Search by description" />
                </template>
            </Column>

            <Column field="uploadedBy" header="Uploaded By" sortable filter filterField="uploadedBy" style="min-width: 10rem">
                <template #filter="{ filterModel }">
                    <InputText v-model="filterModel.value" placeholder="Search by user" />
                </template>
            </Column>

            <Column field="imageUrl" header="Image" :exportable="false" style="min-width: 8rem">
                <template #body="{ data }">
                    <Image v-if="data.imageUrl" :src="data.imageUrl" alt="X-ray" preview imageStyle="width: 4rem; height: 3rem; object-fit: cover; border-radius: 0.5rem; cursor: pointer;" />
                    <span v-else class="text-gray-400 italic">No image</span>
                </template>
            </Column>

            <Column header="Actions" :exportable="false" style="min-width: 10rem">
                <template #body="{ data }">
                    <Button icon="pi pi-pencil" rounded outlined class="mr-2" @click="openEdit(data)" />
                    <Button icon="pi pi-trash" rounded outlined severity="danger" @click="confirmDelete(data)" />
                </template>
            </Column>

            <!-- Empty state -->
            <template #empty>
                <EmptyState icon="pi pi-images" title="No X-Rays Found" description="This patient has no recorded X-rays yet.">
                    <template #action>
                        <Button label="Add X-Ray" icon="pi pi-plus" class="p-button-sm" @click="openNew" />
                    </template>
                </EmptyState>
            </template>
        </DataTable>

        <!-- Dialogs -->
        <XRayDialog v-model:visible="xrayDialog" :xray="selectedXRay" :isProcessing="xrayStore.isProcessing" @save="saveXRay" @cancel="hideDialog" />

        <Dialog v-model:visible="deleteDialog" :style="{ width: '450px' }" header="Confirm" modal>
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span>Are you sure you want to delete this X-ray?</span>
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deleteDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="xrayStore.isProcessing" @click="deleteXRay" severity="danger" outlined />
            </template>
        </Dialog>

        <Dialog v-model:visible="deleteManyDialog" :style="{ width: '450px' }" header="Confirm" modal>
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span>Are you sure you want to delete the selected X-rays?</span>
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deleteManyDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="xrayStore.isProcessing" @click="deleteSelectedXrays" severity="danger" outlined />
            </template>
        </Dialog>
    </div>
</template>
