import { useEffect, useState } from 'react';
import Modal from 'react-bootstrap/Modal';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import InputGroup from 'react-bootstrap/InputGroup';
import Spinner from 'react-bootstrap/Spinner';
import api, { mensajeDeError } from '../utils/api';
import Alerta from '../componentes/Alerta';

/**
 * V7 - Renombrar archivo o carpeta (requerimiento 3.4 del PDF).
 *
 * Regla clave del PDF: de los archivos "únicamente se permite cambiar el nombre,
 * no la extensión". Por eso el campo solo edita el nombre y la extensión se
 * muestra aparte, como texto fijo, para que quede claro que no se puede tocar.
 *
 * El backend guarda el nombre y la extensión en columnas separadas, así que si se
 * enviara el nombre completo con la extensión incluida, la extensión quedaría
 * duplicada.
 *
 * @param {boolean}  mostrar       Controla si la ventana está abierta.
 * @param {object}   elemento      Archivo o carpeta que se va a renombrar.
 * @param {string}   tipo          "archivo" o "directorio". Define la ruta a llamar.
 * @param {Function} onCerrar      Se llama al cerrar la ventana.
 * @param {Function} onRenombrado  Se llama cuando el backend confirma el cambio.
 */
function ModalRenombrar({ mostrar, elemento, tipo, onCerrar, onRenombrado }) {
  const [nombre, setNombre] = useState('');
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(false);

  const esArchivo = tipo === 'archivo';

  /**
   * Identificador del elemento que se va a renombrar, leído UNA sola vez y de
   * forma segura.
   *
   * Es el mismo motivo que en ModalSubir: el componente se dibuja siempre, aunque
   * la ventana esté cerrada, y entonces "elemento" llega como null. El compilador
   * de React adelanta al momento de dibujar la lectura de las propiedades usadas
   * dentro de las funciones memorizadas, así que leer "elemento.id" dentro de
   * "renombrar" hacía fallar la pantalla de repositorio con
   * "Cannot read properties of null (reading 'id')".
   */
  const idElemento = elemento?.id;

  /**
   * Cada vez que se abre la ventana se precarga el nombre actual.
   * En los archivos se precarga "nombre", que NO incluye la extensión.
   */
  useEffect(() => {
    if (mostrar && elemento) {
      setNombre(elemento.nombre || '');
      setError('');
      setCargando(false);
    }
  }, [mostrar, elemento]);

  /** Cierra la ventana. */
  function cerrar() {
    setError('');
    setCargando(false);
    onCerrar();
  }

  /** Envía el nombre nuevo al backend. */
  async function renombrar(evento) {
    evento.preventDefault();
    setError('');
    setCargando(true);

    try {
      // Se usa "idElemento" (ya leído de forma segura) y no "elemento.id".
      const ruta = esArchivo
        ? `/archivos/${idElemento}`
        : `/directorios/${idElemento}`;

      // PATCH porque el PDF habla de editar: solo cambia un campo, no el objeto entero.
      await api.patch(ruta, { nombre: nombre.trim() });

      onRenombrado();
      cerrar();
    } catch (fallo) {
      // Por ejemplo: "Ya existe un directorio con ese nombre en el mismo nivel."
      // o 403 si el usuario no es el dueño ni administrador.
      setError(mensajeDeError(fallo));
      setCargando(false);
    }
  }

  return (
    <Modal show={mostrar} onHide={cerrar} centered>
      <Form onSubmit={renombrar}>
        <Modal.Header closeButton>
          <Modal.Title>{esArchivo ? 'Renombrar archivo' : 'Renombrar carpeta'}</Modal.Title>
        </Modal.Header>

        <Modal.Body>
          <Alerta mensaje={error} />

          {esArchivo ? (
            <>
              <Form.Group className="mb-2" controlId="nombreArchivo">
                <Form.Label>Nombre</Form.Label>
                {/* InputGroup permite pegar la extensión al campo como texto fijo. */}
                <InputGroup>
                  <Form.Control
                    value={nombre}
                    onChange={(evento) => setNombre(evento.target.value)}
                    maxLength={200}
                    required
                    autoFocus
                  />
                  <InputGroup.Text>{elemento?.extension}</InputGroup.Text>
                </InputGroup>
              </Form.Group>
              <Form.Text className="text-muted">
                Solo se puede cambiar el nombre. La extensión se conserva.
              </Form.Text>
            </>
          ) : (
            <Form.Group className="mb-3" controlId="nombreCarpeta">
              <Form.Label>Nombre</Form.Label>
              <Form.Control
                value={nombre}
                onChange={(evento) => setNombre(evento.target.value)}
                maxLength={200}
                required
                autoFocus
              />
            </Form.Group>
          )}
        </Modal.Body>

        <Modal.Footer>
          <Button variant="secondary" onClick={cerrar} disabled={cargando}>
            Cancelar
          </Button>
          <Button type="submit" disabled={cargando}>
            {cargando ? <Spinner animation="border" size="sm" /> : 'Guardar'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

export default ModalRenombrar;
