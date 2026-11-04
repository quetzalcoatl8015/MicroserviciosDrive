import { useEffect, useState } from 'react';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import Button from 'react-bootstrap/Button';
import Card from 'react-bootstrap/Card';
import Col from 'react-bootstrap/Col';
import Form from 'react-bootstrap/Form';
import Row from 'react-bootstrap/Row';
import Spinner from 'react-bootstrap/Spinner';
import api, { mensajeDeError } from '../utils/api';
import { datosUsuario } from '../utils/sesion';
import Alerta from '../componentes/Alerta';
import Paginacion from '../componentes/Paginacion';
import TablaCarpetas from './TablaCarpetas';
import TablaArchivos from './TablaArchivos';
import ModalCarpeta from './ModalCarpeta';
import ModalSubir from './ModalSubir';
import ModalRenombrar from './ModalRenombrar';
import ModalConfirmar from './ModalConfirmar';

/**
 * V4 - Consulta de archivos (sección 3.5 del PDF).
 *
 * Es la pantalla principal del sistema: el PDF la define como la interfaz que debe
 * aparecer cuando un usuario inicia sesión.
 *
 * El repositorio se organiza en niveles. La pantalla muestra siempre DOS listas del
 * mismo nivel: las carpetas que hay dentro y los archivos que hay dentro.
 *
 * Regla importante del backend: la consulta de archivos exige obligatoriamente un
 * directorioId. En la raíz del repositorio no se consultan archivos porque no puede
 * haberlos; los archivos solo existen dentro de una carpeta.
 *
 * Los filtros se aplican al enviar el formulario, no al escribir, para no lanzar una
 * petición por cada tecla.
 */
