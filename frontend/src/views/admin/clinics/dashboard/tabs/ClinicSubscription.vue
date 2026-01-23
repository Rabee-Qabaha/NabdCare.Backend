<script setup lang="ts">
  import { computed, ref } from 'vue';

  // PrimeVue Imports
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import DataTable from 'primevue/datatable';
  import Divider from 'primevue/divider';
  import Tag from 'primevue/tag';
  import ToggleSwitch from 'primevue/toggleswitch';

  // ========================================================================
  // 🧩 TYPES & INTERFACES
  // ========================================================================

  const SubscriptionStatus = {
    0: 'Trialing',
    1: 'Active',
    2: 'PastDue',
    3: 'Canceled',
    4: 'Expired',
  } as const;

  interface SubscriptionUiModel {
    id: string;
    planId: string;
    planName: string;
    status: 0 | 1 | 2 | 3 | 4;
    type: 0 | 1; // 0=Monthly, 1=Yearly
    currency: string;
    fee: number;
    startDate: string;
    endDate: string;
    autoRenew: boolean;
    maxBranches: number;
    purchasedBranches: number;
    includedBranchesSnapshot: number;
    bonusBranches: number;
    currentBranchCount: number;
    maxUsers: number;
    purchasedUsers: number;
    includedUsersSnapshot: number;
    bonusUsers: number;
    currentUserCount: number;
    latestInvoiceNumber: string;
    latestInvoiceStatus: 'Paid' | 'Unpaid' | 'Void';
  }

  // ========================================================================
  // 🛠️ STATE & DUMMY DATA
  // ========================================================================

  const activeSub = ref<SubscriptionUiModel>({
    id: 'sub_1NlK2X2eZvKYlo2CLWjO',
    planId: 'STD_M',
    planName: 'Standard Monthly',
    status: 1,
    type: 0, // Monthly for this example
    currency: 'USD',
    fee: 1200.0,
    startDate: '2023-09-24T00:00:00Z',
    endDate: '2023-10-24T00:00:00Z',
    autoRenew: true,
    includedBranchesSnapshot: 1,
    purchasedBranches: 4,
    bonusBranches: 0,
    maxBranches: 5,
    currentBranchCount: 2,
    includedUsersSnapshot: 2,
    purchasedUsers: 8,
    bonusUsers: 0,
    maxUsers: 10,
    currentUserCount: 6,
    latestInvoiceNumber: 'INV-2023-009',
    latestInvoiceStatus: 'Unpaid', // Trigger the red banner
  });

  const history = ref([
    { id: '#INV-2023-009', billingPeriod: 'Sep 24 - Oct 23, 2023', amount: 1200.0, status: 'Paid' },
    { id: '#INV-2023-008', billingPeriod: 'Aug 24 - Sep 23, 2023', amount: 1050.0, status: 'Paid' },
    { id: '#INV-2023-007', billingPeriod: 'Jul 24 - Aug 23, 2023', amount: 1050.0, status: 'Paid' },
  ]);

  // ========================================================================
  // 🧮 COMPUTED LOGIC
  // ========================================================================

  const daysRemaining = computed(() => {
    const end = new Date(activeSub.value.endDate).getTime();
    const now = new Date().getTime();
    return 12;
  });

  const daysElapsed = computed(() => 18);
  const totalDaysInCycle = computed(() => 30);
  const progressPercent = computed(() => (daysElapsed.value / totalDaysInCycle.value) * 100);

  const breakdown = computed(() => {
    const s = activeSub.value;
    const userPrice = 100;
    const branchPrice = 150;
    const usersCost = s.purchasedUsers * userPrice;
    const branchesCost = s.purchasedBranches * branchPrice;
    const baseCost = s.fee - usersCost - branchesCost;

    return { baseCost, usersCost, userPrice, branchesCost, branchPrice };
  });

  const formatMoney = (val: number) =>
    new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: activeSub.value.currency,
    }).format(val);

  const formatDate = (dateStr: string) =>
    new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
</script>

