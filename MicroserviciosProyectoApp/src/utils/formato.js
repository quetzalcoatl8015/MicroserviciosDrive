/**
 * Funciones de formato compartidas por las pantallas.
 *
 * Se separan en su propio archivo porque las usan tanto la tabla de archivos como
 * los modales de subida, y no conviene repetir la misma cuenta en varios lugares.
 */

/**
 * Convierte un tamaño en bytes a un texto legible.
 *
 * El backend siempre entrega bytes (campo "tamanoBytes"), que para un archivo de
 * 1,5 MB serían 1572864 y no dicen nada al usuario.
 *
 * @param {number} bytes Tamaño en bytes.
 * @returns {string} Por ejemplo "1,50 MB" o "320 KB".
 */
export function formatearTamano(bytes) {
  if (bytes === null || bytes === undefined) return '-';

  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;

  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

/**
 * Convierte una fecha ISO del backend a texto local.
 *
 * Las fechas viajan en UTC (el backend guarda con DateTime.UtcNow), así que se
 * muestra en la zona horaria del navegador, que es la del usuario.
 *
 * @param {string} valor Fecha en formato ISO.
 * @returns {string} Fecha y hora legibles, o "-" si no hay valor.
 */
export function formatearFecha(valor) {
  if (!valor) return '-';

  const fecha = new Date(valor);
  if (Number.isNaN(fecha.getTime())) return '-';

  return fecha.toLocaleString('es-MX', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Convierte una fecha ISO a solo la fecha, sin hora.
 * Se usa para la fecha de nacimiento, que es un dato de tipo fecha sin hora.
 */
export function formatearFechaCorta(valor) {
  if (!valor) return '-';

  const fecha = new Date(valor);
  if (Number.isNaN(fecha.getTime())) return '-';

  return fecha.toLocaleDateString('es-MX', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  });
}

/**
 * Toma el mensaje de error del backend y lo convierte en una línea legible.
 * Los errores de validación llegan como un objeto con la lista de mensajes.
 */
export function textoDeValidacion(datos) {
  if (!datos) return '';

  if (typeof datos === 'string') return datos;

  if (datos.errors) {
    return Object.values(datos.errors).flat().join(' ');
  }

  return datos.title || '';
}
