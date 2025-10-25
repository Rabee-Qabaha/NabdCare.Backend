// src/views/pages/admin/Users.vue
<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useToast } from "primevue/usetoast";
import { useConfirm } from "primevue/useconfirm";
import { useUserStore } from "@/stores/userStore";
import { hasPermission } from "@/utils/permissions";
import UserDialog from "@/components/User/UserDialog.vue";
import ChangePasswordDialog from "@/components/User/ChangePasswordDialog.vue";
import EmptyState from "@/components/EmptyState.vue";
import { formatDate } from "@/utils/uiHelpers";
// IMPORTANT: Keep FilterMatchMode and FilterOperator for advanced filtering
import { FilterMatchMode, FilterOperator } from "@primevue/core/api";
import type { UserResponseDto } from "@/types/backend";

// PrimeVue Component Imports (Assuming these are registered globally or locally)
import InputText from "primevue/inputtext";
import Select from "primevue/select";
import DatePicker from "primevue/datepicker";

const userStore = useUserStore();
const toast = useToast();
const confirm = useConfirm();

// --- Filter Options ---
const statusOptions = ref([
  { label: "Active", value: true, severity: "success" },
  { label: "Inactive", value: false, severity: "danger" },
]);

// 1. ADVANCED FILTER INITIALIZATION: This structure enables the "Add Rule" button and match mode selection.
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
  // Use EQUALS for boolean status
  isActive: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
  },
  // Use DATE_IS for exact date match or DATE_AFTER for custom date ranges
  createdAt: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
  },
};

// State
const dt = ref<any>();
const userDialog = ref(false);
const changePasswordDialog = ref(false);
const deleteUserDialog = ref(false);

// NEW STATE FOR BULK ACTIONS
const bulkActionDialog = ref(false);
const currentBulkAction = ref<"activate" | "deactivate" | "delete" | null>(
  null
);

const user = ref<Partial<UserResponseDto>>({});
const selectedUsers = ref<UserResponseDto[]>([]);

// Initialize filters with the full structure
const filters = ref({ ...initialFilters });

// Permissions
const canCreate = computed(() => hasPermission("Users.Create"));
const canEdit = computed(() => hasPermission("Users.Edit"));
const canDelete = computed(() => hasPermission("Users.Delete"));
const canResetPassword = computed(() => hasPermission("Users.ResetPassword"));
const canActivate = computed(() => hasPermission("Users.Activate"));

// 2. CLEAR FILTERS: Must reset to the full initial structure
function clearFilters(): void {
  filters.value = { ...initialFilters };
}

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

// CRUD Handlers
function openNew() {
  user.value = {};
  userDialog.value = true;
}

function hideDialog() {
  userDialog.value = false;
}

function editUser(selectedUser: UserResponseDto) {
  user.value = { ...selectedUser };
  userDialog.value = true;
}

function openChangePasswordDialog(selectedUser: UserResponseDto) {
  user.value = { ...selectedUser };
  changePasswordDialog.value = true;
}

function confirmDeleteUser(selectedUser: UserResponseDto) {
  user.value = { ...selectedUser };
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
    user.value = {};
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message || "Failed to delete user",
      life: 3000,
    });
  }
}

async function hardDeleteUser(selectedUser: UserResponseDto) {
  console.log("selected user ", selectedUser);

  confirm.require({
    message: `Are you sure you want to PERMANENTLY delete ${selectedUser.fullName}? This action CANNOT be undone!`,
    header: "⚠️ Permanent Deletion",
    icon: "pi pi-exclamation-triangle",
    rejectLabel: "Cancel",
    acceptLabel: "Delete Permanently",
    rejectProps: { outlined: true },
    acceptProps: { severity: "danger" },
    accept: async () => {
      try {
        await userStore.hardDeleteUser(selectedUser.id);
        toast.add({
          severity: "success",
          summary: "Deleted",
          detail: "User permanently deleted",
          life: 3000,
        });
        await fetchUsers();
      } catch (error: any) {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: error.message || "Failed to delete user permanently",
          life: 3000,
        });
      }
    },
  });
}

async function toggleUserStatus(selectedUser: UserResponseDto) {
  try {
    if (selectedUser.isActive) {
      await userStore.deactivateUser(selectedUser.id);
      toast.add({
        severity: "success",
        summary: "Deactivated",
        detail: `${selectedUser.fullName} has been deactivated`,
        life: 3000,
      });
    } else {
      await userStore.activateUser(selectedUser.id);
      toast.add({
        severity: "success",
        summary: "Activated",
        detail: `${selectedUser.fullName} has been activated`,
        life: 3000,
      });
    }
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message || "Failed to update user status",
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
        detail: `User ${newUser.email} created successfully`,
        life: 3000,
      });
    } else {
      await userStore.updateUser(newUser.id, newUser);
      toast.add({
        severity: "success",
        summary: "Updated",
        detail: "User updated successfully",
        life: 3000,
      });
    }
    userDialog.value = false;
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
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message || "Failed to reset password",
      life: 3000,
    });
  }
}

