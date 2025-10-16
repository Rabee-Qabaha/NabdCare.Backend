const isDev = import.meta.env.DEV;

export const API_BASE_URL = isDev
  ? "http://localhost:5175/api"
  : "https://api.nabdcare.com/api";
