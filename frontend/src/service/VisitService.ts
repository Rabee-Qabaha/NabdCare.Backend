// import { addDoc, collection, deleteDoc, doc, getDoc, getDocs, updateDoc, query, where, Timestamp, orderBy } from 'firebase/firestore';
// import { db } from '@/firebase';
// import type { PatientVisit } from '../../../shared/types';
// import { QueryDocumentSnapshot } from 'firebase/firestore';
// import type { SnapshotOptions } from 'firebase/firestore';

// // ---------- Firestore Converter ----------
// const visitConverter = {
//     toFirestore(visit: PatientVisit) {
//         return {
//             ...visit,
//             date: visit.date instanceof Date ? Timestamp.fromDate(visit.date) : visit.date,
//             nextAppointment: visit.nextAppointment instanceof Date ? Timestamp.fromDate(visit.nextAppointment) : visit.nextAppointment,
//             updatedAt: Timestamp.now()
//         };
//     },

//     fromFirestore(snapshot: QueryDocumentSnapshot, options: SnapshotOptions): PatientVisit {
//         const data = snapshot.data(options);
//         return {
//             id: snapshot.id,
//             patientId: data.patientId,
//             date: data.date?.toDate ? data.date.toDate() : data.date,
//             visitType: data.visitType,
//             reason: data.reason,
//             fee: data.fee,
//             description: data.description,
//             practitioner: data.practitioner,
//             nextAppointment: data.nextAppointment?.toDate ? data.nextAppointment.toDate() : data.nextAppointment
//         };
//     }
// };

// const visitsRef = collection(db, 'visits').withConverter(visitConverter);

// // ---------- Service Layer ----------
// export const VisitService = {
//     async getVisits(): Promise<PatientVisit[]> {
//         try {
//             const q = query(visitsRef, orderBy('date', 'desc'));
//             const snapshot = await getDocs(q);
//             return snapshot.docs.map((doc) => doc.data());
//         } catch (error) {
//             console.error('Error fetching visits:', error);
//             throw error;
//         }
//     },

//     async getVisitsByPatientId(patientId: string): Promise<PatientVisit[]> {
//         try {
//             const q = query(visitsRef, where('patientId', '==', patientId), orderBy('date', 'desc'));
//             const snapshot = await getDocs(q);
//             return snapshot.docs.map((doc) => doc.data());
//         } catch (error) {
//             console.error('Error fetching visits by patientId:', error);
//             throw error;
//         }
//     },

//     async getVisitById(id: string): Promise<PatientVisit | null> {
//         try {
//             const docRef = doc(db, 'visits', id).withConverter(visitConverter);
//             const docSnap = await getDoc(docRef);
//             return docSnap.exists() ? docSnap.data() : null;
//         } catch (error) {
//             console.error('Error fetching visit:', error);
//             throw error;
//         }
//     },

//     async createVisit(visit: PatientVisit): Promise<string> {
//         try {
//             if (!visit.patientId) throw new Error('patientId is required');
//             const docRef = await addDoc(visitsRef, visit);
//             return docRef.id;
//         } catch (error) {
//             console.error('Error creating visit:', error);
//             throw error;
//         }
//     },

//     async updateVisit(updated: PatientVisit): Promise<void> {
//         if (!updated.id) throw new Error('Visit id is required for update');
//         try {
//             const docRef = doc(db, 'visits', updated.id).withConverter(visitConverter);

//             const cleanData = { ...updated };
//             Object.keys(cleanData).forEach((key) => {
//                 if (cleanData[key as keyof typeof cleanData] === undefined) {
//                     delete cleanData[key as keyof typeof cleanData];
//                 }
//             });

//             await updateDoc(docRef, cleanData as any);
//         } catch (error) {
//             console.error('Error updating visit:', error);
//             throw error;
//         }
//     },

//     async deleteVisit(id: string): Promise<void> {
//         try {
//             const docRef = doc(db, 'visits', id);
//             await deleteDoc(docRef);
//         } catch (error) {
//             console.error('Error deleting visit:', error);
//             throw error;
//         }
//     },

//     async deleteVisits(ids: string[]): Promise<void> {
//         try {
//             const batchDeletes = ids.map(async (id) => {
//                 const docRef = doc(db, 'visits', id);
//                 await deleteDoc(docRef);
//             });
//             await Promise.all(batchDeletes);
//         } catch (error) {
//             console.error('Error deleting visits:', error);
//             throw error;
//         }
//     }
// };
