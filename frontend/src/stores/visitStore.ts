// import { defineStore } from 'pinia';
// import { ref, computed } from 'vue';
// import type { PatientVisit } from '../../../shared/types';
// import { VisitService } from '@/service/VisitService';

// export const useVisitStore = defineStore('visit', () => {
//     // --- State ---
//     const visits = ref<Record<string, PatientVisit[]>>({});
//     const loading = ref(false);
//     const isProcessing = ref(false);
//     const error = ref<string | null>(null);

//     // --- Helpers ---
//     function sortVisits(list: PatientVisit[]) {
//         return [...list].sort((a, b) => {
//             const aTime = a.date instanceof Date ? a.date.getTime() : new Date(a.date).getTime();
//             const bTime = b.date instanceof Date ? b.date.getTime() : new Date(b.date).getTime();
//             return bTime - aTime; // newest first
//         });
//     }

//     // --- Getters ---
//     const visitsByPatient = (patientId: string) => computed(() => sortVisits(visits.value[patientId] ?? []));

//     const allVisits = computed(() => sortVisits(Object.values(visits.value).flat()));

//     async function fetchAllVisits() {
//         loading.value = true;
//         error.value = null;
//         try {
//             const data = await VisitService.getVisits();
//             const grouped: Record<string, PatientVisit[]> = {};
//             for (const visit of data) {
//                 if (!grouped[visit.patientId]) grouped[visit.patientId] = [];
//                 grouped[visit.patientId].push(visit);
//             }
//             // sort per patient
//             for (const key in grouped) {
//                 grouped[key] = sortVisits(grouped[key]);
//             }
//             visits.value = grouped;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch all visits';
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     }

//     // --- Actions ---
//     async function fetchVisitsByPatientId(patientId: string) {
//         loading.value = true;
//         error.value = null;
//         try {
//             const data = await VisitService.getVisitsByPatientId(patientId); // should already be sorted
//             visits.value[patientId] = sortVisits(data);
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch patient visits';
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     }

//     async function fetchVisitById(patientId: string, id: string): Promise<PatientVisit | null> {
//         try {
//             const visit = await VisitService.getVisitById(id);
//             if (!visit) return null;

//             const list = visits.value[patientId] ?? [];
//             const index = list.findIndex((v) => v.id === id);

//             if (index !== -1) {
//                 list[index] = visit;
//             } else {
//                 list.push(visit);
//             }

//             visits.value[patientId] = sortVisits(list);
//             return visit;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to fetch visit';
//             throw err;
//         }
//     }

//     async function createVisit(newVisit: PatientVisit) {
//         if (!newVisit.patientId) throw new Error('Visit must have patientId');
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             const id = await VisitService.createVisit(newVisit);
//             const list = visits.value[newVisit.patientId] ?? [];
//             list.push({ ...newVisit, id });
//             visits.value[newVisit.patientId] = sortVisits(list);
//             return id;
//         } catch (err: any) {
//             error.value = err.message || 'Failed to create visit';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function updateVisit(updated: PatientVisit) {
//         if (!updated.id || !updated.patientId) throw new Error('Visit id and patientId are required');
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await VisitService.updateVisit(updated);
//             const list = visits.value[updated.patientId] ?? [];
//             const index = list.findIndex((v) => v.id === updated.id);
//             if (index !== -1) list[index] = { ...updated };
//             visits.value[updated.patientId] = sortVisits(list);
//         } catch (err: any) {
//             error.value = err.message || 'Failed to update visit';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function deleteVisit(patientId: string, id: string) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await VisitService.deleteVisit(id);
//             visits.value[patientId] = sortVisits((visits.value[patientId] ?? []).filter((v) => v.id !== id));
//         } catch (err: any) {
//             error.value = err.message || 'Failed to delete visit';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     async function deleteVisits(patientId: string, ids: string[]) {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await VisitService.deleteVisits(ids);
//             visits.value[patientId] = sortVisits((visits.value[patientId] ?? []).filter((v) => !ids.includes(v.id!)));
//         } catch (err: any) {
//             error.value = err.message || 'Failed to delete visits';
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     }

//     return {
//         visits,
//         loading,
//         isProcessing,
//         error,
//         visitsByPatient,
//         allVisits,
//         fetchAllVisits,
//         fetchVisitsByPatientId,
//         fetchVisitById,
//         createVisit,
//         updateVisit,
//         deleteVisit,
//         deleteVisits
//     };
// });
