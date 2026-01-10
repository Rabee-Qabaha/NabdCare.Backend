// src/components/Clinic/ClinicTable.vue
<template>
  <div class="card p-0 border-0 shadow-none h-full">
    <DataTable
      :value="virtualClinics"
      scrollable
      scrollHeight="calc(100vh - 21rem)"
      :virtualScrollerOptions="{
        lazy: true,
        onLazyLoad: onLazyLoad,
        itemSize: 60,
        delay: 200,
        showLoader: true,
        loading: loading,
        numToleratedItems: 10,
      }"
      dataKey="id"
      @sort="onSort"
      tableStyle="min-width: 85rem"
      class="p-datatable-sm"
      :rowClass="rowClass"
    >
      <template #empty>
        <div class="text-center p-8">
          <i class="pi pi-folder-open text-4xl text-surface-400 mb-2"></i>
          <p class="text-surface-500">No clinics found.</p>
        </div>
      </template>

      <Column field="name" header="Clinic" sortable style="width: 20%; min-width: 200px">
        <template #loading><Skeleton width="100%" height="2rem" /></template>
        <template #body="{ data }">
          <div v-if="data" class="flex items-center gap-3 py-1">
            <Avatar
              :image="data.logoUrl || undefined"
              :label="!data.logoUrl ? data.name?.substring(0, 2).toUpperCase() : undefined"
              class="bg-primary-50 text-primary-600 font-bold shrink-0"
              shape="circle"
            />
            <div class="flex flex-col overflow-hidden">
              <span class="font-bold text-surface-900 dark:text-surface-0 truncate">
                {{ data.name }}
              </span>
              <a
                :href="`https://${data.slug}.nabd.care`"
                target="_blank"
                class="text-xs text-primary-600 hover:underline truncate"
                @click.stop
              >
                {{ data.slug }}
              </a>
            </div>
          </div>
          <Skeleton v-else width="100%" height="2rem" />
        </template>
      </Column>

      <Column field="email" header="Email" sortable style="width: 15%">
        <template #loading><Skeleton width="80%" height="1rem" /></template>
        <template #body="{ data }">
          <span v-if="data" class="text-sm truncate" :title="data.email">{{ data.email }}</span>
          <Skeleton v-else width="80%" height="1rem" />
        </template>
      </Column>

      <Column field="phone" header="Phone" sortable style="width: 12%">
        <template #loading><Skeleton width="60%" height="1rem" /></template>
        <template #body="{ data }">
          <span v-if="data" class="text-sm font-mono text-surface-600 dark:text-surface-400">
            {{ data.phone || '-' }}
          </span>
          <Skeleton v-else width="60%" height="1rem" />
        </template>
      </Column>

      <Column field="status" header="Status" sortable style="width: 10%">
        <template #loading><Skeleton width="4rem" height="1.5rem" /></template>
        <template #body="{ data }">
          <Tag
            v-if="data"
            :severity="getStatusSeverity(data.status)"
            :value="data.status"
            class="uppercase text-[10px]"
          />
          <Skeleton v-else width="4rem" height="1.5rem" />
        </template>
      </Column>

      <Column field="subscriptionType" header="Plan" style="width: 15%">
        <template #loading><Skeleton width="70%" height="1rem" /></template>
        <template #body="{ data }">
          <div v-if="data" class="flex flex-col text-sm">
            <span class="font-medium text-surface-700 dark:text-surface-200">
              {{ data.subscriptionType === 0 ? 'Monthly' : 'Yearly' }}
            </span>
            <span class="text-xs text-surface-500">
              Exp: {{ formatDate(data.subscriptionEndDate) }}
            </span>
          </div>
          <Skeleton v-else width="70%" height="1rem" />
        </template>
      </Column>

      <Column field="subscriptionFee" header="Fee" sortable dataType="numeric" style="width: 10%">
        <template #loading><Skeleton width="3rem" height="1rem" /></template>
        <template #body="{ data }">
          <span v-if="data" class="font-mono text-sm">
            {{ formatClinicCurrency(data.subscriptionFee, data.settings) }}
          </span>
          <Skeleton v-else width="3rem" height="1rem" />
        </template>
      </Column>

      <Column field="createdAt" header="Joined" sortable dataType="date" style="width: 12%">
        <template #loading><Skeleton width="5rem" height="1rem" /></template>
        <template #body="{ data }">
          <span v-if="data" class="text-sm text-surface-500">{{ formatDate(data.createdAt) }}</span>
          <Skeleton v-else width="5rem" height="1rem" />
        </template>
      </Column>

      <Column header="" alignFrozen="right" frozen style="width: 4rem">
        <template #loading><Skeleton shape="circle" size="2rem" /></template>
        <template #body="{ data }">
          <div v-if="data" class="flex justify-center">
            <Button
              type="button"
              icon="pi pi-ellipsis-v"
              text
              rounded
              severity="secondary"
              @click="toggleMenu($event, data)"
              aria-haspopup="true"
              aria-controls="overlay_menu"
            />
          </div>
          <Skeleton v-else shape="circle" size="2rem" />
        </template>
      </Column>
    </DataTable>

    <Menu ref="menu" id="overlay_menu" :model="menuItems" :popup="true" />
  </div>
