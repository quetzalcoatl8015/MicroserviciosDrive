import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import ButtonGroup from 'react-bootstrap/ButtonGroup';
import Spinner from 'react-bootstrap/Spinner';
import api, { mensajeDeError } from '../utils/api';

/**
 * V11 - Acciones por fila del módulo de usuarios.
 *
 * Reúne las cuatro operaciones que el PDF pide sobre un usuario ya existente:
 * editar (2.2), activar y desactivar (2.3), revocar el token de sesión (2.5) y
 * eliminar.
 *
 * Se separa de la pantalla de usuarios porque son muchas llamadas al backend y
 * mucha lógica de permisos: dejarlas dentro de la tabla volvería esa pantalla
 * difícil de leer.
 *
 * @param {object}   usuario              Usuario de la fila.
 * @param {object}   usuarioActual        Usuario en sesión, para saber qué acciones están bloqueadas.
 * @param {Function} onEditar             Abre la ventana de edición.
 * @param {Function} onActualizado        Se llama cuando el backend confirma un cambio, para recargar la lista.
 * @param {Function} onSolicitarEliminar  Pide abrir la confirmación de borrado.
 */
function AccionesUsuario({
  usuario,
  usuarioActual,
  onEditar,
  onActualizado,
  onSolicitarEliminar,
}) {
  // Identificador de la operación en curso, para mostrar el indicador solo en el
  // botón que se pulsó y bloquear los demás mientras tanto.
  const [operacionEnCurso, setOperacionEnCurso] = useState(null);
  const [error, setError] = useState('');

  /** Es el usuario que está usando la aplicación en este momento. */
  const esUnoMismo = usuario.id === usuarioActual?.id;

  /** Indica si hay alguna operación en marcha dentro de esta fila. */
  const ocupado = operacionEnCurso !== null;

  /**
   * Envuelve cualquier llamada al backend.
   *
   * Todas las operaciones de esta fila comparten lo mismo: marcar el botón como
   * ocupado, avisar a la pantalla para que recargue y mostrar el mensaje de error
   * del backend si algo sale mal (por ejemplo, "No puedes desactivar tu propio usuario.").
   */
  async function ejecutar(nombreOperacion, peticion) {
    setError('');
    setOperacionEnCurso(nombreOperacion);

    try {
      await peticion();
      onActualizado();
    } catch (fallo) {
      setError(mensajeDeError(fallo));
    } finally {
      setOperacionEnCurso(null);
    }
  }

  /** Activa o desactiva la cuenta (requerimiento 2.3). */
  function cambiarEstado() {
    ejecutar('estado', () =>
      api.patch(`/usuarios/${usuario.id}/estado`, { estaActivo: !usuario.estaActivo })
    );
  }

  /** Revoca el token de refresco, es decir, cierra las sesiones abiertas (requerimiento 2.5). */
  function revocarToken() {
    ejecutar('token', () => api.delete(`/usuarios/${usuario.id}/token-sesion`));
  }

  return (
    <>
      <ButtonGroup size="sm">
        <Button
          variant="outline-secondary"
          disabled={ocupado}
          title="Editar los datos del usuario"
          onClick={() => onEditar(usuario)}
        >
          Editar
        </Button>

        {/*
          El backend rechaza que un administrador se desactive a sí mismo, así que el
          botón se bloquea en la propia fila. El atributo title explica el motivo.
        */}
        <Button
          variant={usuario.estaActivo ? 'outline-warning' : 'outline-success'}
          disabled={ocupado || esUnoMismo}
          title={
            esUnoMismo
              ? 'No puedes activar ni desactivar tu propio usuario.'
              : usuario.estaActivo
                ? 'Desactivar la cuenta'
                : 'Activar la cuenta'
          }
          onClick={cambiarEstado}
        >
          {operacionEnCurso === 'estado' ? (
            <Spinner animation="border" size="sm" />
          ) : usuario.estaActivo ? (
            'Desactivar'
          ) : (
            'Activar'
          )}
        </Button>

        {/*
          Solo tiene sentido revocar la sesión si el usuario tiene un token asignado,
          dato que el backend expone en el campo tieneTokenSesion.
        */}
        <Button
          variant="outline-primary"
          disabled={ocupado || !usuario.tieneTokenSesion || esUnoMismo}
          title={
            esUnoMismo
              ? 'No puedes revocar tu propio token de sesión.'
              : usuario.tieneTokenSesion
                ? 'Cerrar las sesiones abiertas de este usuario'
                : 'Este usuario no tiene una sesión abierta.'
          }
          onClick={revocarToken}
        >
          {operacionEnCurso === 'token' ? <Spinner animation="border" size="sm" /> : 'Revocar sesión'}
        </Button>

        {/*
          El borrado no se hace aquí: se avisa a la pantalla para que abra la ventana
          de confirmación, porque eliminar un usuario es irreversible.
        */}
        <Button
          variant="outline-danger"
          disabled={ocupado}
          title="Eliminar el usuario"
          onClick={() => onSolicitarEliminar(usuario)}
        >
          Eliminar
        </Button>
      </ButtonGroup>

      {/* El error se muestra debajo de los botones de la fila para que quede claro a
          qué usuario corresponde. */}
      {error && <div className="text-danger small mt-1">{error}</div>}
    </>
  );
}

export default AccionesUsuario;
