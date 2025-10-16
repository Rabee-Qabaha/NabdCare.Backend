<script setup lang="ts">
    import VisitDialog from '@/components/Patiesnt/VisitDialog.vue';
    import { formatDate, formatCurrency, getVisitTypeTagClass } from '@/utils/uiHelpers';
    import { FilterMatchMode, FilterOperator } from '@primevue/core/api';
    import { useToast } from 'primevue/usetoast';
    import { onMounted, ref } from 'vue';
    import type { PatientVisit } from '../../../../../shared/types';
    import { VISIT_TYPES } from '../../../../../shared/types';
    import EmptyState from '@/components/EmptyState.vue';
    import { useVisitStore } from '@/stores/visitStore';

    // Props
    const props = defineProps<{
        patientId: string;
    }>();

    // --- Stores ---
    const visitStore = useVisitStore();
    const toast = useToast();

    const VisitType = ref(VISIT_TYPES.map((v) => ({ label: v, value: v })));

    // --- Local state ---
    const dt = ref<any>(null);
    const visitDialog = ref(false);
    const deleteVisitDialog = ref(false);
    const deleteVisitsDialog = ref(false);
    const visit = ref<Partial<PatientVisit>>({});
    const selectedVisits = ref<PatientVisit[] | null>(null);
    const expandedRows = ref<PatientVisit[]>([]);
    const submitted = ref(false);

    // Filters
    const filters = ref({
        global: { value: null as string | null, matchMode: FilterMatchMode.CONTAINS },
        date: { operator: FilterOperator.AND, constraints: [{ value: null as Date | null, matchMode: FilterMatchMode.DATE_IS }] },
        visitType: { operator: FilterOperator.OR, constraints: [{ value: null as string | null, matchMode: FilterMatchMode.EQUALS }] },
        reason: { operator: FilterOperator.AND, constraints: [{ value: null as string | null, matchMode: FilterMatchMode.CONTAINS }] },
        fee: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
        practitioner: { operator: FilterOperator.AND, constraints: [{ value: null as string | null, matchMode: FilterMatchMode.CONTAINS }] },
        nextAppointment: { operator: FilterOperator.AND, constraints: [{ value: null as Date | null, matchMode: FilterMatchMode.DATE_IS }] }
    });

    function clearFilters() {
        filters.value = {
            global: { value: null, matchMode: FilterMatchMode.CONTAINS },
            date: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
            visitType: { operator: FilterOperator.OR, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
            reason: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            fee: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
            practitioner: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            nextAppointment: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] }
        };
    }

    // --- Lifecycle ---
    onMounted(() => {
        visitStore.fetchVisitsByPatientId(props.patientId).catch((err) => toast.add({ severity: 'error', summary: 'Error', detail: err.message, life: 3000 }));
    });

    // --- Actions ---
    function openNew() {
        visit.value = { patientId: props.patientId };
        submitted.value = false;
        visitDialog.value = true;
    }

    function hideDialog() {
        visitDialog.value = false;
        submitted.value = false;
    }

    async function saveVisit(newVisit: Partial<PatientVisit>) {
        submitted.value = true;

        if (!newVisit.patientId) newVisit.patientId = props.patientId;

        if (newVisit.practitioner?.trim() && newVisit.reason && newVisit.visitType && newVisit.date) {
            try {
                if (newVisit.id) {
                    await visitStore.updateVisit(newVisit as PatientVisit);
                    toast.add({ severity: 'success', summary: 'Updated', detail: 'Visit updated', life: 3000 });
                } else {
                    const visitToSave = {
                        ...newVisit,
                        nextAppointment: newVisit.nextAppointment ?? null
                    };
                    await visitStore.createVisit(visitToSave as PatientVisit);
                    toast.add({ severity: 'success', summary: 'Created', detail: 'Visit created', life: 3000 });
                }

                visitDialog.value = false;
                visit.value = {};
            } catch (err: any) {
                toast.add({ severity: 'error', summary: 'Error', detail: visitStore.error || 'Save failed', life: 3000 });
            }
        }
    }

    function editVisit(selectedVisit: PatientVisit) {
        visit.value = { ...selectedVisit };
        visitDialog.value = true;
    }

    function confirmDeleteVisit(selectedVisit: PatientVisit) {
        visit.value = { ...selectedVisit };
        deleteVisitDialog.value = true;
    }

    async function deleteVisit() {
        if (!visit.value.id) return;
        await visitStore.deleteVisit(props.patientId, visit.value.id);
        deleteVisitDialog.value = false;
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'Visit deleted', life: 3000 });
        visit.value = {};
    }

    function confirmDeleteSelected() {
        deleteVisitsDialog.value = true;
    }

    async function deleteSelectedVisits() {
        if (!selectedVisits.value || selectedVisits.value.length === 0) return;
        const ids = selectedVisits.value.map((p) => p.id).filter((id): id is string => !!id);
        await visitStore.deleteVisits(props.patientId, ids);
        deleteVisitsDialog.value = false;
        selectedVisits.value = null;
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'Visits deleted', life: 3000 });
    }
</script>

