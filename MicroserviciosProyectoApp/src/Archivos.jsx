import { useEffect, useState } from 'react';
import Modal from 'react-bootstrap/Modal';
import api from './utils/api';

export default function Archivos({ usuario, equipoId, equipos, informar }) {
  const [ruta, setRuta] = useState([]);
  const [compartidos, setCompartidos] = useState(false);
  const [carpetas, setCarpetas] = useState([]);
  const [archivos, setArchivos] = useState([]);
  const [recibidos, setRecibidos] = useState([]);
  const [filtros, setFiltros] = useState({ nombre: '', extension: '', subidoDesde: '', subidoHasta: '' });
  const [pagina, setPagina] = useState(1);
  const [paginas, setPaginas] = useState(1);
  const [revision, setRevision] = useState(0);
  const [cargando, setCargando] = useState(false);
  const [ocupado, setOcupado] = useState(false);
  const [recurso, setRecurso] = useState(null);
  const actual = ruta.at(-1);
  const soloLectura = actual && usuario.nivelAdmin !== 2 && actual.equipoId !== usuario.equipoId;
  useEffect(() => {
    const controller = new AbortController();
    async function cargar() {
      setCargando(true);
      try {
        if (compartidos && !actual) {
          const { data } = await api.get('/compartidos', { params: { equipoId }, signal: controller.signal });
          setRecibidos(data); setCarpetas([]); setArchivos([]); setPaginas(1);
        } else {
          const params = { equipoId, padreId: actual?.id, pagina, tamanoPagina: 25, nombre: filtros.nombre };
          const [dirs, files] = await Promise.all([
            api.get('/directorios', { params, signal: controller.signal }),
            actual ? api.get('/archivos', { params: { directorioId: actual.id, pagina, tamanoPagina: 25, ...Object.fromEntries(Object.entries(filtros).filter(([, v]) => v)) }, signal: controller.signal }) : Promise.resolve({ data: { elementos: [], totalPaginas: 1 } }),
          ]);
          setCarpetas(dirs.data.elementos); setArchivos(files.data.elementos);
          setPaginas(Math.max(1, dirs.data.totalPaginas, files.data.totalPaginas));
        }
      } catch (e) { informar(e); }
      finally { if (!controller.signal.aborted) setCargando(false); }
    }
    cargar();
    return () => controller.abort();
  }, [actual, compartidos, equipoId, filtros, pagina, revision, informar]);
  async function accion(fn) {
    setOcupado(true);
    try { await fn(); setRevision(n => n + 1); } catch (e) { informar(e); }
    finally { setOcupado(false); }
  }
  function entrar(d) { setRuta([...ruta, d]); setPagina(1); }
  function inicio(shared) { setCompartidos(shared); setRuta([]); setPagina(1); }
  const puedeEditar = item => usuario.nivelAdmin === 2 || (item.equipoId === usuario.equipoId && (usuario.esAdmin || item.propietarioId === usuario.id));
  async function descargar(id) {
    await accion(async () => {
      const [{ data: meta }, { data }] = await Promise.all([api.get(`/archivos/${id}`), api.get(`/archivos/${id}/descargar`, { responseType: 'blob' })]);
      const url = URL.createObjectURL(data);
      const a = document.createElement('a'); a.href = url; a.download = meta.nombreCompleto; a.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    });
  }
  function botones(item, tipo) {
    const editable = puedeEditar(item);
    return <div className="d-flex flex-wrap gap-1">
      {tipo === 'archivos' && <button disabled={ocupado} className="btn btn-sm btn-outline-primary" onClick={() => descargar(item.id)}>Descargar</button>}
      {editable && <>
        <button disabled={ocupado} className="btn btn-sm btn-outline-secondary" onClick={() => {
          const nombre = window.prompt(tipo === 'archivos' ? `Nuevo nombre (se conserva ${item.extension || 'la extensión'})` : 'Nuevo nombre', item.nombre);
          if (nombre?.trim()) accion(() => api.patch(`/${tipo}/${item.id}`, { nombre }));
        }}>Renombrar</button>
        <button disabled={ocupado || (tipo === 'directorios' && !usuario.esAdmin && !item.estaVacio)} className="btn btn-sm btn-outline-danger" onClick={() => {
          if (window.confirm(`¿Eliminar ${item.nombre}?${tipo === 'directorios' && !item.estaVacio ? ' Se eliminará todo su contenido.' : ''}`)) accion(() => api.delete(`/${tipo}/${item.id}`));
        }}>Eliminar</button>
      </>}
      {usuario.nivelAdmin === 2 && <button className="btn btn-sm btn-outline-success" onClick={() => setRecurso({ ...item, tipo })}>Compartir</button>}
    </div>;
  }
  return <section>
    <div className="d-flex gap-2 mb-3">
      <button className={`btn btn-${!compartidos ? 'secondary' : 'outline-secondary'}`} onClick={() => inicio(false)}>Contenido del equipo</button>
      <button className={`btn btn-${compartidos ? 'secondary' : 'outline-secondary'}`} onClick={() => inicio(true)}>Compartido con este equipo</button>
    </div>
    <nav aria-label="Ruta de carpetas" className="mb-3">
      <button className="btn btn-link ps-0" onClick={() => { setRuta([]); setPagina(1); }}>{compartidos ? 'Compartidos' : 'Raíz'}</button>
      {ruta.map((d, i) => <span key={d.id}> / <button className="btn btn-link" onClick={() => { setRuta(ruta.slice(0, i + 1)); setPagina(1); }}>{d.nombre}</button></span>)}
    </nav>
    {soloLectura && <div className="alert alert-info">Contenido compartido de otro equipo. Acceso de solo lectura.</div>}
    {(!compartidos || actual) && <>
      {!soloLectura && <div className="d-flex gap-3 flex-wrap mb-3">
        <form className="d-flex gap-2" onSubmit={e => {
          e.preventDefault(); const form = e.currentTarget; const nombre = new FormData(form).get('nombre');
          accion(async () => { await api.post('/directorios', { nombre, padreId: actual?.id, equipoId: actual?.equipoId || equipoId }); form.reset(); });
        }}>
          <input className="form-control" aria-label="Nombre de nueva carpeta" name="nombre" placeholder="Nueva carpeta" required maxLength={200} /><button disabled={ocupado} className="btn btn-primary text-nowrap">Crear carpeta</button>
        </form>
        {actual ? <form className="d-flex gap-2" onSubmit={e => {
          e.preventDefault(); const form = e.currentTarget; const datos = new FormData(form); const file = datos.get('archivo');
          if (file.size > 10 * 1024 * 1024) { informar({ response: { data: 'El archivo supera los 10 MB.' } }); return; }
          datos.append('directorioId', actual.id);
          accion(async () => { await api.post('/archivos', datos); form.reset(); });
        }}><input aria-label="Archivo para subir" className="form-control" type="file" name="archivo" required /><button disabled={ocupado} className="btn btn-success text-nowrap">Subir archivo</button></form>
          : <p className="small text-secondary align-self-center mb-0">Abre o crea una carpeta para subir archivos (máximo 10 MB).</p>}
      </div>}
      <div className="row g-2 mb-3">
        <label className="col-md-3">Buscar por nombre<input className="form-control" value={filtros.nombre} onChange={e => { setFiltros({ ...filtros, nombre: e.target.value }); setPagina(1); }} /></label>
        {actual && <>
          <label className="col-md-3">Extensión<input className="form-control" placeholder=".pdf" value={filtros.extension} onChange={e => { setFiltros({ ...filtros, extension: e.target.value }); setPagina(1); }} /></label>
          {['subidoDesde', 'subidoHasta'].map((key, i) => <label key={key} className="col-md-3">{i ? 'Subido hasta' : 'Subido desde'}<input className="form-control" type="date" value={filtros[key]} onChange={e => { setFiltros({ ...filtros, [key]: e.target.value }); setPagina(1); }} /></label>)}
        </>}
      </div>
    </>}
    {cargando ? <p role="status">Cargando contenido…</p> : compartidos && !actual ? <>
      <h1 className="h4">Compartido con este equipo</h1>
      {recibidos.length === 0 && <p>Aún no hay archivos ni carpetas compartidos.</p>}
      {recibidos.map(c => <div key={c.id} className="card card-body mb-2 d-flex flex-row gap-3 justify-content-between align-items-center">
        <div><strong>{c.directorioId ? 'Carpeta: ' : 'Archivo: '}{c.nombre}</strong><p className="mb-0 small text-secondary">De {c.equipoOrigen} · Solo lectura</p></div>
        <div className="d-flex gap-2"><button className="btn btn-outline-primary" onClick={() => c.directorioId ? accion(async () => entrar((await api.get(`/directorios/${c.directorioId}`)).data)) : descargar(c.archivoId)}>{c.directorioId ? 'Abrir carpeta' : 'Descargar'}</button>
          {usuario.nivelAdmin === 2 && <button className="btn btn-outline-danger" onClick={() => { if (window.confirm('¿Revocar este acceso compartido?')) accion(() => api.delete(`/compartidos/${c.id}`)); }}>Revocar</button>}
        </div>
      </div>)}
    </> : <>
      <h1 className="h4">{actual?.nombre || 'Carpetas del equipo'}</h1>
      <div className="table-responsive"><table className="table align-middle"><thead><tr><th>Nombre</th><th>Creado por</th><th>Contenido / tamaño</th><th>Acciones</th></tr></thead><tbody>
        {carpetas.map(d => <tr key={`d${d.id}`}><td><button className="btn btn-link p-0" onClick={() => entrar(d)}>📁 {d.nombre}</button></td><td>{d.propietario}</td><td>{d.cantidadSubdirectorios} carpetas, {d.cantidadArchivos} archivos</td><td>{botones(d, 'directorios')}</td></tr>)}
        {archivos.map(a => <tr key={`a${a.id}`}><td>{a.nombreCompleto}<small className="d-block text-secondary">{new Date(a.creadoEn).toLocaleString()}</small></td><td>{a.propietario}</td><td>{(a.tamanoBytes / 1024).toFixed(1)} KB</td><td>{botones(a, 'archivos')}</td></tr>)}
        {!carpetas.length && !archivos.length && <tr><td colSpan={4}>No hay contenido en esta página.</td></tr>}
      </tbody></table></div>
      <div className="d-flex align-items-center gap-3"><button className="btn btn-outline-secondary" disabled={pagina <= 1} onClick={() => setPagina(pagina - 1)}>Anterior</button><span>Página {pagina} de {paginas}</span><button className="btn btn-outline-secondary" disabled={pagina >= paginas} onClick={() => setPagina(pagina + 1)}>Siguiente</button></div>
    </>}
    {recurso && <Compartir key={`${recurso.tipo}${recurso.id}`} recurso={recurso} equipos={equipos} cerrar={() => setRecurso(null)} informar={informar} />}
  </section>;
}

