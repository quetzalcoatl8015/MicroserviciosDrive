import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import Button from 'react-bootstrap/Button';
import Card from 'react-bootstrap/Card';
import Form from 'react-bootstrap/Form';
import InputGroup from 'react-bootstrap/InputGroup';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import Spinner from 'react-bootstrap/Spinner';
import api, { mensajeDeError } from './utils/api';
import Alerta from './componentes/Alerta';

/**
 * V2 - Autoregistro (requerimiento 1.1 del PDF).
 *
 * El PDF lista los datos que debe proporcionar el usuario:
 *   - Nombre de usuario
 *   - Correo
 *   - Contraseña, que se pide DOS VECES para prevenir errores de escritura
 *   - Fecha de nacimiento
 *
 * La fecha de nacimiento no es un adorno: el PDF la vuelve a pedir en la sección 2.2
 * como dato editable y en la 2.4 como criterio de filtrado del módulo de usuarios,
 * así que no se puede omitir.
 *
 * Los usuarios que se registran por su cuenta quedan desactivados y como usuarios
 * comunes hasta que un administrador los active a mano.
 */
function Registro() {
  const navegar = useNavigate();

  // Datos del formulario, con los mismos nombres que espera el modelo ModeloRegistro.
  const [nombre, setNombre] = useState('');
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [confirmarContrasena, setConfirmarContrasena] = useState('');
  const [fechaNacimiento, setFechaNacimiento] = useState('');
  const [verContrasena, setVerContrasena] = useState(false);

  // Estado de la pantalla.
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState('');
  const [exito, setExito] = useState(false);

  /**
   * Envía el registro al backend.
   *
   * El backend valida por su cuenta que las dos contraseñas coincidan, pero se
   * comprueba también aquí para avisar antes de gastar una petición.
   */
  async function registrarse(evento) {
    evento.preventDefault();
    setError('');

    if (contrasena !== confirmarContrasena) {
      setError('Las contraseñas deben coincidir.');
      return;
    }

    setCargando(true);

    try {
      await api.post('/autenticacion/registro', {
        nombre,
        correo,
        contrasena,
        confirmarContrasena,
        // Un input de tipo date ya entrega el formato AAAA-MM-DD que espera el backend.
        fechaNacimiento,
      });

      // El backend responde 201 y la cuenta queda inactiva.
      setExito(true);
    } catch (fallo) {
      setError(mensajeDeError(fallo));
    } finally {
      setCargando(false);
    }
  }

  /**
   * Pantalla de confirmación.
   *
   * Es importante decirle al usuario que su cuenta quedó DESACTIVADA: es el
   * comportamiento que exige el PDF y, si no se comunica, el registro parece
   * haber fallado porque después no podrá iniciar sesión.
   */
  if (exito) {
    return (
      <div className="d-flex align-items-center justify-content-center vh-100 bg-body-tertiary">
        <Card style={{ width: '28rem' }} className="shadow-sm">
          <Card.Body className="p-4 text-center">
            <h5 className="mb-3">Cuenta creada</h5>
            <Alerta
              variante="success"
              mensaje="Tu registro se completó correctamente."
            />
            <p className="text-muted small">
              Tu cuenta está <strong>desactivada</strong> por ahora. Un administrador debe
              activarla antes de que puedas iniciar sesión.
            </p>
            <Button className="w-100" onClick={() => navegar('/login')}>
              Volver al inicio de sesión
            </Button>
          </Card.Body>
        </Card>
      </div>
    );
  }

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-body-tertiary py-4">
      <Card style={{ width: '34rem' }} className="shadow-sm">
        <Card.Body className="p-4">
          <Card.Title className="mb-1 text-center">Crear una cuenta</Card.Title>
          <Card.Text className="text-muted text-center small mb-4">
            Los datos son los que pide el sistema para el autoregistro
          </Card.Text>

          <Alerta mensaje={error} />

          <Form onSubmit={registrarse}>
            <Form.Group className="mb-3" controlId="nombre">
              <Form.Label>Nombre de usuario</Form.Label>
              <Form.Control
                value={nombre}
                onChange={(evento) => setNombre(evento.target.value)}
                required
                maxLength={100}
              />
            </Form.Group>

            <Form.Group className="mb-3" controlId="correo">
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
                <Form.Group className="mb-3" controlId="contrasena">
                  <Form.Label>Contraseña</Form.Label>
                  <InputGroup>
                    <Form.Control
                      type={verContrasena ? 'text' : 'password'}
                      value={contrasena}
                      onChange={(evento) => setContrasena(evento.target.value)}
                      required
                    />
                    {/*
                      Botón que alterna la visibilidad de las dos contraseñas a la vez.

                      Se usa texto ("Mostrar" / "Ocultar") en lugar de un emoji: los
                      emojis dependen de la fuente del sistema y pueden verse como un
                      cuadro vacío en algunos equipos, mientras que el texto siempre
                      se lee. Ambos botones comparten el mismo estado "verContrasena"
                      para que las dos casillas cambien juntas.
                    */}
                    <Button
                      variant="outline-secondary"
                      onClick={() => setVerContrasena(!verContrasena)}
                    >
                      {verContrasena ? 'Ocultar' : 'Mostrar'}
                    </Button>
                  </InputGroup>
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3" controlId="confirmarContrasena">
                  <Form.Label>Confirmar contraseña</Form.Label>
                  <InputGroup>
                    <Form.Control
                      type={verContrasena ? 'text' : 'password'}
                      value={confirmarContrasena}
                      onChange={(evento) => setConfirmarContrasena(evento.target.value)}
                      required
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

            <Form.Group className="mb-4" controlId="fechaNacimiento">
              <Form.Label>Fecha de nacimiento</Form.Label>
              <Form.Control
                type="date"
                value={fechaNacimiento}
                onChange={(evento) => setFechaNacimiento(evento.target.value)}
                required
              />
            </Form.Group>

            <Button type="submit" className="w-100" disabled={cargando}>
              {cargando ? (
                <>
                  <Spinner animation="border" size="sm" className="me-2" />
                  Registrando...
                </>
              ) : (
                'Registrarme'
              )}
            </Button>
          </Form>

          <div className="text-center mt-3 small">
            ¿Ya tienes cuenta? <Link to="/login">Inicia sesión</Link>
          </div>
        </Card.Body>
      </Card>
    </div>
  );
}

export default Registro;
