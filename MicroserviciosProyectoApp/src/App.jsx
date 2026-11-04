import { Navigate, Route, Routes } from 'react-router';
import { datosUsuario, haySesion } from './utils/sesion';
import Login from './Login';
import Registro from './Registro';
import MainLayout from './MainLayout';
import Repositorio from './repositorio/Repositorio';
import Usuarios from './usuarios/Usuarios';

/**
 * Tabla de rutas de la aplicación.
 *
 * Aquí vive el control de acceso del lado del cliente. Antes el proyecto declaraba
 * las rutas directamente en main.jsx, no tenía ruta raíz ("/") ni comprobaba si había
 * sesión, así que se podía entrar a /usuarios escribiendo la dirección a mano.
 *
 * IMPORTANTE: esto es solo comodidad para el usuario. Cualquiera puede modificar lo
 * que hay en el navegador, así que el backend sigue siendo el único que decide de
 * verdad: responde 401 sin token válido y 403 si el usuario no es administrador.
 */

/**
 * Envuelve las rutas que exigen haber iniciado sesión.
 *
 * Si no hay sesión se redirige al Login con "replace", para que el botón "atrás" del
 * navegador no regrese a una pantalla privada.
 */
function RutaPrivada({ children }) {
  if (!haySesion()) return <Navigate to="/login" replace />;

  return children;
}

/**
 * Envuelve las rutas exclusivas de administradores, como el módulo de usuarios.
 * El rol se lee del token, que es lo único que el usuario ya tiene en el navegador.
 */
function RutaAdmin({ children }) {
  if (!haySesion()) return <Navigate to="/login" replace />;

  // Si no es administrador se le manda al repositorio en lugar de mostrarle un error:
  // la pantalla no existe para él.
  if (!datosUsuario()?.esAdmin) return <Navigate to="/repositorio" replace />;

  return children;
}

function App() {
  return (
    <Routes>
      {/* El Login y el autoregistro son las únicas pantallas abiertas. Si ya hay
          sesión, no tiene sentido volver a mostrarlas. */}
      <Route
        path="/login"
        element={haySesion() ? <Navigate to="/repositorio" replace /> : <Login />}
      />
      <Route
        path="/registro"
        element={haySesion() ? <Navigate to="/repositorio" replace /> : <Registro />}
      />

      {/* Rutas internas: todas pasan por el marco de la aplicación (MainLayout),
          que dibuja la barra de navegación y el botón de cerrar sesión. */}
      <Route
        element={
          <RutaPrivada>
            <MainLayout />
          </RutaPrivada>
        }
      >
        <Route path="/repositorio" element={<Repositorio />} />

        <Route
          path="/usuarios"
          element={
            <RutaAdmin>
              <Usuarios />
            </RutaAdmin>
          }
        />
      </Route>

      {/* La raíz y cualquier dirección desconocida se resuelven según haya sesión.
          Antes "/" no existía y el usuario veía una página en blanco. */}
      <Route path="*" element={<Navigate to={haySesion() ? '/repositorio' : '/login'} replace />} />
    </Routes>
  );
}

export default App;
