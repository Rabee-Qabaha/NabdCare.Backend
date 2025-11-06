import { onBeforeUnmount, onMounted, ref } from 'vue';

/**
 * useInfiniteScrollObserver
 * -------------------------
 * Observes a DOM element and calls `onReachEnd` when it enters the viewport.
 */
export function useInfiniteScrollObserver(onReachEnd: () => void) {
  const targetRef = ref<HTMLElement | null>(null);
  const observer = ref<IntersectionObserver | null>(null);

  onMounted(() => {
    observer.value = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (entry.isIntersecting) onReachEnd();
      },
      {
        root: null,
        threshold: 0.8, // when 80% visible
      },
    );

    if (targetRef.value) observer.value.observe(targetRef.value);
  });

  onBeforeUnmount(() => {
    if (targetRef.value && observer.value) {
      observer.value.unobserve(targetRef.value);
    }
    observer.value?.disconnect();
  });

  return targetRef;
}
