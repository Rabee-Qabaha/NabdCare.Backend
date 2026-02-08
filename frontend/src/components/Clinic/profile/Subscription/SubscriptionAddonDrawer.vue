<script setup lang="ts">
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import type {
    PlanDefinition,
    SubscriptionResponseDto,
    UpdateSubscriptionRequestDto,
  } from '@/types/backend';
  import { formatCurrency } from '@/utils/uiHelpers';
  import Button from 'primevue/button';
  import InputNumber from 'primevue/inputnumber';
  import Tag from 'primevue/tag';
  import { computed, ref, watch } from 'vue';

  const props = defineProps<{
    visible: boolean;
    subscription: SubscriptionResponseDto;
    planDefinition: PlanDefinition | null;
  }>();

  const emit = defineEmits(['update:visible', 'success']);

  const { updateMutation } = useSubscriptionActions();

  // Form State
  const form = ref({
    extraUsers: 0,
    extraBranches: 0,
  });

  // Reset form when opening
  watch(
    () => props.visible,
    (isOpen) => {
      if (isOpen) {
        form.value.extraUsers = props.subscription.purchasedUsers || 0;
        form.value.extraBranches = props.subscription.purchasedBranches || 0;
      }
    },
  );

  // -- Calculations --

  const currentTotalFee = computed(() => props.subscription.fee || 0);

  // Calculate individual differences
  const userDiff = computed(() => form.value.extraUsers - (props.subscription.purchasedUsers || 0));
  const branchDiff = computed(
    () => form.value.extraBranches - (props.subscription.purchasedBranches || 0),
  );

  const userCostDiff = computed(() => {
    if (!props.planDefinition) return 0;
    return userDiff.value * props.planDefinition.userPrice;
  });

  const branchCostDiff = computed(() => {
    if (!props.planDefinition) return 0;
    return branchDiff.value * props.planDefinition.branchPrice;
  });

  const newTotalFee = computed(() => {
    return currentTotalFee.value + userCostDiff.value + branchCostDiff.value;
  });

  const handleSubmit = async () => {
    const payload: UpdateSubscriptionRequestDto = {
      extraUsers: form.value.extraUsers,
      extraBranches: form.value.extraBranches,
      bonusUsers: (props.subscription as any).bonusUsers || 0,
      bonusBranches: (props.subscription as any).bonusBranches || 0,
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
      emit('update:visible', false);
    } catch (e) {
      console.error(e);
    }
  };
</script>

