// import { defineStore } from 'pinia';
// import { ref, computed } from 'vue';
// import type { Patient } from '../../../shared/types';
// import { PatientService } from '@/service/PatientService';

// export const usePatientStore = defineStore('patient', () => {
//     // --- State ---
//     const patients = ref<Record<string, Patient>>({});
//     const loading = ref(false);
//     const isProcessing = ref(false);
//     const error = ref<string | null>(null);

//     // --- Getters ---
//     const patientList = computed(() => Object.values(patients.value)); // <-- expose array for UI
//     const getPatientById = (id: string) => computed(() => patients.value[id] ?? null);

//     // --- Actions ---
//     async function fetchPatients() {
//         loading.value = true;
//         error.value = null;
//         try {
//             const list = await PatientService.getPatients();
//             patients.value = list.reduce(
//                 (acc, patient) => {
//                     acc[patient.id] = patient;
//                     return acc;
//                 },
//                 {} as Record<string, Patient>
//             );
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch patients';
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     }

//     async function fetchPatientById(id: string) {
//         try {
//             const patient = await PatientService.getPatientById(id);
//             if (patient) patients.value[id] = patient;
//             return patient ?? null;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch patient';
//             throw err;
//         }
//     }

//     async function createPatient(newPatient: Partial<Patient>) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             const created = await PatientService.createPatient(newPatient);
//             patients.value[created.id] = created;
//             return created;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to create patient';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function updatePatient(updated: Patient) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await PatientService.updatePatient(updated);
//             patients.value[updated.id] = { ...updated };
//         } catch (err: any) {
//             error.value = err.message || 'Failed to update patient';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function deletePatient(id: string) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await PatientService.deletePatient(id);
//             delete patients.value[id];
//         } catch (err: any) {
//             error.value = err.message || 'Failed to delete patient';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function deletePatients(ids: string[]) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await PatientService.deletePatients(ids);
//             ids.forEach((id) => delete patients.value[id]);
//         } catch (err: any) {
//             error.value = err.message || 'Failed to delete patients';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     return {
//         // state
//         patients,
//         loading,
//         isProcessing,
//         error,

//         // getters
//         patientList,
//         getPatientById,

//         // actions
//         fetchPatients,
//         fetchPatientById,
//         createPatient,
//         updatePatient,
//         deletePatient,
//         deletePatients
//     };
// });
