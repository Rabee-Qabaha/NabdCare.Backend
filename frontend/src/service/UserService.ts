// import { app, db } from '@/firebase';
// import { collection, doc, getDoc, getDocs, Timestamp } from 'firebase/firestore';
// import type { User } from '../../../shared/types';
// import { getFunctions, httpsCallable } from 'firebase/functions';
//
// const usersCollection = collection(db, 'users');
// const functions = getFunctions(app);
//
// interface CreateUserInput {
//     email: string;
//     password: string;
//     role: 'admin' | 'user';
//     isActive: boolean;
//     displayName?: string;
// }
//
// interface UpdateUserInput {
//     uid: string;
//     email?: string;
//     displayName?: string;
//     role?: 'admin' | 'user';
//     isActive?: boolean;
// }
//
// const userCache: Record<string, User> = {};
//
// export const UserService = {
//     // 🔹 Get all users
//     async getUsers(): Promise<User[]> {
//         const snapshot = await getDocs(usersCollection);
//         return snapshot.docs.map(
//             (doc) =>
//                 ({
//                     uid: doc.id,
//                     ...doc.data(),
//                     createdAt: (doc.data().createdAt as Timestamp).toDate()
//                 }) as User
//         );
//     },
//
//     // 🔹 Get a single user
//     async getUser(uid: string): Promise<User | null> {
//         // Return from cache if exists
//         if (userCache[uid]) {
//             console.log('Returning cached user for UID:', uid);
//             return userCache[uid];
//         }
//
//         // Fetch from Firestore
//         const docRef = doc(db, 'users', uid);
//         const docSnap = await getDoc(docRef);
//         if (!docSnap.exists()) return null;
//
//         const docData = docSnap.data();
//
//         // Construct User object with all required fields
//         const userData: User = {
//             uid: docSnap.id,
//             email: docData.email || '', // required
//             role: docData.role || 'user', // required
//             isActive: docData.isActive ?? true, // required
//             displayName: docData.displayName || undefined, // optional
//             createdAt: docData.createdAt?.toDate ? (docData.createdAt as any).toDate() : new Date() // fallback
//         };
//
//         // Store in cache
//         userCache[uid] = userData;
//         return userData;
//     },
//
//     // 🔹 Create user (admin only)
//     async createUserWithAdmin(data: CreateUserInput): Promise<User> {
//         const createUserFn = httpsCallable<CreateUserInput, User>(functions, 'createUser');
//         const result = await createUserFn(data);
//         return result.data;
//     },
//
//     // 🔹 Update user (admin can update any, user can only update self)
//     async updateUser(data: UpdateUserInput): Promise<void> {
//         const updateUserFn = httpsCallable<UpdateUserInput, { success: boolean }>(functions, 'updateUser');
//         const result = await updateUserFn(data);
//         if (!result.data.success) {
//             throw new Error('Failed to update user');
//         }
//     },
//
//     // 🔹 Delete user (admin only)
//     async deleteUser(uid: string): Promise<void> {
//         const deleteUserFn = httpsCallable<{ uid: string }, { success: boolean }>(functions, 'deleteUser');
//         const result = await deleteUserFn({ uid });
//         if (!result.data.success) {
//             throw new Error('Failed to delete user');
//         }
//     },
//
//     // 🔹 Change password (user can only change own, admin can change anyone’s)
//     async changePassword(uid: string, newPassword: string): Promise<void> {
//         const changePasswordFn = httpsCallable<{ uid: string; newPassword: string }, { success: boolean }>(functions, 'changePassword');
//         const result = await changePasswordFn({ uid, newPassword });
//
//         if (!result.data.success) {
//             throw new Error('Failed to change password');
//         }
//     }
// };
