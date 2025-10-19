<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :style="{ width: '100%', maxWidth: '600px' }"
    :header="localXRay.id ? 'Edit X-Ray' : 'Add X-Ray'"
    modal
  >
    <div class="flex flex-col gap-4">
      <!-- Image Upload / Preview Card -->
      <div>
        <label class="block font-bold mb-2">Image</label>

        <!-- Drag-and-drop container -->
        <div
          class="border-2 border-dashed rounded-lg p-4 w-full min-h-[180px] flex flex-col sm:flex-row sm:items-center sm:justify-start gap-4"
          :class="{
            'border-red-500':
              submitted &&
              !localXRay.imageUrl &&
              !selectedFile &&
              !fileIsUploading,
          }"
          @drop.prevent="onDrop"
          @dragover.prevent
        >
          <!-- Image exists -->
          <template v-if="selectedFile || localXRay.imageUrl">
            <div
              class="flex-shrink-0 w-full sm:w-28 h-36 sm:h-28 flex items-center justify-center"
            >
              <img
                :src="previewUrl || localXRay.imageUrl"
                alt="X-Ray"
                class="w-full h-full object-contain rounded"
              />
            </div>

            <!-- File info & status -->
            <div class="flex-1 flex flex-col justify-center gap-1 min-w-0">
              <div class="font-semibold truncate">
                {{
                  selectedFile
                    ? selectedFile.name
                    : getFileName(localXRay.imageUrl)
                }}
              </div>
              <div
                class="flex flex-wrap items-center gap-2 text-sm text-surface-500"
              >
                <span>{{
                  selectedFile ? formatSize(selectedFile.size) : ""
                }}</span>
                <span
                  class="inline-block px-2 py-0.5 rounded text-xs text-white"
                  :class="selectedFile ? 'bg-yellow-500' : 'bg-green-500'"
                >
                  {{ selectedFile ? "Pending" : "Completed" }}
                </span>
              </div>
            </div>

            <!-- Remove button -->
            <div class="flex-shrink-0 mt-2 sm:mt-0">
              <Button
                icon="pi pi-times"
                class="p-button-text"
                @click="removeFile"
              />
            </div>
          </template>

          <!-- Placeholder -->
          <template v-else>
            <div
              class="flex flex-col items-center justify-center w-full h-full text-center"
            >
              <i class="pi pi-cloud-upload text-4xl text-surface-400 mb-2"></i>
              <p>Drag and drop file here</p>
              <p>or</p>
              <Button
                label="Choose X-Ray image"
                class="mt-2"
                @click="fileInput!.click()"
              />
            </div>
          </template>

          <!-- Hidden file input -->
          <input
            ref="fileInput"
            type="file"
            accept="image/*"
            class="hidden"
            @change="onFileChange"
          />
        </div>

        <small
          v-if="submitted && !localXRay.imageUrl && !selectedFile"
          class="text-red-500 mt-2 block"
        >
          Image is required.
        </small>
      </div>

      <!-- Description -->
      <div>
        <label class="block font-bold mb-2">Description</label>
        <Textarea v-model="localXRay.description" rows="3" class="w-full" />
      </div>
    </div>

    <!-- Footer -->
    <template #footer>
      <Button label="Cancel" icon="pi pi-times" text @click="onCancel" />
      <Button
        label="Save"
        icon="pi pi-check"
        :loading="isProcessing || fileIsUploading"
        @click="onSave"
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue";
import Dialog from "primevue/dialog";
import Button from "primevue/button";
import Textarea from "primevue/textarea";
// import { getStorage, ref as storageRef, uploadBytes, getDownloadURL, deleteObject } from 'firebase/storage';
import { useToast } from "primevue/usetoast";
// import type { XRay } from '@/../../shared/types';

const props = defineProps<{
  visible: boolean;
  xray: Partial<any>; // Replace 'any' with 'XRay' when the type is available
  isProcessing?: boolean;
}>();
const emit = defineEmits(["update:visible", "save", "cancel"]);
const toast = useToast();