<template>
    <div>
        <Toolbar class="mb-2">
            <template #start>
                <Button label="New" icon="pi pi-plus" severity="secondary" class="mr-2" @click="openNew" />
                <Button label="Delete" icon="pi pi-trash" severity="secondary" @click="confirmDeleteSelected" :disabled="!selectedVisits || selectedVisits.length === 0" />
            </template>

            <template #end>
                <Button type="button" icon="pi pi-filter-slash" label="Clear" class="mr-2" outlined @click="clearFilters" />
                <IconField>
                    <InputIcon><i class="pi pi-search" /></InputIcon>
                    <InputText id="visit-global-search" v-model="filters['global'].value" placeholder="Search..." />
                </IconField>
            </template>
        </Toolbar>

        <DataTable
            ref="dt"
            v-model:selection="selectedVisits"
            v-model:expandedRows="expandedRows"
            :value="visitStore.visitsByPatient(props.patientId).value"
            dataKey="id"
            :loading="visitStore.loading"
            :paginator="true"
            :rows="10"
            size="small"
            v-model:filters="filters"
            filterDisplay="menu"
            :globalFilterFields="['date', 'visitType', 'reason', 'fee', 'practitioner', 'nextAppointment']"
            paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
            :rowsPerPageOptions="[5, 10, 25]"
            currentPageReportTemplate="Showing {first} to {last} of {totalRecords} Visits"
        >
            <!-- Row Expander -->
            <Column expander style="width: 2rem" />
            <Column selectionMode="multiple" style="width: 2rem" />

            <Column field="date" header="Vist Date" dataType="date" sortable filter filterField="date" style="min-width: 8rem">
                <template #body="{ data }">{{ formatDate(data.date) }}</template>
                <template #filter="{ filterModel }">
                    <DatePicker v-model="filterModel.value" dateFormat="dd/mm/yy" placeholder="dd/mm/yyyy" />
                </template>
            </Column>

            <Column field="visitType" header="Type" sortable style="min-width: 7rem">
                <template #body="{ data }">
                    <span :class="getVisitTypeTagClass(data.visitType)">
                        {{ data.visitType }}
                    </span>
                </template>
                <template #filter="{ filterModel }">
                    <Select v-model="filterModel.value" :options="VisitType" optionLabel="label" optionValue="value" placeholder="Select Type" showClear />
                </template>
            </Column>

            <Column field="reason" header="Reason" sortable filter filterField="reason" style="min-width: 12rem">
                <template #body="{ data }">
                    {{ data.reason }}
                </template>
                <template #filter="{ filterModel }">
                    <InputText v-model="filterModel.value" placeholder="Search by Reason" />
                </template>
            </Column>

            <Column field="fee" header="Fee" sortable style="min-width: 5rem">
                <template #body="{ data }">
                    <span :class="data.fee < 0 ? 'font-semibold text-red-400' : 'text-[15px] text-primary-500'">
                        {{ formatCurrency(data.fee) }}
                    </span>
                </template>
            </Column>

            <Column field="practitioner" header="Practitioner" sortable filter filterField="practitioner" style="min-width: 12rem">
                <template #body="{ data }">
                    {{ data.practitioner }}
                </template>
                <template #filter="{ filterModel }">
                    <InputText v-model="filterModel.value" placeholder="Search by Practitioner" />
                </template>
            </Column>

            <Column field="nextAppointment" header="Next Visit" dataType="date" sortable filter filterField="nextAppointment" style="min-width: 8rem">
                <template #body="{ data }">{{ formatDate(data.nextAppointment) }}</template>
                <template #filter="{ filterModel }">
                    <DatePicker v-model="filterModel.value" dateFormat="dd/mm/yy" placeholder="dd/mm/yyyy" />
                </template>
            </Column>

            <Column :exportable="false" header="Actions" style="min-width: 8rem">
                <template #body="slotProps">
                    <Button icon="pi pi-pencil" outlined rounded class="mr-2" @click="editVisit(slotProps.data)" />
                    <Button icon="pi pi-trash" outlined rounded severity="danger" @click="confirmDeleteVisit(slotProps.data)" />
                </template>
            </Column>

            <!-- Expansion Template -->
            <template #expansion="slotProps">
                <div>
                    <Editor v-model="slotProps.data.description" editorStyle="max-height: 200px; overflow-y: auto;" readonly>
                        <template #toolbar>
                            <span class="ql-formats block text-muted-color font-medium">Visit Description</span>
                        </template>
                    </Editor>
                </div>
            </template>

            <!-- Empty slot -->
            <template #empty>
                <EmptyState icon="pi pi-history" title="No Visits Found" description="This patient has no recorded visits yet.">
                    <template #action>
                        <Button label="Add Visit" icon="pi pi-plus" class="p-button-sm" @click="openNew" />
                    </template>
                </EmptyState>
            </template>
        </DataTable>

        <VisitDialog v-model:visible="visitDialog" :visit="visit" :isProcessing="visitStore.isProcessing" @save="saveVisit" @cancel="hideDialog" />

        <!-- Delete Single -->
        <Dialog v-model:visible="deleteVisitDialog" :style="{ width: '450px' }" header="Confirm" modal>
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span>Are you sure you want to delete the visit?</span>
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deleteVisitDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="visitStore.isProcessing" @click="deleteVisit" severity="danger" outlined />
            </template>
        </Dialog>

        <!-- Delete Multiple -->
        <Dialog v-model:visible="deleteVisitsDialog" :style="{ width: '450px' }" header="Confirm" modal>
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span>Are you sure you want to delete the selected visits?</span>
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deleteVisitsDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="visitStore.isProcessing" @click="deleteSelectedVisits" severity="danger" outlined />
            </template>
        </Dialog>
    </div>
</template>
