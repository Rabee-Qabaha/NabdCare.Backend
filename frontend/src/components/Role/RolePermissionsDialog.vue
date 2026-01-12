<template>
  <Dialog
    v-model:visible="visible"
    modal
    :style="{ width: '750px', maxWidth: '90vw' }"
    :pt="{
      root: { class: 'rounded-xl border-0 shadow-2xl overflow-hidden' },
      header: {
        class:
          'border-b border-surface-200/50 dark:border-surface-700/50 py-4 px-6 bg-surface-0 dark:bg-surface-900',
      },
      content: { class: 'p-0 h-full flex flex-col bg-surface-0 dark:bg-surface-900' },
      footer: {
        class:
          'border-t border-surface-200/50 dark:border-surface-700/50 py-4 px-6 bg-surface-50 dark:bg-surface-800',
      },
    }"
    @hide="onClose"
  >
    <template #header>
      <div class="flex items-center gap-3">
        <div
          class="flex items-center justify-center w-10 h-10 rounded-full bg-primary-50 dark:bg-primary-500/20 text-primary-600 dark:text-primary-400 shrink-0"
        >
          <i class="pi pi-briefcase text-xl"></i>
        </div>
        <div class="flex flex-col">
          <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-tight">
            Manage Role Permissions
          </h3>
          <p class="text-sm text-surface-500 dark:text-surface-400">
            Configure capabilities for this role
          </p>
        </div>
      </div>
    </template>

    <div class="flex flex-col h-[600px]">
      <div
        class="px-6 py-4 border-b border-surface-100 dark:border-surface-800 bg-white/95 dark:bg-surface-900/95 backdrop-blur-sm sticky top-0 z-20"
      >
        <div v-if="!isLoading">
          <IconField icon-position="left" class="w-full">
            <InputIcon class="pi pi-search text-surface-400" />
            <InputText
              v-model="search"
              placeholder="Find permission..."
              class="w-full border-surface-200 dark:border-surface-700 focus:border-primary-500 shadow-sm"
              variant="filled"
            />
          </IconField>

          <div class="flex justify-between items-center mt-3 h-6">
            <div class="flex items-center gap-2">
              <span
                class="uppercase tracking-wider text-[10px] font-bold text-surface-400 dark:text-surface-500"
              >
                Role Permissions
              </span>
              <Badge
                :value="totalVisible"
                severity="secondary"
                class="!min-w-[1.5rem] !h-[1.25rem] !leading-[1.25rem]"
              />
            </div>

            <div class="flex items-center justify-end min-w-[120px]">
              <transition
                mode="out-in"
                enter-active-class="transition duration-200 ease-out"
                enter-from-class="opacity-0 translate-y-1"
                enter-to-class="opacity-100 translate-y-0"
                leave-active-class="transition duration-150 ease-in"
                leave-from-class="opacity-100 translate-y-0"
                leave-to-class="opacity-0 -translate-y-1"
              >
                <div
                  v-if="isMutating"
                  class="flex items-center gap-2 text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-500/20 px-2 py-1 rounded-md"
                >
                  <i class="pi pi-spinner pi-spin text-xs"></i>
                  <span class="text-xs font-medium">Saving...</span>
                </div>
                <div v-else class="flex items-center gap-2 text-surface-400 px-2 py-1">
                  <i class="pi pi-check-circle text-xs opacity-70"></i>
                  <span class="text-xs">Saved</span>
                </div>
              </transition>
            </div>
          </div>
        </div>
        <div v-else>
          <Skeleton height="2.5rem" class="rounded-lg" />
        </div>
      </div>

      <div class="flex-1 overflow-y-auto bg-surface-50/50 dark:bg-surface-900 p-6 custom-scrollbar">
        <div v-if="isLoading" class="space-y-4">
          <div v-for="i in 3" :key="i">
            <Skeleton height="3rem" class="mb-2 rounded-lg" />
            <div class="grid grid-cols-2 gap-4 mt-2">
              <Skeleton height="4rem" class="rounded-lg" />
              <Skeleton height="4rem" class="rounded-lg" />
            </div>
          </div>
        </div>

        <div v-else-if="filteredCategories.length" class="space-y-4">
          <Accordion
            :value="openPanels"
            multiple
            :pt="{ root: { class: 'bg-transparent flex flex-col gap-4' } }"
          >
            <AccordionPanel
              v-for="cat in filteredCategories"
              :key="cat.key"
              :value="cat.key"
              :pt="{
                root: {
                  class:
                    'border border-surface-200 dark:border-surface-700 rounded-xl bg-white dark:bg-surface-800 overflow-hidden shadow-sm transition-all duration-200',
                },
              }"
            >
              <AccordionHeader
                :pt="{
                  root: {
                    class:
                      'bg-white dark:bg-surface-800 hover:bg-surface-50 dark:hover:bg-surface-700 px-5 py-4 transition-colors cursor-pointer select-none',
                  },
                  toggleIcon: { class: 'text-surface-400' },
                }"
              >
                <div class="flex items-center justify-between w-full mr-3">
                  <div class="flex items-center gap-2.5">
                    <span class="font-bold text-surface-700 dark:text-surface-100 text-base">
                      {{ cat.label }}
                    </span>
                    <Badge
                      :value="cat.items.length"
                      severity="contrast"
                      size="small"
                      class="opacity-50 scale-90"
                    />
                  </div>

                  <div
                    class="flex items-center gap-3 z-10"
                    :class="{ 'pointer-events-none opacity-50': isMutating }"
                    @click.stop
                  >
                    <span
                      class="text-xs font-medium text-surface-400 hidden sm:block hover:text-surface-600 dark:hover:text-surface-300"
                    >
                      {{ isCategoryFullySelected(cat) ? 'Deselect All' : 'Select All' }}
                    </span>
                    <ToggleSwitch
                      :model-value="isCategoryFullySelected(cat)"
                      :disabled="false"
                      class="scale-90"
                      @update:model-value="(v) => toggleCategory(cat, v)"
                    />
                  </div>
                </div>
              </AccordionHeader>

              <AccordionContent
                :pt="{
                  content: {
                    class:
                      'p-4 bg-surface-50/30 dark:bg-surface-900/30 border-t border-surface-100 dark:border-surface-700',
                  },
                }"
              >
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div
                    v-for="perm in cat.items"
                    :key="perm.id"
                    class="group relative p-3 flex items-start gap-3 rounded-lg border transition-all duration-200 select-none"
                    :class="[
                      // 1. ASSIGNED (Blue)
                      perm.checked
                        ? 'bg-primary-50 dark:bg-primary-500/10 border-primary-200 dark:border-primary-700 ring-1 ring-primary-200 dark:ring-primary-800 cursor-pointer'
                        : '',

                      // 2. UNASSIGNED (White)
                      !perm.checked
                        ? 'bg-white dark:bg-surface-800 border-surface-200 dark:border-surface-700 hover:border-primary-300 dark:hover:border-primary-500 cursor-pointer hover:shadow-sm'
                        : '',

                      isMutating ? 'pointer-events-none' : '',
                    ]"
                    @click="onPermissionClick(perm)"
                  >
                    <div class="mt-0.5 relative">
                      <ToggleSwitch
                        :model-value="perm.checked"
                        class="scale-75 shrink-0 pointer-events-none"
                      />
                    </div>

                    <div class="flex flex-col min-w-0 w-full">
                      <div class="flex items-center justify-between gap-2">
                        <span
                          class="text-sm font-semibold truncate"
                          :class="
                            perm.checked
                              ? 'text-surface-900 dark:text-surface-0'
                              : 'text-surface-600 dark:text-surface-400'
                          "
                        >
                          {{ perm.name }}
                        </span>
                        <span
                          v-if="perm.checked"
                          class="shrink-0 inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wide bg-primary-100 dark:bg-primary-500/20 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-700"
                        >
                          Assigned
                        </span>
                      </div>
                      <span
                        class="text-xs text-surface-500 dark:text-surface-400 leading-snug line-clamp-2 mt-0.5"
                      >
                        {{ perm.description }}
                      </span>
                    </div>
                  </div>
                </div>
              </AccordionContent>
            </AccordionPanel>
          </Accordion>
        </div>

        <div v-else class="flex flex-col items-center justify-center h-full py-12 text-surface-400">
          <div
            class="w-16 h-16 bg-surface-100 dark:bg-surface-800 rounded-full flex items-center justify-center mb-4"
          >
            <i class="pi pi-search text-2xl opacity-50"></i>
          </div>
          <p class="font-medium text-surface-600 dark:text-surface-300">No permissions found</p>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end w-full mt-4">
        <Button
          label="Done"
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
  import Accordion from 'primevue/accordion';
  import AccordionContent from 'primevue/accordioncontent';
  import AccordionHeader from 'primevue/accordionheader';
  import AccordionPanel from 'primevue/accordionpanel';
  import Badge from 'primevue/badge';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Skeleton from 'primevue/skeleton';
  import ToggleSwitch from 'primevue/toggleswitch';

  import { useRolePermissions } from '@/composables/query/roles/useRolePermissions';
  import { computed, ref, watch } from 'vue';

  interface Props {
    visible: boolean;
    roleId: string | null;
  }

  const props = defineProps<Props>();
  const emit = defineEmits(['update:visible']);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  const roleIdRef = computed(() => props.roleId);
  const { categories, togglePermission, toggleCategory, permissionsQuery, isMutating } =
    useRolePermissions(roleIdRef);

  const isLoading = computed(() => permissionsQuery.isLoading.value);

  const search = ref('');

  const filteredCategories = computed(() => {
    const q = search.value.trim().toLowerCase();
    if (!q) return categories.value;

    return categories.value
      .map((cat) => {
        const filtered = cat.items.filter(
          (p) =>
            p.key.toLowerCase().includes(q) ||
            p.name.toLowerCase().includes(q) ||
            (p.description ?? '').toLowerCase().includes(q),
        );
        if (!filtered.length) return null;
        return { ...cat, items: filtered };
      })
      .filter(Boolean) as any[];
  });

  const totalVisible = computed(() =>
    filteredCategories.value.reduce((s, c) => s + c.items.length, 0),
  );

  const openPanels = ref<string[]>([]);

  watch(
    () => categories.value.length,
    (len) => {
      if (len > 0 && openPanels.value.length === 0) {
        openPanels.value = [categories.value[0]!.key];
      }
    },
    { immediate: true },
  );

  watch(search, (val) => {
    if (val && filteredCategories.value.length > 0) {
      openPanels.value = filteredCategories.value.map((c) => c.key);
    }
  });

  function isCategoryFullySelected(category: any) {
    return category.items.length > 0 && category.items.every((p: any) => p.checked);
  }

  function onPermissionClick(perm: any) {
    if (isMutating.value) return;
    togglePermission(perm.id, !perm.checked);
  }

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
