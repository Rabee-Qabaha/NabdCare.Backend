<template>
  <Dialog
    v-model:visible="visible"
    :header="isEditMode ? 'Edit Role' : 'Create New Role'"
    :modal="true"
    :style="{ width: '550px' }"
    class="rounded-xl p-4"
    @hide="onClose"
  >
    <div classs="flex flex-col gap-6 p-2 pt-4">
      <FloatLabel class="mt-4">
        <InputText
          id="name"
          v-model.trim="formData.name"
          type="text"
          maxlength="100"
          class="w-full"
          :invalid="submitted && !isNameValid"
          required
        />
        <label for="name">
          Role Name
          <span class="text-red-500">*</span>
        </label>
      </FloatLabel>
      <small v-if="submitted && !isNameValid" class="mt-1 block text-red-500">
        Role name is required (max 100 characters).
      </small>
      <small v-else class="mt-1 block text-gray-500">Max 100 characters</small>

      <FloatLabel class="mt-6">
        <Textarea
          id="description"
          v-model="formData.description"
          :auto-resize="true"
          rows="4"
          maxlength="500"
          class="w-full"
        />
        <label for="description">Description</label>
      </FloatLabel>
      <small class="mt-1 block text-gray-500">
        {{ formData.description?.length ?? 0 }}/500 characters
      </small>

      <div v-if="!isEditMode || !role?.isSystemRole" class="mt-6 flex flex-col gap-2">
        <FloatLabel>
          <Dropdown
            id="clinicId"
            v-model="formData.clinicId"
            :options="clinics"
            optionLabel="name"
            optionValue="id"
            class="w-full"
          />
          <label for="clinicId">Clinic</label>
        </FloatLabel>
        <small class="text-gray-500">Optional - leave empty for current clinic</small>
      </div>

      <div class="mt-6">
        <label for="colorCode" class="mb-2 block font-medium">
          Color
        </label>
        <div class="flex items-center gap-2">
          <ColorPicker v-model="formData.colorCode" />
          <InputText
            id="colorCode"
            v-model="formData.colorCode"
            class="w-full"
            placeholder="e.g., 3B82F6"
          />
        </div>
      </div>

      <FloatLabel class="mt-6">
        <Dropdown
          id="iconClass"
          v-model="formData.iconClass"
          :options="primeIcons"
          optionLabel="name"
          optionValue="class"
          filter
          showClear
          class="w-full"
        >
          <template #value="slotProps">
            <div v-if="slotProps.value" class="flex items-center gap-2">
              <i :class="slotProps.value" style="font-size: 1.25rem;"></i>
              <span>{{ slotProps.value }}</span>
            </div>
            <span v-else>
              Select an Icon
            </span>
          </template>
          <template #option="slotProps">
            <div class="flex items-center gap-2">
              <i :class="slotProps.option.class" style="font-size: 1.25rem;"></i>
              <span>{{ slotProps.option.name }}</span>
            </div>
          </template>
        </Dropdown>
        <label for="iconClass">Select an Icon</label>
      </FloatLabel>
      <div v-if="isEditMode && role" class="mt-6">
        </div>
    </div>

    <template #footer>
      <div class="flex justify-between gap-2 p-2">
        <Button
          label="Cancel"
          icon="pi pi-times"
          severity="secondary"
          outlined
          @click="onClose"
          :disabled="isSubmitting"
        />
        <Button
          :label="isEditMode ? 'Save Changes' : 'Create Role'"
          icon="pi pi-check"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          @click="submitForm"
        />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { RoleResponseDto, CreateRoleRequestDto, UpdateRoleRequestDto } from '@/types/backend';
import { useCreateRole, useUpdateRole } from '@/composables/query/roles/useRoleActions';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Dropdown from 'primevue/dropdown';
import FloatLabel from 'primevue/floatlabel';
import ColorPicker from 'primevue/colorpicker';

interface Props {
  visible: boolean;
  role?: RoleResponseDto | null;
}

const props = withDefaults(defineProps<Props>(), {
  role: null,
});

const emit = defineEmits<{
  'update:visible': [value: boolean];
  'created': [];
  'updated': [];
}>();

const submitted = ref(false);

