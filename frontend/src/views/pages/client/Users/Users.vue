<script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import { useToast } from 'primevue/usetoast';
  import { useUserStore } from '@/stores/userStore';
  import { useAuthStore } from '@/stores/authStore';
  import UserDialog from '@/components/User/UserDialog.vue';
  import ChangePasswordDialog from '@/components/User/ChangePasswordDialog.vue';
  import EmptyState from '@/components/EmptyState.vue';
  import { formatDate } from '@/utils/uiHelpers';
  import { FilterMatchMode, FilterOperator } from '@primevue/core/api';
  import type { User } from '@/types/backend';

  // ----------------------------
  // Stores & Toast
  // ----------------------------
  const userStore = useUserStore();
  const authStore = useAuthStore();
  const toast = useToast();

  // ----------------------------
  // Reactive state
  // ----------------------------
  const dt = ref<any>();
  const userDialog = ref(false);
  const changePasswordDialog = ref(false);
  const deleteUserDialog = ref(false);
  const user = ref<User | any>({});
  const selectedUsers = ref<User[] | null>(null);
  const submitted = ref(false);

  // ✅ KEEP EXACT SAME FILTER STRUCTURE
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

  // ----------------------------
  // Fetch Users (Client-scoped)
  // ----------------------------
  const fetchUsers = async () => {
    try {
      await userStore.fetchClientUsers(authStore.currentUser?.clinicId);
    } catch (error: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: error.message,
        life: 3000,
      });
    }
  };

  onMounted(fetchUsers);

  // ----------------------------
  // Dialog & CRUD logic
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

  function editUser(selectedUser: User) {
    user.value = { ...selectedUser };
    userDialog.value = true;
  }

  function openChangePasswordDialog(selectedUser: User) {
    user.value = { ...selectedUser };
    changePasswordDialog.value = true;
  }

  function confirmDeleteUser(selectedUser: User) {
    user.value = { ...selectedUser };
    deleteUserDialog.value = true;
  }

  async function deleteUser() {
    if (!user.value.id) return;
    try {
      await userStore.deleteUser(user.value.id);
      toast.add({
        severity: 'success',
        summary: 'Deleted',
        detail: 'User deleted successfully',
        life: 3000,
      });
      deleteUserDialog.value = false;
      await fetchUsers();
    } catch (error: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: error.message,
        life: 3000,
      });
    }
  }

  async function saveUser(newUser: User) {
    try {
      if (!newUser.id) {
        const createdUser = await userStore.createUser({
          ...newUser,
          clinicId: authStore.currentUser?.clinicId,
        });
        toast.add({
          severity: 'success',
          summary: 'Created',
          detail: `User ${createdUser.email} created`,
          life: 3000,
        });
      } else {
        await userStore.updateUser(newUser.id, newUser);
        toast.add({
          severity: 'success',
          summary: 'Updated',
          detail: 'User updated successfully',
          life: 3000,
        });
      }
      userDialog.value = false;
      await fetchUsers();
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: userStore.error || err.message || 'Save failed',
        life: 3000,
      });
    }
  }

  async function handleChangePassword(passwords: { newPassword: string; confirmPassword: string }) {
    if (!passwords.newPassword || passwords.newPassword !== passwords.confirmPassword) {
      toast.add({
        severity: 'warn',
        summary: 'Validation',
        detail: 'Passwords do not match',
        life: 3000,
      });
      return;
    }

    try {
      await userStore.changePassword(user.value.id, passwords.newPassword);
      toast.add({
        severity: 'success',
        summary: 'Success',
        detail: 'Password changed successfully',
        life: 3000,
      });
      changePasswordDialog.value = false;
    } catch (error: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
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
            <InputText v-model="filters['global'].value" placeholder="Search..." />
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
        :globalFilterFields="['displayName', 'email', 'role', 'isActive', 'createdAt']"
        paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
        :rowsPerPageOptions="[5, 10, 25]"
        currentPageReportTemplate="Showing {first} to {last} of {totalRecords} Users"
      >
        <Column
          field="displayName"
          header="Display Name"
          sortable
          filter
          filterField="displayName"
          style="min-width: 16rem"
        >
          <template #filter="{ filterModel }">
            <InputText v-model="filterModel.value" placeholder="Search by name" />
          </template>
        </Column>

        <Column
          field="email"
          header="Email"
          sortable
          filter
          filterField="email"
          style="min-width: 20rem"
        >
          <template #filter="{ filterModel }">
            <InputText v-model="filterModel.value" placeholder="Search by email" />
          </template>
        </Column>

        <Column
          field="role"
          header="Role"
          sortable
          filter
          filterField="role"
          style="min-width: 10rem"
        >
          <template #filter="{ filterModel }">
            <Select
              v-model="filterModel.value"
              :options="['Doctor', 'Nurse', 'Receptionist']"
              placeholder="Select Role"
              showClear
            />
          </template>
        </Column>

        <Column field="isActive" header="Active" sortable>
          <template #body="slotProps">
            <Tag
              :value="slotProps.data.isActive ? 'Active' : 'Inactive'"
              :severity="slotProps.data.isActive ? 'success' : 'danger'"
            />
          </template>
          <template #filter="{ filterModel }">
            <Select
              v-model="filterModel.value"
              :options="[
                { label: 'Active', value: true },
                { label: 'Inactive', value: false },
              ]"
              optionLabel="label"
              optionValue="value"
              placeholder="Select Status"
              showClear
            />
          </template>
        </Column>

        <Column
          field="createdAt"
          header="Created At"
          dataType="date"
          sortable
          filter
          filterField="createdAt"
          style="min-width: 12rem"
        >
          <template #body="slotProps">
            {{ formatDate(slotProps.data.createdAt) }}
          </template>
          <template #filter="{ filterModel }">
            <DatePicker
              v-model="filterModel.value"
              dateFormat="dd/mm/yy"
              placeholder="dd/mm/yyyy"
            />
          </template>
        </Column>

        <Column header="Actions" style="min-width: 12rem">
          <template #body="slotProps">
            <Button
              icon="pi pi-key"
              outlined
              rounded
              class="mr-2"
              severity="info"
              @click="openChangePasswordDialog(slotProps.data)"
            />
            <Button
              icon="pi pi-pencil"
              outlined
              rounded
              class="mr-2"
              @click="editUser(slotProps.data)"
            />
            <Button
              icon="pi pi-trash"
              outlined
              rounded
              severity="danger"
              @click="confirmDeleteUser(slotProps.data)"
            />
          </template>
        </Column>

        <template #empty>
          <EmptyState icon="pi pi-user" title="No Users Found" description="No clinic users found.">
            <template #action>
              <Button label="Add User" icon="pi pi-plus" class="p-button-sm" @click="openNew" />
            </template>
          </EmptyState>
        </template>
      </DataTable>
    </div>

    <UserDialog v-model:visible="userDialog" :user="user" @save="saveUser" @cancel="hideDialog" />
    <ChangePasswordDialog
      v-model:visible="changePasswordDialog"
      :user="user"
      @save="handleChangePassword"
    />

    <Dialog
      v-model:visible="deleteUserDialog"
      :style="{ width: '450px' }"
      header="Confirm"
      :modal="true"
    >
      <div class="flex items-center gap-4">
        <i class="pi pi-exclamation-triangle !text-3xl" />
        <span v-if="user"
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
          :disabled="userStore.isProcessing"
        />
        <Button
          label="Yes"
          icon="pi pi-check"
          :loading="userStore.isProcessing"
          @click="deleteUser"
          severity="danger"
          outlined
        />
      </template>
    </Dialog>
  </div>
</template>
