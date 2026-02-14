import { api } from '@/api/apiClient';

export interface SystemConfiguration {
  functionalCurrency: string;
}

export const configurationApi = {
  getConfiguration: async (): Promise<SystemConfiguration> => {
    const { data } = await api.get<SystemConfiguration>('/configuration');
    return data;
  },
};
