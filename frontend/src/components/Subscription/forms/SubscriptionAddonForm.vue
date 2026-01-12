// Path: src/components/Subscription/forms/SubscriptionAddonForm.vue
<script setup lang="ts">
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import type { SubscriptionResponseDto, UpdateSubscriptionRequestDto } from '@/types/backend';
  import Button from 'primevue/button';
  import InputNumber from 'primevue/inputnumber';
  import Tag from 'primevue/tag';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    subscription: SubscriptionResponseDto;
  }>();

  const emit = defineEmits(['cancel', 'success']);

  // Destructure the mutation from the composable
  const { updateMutation } = useSubscriptionActions();

  // Initialize form
  const form = ref({
    // Map 'purchased' from the DB response to 'extra' for the form/DTO
    extraUsers: props.subscription.purchasedUsers || 0,
    extraBranches: props.subscription.purchasedBranches || 0,

    // Free (Bonus) - Default to 0 if undefined
    bonusUsers: (props.subscription as any).bonusUsers || 0,
    bonusBranches: (props.subscription as any).bonusBranches || 0,
  });

  const showBonusSection = ref(form.value.bonusUsers > 0 || form.value.bonusBranches > 0);

  const handleSubmit = async () => {
    const payload: UpdateSubscriptionRequestDto = {
      extraUsers: form.value.extraUsers,
      extraBranches: form.value.extraBranches,
      bonusUsers: form.value.bonusUsers,
      bonusBranches: form.value.bonusBranches,
      autoRenew: props.subscription.autoRenew,
      gracePeriodDays: props.subscription.gracePeriodDays,
      status: props.subscription.status,
      endDate: new Date(props.subscription.endDate),
      cancelAtPeriodEnd: props.subscription.cancelAtPeriodEnd,
    };

    try {
      await updateMutation.mutateAsync({
        id: props.subscription.id,
        data: payload,
      });
      emit('success');
    } catch (e) {
      // Error is handled by global handler in mutation
    }
  };

  const totalUsers = computed(
    () =>
      (props.subscription.includedUsersSnapshot || 0) +
      form.value.extraUsers +
      form.value.bonusUsers,
  );

  const totalBranches = computed(
    () =>
      (props.subscription.includedBranchesSnapshot || 0) +
      form.value.extraBranches +
      form.value.bonusBranches,
  );
</script>