// --- BULK ACTION INITIATORS (Set state for new dialog) ---
function bulkActivate() {
  if (selectedUsers.value.length === 0) return;
  currentBulkAction.value = "activate";
  bulkActionDialog.value = true;
}

function bulkDeactivate() {
  if (selectedUsers.value.length === 0) return;
  currentBulkAction.value = "deactivate";
  bulkActionDialog.value = true;
}

function bulkDelete() {
  if (selectedUsers.value.length === 0) return;
  currentBulkAction.value = "delete";
  bulkActionDialog.value = true;
}

// --- COMPUTED PROPERTIES FOR DYNAMIC BULK DIALOG ---
const bulkDialogData = computed(() => {
  const count = selectedUsers.value.length;
  switch (currentBulkAction.value) {
    case "activate":
      return {
        header: "Confirm Bulk Activation",
        // FIX: Ensure 'pi pi-' is included for the icon class
        iconClass: "pi pi-check-circle text-green-500",
        actionLabel: "Activate",
        // FIX: Ensure 'pi pi-' is included for the button icon prop
        actionIcon: "pi pi-check",
        severity: "success",
        message: `Are you sure you want to activate ${count} selected user(s)?`,
        showWarning: false,
      };
    case "deactivate":
      return {
        header: "Confirm Bulk Deactivation",
        // FIX: Ensure 'pi pi-' is included for the icon class
        iconClass: "pi pi-ban text-orange-500",
        actionLabel: "Deactivate",
        // FIX: Ensure 'pi pi-' is included for the button icon prop
        actionIcon: "pi pi-ban",
        severity: "warning",
        message: `Are you sure you want to deactivate ${count} selected user(s)?`,
        showWarning: false,
      };
    case "delete":
      return {
        header: "Confirm Bulk Deletion",
        // FIX: Ensure 'pi pi-' is included for the icon class
        iconClass: "pi pi-trash text-red-500",
        actionLabel: "Delete",
        // FIX: Ensure 'pi pi-' is included for the button icon prop
        actionIcon: "pi pi-trash",
        severity: "danger",
        message: `Are you sure you want to delete ${count} selected user(s)?`,
        showWarning: true,
      };
    default:
      return {
        header: "",
        iconClass: "",
        actionLabel: "",
        actionIcon: "",
        severity: "secondary",
        message: "",
        showWarning: false,
      };
  }
});

// --- BULK ACTION CONFIRMATION HANDLER ---
async function handleBulkActionConfirm() {
  const action = currentBulkAction.value;
  bulkActionDialog.value = false;
  if (!action || selectedUsers.value.length === 0) return;
  const userIds = selectedUsers.value.map((u) => u.id);
  const actionFn =
    action === "activate"
      ? userStore.activateUser
      : action === "deactivate"
        ? userStore.deactivateUser
        : userStore.deleteUser;
  const actionDescription =
    action === "activate"
      ? "activated"
      : action === "deactivate"
        ? "deactivated"
        : "deleted";
  try {
    await Promise.all(userIds.map((id) => actionFn(id)));
    await fetchUsers(); // <--- fix: guarantees table is up-to-date
    toast.add({
      severity: "success",
      summary: "Success",
      detail: `${selectedUsers.value.length} users ${actionDescription} successfully.`,
      life: 3000,
    });
    selectedUsers.value = [];
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message || `Bulk ${actionDescription} failed.`,
      life: 3000,
    });
  } finally {
    currentBulkAction.value = null;
  }
}
</script>

