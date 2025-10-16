<script setup lang="ts">
    import { ref, onMounted } from 'vue';
    import { useToast } from 'primevue/usetoast';
    import { FilterMatchMode, FilterOperator } from '@primevue/core/api';
    import EmptyState from '@/components/EmptyState.vue';
    import PatientDialog from '@/components/Patiesnt/PatientDialog.vue';
    import { usePatientStore } from '@/stores/patientStore';
    import { formatDate, formatPhone, getGenderIcon, getGenderTagStyle } from '@/utils/uiHelpers';
    import type { Patient } from '../../../../../shared/types';
    import type DataTable from 'primevue/datatable';

    // --- Stores ---
    const patientStore = usePatientStore();
    const toast = useToast();

    // --- Local state ---
    const selectedPatients = ref<Patient[] | null>(null);
    const patient = ref<Partial<Patient>>({});
    const submitted = ref(false);
    const patientDialog = ref(false);
    const deletePatientDialog = ref(false);
    const deletePatientsDialog = ref(false);

    // DataTable reference
    const dt = ref<InstanceType<typeof DataTable> | null>(null);

    // Filters
    const filters = ref({
        global: { value: null, matchMode: FilterMatchMode.CONTAINS },
        name: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
        phone: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
        address: { operator: FilterOperator.OR, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
        gender: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
        dob: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
        createdAt: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] }
    });

    function clearFilters() {
        filters.value = {
            global: { value: null, matchMode: FilterMatchMode.CONTAINS },
            name: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            phone: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            address: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            gender: { operator: FilterOperator.OR, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
            dob: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
            createdAt: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] }
        };
    }

    // Fetch on mount
    onMounted(() => {
        patientStore.fetchPatients().catch((err) => toast.add({ severity: 'error', summary: 'Error', detail: err.message, life: 3000 }));
    });

    function openNew() {
        patient.value = {};
        submitted.value = false;
        patientDialog.value = true;
    }

    function hideDialog() {
        patientDialog.value = false;
        submitted.value = false;
    }

    async function savePatient(newPatient: Patient) {
        try {
            if (newPatient.id) {
                await patientStore.updatePatient(newPatient);
                toast.add({ severity: 'success', summary: 'Updated', detail: 'Patient updated', life: 3000 });
            } else {
                await patientStore.createPatient(newPatient);
                toast.add({ severity: 'success', summary: 'Created', detail: 'Patient created', life: 3000 });
            }
            patientDialog.value = false;
            patient.value = {};
        } catch (err) {
            toast.add({ severity: 'error', summary: 'Error', detail: patientStore.error || 'Save failed', life: 3000 });
        }
    }

    function editPatient(selected: Patient) {
        patient.value = { ...selected };
        patientDialog.value = true;
    }

    function confirmDeletePatient(selected: Patient) {
        patient.value = { ...selected };
        deletePatientDialog.value = true;
    }

    async function deletePatient() {
        if (!patient.value.id) return;
        await patientStore.deletePatient(patient.value.id);
        deletePatientDialog.value = false;
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'Patient Deleted', life: 3000 });
        patient.value = {};
    }

    function confirmDeleteSelected() {
        deletePatientsDialog.value = true;
    }

    async function deleteSelectedPatients() {
        if (!selectedPatients.value?.length) return;
        const ids = selectedPatients.value.map((p) => p.id);
        await patientStore.deletePatients(ids);
        deletePatientsDialog.value = false;
        selectedPatients.value = null;
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'Patients Deleted', life: 3000 });
    }
</script>

