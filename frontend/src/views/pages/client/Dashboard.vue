<!-- <template>
  <div class="grid grid-cols-12 gap-8">
    <div class="col-span-12 md:col-span-6 xl:col-span-3">
      <StatusWidget
        header="Total Patients"
        :value="totalPatients"
        icon="pi pi-users"
        color="purple"
        :static="newPatientsThisMonth"
        staticText="this month"
      />
    </div>
    <div class="col-span-12 md:col-span-6 xl:col-span-3">
      <StatusWidget
        header="Total Visits"
        :value="totalVisits"
        icon="pi pi-calendar"
        color="blue"
        :static="visitsThisMonth"
        staticText="this month"
      />
    </div>
    <div class="col-span-12 md:col-span-6 xl:col-span-3">
      <StatusWidget
        header="Total Visit Fees"
        :value="formatCurrency(totalRevenue)"
        icon="pi pi-dollar"
        color="green"
        :static="formatCurrency(revenueThisMonth)"
        staticText="this month"
      />
    </div>
    <div class="col-span-12 md:col-span-6 xl:col-span-3">
      <StatusWidget
        header="Total Payments"
        :value="formatCurrency(totalPayments)"
        icon="pi pi-credit-card"
        color="cyan"
        :static="formatCurrency(paymentsThisMonth)"
        staticText="this month"
      />
    </div>

    <div class="col-span-12 xl:col-span-6 h-[28rem]">
      <LineChart
        title="Visit Fees vs Payments (Trend)"
        :rawData="visitFeesTrendData"
        :key="allVisits.length + allPayments.length"
      />
    </div>

    <div class="col-span-12 xl:col-span-6 h-[28rem]">
      <BarChartTowColumns
        title="Visits Comparison (Last 6 Months)"
        :rawData="visitsBarData"
        :key="allVisits.length"
      />
    </div>

    <div class="col-span-12 xl:col-span-4 h-[28rem]">
      <DoughnutChart
        title="Visit Type Distribution"
        :data="visitTypeChartData"
      />
    </div>
    <div class="col-span-12 xl:col-span-4 h-[28rem]">
      <PieChart
        title="Payment Method Distribution"
        :data="paymentMethodChartData"
        :showCurrency="true"
      />
    </div>
    <div class="col-span-12 xl:col-span-4 h-[28rem]">
      <PieChart title="Gender Distribution" :data="genderChartData" />
    </div>
  </div>
</template> -->

<!-- <script setup lang="ts">
import { computed, onMounted } from "vue";
import { startOfMonth, endOfMonth } from "date-fns";
import StatusWidget from "@/components/dashboard/StatusWidget.vue";
import BarChartTowColumns from "@/components/dashboard/BarChartTowColumns.vue";
import LineChart from "@/components/dashboard/LineChart.vue";
import PieChart from "@/components/dashboard/PieChart.vue";
import DoughnutChart from "@/components/dashboard/DoughnutChart.vue";

// import { usePatientStore } from "@/stores/patientStore";
// import { useVisitStore } from "@/stores/visitStore";
// import { usePaymentStore } from "@/stores/paymentStore";
import { formatCurrency } from "@/utils/uiHelpers";
// import { PAYMENT_METHODS, VISIT_TYPES } from '../../../shared/types';

const patientStore = usePatientStore();
const visitStore = useVisitStore();
const paymentStore = usePaymentStore();

// -------------------- Helper Functions --------------------
function sumVisitsByMonth(
  visits: typeof visitStore.allVisits,
  month: number,
  year: number
) {
  return visits
    .filter((v) => v.date.getMonth() === month && v.date.getFullYear() === year)
    .reduce((sum, v) => sum + (v.fee || 0), 0);
}

function sumPaymentsByMonth(
  payments: typeof paymentStore.paymentsAll,
  month: number,
  year: number
) {
  return payments
    .filter(
      (p) => p.paidAt.getMonth() === month && p.paidAt.getFullYear() === year
    )
    .reduce((sum, p) => sum + p.amount, 0);
}

// -------------------- Data Fetch --------------------
onMounted(async () => {
  if (!patientStore.patientList.length) await patientStore.fetchPatients();
  if (!visitStore.allVisits.length) await visitStore.fetchAllVisits();
  if (!paymentStore.paymentsAll.length) await paymentStore.fetchPayments();
});

// -------------------- Stats --------------------
const totalPatients = computed(() => patientStore.patientList.length);
const newPatientsThisMonth = computed(() => {
  const start = startOfMonth(new Date());
  const end = endOfMonth(new Date());
  const count = patientStore.patientList.filter(
    (p) => p.createdAt >= start && p.createdAt <= end
  ).length;
  return `${count} new`;
});

const allVisits = computed(() => visitStore.allVisits);
const totalVisits = computed(() => allVisits.value.length);
const visitsThisMonth = computed(() => {
  const start = startOfMonth(new Date());
  const end = endOfMonth(new Date());
  return (
    allVisits.value.filter((v) => v.date >= start && v.date <= end).length +
    " visits"
  );
});

