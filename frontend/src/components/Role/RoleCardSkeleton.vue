<!-- src/components/Role/RoleCardSkeleton.vue -->
<template>
  <div class="relative flex flex-col justify-between overflow-hidden rounded-2xl border shadow-sm">
    <!-- Icon Area -->
    <div class="h-16 w-full">
      <Skeleton height="100%" width="100%" class="rounded-none" animation="wave" />
    </div>

    <!-- Main Content -->
    <div class="p-4 pb-2">
      <!-- Title -->
      <Skeleton height="1.5rem" width="70%" animation="wave" />

      <!-- Description -->
      <div class="mt-3 flex flex-col gap-2">
        <Skeleton height="1rem" width="100%" animation="wave" />
        <Skeleton height="1rem" width="80%" animation="wave" />
      </div>
    </div>

    <!-- Stats -->
    <div class="border-t px-4 py-3 text-sm flex flex-col gap-2">
      <div class="flex justify-between">
        <Skeleton height="1rem" width="30%" animation="wave" />
        <Skeleton height="1rem" width="20%" animation="wave" />
      </div>
      <div class="flex justify-between">
        <Skeleton height="1rem" width="30%" animation="wave" />
        <Skeleton height="1rem" width="20%" animation="wave" />
      </div>
      <div class="flex justify-between">
        <Skeleton height="1rem" width="30%" animation="wave" />
        <Skeleton height="1rem" width="20%" animation="wave" />
      </div>
    </div>

    <!-- Footer -->
    <div class="border-t px-4 py-2 flex justify-between items-center">
      <Skeleton height="1rem" width="40%" animation="wave" />
      <Skeleton height="2rem" width="25%" animation="wave" />
    </div>
  </div>
</template>

<script setup>
  import Skeleton from 'primevue/skeleton';
</script>
