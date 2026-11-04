import { useState } from 'react';
import Modal from 'react-bootstrap/Modal';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Spinner from 'react-bootstrap/Spinner';
import api, { mensajeDeError } from '../utils/api';
import Alerta from '../componentes/Alerta';

/**
 * V6 - Crear carpeta (requerimiento 3.1 del PDF).
 *
 * El PDF dice que para crear un directorio solo hace falta el nombre, y que no
 * puede haber dos directorios con el mismo nombre en el mismo nivel.
 *
 * @param {boolean}  mostrar     Controla si la ventana está abierta.
 * @param {object}   carpetaPadre Carpeta en la que se creará, o null si es la raíz.
 * @param {Function} onCerrar    Se llama al cerrar la ventana.
 * @param {Function} onCreado    Se llama cuando el backend confirma la creación.
 */
function ModalCarpeta({ mostrar, carpetaPadre, onCerrar, onCreado }) {
  const [nombre, setNombre] = useState('');
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(false);

  /** Limpia el formulario y cierra la ventana. */
  function cerrar() {
    setNombre('');
    setError('');
    setCargando(false);
    onCerrar();
  }

  /** Envía la creación al backend. */
  async function crear(evento) {
    evento.preventDefault();
    setError('');
    setCargando(true);

    try {
      // El backend rechaza los caracteres \ / : * ? " < > | y los nombres vacíos.
      const { data } = await api.post('/directorios', {
        nombre: nombre.trim(),
        // Si se está en la raíz no se envía padreId, para que quede en el primer nivel.
        // Enviarlo como null también funciona, pero omitirlo es más explícito.
        ...(carpetaPadre ? { padreId: carpetaPadre.id } : {}),
      });

      onCreado(data);
      cerrar();
    } catch (fallo) {
      // Aquí aparece, por ejemplo, "Ya existe un directorio con ese nombre en el mismo nivel."
      setError(mensajeDeError(fallo));
      setCargando(false);
    }
  }

  return (
    <Modal show={mostrar} onHide={cerrar} centered>
      <Form onSubmit={crear}>
        <Modal.Header closeButton>
          <Modal.Title>Nueva carpeta</Modal.Title>
        </Modal.Header>

        <Modal.Body>
          <Alerta mensaje={error} />

          <Form.Group className="mb-3" controlId="nombreCarpeta">
            <Form.Label>Nombre</Form.Label>
            <Form.Control
              value={nombre}
              onChange={(evento) => setNombre(evento.target.value)}
              maxLength={200}
              required
              autoFocus
            />
            <Form.Text className="text-muted">
              No se permiten los caracteres \ / : * ? &quot; &lt; &gt; |
            </Form.Text>
          </Form.Group>

          {/* La ubicación es informativa: no se elige aquí, viene de la pantalla. */}
          <Form.Group controlId="ubicacionCarpeta">
            <Form.Label>Se creará en</Form.Label>
            <Form.Control
              value={carpetaPadre ? carpetaPadre.nombre : 'Raíz del repositorio'}
              readOnly
              plaintext
            />
          </Form.Group>
        </Modal.Body>

        <Modal.Footer>
          <Button variant="secondary" onClick={cerrar} disabled={cargando}>
            Cancelar
          </Button>
          <Button type="submit" disabled={cargando}>
            {cargando ? <Spinner animation="border" size="sm" /> : 'Crear carpeta'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

export default ModalCarpeta;
