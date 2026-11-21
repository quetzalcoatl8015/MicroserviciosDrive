import { useEffect, useState } from 'react';
import Modal from 'react-bootstrap/Modal';
import api from './utils/api';

export default function Usuarios({ usuario, equipoId, equipos, informar }) {
  const [usuarios, setUsuarios] = useState([]);
  const [pagina, setPagina] = useState(1);
  const [paginas, setPaginas] = useState(1);
  const [buscar, setBuscar] = useState('');
  const [revision, setRevision] = useState(0);
  const [editar, setEditar] = useState(null);
  const [ocupado, setOcupado] = useState(false);
  const [errorFormulario, setErrorFormulario] = useState('');
  useEffect(() => {
    const controller = new AbortController();
    api.get('/usuarios', { params: { equipoId, nombre: buscar, pagina, tamanoPagina: 25 }, signal: controller.signal })
      .then(({ data }) => { setUsuarios(data.elementos); setPaginas(Math.max(1, data.totalPaginas)); }).catch(informar);
    return () => controller.abort();
  }, [equipoId, buscar, pagina, revision, informar]);
  async function operar(fn) {
    setOcupado(true);
    try { await fn(); setRevision(n => n + 1); } catch (e) {
      if (editar) {
        const d = e.response?.data;
        setErrorFormulario(typeof d === 'string' ? d : Object.values(d?.errors || {}).flat().join(' ') || 'No se pudo guardar el usuario.');
      } else informar(e);
    }
    finally { setOcupado(false); }
  }
  async function guardar(event) {
    event.preventDefault();
    const datos = Object.fromEntries(new FormData(event.currentTarget));
    datos.nivelAdmin = Number(datos.nivelAdmin ?? editar.nivelAdmin ?? 0);
    datos.esAdmin = datos.nivelAdmin > 0;
    datos.equipoId = Number(datos.equipoId ?? editar.equipoId ?? equipoId);
    if (!datos.contrasena) { delete datos.contrasena; delete datos.confirmarContrasena; }
    await operar(async () => {
      if (editar.id) await api.put(`/usuarios/${editar.id}`, datos); else await api.post('/usuarios', datos);
      setEditar(null);
    });
  }
  return <section>
    <div className="d-flex gap-3 justify-content-between align-items-center mb-3"><h1 className="h4">Usuarios del equipo</h1><button className="btn btn-primary" onClick={() => { setErrorFormulario(''); setEditar({ equipoId, nivelAdmin: 0 }); }}>Crear usuario</button></div>
    <label className="mb-3">Buscar por nombre<input className="form-control" value={buscar} onChange={e => { setBuscar(e.target.value); setPagina(1); }} /></label>
    <div className="table-responsive"><table className="table align-middle"><thead><tr><th>Nombre</th><th>Correo</th><th>Rol</th><th>Equipo</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>
      {usuarios.map(u => {
        const gestionable = usuario.nivelAdmin === 2 || u.nivelAdmin < 2;
        return <tr key={u.id}><td>{u.nombre}</td><td>{u.correo}</td><td>{u.nivelAdmin ? `Administrador nivel ${u.nivelAdmin}` : 'Usuario'}</td><td>{u.equipo || 'Sin equipo'}</td><td>{u.estaActivo ? 'Activo' : 'Pendiente / inactivo'}</td><td><div className="d-flex flex-wrap gap-1">
          <button className="btn btn-sm btn-outline-primary" disabled={!gestionable || ocupado} onClick={() => { setErrorFormulario(''); setEditar(u); }}>Editar</button>
          <button className="btn btn-sm btn-outline-secondary" disabled={!gestionable || ocupado || u.id === usuario.id} onClick={() => operar(() => api.patch(`/usuarios/${u.id}/estado`, u.estaActivo ? { estaActivo: false } : { estaActivo: true, equipoId: u.equipoId ?? equipoId }))}>{u.estaActivo ? 'Desactivar' : 'Activar'}</button>
          <button className="btn btn-sm btn-outline-warning" disabled={!gestionable || ocupado || !u.tieneTokenSesion || u.id === usuario.id} onClick={() => operar(() => api.delete(`/usuarios/${u.id}/token-sesion`))}>Revocar refresco</button>
          <button className="btn btn-sm btn-outline-danger" disabled={!gestionable || ocupado || u.id === usuario.id} onClick={() => {
            if (window.confirm(`¿Eliminar la cuenta de ${u.nombre}?`)) operar(() => api.delete(`/usuarios/${u.id}`));
          }}>Eliminar</button>
        </div></td></tr>;
      })}
      {!usuarios.length && <tr><td colSpan={6}>No hay usuarios que coincidan.</td></tr>}
    </tbody></table></div>
    <div className="d-flex gap-3 align-items-center"><button className="btn btn-outline-secondary" disabled={pagina <= 1} onClick={() => setPagina(pagina - 1)}>Anterior</button><span>Página {pagina} de {paginas}</span><button className="btn btn-outline-secondary" disabled={pagina >= paginas} onClick={() => setPagina(pagina + 1)}>Siguiente</button></div>
    {editar && <Modal show onHide={() => setEditar(null)} centered><Modal.Header closeButton><Modal.Title>{editar.id ? 'Editar usuario' : 'Crear usuario'}</Modal.Title></Modal.Header><Modal.Body>
      {errorFormulario && <div role="alert" className="alert alert-danger">{errorFormulario}</div>}
      <form className="d-grid gap-3" onSubmit={guardar}>
        <label>Nombre<input className="form-control" name="nombre" required maxLength={100} defaultValue={editar.nombre || ''} /></label>
        <label>Correo<input className="form-control" name="correo" type="email" required defaultValue={editar.correo || ''} /></label>
        <label>Fecha de nacimiento<input className="form-control" name="fechaNacimiento" type="date" required defaultValue={editar.fechaNacimiento || ''} /></label>
        <label>Contraseña {editar.id ? '(opcional)' : ''}<input className="form-control" name="contrasena" type="password" autoComplete="new-password" required={!editar.id} /></label>
        <label>Confirmar contraseña<input className="form-control" name="confirmarContrasena" type="password" autoComplete="new-password" required={!editar.id} /></label>
        <label>Rol<select className="form-select" name="nivelAdmin" defaultValue={editar.nivelAdmin} disabled={editar.id === usuario.id}>
          <option value={0}>Usuario</option><option value={1}>Administrador nivel 1</option>{usuario.nivelAdmin === 2 && <option value={2}>Administrador nivel 2</option>}
        </select></label>
        {usuario.nivelAdmin === 2 && <label>Equipo<select className="form-select" name="equipoId" defaultValue={editar.equipoId} disabled={editar.id === usuario.id}>
          {equipos.map(e => <option key={e.id} value={e.id}>{e.nombre}</option>)}
        </select><small>El contenido creado antes del cambio permanece en su equipo original.</small></label>}
        <button className="btn btn-primary" disabled={ocupado}>Guardar</button>
      </form>
    </Modal.Body></Modal>}
  </section>;
}
