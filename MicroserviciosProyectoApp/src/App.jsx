import { useCallback, useEffect, useState } from 'react';
import api from './utils/api';
import Login from './Login';
import Usuarios from './Usuarios';
import Archivos from './Archivos';

export default function App() {
  const [usuario, setUsuario] = useState(null);
  const [equipos, setEquipos] = useState([]);
  const [equipoId, setEquipoId] = useState('');
  const [vista, setVista] = useState('archivos');
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(() => Boolean(localStorage.getItem('access_token')));
  const informar = useCallback(e => {
    if (e.code === 'ERR_CANCELED') return;
    const d = e.response?.data;
    setError(typeof d === 'string' ? d : Object.values(d?.errors || {}).flat().join(' ') || (e.response?.status === 403 ? 'No tienes permiso para esta acción.' : 'No se pudo completar la operación.'));
  }, []);
  const cargar = useCallback(async () => {
    try {
      const [{ data: yo }, { data: lista }] = await Promise.all([api.get('/autenticacion/yo'), api.get('/equipos')]);
      setUsuario(yo); setEquipos(lista); setEquipoId(String(yo.equipoId)); setError('');
    } catch (e) {
      localStorage.removeItem('access_token'); localStorage.removeItem('refresh_token'); setUsuario(null); informar(e);
    } finally { setCargando(false); }
  }, [informar]);
  useEffect(() => {
    if (localStorage.getItem('access_token')) {
      cargar();
    }
    const cerrar = () => setUsuario(null);
    window.addEventListener('sesion-cerrada', cerrar);
    return () => window.removeEventListener('sesion-cerrada', cerrar);
  }, [cargar]);
  async function salir() {
    try { await api.post('/autenticacion/cerrar-sesion'); } catch { }
    localStorage.removeItem('access_token'); localStorage.removeItem('refresh_token'); setUsuario(null);
  }
  async function crearEquipo(event) {
    event.preventDefault();
    const form = event.currentTarget;
    try {
      const { data } = await api.post('/equipos', { nombre: new FormData(form).get('nombre') });
      setEquipos((await api.get('/equipos')).data); setEquipoId(String(data.id)); form.reset();
    } catch (e) { informar(e); }
  }
  if (cargando) return <p className="container py-5">Cargando sesión…</p>;
  if (!usuario) return <Login onLogin={cargar} />;
  return <>
    <header className="navbar navbar-dark bg-dark px-4 gap-3">
      <span className="navbar-brand">Drive por equipos</span>
      <span className="text-white">{usuario.nombre} · {usuario.nivelAdmin ? `Administrador nivel ${usuario.nivelAdmin}` : 'Usuario'}</span>
      <button className="btn btn-outline-light" onClick={salir}>Cerrar sesión</button>
    </header>
    <main className="container py-4">
      <div className="d-flex align-items-end gap-3 flex-wrap mb-3">
        <label>Equipo<select className="form-select" value={equipoId} disabled={usuario.nivelAdmin !== 2} onChange={e => { setEquipoId(e.target.value); setError(''); }}>
          {equipos.map(e => <option key={e.id} value={e.id}>{e.nombre}</option>)}
        </select></label>
        <nav className="d-flex gap-2"><button className={`btn btn-${vista === 'archivos' ? 'primary' : 'outline-primary'}`} onClick={() => setVista('archivos')}>Archivos</button>
          {usuario.esAdmin && <button className={`btn btn-${vista === 'usuarios' ? 'primary' : 'outline-primary'}`} onClick={() => setVista('usuarios')}>Usuarios</button>}
          {usuario.nivelAdmin === 2 && <button className={`btn btn-${vista === 'equipos' ? 'primary' : 'outline-primary'}`} onClick={() => setVista('equipos')}>Equipos</button>}
        </nav>
      </div>
      {error && <div role="alert" className="alert alert-danger d-flex justify-content-between">{error}<button aria-label="Cerrar error" className="btn-close" onClick={() => setError('')} /></div>}
      {vista === 'equipos' && usuario.nivelAdmin === 2 ? <section><h1 className="h4">Administrar equipos</h1>
        <form onSubmit={crearEquipo} className="d-flex gap-2 mb-4"><input aria-label="Nombre del nuevo equipo" name="nombre" className="form-control" placeholder="Nombre del nuevo equipo" required maxLength={100} /><button className="btn btn-primary">Crear equipo</button></form>
        {equipos.map(e => <div className="card card-body mb-2" key={e.id}><strong>{e.nombre}</strong><button className="btn btn-link text-start" onClick={async () => {
          const nombre = window.prompt('Nuevo nombre del equipo', e.nombre); if (!nombre?.trim()) return;
          try { await api.put(`/equipos/${e.id}`, { nombre }); setEquipos((await api.get('/equipos')).data); } catch (err) { informar(err); }
        }}>Cambiar nombre</button></div>)}
      </section> : vista === 'usuarios' && usuario.esAdmin ? <Usuarios key={equipoId} usuario={usuario} equipoId={Number(equipoId)} equipos={equipos} informar={informar} />
        : <Archivos key={equipoId} usuario={usuario} equipoId={Number(equipoId)} equipos={equipos} informar={informar} />}
    </main>
  </>;
}
