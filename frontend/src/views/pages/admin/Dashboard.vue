// Admin Dashboard View - src/views/pages/admin/Dashboard.vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import { apiService } from "@/service/apiService";
import type {
  UserResponseDto,
  ClinicResponseDto,
  RoleResponseDto,
} from "@/types/backend";

const router = useRouter();
const authStore = useAuthStore();

// Dashboard state
const loading = ref(true);
const stats = ref({
  totalUsers: 0,
  activeUsers: 0,
  totalClinics: 0,
  activeClinics: 0,
  totalRoles: 0,
  totalSubscriptions: 0,
  activeSubscriptions: 0,
  monthlyRevenue: 0,
});

const recentUsers = ref<UserResponseDto[]>([]);
const recentClinics = ref<ClinicResponseDto[]>([]);

// Load dashboard data
const loadDashboard = async () => {
  try {
    loading.value = true;

    // ✅ Fetch data in parallel with error handling
    const results = await Promise.allSettled([
      apiService.get<UserResponseDto[]>("/users"),
      apiService.get<ClinicResponseDto[]>("/clinics"),
      apiService.get<RoleResponseDto[]>("/roles"),
    ]);

    // Process users
    if (results[0].status === "fulfilled") {
      const users = results[0].value;
      stats.value.totalUsers = users.length;
      stats.value.activeUsers = users.filter((u) => u.isActive).length;
      recentUsers.value = users.slice(-5).reverse(); // Last 5, newest first
    } else {
      console.error("Failed to load users:", results[0].reason);
    }

    // Process clinics
    if (results[1].status === "fulfilled") {
      const clinics = results[1].value;
      stats.value.totalClinics = clinics.length;
      stats.value.activeClinics = clinics.filter(
        (c) => c.status === "Active"
      ).length;
      recentClinics.value = clinics.slice(-5).reverse();
    } else {
      console.error("Failed to load clinics:", results[1].reason);
    }

    // Process roles
    if (results[2].status === "fulfilled") {
      const roles = results[2].value;
      stats.value.totalRoles = roles.length;
    } else {
      console.error("Failed to load roles:", results[2].reason);
    }

    // ✅ Set subscription stats to 0 for now (endpoint not ready)
    stats.value.totalSubscriptions = 0;
    stats.value.activeSubscriptions = 0;
    stats.value.monthlyRevenue = 0;
  } catch (error) {
    console.error("❌ Failed to load dashboard:", error);
  } finally {
    loading.value = false;
  }
};

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(amount);
};