// --- UPDATED: Expanded List of PrimeIcons ---
const primeIcons = ref([
  // Common UI
  { name: 'User', class: 'pi pi-user' },
  { name: 'User Plus', class: 'pi pi-user-plus' },
  { name: 'User Minus', class: 'pi pi-user-minus' },
  { name: 'Users', class: 'pi pi-users' },
  { name: 'Cog', class: 'pi pi-cog' },
  { name: 'Search', class: 'pi pi-search' },
  { name: 'Home', class: 'pi pi-home' },
  { name: 'Envelope', class: 'pi pi-envelope' },
  { name: 'Phone', class: 'pi pi-phone' },
  { name: 'Calendar', class: 'pi pi-calendar' },
  { name: 'Clock', class: 'pi pi-clock' },
  { name: 'Info Circle', class: 'pi pi-info-circle' },
  { name: 'Question Circle', class: 'pi pi-question-circle' },
  { name: 'Exclamation Triangle', class: 'pi pi-exclamation-triangle' },
  { name: 'Exclamation Circle', class: 'pi pi-exclamation-circle' },

  // Actions
  { name: 'Check', class: 'pi pi-check' },
  { name: 'Check Circle', class: 'pi pi-check-circle' },
  { name: 'Times', class: 'pi pi-times' },
  { name: 'Times Circle', class: 'pi pi-times-circle' },
  { name: 'Plus', class: 'pi pi-plus' },
  { name: 'Plus Circle', class: 'pi pi-plus-circle' },
  { name: 'Minus', class: 'pi pi-minus' },
  { name: 'Minus Circle', class: 'pi pi-minus-circle' },
  { name: 'Pencil', class: 'pi pi-pencil' },
  { name: 'Trash', class: 'pi pi-trash' },
  { name: 'Copy', class: 'pi pi-copy' },
  { name: 'Clone', class: 'pi pi-clone' },
  { name: 'Filter', class: 'pi pi-filter' },
  { name: 'Filter Slash', class: 'pi pi-filter-slash' },
  { name: 'Upload', class: 'pi pi-upload' },
  { name: 'Download', class: 'pi pi-download' },
  { name: 'Refresh', class: 'pi pi-refresh' },
  { name: 'Sync', class: 'pi pi-sync' },
  { name: 'Sign In', class: 'pi pi-sign-in' },
  { name: 'Sign Out', class: 'pi pi-sign-out' },
  { name: 'Lock', class: 'pi pi-lock' },
  { name: 'Lock Open', class: 'pi pi-lock-open' },

  // Healthcare
  { name: 'Heart', class: 'pi pi-heart' },
  { name: 'Heart Fill', class: 'pi pi-heart-fill' },
  { name: 'Briefcase', class: 'pi pi-briefcase' },
  { name: 'Book', class: 'pi pi-book' },
  { name: 'Stethoscope', class: 'pi pi-stethoscope' }, // You might need to add this to PrimeIcons if it's custom
  { name: 'Clinic', class: 'pi pi-building' }, // Using 'building' as a proxy

  // Objects & Files
  { name: 'Shield', class: 'pi pi-shield' },
  { name: 'Star', class: 'pi pi-star' },
  { name: 'Star Fill', class: 'pi pi-star-fill' },
  { name: 'Palette', class: 'pi pi-palette' },
  { name: 'Building', class: 'pi pi-building' },
  { name: 'File', class: 'pi pi-file' },
  { name: 'File Excel', class: 'pi pi-file-excel' },
  { name: 'File PDF', class: 'pi pi-file-pdf' },
  { name: 'Folder', class: 'pi pi-folder' },
  { name: 'Folder Open', class: 'pi pi-folder-open' },
  { name: 'Image', class: 'pi pi-image' },
  { name: 'Video', class: 'pi pi-video' },
  { name: 'Globe', class: 'pi pi-globe' },
  { name: 'Database', class: 'pi pi-database' },
  { name: 'Server', class: 'pi pi-server' },

  // Navigation & Arrows
  { name: 'Arrow Left', class: 'pi pi-arrow-left' },
  { name: 'Arrow Right', class: 'pi pi-arrow-right' },
  { name: 'Arrow Up', class: 'pi pi-arrow-up' },
  { name: 'Arrow Down', class: 'pi pi-arrow-down' },
  { name: 'Chevron Left', class: 'pi pi-chevron-left' },
  { name: 'Chevron Right', class: 'pi pi-chevron-right' },
  { name: 'Chevron Up', class: 'pi pi-chevron-up' },
  { name: 'Chevron Down', class: 'pi pi-chevron-down' },
]);
// --- End Expanded List ---

const formData = ref({
  name: '',
  description: '',
  clinicId: null as string | null,
  colorCode: '3B82F6', 
  iconClass: '',
});

const isSubmitting = ref(false);
const clinics = ref([
  { id: '1', name: 'Main Clinic' },
  { id: '2', name: 'Branch Clinic' },
]);

const { mutate: createRole } = useCreateRole();
const { mutate: updateRole } = useUpdateRole();

const visible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});

const isEditMode = computed(() => !!props.role);

const isNameValid = computed(() => {
  return formData.value.name.trim().length > 0 && formData.value.name.length <= 100;
});

const onClose = () => {
  visible.value = false;
};

const submitForm = async () => {
  submitted.value = true;

  if (!isNameValid.value) {
    return;
  }

  isSubmitting.value = true;

  try {
    if (isEditMode.value && props.role) {
      const payload: UpdateRoleRequestDto = {
        name: formData.value.name,
        description: formData.value.description || '',
        colorCode: `#${formData.value.colorCode || '3B82F6'}`, 
        iconClass: formData.value.iconClass || '',
        isTemplate: false 
      };

      updateRole(
        { id: props.role.id, data: payload },
        {
          onSuccess: () => {
            emit('updated');
            onClose();
          },
          onSettled: () => {
            isSubmitting.value = false;
          }
        }
      );
    } else {
      const payload: CreateRoleRequestDto = {
        name: formData.value.name,
        description: formData.value.description || '',
        clinicId: formData.value.clinicId || '', 
        isTemplate: false,
        colorCode: `#${formData.value.colorCode || '3B82F6'}`, 
        iconClass: formData.value.iconClass || '',
        templateRoleId: ''
      };

      createRole(payload, {
        onSuccess: () => {
          emit('created');
          onClose();
        },
        onSettled: () => {
          isSubmitting.value = false;
        }
      });
    }
  } catch (error) {
    isSubmitting.value = false; 
  }
};

const formatDate = (date: string | Date): string => {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};

watch(
  () => props.visible,
  (newVal) => {
    if (newVal) {
      submitted.value = false;   

      if (props.role) {
        // Edit mode
        formData.value = {
          name: props.role.name,
          description: props.role.description || '',
          clinicId: props.role.clinicId || null,
          colorCode: props.role.colorCode?.replace('#', '') || '3B82F6', 
          iconClass: props.role.iconClass || '',
        };
      } else {
        // Create mode
        formData.value = {
          name: '',
          description: '',
          clinicId: null,
          colorCode: '3B82F6', 
          iconClass: '',
        };
      }
    }
  }
);
</script>