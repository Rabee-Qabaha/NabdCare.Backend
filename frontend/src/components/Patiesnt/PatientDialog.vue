<script setup lang="ts">
  import { ref, watch, computed } from 'vue';
  // import type { Patient } from '@/../../shared/types';

  const props = defineProps<{
    visible: boolean;
    patient: Partial<any>; // Replace 'any' with 'Patient' when the type is available
    isProcessing?: boolean;
  }>();

  const emit = defineEmits(['update:visible', 'save', 'cancel']);

  const localPatient = ref<Partial<any>>({ ...props.patient }); // Replace 'any' with 'Patient' when the type is available
  const submitted = ref(false);

  watch(
    () => props.visible,
    (newVal) => {
      if (newVal) {
        submitted.value = false; // reset validation state
        localPatient.value = { ...props.patient }; // reset form with fresh data
      }
    },
  );

  const isFormValid = computed(
    () =>
      !!localPatient.value.name?.trim() &&
      !!localPatient.value.gender &&
      !!localPatient.value.phone?.trim(),
  );

  function onSave() {
    if (props.isProcessing) return; // prevent multiple clicks
    submitted.value = true;
    if (!isFormValid.value) return;

    emit('save', { ...localPatient.value } as any); // Replace 'any' with 'Patient' when the type is available
  }

  function onCancel() {
    emit('cancel');
    emit('update:visible', false);
  }

  const genderOptions = [
    { label: 'Male', value: 'Male' },
    { label: 'Female', value: 'Female' },
  ];
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :style="{ width: '500px' }"
    :header="localPatient.id ? 'Edit Patient' : 'Add Patient'"
    modal
  >
    <div class="flex flex-col gap-6">
      <!-- Name -->
      <div>
        <label for="patient-name" class="mb-3 block font-bold">Name</label>
        <InputText
          id="patient-name"
          v-model.trim="localPatient.name"
          :invalid="submitted && !localPatient.name"
          required
          fluid
        />
        <small v-if="submitted && !localPatient.name" class="text-red-500">Name is required.</small>
      </div>

      <!-- DOB & Gender -->
      <div class="grid grid-cols-12 gap-4">
        <div class="col-span-6">
          <label for="patient-dob" class="mb-3 block font-bold">Date of Birth</label>
          <DatePicker id="patient-dob" v-model="localPatient.dob" showIcon showButtonBar />
        </div>
        <div class="col-span-6">
          <label for="patient-gender" class="mb-3 block font-bold">Gender</label>
          <Select
            id="patient-gender"
            v-model="localPatient.gender"
            :options="genderOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Select Gender"
            :invalid="submitted && !localPatient.gender"
            fluid
          />
          <small v-if="submitted && !localPatient.gender" class="text-red-500">
            Gender is required.
          </small>
        </div>
      </div>

      <!-- Phone -->
      <div>
        <label for="patient-phone" class="mb-3 block font-bold">Phone</label>
        <InputText
          id="patient-phone"
          v-model.trim="localPatient.phone"
          :invalid="submitted && !localPatient.phone"
          required
          fluid
        />
        <small v-if="submitted && !localPatient.phone" class="text-red-500">
          Phone is required.
        </small>
      </div>

      <!-- Address -->
      <div>
        <label for="patient-address" class="mb-3 block font-bold">Address</label>
        <InputText id="patient-address" v-model.trim="localPatient.address" fluid />
      </div>

      <!-- Description -->
      <div>
        <label for="patient-description" class="mb-3 block font-bold">Description</label>
        <Textarea
          id="patient-description"
          v-model="localPatient.description"
          rows="3"
          cols="20"
          fluid
        />
      </div>
    </div>

    <!-- Footer -->
    <template #footer>
      <Button label="Cancel" icon="pi pi-times" text @click="onCancel" />
      <Button label="Save" icon="pi pi-check" :loading="props.isProcessing" @click="onSave" />
    </template>
  </Dialog>
</template>
