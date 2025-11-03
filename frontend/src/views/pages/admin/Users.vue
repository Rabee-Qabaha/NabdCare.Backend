<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount, onMounted } from "vue";
import { useToast } from "primevue/usetoast";
import { useConfirm } from "primevue/useconfirm";
import {
  useInfiniteUsersPaged,
  useCreateUser,
  useUpdateUser,
} from "@/composables/query/users/useUsers";
import {
  useResetPassword,
  useActivateUser,
  useDeactivateUser,
  useSoftDeleteUser,
  useHardDeleteUser,
} from "@/composables/query/users/useUserActions";
import { useRoles, useClinics } from "@/composables/query/useDropdownData";
import UserDialog from "@/components/User/UserDialog.vue";
import ChangePasswordDialog from "@/components/User/ChangePasswordDialog.vue";
import EmptyState from "@/components/EmptyState.vue";
import { formatDate } from "@/utils/uiHelpers";
import { usePermission } from "@/composables/usePermission";
import { FilterMatchMode, FilterOperator } from "@primevue/core/api";
import type { UserResponseDto } from "@/types/backend";

// PrimeVue Components
import InputText from "primevue/inputtext";
import Select from "primevue/select";
import DatePicker from "primevue/datepicker";
import Checkbox from "primevue/checkbox";
import Button from "primevue/button";
import Card from "primevue/card";
import Tag from "primevue/tag";
import Avatar from "primevue/avatar";
import Dialog from "primevue/dialog";
import ConfirmDialog from "primevue/confirmdialog";
import ProgressSpinner from "primevue/progressspinner";
import Skeleton from "primevue/skeleton";
import IconField from "primevue/iconfield";
import InputIcon from "primevue/inputicon";