<template>
  <BaseDrawer
    :visible="visible"
    title="Manage Add-ons"
    subtitle="Scale your clinic by adding more seats and branch locations."
    icon="pi pi-chart-bar"
    width="md:!w-[500px]"
    @update:visible="(v) => emit('update:visible', v)"
  >
    <div class="space-y-8 pb-4">
      <div
        class="bg-surface-50 dark:bg-surface-900 rounded-xl p-5 border border-surface-200 dark:border-surface-800"
      >
        <div class="flex justify-between items-start mb-4">
          <div>
            <span class="text-[10px] font-bold text-surface-400 uppercase tracking-wider">
              Current Plan
            </span>
            <div class="text-lg font-bold text-surface-900 dark:text-surface-0 mt-1">
              {{ planDefinition?.name || subscription.planId }}
            </div>
            <div class="flex items-center gap-2 mt-1">
              <Tag
                :value="subscription.status"
                severity="success"
                class="!px-2 !py-0.5 !text-[10px] !font-bold uppercase"
              />
            </div>
          </div>
          <div class="text-right">
            <span class="text-[10px] font-bold text-surface-400 uppercase tracking-wider block">
              Current Fee
            </span>
            <span class="text-xl font-bold text-surface-900 dark:text-surface-0">
              {{ formatCurrency(currentTotalFee, subscription.currency) }}
            </span>
          </div>
        </div>
      </div>

      <div class="space-y-6">
        <h4
          class="text-xs font-bold text-surface-500 uppercase tracking-wider border-b border-surface-100 dark:border-surface-800 pb-2"
        >
          Adjust Capacity
        </h4>

        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div
              class="w-12 h-12 rounded-xl bg-purple-50 dark:bg-purple-900/20 flex items-center justify-center text-purple-600 dark:text-purple-400"
            >
              <i class="pi pi-users text-xl"></i>
            </div>
            <div>
              <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">Extra Users</div>
              <div class="text-[10px] text-surface-500 mt-1">
                {{ subscription.purchasedUsers }} Paid /
                {{ (subscription as any).bonusUsers || 0 }} Free
              </div>
            </div>
          </div>

          <div class="w-36">
            <InputNumber
              v-model="form.extraUsers"
              show-buttons
              button-layout="horizontal"
              :min="0"
              input-class="!w-12 !text-center !font-bold !text-surface-900 dark:!text-surface-0 !h-10 !border-0 !bg-transparent"
              class="!border-0 !bg-surface-100 dark:!bg-surface-800 !rounded-lg overflow-hidden flex items-center h-10"
              decrement-button-class="!bg-transparent !text-surface-500 !border-none !h-full !w-10 hover:!bg-surface-200 dark:hover:!bg-surface-700 flex items-center justify-center"
              increment-button-class="!bg-transparent !text-surface-500 !border-none !h-full !w-10 hover:!bg-surface-200 dark:hover:!bg-surface-700 flex items-center justify-center"
            >
              <template #incrementbuttonicon>
                <span class="pi pi-plus text-xs font-bold" />
              </template>
              <template #decrementbuttonicon>
                <span class="pi pi-minus text-xs font-bold" />
              </template>
            </InputNumber>
          </div>
        </div>

        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div
              class="w-12 h-12 rounded-xl bg-orange-50 dark:bg-orange-900/20 flex items-center justify-center text-orange-600 dark:text-orange-400"
            >
              <i class="pi pi-building text-xl"></i>
            </div>
            <div>
              <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">
                Extra Branches
              </div>
              <div class="text-[10px] text-surface-500 mt-1">
                {{ subscription.purchasedBranches }} Paid /
                {{ (subscription as any).bonusBranches || 0 }} Free
              </div>
            </div>
          </div>

          <div class="w-36">
            <InputNumber
              v-model="form.extraBranches"
              show-buttons
              button-layout="horizontal"
              :min="0"
              input-class="!w-12 !text-center !font-bold !text-surface-900 dark:!text-surface-0 !h-10 !border-0 !bg-transparent"
              class="!border-0 !bg-surface-100 dark:!bg-surface-800 !rounded-lg overflow-hidden flex items-center h-10"
              decrement-button-class="!bg-transparent !text-surface-500 !border-none !h-full !w-10 hover:!bg-surface-200 dark:hover:!bg-surface-700 flex items-center justify-center"
              increment-button-class="!bg-transparent !text-surface-500 !border-none !h-full !w-10 hover:!bg-surface-200 dark:hover:!bg-surface-700 flex items-center justify-center"
            >
              <template #incrementbuttonicon>
                <span class="pi pi-plus text-xs font-bold" />
              </template>
              <template #decrementbuttonicon>
                <span class="pi pi-minus text-xs font-bold" />
              </template>
            </InputNumber>
          </div>
        </div>
      </div>

      <div
        class="bg-surface-0 dark:bg-surface-900 border-2 border-dashed border-surface-200 dark:border-surface-700 rounded-xl p-5"
      >
        <div class="flex justify-between items-center mb-3 text-sm">
          <span class="text-surface-500">Current Total</span>
          <span class="font-bold text-surface-900 dark:text-surface-0">
            {{ formatCurrency(currentTotalFee, subscription.currency) }}
          </span>
        </div>

        <div v-if="userDiff !== 0" class="flex justify-between items-center mb-2 text-sm">
          <span class="text-surface-500">
            {{ userDiff > 0 ? 'Add-on' : 'Removed' }} ({{ Math.abs(userDiff) }}
            {{ Math.abs(userDiff) === 1 ? 'User' : 'Users' }})
          </span>
          <span :class="userDiff > 0 ? 'text-orange-500 font-bold' : 'text-green-500 font-bold'">
            {{ userDiff > 0 ? '+' : '' }}{{ formatCurrency(userCostDiff, subscription.currency) }}
          </span>
        </div>

        <div v-if="branchDiff !== 0" class="flex justify-between items-center mb-2 text-sm">
          <span class="text-surface-500">
            {{ branchDiff > 0 ? 'Add-on' : 'Removed' }} ({{ Math.abs(branchDiff) }}
            {{ Math.abs(branchDiff) === 1 ? 'Branch' : 'Branches' }})
          </span>
          <span :class="branchDiff > 0 ? 'text-orange-500 font-bold' : 'text-green-500 font-bold'">
            {{ branchDiff > 0 ? '+' : ''
            }}{{ formatCurrency(branchCostDiff, subscription.currency) }}
          </span>
        </div>

        <div
          v-if="userDiff !== 0 || branchDiff !== 0"
          class="h-px bg-surface-100 dark:bg-surface-800 w-full my-3"
        ></div>

        <div class="flex justify-between items-end">
          <span class="text-surface-900 dark:text-surface-0 font-bold text-lg">New Total Fee</span>
          <div class="text-right">
            <div class="text-2xl font-bold text-primary-600 dark:text-primary-400 leading-none">
              {{ formatCurrency(newTotalFee, subscription.currency) }}
            </div>
            <div class="text-[10px] text-surface-400 font-bold uppercase tracking-wider mt-1">
              {{ subscription.type === 'Yearly' ? 'PER YEAR' : 'PER MONTH' }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <template #footer="{ close }">
      <div class="w-full flex gap-4 pt-2 pb-4">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          class="!h-11 !w-[30%] !font-bold !border-surface-300 dark:!border-surface-600"
          @click="close"
        />
        <Button
          label="Confirm Update"
          icon="pi pi-check"
          class="!h-11 !w-[70%] !font-bold"
          :loading="updateMutation.isPending.value"
          @click="handleSubmit"
        />
      </div>
    </template>
  </BaseDrawer>
</template>
