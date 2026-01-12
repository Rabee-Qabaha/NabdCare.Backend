// Path: src/components/Subscription/forms/SubscriptionCreateForm.vue
<script setup lang="ts">
  import PlanSelect from '@/components/Dropdowns/PlanSelect.vue';
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import Button from 'primevue/button';
  import InputNumber from 'primevue/inputnumber';
  import { ref } from 'vue';

  const props = defineProps<{ clinicId: string }>();
  const emit = defineEmits(['cancel', 'success']);

  const form = ref({
    planId: '',
    extraUsers: 0,
    extraBranches: 0,
    bonusUsers: 0,
    bonusBranches: 0,
    currency: 'USD',
  });

  const showDealMode = ref(false);
  const { createSubscriptionMutation } = useSubscriptionActions();

  const handleSubmit = async () => {
    await createSubscriptionMutation.mutateAsync({
      clinicId: props.clinicId,
      ...form.value,
      autoRenew: true,
      customStartDate: new Date(),
    });
    emit('success');
  };
</script>

<template>
  <div
    class="animate-fade-in bg-surface-0 dark:bg-surface-800 rounded-xl p-8 text-center border border-surface-200 dark:border-surface-700 flex-grow flex flex-col items-center justify-center"
  >
    <div
      class="w-16 h-16 bg-primary-50 dark:bg-primary-900/20 rounded-full flex items-center justify-center mb-6 text-primary-600"
    >
      <i class="pi pi-star text-2xl"></i>
    </div>
    <h3 class="text-xl font-bold text-surface-900 dark:text-surface-0 mb-2">New Subscription</h3>
    <p class="text-surface-500 mb-8">Configure the base plan and any add-ons.</p>

    <div class="w-full max-w-md mx-auto space-y-6 text-left">
      <PlanSelect v-model="form.planId" required label="Select Base Plan" />

      <div v-if="form.planId">
        <div class="grid grid-cols-2 gap-4 mb-4">
          <div>
            <label class="text-xs font-bold mb-1 block text-surface-600">Paid Users</label>
            <InputNumber
              v-model="form.extraUsers"
              show-buttons
              :min="0"
              input-class="w-full text-center"
            />
          </div>
          <div>
            <label class="text-xs font-bold mb-1 block text-surface-600">Paid Branches</label>
            <InputNumber
              v-model="form.extraBranches"
              show-buttons
              :min="0"
              input-class="w-full text-center"
            />
          </div>
        </div>

        <div class="pt-4 border-t border-surface-100 dark:border-surface-700">
          <div
            class="flex items-center justify-between cursor-pointer group"
            @click="showDealMode = !showDealMode"
          >
            <span
              class="text-xs font-bold text-green-600 dark:text-green-400 flex items-center gap-1 group-hover:underline"
            >
              <i class="pi pi-gift"></i>
              Deal-Maker Bonuses
            </span>
            <i
              class="pi pi-chevron-down text-xs text-surface-400 transition-transform"
              :class="{ 'rotate-180': showDealMode }"
            ></i>
          </div>

          <div
            v-if="showDealMode"
            class="grid grid-cols-2 gap-4 mt-3 bg-green-50 dark:bg-green-900/10 p-3 rounded-lg border border-green-100 dark:border-green-800 animate-fade-in"
          >
            <div>
              <div class="flex justify-between mb-1">
                <label class="text-[10px] font-bold text-green-800 dark:text-green-300">
                  Free Users
                </label>
              </div>
              <InputNumber
                v-model="form.bonusUsers"
                placeholder="0"
                class="w-full"
                input-class="text-center font-bold bg-white/60 dark:bg-black/20"
                :min="0"
              />
            </div>
            <div>
              <div class="flex justify-between mb-1">
                <label class="text-[10px] font-bold text-green-800 dark:text-green-300">
                  Free Branches
                </label>
              </div>
              <InputNumber
                v-model="form.bonusBranches"
                placeholder="0"
                class="w-full"
                input-class="text-center font-bold bg-white/60 dark:bg-black/20"
                :min="0"
              />
            </div>
          </div>
        </div>
      </div>

      <div class="flex gap-3 mt-6">
        <Button label="Cancel" text class="flex-1" @click="emit('cancel')" />
        <Button
          label="Activate Subscription"
          class="flex-1"
          :loading="createSubscriptionMutation.isPending.value"
          :disabled="!form.planId"
          @click="handleSubmit"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
  .animate-fade-in {
    animation: fadeIn 0.2s ease-in-out;
  }
  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(-5px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
</style>
