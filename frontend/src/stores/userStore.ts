// // src/stores/userStore.ts
// import { ref } from 'vue';
// import { defineStore } from 'pinia';
// import { UserService } from '@/service/UserService';
// import type { User } from '../../../shared/types';
//
// export const useUserStore = defineStore('user', () => {
//     // ----------------------------
//     // State
//     // ----------------------------
//     const users = ref<User[]>([]);
//     const selectedUser = ref<User | null>(null);
//     const loading = ref(false);
//     const isProcessing = ref(false);
//     const error = ref<string | null>(null);
//
//     // ----------------------------
//     // Actions
//     // ----------------------------
//
//     /** Fetch all users */
//     const fetchUsers = async () => {
//         loading.value = true;
//         error.value = null;
//         try {
//             users.value = await UserService.getUsers();
//         } catch (err: any) {
//             error.value = err.message;
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     };
//
//     /** Fetch single user by UID */
//     const userCache: Record<string, User> = {};
//
//     const fetchUser = async (uid: string) => {
//         loading.value = true;
//         error.value = null;
//         try {
//             if (userCache[uid]) return userCache[uid]; // return cached
//
//             const user = await UserService.getUser(uid);
//             if (!user) throw new Error('User not found');
//
//             selectedUser.value = user;
//             users.value.push(user);
//             userCache[uid] = user; // store in cache
//             return user;
//         } catch (err: any) {
//             error.value = err.message;
//             throw err;
//         } finally {
//             loading.value = false;
//         }
//     };
//
//     /** Create a new user (via Cloud Function) */
//     const createUser = async (newUser: Omit<User, 'uid' | 'createdAt'> & { password: string }) => {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             const createdUser = await UserService.createUserWithAdmin(newUser);
//             users.value.push(createdUser);
//             return createdUser;
//         } catch (err: any) {
//             error.value = err.message;
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     };
//
//     /** Update existing user */
//     const updateUser = async (user: User) => {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await UserService.updateUser(user);
//             // Update local store
//             const index = users.value.findIndex((u) => u.uid === user.uid);
//             if (index !== -1) users.value[index] = { ...user };
//             if (selectedUser.value?.uid === user.uid) selectedUser.value = { ...user };
//         } catch (err: any) {
//             error.value = err.message;
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     };
//
//     /** Delete user (admin only) */
//     const deleteUser = async (uid: string) => {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await UserService.deleteUser(uid);
//             users.value = users.value.filter((u) => u.uid !== uid);
//             if (selectedUser.value?.uid === uid) selectedUser.value = null;
//         } catch (err: any) {
//             error.value = err.message;
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     };
//
//     /** Change password (user can only change self, admin can change any) */
//     const changePassword = async (uid: string, newPassword: string) => {
//         isProcessing.value = true;
//         error.value = null;
//         try {
//             await UserService.changePassword(uid, newPassword);
//         } catch (err: any) {
//             error.value = err.message;
//             throw err;
//         } finally {
//             isProcessing.value = false;
//         }
//     };
//
//     // ----------------------------
//     // Return state & actions
//     // ----------------------------
//     return {
//         users,
//         selectedUser,
//         loading,
//         isProcessing,
//         error,
//         fetchUsers,
//         fetchUser,
//         createUser,
//         updateUser,
//         deleteUser,
//         changePassword
//     };
// });
