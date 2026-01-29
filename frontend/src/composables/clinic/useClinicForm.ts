// src/composables/clinic/useClinicForm.ts
import { clinicSchema } from '@/composables/validation/clinicSchema';
import { type ClinicResponseDto } from '@/types/backend';
import { ref, type Ref } from 'vue';

export function useClinicForm(clinic: Ref<Partial<ClinicResponseDto>>) {
  const name = ref('');
  const slug = ref('');
  const email = ref('');
  const phone = ref('');
  const address = ref('');
  const website = ref('');
  const logoUrl = ref('');
  const taxNumber = ref('');
  const registrationNumber = ref('');
  const timeZone = ref('UTC');
  const currency = ref('USD');
  const dateFormat = ref('dd/MM/yyyy');
  const locale = ref('en-US');
  const enablePatientPortal = ref(false);

  const validate = () => {
    const result = clinicSchema.safeParse(getFormData());

    if (!result.success) {
      const fieldErrors: Record<string, string> = {};
      result.error.issues.forEach((issue) => {
        const key = issue.path[0] as string;
        if (key && !fieldErrors[key]) {
          fieldErrors[key] = issue.message;
        }
      });
      return { isValid: false, errors: fieldErrors };
    }
    return { isValid: true, errors: {} };
  };

  function initForm(data?: Partial<ClinicResponseDto> | null) {
    if (data) {
      name.value = data.name || '';
      slug.value = data.slug || '';
      email.value = data.email || '';
      phone.value = data.phone || '';
      address.value = data.address || '';
      website.value = data.website || '';
      logoUrl.value = data.logoUrl || '';
      taxNumber.value = data.taxNumber || '';
      registrationNumber.value = data.registrationNumber || '';
      timeZone.value = data.settings?.timeZone || 'UTC';
      currency.value = data.settings?.currency || 'USD';
      dateFormat.value = data.settings?.dateFormat || 'dd/MM/yyyy';
      locale.value = data.settings?.locale || 'en-US';
      enablePatientPortal.value = data.settings?.enablePatientPortal || false;
    } else {
      resetToDefaults();
    }
  }

  function resetToDefaults() {
    name.value = '';
    slug.value = '';
    email.value = '';
    phone.value = '';
    address.value = '';
    website.value = '';
    logoUrl.value = '';
    taxNumber.value = '';
    registrationNumber.value = '';
    timeZone.value = 'UTC';
    currency.value = 'USD';
    dateFormat.value = 'dd/MM/yyyy';
    locale.value = 'en-US';
    enablePatientPortal.value = false;
  }

  function getFormData() {
    return {
      name: name.value,
      slug: slug.value,
      email: email.value,
      phone: phone.value,
      address: address.value,
      website: website.value,
      logoUrl: logoUrl.value,
      taxNumber: taxNumber.value,
      registrationNumber: registrationNumber.value,
      settings: {
        timeZone: timeZone.value,
        currency: currency.value,
        dateFormat: dateFormat.value,
        locale: locale.value,
        enablePatientPortal: enablePatientPortal.value,
      },
    };
  }

  return {
    name,
    slug,
    email,
    phone,
    address,
    website,
    logoUrl,
    taxNumber,
    registrationNumber,
    timeZone,
    currency,
    initForm,
    getFormData,
    validate,
    dateFormat,
    locale,
    enablePatientPortal,
  };
}