<template>
  <div class="card">
    <!-- Header -->
    <div class="mb-4">
      <h2 class="text-3xl font-bold text-900 m-0 mb-2">Users Management</h2>
      <p class="text-600 mb-4">Manage all users across all clinics</p>
    </div>

    <!-- Toolbar -->
    <Toolbar class="mb-4">
      <template #start>
        <!-- Create User Button - Uses global primary theme color -->
        <Button
          v-if="canCreate"
          label="Create User"
          icon="pi pi-plus"
          class="mr-2"
          @click="openNew"
        />

        <!-- Bulk Actions -->
        <template v-if="selectedUsers.length > 0">
          <Button
            v-if="canActivate"
            label="Activate Selected"
            icon="pi pi-check"
            severity="success"
            outlined
            class="mr-2"
            @click="bulkActivate"
          />
          <Button
            v-if="canActivate"
            label="Deactivate Selected"
            icon="pi pi-ban"
            severity="warning"
            outlined
            class="mr-2"
            @click="bulkDeactivate"
          />
          <Button
            v-if="canDelete"
            label="Delete Selected"
            icon="pi pi-trash"
            severity="danger"
            outlined
            @click="bulkDelete"
          />
        </template>
      </template>

      <template #end>
        <Button
          icon="pi pi-filter-slash"
          label="Clear Filters"
          outlined
          class="mr-2"
          @click="clearFilters"
        />
        <Button
          icon="pi pi-refresh"
          label="Refresh"
          outlined
          @click="fetchUsers"
          :loading="userStore.loading"
        />
      </template>
    </Toolbar>

    <!-- Global Search -->
    <div class="mb-4">
      <IconField>
        <InputIcon><i class="pi pi-search" /></InputIcon>
        <InputText
          v-model="filters['global'].value"
          placeholder="Search users by name, email, role, or clinic..."
          class="w-full"
        />
      </IconField>
    </div>

    <!-- Users Table -->
    <DataTable
      ref="dt"
      v-model:selection="selectedUsers"
      v-model:filters="filters"
      :value="userStore.users"
      :loading="userStore.loading"
      dataKey="id"
      :paginator="true"
      :rows="10"
      filterDisplay="menu"
      :globalFilterFields="['fullName', 'email', 'roleName', 'clinicName']"
      paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
      :rowsPerPageOptions="[10, 25, 50, 100]"
      currentPageReportTemplate="Showing {first} to {last} of {totalRecords} users"
    >
      <Column selectionMode="multiple" headerStyle="width: 3rem" />

      <!-- Name Column - Custom InputText Filter -->
      <Column
        field="fullName"
        header="Name"
        sortable
        filter
        style="min-width: 14rem"
      >
        <template #body="{ data }">
          <div class="flex items-center gap-2">
            <Avatar
              :label="data.fullName?.charAt(0) || '?'"
              shape="circle"
              :class="data.isActive ? 'bg-green-500' : 'bg-gray-400'"
              class="text-white"
            />
            <span class="font-semibold">{{ data.fullName }}</span>
          </div>
        </template>
        <!-- filterModel refers to the constraint object in the advanced filter structure -->
        <template #filter="{ filterModel }">
          <InputText
            v-model="filterModel.value"
            type="text"
            placeholder="Search by Name"
            class="w-full"
          />
        </template>
      </Column>

      <!-- Email Column - Custom InputText Filter -->
      <Column
        field="email"
        header="Email"
        sortable
        filter
        style="min-width: 16rem"
      >
        <template #body="{ data }">
          <span class="text-600">{{ data.email }}</span>
        </template>
        <template #filter="{ filterModel }">
          <InputText
            v-model="filterModel.value"
            type="text"
            placeholder="Search by Email"
            class="w-full"
          />
        </template>
      </Column>

      <!-- Role Column - Custom InputText Filter -->
      <Column
        field="roleName"
        header="Role"
        sortable
        filter
        style="min-width: 10rem"
      >
        <template #body="{ data }">
          <Tag :value="data.roleName" severity="info" />
        </template>
        <template #filter="{ filterModel }">
          <InputText
            v-model="filterModel.value"
            type="text"
            placeholder="Filter by Role"
            class="w-full"
          />
        </template>
      </Column>

      <!-- Clinic Column - Custom InputText Filter -->
      <Column
        field="clinicName"
        header="Clinic"
        sortable
        filter
        style="min-width: 14rem"
      >
        <template #body="{ data }">
          <span v-if="data.clinicName">{{ data.clinicName }}</span>
          <Tag v-else value="No Clinic" severity="secondary" />
        </template>
        <template #filter="{ filterModel }">
          <InputText
            v-model="filterModel.value"
            type="text"
            placeholder="Filter by Clinic"
            class="w-full"
          />
        </template>
      </Column>

      <!-- Status Column - Custom Select Filter -->
      <Column
        field="isActive"
        header="Status"
        sortable
        filter
        style="min-width: 8rem"
        dataType="boolean"
      >
        <template #body="{ data }">
          <Tag
            :value="data.isActive ? 'Active' : 'Inactive'"
            :severity="data.isActive ? 'success' : 'danger'"
          />
        </template>
        <template #filter="{ filterModel }">
          <!-- Using Select for boolean/status filtering -->
          <Select
            v-model="filterModel.value"
            :options="statusOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Select Status"
            showClear
            class="w-full"
          >
            <template #option="slotProps">
              <Tag
                :value="slotProps.option.label"
                :severity="slotProps.option.severity"
              />
            </template>
          </Select>
        </template>
      </Column>

      <!-- Created Date Column - Custom DatePicker Filter -->
      <Column
        field="createdAt"
        header="Created"
        sortable
        filter
        dataType="date"
        style="min-width: 10rem"
      >
        <template #body="{ data }">
          <span class="text-600">{{ formatDate(data.createdAt) }}</span>
        </template>
        <template #filter="{ filterModel }">
          <!-- Using DatePicker for date filtering. The match mode can be adjusted in the filter menu by the user. -->
          <DatePicker
            v-model="filterModel.value"
            dateFormat="mm/dd/yy"
            placeholder="Select Date"
            showIcon
            class="w-full"
          />
        </template>
      </Column>

      <!-- Actions Menu -->
      <Column header="Actions" style="min-width: 8rem">
        <template #body="{ data }">
          <Button
            icon="pi pi-ellipsis-v"
            text
            rounded
            aria-label="Actions"
            @click="(event) => ($refs['menu_' + data.id] as any)?.toggle(event)"
          />
          <Menu
            :ref="'menu_' + data.id"
            :model="
              [
                {
                  label: 'Edit',
                  icon: 'pi pi-pencil',
                  command: () => editUser(data),
                  visible: canEdit,
                },
                {
                  label: data.isActive ? 'Deactivate' : 'Activate',
                  icon: data.isActive ? 'pi pi-ban' : 'pi pi-check',
                  command: () => toggleUserStatus(data),
                  visible: canActivate,
                },
                {
                  label: 'Reset Password',
                  icon: 'pi pi-key',
                  command: () => openChangePasswordDialog(data),
                  visible: canResetPassword,
                },
                { separator: true, visible: canDelete },
                {
                  label: 'Delete',
                  icon: 'pi pi-trash',
                  command: () => confirmDeleteUser(data),
                  visible: canDelete,
                },
                {
                  label: 'Hard Delete',
                  icon: 'pi pi-times-circle',
                  command: () => hardDeleteUser(data),
                  visible: canDelete,
                },
              ].filter((item) => item.visible !== false)
            "
            :popup="true"
          />
        </template>
      </Column>

      <template #empty>
        <EmptyState
          icon="pi pi-users"
          title="No Users Found"
          description="No users match your search criteria"
        >
          <template #action>
            <Button
              v-if="canCreate"
              label="Create First User"
              icon="pi pi-plus"
              @click="openNew"
            />
          </template>
        </EmptyState>
      </template>

      <template #loading>
        <div class="flex items-center justify-center p-6">
          <ProgressSpinner style="width: 50px; height: 50px" strokeWidth="4" />
        </div>
      </template>
    </DataTable>

    <!-- Dialogs -->
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

    <!-- Delete Confirmation (Single User) -->
    <Dialog
      v-model:visible="deleteUserDialog"
      header="Confirm Deletion"
      :modal="true"
      :style="{ width: '450px' }"
      class="rounded-xl shadow-2xl"
    >
      <div class="flex items-center gap-4">
        <i class="pi pi-exclamation-triangle text-orange-500 text-3xl" />
        <span>
          Are you sure you want to delete <b>{{ user.fullName }}</b
          >? <br /><span class="text-sm text-600"
            >This user can be restored later.</span
          >
        </span>
      </div>
      <template #footer>
        <Button
          label="Cancel"
          icon="pi pi-times"
          outlined
          @click="deleteUserDialog = false"
        />
        <Button
          label="Delete"
          icon="pi pi-trash"
          severity="danger"
          @click="deleteUser"
        />
      </template>
    </Dialog>

    <!-- Bulk Action Confirmation Dialog (Unified) -->
    <Dialog
      v-model:visible="bulkActionDialog"
      :header="bulkDialogData.header"
      :modal="true"
      :style="{ width: '450px' }"
      class="rounded-xl shadow-2xl"
    >
      <div class="flex items-center gap-4">
        <span>
          {{ bulkDialogData.message }}
          <br />
          <span v-if="bulkDialogData.showWarning" class="text-sm text-600">
            This is a soft delete; users can be restored later.
          </span>
        </span>
      </div>
      <template #footer>
        <Button
          label="Cancel"
          icon="pi pi-times"
          severity="secondary"
          outlined
          @click="bulkActionDialog = false"
        />
        <!-- actionIcon is now correctly passed, e.g., 'pi pi-check' -->
        <Button
          :label="bulkDialogData.actionLabel"
          :icon="bulkDialogData.actionIcon"
          :severity="bulkDialogData.severity"
          @click="handleBulkActionConfirm"
        />
      </template>
    </Dialog>

    <!-- Confirmation Service (Kept for hardDeleteUser) -->
    <ConfirmDialog />
  </div>
</template>
