<template>
  <Dialog
    v-model:visible="isVisible"
    modal
    :dismissableMask="!saving"
    :style="{ width: '800px', maxWidth: '95vw' }"
    :pt="{
      root: {
        class:
          'rounded-xl border border-surface-200 dark:border-surface-700 shadow-2xl overflow-hidden bg-surface-0 dark:bg-surface-900',
      },
      header: {
        class:
          'border-b border-surface-200 dark:border-surface-700 py-4 px-6 bg-surface-50 dark:bg-surface-800',
      },
      content: { class: 'p-0 h-full' },
      footer: {
        class:
          'border-t border-surface-200 dark:border-surface-700 py-4 px-6 bg-surface-50 dark:bg-surface-800',
      },
    }"
    @hide="onClose"
  >
    <template #header>
      <div class="flex items-center gap-4">
        <div
          class="flex items-center justify-center w-12 h-12 rounded-full bg-primary-100 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 shrink-0 border border-primary-200 dark:border-primary-700/50"
        >
          <i :class="['pi text-xl', isEditMode ? 'pi-pencil' : 'pi-building']"></i>
        </div>
        <div class="flex flex-col">
          <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-tight">
            {{ isEditMode ? 'Edit Clinic Profile' : 'Register New Clinic' }}
          </h3>
          <p class="text-sm text-surface-500 dark:text-surface-400 mt-0.5">
            Identity and Subscription Management.
          </p>
        </div>
      </div>
    </template>

    <div class="flex flex-col max-h-[65vh] overflow-hidden">
      <Tabs v-model:value="activeTab">
        <div
          class="px-6 py-3 bg-surface-0 dark:bg-surface-900 border-b border-surface-200 dark:border-surface-700"
        >
          <TabList class="flex gap-2 border-none bg-transparent">
            <Tab value="0" class="tab-item">
              <div class="flex items-center gap-4 py-1">
                <i class="pi pi-id-card text-lg shrink-0"></i>
                <span class="font-semibold text-sm">General</span>
                <i
                  v-if="hasGeneralErrors"
                  class="pi pi-exclamation-circle text-red-500 text-[10px] -ml-2"
                ></i>
              </div>
            </Tab>
            <Tab value="1" class="tab-item">
              <div class="flex items-center gap-4 py-1">
                <i class="pi pi-palette text-lg shrink-0"></i>
                <span class="font-semibold text-sm">Branding</span>
                <i
                  v-if="hasBrandingErrors"
                  class="pi pi-exclamation-circle text-red-500 text-[10px] -ml-2"
                ></i>
              </div>
            </Tab>
            <Tab value="2" class="tab-item">
              <div class="flex items-center gap-4 py-1">
                <i class="pi pi-star-fill text-lg shrink-0"></i>
                <span class="font-semibold text-sm">Subscription</span>
                <i
                  v-if="hasSubscriptionErrors"
                  class="pi pi-exclamation-circle text-red-500 text-[10px] -ml-2"
                ></i>
              </div>
            </Tab>
          </TabList>
        </div>

        <TabPanels
          class="!p-0 overflow-y-auto h-full bg-surface-0 dark:bg-surface-900 custom-scrollbar"
        >
          <TabPanel value="0" class="p-6">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div class="flex flex-col gap-2">
                <label class="text-sm font-medium">
                  Clinic Name
                  <span class="text-red-500">*</span>
                </label>
                <InputText
                  v-model="name"
                  :invalid="!!errors.name"
                  placeholder="e.g. Nabd Medical"
                  class="w-full"
                />
                <small v-if="errors.name" class="text-red-500 text-xs">{{ errors.name }}</small>
              </div>

              <div class="flex flex-col gap-2">
                <label class="text-sm font-medium">
                  Subdomain
                  <span class="text-red-500">*</span>
                </label>
                <InputGroup>
                  <InputGroupAddon><i class="pi pi-globe"></i></InputGroupAddon>
                  <InputText
                    v-model="slug"
                    :invalid="!!errors.slug"
                    placeholder="slug"
                    @input="onSlugInput"
                  />
                  <InputGroupAddon class="text-xs">.nabd.care</InputGroupAddon>
                </InputGroup>
                <small v-if="errors.slug" class="text-red-500 text-xs">{{ errors.slug }}</small>
              </div>

              <div class="flex flex-col gap-2">
                <label class="text-sm font-medium">
                  Email
                  <span class="text-red-500">*</span>
                </label>
                <InputText v-model="email" :invalid="!!errors.email" type="email" class="w-full" />
                <small v-if="errors.email" class="text-red-500 text-xs">{{ errors.email }}</small>
              </div>

              <div class="flex flex-col gap-2">
                <label class="text-sm font-medium">
                  Phone
                  <span class="text-red-500">*</span>
                </label>
                <InputText v-model="phone" :invalid="!!errors.phone" class="w-full" />
                <small v-if="errors.phone" class="text-red-500 text-xs">{{ errors.phone }}</small>
              </div>

              <div
                class="md:col-span-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 pt-4 border-t border-surface-100 dark:border-surface-700"
              >
                <div class="md:col-span-2 flex flex-col gap-2">
                  <label class="text-xs font-bold text-surface-500 uppercase">
                    Street
                    <span class="text-red-500">*</span>
                  </label>
                  <InputText v-model="splitStreet" :invalid="!!errors.splitStreet" class="w-full" />
                </div>
                <div class="flex flex-col gap-2">
                  <label class="text-xs font-bold text-surface-500 uppercase">
                    City
                    <span class="text-red-500">*</span>
                  </label>
                  <InputText v-model="splitCity" :invalid="!!errors.splitCity" class="w-full" />
                </div>
                <div class="flex flex-col gap-2">
                  <label class="text-xs font-bold text-surface-500 uppercase">Postal Code</label>
                  <InputText v-model="splitPostalCode" class="w-full" />
                </div>
                <div class="flex flex-col gap-2 md:col-span-2 lg:col-span-4">
                  <label class="text-xs font-bold text-surface-500 uppercase">
                    Country
                    <span class="text-red-500">*</span>
                  </label>
                  <CountrySelect v-model="splitCountry" :invalid="!!errors.splitCountry" />
                </div>
              </div>
            </div>
          </TabPanel>

          <TabPanel value="1" class="p-6">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div
                class="md:col-span-2 flex items-start gap-6 p-4 border border-dashed border-surface-300 rounded-xl bg-surface-50 dark:bg-surface-800/50"
              >
                <div
                  class="w-20 h-20 rounded-full bg-white dark:bg-surface-700 shadow-sm border flex items-center justify-center overflow-hidden shrink-0"
                >
                  <img v-if="logoUrl" :src="logoUrl" class="w-full h-full object-cover" />
                  <i v-else class="pi pi-image text-3xl text-surface-400"></i>
                </div>
                <div class="flex-grow flex flex-col gap-2">
                  <label class="text-sm font-medium">Logo URL</label>
                  <InputText v-model="logoUrl" :invalid="!!errors.logoUrl" class="w-full" />
                  <small v-if="errors.logoUrl" class="text-red-500 text-xs">
                    {{ errors.logoUrl }}
                  </small>
                </div>
              </div>
              <div class="flex flex-col gap-2">
                <label class="text-sm font-medium">Website</label>
                <InputText v-model="website" :invalid="!!errors.website" class="w-full" />
              </div>
              <div class="flex flex-col gap-2">
                <label class="text-sm font-medium">Currency</label>
                <InputText v-model="currency" :invalid="!!errors.currency" class="w-full" />
              </div>
              <div class="flex flex-col gap-2">
                <label class="text-sm font-medium">Timezone</label>
                <Select v-model="timeZone" :options="['Asia/Hebron', 'UTC']" class="w-full" />
              </div>
            </div>
          </TabPanel>

          <TabPanel value="2" class="p-6">
            <div class="space-y-6 max-w-xl mx-auto">
              <div
                v-if="isEditMode"
                class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 p-4 rounded-lg flex gap-3"
              >
                <i class="pi pi-info-circle text-blue-600 mt-0.5"></i>
                <span class="text-sm text-blue-800">
                  Subscription is locked in edit mode. Use Subscription Manager for changes.
                </span>
              </div>

              <div class="flex flex-col gap-3" :class="{ 'opacity-60': isEditMode }">
                <h4 class="text-sm font-bold flex items-center gap-2">
                  <i class="pi pi-star text-primary-500"></i>
                  1. Select Base Plan
                </h4>
                <PlanSelect
                  v-model="selectedPlanId"
                  :disabled="isEditMode"
                  :invalid="!!errors.selectedPlanId"
                  @plan-selected="onPlanChanged"
                />
                <small v-if="errors.selectedPlanId" class="text-red-500 text-xs">
                  {{ errors.selectedPlanId }}
                </small>
              </div>

              <div v-if="!isEditMode && selectedPlanId" class="space-y-4 animate-fade-in">
                <h4 class="text-sm font-bold flex items-center gap-2">
                  <i class="pi pi-shopping-cart text-purple-500"></i>
                  2. Paid Add-ons
                </h4>
                <div class="grid grid-cols-2 gap-4">
                  <div class="flex flex-col gap-1">
                    <label class="text-xs font-bold text-surface-600">Extra Users</label>
                    <InputNumber
                      v-model="extraUsers"
                      showButtons
                      :min="0"
                      inputClass="text-center"
                    />
                  </div>
                  <div class="flex flex-col gap-1">
                    <label class="text-xs font-bold text-surface-600">Extra Branches</label>
                    <InputNumber
                      v-model="extraBranches"
                      showButtons
                      :min="0"
                      inputClass="text-center"
                    />
                  </div>
                </div>

                <div class="pt-4 border-t border-surface-100">
                  <div
                    class="flex items-center justify-between cursor-pointer"
                    @click="showDealMode = !showDealMode"
                  >
                    <span class="text-xs font-bold text-green-600 flex items-center gap-1">
                      <i class="pi pi-gift"></i>
                      Deal-Maker Bonuses
                    </span>
                    <i
                      class="pi pi-chevron-down text-xs transition-transform"
                      :class="{ 'rotate-180': showDealMode }"
                    ></i>
                  </div>
                  <div
                    v-if="showDealMode"
                    class="grid grid-cols-2 gap-4 mt-3 bg-green-50 dark:bg-green-900/10 p-3 rounded-lg border border-green-100"
                  >
                    <div>
                      <label class="text-[10px] font-bold text-green-800 block mb-1">
                        Free Users
                      </label>
                      <InputNumber
                        v-model="bonusUsers"
                        class="w-full"
                        inputClass="text-center"
                        :min="0"
                      />
                    </div>
                    <div>
                      <label class="text-[10px] font-bold text-green-800 block mb-1">
                        Free Branches
                      </label>
                      <InputNumber
                        v-model="bonusBranches"
                        class="w-full"
                        inputClass="text-center"
                        :min="0"
                      />
                    </div>
                  </div>
                </div>

                <div
                  class="bg-surface-50 dark:bg-surface-800 p-4 rounded-xl border border-surface-200"
                >
                  <div class="flex justify-between items-center mb-2">
                    <span class="text-xs text-surface-500 uppercase font-bold">
                      Total Initial Fee
                    </span>
                    <span class="text-lg font-bold text-primary-600">
                      {{ formatMoney(computedFee) }}
                    </span>
                  </div>
                  <div class="flex justify-between items-center text-xs text-surface-500">
                    <span>Start Date</span>
                    <DatePicker
                      v-model="subscriptionStartDate"
                      showIcon
                      fluid
                      dateFormat="dd/mm/yy"
                      class="w-32 scale-90"
                    />
                  </div>
                </div>
              </div>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 w-full mt-2">
        <Button label="Cancel" severity="secondary" text @click="onClose" :disabled="saving" />
        <Button
          :label="isEditMode ? 'Save Changes' : 'Register Clinic'"
          icon="pi pi-check"
          @click="onSave"
          :loading="saving"
        />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import { useClinicForm } from '@/composables/clinic/useClinicForm';
  import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
  import type { ClinicResponseDto } from '@/types/backend';
  import { formatClinicCurrency } from '@/utils/uiHelpers';
  import { computed, ref, watch } from 'vue';

  import CountrySelect from '@/components/Dropdowns/CountrySelect.vue';
  import PlanSelect from '@/components/Dropdowns/PlanSelect.vue';
  import Button from 'primevue/button';
  import DatePicker from 'primevue/datepicker';
  import Dialog from 'primevue/dialog';
  import InputGroup from 'primevue/inputgroup';
  import InputGroupAddon from 'primevue/inputgroupaddon';
  import InputNumber from 'primevue/inputnumber';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import Tab from 'primevue/tab';
  import TabList from 'primevue/tablist';
  import TabPanel from 'primevue/tabpanel';
  import TabPanels from 'primevue/tabpanels';
  import Tabs from 'primevue/tabs';

  const props = defineProps<{ visible: boolean; clinic?: ClinicResponseDto | null }>();
  const emit = defineEmits(['update:visible', 'save']);

  const { getValidationErrors, handleErrorAndNotify } = useErrorHandler();
  const isVisible = computed({ get: () => props.visible, set: (v) => emit('update:visible', v) });
  const isEditMode = computed(() => !!props.clinic?.id);

  const localClinic = ref<Partial<ClinicResponseDto>>({});
  const saving = ref(false);
  const activeTab = ref('0');
  const errors = ref<Record<string, string>>({});
  const showDealMode = ref(false);

  const splitStreet = ref('');
  const splitCity = ref('');
  const splitPostalCode = ref('');
  const splitCountry = ref('');

  const {
    name,
    slug,
    email,
    phone,
    address,
    website,
    logoUrl,
    timeZone,
    currency,
    initForm,
    getFormData,
    subscriptionStartDate,
    selectedPlanId,
    selectedPlan,
    extraUsers,
    extraBranches,
    bonusUsers,
    bonusBranches,
    computedFee,
    computedEndDate,
  } = useClinicForm(localClinic);

  const formatMoney = (val: number) =>
    formatClinicCurrency(val, { currency: currency.value, locale: 'en-US' } as any);
  const onPlanChanged = (plan: any) => {
    selectedPlan.value = plan;
  };
  const onSlugInput = (e: any) => {
    slug.value = e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '');
  };

  const hasGeneralErrors = computed(
    () =>
      !!(
        errors.value.name ||
        errors.value.slug ||
        errors.value.email ||
        errors.value.phone ||
        errors.value.splitStreet ||
        errors.value.splitCity ||
        errors.value.splitCountry
      ),
  );
  const hasBrandingErrors = computed(
    () => !!(errors.value.website || errors.value.logoUrl || errors.value.currency),
  );
  const hasSubscriptionErrors = computed(() => !isEditMode.value && !!errors.value.selectedPlanId);

  const autoSwitchTab = () => {
    if (hasGeneralErrors.value) activeTab.value = '0';
    else if (hasBrandingErrors.value) activeTab.value = '1';
    else if (hasSubscriptionErrors.value) activeTab.value = '2';
  };

  async function onSave() {
    errors.value = {};
    let clientErrors = false;

    if (!name.value?.trim()) {
      errors.value.name = 'Required';
      clientErrors = true;
    }
    if (!slug.value?.trim()) {
      errors.value.slug = 'Required';
      clientErrors = true;
    }
    if (!email.value?.trim()) {
      errors.value.email = 'Required';
      clientErrors = true;
    }
    if (!phone.value?.trim()) {
      errors.value.phone = 'Required';
      clientErrors = true;
    }
    if (!splitStreet.value?.trim()) {
      errors.value.splitStreet = 'Required';
      clientErrors = true;
    }
    if (!splitCity.value?.trim()) {
      errors.value.splitCity = 'Required';
      clientErrors = true;
    }
    if (!splitCountry.value?.trim()) {
      errors.value.splitCountry = 'Required';
      clientErrors = true;
    }
    if (!isEditMode.value && !selectedPlanId.value) {
      errors.value.selectedPlanId = 'Plan required';
      clientErrors = true;
    }

    if (clientErrors) {
      autoSwitchTab();
      return;
    }

    saving.value = true;
    address.value = [splitStreet.value, splitCity.value, splitPostalCode.value, splitCountry.value]
      .filter(Boolean)
      .join(', ');

    try {
      await emit('save', { id: props.clinic?.id, ...getFormData() });
    } catch (err: any) {
      errors.value = getValidationErrors(err);
      autoSwitchTab();
      handleErrorAndNotify(err);
    } finally {
      saving.value = false;
    }
  }

  function onClose() {
    if (!saving.value) isVisible.value = false;
  }

  watch(
    () => props.visible,
    (val) => {
      if (val) {
        initForm(props.clinic);
        if (props.clinic) {
          const parts = (address.value || '').split(',').map((p) => p.trim());
          if (parts.length > 0) {
            if (parts.length >= 1) splitCountry.value = parts.pop() || '';
            if (parts.length >= 1) {
              const part = parts.pop() || '';
              if (/^[\d\w\s-]{3,10}$/.test(part) && parts.length > 0) {
                splitPostalCode.value = part;
                if (parts.length >= 1) splitCity.value = parts.pop() || '';
              } else {
                splitCity.value = part;
              }
            }
            if (parts.length >= 1) splitStreet.value = parts.join(', ');
          }
        }
      } else {
        initForm(null);
        errors.value = {};
        showDealMode.value = false;
        splitStreet.value = '';
        splitCity.value = '';
        splitPostalCode.value = '';
        splitCountry.value = '';
        activeTab.value = '0';
      }
    },
  );
</script>

<style scoped>
  .modern-tablist {
    display: flex;
    gap: 8px;
    border: none !important;
  }
  .tab-item {
    border: none !important;
    background: transparent !important;
    padding: 10px 16px !important;
    border-radius: 8px !important;
    color: #64748b !important;
    font-weight: 600;
    cursor: pointer;
    position: relative;
  }
  .tab-item.p-tab-active {
    background: rgba(var(--primary-500-rgb), 0.1) !important;
    color: var(--primary-600) !important;
  }
  .animate-fade-in {
    animation: fadeIn 0.3s ease-out;
  }
  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(8px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
</style>
