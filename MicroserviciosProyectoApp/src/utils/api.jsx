import axios from "axios";
import { cerrarSesion, guardarTokens, obtenerTokenAcceso, obtenerTokenRefresco } from "./sesion";

/**
 * Cliente HTTP único de la aplicación.
 *
 * Este archivo cubre tres de los cuatro requerimientos técnicos del frontend que
 * pide el PDF:
 *   1. Los Refresh Tokens viven en el localStorage (se hace desde utils/sesion.js).
 *   2. Los interceptors de Axios anexan el Access Token en cada petición.
 *   3. Si el backend responde 401 porque el Access Token expiró, se usa el Refresh
 *      Token para pedir un par nuevo y se reintenta la petición una sola vez.
 *   4. Si el Refresh Token también expiró, se cierra la sesión y se manda al Login.
 */

/**
 * Dirección base de la API. Todos los controladores del backend cuelgan de aquí.
 *
 * El valor se toma de la variable de entorno VITE_API_URL, que es la que usa el
 * despliegue: al compilar con Docker, el docker-compose la pasa como argumento de
 * construcción. Si la variable no está definida (por ejemplo al trabajar en local
 * con "npm run dev"), se usa la dirección del servidor de desarrollo.
 *
 * Nota importante: Vite sustituye esta variable al COMPILAR, no al ejecutar. Una
 * vez hecho el build, el valor queda escrito dentro del JavaScript y cambiarlo
 * exige volver a compilar. Por eso mismo la dirección debe ser alcanzable desde el
 * NAVEGADOR del usuario y no desde dentro de la red de Docker.
 */
const DIRECCION_BASE = import.meta.env.VITE_API_URL || "http://localhost:5239/api";

/** Cliente principal: es el que usan todas las pantallas. */
const api = axios.create({ baseURL: DIRECCION_BASE });

/**
 * Cliente aparte, exclusivo para renovar los tokens.
 *
 * ¿Por qué no usar el cliente principal? Porque la renovación se dispara desde el
 * interceptor de respuesta. Si se usara el mismo cliente y la renovación también
 * devolviera 401, el interceptor se llamaría a sí mismo sin fin y la aplicación se
 * quedaría en un bucle de peticiones. Este cliente no tiene interceptores, así que
 * una renovación fallida simplemente falla.
 */
const apiRenovacion = axios.create({ baseURL: DIRECCION_BASE });

// ---------------------------------------------------------------------------
// Interceptor de PETICIÓN
// ---------------------------------------------------------------------------
api.interceptors.request.use((configuracion) => {
  const token = obtenerTokenAcceso();

  // Solo se anexa la cabecera si hay token. Antes el proyecto enviaba siempre
  // "Bearer null" cuando no había sesión, lo cual confundía al backend.
  if (token) {
    configuracion.headers.Authorization = `Bearer ${token}`;
  }

  return configuracion;
});

// ---------------------------------------------------------------------------
// Interceptor de RESPUESTA
// ---------------------------------------------------------------------------
api.interceptors.response.use(
  // Una respuesta exitosa (código 2xx) pasa tal cual hacia la pantalla.
  (respuesta) => respuesta,

  async (error) => {
    const peticion = error.config;
    const estado = error.response ? error.response.status : null;

    /**
     * Las rutas de autenticación quedan fuera de la renovación a propósito.
     * Si el Login devuelve 401 porque la contraseña está mal, no tiene sentido
     * intentar renovar con un token viejo que quizá ya no exista.
     */
    const esRutaDeAutenticacion = peticion?.url?.includes("/autenticacion/");

    // La marca "_reintentado" se pone en la propia petición para garantizar que
    // se reintenta una sola vez, como pide el PDF, y no en cadena.
    if (estado !== 401 || !peticion || peticion._reintentado || esRutaDeAutenticacion) {
      return Promise.reject(error);
    }

    peticion._reintentado = true;

    const tokenRefresco = obtenerTokenRefresco();

    // Sin Refresh Token no hay nada que renovar: la sesión está perdida.
    if (!tokenRefresco) {
      cerrarSesion();
      return Promise.reject(error);
    }

    try {
      const { data } = await apiRenovacion.post("/autenticacion/renovar", {
        tokenRefresco,
      });

      // El backend rota los tokens: el par anterior queda invalidado y hay que
      // guardar el nuevo, tanto el de acceso como el de refresco.
      guardarTokens(data.tokenAcceso, data.tokenRefresco);

      // Se repite la petición original con el token nuevo.
      peticion.headers.Authorization = `Bearer ${data.tokenAcceso}`;
      return api(peticion);
    } catch {
      // El Refresh Token también expiró o fue revocado. El PDF pide cerrar la
      // sesión y regresar al Login para que el usuario vuelva a autenticarse.
      cerrarSesion();
      return Promise.reject(error);
    }
  }
);

/**
 * Extrae el mensaje de error que devolvió el backend.
 *
 * El backend responde de dos formas distintas y conviene cubrir ambas:
 *   - Texto plano con el mensaje, por ejemplo "Ya existe un directorio con ese nombre en el mismo nivel."
 *   - Errores de validación, que llegan como { errors: { Campo: ["mensaje", ...] } }
 *
 * Así las pantallas pueden mostrar el mensaje real del servidor, que ya viene en
 * español y explica mucho mejor qué pasó que un texto genérico escrito a mano.
 */
export function mensajeDeError(error) {
  const datos = error?.response?.data;

  if (typeof datos === "string" && datos.trim()) return datos;

  if (datos?.errors) {
    const mensajes = Object.values(datos.errors).flat();
    if (mensajes.length) return mensajes.join(" ");
  }

  if (datos?.title) return datos.title;

  if (error?.response?.status === 403) {
    return "No tienes permisos para realizar esta acción.";
  }

  if (error?.response?.status === 404) {
    return "El recurso solicitado no existe.";
  }

  return "No se pudo completar la operación. Verifica que el servidor esté disponible.";
}

export default api;
