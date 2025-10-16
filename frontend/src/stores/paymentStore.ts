// // src/stores/paymentStore.ts
// import { defineStore } from 'pinia';
// import { ref, computed } from 'vue';
// import { PaymentService } from '@/service/PaymentService';
// import type { Payment, PaymentWithPatient } from '@/../../shared/types';

// export const usePaymentStore = defineStore('payment', () => {
//     // --- State (Entity pattern) ---
//     // One source of truth
//     const byId = ref<Record<string, PaymentWithPatient>>({});
//     const allIds = ref<string[]>([]);
//     const idsByPatient = ref<Record<string, string[]>>({});

//     const loading = ref(false);
//     const isProcessing = ref(false);
//     const error = ref<string | null>(null);

//     // --- Helpers ---
//     function sortPayments(list: PaymentWithPatient[]) {
//         return [...list].sort((a, b) => {
//             const aTime = a.paidAt instanceof Date ? a.paidAt.getTime() : new Date(a.paidAt).getTime();
//             const bTime = b.paidAt instanceof Date ? b.paidAt.getTime() : new Date(b.paidAt).getTime();
//             return bTime - aTime; // newest first
//         });
//     }

//     function upsertMany(list: PaymentWithPatient[]) {
//         for (const p of list) {
//             byId.value[p.id!] = p;

//             if (!allIds.value.includes(p.id!)) {
//                 allIds.value.push(p.id!);
//             }

//             const pid = p.patientId;
//             const bucket = idsByPatient.value[pid] ?? (idsByPatient.value[pid] = []);
//             if (!bucket.includes(p.id!)) bucket.push(p.id!);
//         }

//         // 🔹 enforce global order
//         allIds.value = sortPayments(allIds.value.map((id) => byId.value[id])).map((p) => p.id!);
//     }

//     function replacePatientIndex(patientId: string, list: PaymentWithPatient[]) {
//         // rebuild the index for that patient to match exactly the fetched list
//         const ids = list.map((p) => p.id!) as string[];
//         idsByPatient.value[patientId] = ids;

//         // upsert the entities
//         upsertMany(list);
//     }

//     function removeIdEverywhere(id: string) {
//         // remove from byId
//         const entity = byId.value[id];
//         if (entity) {
//             const pid = entity.patientId;
//             // remove from patient index
//             idsByPatient.value[pid] = (idsByPatient.value[pid] ?? []).filter((x) => x !== id);
//         }
//         delete byId.value[id];

//         // remove from allIds
//         allIds.value = allIds.value.filter((x) => x !== id);
//     }

//     // --- Getters ---
//     const paymentsAll = computed<PaymentWithPatient[]>(() => allIds.value.map((id) => byId.value[id]).filter(Boolean));

//     const paymentsByPatient = (patientId: string) => computed<PaymentWithPatient[]>(() => (idsByPatient.value[patientId] ?? []).map((id) => byId.value[id]).filter(Boolean));

//     // --- Actions ---
//     async function fetchPayments(patientId?: string) {
//         loading.value = true;
//         error.value = null;
//         try {
//             if (patientId) {
//                 const list = await PaymentService.getByPatientId(patientId); // PaymentWithPatient[]
//                 replacePatientIndex(patientId, list);
//             } else {
//                 const list = await PaymentService.getPayments(); // PaymentWithPatient[]
//                 // Reset and rebuild for "all" load
//                 byId.value = {};
//                 allIds.value = [];
//                 idsByPatient.value = {};
//                 upsertMany(list);
//             }
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch payments';
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     }

//     async function createPayment(input: Payment) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             const id = await PaymentService.createPayment(input);
//             // Fetch that patient's payments so we get PaymentWithPatient (joined) in store
//             await fetchPayments(input.patientId);
//             return id;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to create payment';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function updatePayment(input: Payment) {
//         if (!input.id) throw new Error('Payment ID required');
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await PaymentService.updatePayment(input);
//             // Refresh that patient's bucket to keep store consistent
//             await fetchPayments(input.patientId);
//         } catch (err: any) {
//             error.value = err.message || 'Failed to update payment';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function deletePayment(id: string, patientId?: string) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await PaymentService.deletePayment(id);
//             if (!patientId) {
//                 // try to infer from store if not provided
//                 const existing = byId.value[id];
//                 if (existing) patientId = existing.patientId;
//             }
//             removeIdEverywhere(id);
//         } catch (err: any) {
//             error.value = err.message || 'Failed to delete payment';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function deletePayments(ids: string[], patientId?: string) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await PaymentService.deletePayments(ids);

//             if (patientId) {
//                 // remove from patient index in one go
//                 idsByPatient.value[patientId] = (idsByPatient.value[patientId] ?? []).filter((id) => !ids.includes(id));
//             }
//             // still remove from byId and allIds
//             for (const id of ids) removeIdEverywhere(id);
//         } catch (err: any) {
//             error.value = err.message || 'Failed to delete payments';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     return {
//         // state
//         byId,
//         allIds,
//         idsByPatient,
//         loading,
//         isProcessing,
//         error,

//         // getters
//         paymentsAll,
//         paymentsByPatient,

//         // actions
//         fetchPayments,
//         createPayment,
//         updatePayment,
//         deletePayment,
//         deletePayments
//     };
// });
