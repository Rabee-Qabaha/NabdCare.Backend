<template>
  <BaseDrawer
    :visible="visible"
    :loading="saving"
    width="md:!w-[650px]"
    :no-padding="true"
    :dismissable="false"
    @update:visible="$emit('update:visible', $event)"
    @save="onSave"
    @cancel="onClose"
  >
    <template #header>
      <div class="flex items-center gap-4">
        <div
          class="flex items-center justify-center w-12 h-12 rounded-xl shrink-0 text-white shadow-md transition-all duration-300 ring-2 ring-white dark:ring-surface-800 ring-offset-2 ring-offset-surface-100 dark:ring-offset-surface-900 bg-primary-500"
        >
          <i :class="[isEditMode ? 'pi pi-pencil' : 'pi pi-building', 'text-xl']"></i>
        </div>
        <div class="flex flex-col gap-1">
          <h3 class="text-xl font-bold text-surface-900 dark:text-surface-0 leading-none">
            {{ isEditMode ? 'Clinic Settings' : 'Register Clinic' }}
          </h3>
          <p class="text-sm text-surface-500 dark:text-surface-400 leading-snug">
            {{
              isEditMode
                ? 'Manage clinic details and configuration.'
                : 'Create a new clinic workspace.'
            }}
          </p>
        </div>
      </div>
    </template>

    <div class="p-6 flex flex-col gap-6 h-full custom-scrollbar">
      <!-- 1. Identity Section -->
      <div class="flex flex-col gap-4">
        <h3
          class="flex items-center gap-2 text-xs font-bold text-surface-500 uppercase tracking-wider"
        >
          <i class="pi pi-id-card text-sm"></i>
          Identity
        </h3>

        <!-- Row 1: Name & Subdomain -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-xs font-semibold">
              Clinic Name
              <span class="text-red-500">*</span>
            </label>
            <InputText
              v-model="name"
              :invalid="submitted && !!errors.name"
              placeholder="e.g. Nabd Medical Center"
              class="w-full"
              @input="errors.name = ''"
            />
            <small v-if="submitted && errors.name" class="text-red-500 text-xs">
              {{ errors.name }}
            </small>
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-xs font-semibold">
              Subdomain
              <span class="text-red-500">*</span>
            </label>
            <InputGroup>
              <InputGroupAddon><i class="pi pi-globe"></i></InputGroupAddon>
              <InputText
                v-model="slug"
                :invalid="submitted && !!errors.slug"
                placeholder="clinic-name"
                @input="onSlugInputChange"
              />
              <InputGroupAddon class="text-xs">.nabd.care</InputGroupAddon>
            </InputGroup>
            <small v-if="submitted && errors.slug" class="text-red-500 text-xs">
              {{ errors.slug }}
            </small>
          </div>
        </div>

        <!-- Row 2: Visual Identity (Logo) -->
        <div class="flex flex-col gap-2">
          <label class="text-xs font-semibold">Brand Identity</label>
          <div
            class="flex items-center gap-4 p-3 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-800/50"
          >
            <!-- Logo Preview -->
            <div
              class="w-16 h-16 rounded-lg bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 flex items-center justify-center overflow-hidden shrink-0 shadow-sm"
            >
              <img v-if="logoUrl" :src="logoUrl" class="w-full h-full object-cover" />
              <i v-else class="pi pi-image text-2xl text-surface-300"></i>
            </div>

            <!-- Input Area -->
            <div class="flex-grow flex flex-col gap-1.5">
              <label class="text-[10px] font-bold uppercase text-surface-500">Logo Image URL</label>
              <InputText
                v-model="logoUrl"
                placeholder="https://example.com/logo.png"
                class="w-full p-inputtext-sm"
              />
              <p class="text-[10px] text-surface-400">
                Provide a direct link to your clinic's logo (PNG or JPG recommended).
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- 2. Contact Information -->
      <div class="flex flex-col gap-4">
        <h3
          class="flex items-center gap-2 text-xs font-bold text-surface-500 uppercase tracking-wider"
        >
          <i class="pi pi-address-book text-sm"></i>
          Contact Info
        </h3>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-xs font-semibold">
              Official Email
              <span class="text-red-500">*</span>
            </label>
            <InputText
              v-model="email"
              type="email"
              :invalid="submitted && !!errors.email"
              class="w-full"
            />
            <small v-if="submitted && errors.email" class="text-red-500 text-xs">
              {{ errors.email }}
            </small>
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-xs font-semibold">
              Phone
              <span class="text-red-500">*</span>
            </label>
            <InputText v-model="phone" :invalid="submitted && !!errors.phone" class="w-full" />
            <small v-if="submitted && errors.phone" class="text-red-500 text-xs">
              {{ errors.phone }}
            </small>
          </div>
        </div>

        <!-- Address Grid -->
        <div
          class="p-3 rounded-lg bg-surface-50 dark:bg-surface-800/50 border border-surface-200 dark:border-surface-700 flex flex-col gap-3"
        >
          <div class="grid grid-cols-3 gap-3">
            <div class="col-span-3">
              <label class="text-[10px] font-bold uppercase text-surface-500">
                Street / Address
                <span class="text-red-500">*</span>
              </label>
              <InputText
                v-model="splitStreet"
                :invalid="submitted && !!errors.street"
                placeholder="123 Main St"
                class="w-full p-inputtext-sm mt-1"
              />
            </div>
            <div class="col-span-2">
              <label class="text-[10px] font-bold uppercase text-surface-500">
                City
                <span class="text-red-500">*</span>
              </label>
              <InputText
                v-model="splitCity"
                :invalid="submitted && !!errors.city"
                placeholder="City"
                class="w-full p-inputtext-sm mt-1"
              />
            </div>
            <div>
              <label class="text-[10px] font-bold uppercase text-surface-500">
                Zip Code
                <span class="text-red-500">*</span>
              </label>
              <InputText
                v-model="splitPostalCode"
                :invalid="submitted && !!errors.zip"
                placeholder="00000"
                class="w-full p-inputtext-sm mt-1"
              />
            </div>
            <div class="col-span-3">
              <label class="text-[10px] font-bold uppercase text-surface-500">
                Country
                <span class="text-red-500">*</span>
              </label>
              <CountrySelect
                v-model="splitCountry"
                :invalid="submitted && !!errors.country"
                class="w-full mt-1"
                placeholder="Select Country"
                append-to="self"
              />
              <small v-if="submitted && errors.address" class="text-red-500 text-xs">
                {{ errors.address }}
              </small>
            </div>
          </div>
        </div>
      </div>

      <!-- 3. Branding & Legal (New Fields) -->
      <div class="flex flex-col gap-4">
        <h3
          class="flex items-center gap-2 text-xs font-bold text-surface-500 uppercase tracking-wider"
        >
          <i class="pi pi-briefcase text-sm"></i>
          Legal & Branding
        </h3>

        <div class="flex flex-col gap-1">
          <label class="text-xs font-semibold">Website</label>
          <InputGroup>
            <InputGroupAddon><i class="pi pi-link"></i></InputGroupAddon>
            <InputText v-model="website" placeholder="www.clinic.com" />
          </InputGroup>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-xs font-semibold">Tax Number</label>
            <InputText v-model="taxNumber" placeholder="TAX-123456" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-xs font-semibold">Registration No.</label>
            <InputText v-model="registrationNumber" placeholder="REG-987654" />
          </div>
        </div>
      </div>

      <!-- 4. System Configuration -->
      <div class="flex flex-col gap-4">
        <h3
          class="flex items-center gap-2 text-xs font-bold text-surface-500 uppercase tracking-wider"
        >
          <i class="pi pi-cog text-sm"></i>
          System Configuration
        </h3>

        <!-- Patient Portal Card -->
        <div
          class="flex items-center justify-between p-4 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900 shadow-sm"
        >
          <div class="flex items-center gap-3">
            <div
              class="w-10 h-10 rounded-full bg-primary-50 dark:bg-primary-900/40 text-primary-600 dark:text-primary-400 flex items-center justify-center shrink-0"
            >
              <i class="pi pi-user text-lg"></i>
            </div>
            <div class="flex flex-col gap-0.5">
              <span class="text-sm font-semibold text-surface-900 dark:text-surface-0">
                Patient Portal
              </span>
              <span class="text-xs text-surface-500 dark:text-surface-400">
                Allow patients to access their records online.
              </span>
            </div>
          </div>
          <ToggleSwitch v-model="enablePatientPortal" />
        </div>

        <!-- Regional Settings Grid -->
        <div class="grid grid-cols-2 gap-4">
          <!-- Timezone -->
          <div class="flex flex-col gap-1.5">
            <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
              <i class="pi pi-clock mr-1 text-surface-400"></i>
              Timezone
            </label>
            <Select
              v-model="timeZone"
              :options="TIMEZONE_OPTIONS"
              class="w-full"
              append-to="self"
            />
          </div>

          <!-- Currency -->
          <div class="flex flex-col gap-1.5">
            <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
              <i class="pi pi-money-bill mr-1 text-surface-400"></i>
              Currency
            </label>
            <Select
              v-model="currency"
              :options="CURRENCY_OPTIONS"
              option-label="label"
              option-value="value"
              class="w-full"
              append-to="self"
            />
          </div>

          <!-- Exchange Rate Markup -->
          <div class="col-span-2 border-t border-surface-100 dark:border-surface-700 pt-4 mt-2">
            <label class="text-xs font-bold text-surface-500 uppercase mb-3 block">
              Financial Configuration
            </label>
            <div class="grid grid-cols-2 gap-4">
              <div class="flex flex-col gap-1.5">
                <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
                  Exchange Rate Markup
                </label>
                <Select
                  v-model="exchangeRateMarkupType"
                  :options="MARKUP_TYPE_OPTIONS"
                  option-label="label"
                  option-value="value"
                  class="w-full"
                  append-to="self"
                />
              </div>
              <div class="flex flex-col gap-1.5">
                <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
                  Markup Value
                </label>
                <InputGroup>
                  <InputNumber
                    v-model="exchangeRateMarkupValue"
                    :disabled="exchangeRateMarkupType === MarkupType.None"
                    :min="0"
                    :max="100"
                    class="w-full"
                    placeholder="0"
                  />
                  <InputGroupAddon v-if="exchangeRateMarkupType === MarkupType.Percentage">
                    %
                  </InputGroupAddon>
                </InputGroup>
              </div>
            </div>
          </div>

          <!-- Date Format -->
          <div class="flex flex-col gap-1.5">
            <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
              <i class="pi pi-calendar mr-1 text-surface-400"></i>
              Date Format
            </label>
            <Select
              v-model="dateFormat"
              :options="DATE_FORMAT_OPTIONS"
              class="w-full"
              append-to="self"
            />
          </div>

          <!-- Locale -->
          <div class="flex flex-col gap-1.5">
            <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
              <i class="pi pi-globe mr-1 text-surface-400"></i>
              Locale
            </label>
            <Select
              v-model="locale"
              :options="LOCALE_OPTIONS"
              option-label="label"
              option-value="value"
              class="w-full"
              append-to="self"
            />
          </div>
        </div>
      </div>
    </div>

    <template #footer="{ close }">
      <div class="flex gap-3 w-full">
        <Button
          label="Cancel"
          severity="secondary"
          outlined
          class="!w-[30%]"
          :disabled="saving"
          @click="close"
        />
        <Button
          :label="isEditMode ? 'Save Changes' : 'Create Clinic'"
          :loading="saving"
          icon="pi pi-check"
          class="flex-1"
          @click="onSave"
        />
      </div>
    </template>
  </BaseDrawer>
