<template>
  <Dialog
    v-model:visible="visible"
    header="Role Details"
    :modal="true"
    class="w-full md:w-2/3"
    @hide="onClose"
  >
    <div v-if="role" class="space-y-6">
      <!-- Header Card -->
      <div
        class="rounded-lg p-6 text-white"
        :style="{ backgroundColor: role.colorCode || '#3B82F6' }"
      >
        <div class="flex items-start gap-4">
          <div
            v-if="role.iconClass"
            class="w-16 h-16 bg-white bg-opacity-20 rounded-lg flex items-center justify-center text-3xl"
          >
            <i :class="role.iconClass" />
          </div>
          <div class="flex-1">
            <h3 class="text-2xl font-bold mb-2">{{ role.name }}</h3>
            <div class="flex gap-2 flex-wrap">
              <Tag
                :value="role.isSystemRole ? '🔒 System' : '🏥 Clinic'"
                :severity="role.isSystemRole ? 'info' : 'success'"
              />
              <Tag
                :value="role.isDeleted ? '🗑️ Deleted' : '✓ Active'"
                :severity="role.isDeleted ? 'danger' : 'success'"
              />
              <Tag v-if="role.isTemplate" value="📋 Template" severity="warning" />
            </div>
          </div>
        </div>
      </div>

      <!-- Description -->
      <div>
        <h4 class="font-semibold text-gray-900 mb-2">Description</h4>
        <p class="text-gray-600">{{ role.description || 'No description provided' }}</p>
      </div>

      <!-- Statistics -->
      <div class="grid grid-cols-3 gap-4">
        <div class="bg-blue-50 rounded-lg p-4 text-center">
          <div class="text-2xl font-bold text-blue-600 mb-1">{{ role.userCount }}</div>
          <div class="text-sm text-gray-600">Users Assigned</div>
        </div>
        <div class="bg-purple-50 rounded-lg p-4 text-center">
          <div class="text-2xl font-bold text-purple-600 mb-1">{{ role.permissionCount }}</div>
          <div class="text-sm text-gray-600">Permissions</div>
        </div>
        <div class="bg-green-50 rounded-lg p-4 text-center">
          <div class="text-2xl font-bold text-green-600 mb-1">
            {{ role.displayOrder }}
          </div>
          <div class="text-sm text-gray-600">Display Order</div>
        </div>
      </div>

      <!-- Organization Info -->
      <div>
        <h4 class="font-semibold text-gray-900 mb-3">Organization</h4>
        <div class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span class="text-gray-600">Clinic:</span>
            <div class="font-medium text-gray-900">{{ role.clinicName || 'N/A' }}</div>
          </div>
          <div>
            <span class="text-gray-600">Template:</span>
            <div class="font-medium text-gray-900">{{ role.templateRoleId ? 'Yes' : 'No' }}</div>
          </div>
        </div>
      </div>

      <!-- Audit Information -->
      <div>
        <h4 class="font-semibold text-gray-900 mb-3">Audit Information</h4>
        <div class="grid grid-cols-2 gap-4 text-sm space-y-3">
          <div>
            <span class="text-gray-600">Created By:</span>
            <div class="font-medium text-gray-900">
              {{ role.createdByUserName || role.createdBy || 'N/A' }}
            </div>
          </div>
          <div>
            <span class="text-gray-600">Created At:</span>
            <div class="font-medium text-gray-900">{{ formatDateTime(role.createdAt) }}</div>
          </div>
          <div v-if="role.updatedAt">
            <span class="text-gray-600">Last Modified By:</span>
            <div class="font-medium text-gray-900">
              {{ role.updatedByUserName || role.updatedBy || 'Never' }}
            </div>
          </div>
          <div v-if="role.updatedAt">
            <span class="text-gray-600">Last Modified At:</span>
            <div class="font-medium text-gray-900">{{ formatDateTime(role.updatedAt) }}</div>
          </div>
          <div v-if="role.isDeleted">
            <span class="text-gray-600">Deleted By:</span>
            <div class="font-medium text-gray-900">
              {{ role.deletedByUserName || role.deletedBy || 'N/A' }}
            </div>
          </div>
          <div v-if="role.isDeleted">
            <span class="text-gray-600">Deleted At:</span>
            <div class="font-medium text-gray-900">
              {{ role.deletedAt ? formatDateTime(role.deletedAt) : 'N/A' }}
            </div>
          </div>
        </div>
      </div>

      <!-- IDs -->
      <div class="bg-gray-50 rounded-lg p-4">
        <h4 class="font-semibold text-gray-900 mb-3">Identifiers</h4>
        <div class="space-y-2 font-mono text-sm">
          <div class="flex items-center justify-between">
            <span class="text-gray-600">Role ID:</span>
            <Button
              :label="role.id"
              text
              severity="info"
              @click="copyToClipboard(role.id)"
              class="justify-start"
            />
          </div>
          <div v-if="role.clinicId" class="flex items-center justify-between">
            <span class="text-gray-600">Clinic ID:</span>
            <Button
              :label="role.clinicId"
              text
              severity="info"
              @click="copyToClipboard(role.clinicId)"
              class="justify-start"
            />
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <Button label="Close" @click="onClose" />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { RoleResponseDto } from '@/types/backend';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Tag from 'primevue/tag';

interface Props {
  visible: boolean;
  role?: RoleResponseDto | null;
}

const props = withDefaults(defineProps<Props>(), {
  role: null,
});

const emit = defineEmits<{
  'update:visible': [value: boolean];
}>();

const visible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});

const { role } = props;

const onClose = () => {
  visible.value = false;
};

const formatDateTime = (date: string | Date): string => {
  return new Date(date).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const copyToClipboard = (text: string) => {
  navigator.clipboard.writeText(text);
};
</script>