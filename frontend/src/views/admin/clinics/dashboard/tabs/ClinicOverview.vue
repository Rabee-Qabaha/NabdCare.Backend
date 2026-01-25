<template>
  <div v-if="stats" class="space-y-6">
    <!-- 1. Overdue Invoice Alert -->
    <OverdueInvoiceAlert :has-overdue-invoices="stats.hasOverdueInvoices" />

    <!-- MAIN GRID -->
    <div class="grid grid-cols-1 xl:grid-cols-2 gap-6">
      <!-- LEFT COLUMN: Business Profile -->
      <BusinessIdentityCard :stats="stats" :clinic-id="clinicId" />

      <!-- RIGHT COLUMN: Operational Health -->
      <div class="flex flex-col gap-6 h-full">
        <!-- 1. Subscription Health -->
        <SubscriptionHealthCard
          :subscription-status="stats.subscriptionStatus"
          :subscription-plan="stats.subscriptionPlan"
          :subscription-expires-at="stats.subscriptionExpiresAt"
        />

        <!-- 2. KPI Grid -->
        <KpiStatsGrid :stats="stats" />

        <!-- 3. Resource Utilization -->
        <ResourceUtilizationCard
          :total-branches="stats.totalBranches"
          :active-users-count="stats.activeUsersCount"
        />

        <!-- 4. Admin Profile -->
        <AdministratorProfileCard
          :primary-admin-name="stats.primaryAdminName"
          :created-at="stats.createdAt"
          :last-login-at="stats.lastLoginAt"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import AdministratorProfileCard from '@/components/Subscription/profile/Overview/AdministratorProfileCard.vue';
  import BusinessIdentityCard from '@/components/Subscription/profile/Overview/BusinessIdentityCard.vue';
  import KpiStatsGrid from '@/components/Subscription/profile/Overview/KpiStatsGrid.vue';
  import OverdueInvoiceAlert from '@/components/Subscription/profile/Overview/OverdueInvoiceAlert.vue';
  import ResourceUtilizationCard from '@/components/Subscription/profile/Overview/ResourceUtilizationCard.vue';
  import SubscriptionHealthCard from '@/components/Subscription/profile/Overview/SubscriptionHealthCard.vue';
  import type { ClinicDashboardStatsDto } from '@/types/backend';

  // Define properties with potential growth rates added
  interface ExtendedStats extends ClinicDashboardStatsDto {
    staffGrowthRate?: number;
    patientGrowthRate?: number;
  }

  defineProps<{
    stats: ExtendedStats;
    clinicId: string;
  }>();
</script>
