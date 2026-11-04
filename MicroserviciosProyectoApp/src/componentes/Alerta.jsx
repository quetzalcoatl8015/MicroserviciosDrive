import Alert from 'react-bootstrap/Alert';

/**
 * Bloque de mensajes reutilizable.
 *
 * Antes el proyecto solo escribía los errores en la consola del navegador, así que
 * para el usuario la pantalla parecía congelada. Este componente obliga a que todo
 * mensaje termine visible.
 *
 * @param {string}  mensaje   Texto a mostrar. Si está vacío, no se dibuja nada.
 * @param {string}  variante  Color de Bootstrap: "danger" para errores,
 *                            "success" para confirmaciones, "info" para avisos.
 * @param {boolean} cerrable  Si es true, muestra la cruz para descartar el mensaje.
 * @param {Function} onCerrar Se llama al descartar el mensaje.
 */
function Alerta({ mensaje, variante = 'danger', cerrable = false, onCerrar }) {
  // Sin mensaje no hay nada que mostrar. Devolver null evita huecos vacíos en la página.
  if (!mensaje) return null;

  return (
    <Alert variant={variante} dismissible={cerrable} onClose={onCerrar} className="py-2">
      {mensaje}
    </Alert>
  );
}

export default Alerta;
