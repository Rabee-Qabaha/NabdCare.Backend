<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import { useUsersPaged } from '@/composables/query/users/useUsers';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import ProgressBar from 'primevue/progressbar';
  import { computed } from 'vue';

  const props = defineProps<{ subscription: SubscriptionResponseDto; clinicId?: string }>();

  const { data: usersData } = useUsersPaged({
    clinicId: computed(() => props.clinicId || null),
    limit: 1,
  });

  const calc = (used: number, base: number, purchased: number, bonus: number) => {
    const limit = base + purchased + bonus;
    const remaining = Math.max(0, limit - used);
    const percent = limit > 0 ? Math.min(100, Math.round((used / limit) * 100)) : 0;
    return { used, limit, remaining, percent, purchased, bonus };
  };

  const users = computed(() =>
    calc(
      usersData.value?.totalCount || 0,
      props.subscription.includedUsersSnapshot || 0,
      props.subscription.purchasedUsers || 0,
      props.subscription.bonusUsers || 0,
    ),
  );
</script>

<template>
  <BaseCard>
    <div>
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

      <div class="mt-2 mb-4">
        <div
          class="text-3xl font-bold text-surface-900 dark:text-surface-0 flex items-baseline gap-2"
        >
          {{ users.used }}
          <span class="text-xl text-surface-400 dark:text-surface-500 font-medium">
            / {{ users.limit }}
          </span>
        </div>
      </div>

      <ProgressBar
        :value="users.percent"
        :show-value="false"
        class="!h-2.5 !bg-surface-100 dark:!bg-surface-800 !rounded-full mb-3"
        :pt="{ value: { class: '!bg-primary-500 !rounded-full' } }"
      />
    </div>

    <div
      class="flex justify-between items-center text-xs text-surface-500 dark:text-surface-400 font-medium"
    >
      <span class="text-surface-500">{{ users.percent }}% Used</span>
      <span class="font-bold text-primary-600 dark:text-primary-400">
        {{ users.remaining }} left
      </span>
    </div>
  </BaseCard>
</template>
