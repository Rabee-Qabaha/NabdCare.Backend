// src/views/pages/admin/Users.vue
<script setup lang="ts">
import { ref, onMounted, computed, watch, onBeforeUnmount } from "vue";
import { useToast } from "primevue/usetoast";
import { useConfirm } from "primevue/useconfirm";
import { useUserStore } from "@/stores/userStore";
import { hasPermission } from "@/utils/permissions";
import UserDialog from "@/components/User/UserDialog.vue";
import ChangePasswordDialog from "@/components/User/ChangePasswordDialog.vue";
import EmptyState from "@/components/EmptyState.vue";
import { formatDate } from "@/utils/uiHelpers";
import { FilterMatchMode, FilterOperator } from "@primevue/core/api";
import type { UserResponseDto } from "@/types/backend";

import InputText from "primevue/inputtext";
import Select from "primevue/select";
import DatePicker from "primevue/datepicker";
import Checkbox from "primevue/checkbox";

const userStore = useUserStore();
const toast = useToast();
const confirm = useConfirm();

// --- Filter Options ---
const statusOptions = ref([
  { label: "Active", value: true, severity: "success" },
  { label: "Inactive", value: false, severity: "danger" },
]);

// --- Initial Filter Structure ---
const initialFilters = {
  global: { value: null, matchMode: FilterMatchMode.CONTAINS },
  fullName: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
  },
  email: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
  },
  roleName: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
  },
  clinicName: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
  },
  isActive: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
  },
  createdAt: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
  },
};

// --- State ---
const filters = ref(structuredClone(initialFilters));
const filtersDialogVisible = ref(false);
const userDialog = ref(false);
const changePasswordDialog = ref(false);
const deleteUserDialog = ref(false);
const bulkActionDialog = ref(false);

const currentBulkAction = ref<"activate" | "deactivate" | "delete" | null>(
  null
);
const user = ref<Partial<UserResponseDto>>({});
const selectedUserIds = ref<number[]>([]);

// --- Infinite scroll setup ---
const BATCH_SIZE = 20;
const visibleCount = ref(BATCH_SIZE);
const loadingMore = ref(false);
const sentinel = ref<HTMLElement | null>(null);
let observer: IntersectionObserver | null = null;

// --- Permissions ---
const canCreate = computed(() => hasPermission("Users.Create"));
const canEdit = computed(() => hasPermission("Users.Edit"));
const canDelete = computed(() => hasPermission("Users.Delete"));
const canResetPassword = computed(() => hasPermission("Users.ResetPassword"));
const canActivate = computed(() => hasPermission("Users.Activate"));

// --- Fetch users ---
const fetchUsers = async () => {
  try {
    await userStore.fetchUsers();
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message || "Failed to fetch users",
      life: 3000,
    });
  }
};
onMounted(fetchUsers);

