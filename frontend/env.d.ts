/// <reference types="vite/client" />

declare module '*.vue' {
    import type { DefineComponent } from 'vue';
    const component: DefineComponent<{}, {}, any>;
    export default component;
}

interface ImportMetaEnv {
    readonly VITE_API_BASE_URL: string;
    readonly VITE_APP_TITLE: string;
    readonly VUE_APP_FIREBASE_API_KEY: string;
    readonly VUE_APP_FIREBASE_AUTH_DOMAIN: string;
    readonly VUE_APP_FIREBASE_PROJECT_ID: string;
    readonly VUE_APP_FIREBASE_STORAGE_BUCKET: string;
    readonly VUE_APP_FIREBASE_MESSAGING_SENDER_ID: string;
    readonly VUE_APP_FIREBASE_APP_ID: string;
    readonly VUE_APP_FIREBASE_MEASUREMENT_ID?: string;
}

interface ImportMeta {
    readonly env: ImportMetaEnv;
}
