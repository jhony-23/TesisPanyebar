import { lazy, Suspense } from 'react'
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
import PagosPage from './pages/PagosPage.jsx'
import JornadasPage from './pages/JornadasPage.jsx'
import PersonasPage from './pages/PersonasPage.jsx'
import CuotasPage from './pages/CuotasPage.jsx'
import FinanzasPage from './pages/FinanzasPage.jsx'
import DashboardPage from './pages/DashboardPage.jsx'
import ReportesPage from './pages/ReportesPage.jsx'
import AbastecimientoPage from './pages/AbastecimientoPage.jsx'
import SectoresPage from './pages/SectoresPage.jsx'
import SolicitudesNuevoServicioPage from './pages/SolicitudesNuevoServicioPage.jsx'
import SuministrosPage from './pages/SuministrosPage.jsx'
import SuministroQrPage from './pages/SuministroQrPage.jsx'
const AdministracionPage = lazy(
  () => import('./pages/AdministracionPage'),
)

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<PublicLayout />}>
            <Route element={<HomePage />} path="/" />
            <Route element={<SuministroQrPage />} path="/suministro/qr/:token" />
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
              <Route element={<PagosPage />} path="/admin/pagos" />
              <Route element={<FinanzasPage />} path="/admin/finanzas" />
              <Route element={<DashboardPage />} path="/admin/dashboard" />
              <Route element={<ReportesPage />} path="/admin/reportes" />
              <Route element={<JornadasPage />} path="/admin/jornadas" />
              <Route element={<AbastecimientoPage />} path="/admin/abastecimiento" />
        <Route
          element={
            <ProtectedRoute>
              <Suspense fallback={<AdministracionLoading />}>
                <AdministracionPage />
              </Suspense>
            </ProtectedRoute>
          }
          path="/admin/administracion"
        />
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

function AdministracionLoading() {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-6 py-14 text-center shadow-sm">
      <div className="mx-auto h-7 w-7 animate-spin rounded-full border-2 border-slate-200 border-t-[#28727a]" />
      <p className="mt-4 text-sm text-slate-500">
        Cargando administración...
      </p>
    </div>
  )
}
export default App
