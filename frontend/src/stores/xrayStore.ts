// // src/stores/xrayStore.ts
// import { defineStore } from 'pinia';
// import { ref, computed } from 'vue';
// import type { XRay } from '../../../shared/types';
// import { XRayService } from '@/service/XRayService';
// import { useAuthStore } from './authStore';

// export const useXRayStore = defineStore('xray', () => {
//     // --- State ---
//     const xrays = ref<Record<string, XRay>>({});
//     const loading = ref(false);
//     const isProcessing = ref(false);
//     const error = ref<string | null>(null);

//     const authStore = useAuthStore();

//     // --- Getters ---
//     const xrayList = computed(() => Object.values(xrays.value));
//     const getXRayById = (id: string) => computed(() => xrays.value[id] ?? null);
//     const getXRayByPatient = (patientId: string) =>
//         computed(() =>
//             Object.values(xrays.value)
//                 .filter((x) => x.patientId === patientId)
//                 .sort((a, b) => b.uploadedAt.getTime() - a.uploadedAt.getTime())
//         );

//     // --- Actions ---
//     async function fetchXrays() {
//         loading.value = true;
//         error.value = null;
//         try {
//             const list = await XRayService.getXrays();
//             xrays.value = list.reduce(
//                 (acc, xray) => {
//                     acc[xray.id] = xray;
//                     return acc;
//                 },
//                 {} as Record<string, XRay>
//             );
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch X-rays';
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     }

//     async function fetchXraysByPatient(patientId: string) {
//         loading.value = true;
//         error.value = null;
//         try {
//             const list = await XRayService.getXrays(patientId);
//             list.forEach((x) => (xrays.value[x.id] = x));
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch X-rays for patient';
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     }

//     async function createXRay(newXRay: Partial<XRay>) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             const user = authStore.currentUser;
//             if (!user) throw new Error('User must be logged in to create an X-ray');

//             const created = await XRayService.createXray({
//                 ...newXRay,
//                 uploadedBy: user.displayName,
//                 uploadedAt: new Date()
//             });

//             xrays.value[created.id] = created;
//             return created;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to create X-ray';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function updateXRay(updated: XRay) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             const user = authStore.currentUser;
//             if (!user) throw new Error('User must be logged in to update an X-ray');

//             const updatedWithUser = {
//                 ...updated,
//                 uploadedBy: user.displayName!,
//                 updatedAt: new Date()
//             };

//             await XRayService.updateXray(updatedWithUser);
//             xrays.value[updated.id] = updatedWithUser;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to update X-ray';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function deleteXRay(id: string) {
//         isProcessing.value = true;
//         error.value = null;
//         const old = xrays.value[id];
//         delete xrays.value[id];
//         try {
//             await XRayService.deleteXray(id);
//         } catch (err: any) {
//             xrays.value[id] = old;
//             error.value = err.message || 'Failed to delete X-ray';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     return {
//         // state
//         xrays,
//         loading,
//         isProcessing,
//         error,

//         // getters
//         xrayList,
//         getXRayById,
//         getXRayByPatient,

//         // actions
//         fetchXrays,
//         fetchXraysByPatient,
//         createXRay,
//         updateXRay,
//         deleteXRay
//     };
// });
