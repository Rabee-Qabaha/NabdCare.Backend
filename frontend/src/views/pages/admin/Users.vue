<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useToast } from "primevue/usetoast";
import { useUserStore } from "@/stores/userStore";
import UserDialog from "@/components/User/UserDialog.vue";
import ChangePasswordDialog from "@/components/User/ChangePasswordDialog.vue";
import EmptyState from "@/components/EmptyState.vue";
import { formatDate } from "@/utils/uiHelpers";
import { FilterMatchMode, FilterOperator } from "@primevue/core/api";

const userStore = useUserStore();
const toast = useToast();

// ----------------------------
// State
// ----------------------------
const dt = ref<any>();
const userDialog = ref(false);
const changePasswordDialog = ref(false);
const deleteUserDialog = ref(false);
const user = ref<any>({});
const selectedUsers = ref<any[] | null>(null);
const submitted = ref(false);

const filters = ref({
  global: { value: null, matchMode: FilterMatchMode.CONTAINS },
  displayName: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
  },
  email: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
  },
  role: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
  },
  isActive: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
  },
  createdAt: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
  },
});

function clearFilters(): void {
  filters.value = {
    global: { value: null, matchMode: FilterMatchMode.CONTAINS },
    displayName: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    email: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    role: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
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
}

const fetchUsers = async () => {
  try {
    await userStore.fetchUsers(); // 🔹 Fetch all users (super admin endpoint)
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message,
      life: 3000,
    });
  }
};

onMounted(fetchUsers);

// ----------------------------
// CRUD Handlers
// ----------------------------
function openNew() {
  user.value = {};
  submitted.value = false;
  userDialog.value = true;
}

function hideDialog() {
  userDialog.value = false;
  submitted.value = false;
}

function editUser(selectedUser: any) {
  user.value = { ...selectedUser };
  userDialog.value = true;
}

function openChangePasswordDialog(selectedUser: any) {
  user.value = { ...selectedUser };
  changePasswordDialog.value = true;
}

function confirmDeleteUser(selectedUser: any) {
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
      detail: error.message,
      life: 3000,
    });
  }
}

async function saveUser(newUser: any) {
  try {
    if (!newUser.id) {
      const createdUser = await userStore.createUser(newUser);
      toast.add({
        severity: "success",
        summary: "Created",
        detail: `User ${createdUser.email} created`,
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

async function handleChangePassword(passwords: {
  newPassword: string;
  confirmPassword: string;
}) {
  if (
    !passwords.newPassword ||
    passwords.newPassword !== passwords.confirmPassword
  ) {
    toast.add({
      severity: "warn",
      summary: "Validation",
      detail: "Passwords do not match",
      life: 3000,
    });
    return;
  }
  try {
    await userStore.changePassword(user.value.id, passwords.newPassword);
    toast.add({
      severity: "success",
      summary: "Success",
      detail: "Password changed successfully",
      life: 3000,
    });
    changePasswordDialog.value = false;
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message,
      life: 3000,
    });
  }
}
</script>

<template>
  <div>
    <div class="card">
      <Toolbar class="mb-6">
        <template #start>
          <Button
            label="New"
            icon="pi pi-plus"
            severity="secondary"
            class="mr-2"
            @click="openNew"
          />
        </template>

        <template #end>
          <Button
            type="button"
            icon="pi pi-filter-slash"
            label="Clear"
            outlined
            class="mr-2"
            @click="clearFilters"
          />
          <IconField>
            <InputIcon><i class="pi pi-search" /></InputIcon>
            <InputText
              v-model="filters['global'].value"
              placeholder="Search..."
            />
          </IconField>
        </template>
      </Toolbar>

      <DataTable
        ref="dt"
        v-model:selection="selectedUsers"
        :value="userStore.users"
        dataKey="id"
        :paginator="true"
        :rows="10"
        v-model:filters="filters"
        filterDisplay="menu"
        :globalFilterFields="[
          'displayName',
          'email',
          'role',
          'clinicName',
          'isActive',
          'createdAt',
        ]"
        paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
        :rowsPerPageOptions="[5, 10, 25]"
        currentPageReportTemplate="Showing {first} to {last} of {totalRecords} Users"
      >
        <Column
          field="displayName"
          header="Display Name"
          sortable
          filter
          style="min-width: 16rem"
        />
        <Column
          field="email"
          header="Email"
          sortable
          filter
          style="min-width: 18rem"
        />
        <Column
          field="role"
          header="Role"
          sortable
          filter
          style="min-width: 10rem"
        />
        <Column
          field="clinicName"
          header="Clinic"
          sortable
          filter
          style="min-width: 14rem"
        />
        <Column field="isActive" header="Status" sortable>
          <template #body="{ data }">
            <Tag
              :value="data.isActive ? 'Active' : 'Inactive'"
              :severity="data.isActive ? 'success' : 'danger'"
            />
          </template>
        </Column>
        <Column field="createdAt" header="Created At" dataType="date" sortable>
          <template #body="{ data }">
            {{ formatDate(data.createdAt) }}
          </template>
        </Column>
        <Column header="Actions" style="min-width: 12rem">
          <template #body="{ data }">
            <Button
              icon="pi pi-key"
              outlined
              rounded
              class="mr-2"
              severity="info"
              @click="openChangePasswordDialog(data)"
            />
            <Button
              icon="pi pi-pencil"
              outlined
              rounded
              class="mr-2"
              @click="editUser(data)"
            />
            <Button
              icon="pi pi-trash"
              outlined
              rounded
              severity="danger"
              @click="confirmDeleteUser(data)"
            />
          </template>
        </Column>
        <template #empty>
          <EmptyState
            icon="pi pi-user"
            title="No Users Found"
            description="No users available yet."
          >
            <template #action>
              <Button
                label="Add User"
                icon="pi pi-plus"
                class="p-button-sm"
                @click="openNew"
              />
            </template>
          </EmptyState>
        </template>
      </DataTable>
    </div>

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
      header="Confirm"
      :modal="true"
      :style="{ width: '450px' }"
    >
      <div class="flex items-center gap-4">
        <i class="pi pi-exclamation-triangle !text-3xl" />
        <span
          >Are you sure you want to delete <b>{{ user.displayName }}</b
          >?</span
        >
      </div>
      <template #footer>
        <Button
          label="No"
          icon="pi pi-times"
          text
          @click="deleteUserDialog = false"
        />
        <Button
          label="Yes"
          icon="pi pi-check"
          severity="danger"
          outlined
          @click="deleteUser"
        />
      </template>
    </Dialog>
  </div>
</template>
