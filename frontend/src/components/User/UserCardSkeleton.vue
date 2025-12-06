<template>
  <div
    class="relative flex flex-col justify-between overflow-hidden rounded-xl border border-surface-200 shadow-sm bg-white dark:bg-surface-900 dark:border-surface-700"
  >
    <div class="absolute top-3 right-3 flex justify-end">
      <Skeleton height="1.5rem" width="1.5rem" class="rounded" animation="wave" />
    </div>

    <div class="p-5 flex flex-col items-center text-center mt-2">
      <Skeleton shape="circle" size="4rem" class="mb-3" animation="wave" />

      <Skeleton height="1.5rem" width="70%" class="mb-2" animation="wave" />

      <Skeleton height="1rem" width="50%" class="mb-4" animation="wave" />

      <div class="flex justify-center gap-2 w-full">
        <Skeleton height="1.5rem" width="30%" class="rounded" animation="wave" />
        <Skeleton height="1.5rem" width="20%" class="rounded" animation="wave" />
      </div>
    </div>

    <div class="px-5 pb-4">
      <div
        class="rounded-lg border border-surface-100 dark:border-surface-700 p-3 flex flex-col gap-2 bg-surface-50 dark:bg-surface-800/50"
      >
        <div class="flex justify-between items-center">
          <Skeleton height="0.8rem" width="30%" animation="wave" />
          <Skeleton height="0.8rem" width="40%" animation="wave" />
        </div>
        <div class="flex justify-between items-center">
          <Skeleton height="0.8rem" width="30%" animation="wave" />
          <Skeleton height="0.8rem" width="25%" animation="wave" />
        </div>
        <div class="flex justify-between items-center">
          <Skeleton height="0.8rem" width="30%" animation="wave" />
          <Skeleton height="0.8rem" width="35%" animation="wave" />
        </div>
      </div>
    </div>

    <div
      class="mt-auto flex items-center justify-between p-3 border-t border-surface-100 dark:border-surface-700"
    >
      <Skeleton height="1rem" width="20%" animation="wave" />
      <div class="flex gap-1">
        <Skeleton shape="circle" size="1.5rem" animation="wave" />
        <Skeleton shape="circle" size="1.5rem" animation="wave" />
        <Skeleton shape="circle" size="1.5rem" animation="wave" />
      </div>
    </div>
  </div>
</template>

<script setup>
  import Skeleton from 'primevue/skeleton';
</script>
