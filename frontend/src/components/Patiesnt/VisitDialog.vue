<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :style="{ width: '100%', maxWidth: '800px' }"
    :header="localVisit.id ? 'Edit Visit' : 'Add Visit'"
    modal
  >
    <div class="flex flex-col gap-6">
      <!-- Practitioner -->
      <div>
        <label for="visit-practitioner" class="mb-2 block font-bold">Practitioner Name</label>
        <InputText
          id="visit-practitioner"
          v-model.trim="localVisit.practitioner"
          autofocus
          :invalid="submitted && !localVisit.practitioner"
          class="w-full"
        />
        <small v-if="submitted && !localVisit.practitioner" class="text-red-500"
          >Name is required.</small
        >
      </div>

      <!-- Reason & Visit Type -->
      <div class="grid-nogutter grid gap-4 md:grid-cols-2">
        <div>
          <label for="visit-reason" class="mb-2 block font-bold">Reason</label>
          <InputText
            id="visit-reason"
            v-model.trim="localVisit.reason"
            :invalid="submitted && !localVisit.reason"
            class="w-full"
          />
          <small v-if="submitted && !localVisit.reason" class="text-red-500"
            >Visit Reason is required.</small
          >
        </div>

        <div class="grid-nogutter grid gap-4 md:grid-cols-2">
          <!-- Visit Type -->
          <div>
            <label for="visit-type" class="mb-2 block font-bold">Visit Type</label>
            <!-- Replace [] with VisitType when available -->
            <Select
              id="visit-type"
              v-model="localVisit.visitType"
              :options="[]"
              optionLabel="label"
              optionValue="value"
              placeholder="Select Visit Type"
              :invalid="submitted && !localVisit.visitType"
              class="w-full"
            />
            <small v-if="submitted && !localVisit.visitType" class="text-red-500"
              >Visit Type is required.</small
            >
          </div>

          <!-- Amount -->
          <div>
            <label for="fee" class="mb-2 block font-bold">Fee</label>
            <InputNumber
              id="fee"
              v-model="localVisit.fee"
              mode="currency"
              currency="ILS"
              locale="en-US"
              :min="0"
              :step="0.01"
              :minFractionDigits="0"
              :maxFractionDigits="2"
              inputId="fee-amount"
              aria-label="Fee Amount"
              :invalid="submitted && !isFeeValid"
              class="w-full"
            />

            <small v-if="submitted && !isFeeValid" class="text-red-500"
              >Amount must be a positive number.</small
            >
          </div>
        </div>
      </div>

      <!-- Dates -->
      <div class="grid-nogutter grid gap-4 md:grid-cols-2">
        <!-- Visit Date -->
        <div>
          <label for="visit-date" class="mb-2 block font-bold">Visit Date</label>
          <DatePicker
            id="visit-date"
            v-model="localVisit.date"
            showIcon
            showButtonBar
            :invalid="submitted && !localVisit.date"
            class="w-full"
          />
          <small v-if="submitted && !localVisit.date" class="text-red-500"
            >Visit Date is required.</small
          >
        </div>

        <!-- Next Appointment -->
        <div>
          <label for="visit-appointment" class="mb-2 block font-bold">Next Appointment</label>
          <DatePicker
            id="visit-appointment"
            v-model="localVisit.nextAppointment"
            showIcon
            showButtonBar
            class="w-full"
          />
        </div>
      </div>

      <!-- Description -->
      <div>
        <label for="visit-description" class="mb-2 block font-bold">Description</label>
        <Editor
          id="visit-description"
          v-model="localVisit.description"
          editorStyle="height: 320px"
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

<script setup lang="ts">
  import { ref, watch, computed } from 'vue';
  // import { VISIT_TYPES, type PatientVisit } from '@/../../shared/types';

  const props = defineProps<{
    visible: boolean;
    visit: Partial<any>; // Replace 'any' with 'PatientVisit' when the type is available
    isProcessing?: boolean;
  }>();

  // const VisitType = ref(VISIT_TYPES.map((v) => ({ label: v, value: v })));

  const emit = defineEmits(['update:visible', 'save', 'cancel']);

  const localVisit = ref<Partial<any>>({ ...props.visit }); // Replace 'any' with 'PatientVisit' when the type is available
  const submitted = ref(false);

  watch(
    () => props.visible,
    (newVal) => {
      if (newVal) {
        submitted.value = false;
        localVisit.value = { ...props.visit };
      }
    },
  );

  // Validation
  const isFeeValid = computed(
    () =>
      localVisit.value.fee !== null &&
      localVisit.value.fee !== undefined &&
      localVisit.value.fee >= 0,
  );
  const isFormValid = computed(
    () =>
      !!localVisit.value.practitioner &&
      !!localVisit.value.reason &&
      !!localVisit.value.visitType &&
      !!localVisit.value.date &&
      isFeeValid.value,
  );

  function onSave() {
    if (props.isProcessing) return; // prevent multiple clicks
    submitted.value = true;
    if (!isFormValid.value) return;

    emit('save', { ...localVisit.value });
  }

  function onCancel() {
    emit('cancel');
    emit('update:visible', false);
  }
</script>
