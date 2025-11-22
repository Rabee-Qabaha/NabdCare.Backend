<template>
  <Dialog
    v-model:visible="visible"
    modal
    :style="{ width: '600px', maxWidth: '90vw' }"
    :pt="{
      root: { class: 'rounded-xl border-0 shadow-2xl overflow-hidden' },
      header: { class: 'border-b border-surface-200/50 py-4 px-6 bg-white' },
      content: { class: 'p-0 h-full flex flex-col' },
      footer: { class: 'border-t border-surface-200/50 py-4 px-6 bg-surface-50' },
    }"
    @hide="onClose"
  >
    <template #header>
      <div class="flex items-center gap-3">
        <div
          class="flex items-center justify-center w-12 h-12 rounded-full shrink-0 shadow-sm"
          :class="
            role?.isSystemRole ? 'bg-orange-50 text-orange-600' : 'bg-primary-50 text-primary-600'
          "
        >
          <i :class="['text-xl pi', role?.isSystemRole ? 'pi-lock' : 'pi-briefcase']"></i>
        </div>

        <div class="flex flex-col">
          <h3 class="text-xl font-bold text-surface-900 leading-tight">{{ role?.name }}</h3>
          <div class="flex items-center gap-2 text-sm mt-1">
            <Tag
              :value="role?.isSystemRole ? 'System Role' : 'Clinic Role'"
              :severity="role?.isSystemRole ? 'warn' : 'info'"
              class="!text-[10px] !px-1.5 !py-0.5 uppercase font-bold"
            />
            <span v-if="role?.isTemplate" class="text-surface-500 text-xs italic">(Template)</span>
          </div>
        </div>
      </div>
    </template>

    <div class="flex flex-col h-[600px] bg-surface-50/30">
      <div class="p-6 pb-4 bg-white border-b border-surface-200/50">
        <div class="mb-4 text-sm text-surface-600 leading-relaxed">
          {{ role?.description || 'No description provided for this role.' }}
        </div>

        <div class="grid grid-cols-3 gap-3">
          <div class="p-3 bg-surface-50 border border-surface-200 rounded-lg flex flex-col gap-1">
            <span class="text-[10px] font-bold text-surface-400 uppercase tracking-wider">
              Assigned Users
            </span>
            <div class="flex items-center gap-2 text-lg font-bold text-surface-800">
              <i class="pi pi-users text-primary-500 text-base"></i>
              {{ role?.userCount ?? 0 }}
            </div>
          </div>

          <div class="p-3 bg-surface-50 border border-surface-200 rounded-lg flex flex-col gap-1">
            <span class="text-[10px] font-bold text-surface-400 uppercase tracking-wider">
              Permissions
            </span>
            <div class="flex items-center gap-2 text-lg font-bold text-surface-800">
              <i class="pi pi-shield text-primary-500 text-base"></i>
              {{ totalPermissionCount }}
            </div>
          </div>

          <div class="p-3 bg-surface-50 border border-surface-200 rounded-lg flex flex-col gap-1">
            <span class="text-[10px] font-bold text-surface-400 uppercase tracking-wider">
              Status
            </span>
            <div class="flex items-center gap-2 text-lg font-bold text-surface-800">
              <i class="pi pi-check-circle text-green-500 text-base"></i>
              Active
            </div>
          </div>
        </div>
      </div>

      <div class="flex-1 overflow-y-auto p-6 custom-scrollbar">
        <div class="flex items-center justify-between mb-3">
          <span class="text-xs font-bold text-surface-500 uppercase tracking-wide">
            Assigned Capabilities
          </span>
        </div>

        <div v-if="isLoading" class="space-y-3">
          <Skeleton height="4rem" class="rounded-lg" v-for="i in 2" :key="i" />
        </div>

        <div
          v-else-if="assignedCategories.length === 0"
          class="flex flex-col items-center justify-center py-12 text-surface-400 bg-white rounded-xl border border-surface-200 border-dashed"
        >
          <i class="pi pi-lock-open text-3xl mb-2 opacity-30"></i>
          <span class="text-sm font-medium">No permissions assigned</span>
          <span class="text-xs opacity-70">This role has no active capabilities.</span>
        </div>

        <div v-else class="space-y-4">
          <div
            v-for="cat in assignedCategories"
            :key="cat.key"
            class="bg-white border border-surface-200 rounded-lg overflow-hidden shadow-sm"
          >
            <div
              class="bg-surface-50/80 px-4 py-2 border-b border-surface-100 flex items-center justify-between"
            >
              <span class="font-semibold text-sm text-surface-700">{{ cat.label }}</span>
              <Badge
                :value="cat.items.length"
                severity="secondary"
                class="!bg-white !text-surface-500 shadow-sm !min-w-[1.25rem]"
              />
            </div>

            <div class="p-3 flex flex-wrap gap-2">
              <div
                v-for="perm in cat.items"
                :key="perm.id"
                class="inline-flex items-center gap-2 px-2.5 py-1.5 rounded-md bg-primary-50 text-primary-700 text-xs font-medium border border-primary-100"
                :title="perm.description"
              >
                <i class="pi pi-check-circle text-[10px]"></i>
                {{ perm.name }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end w-full mt-4">
        <Button
          label="Close"
          severity="secondary"
          outlined
          class="w-full sm:w-auto min-w-[100px]"
          @click="onClose"
        />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import Badge from 'primevue/badge';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';

  import { useRolePermissions } from '@/composables/query/roles/useRolePermissions';
  import { computed } from 'vue';

  interface Props {
    visible: boolean;
    roleId: string | null;
    role?: any;
  }

  const props = defineProps<Props>();
  const emit = defineEmits(['update:visible']);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  const roleIdRef = computed(() => props.roleId);

  // Use the composable logic to fetch data
  const { categories, permissionsQuery } = useRolePermissions(roleIdRef);

  const isLoading = computed(() => permissionsQuery.isLoading.value);

  // Logic: Filter to show ONLY assigned categories/permissions
  const assignedCategories = computed(() => {
    return categories.value
      .map((cat) => {
        // Filter items that are checked (assigned)
        const assignedItems = cat.items.filter((p) => p.checked);

        // If category has no items, remove it entirely
        if (assignedItems.length === 0) return null;

        return { ...cat, items: assignedItems };
      })
      .filter(Boolean) as any[];
  });

  const totalPermissionCount = computed(() =>
    assignedCategories.value.reduce((sum, c) => sum + c.items.length, 0),
  );

  function onClose() {
    visible.value = false;
  }
</script>

<style scoped>
  .custom-scrollbar::-webkit-scrollbar {
    width: 6px;
  }
  .custom-scrollbar::-webkit-scrollbar-track {
    background: transparent;
  }
  .custom-scrollbar::-webkit-scrollbar-thumb {
    background-color: var(--surface-300);
    border-radius: 20px;
  }
  .custom-scrollbar::-webkit-scrollbar-thumb:hover {
    background-color: var(--surface-400);
  }
</style>
