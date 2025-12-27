import { type ClinicResponseDto, SubscriptionStatus } from '@/types/backend';
import { computed, ref, type Ref } from 'vue';

export function useClinicForm(clinic: Ref<Partial<ClinicResponseDto>>) {
  // --- General ---
  const name = ref('');
  const slug = ref('');
  const email = ref('');
  const phone = ref('');
  const address = ref('');

  // --- Branding ---
  const website = ref('');
  const logoUrl = ref('');
  const taxNumber = ref('');
  const registrationNumber = ref('');

  // --- Settings ---
  const timeZone = ref('UTC');
  const currency = ref('USD');
  const dateFormat = ref('dd/MM/yyyy');
  const locale = ref('en-US');
  const enablePatientPortal = ref(false);

  // --- Subscription Logic ---
  const status = ref<SubscriptionStatus>(SubscriptionStatus.Active);
  const subscriptionStartDate = ref<Date>(new Date());
  const selectedPlanId = ref('');
  const selectedPlan = ref<any>(null);
  const extraUsers = ref(0);
  const extraBranches = ref(0);
  const bonusUsers = ref(0);
  const bonusBranches = ref(0);

  const computedFee = computed(() => {
    if (!selectedPlan.value) return 0;
    const base = selectedPlan.value.baseFee || 0;
    const userCost = extraUsers.value * (selectedPlan.value.userPrice || 0);
    const branchCost = extraBranches.value * (selectedPlan.value.branchPrice || 0);
    return base + userCost + branchCost;
  });

  const computedBranchCount = computed(() => {
    const base = selectedPlan.value?.includedBranches || 1;
    return base + extraBranches.value + bonusBranches.value;
  });

  const computedEndDate = computed(() => {
    if (!subscriptionStartDate.value) return new Date();
    const d = new Date(subscriptionStartDate.value);
    const duration = selectedPlan.value?.durationDays || 30;
    d.setDate(d.getDate() + duration);
    return d;
  });

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
      enablePatientPortal.value = data.settings?.enablePatientPortal ?? false;
      status.value = data.status ?? SubscriptionStatus.Active;
      subscriptionStartDate.value = data.subscriptionStartDate
        ? new Date(data.subscriptionStartDate)
        : new Date();
    } else {
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
      status.value = SubscriptionStatus.Active;
      subscriptionStartDate.value = new Date();
      selectedPlanId.value = '';
      selectedPlan.value = null;
      extraUsers.value = 0;
      extraBranches.value = 0;
      bonusUsers.value = 0;
      bonusBranches.value = 0;
    }
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
      subscriptionType: selectedPlan.value?.type ?? 0,
      subscriptionFee: computedFee.value,
      branchCount: computedBranchCount.value,
      status: status.value,
      subscriptionStartDate: subscriptionStartDate.value,
      subscriptionEndDate: computedEndDate.value,
      planId: selectedPlanId.value,
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
    dateFormat,
    locale,
    enablePatientPortal,
    status,
    subscriptionStartDate,
    selectedPlanId,
    selectedPlan,
    extraUsers,
    extraBranches,
    bonusUsers,
    bonusBranches,
    computedFee,
    computedEndDate,
    computedBranchCount,
    initForm,
    getFormData,
  };
}