/**
 * Users Management Page
 * Location: src/views/pages/admin/Users.vue
 *
 * Features:
 * ✅ User CRUD operations (create, read, update, delete)
 * ✅ Advanced filtering and global search
 * ✅ Infinite scroll pagination
 * ✅ Bulk actions (activate, deactivate, delete)
 * ✅ Password reset functionality
 * ✅ Checkbox selection
 * ✅ Vue Query + SWR caching (automatic background refresh)
 * ✅ Role-based permission control
 *
 * Architecture:
 * - Vue Query composables for data fetching & mutations
 * - Service layer for API abstraction
 * - Composables for permissions and dropdowns
 * - Client-side filtering and search
 * - Infinite scroll with intersection observer
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

// ========================================
// COMPOSABLES & SERVICES
// ========================================

const toast = useToast();
const confirm = useConfirm();
const { can: canPermission } = usePermission();

// ========================================
// FILTER CONFIGURATION
// ========================================

const statusOptions = ref([
  { label: "Active", value: true, severity: "success" },
  { label: "Inactive", value: false, severity: "danger" },
]);

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

// ========================================
// DROPDOWN DATA (Cached)
// ========================================

const { data: rolesDropdown = [], isLoading: rolesDropdownLoading } =
  useRoles();
const { data: clinicsDropdown = [], isLoading: clinicsDropdownLoading } =
  useClinics();

// ========================================
// STATE MANAGEMENT
// ========================================

const filters = ref(structuredClone(initialFilters));
const filtersDialogVisible = ref(false);
const userDialog = ref(false);
const changePasswordDialog = ref(false);
const deleteUserDialog = ref(false);
const bulkActionDialog = ref(false);
const softDeleteLoading = ref(false);

const currentBulkAction = ref<"activate" | "deactivate" | "delete" | null>(
  null
);
const user = ref<Partial<UserResponseDto>>({});
const selectedUserIds = ref<number[]>([]);
const loadingUserIds = ref<Set<string>>(new Set());
const bulkActionLoading = ref(false);

// ========================================
// INFINITE SCROLL
// ========================================

const BATCH_SIZE = 20;
const visibleCount = ref(BATCH_SIZE);
const loadingMore = ref(false);
const sentinel = ref<HTMLElement | null>(null);
let observer: IntersectionObserver | null = null;

// ========================================
// PERMISSIONS
// ========================================

const canCreate = computed(() => canPermission("Users.Create"));
const canEdit = computed(() => canPermission("Users.Edit"));
const canDelete = computed(() => canPermission("Users.Delete"));
const canResetPassword = computed(() => canPermission("Users.ResetPassword"));
const canActivate = computed(() => canPermission("Users.Activate"));

// ========================================
// QUERIES & MUTATIONS
// ========================================

const {
  data: infiniteData,
  fetchNextPage,
  hasNextPage,
  isFetchingNextPage,
  isFetching,
  refetch,
  error,
  isError,
} = useInfiniteUsersPaged({
  search: "",
});

// CRUD Mutations
const createUserMutation = useCreateUser();
const updateUserMutation = useUpdateUser();

// Action Mutations (from useUserActions)
const softDeleteUserMutation = useSoftDeleteUser();
const hardDeleteUserMutation = useHardDeleteUser();
const resetPasswordMutation = useResetPassword();
const activateUserMutation = useActivateUser();
const deactivateUserMutation = useDeactivateUser();

// ========================================
// COMPUTED PROPERTIES
// ========================================

const allUsers = computed(() => {
  if (!infiniteData.value?.pages) return [];
  return infiniteData.value.pages.flatMap((page) => page.items || []);
});

const filteredUsers = computed(() => {
  const list = allUsers.value || [];
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
        message: `Activate ${count} selected user${count !== 1 ? "s" : ""}?`,
      };
    case "deactivate":
      return {
        header: "Confirm Bulk Deactivation",
        actionLabel: "Deactivate",
        actionIcon: "pi pi-ban",
        severity: "warning",
        message: `Deactivate ${count} selected user${count !== 1 ? "s" : ""}?`,
      };
    case "delete":
      return {
        header: "Confirm Bulk Deletion",
        actionLabel: "Delete",
        actionIcon: "pi pi-trash",
        severity: "danger",
        message: `Delete ${count} selected user${count !== 1 ? "s" : ""}?`,
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

// ========================================
// HELPER FUNCTIONS
// ========================================

function contains(h: any, n: any): boolean {
  if (!n) return true;
  return String(h ?? "")
    .toLowerCase()
    .includes(String(n).toLowerCase());
}

function equals(a: any, b: any): boolean {
  if (b === null || b === undefined) return true;
  return a === b;
}

function sameDate(a: any, b: any): boolean {
  if (!b) return true;
  if (!a) return false;
  const da = new Date(a);
  const db = new Date(b);
  return da.toDateString() === db.toDateString();
}

// ========================================
// FILTER OPERATIONS
// ========================================

function resetFilters(): void {
  filters.value = structuredClone(initialFilters);
  visibleCount.value = BATCH_SIZE;
}

function openFilters(): void {
  filtersDialogVisible.value = true;
}

function applyFilters(): void {
  filtersDialogVisible.value = false;
  visibleCount.value = BATCH_SIZE;
}

function clearFilters(): void {
  resetFilters();
  filtersDialogVisible.value = false;
}

// ========================================
// DIALOG OPERATIONS
// ========================================

function openNew(): void {
  user.value = {};
  userDialog.value = true;
}

function hideDialog(): void {
  userDialog.value = false;
}

function editUser(u: UserResponseDto): void {
  user.value = { ...u };
  userDialog.value = true;
}

function openChangePasswordDialog(u: UserResponseDto): void {
  user.value = { ...u };
  changePasswordDialog.value = true;
}

function confirmDeleteUser(u: UserResponseDto): void {
  user.value = { ...u };
  deleteUserDialog.value = true;
}

// ========================================
// CRUD OPERATIONS
// ========================================

async function saveUser(newUser: any): Promise<void> {
  try {
    if (!newUser.id) {
      await createUserMutation.mutateAsync(newUser);
      toast.add({
        severity: "success",
        summary: "Created",
        detail: `User ${newUser.email} created`,
        life: 3000,
      });
    } else {
      await updateUserMutation.mutateAsync({
        id: newUser.id,
        data: newUser,
      });
      toast.add({
        severity: "success",
        summary: "Updated",
        detail: "User updated",
        life: 3000,
      });
    }
    userDialog.value = false;
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Save failed",
      life: 3000,
    });
  }
}

async function deleteUser(): Promise<void> {
  if (!user.value.id) return;

  softDeleteLoading.value = true;

  try {
    await softDeleteUserMutation.mutateAsync(user.value.id);
    toast.add({
      severity: "success",
      summary: "Deleted",
      detail: "User deleted successfully",
      life: 3000,
    });
    deleteUserDialog.value = false;
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed to delete user",
      life: 3000,
    });
  } finally {
    softDeleteLoading.value = false;
  }
}

async function hardDeleteUser(u: UserResponseDto): Promise<void> {
  confirm.require({
    message: `Permanently delete ${u.fullName}? This action cannot be undone.`,
    header: "⚠️ Permanent Deletion",
    icon: "pi pi-exclamation-triangle",
    rejectLabel: "Cancel",
    acceptLabel: "Delete Permanently",
    rejectProps: { outlined: true },
    acceptProps: { severity: "danger" },
    accept: async () => {
      try {
        await hardDeleteUserMutation.mutateAsync(u.id);
        toast.add({
          severity: "success",
          summary: "Deleted",
          detail: "User permanently deleted",
          life: 3000,
        });
      } catch (err: any) {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: err.message || "Failed to permanently delete user",
          life: 3000,
        });
      }
    },
  });
}

async function toggleUserStatus(u: UserResponseDto): Promise<void> {
  try {
    loadingUserIds.value.add(u.id);
    if (u.isActive) {
      await deactivateUserMutation.mutateAsync(u.id);
      toast.add({
        severity: "success",
        summary: "Deactivated",
        detail: `${u.fullName} has been deactivated`,
        life: 3000,
      });
    } else {
      await activateUserMutation.mutateAsync(u.id);
      toast.add({
        severity: "success",
        summary: "Activated",
        detail: `${u.fullName} has been activated`,
        life: 3000,
      });
    }
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed to update user status",
      life: 3000,
    });
  } finally {
    loadingUserIds.value.delete(u.id);
  }
}

async function handleChangePassword(data: {
  newPassword: string;
}): Promise<void> {
  if (!user.value.id) return;
  try {
    await resetPasswordMutation.mutateAsync({
      id: user.value.id,
      data: { newPassword: data.newPassword } as any,
    });
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
      detail: err.message || "Failed to reset password",
      life: 3000,
    });
  }
}

// ========================================
// BULK ACTIONS
// ========================================

function bulkActivate(): void {
  currentBulkAction.value = "activate";
  bulkActionDialog.value = true;
}

function bulkDeactivate(): void {
  currentBulkAction.value = "deactivate";
  bulkActionDialog.value = true;
}

function bulkDelete(): void {
  currentBulkAction.value = "delete";
  bulkActionDialog.value = true;
}

async function handleBulkActionConfirm(): Promise<void> {
  const ids = [...selectedUserIds.value];
  const action = currentBulkAction.value;

  if (!action || !ids.length) return;

  bulkActionDialog.value = false;
  bulkActionLoading.value = true; // ✅ NEW: Set loading state
  let successCount = 0;
  let errorCount = 0;

  try {
    const getMutation = () => {
      switch (action) {
        case "activate":
          return activateUserMutation.mutateAsync;
        case "deactivate":
          return deactivateUserMutation.mutateAsync;
        case "delete":
          return softDeleteUserMutation.mutateAsync;
        default:
          return null;
      }
    };

    const mutationFn = getMutation();
    if (!mutationFn) return;

    const results = await Promise.allSettled(ids.map((id) => mutationFn(id)));

    results.forEach((result) => {
      if (result.status === "fulfilled") {
        successCount++;
      } else {
        errorCount++;
      }
    });

    if (successCount > 0) {
      const actionText = {
        activate: "activated",
        deactivate: "deactivated",
        delete: "deleted",
      }[action];

      toast.add({
        severity: "success",
        summary: "Success",
        detail:
          errorCount === 0
            ? `${successCount} user${successCount !== 1 ? "s" : ""} ${actionText}`
            : `${successCount} user${successCount !== 1 ? "s" : ""} ${actionText}, ${errorCount} failed`,
        life: 3000,
      });
    }

    if (errorCount > 0) {
      toast.add({
        severity: "warn",
        summary: "Partial Failure",
        detail: `${errorCount} user${errorCount !== 1 ? "s" : ""} could not be ${action}d`,
        life: 3000,
      });
    }

    selectedUserIds.value = [];
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Bulk action failed",
      life: 3000,
    });
  } finally {
    bulkActionLoading.value = false; // ✅ NEW: Clear loading state
    currentBulkAction.value = null;
  }
}

// ========================================
// INFINITE SCROLL
// ========================================

function setupObserver(): void {
  if (observer) observer.disconnect();
  if (!sentinel.value) return;

  observer = new IntersectionObserver(async (entries) => {
    const [entry] = entries;
    if (!entry.isIntersecting) return;

    if (hasNextPage.value && !isFetchingNextPage.value) {
      loadingMore.value = true;
      await fetchNextPage();
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

// ========================================
// LIFECYCLE HOOKS
// ========================================

onMounted(setupObserver);
onBeforeUnmount(() => observer?.disconnect());
watch(sentinel, setupObserver);
watch(
  () => filters.value.global.value,
  () => {
    visibleCount.value = BATCH_SIZE;
  }
);
</script>

<template>
  <div class="p-4 md:p-6 space-y-4">
    <!-- HEADER -->
    <div>
      <h2 class="text-2xl md:text-3xl font-bold">Users Management</h2>
      <p class="text-600">Manage all users across all clinics</p>
    </div>

    <!-- TOOLBAR -->
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
          @click="refetch"
          :loading="isFetching"
        />
      </div>
    </div>

    <!-- SEARCH -->
    <div>
      <IconField class="w-full">
        <InputIcon><i class="pi pi-search" /></InputIcon>
        <InputText
          v-model="filters.global.value"
          placeholder="Search users by name, email, role, or clinic..."
          class="w-full"
        />
      </IconField>
      <small v-if="filters.global.value" class="text-surface-500 block mt-1">
        {{ filteredUsers.length }} result{{
          filteredUsers.length !== 1 ? "s" : ""
        }}
        found
      </small>
    </div>

    <!-- ERROR STATE -->
    <div v-if="isError" class="p-4 bg-red-50 dark:bg-red-900 rounded-lg">
      <p class="text-red-800 dark:text-red-200">
        ❌ {{ error?.message || "Failed to load users" }}
      </p>
      <Button
        label="Retry"
        icon="pi pi-refresh"
        text
        @click="refetch"
        class="mt-2"
      />
    </div>

    <!-- LOADING -->
    <div
      v-if="isFetching && allUsers.length === 0"
      class="grid gap-4 md:gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
    >
      <Card v-for="n in 8" :key="n">
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

        <template #content>
          <div class="flex flex-col gap-2 mt-2 text-sm">
            <div
              v-for="i in 4"
              :key="i"
              class="flex justify-between items-center"
            >
              <Skeleton width="4rem" height="0.9rem" animation="wave" />
              <Skeleton width="5rem" height="0.9rem" animation="wave" />
            </div>
          </div>
        </template>

        <template #footer>
          <div class="flex flex-wrap gap-2 mt-2">
            <Skeleton
              v-for="i in 5"
              :key="i"
              width="2rem"
              height="2rem"
              borderRadius="50%"
              animation="wave"
            />
          </div>
        </template>
      </Card>
    </div>

    <!-- CONTENT -->
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
              :loading="loadingUserIds.has(u.id)"
              :disabled="loadingUserIds.has(u.id)"
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

    <!-- EMPTY STATE -->
    <EmptyState
      v-else
      icon="pi pi-users"
      title="No Users Found"
      description="No users match your search or filters"
    />

    <!-- INFINITE SCROLL -->
    <div ref="sentinel" class="h-6"></div>
    <div v-if="loadingMore" class="flex justify-center p-4">
      <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
    </div>

    <!-- FILTERS DIALOG -->
    <Dialog
      v-model:visible="filtersDialogVisible"
      header="Filters"
      modal
      :style="{ width: '500px', maxWidth: '95vw' }"
    >
      <div class="grid gap-4">
        <div>
          <label class="block text-sm font-medium mb-2">Full Name</label>
          <InputText
            v-model="filters.fullName.constraints[0].value"
            placeholder="Search by full name..."
            class="w-full"
          />
        </div>

        <div>
          <label class="block text-sm font-medium mb-2">Email</label>
          <InputText
            v-model="filters.email.constraints[0].value"
            placeholder="Search by email..."
            class="w-full"
          />
        </div>

        <div>
          <label class="block text-sm font-medium mb-2">Role</label>
          <Select
            v-model="filters.roleName.constraints[0].value"
            :options="rolesDropdown"
            optionLabel="name"
            optionValue="name"
            placeholder="Select a role..."
            :loading="rolesDropdownLoading"
            showClear
            class="w-full"
            filter
            filterPlaceholder="Search roles..."
          >
            <template #value="{ value }">
              <div v-if="value" class="flex items-center gap-2">
                <i class="pi pi-shield"></i>
                <span>{{ value }}</span>
              </div>
              <span v-else class="text-surface-500">Select a role</span>
            </template>

            <template #option="{ option }">
              <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                  <i class="pi pi-shield text-primary"></i>
                  <span class="font-semibold">{{ option.name }}</span>
                </div>
                <small class="text-surface-500">{{
                  option.description || "No description"
                }}</small>
              </div>
            </template>
          </Select>
          <small
            v-if="rolesDropdownLoading"
            class="text-surface-500 block mt-1"
          >
            Loading roles...
          </small>
        </div>

        <div>
          <label class="block text-sm font-medium mb-2">Clinic</label>
          <Select
            v-model="filters.clinicName.constraints[0].value"
            :options="clinicsDropdown"
            optionLabel="name"
            optionValue="name"
            placeholder="Select a clinic..."
            :loading="clinicsDropdownLoading"
            showClear
            class="w-full"
            filter
            filterPlaceholder="Search clinics..."
          >
            <template #value="{ value }">
              <div v-if="value" class="flex items-center gap-2">
                <i class="pi pi-building"></i>
                <span>{{ value }}</span>
              </div>
              <span v-else class="text-surface-500">Select a clinic</span>
            </template>

            <template #option="{ option }">
              <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                  <i class="pi pi-building text-green-500"></i>
                  <span class="font-semibold">{{ option.name }}</span>
                </div>
                <small class="text-surface-500">{{ option.email }}</small>
              </div>
            </template>
          </Select>
          <small
            v-if="clinicsDropdownLoading"
            class="text-surface-500 block mt-1"
          >
            Loading clinics...
          </small>
        </div>

        <div>
          <label class="block text-sm font-medium mb-2">Status</label>
          <Select
            v-model="filters.isActive.constraints[0].value"
            :options="statusOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Select status..."
            showClear
            class="w-full"
          >
            <template #value="{ value }">
              <div
                v-if="value !== null && value !== undefined"
                class="flex items-center gap-2"
              >
                <Tag
                  :value="value ? 'Active' : 'Inactive'"
                  :severity="value ? 'success' : 'danger'"
                />
              </div>
              <span v-else class="text-surface-500">Select status</span>
            </template>

            <template #option="{ option }">
              <Tag :value="option.label" :severity="option.severity" />
            </template>
          </Select>
        </div>

        <div>
          <label class="block text-sm font-medium mb-2">Created Date</label>
          <DatePicker
            v-model="filters.createdAt.constraints[0].value"
            dateFormat="mm/dd/yy"
            placeholder="Select date..."
            showIcon
            class="w-full"
          />
        </div>
      </div>

      <template #footer>
        <div class="flex justify-between gap-2">
          <Button
            icon="pi pi-filter-slash"
            label="Clear All"
            outlined
            severity="secondary"
            @click="clearFilters"
          />
          <Button
            icon="pi pi-check"
            label="Apply Filters"
            @click="applyFilters"
          />
        </div>
      </template>
    </Dialog>

    <!-- DIALOGS -->
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

    <!-- SOFT DELETE DIALOG -->
    <Dialog
      v-model:visible="deleteUserDialog"
      header="Confirm Delete"
      modal
      :style="{ width: '450px' }"
    >
      <p>
        Are you sure you want to delete <b>{{ user.fullName }}</b
        >? This user will be soft-deleted and can be recovered.
      </p>
      <template #footer>
        <Button
          label="Cancel"
          outlined
          @click="deleteUserDialog = false"
          :disabled="softDeleteLoading"
        />
        <Button
          label="Delete"
          severity="danger"
          @click="deleteUser"
          :loading="softDeleteLoading"
          :disabled="softDeleteLoading"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="bulkActionDialog"
      :header="bulkDialogData.header"
      modal
      :style="{ width: '450px' }"
    >
      <p>{{ bulkDialogData.message }}</p>
      <template #footer>
        <Button
          label="Cancel"
          outlined
          @click="bulkActionDialog = false"
          :disabled="bulkActionLoading"
        />
        <Button
          :label="bulkDialogData.actionLabel"
          :icon="bulkDialogData.actionIcon"
          :severity="bulkDialogData.severity"
          @click="handleBulkActionConfirm"
          :loading="bulkActionLoading"
          :disabled="bulkActionLoading"
        />
      </template>
    </Dialog>

    <ConfirmDialog />
  </div>
</template>
