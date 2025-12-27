// src/components/Subscription/SubscriptionDialog.vue
<script setup lang="ts">
  import { useActiveSubscription } from '@/composables/query/subscriptions/useSubscriptions';
  import type { ClinicResponseDto } from '@/types/backend';
  import { SubscriptionStatus } from '@/types/backend';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';
  import { computed, ref, watch } from 'vue';
  import { useRouter } from 'vue-router';

  // Child Components
  import CurrentPlanCard from '@/components/Subscription/CurrentPlanCard.vue';
  import SubscriptionAddonForm from '@/components/Subscription/forms/SubscriptionAddonForm.vue';
  import SubscriptionCreateForm from '@/components/Subscription/forms/SubscriptionCreateForm.vue';
  import InvoiceHistory from '@/components/Subscription/InvoiceHistory.vue';
  import ResourceUtilizationCard from '@/components/Subscription/ResourceUtilizationCard.vue';
  import SubscriptionActions from '@/components/Subscription/SubscriptionActions.vue';

  const props = defineProps<{
    visible: boolean;
    clinic: ClinicResponseDto | null;
  }>();

  const emit = defineEmits(['update:visible']);
  const router = useRouter();

  // -- State --
  const viewState = ref<'dashboard' | 'edit' | 'create'>('dashboard');

  // -- Data Fetching --
  const {
    data: activeSubData,
    isLoading: isLoadingActive,
    refetch: refreshActive,
  } = useActiveSubscription(computed(() => props.clinic?.id || ''));

  // -- Computed --
  const activeSub = computed(() => {
    const raw = activeSubData.value;
    if (!raw) return null;
    return (raw as any).subscription || raw;
  });

  const hasActive = computed(() => {
    if (!activeSub.value) return false;
    const s = activeSub.value.status;
    return s === SubscriptionStatus.Active || s === SubscriptionStatus.Trial || s === 0;
  });

  // -- Actions --
  const handleOpenCreate = () => {
    viewState.value = 'create';
  };
  const handleOpenEdit = () => {
    viewState.value = 'edit';
  };
  const handleBackToDashboard = () => {
    viewState.value = 'dashboard';
    refreshActive();
  };

  const openFullInvoicePage = () => {
    if (!props.clinic?.id) return;
    const routeData = router.resolve({
      name: 'invoices',
      query: { clinicId: props.clinic.id },
    });
    window.open(routeData.href, '_blank');
  };

  watch(
    () => props.visible,
    (val) => {
      if (val) viewState.value = 'dashboard';
    },
  );
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    modal
    header=" "
    :style="{ width: '100%', maxWidth: '64rem' }"
    :breakpoints="{ '960px': '90vw', '640px': '95vw' }"
    :contentStyle="{
      padding: '0',
      borderRadius: '0 0 12px 12px',
      maxHeight: '85vh',
      overflowY: 'auto',
    }"
    :pt="{
      header: {
        class:
          'bg-surface-0 dark:bg-surface-900 border-b border-surface-200 dark:border-surface-700 px-4 md:px-6 py-4 rounded-t-xl',
      },
      mask: { class: 'backdrop-blur-sm' },
    }"
  >
    <template #header>
      <div class="flex flex-col sm:flex-row sm:items-center justify-between w-full gap-3">
        <div class="flex items-center gap-3">
          <div
            class="w-10 h-10 rounded-lg bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 flex items-center justify-center text-lg font-bold border border-primary-100 dark:border-primary-800 shrink-0"
          >
            {{ clinic?.name?.charAt(0).toUpperCase() }}
          </div>
          <div class="overflow-hidden">
            <h2
              class="text-lg font-bold text-surface-900 dark:text-surface-0 m-0 leading-none truncate"
            >
              {{ clinic?.name }}
            </h2>
            <span class="text-sm text-surface-500 block truncate">
              {{ clinic?.slug }}.nabd.care
            </span>
          </div>
        </div>
        <div class="flex gap-2 items-center self-start sm:self-auto">
          <template v-if="!isLoadingActive">
            <Tag
              v-if="hasActive"
              severity="success"
              value="Active"
              icon="pi pi-check-circle"
              rounded
            />
            <Tag v-else severity="danger" value="No Plan" icon="pi pi-exclamation-circle" rounded />
          </template>
          <Skeleton v-else width="100px" height="28px" borderRadius="16px" />
        </div>
      </div>
    </template>

    <div
      class="bg-surface-50 dark:bg-surface-900 min-h-[400px] md:min-h-[550px] p-4 md:p-6 flex flex-col"
    >
      <div v-if="isLoadingActive" class="flex flex-col gap-6 flex-grow justify-center items-center">
        <i class="pi pi-spin pi-spinner text-4xl text-primary-500"></i>
        <p class="text-surface-500">Loading subscription details...</p>
      </div>

      <div
        v-else-if="viewState === 'dashboard'"
        class="animate-fade-in flex flex-col gap-6 flex-grow"
      >
        <div v-if="hasActive && activeSub" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <CurrentPlanCard :subscription="activeSub" />
          <ResourceUtilizationCard :subscription="activeSub" :clinic-id="clinic?.id" />
        </div>

        <div v-if="hasActive && activeSub">
          <SubscriptionActions
            :subscription="activeSub"
            @edit="handleOpenEdit"
            @renew="refreshActive"
          />
        </div>

        <div v-if="clinic?.id && hasActive" class="mt-2">
          <InvoiceHistory :clinic-id="clinic.id" @view-all="openFullInvoicePage" />
        </div>

        <div
          v-if="!hasActive"
          class="text-center py-10 md:py-20 flex-grow flex flex-col justify-center items-center"
        >
          <div
            class="w-16 h-16 md:w-20 md:h-20 bg-surface-200 dark:bg-surface-800 rounded-full flex items-center justify-center mb-4"
          >
            <i class="pi pi-box text-2xl md:text-3xl text-surface-500"></i>
          </div>
          <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0">No Active Plan</h3>
          <p class="text-surface-500 mb-6 px-4">
            This clinic does not have an active subscription.
          </p>
          <Button label="Subscribe Now" icon="pi pi-sparkles" @click="handleOpenCreate" />
        </div>
      </div>

      <SubscriptionCreateForm
        v-if="viewState === 'create' && clinic"
        :clinic-id="clinic.id"
        @cancel="handleBackToDashboard"
        @success="handleBackToDashboard"
      />

      <SubscriptionAddonForm
        v-if="viewState === 'edit' && activeSub"
        :subscription="activeSub"
        @cancel="handleBackToDashboard"
        @success="handleBackToDashboard"
      />
    </div>
  </Dialog>
</template>

<style scoped>
  .animate-fade-in {
    animation: fadeIn 0.3s ease-in-out;
  }
  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(10px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
</style>
