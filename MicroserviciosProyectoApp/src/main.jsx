import { createRoot } from 'react-dom/client'
import Login from './Login.jsx'
import Usuarios from './Usuarios.jsx';
import { BrowserRouter, Routes, Route } from "react-router"
import MainLayout from './MainLayout.jsx';

import 'bootstrap/dist/css/bootstrap.min.css';

createRoot(document.getElementById('root')).render(
  <BrowserRouter>
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route element={<MainLayout />}>
        <Route path="/usuarios" element={<Usuarios />} />
      </Route>
    </Routes>
  </BrowserRouter>
)
