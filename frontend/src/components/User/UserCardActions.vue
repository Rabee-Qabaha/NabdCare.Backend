// src/components/User/UserCardActions.vue
<script setup lang="ts">
  import { useResourceAuthorization } from '@/composables/query/authorization/useResourceAuthorization';
  import type { UserResponseDto } from '@/types/backend';
  import Button from 'primevue/button';
  import { computed } from 'vue';

  /**
   * User Card Action Buttons Component
   * Location: src/components/User/UserCardActions.vue
   *
   * Resource-aware Edit/Delete buttons using ABAC composable
   * Shows buttons based on RBAC permissions AND ABAC resource policies
   *
   * Author: Rabee Qabaha
   * Updated: 2025-11-02
   */

  interface Props {
    user: UserResponseDto;
    canEdit: boolean;
    canDelete: boolean;
    canResetPassword: boolean;
    canActivate: boolean;
  }

  interface Emits {
    (e: 'edit', user: UserResponseDto): void;
    (e: 'delete', user: UserResponseDto): void;
    (e: 'toggle-status', user: UserResponseDto): void;
    (e: 'show-auth', user: UserResponseDto): void;
  }

  const props = defineProps<Props>();
  const emit = defineEmits<Emits>();

  // ✅ ABAC checks for this specific user
  const { allowed: canEditThis, isLoading: isCheckingEdit } = useResourceAuthorization(
    'user',
    props.user.id,
    'edit',
  );

  const { allowed: canDeleteThis, isLoading: isCheckingDelete } = useResourceAuthorization(
    'user',
    props.user.id,
    'delete',
  );

  // Computed: show button if RBAC allows AND ABAC allows
  const showEditButton = computed(() => props.canEdit && canEditThis.value);
  const showDeleteButton = computed(() => props.canDelete && canDeleteThis.value);

  // Computed: show disabled state while loading
  const editButtonDisabled = computed(() => isCheckingEdit.value);
  const deleteButtonDisabled = computed(
    () => isCheckingDelete.value || (!canEditThis.value && !isCheckingEdit.value),
  );

  // Computed: button severity based on authorization
  const editButtonSeverity = computed(() => (canEditThis.value ? 'primary' : 'warning'));
  const deleteButtonSeverity = computed(() => (canDeleteThis.value ? 'danger' : 'danger'));

  // Tooltips
  const editTooltip = computed(() => {
    if (isCheckingEdit.value) return 'Checking permissions...';
    if (!canEditThis.value) return 'You cannot edit this user';
    return 'Edit user';
  });

  const deleteTooltip = computed(() => {
    if (isCheckingDelete.value) return 'Checking permissions...';
    if (!canDeleteThis.value) return 'You cannot delete this user';
    return 'Delete user';
  });
</script>

<template>
  <div class="flex gap-2">
    <!-- Edit Button -->
    <Button
      v-if="showEditButton"
      v-tooltip="editTooltip"
      icon="pi pi-pencil"
      text
      :severity="editButtonSeverity"
      :loading="isCheckingEdit"
      :disabled="editButtonDisabled"
      @click="emit('edit', user)"
    />

    <!-- Edit Button (Disabled - No Permission) -->
    <Button
      v-else-if="canEdit && !isCheckingEdit"
      v-tooltip="'You cannot edit users from other clinics'"
      icon="pi pi-pencil"
      text
      severity="secondary"
      disabled
    />

    <!-- Activate/Deactivate Button -->
    <Button
      v-if="canActivate"
      :icon="user.isActive ? 'pi pi-ban' : 'pi pi-check'"
      text
      @click="emit('toggle-status', user)"
    />

    <!-- Reset Password Button -->
    <Button
      v-if="canResetPassword"
      v-tooltip="'Reset password'"
      icon="pi pi-key"
      text
      @click="emit('show-auth', user)"
    />

    <!-- Delete Button -->
    <Button
      v-if="showDeleteButton"
      v-tooltip="deleteTooltip"
      icon="pi pi-trash"
      text
      :severity="deleteButtonSeverity"
      :loading="isCheckingDelete"
      :disabled="deleteButtonDisabled"
      @click="emit('delete', user)"
    />

    <!-- Delete Button (Disabled - No Permission) -->
    <Button
      v-else-if="canDelete && !isCheckingDelete"
      v-tooltip="'You cannot delete users from other clinics'"
      icon="pi pi-trash"
      text
      severity="secondary"
      disabled
    />
  </div>
</template>
