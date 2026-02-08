// Path: src/composables/query/invoices/useInvoiceActions.ts
import { invoicesApi } from '@/api/modules/invoices';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { useToastService } from '@/composables/useToastService';
import { useMutation } from '@tanstack/vue-query';

export function useInvoiceActions() {
  const toast = useToastService();
  const { handleErrorAndNotify } = useErrorHandler();

  const downloadPdfMutation = useMutation({
    mutationFn: async ({ id, number }: { id: string; number: string }) => {
      const response = await invoicesApi.downloadPdf(id);

      // Create download link
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `${number}.pdf`);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    },
    onSuccess: () => toast.success('Invoice downloaded successfully'),
    onError: (err) => handleErrorAndNotify(err),
  });

  const writeOffInvoiceMutation = useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      await invoicesApi.writeOff(id, { reason });
    },
    onSuccess: () => toast.success('Invoice written off successfully'),
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    downloadPdfMutation,
    downloadPdf: (id: string, number: string) => downloadPdfMutation.mutate({ id, number }),
    writeOffInvoiceMutation,
    writeOffInvoice: (data: { id: string; reason: string }, options?: any) =>
      writeOffInvoiceMutation.mutate(data, options),
    isWritingOff: writeOffInvoiceMutation.isPending,
  };
}
