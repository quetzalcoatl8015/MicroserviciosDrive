import { useState } from 'react';
import Modal from 'react-bootstrap/Modal';
import Button from 'react-bootstrap/Button';
import Spinner from 'react-bootstrap/Spinner';
import { mensajeDeError } from '../utils/api';
import Alerta from '../componentes/Alerta';

/**
 * V8 - Ventana de confirmación para operaciones destructivas.
 *
 * Se usa antes de eliminar carpetas, archivos o usuarios porque todas esas acciones
 * son irreversibles: en el caso de una carpeta con contenido, el backend borra
 * también todo el subárbol y los archivos físicos del disco.
 *
 * Este componente NO sabe qué se está borrando. Recibe una función "onConfirmar" que
 * hace la petición y se limita a esperarla: así la misma ventana sirve para las tres
 * pantallas sin duplicar código ni rutas.
 *
 * @param {boolean}  mostrar      Controla si la ventana está abierta.
 * @param {string}   titulo       Título de la ventana, por ejemplo "Eliminar carpeta".
 * @param {string}   mensaje      Texto principal con el nombre del elemento afectado.
 * @param {string}   aviso        Advertencia adicional (por ejemplo, borrado en cascada).
 * @param {string}   textoBoton   Texto del botón que confirma. "Eliminar" por omisión.
 * @param {Function} onConfirmar  Función asíncrona que ejecuta la operación real.
 * @param {Function} onCerrar     Se llama al cerrar la ventana.
 */
function ModalConfirmar({
  mostrar,
  titulo = 'Confirmar operación',
  mensaje,
  aviso,
  textoBoton = 'Eliminar',
  onConfirmar,
  onCerrar,
}) {
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState('');

  /** Cierra la ventana. No se permite cerrar mientras la petición está en curso. */
  function cerrar() {
    if (cargando) return;

    setError('');
    onCerrar();
  }

  /**
   * Ejecuta la operación confirmada.
   *
   * "onConfirmar" la escribe la pantalla dueña y puede fallar (403 si el usuario no
   * tiene permiso, 400 si un usuario común intenta borrar una carpeta con contenido,
   * 404 si el elemento ya no existe). Si falla, se muestra el mensaje del backend y
   * la ventana permanece abierta para que el usuario pueda leerlo.
   */
  async function confirmar() {
    setError('');
    setCargando(true);

    try {
      await onConfirmar();
      setCargando(false);
      onCerrar();
    } catch (fallo) {
      setError(mensajeDeError(fallo));
      setCargando(false);
    }
  }

  return (
    <Modal show={mostrar} onHide={cerrar} centered>
      <Modal.Header closeButton>
        <Modal.Title>{titulo}</Modal.Title>
      </Modal.Header>

      <Modal.Body>
        <Alerta mensaje={error} />

        <p className="mb-2">{mensaje}</p>

        {/* El aviso solo aparece cuando la pantalla lo manda. En una carpeta con
            contenido, por ejemplo, advierte que se borrará todo lo que hay dentro. */}
        {aviso && <p className="text-danger small mb-0">{aviso}</p>}
      </Modal.Body>

      <Modal.Footer>
        <Button variant="secondary" onClick={cerrar} disabled={cargando}>
          Cancelar
        </Button>

        <Button variant="danger" onClick={confirmar} disabled={cargando}>
          {cargando ? <Spinner animation="border" size="sm" /> : textoBoton}
        </Button>
      </Modal.Footer>
    </Modal>
  );
}

export default ModalConfirmar;