// --- CRUD logic ---
function openNew() {
  user.value = {};
  userDialog.value = true;
}
function hideDialog() {
  userDialog.value = false;
}
function editUser(u: UserResponseDto) {
  user.value = { ...u };
  userDialog.value = true;
}
function openChangePasswordDialog(u: UserResponseDto) {
  user.value = { ...u };
  changePasswordDialog.value = true;
}
function confirmDeleteUser(u: UserResponseDto) {
  user.value = { ...u };
  deleteUserDialog.value = true;
}
async function deleteUser() {
  if (!user.value.id) return;
  try {
    await userStore.deleteUser(user.value.id);
    toast.add({
      severity: "success",
      summary: "Deleted",
      detail: "User deleted successfully",
      life: 3000,
    });
    deleteUserDialog.value = false;
    await fetchUsers();
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed to delete user",
      life: 3000,
    });
  }
}
async function hardDeleteUser(u: UserResponseDto) {
  confirm.require({
    message: `Permanently delete ${u.fullName}?`,
    header: "⚠️ Permanent Deletion",
    icon: "pi pi-exclamation-triangle",
    rejectLabel: "Cancel",
    acceptLabel: "Delete Permanently",
    rejectProps: { outlined: true },
    acceptProps: { severity: "danger" },
    accept: async () => {
      try {
        await userStore.hardDeleteUser(u.id);
        toast.add({
          severity: "success",
          summary: "Deleted",
          detail: "User permanently deleted",
          life: 3000,
        });
        await fetchUsers();
      } catch (err: any) {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: err.message || "Failed",
          life: 3000,
        });
      }
    },
  });
}
async function toggleUserStatus(u: UserResponseDto) {
  try {
    if (u.isActive) {
      await userStore.deactivateUser(u.id);
      toast.add({
        severity: "success",
        summary: "Deactivated",
        detail: `${u.fullName} deactivated`,
        life: 3000,
      });
    } else {
      await userStore.activateUser(u.id);
      toast.add({
        severity: "success",
        summary: "Activated",
        detail: `${u.fullName} activated`,
        life: 3000,
      });
    }
    await fetchUsers();
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed",
      life: 3000,
    });
  }
}
async function saveUser(newUser: any) {
  try {
    if (!newUser.id) {
      await userStore.createUser(newUser);
      toast.add({
        severity: "success",
        summary: "Created",
        detail: `User ${newUser.email} created`,
        life: 3000,
      });
    } else {
      await userStore.updateUser(newUser.id, newUser);
      toast.add({
        severity: "success",
        summary: "Updated",
        detail: "User updated",
        life: 3000,
      });
    }
    userDialog.value = false;
    await fetchUsers();
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: userStore.error || err.message || "Save failed",
      life: 3000,
    });
  }
}
async function handleChangePassword(data: { newPassword: string }) {
  if (!user.value.id) return;
  try {
    await userStore.adminResetPassword(user.value.id, data.newPassword);
    toast.add({
      severity: "success",
      summary: "Success",
      detail: "Password reset successfully",
      life: 3000,
    });
    changePasswordDialog.value = false;
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed",
      life: 3000,
    });
  }
}

// --- Filters ---
function resetFilters() {
  filters.value = structuredClone(initialFilters);
  visibleCount.value = BATCH_SIZE;
}
function openFilters() {
  filtersDialogVisible.value = true;
}
function applyFilters() {
  filtersDialogVisible.value = false;
  visibleCount.value = BATCH_SIZE;
}
function clearFilters() {
  resetFilters();
  filtersDialogVisible.value = false;
}
function contains(h: any, n: any) {
  if (!n) return true;
  return String(h ?? "")
    .toLowerCase()
    .includes(String(n).toLowerCase());
}
function equals(a: any, b: any) {
  if (b === null || b === undefined) return true;
  return a === b;
}
function sameDate(a: any, b: any) {
  if (!b) return true;
  if (!a) return false;
  const da = new Date(a);
  const db = new Date(b);
  return da.toDateString() === db.toDateString();
}
const filteredUsers = computed(() => {
  const list = userStore.users || [];
  const f = filters.value;
  const global = f.global.value;
  const name = f.fullName.constraints[0].value;
  const email = f.email.constraints[0].value;
  const role = f.roleName.constraints[0].value;
  const clinic = f.clinicName.constraints[0].value;
  const status = f.isActive.constraints[0].value;
  const created = f.createdAt.constraints[0].value;

  return list.filter((u) => {
    const globalPass = global
      ? [u.fullName, u.email, u.roleName, u.clinicName].some((val) =>
          contains(val, global)
        )
      : true;
    return (
      globalPass &&
      contains(u.fullName, name) &&
      contains(u.email, email) &&
      contains(u.roleName, role) &&
      contains(u.clinicName, clinic) &&
      equals(u.isActive, status) &&
      sameDate(u.createdAt, created)
    );
  });
});
const visibleUsers = computed(() =>
  filteredUsers.value.slice(0, visibleCount.value)
);

// --- Infinite Scroll ---
function setupObserver() {
  if (observer) observer.disconnect();
  if (!sentinel.value) return;
  observer = new IntersectionObserver(async (entries) => {
    const [entry] = entries;
    if (!entry.isIntersecting) return;
    if (visibleCount.value < filteredUsers.value.length) {
      loadingMore.value = true;
      await new Promise((r) => setTimeout(r, 400));
      visibleCount.value = Math.min(
        visibleCount.value + BATCH_SIZE,
        filteredUsers.value.length
      );
      loadingMore.value = false;
    }
  });
  observer.observe(sentinel.value);
}
onMounted(setupObserver);
onBeforeUnmount(() => observer?.disconnect());
watch(sentinel, setupObserver);

