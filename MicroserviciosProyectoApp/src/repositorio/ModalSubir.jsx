import { useState } from 'react';
import Modal from 'react-bootstrap/Modal';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Spinner from 'react-bootstrap/Spinner';
import ProgressBar from 'react-bootstrap/ProgressBar';
import api, { mensajeDeError } from '../utils/api';
import { formatearTamano } from '../utils/formato';
import Alerta from '../componentes/Alerta';

/** Límite de tamaño por archivo que impone el requerimiento 3.2 del PDF. */
const TAMANO_MAXIMO_BYTES = 10 * 1024 * 1024;

/**
 * V5 - Subir archivo (requerimiento 3.2 del PDF).
 *
 * Reglas del PDF que aplican aquí:
 *   - Los archivos solo pueden subirse dentro de un directorio, nunca en la raíz.
 *   - No pueden pesar más de 10 MB.
 *   - Cualquier tipo de archivo es aceptado mientras respete el tamaño.
 *
 * @param {boolean}  mostrar    Controla si la ventana está abierta.
 * @param {object}   directorio Carpeta destino. Es obligatoria.
 * @param {Function} onCerrar   Se llama al cerrar la ventana.
 * @param {Function} onSubido   Se llama cuando la subida termina bien.
 */
function ModalSubir({ mostrar, directorio, onCerrar, onSubido }) {
  /**
   * Identificador de la carpeta destino, leído UNA sola vez y de forma segura.
   *
   * ¿Por qué no se lee "directorio.id" dentro de la función "subir"? Porque este
   * componente se dibuja siempre, incluso con la ventana cerrada, y en la raíz del
   * repositorio no hay carpeta: la pantalla le pasa "null". El compilador de React
   * (reactCompilerPreset, activado en vite.config.js) adelanta la lectura de las
   * propiedades que se usan dentro de las funciones memorizadas, así que
   * "directorio.id" se evaluaba al dibujar y toda la pantalla fallaba con
   * "Cannot read properties of null (reading 'id')".
   *
   * Con el encadenamiento opcional (?.) la lectura devuelve undefined en lugar de
   * reventar, así que dibujar el componente siempre es seguro.
   */
  const idDirectorio = directorio?.id;

  const [archivo, setArchivo] = useState(null);
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(false);

  // Porcentaje de avance de la subida, para la barra de progreso.
  const [avance, setAvance] = useState(0);

  /** Limpia el formulario y cierra la ventana. */
  function cerrar() {
    setArchivo(null);
    setError('');
    setAvance(0);
    setCargando(false);
    onCerrar();
  }

  /**
   * Guarda el archivo elegido.
   * Se comprueba el tamaño aquí para avisar al instante, sin gastar una subida
   * completa que el backend rechazaría al final.
   */
  function elegir(evento) {
    const seleccionado = evento.target.files[0];
    setError('');

    if (!seleccionado) {
      setArchivo(null);
      return;
    }

    if (seleccionado.size > TAMANO_MAXIMO_BYTES) {
      setError(
        `El archivo pesa ${formatearTamano(seleccionado.size)} y el límite es 10 MB.`
      );
      setArchivo(null);
      evento.target.value = '';
      return;
    }

    setArchivo(seleccionado);
  }

  /** Envía el archivo al backend como multipart/form-data. */
  async function subir(evento) {
    evento.preventDefault();
    setError('');
    setCargando(true);
    setAvance(0);

    // El backend espera dos partes: "Archivo" con el contenido y "DirectorioId" con
    // el destino. Los nombres deben coincidir con el modelo ModeloSubirArchivo.
    // Se usa "idDirectorio" (ya leído de forma segura) y no "directorio.id".
    const datos = new FormData();
    datos.append('Archivo', archivo);
    datos.append('DirectorioId', idDirectorio);

    try {
      const { data } = await api.post('/archivos', datos, {
        // NO se fija la cabecera Content-Type a mano: Axios la construye con su
        // propio separador al detectar un FormData. Si se fija manualmente, el
        // backend no puede separar las partes y la subida falla.
        onUploadProgress: (eventoProgreso) => {
          if (eventoProgreso.total) {
            setAvance(Math.round((eventoProgreso.loaded * 100) / eventoProgreso.total));
          }
        },
      });

      onSubido(data);
      cerrar();
    } catch (fallo) {
      setError(mensajeDeError(fallo));
      setCargando(false);
    }
  }

  return (
    <Modal show={mostrar} onHide={cerrar} centered>
      <Form onSubmit={subir}>
        <Modal.Header closeButton>
          <Modal.Title>Subir archivo</Modal.Title>
        </Modal.Header>

        <Modal.Body>
          <Alerta mensaje={error} />

          <Form.Group className="mb-3" controlId="archivo">
            <Form.Label>Archivo</Form.Label>
            <Form.Control
              type="file"
              onChange={elegir}
              disabled={cargando}
              required
            />
            <Form.Text className="text-muted">
              Tamaño máximo 10 MB. Se acepta cualquier tipo de archivo.
            </Form.Text>
          </Form.Group>

          {/* El destino no se elige: es la carpeta en la que está el usuario.
              El backend no admite archivos en la raíz del repositorio. */}
          <Form.Group controlId="destino">
            <Form.Label>Se subirá a</Form.Label>
            <Form.Control value={directorio?.nombre || ''} readOnly plaintext />
          </Form.Group>

          {cargando && <ProgressBar now={avance} label={`${avance}%`} className="mt-3" />}
        </Modal.Body>

        <Modal.Footer>
          <Button variant="secondary" onClick={cerrar} disabled={cargando}>
            Cancelar
          </Button>
          <Button type="submit" disabled={cargando || !archivo}>
            {cargando ? <Spinner animation="border" size="sm" /> : 'Subir'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

export default ModalSubir;
