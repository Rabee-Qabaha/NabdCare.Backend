/// <reference lib="dom" />
import type { ComputedRef } from 'vue';
import { computed, onMounted, reactive } from 'vue';

interface LayoutConfig {
    preset: string;
    primary: string;
    surface: string | null;
    darkTheme: boolean;
    menuMode: 'static' | 'overlay';
}

interface LayoutState {
    staticMenuDesktopInactive: boolean;
    overlayMenuActive: boolean;
    profileSidebarVisible: boolean;
    configSidebarVisible: boolean;
    staticMenuMobileActive: boolean;
    menuHoverActive: boolean;
    activeMenuItem: unknown;
}

// Load saved theme settings from localStorage
const loadSavedTheme = (): Partial<LayoutConfig> => {
    const savedTheme = localStorage.getItem('layoutConfig');
    return savedTheme ? JSON.parse(savedTheme) : {};
};

const savedTheme = loadSavedTheme();

const layoutConfig = reactive<LayoutConfig>({
    preset: savedTheme.preset || 'Aura',
    primary: savedTheme.primary || 'emerald',
    surface: savedTheme.surface || null,
    darkTheme: savedTheme.darkTheme || false,
    menuMode: savedTheme.menuMode || 'static'
});

const layoutState = reactive<LayoutState>({
    staticMenuDesktopInactive: false,
    overlayMenuActive: false,
    profileSidebarVisible: false,
    configSidebarVisible: false,
    staticMenuMobileActive: false,
    menuHoverActive: false,
    activeMenuItem: null
});

interface UseLayout {
    layoutConfig: LayoutConfig;
    layoutState: LayoutState;
    toggleMenu: () => void;
    isSidebarActive: ComputedRef<boolean>;
    isDarkTheme: ComputedRef<boolean>;
    getPrimary: ComputedRef<string>;
    getSurface: ComputedRef<string | null>;
    setActiveMenuItem: (item: unknown) => void;
    toggleDarkMode: () => void;
}

export function useLayout(): UseLayout {
    // Save theme settings to localStorage whenever they change
    const saveThemeSettings = () => {
        localStorage.setItem('layoutConfig', JSON.stringify(layoutConfig));
    };

    const setActiveMenuItem = (item: unknown) => {
        layoutState.activeMenuItem = (item as { value: unknown })?.value || item;
    };

    const toggleDarkMode = () => {
        if (!document.startViewTransition) {
            executeDarkModeToggle();
            return;
        }

        document.startViewTransition(() => executeDarkModeToggle());
    };

    const executeDarkModeToggle = () => {
        layoutConfig.darkTheme = !layoutConfig.darkTheme;
        document.documentElement.classList.toggle('app-dark');
        saveThemeSettings();
    };

    const toggleMenu = () => {
        if (layoutConfig.menuMode === 'overlay') {
            layoutState.overlayMenuActive = !layoutState.overlayMenuActive;
        }

        if (window.innerWidth > 991) {
            layoutState.staticMenuDesktopInactive = !layoutState.staticMenuDesktopInactive;
        } else {
            layoutState.staticMenuMobileActive = !layoutState.staticMenuMobileActive;
        }
    };

    // Apply saved dark theme on mount
    onMounted(() => {
        if (layoutConfig.darkTheme) {
            document.documentElement.classList.add('app-dark');
        }
    });

    const isSidebarActive = computed(() => layoutState.overlayMenuActive || layoutState.staticMenuMobileActive);

    const isDarkTheme = computed(() => layoutConfig.darkTheme);

    const getPrimary = computed(() => layoutConfig.primary);

    const getSurface = computed(() => layoutConfig.surface);

    return {
        layoutConfig,
        layoutState,
        toggleMenu,
        isSidebarActive,
        isDarkTheme,
        getPrimary,
        getSurface,
        setActiveMenuItem,
        toggleDarkMode
    };
}
