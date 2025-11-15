<template>
  <Dialog
    v-model:visible="visible"
    :header="`Manage Permissions - ${role?.name}`"
    :modal="true"
    class="w-full md:w-2/3"
    @hide="onClose"
  >
    <div class="space-y-4">
      <div v-if="isLoadingPermissions" class="flex justify-center items-center py-8">
        <ProgressSpinner style="width: 40px; height: 40px" stroke-width="4" />
      </div>

      <template v-else>
        <div>
          <IconField class="w-full">
            <InputIcon><i class="pi pi-search" /></InputIcon>
            <InputText
              v-model="searchQuery"
              type="text"
              placeholder="Search permissions..."
              class="w-full"
            />
          </IconField>
        </div>

        <div class="space-y-2 max-h-[60vh] overflow-y-auto pr-2">
          <Panel
            v-for="(permissions, category) in groupedPermissions"
            :key="category"
            :header="formatCategoryName(category)"
            toggleable
            :collapsed="searchQuery.length > 0 ? false : true"
          >
            <template #header>
              <div
                class="flex items-center gap-3 w-full"
                @click.stop="toggleCategory(category, permissions)"
              >
                <Checkbox
                  v-model="categorySelectionState[category].selected"
                  :binary="true"
                  :indeterminate="categorySelectionState[category].indeterminate"
                  @click.stop="toggleCategory(category, permissions)"
                />
                <span class="font-semibold">
                  {{ formatCategoryName(category) }}
                </span>
              </div>
            </template>
            
            <div class="flex flex-col gap-4 pt-2 pb-1 pl-6">
              <div
                v-for="permission in permissions"
                :key="permission.id"
                class="flex items-center"
              >
                <Checkbox
                  :input-id="`perm-${permission.id}`"
                  v-model="selectedPermissions"
                  :value="permission.id"
                  :disabled="isSavingPermissions"
                />
                <label :for="`perm-${permission.id}`" class="ml-2 cursor-pointer flex-1">
                  <div class="font-medium text-surface-800 dark:text-surface-100">
                    {{ permission.name }}
                  </div>
                  <div class="text-sm text-surface-600 dark:text-surface-400">
                    {{ permission.description }}
                  </div>
                </label>
              </div>
            </div>
          </Panel>
        </div>

        <div v-if="permissionError" class="rounded-lg bg-red-50 p-4 dark:bg-red-900">
          <p class="text-sm text-red-800">{{ permissionError }}</p>
        </div>
      </template>
    </div>

    <template #footer>
      <Button label="Close" @click="onClose" severity="secondary" />
      <Button
        label="Save Permissions"
        icon="pi pi-check"
        @click="savePermissions"
        :loading="isSavingPermissions"
        :disabled="isLoadingPermissions"
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import type { RoleResponseDto } from '@/types/backend';
import { rolesApi } from '@/api/modules/roles';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Checkbox from 'primevue/checkbox';
import ProgressSpinner from 'primevue/progressspinner';
// --- New Imports ---
import Panel from 'primevue/panel';
import IconField from 'primevue/iconfield';
import InputIcon from 'primevue/inputicon';
// --- Removed Tag import ---

interface Permission {
  id: string;
  name: string;
  description: string;
  category: string;
}

interface Props {
  visible: boolean;
  role?: RoleResponseDto | null;
}

const props = withDefaults(defineProps<Props>(), {
  role: null,
});

const emit = defineEmits<{
  'update:visible': [value: boolean];
  'updated': [];
}>();

const queryClient = useQueryClient();
const searchQuery = ref('');
const selectedPermissions = ref<string[]>([]);
const isSavingPermissions = ref(false);
const permissionError = ref('');