<template>
  <div
    class="w-full max-w-[1600px] mx-auto p-6 md:p-8 space-y-6 bg-surface-50 dark:bg-transparent rounded-2xl font-sans"
  >
    <div
      class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-5 border border-transparent dark:border-surface-700 shadow dark:shadow-sm flex flex-col md:flex-row justify-between items-center gap-4 transition-colors duration-300"
    >
      <div class="flex items-center gap-4 w-full md:w-auto">
        <div
          class="w-12 h-12 rounded-lg bg-primary-50 dark:bg-primary-900/20 text-primary-600 flex items-center justify-center shadow-sm"
        >
          <i class="pi pi-star-fill text-xl"></i>
        </div>
        <div>
          <div class="flex items-center gap-3">
            <h2 class="text-xl font-bold text-surface-900 dark:text-surface-0">
              {{ activeSub.planName }}
            </h2>
            <Tag
              value="ACTIVE"
              severity="success"
              class="!px-2 !py-0.5 !text-[10px] !font-bold"
              rounded
            />
          </div>
          <div
            class="flex items-center gap-2 text-surface-500 dark:text-surface-400 text-sm font-medium mt-1"
          >
            <i class="pi pi-calendar"></i>
            <span>
              Renewal in
              <span class="text-surface-900 dark:text-surface-0 font-bold">
                {{ daysRemaining }} Days
              </span>
            </span>
          </div>
        </div>
      </div>

      <div class="flex flex-col sm:flex-row items-center gap-3 w-full md:w-auto">
        <div
          v-if="activeSub.latestInvoiceStatus === 'Unpaid'"
          class="flex items-center gap-2 px-4 py-2.5 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-lg border border-red-100 dark:border-red-800 text-sm font-bold w-full sm:w-auto justify-center"
        >
          <i class="pi pi-exclamation-triangle"></i>
          <span>Payment Action Required</span>
        </div>

        <Button
          label="Manage Subscription"
          icon="pi pi-cog"
          class="!bg-emerald-500 !border-emerald-500 hover:!bg-emerald-600 !rounded-lg !font-bold w-full sm:w-auto"
        />
      </div>
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
      <div class="xl:col-span-2 flex flex-col gap-6">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div
            class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-6 border border-transparent dark:border-surface-700 shadow dark:shadow-sm transition-colors duration-300"
          >
            <div class="flex justify-between items-start mb-4">
              <div class="flex items-center gap-3">
                <div
                  class="w-10 h-10 rounded-lg bg-orange-50 dark:bg-orange-900/20 text-orange-500 flex items-center justify-center"
                >
                  <i class="pi pi-building text-lg"></i>
                </div>
                <span class="font-bold text-surface-900 dark:text-surface-0 text-lg">Branches</span>
              </div>
              <span
                class="text-xs font-medium text-surface-400 dark:text-surface-500 uppercase tracking-wide"
              >
                Capacity
              </span>
            </div>

            <div class="mt-2">
              <div
                class="text-4xl font-bold text-surface-900 dark:text-surface-0 flex items-baseline gap-2"
              >
                {{ activeSub.currentBranchCount }}
                <span class="text-xl text-surface-400 dark:text-surface-500 font-medium">
                  / {{ activeSub.maxBranches }}
                </span>
              </div>
              <p class="text-sm text-surface-500 dark:text-surface-400 mt-2 font-medium">
                Includes {{ activeSub.includedBranchesSnapshot }} base +
                {{ activeSub.purchasedBranches }} add-ons
              </p>
            </div>
          </div>

          <div
            class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-6 border border-transparent dark:border-surface-700 shadow dark:shadow-sm transition-colors duration-300"
          >
            <div class="flex justify-between items-start mb-4">
              <div class="flex items-center gap-3">
                <div
                  class="w-10 h-10 rounded-lg bg-primary-50 dark:bg-primary-900/20 text-primary-500 flex items-center justify-center"
                >
                  <i class="pi pi-users text-lg"></i>
                </div>
                <span class="font-bold text-surface-900 dark:text-surface-0 text-lg">Users</span>
              </div>
              <span
                class="text-xs font-medium text-surface-400 dark:text-surface-500 uppercase tracking-wide"
              >
                Capacity
              </span>
            </div>

            <div class="mt-2">
              <div
                class="text-4xl font-bold text-surface-900 dark:text-surface-0 flex items-baseline gap-2"
              >
                {{ activeSub.currentUserCount }}
                <span class="text-xl text-surface-400 dark:text-surface-500 font-medium">
                  / {{ activeSub.maxUsers }}
                </span>
              </div>
              <p class="text-sm text-surface-500 dark:text-surface-400 mt-2 font-medium">
                Includes {{ activeSub.includedUsersSnapshot }} base +
                {{ activeSub.purchasedUsers }} add-ons
              </p>
            </div>
          </div>
        </div>

        <div
          class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-6 border border-transparent dark:border-surface-700 shadow dark:shadow-sm transition-colors duration-300"
        >
          <div class="flex items-center gap-3 mb-6">
            <div
              class="w-8 h-8 rounded-lg bg-primary-50 dark:bg-primary-900/20 text-primary-600 flex items-center justify-center"
            >
              <i class="pi pi-sync"></i>
            </div>
            <h3 class="font-bold text-lg text-surface-900 dark:text-surface-0">
              Billing Cycle & Timeline
            </h3>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-6 mb-8">
            <div>
              <div
                class="text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider mb-1"
              >
                Start Date
              </div>
              <div class="font-bold text-surface-900 dark:text-surface-0">
                {{ formatDate(activeSub.startDate) }}
              </div>
            </div>
            <div>
              <div
                class="text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider mb-1"
              >
                Next Billing Date
              </div>
              <div class="font-bold text-primary-600 dark:text-primary-400">
                {{ formatDate(activeSub.endDate) }}
              </div>
            </div>
            <div>
              <div
                class="text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider mb-1"
              >
                Auto-Renew
              </div>
              <div class="flex items-center gap-2">
                <span class="font-bold text-surface-900 dark:text-surface-0">Enabled</span>
                <ToggleSwitch v-model="activeSub.autoRenew" class="scale-75" />
              </div>
            </div>
          </div>

          <div>
            <div class="flex justify-between mb-2 text-sm font-medium">
              <span class="text-surface-500 dark:text-surface-400">Cycle Progress</span>
              <span class="text-surface-900 dark:text-surface-0 font-bold">
                {{ daysElapsed }} / {{ totalDaysInCycle }} Days Elapsed
              </span>
            </div>

            <div
              class="relative h-3 bg-surface-100 dark:bg-surface-950 rounded-full w-full overflow-hidden"
            >
              <div
                class="absolute top-0 left-0 h-full bg-primary-500 rounded-full"
                :style="{ width: `${progressPercent}%` }"
              ></div>
            </div>

            <div
              class="flex justify-between mt-2 text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider"
            >
              <span>{{ formatDate(activeSub.startDate).split(',')[0] }}</span>
              <span class="text-primary-600 dark:text-primary-400">
                {{ daysRemaining }} Days Remaining
              </span>
              <span>{{ formatDate(activeSub.endDate).split(',')[0] }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="xl:col-span-1 h-full">
        <div
          class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-6 border border-transparent dark:border-surface-700 shadow dark:shadow-sm h-full flex flex-col transition-colors duration-300"
        >
          <h3
            class="text-xs font-bold text-surface-400 dark:text-surface-500 uppercase tracking-widest mb-2"
          >
            Subscription Breakdown
          </h3>

          <div class="flex items-start justify-between mb-8">
            <div>
              <div
                class="text-xl font-extrabold text-surface-900 dark:text-surface-0 leading-tight"
              >
                {{ activeSub.planName }}
              </div>
              <div class="text-sm text-surface-500 dark:text-surface-400 font-medium mt-1">
                Billed {{ activeSub.type === 1 ? 'Yearly' : 'Monthly' }}
              </div>
            </div>
            <div
              class="p-2 bg-surface-100 dark:bg-surface-800 rounded-lg text-surface-500 dark:text-surface-400"
            >
              <i class="pi pi-receipt text-xl"></i>
            </div>
          </div>

          <ul class="space-y-6 flex-1">
            <li class="flex justify-between items-start group">
              <div class="flex gap-3">
                <div
                  class="w-9 h-9 rounded-lg bg-surface-50 dark:bg-surface-800 flex items-center justify-center text-surface-500 shrink-0"
                >
                  <i class="pi pi-box"></i>
                </div>
                <div>
                  <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">Base Fee</div>
                  <div class="text-xs text-surface-500 mt-0.5">Plan core features</div>
                </div>
              </div>
              <div class="font-bold text-surface-900 dark:text-surface-0">
                {{ formatMoney(breakdown.baseCost) }}
              </div>
            </li>

            <li class="flex justify-between items-start group">
              <div class="flex gap-3">
                <div
                  class="w-9 h-9 rounded-lg bg-primary-50 dark:bg-primary-900/20 flex items-center justify-center text-primary-500 shrink-0"
                >
                  <i class="pi pi-users"></i>
                </div>
                <div>
                  <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">
                    Add-on Users
                  </div>
                  <div class="text-xs text-surface-500 mt-0.5">
                    {{ activeSub.purchasedUsers }} × {{ formatMoney(breakdown.userPrice) }}
                  </div>
                </div>
              </div>
              <div class="font-bold text-surface-900 dark:text-surface-0">
                {{ formatMoney(breakdown.usersCost) }}
              </div>
            </li>

            <li class="flex justify-between items-start group">
              <div class="flex gap-3">
                <div
                  class="w-9 h-9 rounded-lg bg-orange-50 dark:bg-orange-900/20 flex items-center justify-center text-orange-500 shrink-0"
                >
                  <i class="pi pi-building"></i>
                </div>
                <div>
                  <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">
                    Add-on Branches
                  </div>
                  <div class="text-xs text-surface-500 mt-0.5">
                    {{ activeSub.purchasedBranches }} × {{ formatMoney(breakdown.branchPrice) }}
                  </div>
                </div>
              </div>
              <div class="font-bold text-surface-900 dark:text-surface-0">
                {{ formatMoney(breakdown.branchesCost) }}
              </div>
            </li>
          </ul>

          <Divider class="my-6 border-dashed !border-surface-200 dark:!border-surface-700" />

          <div
            class="bg-primary-50 dark:bg-primary-900/10 rounded-xl p-5 border border-primary-100 dark:border-primary-800/30"
          >
            <div class="flex justify-between items-center mb-1">
              <span
                class="text-[10px] font-bold text-primary-600/70 dark:text-primary-400/70 uppercase tracking-wider"
              >
                Total Monthly Fee
              </span>
            </div>
            <div class="flex justify-between items-baseline">
              <span class="text-xs font-medium text-surface-500 dark:text-surface-400">
                Next bill {{ formatDate(activeSub.endDate) }}
              </span>
              <span class="text-2xl font-bold text-primary-600 dark:text-primary-400">
                {{ formatMoney(activeSub.fee) }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div>
      <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 mb-4 px-1">
        Billing History
      </h3>
      <div
        class="bg-surface-0 dark:bg-[#27272a] rounded-xl border border-transparent dark:border-surface-700 shadow dark:shadow-sm overflow-hidden transition-colors duration-300"
      >
        <DataTable :value="history" class="text-sm">
          <Column
            field="id"
            header="INVOICE ID"
            class="font-medium text-surface-900 dark:text-surface-0"
          ></Column>
          <Column
            field="billingPeriod"
            header="BILLING PERIOD"
            class="text-surface-500 dark:text-surface-400"
          ></Column>
          <Column field="amount" header="AMOUNT">
            <template #body="{ data }">
              <span class="font-bold text-primary-600 dark:text-primary-400">
                {{ formatMoney(data.amount) }}
              </span>
            </template>
          </Column>
          <Column field="status" header="STATUS">
            <template #body="{ data }">
              <Tag
                :value="data.status"
                severity="success"
                class="!uppercase !text-[10px] !px-2.5"
                rounded
              />
            </template>
          </Column>
          <Column header="ACTIONS" class="text-right">
            <template #body>
              <Button
                icon="pi pi-download"
                text
                rounded
                severity="secondary"
                class="!w-8 !h-8 !text-surface-400 hover:!text-surface-600 dark:hover:!text-surface-200"
              />
            </template>
          </Column>
        </DataTable>
      </div>
    </div>
  </div>
</template>

<style scoped>
  :deep(.p-datatable .p-datatable-thead > tr > th) {
    background: transparent;
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--surface-400);
    font-weight: 700;
    padding-top: 1.5rem;
    padding-bottom: 0.75rem;
  }
  :deep(.p-datatable .p-datatable-tbody > tr > td) {
    padding-top: 1.25rem;
    padding-bottom: 1.25rem;
    border-bottom: 1px solid var(--surface-100);
  }
  :global(.dark) :deep(.p-datatable .p-datatable-tbody > tr > td) {
    border-bottom: 1px solid var(--surface-800);
  }
  :deep(.p-datatable .p-datatable-tbody > tr:last-child > td) {
    border-bottom: none;
  }
</style>