</template>

<script setup lang="ts">
  import { computed, ref, watch } from 'vue';

  // Components
  import CountrySelect from '@/components/Dropdowns/CountrySelect.vue';
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import InputGroup from 'primevue/inputgroup';
  import InputGroupAddon from 'primevue/inputgroupaddon';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import ToggleSwitch from 'primevue/toggleswitch';

  // Composables & Types
  import { useClinicForm } from '@/composables/clinic/useClinicForm';
  import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
  import { useClinicActions } from '@/composables/query/clinics/useClinicActions';
  import { useToastService } from '@/composables/useToastService';
  import type { ClinicResponseDto } from '@/types/backend';
  import { MarkupType } from '@/types/backend/markup-type';
  import {
    CURRENCY_OPTIONS,
    DATE_FORMAT_OPTIONS,
    LOCALE_OPTIONS,
    TIMEZONE_OPTIONS,
  } from '@/utils/constants';
  import { getFieldErrors } from '@/utils/errorHandler';
  import InputNumber from 'primevue/inputnumber';

  const MARKUP_TYPE_OPTIONS = [
    { label: 'None', value: MarkupType.None },
    { label: 'Percentage', value: MarkupType.Percentage },
  ];

  const props = defineProps<{ visible: boolean; clinic?: ClinicResponseDto | null }>();
  const emit = defineEmits(['update:visible', 'saved']);

  const toast = useToastService();
  const { handleErrorAndNotify } = useErrorHandler();
  const { createClinicMutation, updateClinicMutation } = useClinicActions();

  const isEditMode = computed(() => !!props.clinic?.id);
  const saving = ref(false);
  const submitted = ref(false);
  const errors = ref<Record<string, string>>({});
  const localClinic = ref<Partial<ClinicResponseDto>>({});

  // Address split
  const splitStreet = ref('');
  const splitCity = ref('');
  const splitPostalCode = ref('');
  const splitCountry = ref<string | null>(null);

  const {
    name,
    slug,
    email,
    phone,
    address,
    website,
    logoUrl,
    taxNumber,
    registrationNumber,
    timeZone,
    currency,
    exchangeRateMarkupType,
    exchangeRateMarkupValue,
    dateFormat,
    locale,
    enablePatientPortal,
    initForm,
    getFormData,
    validate,
  } = useClinicForm();

  const onSlugInputChange = (e: any) => {
    slug.value = e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '');
    if (errors.value.slug) errors.value.slug = '';
  };

  async function onSave() {
    submitted.value = true;
    errors.value = {};

    // Reconstruct Address
    const countryName = splitCountry.value || '';
    address.value = [splitStreet.value, splitCity.value, splitPostalCode.value, countryName]
      .filter((p) => p && p.trim() !== '')
      .join(', ');

    // Validate split parts manually
    const addressErrors: Record<string, string> = {};
    if (!splitStreet.value?.trim()) addressErrors.street = 'Street is required';
    if (!splitCity.value?.trim()) addressErrors.city = 'City is required';
    if (!splitPostalCode.value?.trim()) addressErrors.zip = 'Zip Code is required';
    if (!splitCountry.value) addressErrors.country = 'Country is required';

    const validation = validate();
    if (!validation.isValid || Object.keys(addressErrors).length > 0) {
      errors.value = { ...validation.errors, ...addressErrors };
      toast.error('Please fix the highlighted errors.');
      return;
    }

    saving.value = true;
    try {
      const payload = getFormData();

      if (isEditMode.value && props.clinic?.id) {
        await updateClinicMutation.mutateAsync({ id: props.clinic.id, data: payload });
        toast.success('Clinic updated successfully');
      } else {
        await createClinicMutation.mutateAsync(payload);
        toast.success('Clinic created successfully');
      }
      emit('saved');
      onClose();
    } catch (err: any) {
      const serverErrors = getFieldErrors(err);
      if (Object.keys(serverErrors).length > 0) errors.value = serverErrors;
      else handleErrorAndNotify(err);
    } finally {
      saving.value = false;
    }
  }

  function onClose() {
    if (!saving.value) emit('update:visible', false);
  }

  watch(
    () => props.visible,
    (val) => {
      if (val) {
        submitted.value = false;
        initForm(props.clinic);

        // Parse Address
        if (props.clinic?.address) {
          const parts = props.clinic.address.split(',').map((p) => p.trim());
          if (parts.length >= 1) splitCountry.value = parts.pop() || '';
          if (parts.length >= 1) splitPostalCode.value = parts.pop() || '';
          if (parts.length >= 1) splitCity.value = parts.pop() || '';
          if (parts.length >= 1) splitStreet.value = parts.join(', ');
        } else {
          splitStreet.value = '';
          splitCity.value = '';
          splitPostalCode.value = '';
          splitCountry.value = '';
        }
      } else {
        initForm(null);
        errors.value = {};
      }
    },
  );
</script>
