<template>
  <Dialog
    v-model:visible="visible"
    :header="title"
    :modal="true"
    class="w-full md:w-1/2"
    @hide="onClose"
  >
    <div class="space-y-4">
      <p class="text-gray-600">{{ message }}</p>

      <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4 flex gap-3">
        <i class="pi pi-exclamation-triangle text-yellow-600 text-lg flex-shrink-0 mt-0.5" />
        <span class="text-sm text-yellow-800">This action cannot be undone.</span>
      </div>
    </div>

    <template #footer>
      <Button label="Cancel" @click="onClose" severity="secondary" />
      <Button label="Delete" @click="confirmDelete" severity="danger" :loading="isDeleting" />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';

interface Props {
  visible: boolean;
  title?: string;
  message?: string;
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Confirm Delete',
  message: 'Are you sure you want to delete this item?',
});

const emit = defineEmits<{
  'update:visible': [value: boolean];
  'confirm': [];
}>();

const isDeleting = ref(false);

const visible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});

const onClose = () => {
  visible.value = false;
};

const confirmDelete = async () => {
  isDeleting.value = true;
  try {
    emit('confirm');
    onClose();
  } finally {
    isDeleting.value = false;
  }
};
</script>