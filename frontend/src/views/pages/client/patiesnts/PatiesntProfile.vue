<template>
    <PatientProfileHeader v-if="patient" :patient="patient" :openEditDialog="openEditDialog" />

    <div class="grid grid-cols-12 gap-8">
        <!-- Left Column -->
        <div class="col-span-12 xl:col-span-3">
            <div class="col-span-3">
                <!-- Patient Details -->
                <PatientDetails v-if="patient" :patient="patient" header="Patient Info" />

                <!-- Treatment History -->
                <TreatmentHistory :visits="visits" header="Treatment Timeline" />
            </div>
        </div>

        <!-- Right Column -->
        <div class="col-span-12 xl:col-span-9">
            <!-- Stats -->
            <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-8 mb-6">
                <StatsWidget header="Visits" :value="totalVisits" icon="pi pi-calendar" color="blue" :static="visitsThisMonthLabel" staticText="this month" />
                <StatsWidget header="Balance" :value="balanceLabels.value" icon="pi pi-dollar" color="green" :static="balanceLabels.static" :staticText="balanceLabels.staticText" />
                <StatsWidget header="Upcoming Appointment" :value="nextAppointment" icon="pi pi-calendar-plus" color="purple" :static="upcomingStatic" :staticText="upcomingStaticText" />
            </div>

            <!-- Tabs -->
            <Tabs value="0" class="rounded-lg overflow-hidden pt-2">
                <TabList class="rounded-t-lg">
                    <Tab value="0" class="text-lg">Patient Visits</Tab>
                    <Tab value="1" class="text-lg">Patient Payments</Tab>
                    <Tab value="2" class="text-lg">Patient X-Rays</Tab>
                </TabList>

                <TabPanels>
                    <TabPanel value="0">
                        <VisitsList v-if="patient?.id" :patient-id="patient.id" />
                    </TabPanel>
                    <TabPanel value="1">
                        <PaymentsList :patientId="patient?.id" :showPatientInfo="false" />
                    </TabPanel>
                    <TabPanel value="2">
                        <XraysList v-if="patient?.id" :patient-id="patient.id" />
                    </TabPanel>
                </TabPanels>
            </Tabs>
        </div>
    </div>

    <!-- Edit Dialog -->
    <PatientDialog v-if="patient" v-model:visible="patientDialog" :patient="patient" @save="savePatient" @cancel="hideDialog" />
</template>

<script setup lang="ts">
    import { ref, onMounted, computed } from 'vue';
    import { startOfMonth, endOfMonth } from 'date-fns';
    import { useToast } from 'primevue/usetoast';

    import type { Patient } from '../../../../../shared/types';
    import StatsWidget from '@/components/dashboard/StatusWidget.vue';
    import PatientDetails from '@/components/Patiesnt/patientDetails.vue';
    import PatientProfileHeader from '@/components/Patiesnt/PatientProfileHeader.vue';
    import TreatmentHistory from '@/components/Patiesnt/TreatmentHistory.vue';
    import PatientDialog from '@/components/Patiesnt/PatientDialog.vue';
    import VisitsList from './VisitsList.vue';
    import PaymentsList from '../Payments/PaymentsList.vue';
    import XraysList from './XraysList.vue';

    import { usePatientStore } from '@/stores/patientStore';
    import { useVisitStore } from '@/stores/visitStore';
    import { usePaymentStore } from '@/stores/paymentStore';
    import { formatCurrency, formatDate } from '@/utils/uiHelpers';

    // Props
    const props = defineProps<{ id: string }>();

    // Stores
    const patientStore = usePatientStore();
    const visitStore = useVisitStore();
    const paymentStore = usePaymentStore();

    const toast = useToast();

    // Local state
    const patientDialog = ref(false);
    const submitted = ref(false);

    // --- Load data ---
    onMounted(async () => {
        try {
            await patientStore.fetchPatientById(props.id);
            await Promise.all([visitStore.fetchVisitsByPatientId(props.id), paymentStore.fetchPayments(props.id)]);
        } catch (err: any) {
            toast.add({ severity: 'error', summary: 'Error', detail: err.message ?? 'Failed to load patient data', life: 3000 });
        }
    });

    // --- Computed from stores ---
    const patient = computed(() => patientStore.getPatientById(props.id).value);
    const visits = computed(() => visitStore.visitsByPatient(props.id).value);
    const payments = computed(() => paymentStore.paymentsByPatient(props.id).value);

    // --- Stats ---
    const totalVisits = computed(() => visits.value.length);

    const visitsThisMonthCount = computed(() => visits.value.filter((v) => v.date >= startOfMonth(new Date()) && v.date <= endOfMonth(new Date())).length);
    const visitsThisMonthLabel = computed(() => `${visitsThisMonthCount.value} ${visitsThisMonthCount.value === 1 ? 'visit' : 'visits'}`);

    const totalFees = computed(() => visits.value.reduce((sum, v) => sum + v.fee, 0));
    const totalPayments = computed(() => payments.value.reduce((sum, p) => sum + p.amount, 0));
    const balance = computed(() => totalFees.value - totalPayments.value);

    function getBalanceLabels(balance: number) {
        if (balance > 0) {
            return { value: formatCurrency(balance), static: 'Due', staticText: 'Unpaid balance' };
        } else if (balance === 0) {
            return { value: formatCurrency(0), static: 'Settled', staticText: 'No outstanding balance' };
        } else {
            return { value: formatCurrency(Math.abs(balance)), static: 'Overpaid', staticText: 'Credit available' };
        }
    }
    const balanceLabels = computed(() => getBalanceLabels(balance.value));

    // Future visits / next appointment
    const futureVisits = computed(() => visits.value.filter((v) => v.nextAppointment && v.nextAppointment.getTime() > Date.now()));

    const nextAppointment = computed(() => {
        if (!futureVisits.value.length) return '—';

        const sorted = [...futureVisits.value].sort((a, b) => a.nextAppointment!.getTime() - b.nextAppointment!.getTime());

        return formatDate(sorted[0].nextAppointment!);
    });

    const upcomingVisitsCount = computed(() => futureVisits.value.length);
    const upcomingStatic = computed(() => `${upcomingVisitsCount.value} upcoming`);
    const upcomingStaticText = computed(() => (upcomingVisitsCount.value === 1 ? 'visit' : 'visits'));

    // --- Dialog handlers ---
    function openEditDialog() {
        submitted.value = false;
        patientDialog.value = true;
    }
    function hideDialog() {
        patientDialog.value = false;
        submitted.value = false;
    }
    function savePatient(updatedPatient: Patient) {
        patientStore.updatePatient(updatedPatient);
        patientDialog.value = false;
    }
</script>
