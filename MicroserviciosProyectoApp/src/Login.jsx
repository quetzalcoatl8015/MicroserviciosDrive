import { useState } from 'react';
import api from './utils/api';

export default function Login({ onLogin }) {
  const [registro, setRegistro] = useState(false);
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');
  const [ocupado, setOcupado] = useState(false);
  async function enviar(event) {
    event.preventDefault();
    const datos = Object.fromEntries(new FormData(event.currentTarget));
    setError(''); setAviso(''); setOcupado(true);
    try {
      if (registro) {
        await api.post('/autenticacion/registro', datos);
        setAviso('Cuenta registrada. Un administrador debe activarla antes de que puedas ingresar.');
        setRegistro(false);
      } else {
        const { data } = await api.post('/autenticacion/iniciar-sesion', datos);
        localStorage.setItem('access_token', data.tokenAcceso);
        localStorage.setItem('refresh_token', data.tokenRefresco);
        await onLogin();
      }
    } catch (e) {
      const data = e.response?.data;
      setError(typeof data === 'string' ? data : Object.values(data?.errors || {}).flat().join(' ') || 'No se pudo conectar con la API.');
    } finally { setOcupado(false); }
  }
  return <main className="container py-5" style={{ maxWidth: 480 }}>
    <h1>Drive por equipos</h1><p className="text-secondary">Archivos y colaboración de tu equipo</p>
    {error && <div role="alert" className="alert alert-danger">{error}</div>}
    {aviso && <div role="status" className="alert alert-success">{aviso}</div>}
    <form onSubmit={enviar} className="card card-body gap-3">
      <h2 className="h5">{registro ? 'Crear cuenta' : 'Iniciar sesión'}</h2>
      {registro && <label>Nombre<input className="form-control" name="nombre" required maxLength={100} /></label>}
      <label>Correo<input className="form-control" name="correo" type="email" autoComplete="username" required /></label>
      <label>Contraseña<input className="form-control" name="contrasena" type="password" autoComplete={registro ? 'new-password' : 'current-password'} required /></label>
      {registro && <>
        <label>Confirmar contraseña<input className="form-control" name="confirmarContrasena" type="password" required /></label>
        <label>Fecha de nacimiento<input className="form-control" name="fechaNacimiento" type="date" required /></label>
        <small className="text-secondary">Tu cuenta quedará inactiva hasta que un administrador la active.</small>
      </>}
      <button className="btn btn-primary" disabled={ocupado}>{registro ? 'Registrarme' : 'Ingresar'}</button>
      <button className="btn btn-link" type="button" onClick={() => { setRegistro(!registro); setError(''); }}>{registro ? 'Ya tengo cuenta' : 'Crear una cuenta'}</button>
    </form>
  </main>;
}