function Compartir({ recurso, equipos, cerrar, informar }) {
  const [accesos, setAccesos] = useState([]);
  const [revision, setRevision] = useState(0);
  const [ocupado, setOcupado] = useState(false);
  const [errorCompartir, setErrorCompartir] = useState('');
  const campo = recurso.tipo === 'archivos' ? 'archivoId' : 'directorioId';
  useEffect(() => {
    const controller = new AbortController();
    api.get('/compartidos/recurso', { params: { [campo]: recurso.id }, signal: controller.signal }).then(r => setAccesos(r.data)).catch(informar);
    return () => controller.abort();
  }, [campo, recurso.id, revision, informar]);
  async function operar(fn) {
    setOcupado(true);
    setErrorCompartir('');
    try { await fn(); setRevision(n => n + 1); } catch (e) {
      const d = e.response?.data;
      setErrorCompartir(typeof d === 'string' ? d : 'No se pudo modificar el acceso compartido.');
    }
    finally { setOcupado(false); }
  }
  const destinos = equipos.filter(e => e.id !== recurso.equipoId && !accesos.some(a => a.equipoDestinoId === e.id));
  return <Modal show onHide={cerrar} centered><Modal.Header closeButton><Modal.Title>Compartir {recurso.nombre}</Modal.Title></Modal.Header><Modal.Body>
    {errorCompartir && <div role="alert" className="alert alert-danger">{errorCompartir}</div>}
    <p>Se concede lectura y descarga. Compartir una carpeta incluye sus subcarpetas y archivos actuales y futuros.</p>
    {destinos.length ? <form className="d-flex gap-2 mb-3" onSubmit={e => {
      e.preventDefault(); const equipoDestinoId = Number(new FormData(e.currentTarget).get('equipo'));
      operar(() => api.post('/compartidos', { [campo]: recurso.id, equipoDestinoId }));
    }}><select aria-label="Equipo destino" className="form-select" name="equipo">{destinos.map(e => <option key={e.id} value={e.id}>{e.nombre}</option>)}</select><button disabled={ocupado} className="btn btn-primary">Compartir</button></form> : <p>No hay más equipos disponibles.</p>}
    {accesos.map(a => <div className="d-flex justify-content-between align-items-center mb-2" key={a.id}><span>{a.equipoDestino}</span><button disabled={ocupado} className="btn btn-sm btn-outline-danger" onClick={() => operar(() => api.delete(`/compartidos/${a.id}`))}>Revocar</button></div>)}
    {!accesos.length && <p className="small text-secondary">Sin accesos compartidos directos. Puede existir acceso heredado de una carpeta superior.</p>}
  </Modal.Body></Modal>;
}
