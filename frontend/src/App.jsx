import { BrowserRouter, Link, Outlet, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './app/AuthContext.jsx'
import AdminLayout from './components/AdminLayout.jsx'
import ProtectedRoute from './components/ProtectedRoute.jsx'
import PublicOnlyRoute from './components/PublicOnlyRoute.jsx'
import AdminPage from './pages/AdminPage.jsx'
import HomePage from './pages/HomePage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import NotFoundPage from './pages/NotFoundPage.jsx'
import ObligacionesPage from './pages/ObligacionesPage.jsx'
import JornadasPage from './pages/JornadasPage.jsx'
import PersonasPage from './pages/PersonasPage.jsx'
import CuotasPage from './pages/CuotasPage.jsx'
import SectoresPage from './pages/SectoresPage.jsx'
import SolicitudesNuevoServicioPage from './pages/SolicitudesNuevoServicioPage.jsx'
import SuministrosPage from './pages/SuministrosPage.jsx'

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<PublicLayout />}>
            <Route element={<HomePage />} path="/" />
            <Route element={<PublicOnlyRoute />}>
              <Route element={<LoginPage />} path="/login" />
            </Route>
          </Route>
          <Route element={<ProtectedRoute />}>
            <Route element={<AdminLayout />}>
              <Route element={<AdminPage />} path="/admin" />
              <Route element={<PersonasPage />} path="/admin/personas" />
              <Route element={<SectoresPage />} path="/admin/sectores" />
              <Route element={<SuministrosPage />} path="/admin/suministros" />
              <Route element={<SolicitudesNuevoServicioPage />} path="/admin/solicitudes" />
              <Route element={<CuotasPage />} path="/admin/cuotas" />
              <Route element={<ObligacionesPage />} path="/admin/obligaciones" />
              <Route element={<JornadasPage />} path="/admin/jornadas" />
            </Route>
          </Route>
          <Route element={<NotFoundPage />} path="*" />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}

function PublicLayout() {
  return (
    <>
      <header className="border-b border-slate-200 bg-white">
        <nav className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Link className="font-semibold text-slate-900" to="/">Panyebar</Link>
          <Link className="text-sm text-slate-600 hover:text-slate-900" to="/login">Acceso administrativo</Link>
        </nav>
      </header>
      <Outlet />
    </>
  )
}

export default App
