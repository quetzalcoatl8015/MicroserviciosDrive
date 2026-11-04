import { NavLink, Outlet, useNavigate } from 'react-router';
import Navbar from 'react-bootstrap/Navbar';
import Nav from 'react-bootstrap/Nav';
import Button from 'react-bootstrap/Button';
import Container from 'react-bootstrap/Container';
import Badge from 'react-bootstrap/Badge';
import api from './utils/api';
import { datosUsuario, limpiarSesion } from './utils/sesion';

/**
 * V3 - Marco de la aplicación.
 *
 * Es el componente que envuelve todas las pantallas internas: dibuja la barra de
 * navegación y deja el hueco donde el enrutador coloca la pantalla activa.
 *
 * Los datos que muestra (nombre del usuario y si es administrador) se leen del
 * propio token con utils/sesion.js, porque el backend no los devuelve en el Login.
 * Recuerda que ese dato solo sirve para decidir qué mostrar: los permisos reales
 * los valida siempre el backend.
 */
function MainLayout() {
  const navegar = useNavigate();
  const usuario = datosUsuario();

  /**
   * Cierra la sesión.
   *
   * El orden importa y es un requisito del PDF: primero se avisa al backend para
   * que revoque el Refresh Token en la base de datos (POST cerrar-sesion), y solo
   * después se limpia el navegador. La versión anterior solo borraba el
   * localStorage, así que el token seguía siendo válido en el servidor.
   */
  async function cerrarSesion() {
    try {
      await api.post('/autenticacion/cerrar-sesion');
    } catch {
      // Si el token ya no servía, el backend responde 401. Eso no debe impedir que
      // el usuario cierre sesión en el navegador, así que el error se ignora.
    } finally {
      limpiarSesion();
      navegar('/login', { replace: true });
    }
  }

  return (
    <>
      <Navbar expand="lg" className="bg-body-tertiary">
        <Container fluid>
          <Navbar.Brand href="/repositorio">Repositorio de archivos</Navbar.Brand>

          <Nav className="me-auto" navbar>
            <Nav.Link as={NavLink} to="/repositorio">
              Repositorio
            </Nav.Link>

            {/* El módulo de usuarios es exclusivo de administradores (requerimiento 2),
                así que el enlace se oculta a quien no lo es. Esto es solo apariencia:
                el backend responde 403 si un usuario común intenta entrar. */}
            {usuario?.esAdmin && (
              <Nav.Link as={NavLink} to="/usuarios">
                Usuarios
              </Nav.Link>
            )}
          </Nav>

          <Nav className="ms-auto align-items-center" navbar>
            {usuario && (
              <Navbar.Text className="me-3">
                {usuario.nombre}{' '}
                <Badge bg={usuario.esAdmin ? 'primary' : 'secondary'}>
                  {usuario.esAdmin ? 'Administrador' : 'Usuario común'}
                </Badge>
              </Navbar.Text>
            )}

            <Button size="sm" variant="outline-danger" onClick={cerrarSesion}>
              Cerrar sesión
            </Button>
          </Nav>
        </Container>
      </Navbar>

      <Container fluid className="py-4">
        {/* Outlet es el hueco donde el enrutador dibuja la pantalla activa. */}
        <Outlet />
      </Container>
    </>
  );
}

export default MainLayout;
