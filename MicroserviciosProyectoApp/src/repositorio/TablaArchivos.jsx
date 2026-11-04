import { useState } from 'react';
import Table from 'react-bootstrap/Table';
import Button from 'react-bootstrap/Button';
import ButtonGroup from 'react-bootstrap/ButtonGroup';
import Spinner from 'react-bootstrap/Spinner';
import api from '../utils/api';
import { formatearFecha, formatearTamano } from '../utils/formato';

/**
 * Tabla de archivos de la carpeta que se está consultando.
 *
 * @param {Array}    archivos     Archivos que devolvió el backend en esta página.
 * @param {object}   usuario      Datos del usuario en sesión ({ id, nombre, esAdmin }).
 * @param {Function} onRenombrar  Se llama al pedir el cambio de nombre.
 * @param {Function} onEliminar   Se llama al pedir la eliminación.
 */
function TablaArchivos({ archivos, usuario, onRenombrar, onEliminar }) {
  // Identificador del archivo que se está descargando, para mostrar el indicador
  // únicamente en la fila correspondiente.
  const [descargandoId, setDescargandoId] = useState(null);
  const [errorDescarga, setErrorDescarga] = useState('');

  /**
   * Indica si el usuario en sesión subió el archivo.
   * El identificador del token se lee como texto, así que se convierte antes de comparar.
   */
  const esPropietario = (archivo) => Number(archivo.propietarioId) === usuario?.id;

  /** El creador del archivo o cualquier administrador pueden renombrar y eliminar. */
  const puedeGestionar = (archivo) => Boolean(usuario?.esAdmin || esPropietario(archivo));

  /**
   * Descarga el contenido del archivo.
   *
   * Este es el punto donde el proyecto solía fallar: un enlace normal (<a href>) NO
   * envía la cabecera Authorization, así que la petición llegaba sin token y el
   * backend respondía 401. Por eso la descarga se pide con Axios, que sí pasa por el
   * interceptor y anexa el token, y se pide como "blob" (datos binarios) en lugar de
   * JSON.
   *
   * Una vez recibido el binario se fabrica un enlace temporal en memoria, se simula
   * el clic para que el navegador abra el diálogo de descarga y se libera la memoria
   * con revokeObjectURL, que si no quedaría ocupada hasta recargar la página.
   */
  async function descargar(archivo) {
    setErrorDescarga('');
    setDescargandoId(archivo.id);

    try {
      const respuesta = await api.get(`/archivos/${archivo.id}/descargar`, {
        // Sin responseType "blob", Axios intentaría interpretar el contenido como texto.
        responseType: 'blob',
      });

      const url = window.URL.createObjectURL(new Blob([respuesta.data]));

      const enlace = document.createElement('a');
      enlace.href = url;
      // El nombre con el que se guarda en el equipo del usuario. Se usa el nombre
      // completo que ya devuelve el backend (nombre + extensión).
      enlace.download = archivo.nombreCompleto;
      document.body.appendChild(enlace);
      enlace.click();
      enlace.remove();

      window.URL.revokeObjectURL(url);
    } catch {
      // Ojo: cuando la respuesta se pide como blob, el cuerpo del error también llega
      // en binario, así que no se puede leer el mensaje del backend. Se muestra un
      // texto propio.
      setErrorDescarga(
        `No se pudo descargar "${archivo.nombreCompleto}". Verifica que el archivo siga disponible en el servidor.`
      );
    } finally {
      setDescargandoId(null);
    }
  }

  if (!archivos.length) {
    return <p className="text-muted small mb-0">No hay archivos que coincidan con la consulta.</p>;
  }

  return (
    <>
      {errorDescarga && <p className="text-danger small mb-2">{errorDescarga}</p>}

      <Table responsive hover size="sm" className="align-middle mb-0">
        <thead>
          <tr>
            <th>Nombre</th>
            <th>Tipo</th>
            <th className="text-end">Tamaño</th>
            <th>Subido por</th>
            <th>Fecha de subida</th>
            <th className="text-end">Acciones</th>
          </tr>
        </thead>
        <tbody>
          {archivos.map((archivo) => {
            const gestionable = puedeGestionar(archivo);

            return (
              <tr key={archivo.id}>
                <td>{archivo.nombreCompleto}</td>

                <td className="text-muted">
                  {/* El backend guarda la extensión con su punto, por ejemplo ".pdf". */}
                  {archivo.extension || '-'}
                </td>

                <td className="text-end">{formatearTamano(archivo.tamanoBytes)}</td>

                {/* El propietario llega resuelto desde el backend, sin necesidad de
                    otra consulta: es el nombre del usuario que subió el archivo. */}
                <td className="text-muted">{archivo.propietario}</td>

                <td className="text-muted small">{formatearFecha(archivo.creadoEn)}</td>

                <td className="text-end">
                  <ButtonGroup size="sm">
                    {/* La descarga está abierta a cualquier usuario autenticado
                        (requerimiento 3.6), por eso este botón nunca se deshabilita. */}
                    <Button
                      variant="outline-primary"
                      disabled={descargandoId === archivo.id}
                      title="Descargar el archivo"
                      onClick={() => descargar(archivo)}
                    >
                      {descargandoId === archivo.id ? (
                        <Spinner animation="border" size="sm" />
                      ) : (
                        'Descargar'
                      )}
                    </Button>

                    <Button
                      variant="outline-secondary"
                      disabled={!gestionable}
                      title={
                        gestionable
                          ? 'Cambiar el nombre del archivo, nunca la extensión'
                          : 'Solo quien subió el archivo o un administrador pueden renombrarlo.'
                      }
                      onClick={() => onRenombrar(archivo)}
                    >
                      Renombrar
                    </Button>

                    <Button
                      variant="outline-danger"
                      disabled={!gestionable}
                      title={
                        gestionable
                          ? 'Eliminar el archivo'
                          : 'Solo quien subió el archivo o un administrador pueden eliminarlo.'
                      }
                      onClick={() => onEliminar(archivo)}
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
    </>
  );
}

export default TablaArchivos;
