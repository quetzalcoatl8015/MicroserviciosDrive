/**
 * Manejo centralizado de la sesión del usuario en el navegador.
 *
 * ¿Por qué existe este archivo?
 * El requerimiento técnico del PDF exige que los Refresh Tokens se guarden en el
 * localStorage y que el Access Token se anexe automáticamente en cada petición.
 * Si cada pantalla lee y escribe el localStorage por su cuenta, cualquier cambio en
 * los nombres de las claves obliga a corregir muchos archivos a la vez. Por eso todo
 * el acceso a la sesión pasa por las funciones de este módulo.
 *
 * Los nombres de las claves son los mismos que ya usaba el proyecto
 * ("access_token" y "refresh_token") para no romper nada de lo que hubiera antes.
 */

// Claves con las que los tokens quedan guardados en el localStorage.
const CLAVE_ACCESO = 'access_token';
const CLAVE_REFRESCO = 'refresh_token';

/**
 * Nombres exactos de los claims dentro del JWT.
 *
 * El backend está escrito en .NET y usa ClaimTypes, que son identificadores largos
 * en forma de dirección web en lugar de nombres cortos como "role" o "name".
 * Estos valores se copiaron de ServicioJwt.cs del backend, donde se crean los claims.
 */
const CLAIM_IDENTIFICADOR = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const CLAIM_NOMBRE = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const CLAIM_ROL = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/**
 * Guarda el par de tokens que devuelve el backend al iniciar sesión o al renovar.
 * @param {string} tokenAcceso Token de corta duración que viaja en cada petición.
 * @param {string} tokenRefresco Token de larga duración que sirve para pedir un par nuevo.
 */
export function guardarTokens(tokenAcceso, tokenRefresco) {
  if (tokenAcceso) localStorage.setItem(CLAVE_ACCESO, tokenAcceso);
  if (tokenRefresco) localStorage.setItem(CLAVE_REFRESCO, tokenRefresco);
}

/** Devuelve el Access Token guardado, o null si no hay sesión. */
export function obtenerTokenAcceso() {
  return localStorage.getItem(CLAVE_ACCESO);
}

/** Devuelve el Refresh Token guardado, o null si no hay sesión. */
export function obtenerTokenRefresco() {
  return localStorage.getItem(CLAVE_REFRESCO);
}

/** Indica si hay una sesión guardada en el navegador. */
export function haySesion() {
  return Boolean(obtenerTokenAcceso());
}

/**
 * Convierte una de las tres partes de un JWT en un objeto de JavaScript.
 *
 * Un JWT tiene la forma "cabecera.contenido.firma" y cada parte viaja en Base64
 * con variante "url-safe", que cambia "+" por "-" y "/" por "_". Antes de
 * decodificar hay que devolver esos caracteres y rellenar el texto con "=" hasta
 * un múltiplo de 4, porque atob es estricto con la longitud.
 *
 * El paso por decodeURIComponent es necesario para que los acentos y las eñes del
 * nombre del usuario se lean bien: atob entrega bytes y no texto.
 */
function decodificarParte(texto) {
  const base64 = texto.replace(/-/g, '+').replace(/_/g, '/');
  const relleno = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  const bytes = atob(relleno);

  const textoPlano = decodeURIComponent(
    Array.prototype.map
      .call(bytes, (caracter) => '%' + ('00' + caracter.charCodeAt(0).toString(16)).slice(-2))
      .join('')
  );

  return JSON.parse(textoPlano);
}

/**
 * Lee los datos del usuario directamente del Access Token.
 *
 * El backend no devuelve los datos del usuario al iniciar sesión: solo entrega los
 * dos tokens. Como el rol viaja dentro del token, se lee de ahí para poder mostrar
 * u ocultar el módulo de usuarios.
 *
 * ADVERTENCIA IMPORTANTE: el contenido de un JWT se puede decodificar y también
 * modificar en el navegador. Por eso esto sirve únicamente para decidir qué
 * mostrar en pantalla, NUNCA para decidir permisos. Los permisos reales los
 * valida siempre el backend.
 *
 * @returns {{id: number, nombre: string, esAdmin: boolean}|null}
 */
export function datosUsuario() {
  const token = obtenerTokenAcceso();
  if (!token) return null;

  try {
    const partes = token.split('.');
    if (partes.length !== 3) return null;

    const claims = decodificarParte(partes[1]);

    return {
      id: Number(claims[CLAIM_IDENTIFICADOR]),
      nombre: claims[CLAIM_NOMBRE] || '',
      // El backend guarda el rol como el texto "True" o "False", nunca como booleano.
      esAdmin: claims[CLAIM_ROL] === 'True',
    };
  } catch {
    // Si el token está corrupto se trata como si no hubiera sesión.
    return null;
  }
}

/**
 * Borra los tokens del navegador, sin redirigir.
 * Se usa cuando la pantalla se encarga de la navegación (por ejemplo, el botón
 * "Cerrar sesión" de la barra superior).
 */
export function limpiarSesion() {
  localStorage.removeItem(CLAVE_ACCESO);
  localStorage.removeItem(CLAVE_REFRESCO);
}

/**
 * Borra los tokens y manda al Login.
 * Se usa desde el interceptor de Axios, donde no hay acceso al enrutador de React.
 * El PDF exige este comportamiento cuando el Refresh Token también expiró.
 */
export function cerrarSesion() {
  limpiarSesion();

  // Solo se navega si no se está ya en el Login, para no recargar la pantalla sin motivo.
  if (window.location.pathname !== '/login') {
    window.location.assign('/login');
  }
}