<template>
  <div
    class="animate-fade-in bg-surface-0 dark:bg-surface-800 rounded-xl border border-surface-200 dark:border-surface-700 p-6 flex-grow flex flex-col"
  >
    <div class="flex justify-between items-center mb-6">
      <div>
        <h3 class="font-bold text-lg m-0 text-surface-900 dark:text-surface-0">Manage Add-ons</h3>
        <p class="text-sm text-surface-500 m-0">Adjust paid add-ons or grant free bonus units.</p>
      </div>
      <Button icon="pi pi-times" text rounded @click="emit('cancel')" />
    </div>

    <div class="flex-grow space-y-6">
      <div
        class="bg-surface-50 dark:bg-surface-900 p-5 rounded-xl border border-surface-200 dark:border-surface-700"
      >
        <h4
          class="text-xs font-bold text-surface-500 uppercase tracking-wider mb-4 flex items-center gap-2"
        >
          <i class="pi pi-credit-card"></i>
          Billable Upgrades
        </h4>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium mb-1">Extra Users</label>
            <InputNumber
              v-model="form.extraUsers"
              show-buttons
              button-layout="horizontal"
              :min="0"
              input-class="w-full text-center font-bold"
              class="w-full"
            >
              <template #incrementbuttonicon><span class="pi pi-plus" /></template>
              <template #decrementbuttonicon><span class="pi pi-minus" /></template>
            </InputNumber>
          </div>
          <div>
            <label class="block text-sm font-medium mb-1">Extra Branches</label>
            <InputNumber
              v-model="form.extraBranches"
              show-buttons
              button-layout="horizontal"
              :min="0"
              input-class="w-full text-center font-bold"
              class="w-full"
            >
              <template #incrementbuttonicon><span class="pi pi-plus" /></template>
              <template #decrementbuttonicon><span class="pi pi-minus" /></template>
            </InputNumber>
          </div>
        </div>
      </div>

      <div v-if="!showBonusSection" class="text-center">
        <Button
          label="Add Deal-Maker Bonuses (Free)"
          icon="pi pi-gift"
          text
          size="small"
          class="text-green-600 hover:bg-green-50 dark:text-green-400 dark:hover:bg-green-900/20"
          @click="showBonusSection = true"
        />
      </div>

      <div
        v-if="showBonusSection"
        class="animate-fade-in bg-green-50 dark:bg-green-900/10 p-5 rounded-xl border border-green-200 dark:border-green-800"
      >
        <div class="flex justify-between items-center mb-4">
          <h4
            class="text-xs font-bold text-green-700 dark:text-green-400 uppercase tracking-wider flex items-center gap-2"
          >
            <i class="pi pi-gift"></i>
            Free Bonus Units
          </h4>
          <Button
            icon="pi pi-times"
            text
            rounded
            size="small"
            class="h-6 w-6 text-green-700"
            @click="showBonusSection = false"
          />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <div class="flex justify-between mb-1">
              <label class="text-sm font-medium text-green-800 dark:text-green-200">
                Bonus Users
              </label>
              <Tag value="FREE" severity="success" class="text-[10px] px-1 py-0" />
            </div>
            <InputNumber
              v-model="form.bonusUsers"
              show-buttons
              button-layout="horizontal"
              :min="0"
              input-class="w-full text-center font-bold bg-white/50 dark:bg-surface-900/50"
              class="w-full"
            >
              <template #incrementbuttonicon><span class="pi pi-plus text-green-600" /></template>
              <template #decrementbuttonicon><span class="pi pi-minus text-green-600" /></template>
            </InputNumber>
            <div class="text-[10px] text-green-700 dark:text-green-300 mt-1 text-center">
              Currently granting:
              <strong>{{ form.bonusUsers }}</strong>
              users
            </div>
          </div>

          <div>
            <div class="flex justify-between mb-1">
              <label class="text-sm font-medium text-green-800 dark:text-green-200">
                Bonus Branches
              </label>
              <Tag value="FREE" severity="success" class="text-[10px] px-1 py-0" />
            </div>
            <InputNumber
              v-model="form.bonusBranches"
              show-buttons
              button-layout="horizontal"
              :min="0"
              input-class="w-full text-center font-bold bg-white/50 dark:bg-surface-900/50"
              class="w-full"
            >
              <template #incrementbuttonicon><span class="pi pi-plus text-green-600" /></template>
              <template #decrementbuttonicon><span class="pi pi-minus text-green-600" /></template>
            </InputNumber>
            <div class="text-[10px] text-green-700 dark:text-green-300 mt-1 text-center">
              Currently granting:
              <strong>{{ form.bonusBranches }}</strong>
              branches
            </div>
          </div>
        </div>
      </div>

      <div class="flex items-center justify-between text-xs text-surface-500 px-2">
        <span>New Total Capacity:</span>
        <div class="space-x-3 font-bold text-surface-900 dark:text-surface-0">
          <span>
            <i class="pi pi-users mr-1"></i>
            {{ totalUsers }} Users
          </span>
          <span>
            <i class="pi pi-building mr-1"></i>
            {{ totalBranches }} Branches
          </span>
        </div>
      </div>
    </div>

    <div class="flex justify-end gap-3 pt-4 border-t border-surface-100 dark:border-surface-700">
      <Button label="Cancel" text @click="emit('cancel')" />
      <Button
        label="Save Changes"
        icon="pi pi-check"
        :loading="updateMutation.isPending.value"
        @click="handleSubmit"
      />
    </div>
  </div>
</template>

<style scoped>
  .animate-fade-in {
    animation: fadeIn 0.3s ease-in-out;
  }
  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(5px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  :deep(.p-inputnumber-input) {
    @apply border-surface-300 focus:ring-green-500 dark:border-surface-600;
  }
</style>
