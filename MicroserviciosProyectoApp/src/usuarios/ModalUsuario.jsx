import { useEffect, useState } from 'react';
import Modal from 'react-bootstrap/Modal';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import InputGroup from 'react-bootstrap/InputGroup';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import Spinner from 'react-bootstrap/Spinner';
import api, { mensajeDeError } from '../utils/api';
import Alerta from '../componentes/Alerta';

/**
 * V10 - Crear o editar un usuario (requerimientos 2.1 y 2.2 del PDF).
 *
 * La misma ventana sirve para las dos operaciones porque los campos son los mismos:
 * la única diferencia es que al editar la contraseña es opcional.
 *
 * Datos que pide el PDF en ambos casos: nombre, correo, contraseña (dos veces) y
 * fecha de nacimiento, más el rol de administrador.
 *
 * @param {boolean}  mostrar       Controla si la ventana está abierta.
 * @param {object}   usuario       Usuario a editar. Si es null, la ventana crea uno nuevo.
 * @param {object}   usuarioActual Usuario en sesión, para bloquear el cambio de rol propio.
 * @param {Function} onCerrar      Se llama al cerrar la ventana.
 * @param {Function} onGuardado    Se llama cuando el backend confirma la operación.
 */
function ModalUsuario({ mostrar, usuario, usuarioActual, onCerrar, onGuardado }) {
  const editando = Boolean(usuario);

  // Campos del formulario.
  const [nombre, setNombre] = useState('');
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [confirmarContrasena, setConfirmarContrasena] = useState('');
  const [fechaNacimiento, setFechaNacimiento] = useState('');
  const [esAdmin, setEsAdmin] = useState(false);
  const [verContrasena, setVerContrasena] = useState(false);

  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(false);

  /**
   * Precarga el formulario cada vez que la ventana se abre.
   *
   * Es importante vaciar todos los campos: sin esto, al editar un usuario después de
   * haber creado otro, quedarían los datos anteriores y se enviarían sin que el
   * usuario se diera cuenta.
   */
  useEffect(() => {
    if (!mostrar) return;

    setError('');
    setCargando(false);
    setVerContrasena(false);
    // Las contraseñas nunca se rellenan: el backend solo guarda su hash y no puede
    // devolverlas. Al editar, dejarlas vacías significa "no cambiar la contraseña".
    setContrasena('');
    setConfirmarContrasena('');

    if (usuario) {
      setNombre(usuario.nombre || '');
      setCorreo(usuario.correo || '');
      // El backend devuelve la fecha de nacimiento como AAAA-MM-DD, que es justo lo
      // que necesita el input de tipo date.
      setFechaNacimiento(usuario.fechaNacimiento || '');
      setEsAdmin(Boolean(usuario.esAdmin));
    } else {
      setNombre('');
      setCorreo('');
      setFechaNacimiento('');
      setEsAdmin(false);
    }
  }, [mostrar, usuario]);

  /** Cierra la ventana. */
  function cerrar() {
    setError('');
    onCerrar();
  }

  /**
   * Es el propio usuario en sesión.
   * El backend rechaza revocarse a sí mismo el rol de administrador, así que la
   * casilla se bloquea aquí para no llevar al usuario a un error evitable.
   */
  const esUnoMismo = editando && usuario.id === usuarioActual?.id;

  /** Guarda los cambios: crea el usuario o lo actualiza. */
  async function guardar(evento) {
    evento.preventDefault();
    setError('');

    // Comprobación en el cliente para avisar antes de gastar una petición. El backend
    // valida lo mismo por su cuenta.
    if (contrasena !== confirmarContrasena) {
      setError('Las contraseñas deben coincidir.');
      return;
    }

    // Al crear, la contraseña es obligatoria. Al editar puede venir vacía.
    if (!editando && !contrasena) {
      setError('La contraseña es obligatoria.');
      return;
    }

    setCargando(true);

    try {
      if (editando) {
        // PUT porque se reemplazan los datos del usuario, no un solo campo.
        const cuerpo = { nombre, correo, fechaNacimiento, esAdmin };

        // Solo se envía la contraseña si se escribió una. Mandarla vacía y sin
        // confirmación no cambia nada, pero es más claro omitirla del todo.
        if (contrasena) {
          cuerpo.contrasena = contrasena;
          cuerpo.confirmarContrasena = confirmarContrasena;
        }

        await api.put(`/usuarios/${usuario.id}`, cuerpo);
      } else {
        await api.post('/usuarios', {
          nombre,
          correo,
          contrasena,
          confirmarContrasena,
          fechaNacimiento,
          esAdmin,
        });
      }

      onGuardado();
      cerrar();
    } catch (fallo) {
      // Aquí aparece, por ejemplo, "Ya existe un usuario registrado con ese correo."
      setError(mensajeDeError(fallo));
      setCargando(false);
    }
  }

  return (
    <Modal show={mostrar} onHide={cerrar} centered>
      <Form onSubmit={guardar}>
        <Modal.Header closeButton>
          <Modal.Title>{editando ? 'Editar usuario' : 'Nuevo usuario'}</Modal.Title>
        </Modal.Header>

        <Modal.Body>
          <Alerta mensaje={error} />

          <Form.Group className="mb-3" controlId="nombreUsuario">
            <Form.Label>Nombre de usuario</Form.Label>
            <Form.Control
              value={nombre}
              onChange={(evento) => setNombre(evento.target.value)}
              maxLength={100}
              required
              autoFocus
            />
          </Form.Group>

          <Form.Group className="mb-3" controlId="correoUsuario">
            <Form.Label>Correo</Form.Label>
            <Form.Control
              type="email"
              value={correo}
              onChange={(evento) => setCorreo(evento.target.value)}
              required
            />
          </Form.Group>

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3" controlId="contrasenaUsuario">
                <Form.Label>Contraseña</Form.Label>
                {/* Al editar, la contraseña deja de ser obligatoria: dejarla vacía
                    significa conservar la actual. */}
                <InputGroup>
                  <Form.Control
                    type={verContrasena ? 'text' : 'password'}
                    value={contrasena}
                    onChange={(evento) => setContrasena(evento.target.value)}
                    required={!editando}
                  />
                  {/*
                    Botón que alterna la visibilidad de las dos contraseñas.

                    Se usa texto ("Mostrar" / "Ocultar") en lugar de un emoji: los
                    emojis dependen de la fuente del sistema y pueden aparecer como un
                    cuadro vacío en algunos equipos, mientras que el texto siempre se
                    lee. Los dos botones comparten el estado "verContrasena" para que
                    las dos casillas cambien a la vez.
                  */}
                  <Button
                    variant="outline-secondary"
                    onClick={() => setVerContrasena(!verContrasena)}
                  >
                    {verContrasena ? 'Ocultar' : 'Mostrar'}
                  </Button>
                </InputGroup>
                {editando && (
                  <Form.Text className="text-muted">
                    Déjala vacía para conservar la contraseña actual. Cambiarla cierra
                    las sesiones abiertas del usuario.
                  </Form.Text>
                )}
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3" controlId="confirmarContrasenaUsuario">
                <Form.Label>Confirmar contraseña</Form.Label>
                <InputGroup>
                  <Form.Control
                    type={verContrasena ? 'text' : 'password'}
                    value={confirmarContrasena}
                    onChange={(evento) => setConfirmarContrasena(evento.target.value)}
                    required={!editando}
                  />
                  <Button
                    variant="outline-secondary"
                    onClick={() => setVerContrasena(!verContrasena)}
                  >
                    {verContrasena ? 'Ocultar' : 'Mostrar'}
                  </Button>
                </InputGroup>
              </Form.Group>
            </Col>
          </Row>

          <Form.Group className="mb-3" controlId="fechaNacimientoUsuario">
            <Form.Label>Fecha de nacimiento</Form.Label>
            {/* El PDF pide este dato en el autoregistro (1.1), al crear usuarios (2.1),
                al editarlos (2.2) y como filtro de búsqueda (2.4). */}
            <Form.Control
              type="date"
              value={fechaNacimiento}
              onChange={(evento) => setFechaNacimiento(evento.target.value)}
              required
            />
          </Form.Group>

          <Form.Group controlId="rolUsuario">
            <Form.Check
              type="switch"
              label="Administrador"
              checked={esAdmin}
              disabled={esUnoMismo}
              onChange={(evento) => setEsAdmin(evento.target.checked)}
            />
            <Form.Text className="text-muted">
              {esUnoMismo
                ? 'No puedes revocar tu propio rol de administrador.'
                : 'Un administrador puede gestionar usuarios y eliminar carpetas con contenido.'}
            </Form.Text>
          </Form.Group>

          {/* Aviso útil al crear: a diferencia del autoregistro, aquí la cuenta nace
              activa, así que no hay que activarla después. */}
          {!editando && (
            <p className="text-muted small mt-3 mb-0">
              El usuario queda activo desde el momento en que se crea, a diferencia de
              quien se registra por su cuenta.
            </p>
          )}
        </Modal.Body>

        <Modal.Footer>
          <Button variant="secondary" onClick={cerrar} disabled={cargando}>
            Cancelar
          </Button>
          <Button type="submit" disabled={cargando}>
            {cargando ? <Spinner animation="border" size="sm" /> : editando ? 'Guardar cambios' : 'Crear usuario'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

export default ModalUsuario;
