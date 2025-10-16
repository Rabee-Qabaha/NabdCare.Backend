<script setup lang="ts">
    import { FilterMatchMode, FilterOperator } from '@primevue/core/api';
    import { useToast } from 'primevue/usetoast';
    import { computed, onMounted, ref } from 'vue';
    import { usePaymentStore } from '@/stores/paymentStore';
    import type { Payment, PaymentWithPatient } from '../../../../../shared/types';
    import { PAYMENT_METHODS } from '../../../../../shared/types';
    import { formatCurrency, formatDate, formatPhone, getPaymentRenderInfo } from '@/utils/uiHelpers';
    import EmptyState from '@/components/EmptyState.vue';
    import PaymentDialog from '@/components/Payments/PaymentDialog.vue';

    const props = defineProps<{
        patientId?: string;
        showPatientInfo?: boolean;
    }>();

    const toast = useToast();
    const paymentStore = usePaymentStore();

    const dt = ref();
    const paymentDialog = ref(false);
    const deletePaymentDialog = ref(false);
    const deletePaymentsDialog = ref(false);
    const payment = ref<Partial<PaymentWithPatient>>({});
    const selectedPayments = ref<PaymentWithPatient[]>([]);
    const submitted = ref(false);

    const paymentOptions = ref(PAYMENT_METHODS.map((m) => ({ label: m, value: m })));

    const filters = ref({
        global: { value: null, matchMode: FilterMatchMode.CONTAINS },
        'patient.name': { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
        'patient.phone': { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
        amount: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
        method: { operator: FilterOperator.OR, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
        paidAt: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
        createdAt: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
        notes: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] }
    });

    function clearFilters() {
        filters.value = {
            global: { value: null, matchMode: FilterMatchMode.CONTAINS },
            'patient.name': { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            'patient.phone': { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] },
            amount: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
            method: { operator: FilterOperator.OR, constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }] },
            paidAt: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
            createdAt: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }] },
            notes: { operator: FilterOperator.AND, constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }] }
        };
    }

    async function loadPayments() {
        await paymentStore.fetchPayments(props.patientId);
    }

    const displayedPayments = computed(() => {
        return props.patientId ? paymentStore.paymentsByPatient(props.patientId).value : paymentStore.paymentsAll;
    });

    onMounted(loadPayments);

    function openNew() {
        payment.value = {};
        submitted.value = false;
        paymentDialog.value = true;
    }

    function hideDialog() {
        paymentDialog.value = false;
        submitted.value = false;
    }

    async function savePayment(newPayment: Payment) {
        try {
            if (props.patientId) newPayment.patientId = props.patientId;
            if (!newPayment.createdAt) newPayment.createdAt = new Date();

            if (newPayment.id) {
                await paymentStore.updatePayment(newPayment);
                toast.add({ severity: 'success', summary: 'Updated', detail: 'Payment updated', life: 3000 });
            } else {
                await paymentStore.createPayment(newPayment);
                toast.add({ severity: 'success', summary: 'Created', detail: 'Payment created', life: 3000 });
            }

            paymentDialog.value = false;
            payment.value = {};
        } catch {
            toast.add({ severity: 'error', summary: 'Error', detail: paymentStore.error || 'Save failed', life: 3000 });
        }
    }

    function editPayment(selectedPayment: Payment) {
        payment.value = { ...selectedPayment };
        paymentDialog.value = true;
    }

    function confirmDeletePayment(selectedPayment: Payment) {
        payment.value = { ...selectedPayment };
        deletePaymentDialog.value = true;
    }

    async function deletePayment() {
        if (!payment.value.id) return;
        await paymentStore.deletePayment(payment.value.id as string, props.patientId);
        deletePaymentDialog.value = false;
        toast.add({ severity: 'success', summary: 'Successful', detail: 'Payment Deleted', life: 3000 });
        payment.value = {};
    }

    function confirmDeleteSelected() {
        deletePaymentsDialog.value = true;
    }

    async function deleteSelectedPayments() {
        if (!selectedPayments.value.length) return;
        const ids = selectedPayments.value.map((p) => p.id).filter((id): id is string => !!id);
        await paymentStore.deletePayments(ids, props.patientId);
        deletePaymentsDialog.value = false;
        selectedPayments.value = [];
        toast.add({ severity: 'success', summary: 'Successful', detail: 'Payments Deleted', life: 3000 });
    }
</script>

