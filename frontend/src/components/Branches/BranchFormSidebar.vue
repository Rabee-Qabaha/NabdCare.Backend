<script setup lang="ts">
  import CountrySelect from '@/components/Dropdowns/CountrySelect.vue';
  import { useBranchActions } from '@/composables/query/branches/useBranchActions';
  import type {
    BranchResponseDto,
    CreateBranchRequestDto,
    UpdateBranchRequestDto,
  } from '@/types/backend';
  import Button from 'primevue/button';
  import Drawer from 'primevue/drawer';
  import InputText from 'primevue/inputtext';
  import Message from 'primevue/message';
  import Textarea from 'primevue/textarea';
  import ToggleSwitch from 'primevue/toggleswitch';
  import { computed, ref, watch } from 'vue';
  import { z } from 'zod';

  const props = defineProps<{
    visible: boolean;
    clinicId: string;
    branchToEdit?: BranchResponseDto | null;
  }>();

  const emit = defineEmits(['update:visible', 'saved']);
  const { createMutation, updateMutation, canSetMain } = useBranchActions();

  // -- 1. Zod Validation Schema --
  const branchSchema = z
    .object({
      name: z.string().trim().min(1, 'Branch Name is required').max(100, 'Name is too long'),
      email: z.string().trim().email('Invalid email format').or(z.literal('')),
      phone: z.string().trim().max(20, 'Phone is too long').or(z.literal('')),

      // Address parts
      street: z.string().trim().max(150, 'Street is too long').or(z.literal('')),
      city: z.string().trim().max(50, 'City is too long').or(z.literal('')),
      postalCode: z.string().trim().max(20, 'Zip code is too long').or(z.literal('')),
      country: z.string().trim().optional(),

      isMain: z.boolean(),
    })
    .superRefine((data, ctx) => {
      // Backend Rule: Combined address cannot exceed 255 chars
      const fullAddr = [data.street, data.city, data.country, data.postalCode]
        .filter((p) => p && p.trim() !== '')
        .join(', ');

      if (fullAddr.length > 255) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Total address length exceeds limit (255 chars)',
          path: ['street'],
        });
      }
    });

  // -- State --
  const form = ref({
    name: '',
    email: '',
    phone: '',
    isMain: false,
    street: '',
    city: '',
    postalCode: '',
    country: null as string | null, // Corrected type for CountrySelect
  });

  const errors = ref<Record<string, string>>({});
  const serverError = ref<string | null>(null);
  const isEditing = computed(() => !!props.branchToEdit);

  // -- Helper: Parse Address --
  const parseAddress = (fullAddress: string) => {
    if (!fullAddress) return { street: '', city: '', postalCode: '', country: '' };
    const parts = fullAddress.split(',').map((p) => p.trim());

    // Heuristic: Last part country, second to last zip, third to last city, rest street
    // This is just a best-effort parser for UI prepopulation
    if (parts.length >= 3) {
      const country = parts.pop() || '';
      const postalCode = parts.pop() || '';
      const city = parts.pop() || '';
      const street = parts.join(', ');
      return { street, city, postalCode, country };
    }
    return { street: fullAddress, city: '', postalCode: '', country: '' };
  };

  // -- Watchers --
  watch(
    () => props.visible,
    (isOpen) => {
      if (isOpen) {
        serverError.value = null;
        errors.value = {};

        if (props.branchToEdit) {
          const addr = parseAddress(props.branchToEdit.address || '');
          form.value = {
            name: props.branchToEdit.name,
            email: props.branchToEdit.email || '',
            phone: props.branchToEdit.phone || '',
            isMain: props.branchToEdit.isMain,
            street: addr.street,
            city: addr.city,
            postalCode: addr.postalCode,
            country: addr.country || null,
          };
        } else {
          form.value = {
            name: '',
            email: '',
            phone: '',
            street: '',
            city: '',
            postalCode: '',
            country: null,
            isMain: false,
          };
        }
      }
    },
  );

  // -- Actions --
  const validate = () => {
    const result = branchSchema.safeParse(form.value);
    if (!result.success) {
      const newErrors: Record<string, string> = {};
      result.error.issues.forEach((issue) => {
        const field = (issue.path[0] ?? 'general').toString();
        newErrors[field] = issue.message;
      });
      errors.value = newErrors;
      return false;
    }
    errors.value = {};
    return true;
  };

  const onSubmit = async () => {
    serverError.value = null;

    if (!validate()) return;

    const fullAddress = [
      form.value.street,
      form.value.city,
      form.value.postalCode, // Adjusted order slightly
      form.value.country,
    ]
      .filter((p) => p && p.toString().trim() !== '')
      .join(', ');

    try {
      if (isEditing.value && props.branchToEdit) {
        const updatePayload: UpdateBranchRequestDto = {
          clinicId: props.clinicId,
          name: form.value.name,
          email: form.value.email,
          phone: form.value.phone,
          address: fullAddress,
          isMain: form.value.isMain,
          isActive: props.branchToEdit.isActive,
        };

        await updateMutation.mutateAsync({
          id: props.branchToEdit.id,
          dto: updatePayload,
        });
      } else {
        const createPayload: CreateBranchRequestDto = {
          clinicId: props.clinicId,
          name: form.value.name,
          email: form.value.email,
          phone: form.value.phone,
          address: fullAddress,
          isMain: form.value.isMain,
          isActive: true,
        };

        await createMutation.mutateAsync(createPayload);
      }

      emit('saved');
      emit('update:visible', false);
    } catch (err: any) {
      console.error('Full Error Object:', err);
      const responseBody = err.response?.data || err;
      const backendError = responseBody?.error || responseBody;

      if (backendError && (backendError.code || backendError.details)) {
        if (backendError.details) {
          const newErrors: Record<string, string> = {};
          Object.keys(backendError.details).forEach((key) => {
            const messages = backendError.details[key];
            if (messages && messages.length > 0) {
              const fieldName = key.toLowerCase() === 'address' ? 'street' : key;
              newErrors[fieldName] = messages[0];
            }
          });
          errors.value = newErrors;
          if (Object.keys(errors.value).length > 0) return;
        }
        if (backendError.message) {
          serverError.value = backendError.message;
          return;
        }
      }

      if (err.response) {
        serverError.value =
          err.response.data?.title ||
          err.response.data ||
          `Request failed: ${err.response.statusText} (${err.response.status})`;
      } else {
        serverError.value = err.message || 'Unable to connect to the server.';
      }
    }
  };
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="!w-full md:!w-[400px]"
    :header="isEditing ? 'Edit Branch' : 'New Branch'"
    :dismissable="false"
    :pt="{
      root: {
        class:
          'bg-surface-0 dark:bg-surface-900 border-l border-surface-200 dark:border-surface-700',
      },
      header: {
        class:
          'border-b border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900 text-surface-900 dark:text-surface-0',
      },
      content: { class: 'bg-surface-0 dark:bg-surface-900' },
      footer: {
        class:
          'bg-surface-50 dark:bg-surface-800 border-t border-surface-200 dark:border-surface-700',
      },
    }"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="flex flex-col h-full gap-6">
      <Message
        v-if="serverError"
        severity="error"
        :closable="false"
        icon="pi pi-times-circle"
        class="w-full"
      >
        {{ serverError }}
      </Message>

      <div
        v-if="canSetMain"
        class="flex items-center justify-between p-4 rounded-xl border transition-colors duration-200 mt-6"
        :class="[
          form.isMain
            ? 'bg-orange-50 border-orange-200 dark:bg-orange-500/10 dark:border-orange-500/30'
            : 'bg-surface-50 border-surface-200 dark:bg-surface-800 dark:border-surface-700',
        ]"
      >
        <div class="flex gap-3">
          <div
            class="mt-1 w-8 h-8 rounded-full flex items-center justify-center shrink-0 transition-colors"
            :class="
              form.isMain
                ? 'bg-orange-100 text-orange-600 dark:bg-orange-500/20 dark:text-orange-400'
                : 'bg-surface-200 text-surface-500 dark:bg-surface-700 dark:text-surface-400'
            "
          >
            <i class="pi pi-star-fill text-sm"></i>
          </div>
          <div class="flex flex-col">
            <span
              class="font-semibold text-sm transition-colors"
              :class="
                form.isMain
                  ? 'text-orange-700 dark:text-orange-400'
                  : 'text-surface-900 dark:text-surface-0'
              "
            >
              Main Headquarters
            </span>
            <span class="text-xs text-surface-500 dark:text-surface-400 max-w-[250px]">
              Set this as the primary location used for billing and legal identity.
            </span>
          </div>
        </div>
        <ToggleSwitch
          v-model="form.isMain"
          v-tooltip.left="isEditing && form.isMain ? 'Promote another branch to switch HQ' : ''"
          :disabled="isEditing && form.isMain"
        />
      </div>

      <div class="flex flex-col gap-5">
        <h3
          class="text-sm font-bold text-surface-900 dark:text-surface-0 border-b border-surface-200 dark:border-surface-700 pb-2 flex items-center gap-2"
        >
          <i class="pi pi-info-circle text-primary"></i>
          General Information
        </h3>

        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-medium text-surface-700 dark:text-surface-300">
            Branch Name
            <span class="text-red-500">*</span>
          </label>
          <InputText
            v-model="form.name"
            placeholder="e.g. City Center Clinic"
            class="w-full"
            :invalid="!!errors.name"
            @input="errors.name = ''"
          />
          <small v-if="errors.name" class="text-red-500 text-xs">{{ errors.name }}</small>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-surface-700 dark:text-surface-300">Email</label>
            <InputText
              v-model="form.email"
              placeholder="branch@email.com"
              class="w-full"
              :invalid="!!errors.email"
              @input="errors.email = ''"
            />
            <small v-if="errors.email" class="text-red-500 text-xs">{{ errors.email }}</small>
          </div>
          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-surface-700 dark:text-surface-300">Phone</label>
            <InputText
              v-model="form.phone"
              placeholder="+1 234..."
              class="w-full"
              :invalid="!!errors.phone"
              @input="errors.phone = ''"
            />
            <small v-if="errors.phone" class="text-red-500 text-xs">{{ errors.phone }}</small>
          </div>
        </div>
      </div>

      <div class="flex flex-col gap-5">
        <h3
          class="text-sm font-bold text-surface-900 dark:text-surface-0 border-b border-surface-200 dark:border-surface-700 pb-2 mt-2 flex items-center gap-2"
        >
          <i class="pi pi-map-marker text-primary"></i>
          Address Details
        </h3>

        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-medium text-surface-700 dark:text-surface-300">
            Street / Building
          </label>
          <Textarea
            v-model="form.street"
            placeholder="123 Health Ave, Building B"
            class="w-full resize-none"
            rows="2"
            auto-resize
            :invalid="!!errors.street"
            @input="errors.street = ''"
          />
          <small v-if="errors.street" class="text-red-500 text-xs">{{ errors.street }}</small>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-surface-700 dark:text-surface-300">City</label>
            <InputText
              v-model="form.city"
              placeholder="e.g. Ramallah"
              class="w-full"
              :invalid="!!errors.city"
              @input="errors.city = ''"
            />
            <small v-if="errors.city" class="text-red-500 text-xs">{{ errors.city }}</small>
          </div>
          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-surface-700 dark:text-surface-300">
              Postal Code
            </label>
            <InputText
              v-model="form.postalCode"
              placeholder="e.g. 90210"
              class="w-full"
              :invalid="!!errors.postalCode"
              @input="errors.postalCode = ''"
            />
            <small v-if="errors.postalCode" class="text-red-500 text-xs">
              {{ errors.postalCode }}
            </small>
          </div>
        </div>

        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-medium text-surface-700 dark:text-surface-300">Country</label>
          <CountrySelect v-model="form.country" placeholder="Select Country" class="w-full" />
        </div>
      </div>

      <div class="flex-grow"></div>

      <div
        class="flex gap-3 pt-4 border-t border-surface-100 dark:border-surface-800 sticky bottom-0 bg-surface-0 dark:bg-surface-900 pb-2"
      >
        <Button
          label="Cancel"
          severity="secondary"
          text
          class="flex-1"
          @click="emit('update:visible', false)"
        />
        <Button
          :label="isEditing ? 'Save Changes' : 'Create Branch'"
          icon="pi pi-check"
          class="flex-1"
          :loading="createMutation.isPending.value || updateMutation.isPending.value"
          @click="onSubmit"
        />
      </div>
    </div>
  </Drawer>
</template>
