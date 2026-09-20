import { lazy, Suspense } from 'react'
import { BrowserRouter, Link, Outlet, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './app/AuthContext.jsx'
import AdminLayout from './components/AdminLayout.jsx'
import ProtectedRoute from './components/ProtectedRoute.jsx'
import PublicOnlyRoute from './components/PublicOnlyRoute.jsx'
import PermissionRoute from './components/PermissionRoute.jsx'
import { ADMIN_MODULE_PERMISSIONS, PERMISOS } from './app/permissions.js'
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

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.PERSONAS_VER, PERMISOS.PERSONAS_GESTIONAR]}>
                    <PersonasPage />
                  </PermissionRoute>
                }
                path="/admin/personas"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.SECTORES_VER, PERMISOS.SECTORES_GESTIONAR]}>
                    <SectoresPage />
                  </PermissionRoute>
                }
                path="/admin/sectores"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.SUMINISTROS_VER, PERMISOS.SUMINISTROS_GESTIONAR]}>
                    <SuministrosPage />
                  </PermissionRoute>
                }
                path="/admin/suministros"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.SUMINISTROS_VER, PERMISOS.SUMINISTROS_GESTIONAR]}>
                    <SolicitudesNuevoServicioPage />
                  </PermissionRoute>
                }
                path="/admin/solicitudes"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.CUOTAS_VER, PERMISOS.CUOTAS_GESTIONAR]}>
                    <CuotasPage />
                  </PermissionRoute>
                }
                path="/admin/cuotas"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.OBLIGACIONES_VER, PERMISOS.OBLIGACIONES_GESTIONAR]}>
                    <ObligacionesPage />
                  </PermissionRoute>
                }
                path="/admin/obligaciones"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.JORNADAS_VER, PERMISOS.JORNADAS_GESTIONAR]}>
                    <JornadasPage />
                  </PermissionRoute>
                }
                path="/admin/jornadas"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.PAGOS_VER, PERMISOS.PAGOS_GESTIONAR]}>
                    <PagosPage />
                  </PermissionRoute>
                }
                path="/admin/pagos"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.FINANZAS_VER, PERMISOS.FINANZAS_GESTIONAR]}>
                    <FinanzasPage />
                  </PermissionRoute>
                }
                path="/admin/finanzas"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.DASHBOARD_VER]}>
                    <DashboardPage />
                  </PermissionRoute>
                }
                path="/admin/dashboard"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.REPORTES_VER]}>
                    <ReportesPage />
                  </PermissionRoute>
                }
                path="/admin/reportes"
              />

              <Route
                element={
                  <PermissionRoute permissions={[PERMISOS.ABASTECIMIENTO_VER, PERMISOS.ABASTECIMIENTO_GESTIONAR]}>
                    <AbastecimientoPage />
                  </PermissionRoute>
                }
                path="/admin/abastecimiento"
              />

              <Route
                element={
                  <PermissionRoute permissions={ADMIN_MODULE_PERMISSIONS}>
                    <Suspense fallback={<AdministracionLoading />}>
                      <AdministracionPage />
                    </Suspense>
                  </PermissionRoute>
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
          <Link className="text-sm text-slate-600 hover:text-slate-900" to="/login">
            Acceso administrativo
          </Link>
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
