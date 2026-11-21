import axios from 'axios';

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL || '/api' });
api.interceptors.request.use(config => {
  const token = localStorage.getItem('access_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
let renovacion;
api.interceptors.response.use(response => response, async error => {
  const config = error.config;
  if (error.response?.status === 401 && !config.reintento && !['/autenticacion/renovar', '/autenticacion/iniciar-sesion', '/autenticacion/registro'].includes(config.url)) {
    config.reintento = true;
    try {
      renovacion ??= api.post('/autenticacion/renovar', {
        tokenRefresco: localStorage.getItem('refresh_token'),
      }).then(({ data }) => {
        localStorage.setItem('access_token', data.tokenAcceso);
        localStorage.setItem('refresh_token', data.tokenRefresco);
      }).finally(() => { renovacion = null; });
      await renovacion;
      return await api(config);
    } catch {
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
      window.dispatchEvent(new Event('sesion-cerrada'));
    }
  }
  return Promise.reject(error);
});
export default api;