function Repositorio() {
  // Datos del usuario en sesión. Se leen del token y solo sirven para mostrar u
  // ocultar botones: los permisos reales los valida el backend en cada petición.
  const usuario = datosUsuario();

  /**
   * Migas de pan.
   *
   * Es un arreglo de carpetas ordenado desde la raíz hasta la carpeta actual. Estar
   * en la raíz se representa con un arreglo vacío, y la carpeta actual es siempre el
   * último elemento. Guardar toda la ruta (y no solo la carpeta actual) es lo que
   * permite dibujar las migas y regresar a un nivel intermedio sin volver a consultar
   * el backend.
   */
  const [ruta, setRuta] = useState([]);
  const carpetaActual = ruta.length > 0 ? ruta[ruta.length - 1] : null;

  // ------------------------- Lista de carpetas -------------------------
  const [carpetas, setCarpetas] = useState([]);
  const [totalCarpetas, setTotalCarpetas] = useState(0);
  const [paginaCarpetas, setPaginaCarpetas] = useState(1);
  const [tamanoCarpetas, setTamanoCarpetas] = useState(10);

  // Valor que se escribe en el buscador y valor que ya se envió al backend.
  const [busquedaCarpeta, setBusquedaCarpeta] = useState('');
  const [filtroCarpeta, setFiltroCarpeta] = useState('');

  // ------------------------- Lista de archivos -------------------------
  const [archivos, setArchivos] = useState([]);
  const [totalArchivos, setTotalArchivos] = useState(0);
  const [paginaArchivos, setPaginaArchivos] = useState(1);
  const [tamanoArchivos, setTamanoArchivos] = useState(10);

  /**
   * Filtros de archivos.
   *
   * Se manejan en dos objetos separados: "filtrosArchivo" guarda lo que el usuario
   * escribe y "filtrosArchivoAplicados" lo que ya se consultó. Como el efecto que
   * consulta depende del segundo, escribir no dispara peticiones; solo el botón
   * "Buscar" las dispara.
   */
  const [filtrosArchivo, setFiltrosArchivo] = useState({
    nombre: '',
    extension: '',
    propietario: '',
    subidoDesde: '',
    subidoHasta: '',
  });
  const [filtrosArchivoAplicados, setFiltrosArchivoAplicados] = useState({
    nombre: '',
    extension: '',
    propietario: '',
    subidoDesde: '',
    subidoHasta: '',
  });

  // ------------------------- Estado general -------------------------
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');

  /**
   * Contador que se incrementa para forzar una recarga.
   *
   * Después de crear, subir, renombrar o eliminar hay que volver a pedir las listas
   * al backend. Incrementar este número hace que los efectos de carga se ejecuten de
   * nuevo sin tener que repetir la lógica de consulta.
   */
  const [recarga, setRecarga] = useState(0);

  // ------------------------- Ventanas -------------------------
  const [modalCarpeta, setModalCarpeta] = useState(false);
  const [modalSubir, setModalSubir] = useState(false);
  const [modalRenombrar, setModalRenombrar] = useState({ mostrar: false, elemento: null, tipo: 'archivo' });
  const [modalEliminar, setModalEliminar] = useState({ mostrar: false, elemento: null, tipo: 'archivo' });

  // ---------------------------------------------------------------------------
  // Consulta de carpetas
  // ---------------------------------------------------------------------------
  useEffect(() => {
    // "activo" evita aplicar respuestas viejas si el usuario cambia de carpeta o de
    // página antes de que la petición anterior termine.
    let activo = true;

    async function consultar() {
      setCargando(true);

      const parametros = { pagina: paginaCarpetas, tamanoPagina: tamanoCarpetas };

      // En la raíz NO se envía padreId. El backend interpreta la ausencia del
      // parámetro como "dame los directorios del primer nivel".
      if (carpetaActual) parametros.padreId = carpetaActual.id;

      // El filtro es por subcadena: "doc" encuentra "Documentos".
      if (filtroCarpeta) parametros.nombre = filtroCarpeta;

      try {
        const { data } = await api.get('/directorios', { params: parametros });
        if (!activo) return;

        // El backend siempre responde { elementos, pagina, tamanoPagina, totalRegistros }.
        setCarpetas(data.elementos);
        setTotalCarpetas(data.totalRegistros);
        setError('');
      } catch (fallo) {
        if (!activo) return;
        setError(mensajeDeError(fallo));
      } finally {
        if (activo) setCargando(false);
      }
    }

    consultar();

    return () => {
      activo = false;
    };
  }, [carpetaActual, paginaCarpetas, tamanoCarpetas, filtroCarpeta, recarga]);

  // ---------------------------------------------------------------------------
  // Consulta de archivos
  // ---------------------------------------------------------------------------
  useEffect(() => {
    // En la raíz no hay archivos que consultar: el backend exige un directorioId y no
    // existe un identificador para la raíz. La pantalla ya muestra un mensaje en ese
    // caso en lugar de la tabla, así que basta con no pedir nada. La lista se llena
    // de nuevo en cuanto se entra a una carpeta.
    if (!carpetaActual) return undefined;

    let activo = true;

    async function consultar() {
      setCargando(true);

      const parametros = { directorioId: carpetaActual.id, pagina: paginaArchivos, tamanoPagina: tamanoArchivos };

      // Solo se envían los filtros que tienen valor. Mandar un parámetro vacío haría
      // que el backend intentara convertirlo a número o a fecha y respondiera 400.
      const aplicados = filtrosArchivoAplicados;
      if (aplicados.nombre) parametros.nombre = aplicados.nombre;
      if (aplicados.extension) parametros.extension = aplicados.extension;
      if (aplicados.propietario) parametros.propietario = aplicados.propietario;
      if (aplicados.subidoDesde) parametros.subidoDesde = aplicados.subidoDesde;
      if (aplicados.subidoHasta) parametros.subidoHasta = aplicados.subidoHasta;

      try {
        const { data } = await api.get('/archivos', { params: parametros });
        if (!activo) return;

        setArchivos(data.elementos);
        setTotalArchivos(data.totalRegistros);
        setError('');
      } catch (fallo) {
        if (!activo) return;
        setError(mensajeDeError(fallo));
      } finally {
        if (activo) setCargando(false);
      }
    }

    consultar();

    return () => {
      activo = false;
    };
  }, [carpetaActual, paginaArchivos, tamanoArchivos, filtrosArchivoAplicados, recarga]);

  // ---------------------------------------------------------------------------
  // Navegación entre niveles
  // ---------------------------------------------------------------------------

  /** Entra a una carpeta y reinicia las listas y los filtros de ese nivel. */
  function abrirCarpeta(carpeta) {
    setRuta([...ruta, carpeta]);

    // Se vuelve a la primera página y se limpian los filtros: los de la carpeta
    // anterior no tienen por qué aplicar a la nueva.
    setPaginaCarpetas(1);
    setPaginaArchivos(1);
    setBusquedaCarpeta('');
    setFiltroCarpeta('');
    setFiltrosArchivo({ nombre: '', extension: '', propietario: '', subidoDesde: '', subidoHasta: '' });
    setFiltrosArchivoAplicados({ nombre: '', extension: '', propietario: '', subidoDesde: '', subidoHasta: '' });
  }

  /**
   * Regresa a un nivel anterior de la ruta.
   * @param {number} indice Posición de la carpeta destino. -1 significa la raíz.
   */
  function irANivel(indice) {
    setRuta(ruta.slice(0, indice + 1));

    setPaginaCarpetas(1);
    setPaginaArchivos(1);
    setBusquedaCarpeta('');
    setFiltroCarpeta('');
    setFiltrosArchivo({ nombre: '', extension: '', propietario: '', subidoDesde: '', subidoHasta: '' });
    setFiltrosArchivoAplicados({ nombre: '', extension: '', propietario: '', subidoDesde: '', subidoHasta: '' });
  }

  /** Fuerza la recarga de las dos listas. */
  function recargar() {
    setRecarga((numero) => numero + 1);
  }

  // ---------------------------------------------------------------------------
  // Operaciones
  // ---------------------------------------------------------------------------

  /** Elimina el elemento que está pendiente de confirmación. */
  async function confirmarEliminacion() {
    // La ruta depende del tipo: los archivos y las carpetas viven en controladores distintos.
    const base = modalEliminar.tipo === 'archivo' ? '/archivos' : '/directorios';

    await api.delete(`${base}/${modalEliminar.elemento.id}`);

    recargar();
  }

  /**
   * Texto del aviso del modal de confirmación.
   *
   * En una carpeta con contenido el backend borra todo el subárbol, tanto los
   * registros como los archivos físicos, así que conviene advertirlo con números.
   */
  function avisoDeEliminacion() {
    const elemento = modalEliminar.elemento;
    if (!elemento) return '';

    if (modalEliminar.tipo === 'directorio' && !elemento.estaVacio) {
      return `Esta carpeta contiene ${elemento.cantidadArchivos} archivo(s) y ` +
        `${elemento.cantidadSubdirectorios} subcarpeta(s). Se eliminará todo su contenido de forma permanente.`;
    }

    return '';
  }

  return (
    <>
      <Row className="align-items-center mb-3">
        <Col>
          {/* Migas de pan: siempre empiezan en la raíz y cada nivel es un enlace para
              regresar. El último nivel no es enlace porque es donde ya se está. */}
          <Breadcrumb className="mb-1">
            <Breadcrumb.Item onClick={() => irANivel(-1)} active={ruta.length === 0} linkAs="button">
              Raíz del repositorio
            </Breadcrumb.Item>

            {ruta.map((carpeta, indice) => (
              <Breadcrumb.Item
                key={carpeta.id}
                onClick={() => irANivel(indice)}
                active={indice === ruta.length - 1}
                linkAs="button"
              >
                {carpeta.nombre}
              </Breadcrumb.Item>
            ))}
          </Breadcrumb>

          <h4 className="mb-0">{carpetaActual ? carpetaActual.nombre : 'Repositorio de archivos'}</h4>
        </Col>

        <Col xs="auto">
          <Button variant="outline-secondary" className="me-2" onClick={recargar} disabled={cargando}>
            Actualizar
          </Button>

          {/* Crear carpetas se permite en cualquier nivel, incluida la raíz. */}
          <Button variant="outline-primary" className="me-2" onClick={() => setModalCarpeta(true)}>
            Nueva carpeta
          </Button>

          {/* Subir archivos solo dentro de una carpeta: el backend no admite archivos
              en la raíz del repositorio. */}
          <Button
            variant="primary"
            disabled={!carpetaActual}
            title={
              carpetaActual
                ? 'Subir un archivo a esta carpeta'
                : 'Entra a una carpeta para poder subir archivos: la raíz no admite archivos.'
            }
            onClick={() => setModalSubir(true)}
          >
            Subir archivo
          </Button>
        </Col>
      </Row>

      <Alerta mensaje={error} cerrable onCerrar={() => setError('')} />

      {/* ------------------------------------------------------------------
          Carpetas del nivel actual
      ------------------------------------------------------------------ */}
      <Card className="mb-4">
        <Card.Header className="bg-white">
          <Row className="align-items-center g-2">
            <Col xs="auto">
              <strong>Carpetas</strong>
            </Col>

            <Col>
              <Form
                className="d-flex"
                onSubmit={(evento) => {
                  evento.preventDefault();
                  // Al buscar se vuelve a la primera página: la página actual puede
                  // no existir con el nuevo filtro.
                  setPaginaCarpetas(1);
                  setFiltroCarpeta(busquedaCarpeta.trim());
                }}
              >
                <Form.Control
                  size="sm"
                  placeholder="Buscar carpeta por nombre"
                  value={busquedaCarpeta}
                  onChange={(evento) => setBusquedaCarpeta(evento.target.value)}
                />
                <Button size="sm" variant="outline-secondary" type="submit" className="ms-2">
                  Buscar
                </Button>
                <Button
                  size="sm"
                  variant="outline-secondary"
                  type="button"
                  className="ms-1"
                  onClick={() => {
                    setBusquedaCarpeta('');
                    setFiltroCarpeta('');
                    setPaginaCarpetas(1);
                  }}
                >
                  Limpiar
                </Button>
              </Form>
            </Col>

            <Col xs="auto">
              {cargando && <Spinner animation="border" size="sm" />}
            </Col>
          </Row>
        </Card.Header>

        <Card.Body>
          <TablaCarpetas
            carpetas={carpetas}
            usuario={usuario}
            onAbrir={abrirCarpeta}
            onRenombrar={(carpeta) => setModalRenombrar({ mostrar: true, elemento: carpeta, tipo: 'directorio' })}
            onEliminar={(carpeta) => setModalEliminar({ mostrar: true, elemento: carpeta, tipo: 'directorio' })}
          />

          <Paginacion
            pagina={paginaCarpetas}
            totalRegistros={totalCarpetas}
            tamanoPagina={tamanoCarpetas}
            onCambiarPagina={setPaginaCarpetas}
            onCambiarTamano={setTamanoCarpetas}
          />
        </Card.Body>
      </Card>

      {/* ------------------------------------------------------------------
          Archivos de la carpeta actual
      ------------------------------------------------------------------ */}
      <Card>
        <Card.Header className="bg-white">
          <strong>Archivos</strong>
        </Card.Header>

        <Card.Body>
          {!carpetaActual ? (
            // En la raíz solo puede haber carpetas. Se explica en lugar de mostrar una
            // tabla vacía que parecería un error.
            <p className="text-muted small mb-0">
              Estás en la raíz del repositorio. Entra a una carpeta para ver y subir archivos.
            </p>
          ) : (
            <>
              <Form
                className="mb-3"
                onSubmit={(evento) => {
                  evento.preventDefault();
                  setPaginaArchivos(1);
                  // Se copia el objeto para que el efecto detecte el cambio y consulte.
                  setFiltrosArchivoAplicados({ ...filtrosArchivo });
                }}
              >
                <Row className="g-2">
                  <Col md={3}>
                    <Form.Label className="small mb-1">Nombre</Form.Label>
                    <Form.Control
                      size="sm"
                      placeholder="Subcadena del nombre"
                      value={filtrosArchivo.nombre}
                      onChange={(evento) =>
                        setFiltrosArchivo({ ...filtrosArchivo, nombre: evento.target.value })
                      }
                    />
                  </Col>

                  <Col md={2}>
                    <Form.Label className="small mb-1">Tipo</Form.Label>
                    <Form.Control
                      size="sm"
                      placeholder=".pdf"
                      value={filtrosArchivo.extension}
                      onChange={(evento) =>
                        setFiltrosArchivo({ ...filtrosArchivo, extension: evento.target.value })
                      }
                    />
                  </Col>

                  <Col md={2}>
                    <Form.Label className="small mb-1">Propietario</Form.Label>
                    {/*
                      El filtro es por el NOMBRE del propietario y no por su identificador.
                      Buscar por número era inusable: a escala nadie recuerda a qué usuario
                      corresponde cada id. Tampoco se ofrece una lista de usuarios porque el
                      listado de usuarios es exclusivo de administradores y esta pantalla la
                      usa cualquiera; escribir parte del nombre funciona para todos.
                    */}
                    <Form.Control
                      size="sm"
                      placeholder="Nombre del usuario"
                      value={filtrosArchivo.propietario}
                      onChange={(evento) =>
                        setFiltrosArchivo({ ...filtrosArchivo, propietario: evento.target.value })
                      }
                    />
                  </Col>

                  <Col md={2}>
                    <Form.Label className="small mb-1">Subido desde</Form.Label>
                    {/* El input de tipo date entrega AAAA-MM-DD, que es el formato
                        que el backend acepta para las fechas. */}
                    <Form.Control
                      size="sm"
                      type="date"
                      value={filtrosArchivo.subidoDesde}
                      onChange={(evento) =>
                        setFiltrosArchivo({ ...filtrosArchivo, subidoDesde: evento.target.value })
                      }
                    />
                  </Col>

                  <Col md={2}>
                    <Form.Label className="small mb-1">Subido hasta</Form.Label>
                    <Form.Control
                      size="sm"
                      type="date"
                      value={filtrosArchivo.subidoHasta}
                      onChange={(evento) =>
                        setFiltrosArchivo({ ...filtrosArchivo, subidoHasta: evento.target.value })
                      }
                    />
                  </Col>

                  <Col md={1} className="d-flex align-items-end">
                    <Button size="sm" variant="outline-secondary" type="submit" className="w-100">
                      Buscar
                    </Button>
                  </Col>
                </Row>

                <Button
                  size="sm"
                  variant="link"
                  className="p-0 mt-1 text-decoration-none"
                  type="button"
                  onClick={() => {
                    const vacios = { nombre: '', extension: '', propietario: '', subidoDesde: '', subidoHasta: '' };
                    setFiltrosArchivo(vacios);
                    setFiltrosArchivoAplicados(vacios);
                    setPaginaArchivos(1);
                  }}
                >
                  Limpiar filtros
                </Button>
              </Form>

              <TablaArchivos
                archivos={archivos}
                usuario={usuario}
                onRenombrar={(archivo) => setModalRenombrar({ mostrar: true, elemento: archivo, tipo: 'archivo' })}
                onEliminar={(archivo) => setModalEliminar({ mostrar: true, elemento: archivo, tipo: 'archivo' })}
              />

              <Paginacion
                pagina={paginaArchivos}
                totalRegistros={totalArchivos}
                tamanoPagina={tamanoArchivos}
                onCambiarPagina={setPaginaArchivos}
                onCambiarTamano={setTamanoArchivos}
              />
            </>
          )}
        </Card.Body>
      </Card>

      {/* ------------------------- Ventanas ------------------------- */}

      {/* V6: crear carpeta. Se crea en la carpeta en la que está el usuario. */}
      <ModalCarpeta
        mostrar={modalCarpeta}
        carpetaPadre={carpetaActual}
        onCerrar={() => setModalCarpeta(false)}
        onCreado={recargar}
      />

      {/* V5: subir archivo. El destino es la carpeta actual y no se puede cambiar. */}
      <ModalSubir
        mostrar={modalSubir}
        directorio={carpetaActual}
        onCerrar={() => setModalSubir(false)}
        onSubido={recargar}
      />

      {/* V7: renombrar. Sirve tanto para archivos como para carpetas. */}
      <ModalRenombrar
        mostrar={modalRenombrar.mostrar}
        elemento={modalRenombrar.elemento}
        tipo={modalRenombrar.tipo}
        onCerrar={() => setModalRenombrar({ mostrar: false, elemento: null, tipo: 'archivo' })}
        onRenombrado={recargar}
      />

      {/* V8: confirmar la eliminación, con aviso de borrado en cascada si aplica. */}
      <ModalConfirmar
        mostrar={modalEliminar.mostrar}
        titulo={modalEliminar.tipo === 'archivo' ? 'Eliminar archivo' : 'Eliminar carpeta'}
        mensaje={`¿Seguro que quieres eliminar "${
          modalEliminar.elemento?.nombreCompleto || modalEliminar.elemento?.nombre || ''
        }"? Esta acción no se puede deshacer.`}
        aviso={avisoDeEliminacion()}
        onConfirmar={confirmarEliminacion}
        onCerrar={() => setModalEliminar({ mostrar: false, elemento: null, tipo: 'archivo' })}
      />
    </>
  );
}

export default Repositorio;