<template>
    <div>
        <Toolbar class="mb-2">
            <template #start>
                <Button v-if="!showPatientInfo" label="New" icon="pi pi-plus" severity="secondary" class="mr-2" @click="openNew" />
                <Button label="Delete" icon="pi pi-trash" severity="secondary" @click="confirmDeleteSelected" :disabled="!selectedPayments || !selectedPayments.length" />
            </template>

            <template #end>
                <Button type="button" icon="pi pi-filter-slash" label="Clear" class="mr-2" outlined @click="clearFilters" />
                <IconField>
                    <InputIcon>
                        <i class="pi pi-search" />
                    </InputIcon>
                    <InputText id="payment-global-search" name="globalSearch" v-model="filters['global'].value" placeholder="Search..." />
                </IconField>
            </template>
        </Toolbar>

        <DataTable
            ref="dt"
            v-model:selection="selectedPayments"
            :value="displayedPayments"
            dataKey="id"
            :loading="paymentStore.loading"
            :paginator="true"
            :rows="10"
            :size="!showPatientInfo ? 'small' : undefined"
            v-model:filters="filters"
            filterDisplay="menu"
            :globalFilterFields="['patient.name', 'patient.phone', 'amount', 'method', 'paidAt', 'createdAt', 'notes']"
            paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
            :rowsPerPageOptions="[5, 10, 25]"
            currentPageReportTemplate="Showing {first} to {last} of {totalRecords} Payments"
        >
            <Column selectionMode="multiple" style="width: 3rem" :exportable="false" />

            <!-- Patient Name -->
            <Column v-if="showPatientInfo" field="patient.name" header="Name" sortable filter filterField="patient.name" style="min-width: 20rem">
                <template #body="{ data }">
                    <div class="flex items-center gap-2 cursor-pointer" @click="$router.push({ name: 'patient-profile', params: { id: data.patientId } })">
                        <Avatar :image="data.patient.gender == 'Male' ? '/images/default-Male_Avatar.avif' : '/images/default-Female_Avatar.avif'" size="large" shape="circle" />
                        <span>{{ data.patient.name }}</span>
                    </div>
                </template>
                <template #filter="{ filterModel }">
                    <InputText v-model="filterModel.value" placeholder="Search by name" />
                </template>
            </Column>

            <!-- Patient Phone -->
            <Column v-if="showPatientInfo" field="patient.phone" header="Phone" sortable filter filterField="patient.phone" style="min-width: 10rem">
                <template #body="{ data }">
                    {{ formatPhone(data.patient.phone) }}
                </template>
                <template #filter="{ filterModel }">
                    <InputText v-model="filterModel.value" placeholder="Search by phone" />
                </template>
            </Column>

            <!-- Amount -->
            <Column field="amount" header="Amount" sortable style="min-width: 8rem">
                <template #body="{ data }">
                    <span :class="data.amount < 0 ? 'font-semibold text-red-400' : 'text-[15px] text-primary-500'">
                        {{ formatCurrency(data.amount) }}
                    </span>
                </template>
            </Column>

            <!-- Method -->
            <Column field="method" header="Method" filterField="method" sortable style="min-width: 8rem">
                <template #body="{ data }">
                    <span :class="getPaymentRenderInfo(data.method).classes">
                        <i :class="getPaymentRenderInfo(data.method).icon + ' mr-1'"></i>
                        {{ data.method }}
                    </span>
                </template>
                <template #filter="{ filterModel }">
                    <Select v-model="filterModel.value" :options="paymentOptions" placeholder="Select Method" showClear />
                </template>
            </Column>

            <!-- Paid At -->
            <Column field="paidAt" header="Paid At" dataType="date" sortable style="min-width: 11rem">
                <template #body="{ data }">{{ formatDate(data.paidAt) }}</template>
                <template #filter="{ filterModel }">
                    <DatePicker v-model="filterModel.value" dateFormat="dd/mm/yy" placeholder="dd/mm/yyyy" />
                </template>
            </Column>

            <!-- Created At -->
            <Column field="createdAt" header="Created At" dataType="date" sortable style="min-width: 11rem">
                <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
                <template #filter="{ filterModel }">
                    <DatePicker v-model="filterModel.value" dateFormat="dd/mm/yy" placeholder="dd/mm/yyyy" />
                </template>
            </Column>

            <!-- Notes -->
            <Column field="notes" header="Notes" sortable style="min-width: 12rem">
                <template #filter="{ filterModel }">
                    <InputText v-model="filterModel.value" placeholder="Search by notes" />
                </template>
            </Column>

            <!-- Actions -->
            <Column :exportable="false" header="Actions" style="min-width: 8rem">
                <template #body="slotProps">
                    <Button icon="pi pi-pencil" outlined rounded class="mr-2" @click="editPayment(slotProps.data)" />
                    <Button icon="pi pi-trash" outlined rounded severity="danger" @click="confirmDeletePayment(slotProps.data)" />
                </template>
            </Column>

            <!-- Empty State -->
            <template #empty>
                <EmptyState icon="pi pi-history" title="No Payments Found" description="There are no recorded payments yet.">
                    <template #action>
                        <Button v-if="!showPatientInfo" label="Add Payment" icon="pi pi-plus" class="p-button-sm" @click="openNew" />
                    </template>
                </EmptyState>
            </template>
        </DataTable>

        <PaymentDialog v-model:visible="paymentDialog" :payment="payment" :isProcessing="paymentStore.isProcessing" @save="savePayment" @cancel="hideDialog" />

        <Dialog v-model:visible="deletePaymentDialog" :style="{ width: '450px' }" header="Confirm" :modal="true">
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span v-if="payment">Are you sure you want to delete this payment?</span>
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deletePaymentDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="paymentStore.isProcessing" @click="deletePayment" severity="danger" outlined />
            </template>
        </Dialog>

        <Dialog v-model:visible="deletePaymentsDialog" :style="{ width: '450px' }" header="Confirm" :modal="true">
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span>Are you sure you want to delete the selected payments?</span>
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" text @click="deletePaymentsDialog = false" />
                <Button label="Yes" icon="pi pi-check" :loading="paymentStore.isProcessing" @click="deleteSelectedPayments" severity="danger" outlined />
            </template>
        </Dialog>
    </div>
</template>
