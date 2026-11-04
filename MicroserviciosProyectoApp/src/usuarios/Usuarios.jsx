import { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import Card from 'react-bootstrap/Card';
import Col from 'react-bootstrap/Col';
import Form from 'react-bootstrap/Form';
import Row from 'react-bootstrap/Row';
import Spinner from 'react-bootstrap/Spinner';
import Table from 'react-bootstrap/Table';
import Badge from 'react-bootstrap/Badge';
import api, { mensajeDeError } from '../utils/api';
import { datosUsuario } from '../utils/sesion';
import { formatearFecha, formatearFechaCorta } from '../utils/formato';
import Alerta from '../componentes/Alerta';
import Paginacion from '../componentes/Paginacion';
import AccionesUsuario from './AccionesUsuario';
import ModalUsuario from './ModalUsuario';
import ModalConfirmar from '../repositorio/ModalConfirmar';

/**
 * V9 - Módulo de usuarios (sección 2 del PDF). Solo para administradores.
 *
 * El PDF pide en 2.4 que esta lista se pueda filtrar por nombre, correo, fecha de
 * nacimiento, rol de administrador y si el usuario tiene una sesión abierta, y que
 * esté paginada con tamaño de página configurable.
 *
 * Esta pantalla estaba escrita contra una API que no existe: pedía GET "Users" y leía
 * un arreglo con campos "name" y "email". El backend responde en /api/usuarios con
 * { elementos, pagina, tamanoPagina, totalRegistros } y campos en español, así que la
 * lista nunca se llenaba.
 */
function Usuarios() {
  // Usuario en sesión: se necesita para bloquear las acciones sobre uno mismo.
  const usuarioActual = datosUsuario();

  // Datos de la lista.
  const [usuarios, setUsuarios] = useState([]);
  const [totalRegistros, setTotalRegistros] = useState(0);

  // Paginación.
  const [pagina, setPagina] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(10);

  /**
   * Filtros de la consulta.
   *
   * Igual que en el repositorio, se separa lo que el usuario escribe de lo que ya se
   * consultó: escribir no lanza peticiones, solo el botón "Buscar" lo hace.
   *
   * Los filtros de administrador y de sesión abierta son listas desplegables con la
   * opción "Todos", que se representa con una cadena vacía. Esa opción NO se envía al
   * backend: si se mandara, ASP.NET intentaría convertir "" a booleano y respondería
   * 400. Omitir el parámetro es lo que significa "sin filtrar".
   */
  const filtrosVacios = {
    nombre: '',
    correo: '',
    fechaNacimiento: '',
    esAdmin: '',
    tieneTokenSesion: '',
  };

  const [filtros, setFiltros] = useState(filtrosVacios);
  const [filtrosAplicados, setFiltrosAplicados] = useState(filtrosVacios);

  // Estado general.
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');
  const [recarga, setRecarga] = useState(0);

  // Ventanas.
  const [modalUsuario, setModalUsuario] = useState({ mostrar: false, usuario: null });
  const [modalEliminar, setModalEliminar] = useState({ mostrar: false, usuario: null });

  // ---------------------------------------------------------------------------
  // Consulta de la lista
  // ---------------------------------------------------------------------------
  useEffect(() => {
    // Evita aplicar respuestas viejas si el usuario cambia de página o de filtro
    // antes de que la petición anterior termine.
    let activo = true;

    async function consultar() {
      setCargando(true);

      const parametros = { pagina, tamanoPagina };

      if (filtrosAplicados.nombre) parametros.nombre = filtrosAplicados.nombre;
      if (filtrosAplicados.correo) parametros.correo = filtrosAplicados.correo;
      if (filtrosAplicados.fechaNacimiento) parametros.fechaNacimiento = filtrosAplicados.fechaNacimiento;
      if (filtrosAplicados.esAdmin) parametros.esAdmin = filtrosAplicados.esAdmin;
      if (filtrosAplicados.tieneTokenSesion) parametros.tieneTokenSesion = filtrosAplicados.tieneTokenSesion;

      try {
        const { data } = await api.get('/usuarios', { params: parametros });
        if (!activo) return;

        setUsuarios(data.elementos);
        setTotalRegistros(data.totalRegistros);
        setError('');
      } catch (fallo) {
        if (!activo) return;
        // Si un usuario común llegara a esta pantalla, el backend responde 403.
        setError(mensajeDeError(fallo));
      } finally {
        if (activo) setCargando(false);
      }
    }

    consultar();

    return () => {
      activo = false;
    };
  }, [pagina, tamanoPagina, filtrosAplicados, recarga]);

  /** Fuerza la recarga de la lista después de cualquier cambio. */
  function recargar() {
    setRecarga((numero) => numero + 1);
  }

  /** Elimina el usuario pendiente de confirmación. */
  async function confirmarEliminacion() {
    await api.delete(`/usuarios/${modalEliminar.usuario.id}`);
    recargar();
  }

  return (
    <>
      <Row className="align-items-center mb-3">
        <Col>
          <h4 className="mb-0">Usuarios</h4>
          <span className="text-muted small">
            Solo los administradores pueden ver este módulo
          </span>
        </Col>

        <Col xs="auto">
          <Button
            variant="primary"
            onClick={() => setModalUsuario({ mostrar: true, usuario: null })}
          >
            Nuevo usuario
          </Button>
        </Col>
      </Row>

      <Alerta mensaje={error} cerrable onCerrar={() => setError('')} />

      <Card>
        <Card.Header className="bg-white">
          <strong>Filtros</strong>
        </Card.Header>

        <Card.Body>
          <Form
            className="mb-3"
            onSubmit={(evento) => {
              evento.preventDefault();
              // Al filtrar se vuelve a la primera página: la actual puede quedar fuera
              // de rango con el nuevo resultado.
              setPagina(1);
              setFiltrosAplicados({ ...filtros });
            }}
          >
            <Row className="g-2">
              <Col md={3}>
                <Form.Label className="small mb-1">Nombre</Form.Label>
                <Form.Control
                  size="sm"
                  placeholder="Subcadena del nombre"
                  value={filtros.nombre}
                  onChange={(evento) => setFiltros({ ...filtros, nombre: evento.target.value })}
                />
              </Col>

              <Col md={3}>
                <Form.Label className="small mb-1">Correo</Form.Label>
                <Form.Control
                  size="sm"
                  placeholder="Subcadena del correo"
                  value={filtros.correo}
                  onChange={(evento) => setFiltros({ ...filtros, correo: evento.target.value })}
                />
              </Col>

              <Col md={2}>
                <Form.Label className="small mb-1">Fecha de nacimiento</Form.Label>
                {/* Este filtro compara la fecha exacta, no un rango. */}
                <Form.Control
                  size="sm"
                  type="date"
                  value={filtros.fechaNacimiento}
                  onChange={(evento) =>
                    setFiltros({ ...filtros, fechaNacimiento: evento.target.value })
                  }
                />
              </Col>

              <Col md={2}>
                <Form.Label className="small mb-1">Rol</Form.Label>
                <Form.Select
                  size="sm"
                  value={filtros.esAdmin}
                  onChange={(evento) => setFiltros({ ...filtros, esAdmin: evento.target.value })}
                >
                  <option value="">Todos</option>
                  <option value="true">Administradores</option>
                  <option value="false">Usuarios comunes</option>
                </Form.Select>
              </Col>

              <Col md={2}>
                <Form.Label className="small mb-1">Sesión</Form.Label>
                <Form.Select
                  size="sm"
                  value={filtros.tieneTokenSesion}
                  onChange={(evento) =>
                    setFiltros({ ...filtros, tieneTokenSesion: evento.target.value })
                  }
                >
                  <option value="">Todos</option>
                  <option value="true">Con sesión abierta</option>
                  <option value="false">Sin sesión</option>
                </Form.Select>
              </Col>
            </Row>

            <div className="mt-2">
              <Button size="sm" variant="outline-secondary" type="submit" className="me-2">
                Buscar
              </Button>
              <Button
                size="sm"
                variant="outline-secondary"
                type="button"
                onClick={() => {
                  setFiltros(filtrosVacios);
                  setFiltrosAplicados(filtrosVacios);
                  setPagina(1);
                }}
              >
                Limpiar
              </Button>

              {cargando && <Spinner animation="border" size="sm" className="ms-3" />}
            </div>
          </Form>

          <Table responsive hover size="sm" className="align-middle mb-0">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Correo</th>
                <th>Fecha de nacimiento</th>
                <th>Rol</th>
                <th>Estado</th>
                <th>Sesión</th>
                <th>Creado</th>
                <th className="text-end">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {usuarios.length === 0 && !cargando && (
                <tr>
                  <td colSpan={8} className="text-muted text-center">
                    No hay usuarios que coincidan con la consulta.
                  </td>
                </tr>
              )}

              {usuarios.map((usuario) => (
                <tr key={usuario.id}>
                  <td>{usuario.nombre}</td>
                  <td className="text-muted">{usuario.correo}</td>
                  <td className="text-muted small">{formatearFechaCorta(usuario.fechaNacimiento)}</td>

                  <td>
                    <Badge bg={usuario.esAdmin ? 'primary' : 'secondary'}>
                      {usuario.esAdmin ? 'Administrador' : 'Usuario común'}
                    </Badge>
                  </td>

                  {/* Una cuenta desactivada no puede iniciar sesión: los usuarios que
                      se autoregistran nacen en este estado hasta que un administrador
                      los active (requerimiento 1.1 y 2.3). */}
                  <td>
                    <Badge bg={usuario.estaActivo ? 'success' : 'danger'}>
                      {usuario.estaActivo ? 'Activo' : 'Desactivado'}
                    </Badge>
                  </td>

                  <td>
                    <Badge bg={usuario.tieneTokenSesion ? 'info' : 'light'} text={usuario.tieneTokenSesion ? undefined : 'dark'}>
                      {usuario.tieneTokenSesion ? 'Con sesión' : 'Sin sesión'}
                    </Badge>
                  </td>

                  <td className="text-muted small">{formatearFecha(usuario.creadoEn)}</td>

                  <td className="text-end">
                    <AccionesUsuario
                      usuario={usuario}
                      usuarioActual={usuarioActual}
                      onEditar={(seleccionado) =>
                        setModalUsuario({ mostrar: true, usuario: seleccionado })
                      }
                      onActualizado={recargar}
                      onSolicitarEliminar={(seleccionado) =>
                        setModalEliminar({ mostrar: true, usuario: seleccionado })
                      }
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>

          <Paginacion
            pagina={pagina}
            totalRegistros={totalRegistros}
            tamanoPagina={tamanoPagina}
            onCambiarPagina={setPagina}
            onCambiarTamano={setTamanoPagina}
          />
        </Card.Body>
      </Card>

      {/* V10: crear o editar. El mismo componente sirve para las dos operaciones. */}
      <ModalUsuario
        mostrar={modalUsuario.mostrar}
        usuario={modalUsuario.usuario}
        usuarioActual={usuarioActual}
        onCerrar={() => setModalUsuario({ mostrar: false, usuario: null })}
        onGuardado={recargar}
      />

      {/* V8 reutilizada: confirmación antes de eliminar un usuario. */}
      <ModalConfirmar
        mostrar={modalEliminar.mostrar}
        titulo="Eliminar usuario"
        mensaje={`¿Seguro que quieres eliminar a "${
          modalEliminar.usuario?.nombre || ''
        }"? También se revocará su sesión y esta acción no se puede deshacer.`}
        onConfirmar={confirmarEliminacion}
        onCerrar={() => setModalEliminar({ mostrar: false, usuario: null })}
      />
    </>
  );
}

export default Usuarios;
