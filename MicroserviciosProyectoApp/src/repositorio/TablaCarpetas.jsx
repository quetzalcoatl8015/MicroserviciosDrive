import Table from 'react-bootstrap/Table';
import Button from 'react-bootstrap/Button';
import ButtonGroup from 'react-bootstrap/ButtonGroup';
import Badge from 'react-bootstrap/Badge';
import { formatearFecha } from '../utils/formato';

/**
 * Tabla de carpetas del nivel que se está consultando dentro del repositorio.
 *
 * Está separada de la pantalla principal por dos razones:
 *   1. La pantalla principal ya maneja dos listas (carpetas y archivos) y bastante
 *      estado; tener las tablas aparte la mantiene legible.
 *   2. Las reglas de permisos son distintas para carpetas y para archivos, así que
 *      conviene que cada tabla las explique en su propio archivo.
 *
 * @param {Array}    carpetas      Directorios que devolvió el backend en esta página.
 * @param {object}   usuario       Datos del usuario en sesión ({ id, nombre, esAdmin }).
 * @param {Function} onAbrir       Se llama al hacer clic en el nombre para entrar a la carpeta.
 * @param {Function} onRenombrar   Se llama al pedir el cambio de nombre.
 * @param {Function} onEliminar    Se llama al pedir la eliminación.
 */
function TablaCarpetas({ carpetas, usuario, onAbrir, onRenombrar, onEliminar }) {
  // Sin registros se informa en texto en lugar de dibujar una tabla vacía.
  if (!carpetas.length) {
    return <p className="text-muted small mb-0">No hay carpetas en este nivel.</p>;
  }

  /**
   * Indica si el usuario en sesión es el creador del elemento.
   *
   * El backend compara el identificador del token con el campo propietarioId, y aquí
   * se hace exactamente lo mismo para decidir si se habilitan los botones.
   * Se convierte a Number porque el identificador del token se lee como texto.
   */
  const esPropietario = (elemento) => Number(elemento.propietarioId) === usuario?.id;

  /** El creador del elemento o cualquier administrador pueden renombrar y eliminar. */
  const puedeGestionar = (elemento) => Boolean(usuario?.esAdmin || esPropietario(elemento));

  /**
   * Motivo por el que el botón de eliminar está deshabilitado, o cadena vacía si se puede usar.
   *
   * El backend rechaza la operación con 403 (no es el dueño) o con 400 (usuario común
   * sobre una carpeta con contenido). Deshabilitar el botón evita que el usuario
   * descubra la regla por medio de un error.
   */
  const motivoNoEliminar = (carpeta) => {
    if (!puedeGestionar(carpeta)) {
      return 'Solo el creador de la carpeta o un administrador pueden eliminarla.';
    }

    if (!usuario?.esAdmin && !carpeta.estaVacio) {
      return 'Solo un administrador puede eliminar carpetas con contenido.';
    }

    return '';
  };

  return (
    <Table responsive hover size="sm" className="align-middle mb-0">
      <thead>
        <tr>
          <th>Nombre</th>
          <th>Creador</th>
          <th className="text-end">Archivos</th>
          <th className="text-end">Subcarpetas</th>
          <th>Estado</th>
          <th>Creada</th>
          <th className="text-end">Acciones</th>
        </tr>
      </thead>
      <tbody>
        {carpetas.map((carpeta) => {
          const motivo = motivoNoEliminar(carpeta);

          return (
            <tr key={carpeta.id}>
              <td>
                {/*
                  El nombre funciona como enlace para entrar a la carpeta. Es el gesto
                  habitual en cualquier explorador de archivos y evita un botón extra.
                */}
                <Button
                  variant="link"
                  size="sm"
                  className="p-0 text-decoration-none"
                  onClick={() => onAbrir(carpeta)}
                >
                  {carpeta.nombre}
                </Button>
              </td>

              <td className="text-muted">{carpeta.propietario}</td>

              <td className="text-end">{carpeta.cantidadArchivos}</td>
              <td className="text-end">{carpeta.cantidadSubdirectorios}</td>

              <td>
                {/* "Vacía" es el dato que decide si un usuario común puede eliminarla. */}
                <Badge bg={carpeta.estaVacio ? 'success' : 'secondary'}>
                  {carpeta.estaVacio ? 'Vacía' : 'Con contenido'}
                </Badge>
              </td>

              <td className="text-muted small">{formatearFecha(carpeta.creadoEn)}</td>

              <td className="text-end">
                <ButtonGroup size="sm">
                  <Button
                    variant="outline-secondary"
                    disabled={!puedeGestionar(carpeta)}
                    title={
                      puedeGestionar(carpeta)
                        ? 'Cambiar el nombre de la carpeta'
                        : 'Solo el creador de la carpeta o un administrador pueden renombrarla.'
                    }
                    onClick={() => onRenombrar(carpeta)}
                  >
                    Renombrar
                  </Button>

                  <Button
                    variant="outline-danger"
                    disabled={Boolean(motivo)}
                    title={motivo || 'Eliminar la carpeta'}
                    onClick={() => onEliminar(carpeta)}
                  >
                    Eliminar
                  </Button>
                </ButtonGroup>
              </td>
            </tr>
          );
        })}
      </tbody>
    </Table>
  );
}

export default TablaCarpetas;
