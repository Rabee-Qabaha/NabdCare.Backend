// import { addDoc, collection, deleteDoc, doc, getDoc, getDocs, updateDoc, query, where, Timestamp, orderBy } from 'firebase/firestore';
// import { db } from '@/firebase';
// import type { Payment, PaymentWithPatient, Patient } from '@/../../shared/types';

// // Utility: Convert Firestore doc → Payment
// function fromFirestore(docSnap: any): Payment {
//     const data = docSnap.data();
//     return {
//         id: docSnap.id,
//         patientId: data.patientId,
//         amount: data.amount,
//         method: data.method,
//         paidAt: data.paidAt instanceof Timestamp ? data.paidAt.toDate() : data.paidAt,
//         createdAt: data.createdAt instanceof Timestamp ? data.createdAt.toDate() : data.createdAt,
//         notes: data.notes
//     };
// }

// // Utility: Convert Payment → Firestore object
// function toFirestore(payment: Payment) {
//     return {
//         patientId: payment.patientId,
//         amount: payment.amount,
//         method: payment.method,
//         paidAt: Timestamp.fromDate(payment.paidAt),
//         createdAt: Timestamp.fromDate(payment.createdAt),
//         notes: payment.notes || ''
//     };
// }

// export const PaymentService = {
//     async getPayments(): Promise<PaymentWithPatient[]> {
//         try {
//             const q = query(collection(db, 'payments'), orderBy('paidAt', 'desc'));
//             const snapshot = await getDocs(q);
//             const payments: Payment[] = snapshot.docs.map(fromFirestore);

//             const patientSnapshot = await getDocs(collection(db, 'patients'));
//             const patients: Patient[] = patientSnapshot.docs.map((d) => ({
//                 ...(d.data() as Omit<Patient, 'id'>),
//                 id: d.id,
//                 dob: d.data().dob instanceof Timestamp ? d.data().dob.toDate() : d.data().dob,
//                 createdAt: d.data().createdAt instanceof Timestamp ? d.data().createdAt.toDate() : d.data().createdAt
//             }));

//             return payments.map((payment) => {
//                 const patient = patients.find((p) => p.id === payment.patientId);
//                 return {
//                     ...payment,
//                     patient: patient ? { name: patient.name, phone: patient.phone, gender: patient.gender } : { name: 'Unknown', phone: 'N/A', gender: 'Male' }
//                 };
//             });
//         } catch (error) {
//             console.error('Error fetching payments:', error);
//             throw error;
//         }
//     },

//     // Get payments by patientId
//     async getByPatientId(patientId: string): Promise<PaymentWithPatient[]> {
//         try {
//             const q = query(collection(db, 'payments'), where('patientId', '==', patientId), orderBy('paidAt', 'desc'));
//             const snapshot = await getDocs(q);
//             const payments: Payment[] = snapshot.docs.map(fromFirestore);

//             const patientDoc = await getDoc(doc(db, 'patients', patientId));
//             const patientData = patientDoc.exists()
//                 ? ({
//                       ...(patientDoc.data() as Omit<Patient, 'id'>),
//                       id: patientDoc.id
//                   } as Patient)
//                 : null;

//             return payments.map((payment) => ({
//                 ...payment,
//                 patient: patientData ? { name: patientData.name, phone: patientData.phone, gender: patientData.gender } : { name: 'Unknown', phone: 'N/A', gender: 'Male' }
//             }));
//         } catch (error) {
//             console.error('Error fetching payments by patient:', error);
//             throw error;
//         }
//     },

//     async getPaymentById(id: string): Promise<Payment | null> {
//         try {
//             const docRef = doc(db, 'payments', id);
//             const docSnap = await getDoc(docRef);
//             if (!docSnap.exists()) return null;

//             return fromFirestore(docSnap);
//         } catch (error) {
//             console.error('Error fetching payment by id:', error);
//             throw error;
//         }
//     },

//     async createPayment(payment: Payment): Promise<string> {
//         try {
//             const docRef = await addDoc(collection(db, 'payments'), toFirestore(payment));
//             return docRef.id;
//         } catch (error) {
//             console.error('Error creating payment:', error);
//             throw error;
//         }
//     },

//     async updatePayment(payment: Payment): Promise<void> {
//         if (!payment.id) throw new Error('Payment ID is required for update');
//         try {
//             await updateDoc(doc(db, 'payments', payment.id), toFirestore(payment));
//         } catch (error) {
//             console.error('Error updating payment:', error);
//             throw error;
//         }
//     },

//     async deletePayment(id: string): Promise<void> {
//         try {
//             await deleteDoc(doc(db, 'payments', id));
//         } catch (error) {
//             console.error('Error deleting payment:', error);
//             throw error;
//         }
//     },

//     async deletePayments(ids: string[]): Promise<void> {
//         try {
//             await Promise.all(ids.map((id) => deleteDoc(doc(db, 'payments', id))));
//         } catch (error) {
//             console.error('Error deleting multiple payments:', error);
//             throw error;
//         }
//     }
// };