// --- Updated Sample Data to match screenshot ---
const allPermissions = ref<Permission[]>([
  { id: 'patient.view', name: 'View Patient Record', description: 'Can view patient charts', category: 'Patient Management' },
  { id: 'patient.create', name: 'Create Patient Record', description: 'Can create new patient files', category: 'Patient Management' },
  { id: 'patient.edit', name: 'Edit Patient Record', description: 'Can modify existing patient records', category: 'Patient Management' },
  { id: 'patient.delete', name: 'Delete Patient Record', description: 'Can delete patient records', category: 'Patient Management' },

  { id: 'appt.view', name: 'View Appointments', description: 'Can see the schedule', category: 'Appointments' },
  { id: 'appt.book', name: 'Book Appointments', description: 'Can create new appointments', category: 'Appointments' },
  { id: 'appt.cancel', name: 'Cancel Appointments', description: 'Can cancel/reschedule appointments', category: 'Appointments' },

  { id: 'billing.view', name: 'View Invoices', description: 'Can see billing history', category: 'Billing & Invoicing' },
  { id: 'billing.create', name: 'Create Invoices', description: 'Can generate new invoices', category: 'Billing & Invoicing' },
  { id: 'billing.process', name: 'Process Payments', description: 'Can record payments', category: 'Billing & Invoicing' },
  
  { id: 'admin.users', name: 'Manage Users', description: 'Can create/edit user accounts', category: 'System Administration' },
  { id: 'admin.roles', name: 'Manage Roles', description: 'Can define permissions and roles', category: 'System Administration' },
  { id: 'admin.settings', name: 'Edit System Settings', description: 'Can change global settings', category: 'System Administration' },
]);

const visible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});

// Load role permissions (unchanged)
const { isLoading: isLoadingPermissions } = useQuery({
  queryKey: ['role-permissions', props.role?.id],
  queryFn: async () => {
    if (!props.role) return [];
    try {
      // FAKE API CALL FOR DEMO - REPLACE WITH YOURS
      // const permissions = await rolesApi.getPermissions(props.role.id);
      const permissions = ['patient.view', 'patient.create', 'billing.view']; // Demo data
      selectedPermissions.value = permissions;
      permissionError.value = '';
      return permissions;
    } catch (error: any) {
      permissionError.value = error.message || 'Failed to load permissions';
      return [];
    }
  },
  enabled: computed(() => visible.value && !!props.role),
});

const filteredPermissions = computed(() => {
  if (!searchQuery.value) return allPermissions.value;
  const query = searchQuery.value.toLowerCase();
  return allPermissions.value.filter(
    (p) => p.name.toLowerCase().includes(query) || p.description.toLowerCase().includes(query)
  );
});

// --- NEW: Group permissions by category ---
const groupedPermissions = computed(() => {
  return filteredPermissions.value.reduce((acc, permission) => {
    const { category } = permission;
    if (!acc[category]) {
      acc[category] = [];
    }
    acc[category].push(permission);
    return acc;
  }, {} as Record<string, Permission[]>);
});

// --- NEW: Logic for category header checkbox state ---
const categorySelectionState = computed(() => {
  const state: Record<string, { selected: boolean; indeterminate: boolean }> = {};
  for (const category in groupedPermissions.value) {
    const categoryPermissions = groupedPermissions.value[category];
    const categoryPermissionIds = categoryPermissions.map(p => p.id);
    
    const selectedCount = categoryPermissionIds.filter(id => selectedPermissions.value.includes(id)).length;
    
    const allSelected = selectedCount === categoryPermissionIds.length;
    const noneSelected = selectedCount === 0;
    
    state[category] = {
      selected: allSelected,
      indeterminate: !allSelected && !noneSelected,
    };
  }
  return state;
});

// --- NEW: Toggle all permissions in a category ---
const toggleCategory = (category: string, permissions: Permission[]) => {
  const state = categorySelectionState.value[category];
  const permissionIds = permissions.map(p => p.id);

  if (state.selected) {
    // All are selected, so deselect all
    selectedPermissions.value = selectedPermissions.value.filter(id => !permissionIds.includes(id));
  } else {
    // Not all are selected (some or none), so select all
    const newSelections = new Set([...selectedPermissions.value, ...permissionIds]);
    selectedPermissions.value = Array.from(newSelections);
  }
};

const formatCategoryName = (category: string): string => {
  // This can be more robust if needed
  return category;
};

// --- REMOVED: getCategoryIcon, getSelectedCountInCategory, permissionCategories, getCategoryPermissions ---

const onClose = () => {
  visible.value = false;
};

const savePermissions = async () => {
  isSavingPermissions.value = true;
  try {
    console.log('Saving:', selectedPermissions.value);
    // TODO: Implement actual API call with selectedPermissions.value
    await new Promise((resolve) => setTimeout(resolve, 1000));
    emit('updated');
    onClose();
  } finally {
    isSavingPermissions.value = false;
  }
};

watch(
  () => props.visible,
  (newVal) => {
    if (!newVal) {
      searchQuery.value = '';
      permissionError.value = '';
    }
  }
);
</script>