</template>

<script setup lang="ts">
  import { SubscriptionStatus, type ClinicResponseDto } from '@/types/backend';
  import { formatClinicCurrency, formatDate } from '@/utils/uiHelpers';
  import Avatar from 'primevue/avatar';
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import DataTable from 'primevue/datatable';
  import Menu from 'primevue/menu';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';
  import { ref } from 'vue';

  defineProps<{
    virtualClinics: (ClinicResponseDto | undefined)[];
    loading: boolean;
    filters: any;
  }>();

  const emit = defineEmits(['lazy-load', 'sort', 'refresh', 'action']);

  const menu = ref();
  const menuItems = ref<any[]>([]);

  // ✅ Updated Row Styling Function
  const rowClass = (data: ClinicResponseDto) => {
    if (data && data.isDeleted) {
      // We use !bg-red-100 to force the background color, overriding default stripes.
      // We also set a specific darker hover state.
      return '!bg-red-100 hover:!bg-red-200 dark:!bg-red-900/40 dark:hover:!bg-red-900/60 transition-colors';
    }
    return '';
  };

  function toggleMenu(event: any, clinic: ClinicResponseDto) {
    const isDeleted = clinic.isDeleted;
    const isActive = clinic.status === SubscriptionStatus.Active;

    menuItems.value = [
      {
        label: 'Actions',
        items: [
          {
            label: 'Edit',
            icon: 'pi pi-pencil',
            visible: !isDeleted,
            command: () => emit('action', { type: 'edit', clinic }),
          },
          {
            label: 'Subscription',
            icon: 'pi pi-credit-card',
            visible: !isDeleted,
            command: () => emit('action', { type: 'manage-subscription', clinic }),
          },
          {
            label: 'Branches',
            icon: 'pi pi-sitemap',
            visible: !isDeleted,
            command: () => emit('action', { type: 'manage-branches', clinic }),
          },
          {
            label: isActive ? 'Suspend' : 'Activate',
            icon: isActive ? 'pi pi-pause' : 'pi pi-play',
            visible: !isDeleted,
            class: isActive ? 'text-orange-600' : 'text-green-600',
            command: () => emit('action', { type: 'toggle-status', clinic }),
          },
          { separator: true, visible: !isDeleted },
          {
            label: 'Trash',
            icon: 'pi pi-trash',
            visible: !isDeleted,
            class: 'text-red-500',
            command: () => emit('action', { type: 'delete', clinic }),
          },
          {
            label: 'Restore',
            icon: 'pi pi-undo',
            visible: isDeleted,
            class: 'text-green-600',
            command: () => emit('action', { type: 'restore', clinic }),
          },
          {
            label: 'Delete',
            icon: 'pi pi-times-circle',
            visible: isDeleted,
            class: 'text-red-600',
            command: () => emit('action', { type: 'hard-delete', clinic }),
          },
        ],
      },
    ];
    menu.value.toggle(event);
  }

  const StatusConfig: Record<
    string,
    { severity: 'success' | 'warn' | 'danger' | 'info' | 'secondary' | 'contrast' }
  > = {
    [SubscriptionStatus.Active]: { severity: 'success' },
    [SubscriptionStatus.Inactive]: { severity: 'warn' },
    [SubscriptionStatus.Expired]: { severity: 'danger' },
    [SubscriptionStatus.Cancelled]: { severity: 'secondary' },
    [SubscriptionStatus.Suspended]: { severity: 'danger' },
    [SubscriptionStatus.Trial]: { severity: 'info' },
    [SubscriptionStatus.PastDue]: { severity: 'warn' },
    [SubscriptionStatus.Future]: { severity: 'contrast' },
  };

  const onLazyLoad = (e: any) => emit('lazy-load', e);
  const onSort = (e: any) => emit('sort', e);

  function getStatusSeverity(status: string) {
    return StatusConfig[status]?.severity || 'secondary';
  }
</script>
