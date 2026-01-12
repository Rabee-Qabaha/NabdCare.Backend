<template>
  <Dialog
    v-model:visible="isVisible"
    modal
    :dismissableMask="!saving"
    :style="{ width: '750px', maxWidth: '95vw' }"
    :pt="{
      root: {
        class:
          'rounded-xl border border-surface-200 dark:border-surface-700 shadow-2xl overflow-hidden bg-surface-0 dark:bg-surface-900',
      },
      header: {
        class:
          'border-b border-surface-200 dark:border-surface-700 py-4 px-6 bg-surface-50 dark:bg-surface-800',
      },
      content: { class: 'p-6 bg-surface-0 dark:bg-surface-900' },
      footer: {
        class:
          'border-t border-surface-200 dark:border-surface-700 py-4 px-6 bg-surface-50 dark:bg-surface-800',
      },
    }"
    @hide="onClose"
  >
    <template #header>
      <div class="flex items-center gap-4 text-left">
        <div
          class="flex items-center justify-center w-12 h-12 rounded-full bg-primary-100 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 shrink-0 border border-primary-200 dark:border-primary-700/50"
        >
          <i :class="['pi text-xl', isEditMode ? 'pi-pencil' : 'pi-building']"></i>
        </div>
        <div class="flex flex-col">
          <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-tight">
            {{ isEditMode ? 'Clinic Settings' : 'Register Clinic' }}
          </h3>
          <p class="text-sm text-surface-500 dark:text-surface-400 mt-0.5">
            Define the clinic's identity and contact details.
          </p>
        </div>
      </div>
    </template>

    <div
      class="grid grid-cols-1 md:grid-cols-2 gap-6 text-left overflow-y-auto max-h-[60vh] custom-scrollbar px-1"
    >
      <div
        v-if="errors.global"
        class="md:col-span-2 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 border border-red-200 dark:border-red-800 rounded-lg text-sm flex items-center gap-2"
      >
        <i class="pi pi-exclamation-circle"></i>
        {{ errors.global }}
      </div>

      <div class="md:col-span-2 mt-2">
        <h4 class="text-xs font-bold text-surface-400 uppercase tracking-wider">
          Identity & Branding
        </h4>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold">
          Clinic Name
          <span class="text-red-500">*</span>
        </label>
        <InputText
          v-model="name"
          :invalid="submitted && !!errors.name"
          @input="errors.name = ''"
          placeholder="e.g. Nabd Medical Center"
          class="w-full"
        />
        <small v-if="submitted && errors.name" class="text-red-500 text-xs">
          {{ errors.name }}
        </small>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold">
          Subdomain
          <span class="text-red-500">*</span>
        </label>
        <InputGroup>
          <InputGroupAddon><i class="pi pi-globe"></i></InputGroupAddon>
          <InputText
            v-model="slug"
            :invalid="submitted && !!errors.slug"
            @input="onSlugInputChange"
            placeholder="clinic-name"
          />
          <InputGroupAddon class="text-xs">.nabd.care</InputGroupAddon>
        </InputGroup>
        <small v-if="submitted && errors.slug" class="text-red-500 text-xs">
          {{ errors.slug }}
        </small>
      </div>

      <div
        class="md:col-span-2 flex items-center gap-4 p-4 rounded-xl bg-surface-50 dark:bg-surface-800/50 border border-surface-200 dark:border-surface-700"
      >
        <div
          class="w-16 h-16 rounded-lg bg-white dark:bg-surface-900 border flex items-center justify-center overflow-hidden shrink-0"
        >
          <img v-if="logoUrl" :src="logoUrl" class="w-full h-full object-cover" />
          <i v-else class="pi pi-image text-2xl text-surface-300"></i>
        </div>
        <div class="flex-grow flex flex-col gap-1.5">
          <label class="text-xs font-bold text-surface-500 uppercase">Logo URL</label>
          <InputText
            v-model="logoUrl"
            :invalid="submitted && !!errors.logoUrl"
            @input="errors.logoUrl = ''"
            placeholder="https://..."
            class="w-full p-inputtext-sm"
          />
          <small v-if="submitted && errors.logoUrl" class="text-red-500 text-xs">
            {{ errors.logoUrl }}
          </small>
        </div>
      </div>

      <div class="md:col-span-2 mt-4">
        <h4 class="text-xs font-bold text-surface-400 uppercase tracking-wider">
          Contact Information
        </h4>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold">
          Official Email
          <span class="text-red-500">*</span>
        </label>
        <InputText
          v-model="email"
          :invalid="submitted && !!errors.email"
          @input="errors.email = ''"
          type="email"
          placeholder="admin@clinic.com"
          class="w-full"
        />
        <small v-if="submitted && errors.email" class="text-red-500 text-xs">
          {{ errors.email }}
        </small>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold">
          Phone Number
          <span class="text-red-500">*</span>
        </label>
        <InputText
          v-model="phone"
          :invalid="submitted && !!errors.phone"
          @input="errors.phone = ''"
          placeholder="+970 ..."
          class="w-full"
        />
        <small v-if="submitted && errors.phone" class="text-red-500 text-xs">
          {{ errors.phone }}
        </small>
      </div>

      <div
        class="md:col-span-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 pt-4 border-t border-surface-100 dark:border-surface-700"
      >
        <div class="md:col-span-2 flex flex-col gap-2">
          <label class="text-xs font-bold text-surface-500 uppercase">
            Street / Building
            <span class="text-red-500">*</span>
          </label>
          <InputText
            v-model="splitStreet"
            :invalid="submitted && !!errors.address"
            @input="errors.address = ''"
            placeholder="Street"
            class="w-full"
          />
        </div>
        <div class="flex flex-col gap-2">
          <label class="text-xs font-bold text-surface-500 uppercase">
            City
            <span class="text-red-500">*</span>
          </label>
          <InputText
            v-model="splitCity"
            @input="errors.address = ''"
            placeholder="City"
            class="w-full"
          />
        </div>
        <div class="flex flex-col gap-2">
          <label class="text-xs font-bold text-surface-500 uppercase">Postal Code</label>
          <InputText v-model="splitPostalCode" placeholder="Code" class="w-full" />
        </div>
        <div class="flex flex-col gap-2 md:col-span-4">
          <label class="text-xs font-bold text-surface-500 uppercase">
            Country
            <span class="text-red-500">*</span>
          </label>
          <CountrySelect
            v-model="splitCountry"
            @change="errors.address = ''"
            :class="{ 'p-invalid': submitted && !!errors.address }"
          />
        </div>
        <small v-if="submitted && errors.address" class="text-red-500 text-xs col-span-4">
          {{ errors.address }}
        </small>
      </div>

      <div
        class="md:col-span-2 mt-4 pt-4 border-t border-surface-100 dark:border-surface-700 grid grid-cols-2 gap-4"
      >
        <div class="flex flex-col gap-2">
          <label class="text-xs font-bold text-surface-500 uppercase">Currency</label>
          <InputText v-model="currency" placeholder="USD" class="w-full" />
        </div>
        <div class="flex flex-col gap-2">
          <label class="text-xs font-bold text-surface-500 uppercase">Timezone</label>
          <Select
            v-model="timeZone"
            :options="['Asia/Hebron', 'Asia/Jerusalem', 'UTC']"
            class="w-full"
          />
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 w-full mt-2">
        <Button label="Cancel" severity="secondary" text @click="onClose" :disabled="saving" />
        <Button
          :label="isEditMode ? 'Update Profile' : 'Create Clinic'"
          icon="pi pi-check"
          @click="onSave"
          :loading="saving"
        />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import { computed, ref, watch } from 'vue';

  // 1. Logic & State Management
  import { useClinicForm } from '@/composables/clinic/useClinicForm';
  import { useClinicActions } from '@/composables/query/clinics/useClinicActions';
  import type { ClinicResponseDto } from '@/types/backend';

  // 2. Error & Notification Handling
  import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
  import { useToastService } from '@/composables/useToastService';
  import { getFieldErrors } from '@/utils/errorHandler'; // ✅ The bridge between API error -> Form fields

  // 3. UI Components
  import CountrySelect from '@/components/Dropdowns/CountrySelect.vue';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import InputGroup from 'primevue/inputgroup';
  import InputGroupAddon from 'primevue/inputgroupaddon';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';

  const props = defineProps<{ visible: boolean; clinic?: ClinicResponseDto | null }>();
  const emit = defineEmits(['update:visible', 'saved']);

  // Composable setup
  const toast = useToastService();
  const { handleErrorAndNotify } = useErrorHandler();
  const { createClinicMutation, updateClinicMutation } = useClinicActions();

  const isVisible = computed({ get: () => props.visible, set: (v) => emit('update:visible', v) });
  const isEditMode = computed(() => !!props.clinic?.id);

  const localClinic = ref<Partial<ClinicResponseDto>>({});
  const saving = ref(false);
  const submitted = ref(false);
  const errors = ref<Record<string, string>>({}); // Maps field names to error messages

  // Address Construction
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
    logoUrl,
    currency,
    timeZone,
    initForm,
    getFormData,
    validate,
  } = useClinicForm(localClinic);

  // ✅ Clear Slug Error on Typing
  const onSlugInputChange = (e: any) => {
    slug.value = e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '');
    if (errors.value.slug) {
      errors.value.slug = '';
    }
  };

  // ============================================
  // 💾 SAVE HANDLER (Core Logic)
  // ============================================
  async function onSave() {
    submitted.value = true;
    errors.value = {};

    // 1. Handle Address Construction
    const countryName = splitCountry.value || '';

    address.value = [splitStreet.value, splitCity.value, splitPostalCode.value, countryName]
      .filter((p) => p && p.trim() !== '')
      .join(', ');

    // 2. Frontend Validation
    const validation = validate();
    if (!validation.isValid) {
      errors.value = validation.errors;
      toast.error('Please fix the highlighted errors.');
      return;
    }

    saving.value = true;

    try {
      const rawPayload = getFormData();

      // 🛠️ FIX: Inject missing required DTO properties with defaults
      const payload = {
        ...rawPayload,
        settings: {
          ...rawPayload.settings,
          dateFormat: 'YYYY-MM-DD',
          locale: 'en-US',
          enablePatientPortal: false,
        },
      };

      // 3. Execute Mutation
      if (isEditMode.value && props.clinic?.id) {
        await updateClinicMutation.mutateAsync({
          id: props.clinic.id,
          data: payload,
        });
        toast.success('Clinic updated successfully');
      } else {
        await createClinicMutation.mutateAsync(payload);
        toast.success('Clinic created successfully');
      }

      emit('saved');
      onClose();
    } catch (err: any) {
      const serverErrors = getFieldErrors(err);

      if (Object.keys(serverErrors).length > 0) {
        errors.value = serverErrors;
      } else {
        handleErrorAndNotify(err);
      }
    } finally {
      saving.value = false;
    }
  }

  function onClose() {
    if (!saving.value) isVisible.value = false;
  }

  // ============================================
  // 👁️ WATCHERS (Init Form)
  // ============================================
  watch(
    () => props.visible,
    (val) => {
      if (val) {
        submitted.value = false;
        initForm(props.clinic);

        // Parse Address String into fields
        if (props.clinic?.address) {
          const parts = props.clinic.address.split(',').map((p) => p.trim());

          // Simple right-to-left parsing assumption
          if (parts.length >= 1) splitPostalCode.value = parts.pop() || '';
          if (parts.length >= 1) splitCountry.value = parts.pop() || '';
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

<style scoped>
  .custom-scrollbar::-webkit-scrollbar {
    width: 4px;
  }
  .custom-scrollbar::-webkit-scrollbar-track {
    background: transparent;
  }
  .custom-scrollbar::-webkit-scrollbar-thumb {
    background: var(--p-surface-300);
    border-radius: 10px;
  }
</style>
