import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5239/api",
});

api.interceptors.request.use(
  function (config) {
    // Hacer algo antes de enviar el request
    config.headers.Authorization = 'Bearer ' + localStorage.getItem('access_token');

    return config;
  },
  function (error) {
    // Hacer algo con el error del request

    return Promise.reject(error);
  }
);

api.interceptors.response.use(
  function (response) {
    // Cualquier respuesta con estatus dentro del rango 2xx causa que se dispare esta función
    // Hacer algo con los datos de la respuesta

    return response;
  },
  function (error) {
    // Cualquier respuesta con estatus fuera del rango 2xx causa que se dispare esta función
    // Hacer algo con el error de la respuesta
    const status = error.response ? error.response.status : null

    if (status === 401) {
      
    }

    return Promise.reject(error);
  }
);

export default api;