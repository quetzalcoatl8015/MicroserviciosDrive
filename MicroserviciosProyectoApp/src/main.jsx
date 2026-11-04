import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import App from './App.jsx'

// Estilos de Bootstrap. Se importan una sola vez, en el punto de entrada.
import 'bootstrap/dist/css/bootstrap.min.css';

/**
 * Punto de entrada de la aplicación.
 *
 * Este archivo se limita a dos cosas:
 *   1. Montar el enrutador del navegador (BrowserRouter).
 *   2. Dibujar App, que es quien declara las rutas y la guardia de sesión.
 *
 * Antes aquí mismo se declaraban las rutas, y solo había dos (/login y /usuarios):
 * faltaba la ruta raíz y la pantalla del repositorio, que es la principal del sistema.
 */
createRoot(document.getElementById('root')).render(
  <BrowserRouter>
    <App />
  </BrowserRouter>
)