const submitted = ref(false);
const localXRay = ref<Partial<any>>({});
const fileInput = ref<HTMLInputElement>();
const selectedFile = ref<File | null>(null);
const fileIsUploading = ref(false);
const previewUrl = ref<string | null>(null);

watch(
  () => props.visible,
  (val) => {
    if (val) {
      submitted.value = false;
      localXRay.value = { ...props.xray };
      selectedFile.value = null;
      previewUrl.value = null;
      if (fileInput.value) fileInput.value.value = "";
    }
  },
  { immediate: true }
);

const isFormValid = computed(
  () =>
    !!localXRay.value.imageUrl || !!selectedFile.value || fileIsUploading.value
);

function onFileChange(event: Event) {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    selectedFile.value = target.files[0];
    previewUrl.value = URL.createObjectURL(selectedFile.value);
  }
}

function onDrop(event: DragEvent) {
  if (event.dataTransfer?.files && event.dataTransfer.files[0]) {
    selectedFile.value = event.dataTransfer.files[0];
    previewUrl.value = URL.createObjectURL(selectedFile.value);
  }
}

function removeFile() {
  selectedFile.value = null;
  previewUrl.value = null;
  localXRay.value.imageUrl = undefined;
  if (fileInput.value) fileInput.value.value = "";
}

function generateFileName(file: File, patientId: string): string {
  const ext = file.name.split(".").pop() || "png";
  const d = new Date();
  const yyyy = d.getFullYear();
  const MM = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  const HH = String(d.getHours()).padStart(2, "0");
  const mm = String(d.getMinutes()).padStart(2, "0");
  const ss = String(d.getSeconds()).padStart(2, "0");
  const timestamp = `${yyyy}${MM}${dd}_${HH}${mm}${ss}`;
  return `xrays/${patientId}_${timestamp}.${ext}`;
}

async function onSave() {
  submitted.value = true;
  if (props.isProcessing || fileIsUploading.value) return;

  let fileRefPath: string | null = null;
  let uploadedImageUrl: string | null = null;

  // try {
  //     if (selectedFile.value) {
  //         fileIsUploading.value = true;

  //         const storage = getStorage();
  //         fileRefPath = generateFileName(selectedFile.value, localXRay.value.patientId!);
  //         const fileRef = storageRef(storage, fileRefPath);

  //         // Upload file
  //         const snapshot = await uploadBytes(fileRef, selectedFile.value);
  //         uploadedImageUrl = await getDownloadURL(snapshot.ref);

  //         localXRay.value.imageUrl = uploadedImageUrl;
  //     }

  //     if (!isFormValid.value) return;

  //     // Emit save to parent component
  //     await emit('save', { ...localXRay.value });

  //     // Clear file selection after successful save
  //     selectedFile.value = null;
  //     previewUrl.value = null;
  // } catch (error) {
  //     console.error('Failed to save X-ray:', error);

  //     // Delete uploaded file if save failed
  //     if (fileRefPath) {
  //         try {
  //             const storage = getStorage();
  //             const fileRef = storageRef(storage, fileRefPath);
  //             await deleteObject(fileRef);
  //             console.log('Deleted orphaned file:', fileRefPath);
  //         } catch (delError) {
  //             console.error('Failed to delete orphaned image:', delError);
  //         }
  //     }

  //     toast.add({
  //         severity: 'error',
  //         summary: 'Save Failed',
  //         detail: 'Failed to save X-ray.',
  //         life: 3000
  //     });
  // } finally {
  //     fileIsUploading.value = false;
  // }
}

function onCancel() {
  selectedFile.value = null;
  previewUrl.value = null;
  emit("cancel");
  emit("update:visible", false);
}

function formatSize(bytes: number) {
  const k = 1024;
  const sizes = ["B", "KB", "MB"];
  if (bytes === 0) return "0 B";
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
}

function getFileName(url?: string) {
  return url ? url.split("/").pop() : "";
}
</script>
