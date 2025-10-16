import { addDoc, collection, deleteDoc, doc, getDoc, getDocs, updateDoc, query, orderBy, where, Timestamp } from 'firebase/firestore';
import { db } from '@/firebase';
import type { XRay } from '@/../../shared/types';
import { getStorage, ref as storageRef, deleteObject } from 'firebase/storage';

// Utility: Firestore → XRay
function fromFirestore(docSnap: any): XRay {
    const data = docSnap.data();
    return {
        id: docSnap.id,
        patientId: data.patientId,
        imageUrl: data.imageUrl,
        description: data.description ?? '',
        uploadedAt: data.uploadedAt instanceof Timestamp ? data.uploadedAt.toDate() : data.uploadedAt,
        uploadedBy: data.uploadedBy
    };
}

// Utility: XRay → Firestore
function toFirestore(xray: XRay) {
    return {
        patientId: xray.patientId,
        imageUrl: xray.imageUrl,
        description: xray.description || '',
        uploadedAt: xray.uploadedAt ? Timestamp.fromDate(xray.uploadedAt) : Timestamp.now(),
        uploadedBy: xray.uploadedBy || 'unknown'
    };
}

export const XRayService = {
    async getXrays(patientId?: string): Promise<XRay[]> {
        try {
            const xraysRef = collection(db, 'xrays');
            const q = patientId ? query(xraysRef, where('patientId', '==', patientId), orderBy('uploadedAt', 'desc')) : query(xraysRef, orderBy('uploadedAt', 'desc'));
            const snapshot = await getDocs(q);
            return snapshot.docs.map(fromFirestore);
        } catch (error) {
            console.error('Error fetching xrays:', error);
            throw error;
        }
    },

    async getXrayById(id: string): Promise<XRay | null> {
        try {
            const docRef = doc(db, 'xrays', id);
            const docSnap = await getDoc(docRef);
            if (!docSnap.exists()) return null;
            return fromFirestore(docSnap);
        } catch (error) {
            console.error('Error fetching xray by id:', error);
            throw error;
        }
    },

    async createXray(xray: Omit<Partial<XRay>, 'id'>): Promise<XRay> {
        try {
            const docRef = await addDoc(collection(db, 'xrays'), toFirestore(xray as XRay));
            const docSnap = await getDoc(docRef);
            return fromFirestore(docSnap);
        } catch (error) {
            console.error('Error creating xray:', error);
            throw error;
        }
    },

    async updateXray(xray: XRay): Promise<void> {
        if (!xray.id) throw new Error('XRay ID is required for update');

        try {
            const docRef = doc(db, 'xrays', xray.id);
            const oldSnap = await getDoc(docRef);
            const oldData = oldSnap.exists() ? oldSnap.data() : null;
            const oldImageUrl = oldData?.imageUrl;

            // 1. Update Firestore record
            await updateDoc(docRef, {
                patientId: xray.patientId,
                imageUrl: xray.imageUrl,
                description: xray.description || '',
                uploadedAt: xray.uploadedAt,
                uploadedBy: xray.uploadedBy
            });

            // 2. If imageUrl changed → delete old image
            if (oldImageUrl && oldImageUrl !== xray.imageUrl) {
                try {
                    const storage = getStorage();

                    // extract correct path from downloadURL
                    const matches = oldImageUrl.match(/\/o\/([^?]+)/);
                    if (matches && matches[1]) {
                        const path = decodeURIComponent(matches[1]); // e.g. "xrays/123_file.png"
                        const fileRef = storageRef(storage, path);
                        await deleteObject(fileRef);
                        console.log(`Old image deleted: ${path}`);
                    }
                } catch (err) {
                    console.error('Failed to delete old image:', err);
                }
            }
        } catch (error) {
            console.error('Error updating xray:', error);
            throw error;
        }
    },

    async deleteXray(id: string): Promise<void> {
        try {
            // 1. جيب الدوكيومنت
            const docRef = doc(db, 'xrays', id);
            const docSnap = await getDoc(docRef);

            if (docSnap.exists()) {
                const data = docSnap.data();
                const imageUrl = data.imageUrl;

                // 2. امسح الدوكيومنت من Firestore
                await deleteDoc(docRef);

                // 3. امسح الصورة من Storage إذا موجودة
                if (imageUrl) {
                    try {
                        const storage = getStorage();
                        const path = decodeURIComponent(imageUrl.split('/o/')[1].split('?')[0]);
                        const fileRef = storageRef(storage, path);
                        await deleteObject(fileRef);
                    } catch (err) {
                        console.error('Failed to delete image from storage:', err);
                    }
                }
            }
        } catch (error) {
            console.error('Error deleting xray:', error);
            throw error;
        }
    },

    async deleteXrays(ids: string[]): Promise<void> {
        try {
            await Promise.all(ids.map((id) => deleteDoc(doc(db, 'xrays', id))));
        } catch (error) {
            console.error('Error deleting multiple xrays:', error);
            throw error;
        }
    }
};