// --- Bulk Actions ---
const bulkVisible = computed(() => selectedUserIds.value.length > 0);
const bulkDialogData = computed(() => {
  const count = selectedUserIds.value.length;
  switch (currentBulkAction.value) {
    case "activate":
      return {
        header: "Confirm Bulk Activation",
        actionLabel: "Activate",
        actionIcon: "pi pi-check",
        severity: "success",
        message: `Activate ${count} selected users?`,
      };
    case "deactivate":
      return {
        header: "Confirm Bulk Deactivation",
        actionLabel: "Deactivate",
        actionIcon: "pi pi-ban",
        severity: "warning",
        message: `Deactivate ${count} selected users?`,
      };
    case "delete":
      return {
        header: "Confirm Bulk Deletion",
        actionLabel: "Delete",
        actionIcon: "pi pi-trash",
        severity: "danger",
        message: `Delete ${count} selected users?`,
      };
    default:
      return {
        header: "",
        actionLabel: "",
        actionIcon: "",
        severity: "secondary",
        message: "",
      };
  }
});
function bulkActivate() {
  currentBulkAction.value = "activate";
  bulkActionDialog.value = true;
}
function bulkDeactivate() {
  currentBulkAction.value = "deactivate";
  bulkActionDialog.value = true;
}
function bulkDelete() {
  currentBulkAction.value = "delete";
  bulkActionDialog.value = true;
}
async function handleBulkActionConfirm() {
  const ids = [...selectedUserIds.value];
  const action = currentBulkAction.value;
  if (!action || !ids.length) return;
  bulkActionDialog.value = false;
  const actionFn =
    action === "activate"
      ? userStore.activateUser
      : action === "deactivate"
        ? userStore.deactivateUser
        : userStore.deleteUser;
  try {
    await Promise.all(ids.map((id) => actionFn(id)));
    toast.add({
      severity: "success",
      summary: "Success",
      detail: `${ids.length} users processed`,
      life: 3000,
    });
    await fetchUsers();
    selectedUserIds.value = [];
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Bulk action failed",
      life: 3000,
    });
  }
  currentBulkAction.value = null;
}
</script>