const formatDate = (date: string | Date) => {
  return new Date(date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
};

onMounted(() => {
  loadDashboard();
});
</script>

<template>
  <div class="grid">
    <!-- Header -->
    <div class="col-12">
      <div class="flex align-items-center justify-content-between mb-4">
        <div>
          <h1 class="text-3xl font-bold text-900 m-0 mb-2">
            Welcome back, {{ authStore.currentUser.name }}! 👋
          </h1>
          <p class="text-600 text-lg m-0">
            Here's what's happening with NabdCare today
          </p>
        </div>
        <Button
          icon="pi pi-refresh"
          label="Refresh"
          @click="loadDashboard"
          :loading="loading"
          outlined
        />
      </div>
    </div>

    <!-- Statistics Cards -->
    <div class="col-12 lg:col-6 xl:col-3">
      <div
        class="card mb-0 bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800"
      >
        <div class="flex justify-content-between mb-3">
          <div>
            <span
              class="block text-blue-600 dark:text-blue-400 font-medium mb-3"
              >Total Users</span
            >
            <div class="text-blue-900 dark:text-blue-100 font-bold text-3xl">
              <Skeleton v-if="loading" width="4rem" height="2rem" />
              <span v-else>{{ stats.totalUsers }}</span>
            </div>
            <span class="text-blue-600 dark:text-blue-400 text-sm">
              {{ stats.activeUsers }} active
            </span>
          </div>
          <div
            class="flex align-items-center justify-content-center bg-blue-500 text-white border-round"
            style="width: 3.5rem; height: 3.5rem"
          >
            <i class="pi pi-users text-2xl"></i>
          </div>
        </div>
        <Button
          label="View Users"
          icon="pi pi-arrow-right"
          icon-pos="right"
          @click="router.push('/superadmin/users')"
          class="w-full"
          severity="info"
          outlined
        />
      </div>
    </div>

    <div class="col-12 lg:col-6 xl:col-3">
      <div
        class="card mb-0 bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800"
      >
        <div class="flex justify-content-between mb-3">
          <div>
            <span
              class="block text-green-600 dark:text-green-400 font-medium mb-3"
              >Active Clinics</span
            >
            <div class="text-green-900 dark:text-green-100 font-bold text-3xl">
              <Skeleton v-if="loading" width="4rem" height="2rem" />
              <span v-else>{{ stats.activeClinics }}</span>
            </div>
            <span class="text-green-600 dark:text-green-400 text-sm">
              of {{ stats.totalClinics }} total
            </span>
          </div>
          <div
            class="flex align-items-center justify-content-center bg-green-500 text-white border-round"
            style="width: 3.5rem; height: 3.5rem"
          >
            <i class="pi pi-building text-2xl"></i>
          </div>
        </div>
        <Button
          label="Manage Clinics"
          icon="pi pi-arrow-right"
          icon-pos="right"
          @click="router.push('/superadmin/clinics')"
          class="w-full"
          severity="success"
          outlined
        />
      </div>
    </div>

    <div class="col-12 lg:col-6 xl:col-3">
      <div
        class="card mb-0 bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-800"
      >
        <div class="flex justify-content-between mb-3">
          <div>
            <span
              class="block text-orange-600 dark:text-orange-400 font-medium mb-3"
              >Total Roles</span
            >
            <div
              class="text-orange-900 dark:text-orange-100 font-bold text-3xl"
            >
              <Skeleton v-if="loading" width="4rem" height="2rem" />
              <span v-else>{{ stats.totalRoles }}</span>
            </div>
            <span class="text-orange-600 dark:text-orange-400 text-sm">
              System & Custom
            </span>
          </div>
          <div
            class="flex align-items-center justify-content-center bg-orange-500 text-white border-round"
            style="width: 3.5rem; height: 3.5rem"
          >
            <i class="pi pi-shield text-2xl"></i>
          </div>
        </div>
        <Button
          label="View Roles"
          icon="pi pi-arrow-right"
          icon-pos="right"
          @click="router.push('/superadmin/roles')"
          class="w-full"
          severity="warning"
          outlined
        />
      </div>
    </div>

    <div class="col-12 lg:col-6 xl:col-3">
      <div
        class="card mb-0 bg-purple-50 dark:bg-purple-900/20 border-purple-200 dark:border-purple-800"
      >
        <div class="flex justify-content-between mb-3">
          <div>
            <span
              class="block text-purple-600 dark:text-purple-400 font-medium mb-3"
              >Total Permissions</span
            >
            <div
              class="text-purple-900 dark:text-purple-100 font-bold text-3xl"
            >
              <Skeleton v-if="loading" width="4rem" height="2rem" />
              <span v-else>{{ authStore.permissions.length }}</span>
            </div>
            <span class="text-purple-600 dark:text-purple-400 text-sm">
              Available permissions
            </span>
          </div>
          <div
            class="flex align-items-center justify-content-center bg-purple-500 text-white border-round"
            style="width: 3.5rem; height: 3.5rem"
          >
            <i class="pi pi-lock text-2xl"></i>
          </div>
        </div>
        <Button
          label="View Permissions"
          icon="pi pi-arrow-right"
          icon-pos="right"
          @click="router.push('/superadmin/permissions')"
          class="w-full"
          severity="help"
          outlined
        />
      </div>
    </div>

    <!-- Recent Users -->
    <div class="col-12 xl:col-6">
      <div class="card">
        <div class="flex align-items-center justify-content-between mb-4">
          <h5 class="m-0">Recent Users</h5>
          <Button
            label="View All"
            icon="pi pi-arrow-right"
            icon-pos="right"
            @click="router.push('/superadmin/users')"
            text
          />
        </div>

        <DataTable :value="recentUsers" :loading="loading">
          <Column field="fullName" header="Name" />
          <Column field="email" header="Email" />
          <Column field="roleName" header="Role">
            <template #body="{ data }">
              <Tag :value="data.roleName" severity="info" />
            </template>
          </Column>
          <Column field="isActive" header="Status">
            <template #body="{ data }">
              <Tag
                :value="data.isActive ? 'Active' : 'Inactive'"
                :severity="data.isActive ? 'success' : 'danger'"
              />
            </template>
          </Column>
          <template #empty>
            <div class="text-center p-4 text-500">No users yet</div>
          </template>
        </DataTable>
      </div>
    </div>

    <!-- Recent Clinics -->
    <div class="col-12 xl:col-6">
      <div class="card">
        <div class="flex align-items-center justify-content-between mb-4">
          <h5 class="m-0">Recent Clinics</h5>
          <Button
            label="View All"
            icon="pi pi-arrow-right"
            icon-pos="right"
            @click="router.push('/superadmin/clinics')"
            text
          />
        </div>

        <DataTable :value="recentClinics" :loading="loading">
          <Column field="name" header="Name" />
          <Column field="email" header="Email" />
          <Column field="status" header="Status">
            <template #body="{ data }">
              <Tag
                :value="data.status"
                :severity="data.status === 'Active' ? 'success' : 'warning'"
              />
            </template>
          </Column>
          <Column field="createdAt" header="Created">
            <template #body="{ data }">
              {{ formatDate(data.createdAt) }}
            </template>
          </Column>
          <template #empty>
            <div class="text-center p-4 text-500">No clinics yet</div>
          </template>
        </DataTable>
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="col-12">
      <div class="card">
        <h5>Quick Actions</h5>
        <div class="grid">
          <div class="col-12 md:col-6 lg:col-3">
            <Button
              label="Create User"
              icon="pi pi-user-plus"
              class="w-full"
              @click="router.push('/superadmin/users')"
              outlined
            />
          </div>
          <div class="col-12 md:col-6 lg:col-3">
            <Button
              label="Add Clinic"
              icon="pi pi-building"
              class="w-full"
              @click="router.push('/superadmin/clinics')"
              outlined
              severity="success"
            />
          </div>
          <div class="col-12 md:col-6 lg:col-3">
            <Button
              label="Manage Roles"
              icon="pi pi-shield"
              class="w-full"
              @click="router.push('/superadmin/roles')"
              outlined
              severity="warning"
            />
          </div>
          <div class="col-12 md:col-6 lg:col-3">
            <Button
              label="View Permissions"
              icon="pi pi-lock"
              class="w-full"
              @click="router.push('/superadmin/permissions')"
              outlined
              severity="help"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.card {
  padding: 1.5rem;
  border-radius: 12px;
}
</style>