<template>
    <div>
        <div class="card">
            <Toolbar class="mb-6">
                <template #start>
                    <Button label="New" icon="pi pi-plus" severity="secondary" class="mr-2" @click="openNew" />
                    <Button label="Delete" icon="pi pi-trash" severity="secondary" @click="confirmDeleteSelected" :disabled="!selectedPatients || !selectedPatients.length" />
                </template>

                <template #end>
                    <Button type="button" icon="pi pi-filter-slash" label="Clear" class="mr-2" outlined @click="clearFilters" />
                    <IconField>
                        <InputIcon><i class="pi pi-search" /></InputIcon>
                        <InputText v-model="filters['global'].value" placeholder="Search..." />
                    </IconField>
                </template>
            </Toolbar>

            <DataTable
                ref="dt"
                v-model:selection="selectedPatients"
                :value="patientStore.patientList"
                dataKey="id"
                :loading="patientStore.loading"
                :paginator="true"
                :rows="10"
                v-model:filters="filters"
                filterDisplay="menu"
                :globalFilterFields="['name', 'phone', 'address', 'gender', 'dob']"
                paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
                :rowsPerPageOptions="[5, 10, 25]"
                currentPageReportTemplate="Showing {first} to {last} of {totalRecords} Patients"
            >
                <Column selectionMode="multiple" style="width: 3rem" :exportable="false" />

                <Column field="name" header="Name" sortable filter filterField="name" style="min-width: 20rem">
                    <template #body="slotProps">
                        <div class="flex items-center gap-2 cursor-pointer" @click="$router.push({ name: 'patient-profile', params: { id: slotProps.data.id } })">
                            <Avatar :image="slotProps.data.gender == 'Male' ? '/images/default-Male_Avatar.avif' : '/images/default-Female_Avatar.avif'" size="large" shape="circle" />
                            <span>{{ slotProps.data.name }}</span>
                        </div>
                    </template>
                    <template #filter="{ filterModel }">
                        <InputText v-model="filterModel.value" placeholder="Search by name" />
                    </template>
                </Column>

                <Column field="phone" header="Phone" sortable filter filterField="phone" style="min-width: 10rem">
                    <template #body="slotProps">
                        {{ formatPhone(slotProps.data.phone) }}
                    </template>
                    <template #filter="{ filterModel }">
                        <InputText v-model="filterModel.value" placeholder="Search by phone" />
                    </template>
                </Column>

                <Column field="address" header="Address" sortable filter filterField="address" style="min-width: 16rem">
                    <template #filter="{ filterModel }">
                        <InputText v-model="filterModel.value" placeholder="Search by address" />
                    </template>
                </Column>

                <Column field="gender" header="Gender" sortable filter filterField="gender" style="min-width: 10rem">
                    <template #body="slotProps">
                        <Tag :value="slotProps.data.gender" :style="getGenderTagStyle(slotProps.data.gender)" :icon="getGenderIcon(slotProps.data.gender)" />
                    </template>
                    <template #filter="{ filterModel }">
                        <Select v-model="filterModel.value" :options="['Male', 'Female']" placeholder="Select Gender" showClear />
                    </template>
                </Column>

                <Column field="dob" header="Birth Date" dataType="date" sortable filter filterField="dob" style="min-width: 10rem">
                    <template #body="slotProps">
                        {{ formatDate(slotProps.data.dob) }}
                    </template>
                    <template #filter="{ filterModel }">
                        <DatePicker v-model="filterModel.value" dateFormat="dd/mm/yy" placeholder="dd/mm/yyyy" />
                    </template>
                </Column>

                <Column field="createdAt" header="Date Created" dataType="date" sortable filter filterField="createdAt" style="min-width: 10rem">
                    <template #body="slotProps">
                        {{ formatDate(slotProps.data.createdAt) }}
                    </template>
                    <template #filter="{ filterModel }">
                        <DatePicker v-model="filterModel.value" dateFormat="dd/mm/yy" placeholder="dd/mm/yyyy" />
                    </template>
                </Column>

                <Column :exportable="false" header="Actions" style="min-width: 12rem">
                    <template #body="slotProps">
                        <Button icon="pi pi-pencil" outlined rounded class="mr-2" @click="editPatient(slotProps.data)" />
                        <Button icon="pi pi-trash" outlined rounded severity="danger" @click="confirmDeletePatient(slotProps.data)" />
                    </template>
                </Column>

                <!-- 👇 Empty slot -->
                <template #empty>
                    <EmptyState icon="pi pi-users" title="No Patients Found" description="Start by adding a new patient to the system.">
                        <template #action>
                            <Button label="Add Patient" icon="pi pi-plus" class="p-button-sm" @click="openNew" />
                        </template>
                    </EmptyState>
                </template>
            </DataTable>
        </div>

        <PatientDialog v-model:visible="patientDialog" :patient="patient" :isProcessing="patientStore.isProcessing" @save="savePatient" @cancel="hideDialog" />

        <Dialog v-model:visible="deletePatientDialog" :style="{ width: '450px' }" header="Confirm" :modal="true">
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span v-if="patient"
                    >Are you sure you want to delete <b>{{ patient.name }}</b
                    >?</span
                >
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deletePatientDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="patientStore.isProcessing" @click="deletePatient" severity="danger" outlined />
            </template>
        </Dialog>

        <Dialog v-model:visible="deletePatientsDialog" :style="{ width: '450px' }" header="Confirm" :modal="true">
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span v-if="patient">Are you sure you want to delete the selected patients?</span>
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deletePatientsDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="patientStore.isProcessing" @click="deleteSelectedPatients" severity="danger" outlined />
            </template>
        </Dialog>
    </div>
</template>
