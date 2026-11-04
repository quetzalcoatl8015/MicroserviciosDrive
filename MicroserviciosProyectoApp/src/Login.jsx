import { useState } from 'react';
import { useNavigate, Link } from 'react-router';
import Button from 'react-bootstrap/Button';
import Card from 'react-bootstrap/Card';
import Form from 'react-bootstrap/Form';
import InputGroup from 'react-bootstrap/InputGroup';
import Spinner from 'react-bootstrap/Spinner';
import api, { mensajeDeError } from './utils/api';
import { guardarTokens } from './utils/sesion';
import Alerta from './componentes/Alerta';

/**
 * V1 - Inicio de sesión (requerimiento 1 del PDF).
 *
 * El PDF pide que esta sea la pantalla principal cuando no hay nadie autenticado,
 * con dos campos (E-mail y Contraseña) y un botón "Ingresar" que redirija a la
 * interfaz principal de consulta de archivos, que es la sección 3.5.
 */
function Login() {
  const navegar = useNavigate();

  // Datos del formulario.
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [verContrasena, setVerContrasena] = useState(false);

  // Estado de la pantalla mientras se habla con la API.
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState('');

  /**
   * Envía las credenciales al backend.
   *
   * Se conservan los nombres en español del backend (correo y contrasena) porque
   * es lo que espera el modelo CredencialesAcceso. La versión anterior del archivo
   * enviaba { email, password } a la ruta /auth/login, que no existe, y por eso el
   * inicio de sesión nunca funcionaba.
   */
  async function iniciarSesion(evento) {
    evento.preventDefault();

    setError('');
    setCargando(true);

    try {
      const { data } = await api.post('/autenticacion/iniciar-sesion', {
        correo,
        contrasena,
      });

      // El backend devuelve { tokenAcceso, tokenRefresco }.
      guardarTokens(data.tokenAcceso, data.tokenRefresco);

      // El PDF exige redirigir a la consulta de archivos (sección 3.5) tras entrar.
      // Se usa replace para que el botón "atrás" del navegador no regrese al Login.
      navegar('/repositorio', { replace: true });
    } catch (fallo) {
      // mensajeDeError aprovecha el texto que ya manda el backend, que distingue
      // entre credenciales incorrectas y una cuenta pendiente de activación.
      setError(mensajeDeError(fallo));
    } finally {
      setCargando(false);
    }
  }

  return (
    <div className="d-flex align-items-center justify-content-center vh-100 bg-body-tertiary">
      <Card style={{ width: '26rem' }} className="shadow-sm">
        <Card.Body className="p-4">
          <Card.Title className="mb-1 text-center">Repositorio de archivos</Card.Title>
          <Card.Text className="text-muted text-center small mb-4">
            Inicia sesión para acceder a tus archivos
          </Card.Text>

          <Alerta mensaje={error} />

          <Form onSubmit={iniciarSesion}>
            <Form.Group className="mb-3" controlId="correo">
              <Form.Label>E-mail</Form.Label>
              <Form.Control
                type="email"
                placeholder="correo@ejemplo.com"
                value={correo}
                onChange={(evento) => setCorreo(evento.target.value)}
                required
                autoFocus
              />
            </Form.Group>

            <Form.Group className="mb-4" controlId="contrasena">
              <Form.Label>Contraseña</Form.Label>
              <InputGroup>
                <Form.Control
                  type={verContrasena ? 'text' : 'password'}
                  placeholder="Contraseña"
                  value={contrasena}
                  onChange={(evento) => setContrasena(evento.target.value)}
                  required
                />
                {/*
                  Botón que alterna la visibilidad de la contraseña.

                  Se usa texto ("Mostrar" / "Ocultar") en lugar de un emoji o un
                  icono: los emojis dependen de la fuente instalada en el sistema y
                  en algunos equipos se ven como un cuadro vacío, así que el texto
                  garantiza que la acción se entienda siempre. El atributo title
                  repite la acción al pasar el cursor, por accesibilidad.
                */}
                <Button
                  variant="outline-secondary"
                  onClick={() => setVerContrasena(!verContrasena)}
                  title={verContrasena ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  {verContrasena ? 'Ocultar' : 'Mostrar'}
                </Button>
              </InputGroup>
            </Form.Group>

            {/* El botón se deshabilita durante la petición para evitar dobles envíos. */}
            <Button type="submit" className="w-100" disabled={cargando}>
              {cargando ? (
                <>
                  <Spinner animation="border" size="sm" className="me-2" />
                  Ingresando...
                </>
              ) : (
                'Ingresar'
              )}
            </Button>
          </Form>

          {/* Link de autoregistro que exige el requerimiento 1.1 del PDF. */}
          <div className="text-center mt-3 small">
            ¿No tienes cuenta? <Link to="/registro">Regístrate</Link>
          </div>
        </Card.Body>
      </Card>
    </div>
  );
}

export default Login;