const totalRevenue = computed(() =>
  allVisits.value.reduce((sum, p) => sum + (p.fee || 0), 0)
);
const revenueThisMonth = computed(() => {
  const start = startOfMonth(new Date());
  const end = endOfMonth(new Date());
  return allVisits.value
    .filter((v) => v.date >= start && v.date <= end)
    .reduce((sum, p) => sum + (p.fee || 0), 0);
});

const allPayments = computed(() => paymentStore.paymentsAll);
const totalPayments = computed(() =>
  allPayments.value.reduce((sum, p) => sum + p.amount, 0)
);
const paymentsThisMonth = computed(() => {
  const start = startOfMonth(new Date());
  const end = endOfMonth(new Date());
  return allPayments.value
    .filter((p) => p.paidAt >= start && p.paidAt <= end)
    .reduce((sum, p) => sum + p.amount, 0);
});

// -------------------- Pie Charts --------------------
const genderChartData = computed(() => {
  const male = patientStore.patientList.filter(
    (p) => p.gender === "Male"
  ).length;
  const female = patientStore.patientList.filter(
    (p) => p.gender === "Female"
  ).length;
  return {
    labels: ["Male", "Female"],
    datasets: [
      { data: [male, female], backgroundColor: ["#2196f3", "#e91e63"] },
    ],
  };
});

// const visitTypeChartData = computed(() => {
//     const counts = VISIT_TYPES.reduce((acc, t) => ({ ...acc, [t]: allVisits.value.filter((v) => v.visitType === t).length }), {} as Record<string, number>);
//     return { labels: VISIT_TYPES, datasets: [{ data: VISIT_TYPES.map((t) => counts[t]), backgroundColor: ['#60a5fa', '#facc15', '#34d399', '#f87171', , '#9ca3af'] }] };
// });

// const paymentMethodChartData = computed(() => {
//     const counts = PAYMENT_METHODS.reduce((acc, m) => ({ ...acc, [m]: allPayments.value.filter((p) => p.method === m).reduce((sum, p) => sum + p.amount, 0) }), {} as Record<string, number>);
//     return { labels: PAYMENT_METHODS, datasets: [{ data: PAYMENT_METHODS.map((m) => counts[m]), backgroundColor: ['#34d399', '#3B82F6', '#a78bfa', '#22d3ee', '#6B7280', '#facc15'] }] };
// });

// -------------------- Line Chart: Visit Fees vs Payments vs Outstanding --------------------
const visitFeesTrendData = computed(() => {
  if (!allVisits.value.length && !allPayments.value.length)
    return { labels: [], datasets: [] };

  const now = new Date();
  const currentMonth = now.getMonth();
  const half = currentMonth < 6 ? 0 : 6; // first or second half
  const months = Array.from({ length: 6 }, (_, i) => half + i);
  const monthLabels = months.map((m) =>
    new Date(now.getFullYear(), m, 1).toLocaleString("default", {
      month: "short",
    })
  );

  const visitsPerMonth = months.map((m) =>
    sumVisitsByMonth(allVisits.value, m, now.getFullYear())
  );
  const paymentsPerMonth = months.map((m) =>
    sumPaymentsByMonth(allPayments.value, m, now.getFullYear())
  );
  const outstandingPerMonth = visitsPerMonth.map((v, idx) =>
    Math.max(v - paymentsPerMonth[idx], 0)
  );

  return {
    labels: monthLabels,
    datasets: [
      { label: "Total Visit Fees", data: visitsPerMonth },
      { label: "Payments Received", data: paymentsPerMonth },
      { label: "Outstanding Amount", data: outstandingPerMonth },
    ],
  };
});

// -------------------- Bar Chart: Current vs Last Year Visits --------------------
const visitsBarData = computed(() => {
  const now = new Date();
  const months = Array.from(
    { length: 6 },
    (_, i) => new Date(now.getFullYear(), now.getMonth() - 5 + i, 1)
  );

  const monthLabels = months.map((d) =>
    d.toLocaleString("default", { month: "short" })
  );

  const currentYearCounts = months.map(
    (m) =>
      allVisits.value.filter(
        (v) =>
          v.date.getFullYear() === m.getFullYear() &&
          v.date.getMonth() === m.getMonth()
      ).length
  );

  const lastYearCounts = months.map(
    (m) =>
      allVisits.value.filter(
        (v) =>
          v.date.getFullYear() === m.getFullYear() - 1 &&
          v.date.getMonth() === m.getMonth()
      ).length
  );

  return {
    labels: monthLabels,
    datasets: [
      { label: "This Year", data: currentYearCounts },
      { label: "Last Year", data: lastYearCounts },
    ],
  };
});
</script> -->

<template>
  <div class="div">
    <h1>Client Dashboard</h1>
  </div>
</template>