<template>
  <div class="p-4 md:p-6 space-y-4">
    <div>
      <h2 class="text-2xl md:text-3xl font-bold">Users Management</h2>
      <p class="text-600">Manage all users across all clinics</p>
    </div>

    <!-- Toolbar -->
    <div
      class="flex flex-col md:flex-row md:items-center md:justify-between gap-2"
    >
      <div class="flex flex-wrap gap-2">
        <Button
          v-if="canCreate"
          label="Create User"
          icon="pi pi-plus"
          @click="openNew"
        />
        <template v-if="bulkVisible">
          <Button
            v-if="canActivate"
            label="Activate Selected"
            icon="pi pi-check"
            outlined
            severity="success"
            @click="bulkActivate"
          />
          <Button
            v-if="canActivate"
            label="Deactivate Selected"
            icon="pi pi-ban"
            outlined
            severity="warning"
            @click="bulkDeactivate"
          />
          <Button
            v-if="canDelete"
            label="Delete Selected"
            icon="pi pi-trash"
            outlined
            severity="danger"
            @click="bulkDelete"
          />
        </template>
      </div>

      <div class="flex flex-wrap gap-2">
        <Button
          icon="pi pi-sliders-h"
          label="Filters"
          outlined
          @click="openFilters"
        />
        <Button
          icon="pi pi-filter-slash"
          label="Clear Filters"
          outlined
          @click="resetFilters"
        />
        <Button
          icon="pi pi-refresh"
          label="Refresh"
          outlined
          @click="fetchUsers"
          :loading="userStore.loading"
        />
      </div>
    </div>

    <!-- Search -->
    <div>
      <IconField class="w-full">
        <InputIcon><i class="pi pi-search" /></InputIcon>
        <InputText
          v-model="filters.global.value"
          placeholder="Search users..."
          class="w-full"
        />
      </IconField>
    </div>

    <!-- Skeleton loader -->
    <div
      v-if="userStore.loading"
      class="grid gap-4 md:gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
    >
      <Card
        v-for="n in 8"
        :key="n"
        class="shadow-md flex flex-col justify-between h-full"
      >
        <!-- Header (Title section) -->
        <template #title>
          <div class="flex items-start justify-between">
            <div class="flex items-center gap-2">
              <Skeleton shape="circle" size="2.5rem" animation="wave" />
              <div class="flex flex-col gap-2">
                <Skeleton width="8rem" height="1rem" animation="wave" />
                <Skeleton width="6rem" height="0.8rem" animation="wave" />
              </div>
            </div>
            <Skeleton
              width="1.2rem"
              height="1.2rem"
              borderRadius="6px"
              animation="wave"
            />
          </div>
        </template>

        <!-- Content (matches info layout of real card) -->
        <template #content>
          <div class="flex flex-col gap-2 mt-2 text-sm">
            <div class="flex justify-between items-center">
              <Skeleton width="4rem" height="0.9rem" animation="wave" />
              <Skeleton width="5rem" height="0.9rem" animation="wave" />
            </div>
            <div class="flex justify-between items-center">
              <Skeleton width="4rem" height="0.9rem" animation="wave" />
              <Skeleton width="5rem" height="0.9rem" animation="wave" />
            </div>
            <div class="flex justify-between items-center">
              <Skeleton width="4rem" height="0.9rem" animation="wave" />
              <Skeleton width="5rem" height="0.9rem" />
            </div>
            <div class="flex justify-between items-center">
              <Skeleton width="4rem" height="0.9rem" animation="wave" />
              <Skeleton width="5rem" height="0.9rem" animation="wave" />
            </div>
          </div>
        </template>

        <!-- Footer (actions area) -->
        <template #footer>
          <div class="flex flex-wrap gap-2 mt-2">
            <Skeleton
              width="2rem"
              height="2rem"
              borderRadius="50%"
              animation="wave"
            />
            <Skeleton
              width="2rem"
              height="2rem"
              borderRadius="50%"
              animation="wave"
            />
            <Skeleton
              width="2rem"
              height="2rem"
              borderRadius="50%"
              animation="wave"
            />
            <Skeleton
              width="2rem"
              height="2rem"
              borderRadius="50%"
              animation="wave"
            />
            <Skeleton
              width="2rem"
              height="2rem"
              borderRadius="50%"
              animation="wave"
            />
          </div>
        </template>
      </Card>
    </div>

    <!-- Users grid -->
    <div
      v-else-if="visibleUsers.length"
      class="grid gap-4 md:gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
    >
      <Card v-for="u in visibleUsers" :key="u.id">
        <template #title>
          <div class="flex items-start justify-between">
            <div class="flex items-center gap-2">
              <Avatar
                :label="u.fullName?.charAt(0)"
                shape="circle"
                :class="u.isActive ? 'bg-green-500' : 'bg-gray-400'"
                class="text-white"
              />
              <div>
                <p class="font-semibold m-0">{{ u.fullName }}</p>
                <p class="text-600 text-sm m-0">{{ u.email }}</p>
              </div>
            </div>
            <Checkbox :value="u.id" v-model="selectedUserIds" />
          </div>
        </template>

        <template #content>
          <div class="flex flex-col gap-1 text-sm">
            <div class="flex justify-between">
              <span>Role:</span>
              <Tag :value="u.roleName" severity="info" />
            </div>
            <div class="flex justify-between">
              <span>Clinic:</span>
              <span>{{ u.clinicName || "No Clinic" }}</span>
            </div>
            <div class="flex justify-between">
              <span>Status:</span>
              <Tag
                :value="u.isActive ? 'Active' : 'Inactive'"
                :severity="u.isActive ? 'success' : 'danger'"
              />
            </div>
            <div class="flex justify-between">
              <span>Created:</span>
              <span>{{ formatDate(u.createdAt) }}</span>
            </div>
          </div>
        </template>

        <template #footer>
          <div class="flex flex-wrap gap-2">
            <Button
              v-if="canEdit"
              icon="pi pi-pencil"
              text
              @click="editUser(u)"
              v-tooltip.top="'Edit user'"
            />
            <Button
              v-if="canActivate"
              :icon="u.isActive ? 'pi pi-ban' : 'pi pi-check'"
              text
              @click="toggleUserStatus(u)"
              v-tooltip.top="u.isActive ? 'Deactivate' : 'Activate'"
            />
            <Button
              v-if="canResetPassword"
              icon="pi pi-key"
              text
              @click="openChangePasswordDialog(u)"
              v-tooltip.top="'Reset password'"
            />
            <Button
              v-if="canDelete"
              icon="pi pi-trash"
              text
              severity="danger"
              @click="confirmDeleteUser(u)"
              v-tooltip.top="'Soft delete'"
            />
            <Button
              v-if="canDelete"
              icon="pi pi-times-circle"
              text
              severity="danger"
              @click="hardDeleteUser(u)"
              v-tooltip.top="'Hard delete'"
            />
          </div>
        </template>
      </Card>
    </div>

    <!-- Empty state -->
    <EmptyState
      v-else
      icon="pi pi-users"
      title="No Users Found"
      description="No users match your filters"
    />

    <div ref="sentinel" class="h-6"></div>
    <div v-if="loadingMore" class="flex justify-center p-4">
      <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
    </div>

    <!-- Filters Dialog -->
    <Dialog
      v-model:visible="filtersDialogVisible"
      header="Filters"
      modal
      :style="{ width: '400px', maxWidth: '95vw' }"
    >
      <div class="grid gap-3">
        <InputText
          v-model="filters.fullName.constraints[0].value"
          placeholder="Full name"
        />
        <InputText
          v-model="filters.email.constraints[0].value"
          placeholder="Email"
        />
        <InputText
          v-model="filters.roleName.constraints[0].value"
          placeholder="Role"
        />
        <InputText
          v-model="filters.clinicName.constraints[0].value"
          placeholder="Clinic"
        />
        <Select
          v-model="filters.isActive.constraints[0].value"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Status"
          showClear
        >
          <template #option="slotProps"
            ><Tag
              :value="slotProps.option.label"
              :severity="slotProps.option.severity"
          /></template>
        </Select>
        <DatePicker
          v-model="filters.createdAt.constraints[0].value"
          dateFormat="mm/dd/yy"
          placeholder="Created date"
          showIcon
          class="w-full"
        />
      </div>
      <template #footer>
        <Button
          icon="pi pi-filter-slash"
          label="Clear"
          outlined
          @click="clearFilters"
        />
        <Button icon="pi pi-check" label="Apply" @click="applyFilters" />
      </template>
    </Dialog>

    <!-- User Dialogs -->
    <UserDialog
      v-model:visible="userDialog"
      :user="user"
      @save="saveUser"
      @cancel="hideDialog"
    />
    <ChangePasswordDialog
      v-model:visible="changePasswordDialog"
      :user="user"
      @save="handleChangePassword"
    />

    <Dialog
      v-model:visible="deleteUserDialog"
      header="Confirm Delete"
      modal
      :style="{ width: '450px' }"
    >
      <p>
        Are you sure you want to delete <b>{{ user.fullName }}</b
        >?
      </p>
      <template #footer>
        <Button label="Cancel" outlined @click="deleteUserDialog = false" />
        <Button label="Delete" severity="danger" @click="deleteUser" />
      </template>
    </Dialog>

    <!-- Bulk Action Dialog -->
    <Dialog
      v-model:visible="bulkActionDialog"
      :header="bulkDialogData.header"
      modal
      :style="{ width: '450px' }"
    >
      <p>{{ bulkDialogData.message }}</p>
      <template #footer>
        <Button label="Cancel" outlined @click="bulkActionDialog = false" />
        <Button
          :label="bulkDialogData.actionLabel"
          :icon="bulkDialogData.actionIcon"
          :severity="bulkDialogData.severity"
          @click="handleBulkActionConfirm"
        />
      </template>
    </Dialog>

    <ConfirmDialog />
  </div>
</template>
