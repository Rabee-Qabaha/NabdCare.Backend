// import { collection, getDocs, getDoc, doc, addDoc, updateDoc, deleteDoc, serverTimestamp, Timestamp, orderBy, query } from 'firebase/firestore';

// // import { db } from '@/firebase';
// // import type { Patient } from '../../../shared/types';

// function mapDocToPatient(docSnap: any): Patient {
//     const data = docSnap.data();
//     return {
//         id: docSnap.id,
//         name: data.name,
//         dob: data.dob instanceof Timestamp ? data.dob.toDate() : data.dob,
//         gender: data.gender,
//         phone: data.phone,
//         address: data.address,
//         description: data.description ?? '',
//         createdAt: data.createdAt instanceof Timestamp ? data.createdAt.toDate() : data.createdAt
//     };
// }

// export const PatientService = {
//     async getPatients(): Promise<Patient[]> {
//         try {
//             const patientsQuery = query(collection(db, 'patients'), orderBy('createdAt', 'desc'));

//             const snapshot = await getDocs(patientsQuery);
//             return snapshot.docs.map(mapDocToPatient);
//         } catch (error) {
//             console.error('Error fetching patients:', error);
//             throw error;
//         }
//     },

//     async getPatientById(id: string): Promise<Patient | undefined> {
//         try {
//             const docRef = doc(db, 'patients', id);
//             const docSnap = await getDoc(docRef);

//             if (docSnap.exists()) {
//                 return mapDocToPatient(docSnap);
//             }
//             return undefined;
//         } catch (error) {
//             console.error('Error fetching patient:', error);
//             throw error;
//         }
//     },

//     async createPatient(newPatient: Partial<Patient>): Promise<Patient> {
//         try {
//             const docRef = await addDoc(collection(db, 'patients'), {
//                 ...newPatient,
//                 dob: newPatient.dob ?? null,
//                 address: newPatient.address ?? null,
//                 description: newPatient.description ?? '',
//                 createdAt: serverTimestamp()
//             });

//             const created = await getDoc(docRef);
//             return mapDocToPatient(created);
//         } catch (error) {
//             console.error('Error creating patient:', error);
//             throw error;
//         }
//     },

//     async updatePatient(updatedPatient: Patient): Promise<void> {
//         if (!updatedPatient.id) throw new Error('Patient id is required');

//         try {
//             const docRef = doc(db, 'patients', updatedPatient.id);

//             const payload = {
//                 ...updatedPatient,
//                 dob: updatedPatient.dob ?? null,
//                 address: updatedPatient.address ?? null,
//                 description: updatedPatient.description ?? '',
//                 createdAt: updatedPatient.createdAt ?? serverTimestamp()
//             };

//             await updateDoc(docRef, payload);
//         } catch (error) {
//             console.error('Error updating patient:', error);
//             throw error;
//         }
//     },

//     async deletePatient(id: string): Promise<void> {
//         try {
//             await deleteDoc(doc(db, 'patients', id));
//         } catch (error) {
//             console.error('Error deleting patient:', error);
//             throw error;
//         }
//     },

//     async deletePatients(ids: string[]): Promise<void> {
//         try {
//             await Promise.all(ids.map((id) => deleteDoc(doc(db, 'patients', id))));
//         } catch (error) {
//             console.error('Error deleting multiple patients:', error);
//             throw error;
//         }
//     }
// };
