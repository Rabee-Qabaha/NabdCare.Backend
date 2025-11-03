<script setup lang="ts">
import { computed, ref } from "vue";
import { useResourceAuthorization } from "@/composables/query/authorization/useResourceAuthorization";
import Dialog from "primevue/dialog";
import Button from "primevue/button";
import Tag from "primevue/tag";
import Skeleton from "primevue/skeleton";
import type { UserResponseDto } from "@/types/backend";

/**
 * Authorization Status Debug Dialog
 * Location: src/components/Authorization/AuthorizationStatusDialog.vue
 *
 * Shows detailed authorization check results for a user
 * Useful for testing and debugging authorization policies
 *
 * Usage:
 * ```vue
 * <AuthorizationStatusDialog
 *   v-if="selectedUser"
 *   :user="selectedUser"
 *   @close="selectedUser = null"
 * />
 * ```
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

interface Props {
  user: UserResponseDto;
}

interface Emits {
  (e: "close"): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// ✅ FIX: Use reactive ref instead of literal true
const isVisible = ref(true);

// Check all possible actions
const authView = useResourceAuthorization("user", props.user.id, "view");
const authEdit = useResourceAuthorization("user", props.user.id, "edit");
const authDelete = useResourceAuthorization("user", props.user.id, "delete");

const actions = computed(() => [
  { action: "view", ...authView },
  { action: "edit", ...authEdit },
  { action: "delete", ...authDelete },
]);

// Handle dialog close
function handleClose() {
  isVisible.value = false;
  emit("close");
}
</script>

<template>
  <!-- ✅ FIX: Use reactive ref isVisible instead of literal true -->
  <Dialog
    v-model:visible="isVisible"
    header="Authorization Status"
    modal
    style="width: 90vw; max-width: 600px"
    @update:visible="(val) => !val && handleClose()"
  >
    <div class="space-y-4">
      <!-- User Info -->
      <div class="bg-surface-50 dark:bg-surface-800 p-4 rounded-lg">
        <p class="text-sm text-surface-600 dark:text-surface-400">User</p>
        <p class="font-semibold text-lg">{{ user.fullName }}</p>
        <p class="text-sm text-surface-500">{{ user.email }}</p>
      </div>

      <!-- Authorization Results -->
      <div class="space-y-2">
        <h4 class="font-semibold">Authorization Checks</h4>

        <div
          v-for="action in actions"
          :key="action.action"
          class="flex items-center justify-between p-3 border border-surface-200 dark:border-surface-700 rounded"
        >
          <div class="flex items-center gap-3 flex-1">
            <span class="capitalize font-medium">{{ action.action }}</span>
            <Skeleton v-if="action.isLoading" width="80px" height="1.5rem" />
            <Tag
              v-else
              :value="action.result?.allowed ? 'Allowed' : 'Denied'"
              :severity="action.result?.allowed ? 'success' : 'danger'"
            />
          </div>

          <div v-if="action.result?.policy" class="text-xs text-surface-500">
            {{ action.result.policy }}
          </div>
        </div>
      </div>

      <!-- Denial Reasons -->
      <div
        v-if="actions.some((a) => !a.result?.allowed)"
        class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-4 rounded"
      >
        <h5 class="font-semibold text-red-800 dark:text-red-200 mb-2">
          Denial Reasons
        </h5>
        <ul class="space-y-1 text-sm text-red-700 dark:text-red-300">
          <li
            v-for="action in actions.filter((a) => !a.result?.allowed)"
            :key="action.action"
          >
            <strong>{{ action.action }}:</strong> {{ action.result?.reason }}
          </li>
        </ul>
      </div>
    </div>

    <template #footer>
      <Button label="Close" icon="pi pi-times" text @click="handleClose" />
    </template>
  </Dialog>
</template>
