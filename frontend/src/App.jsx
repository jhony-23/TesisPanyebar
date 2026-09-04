import { BrowserRouter, Link, Outlet, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './app/AuthContext.jsx'
import AdminLayout from './components/AdminLayout.jsx'
import ProtectedRoute from './components/ProtectedRoute.jsx'
import PublicOnlyRoute from './components/PublicOnlyRoute.jsx'
import AdminPage from './pages/AdminPage.jsx'
import HomePage from './pages/HomePage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import NotFoundPage from './pages/NotFoundPage.jsx'
import PersonasPage from './pages/PersonasPage.jsx'
import SectoresPage from './pages/SectoresPage.jsx'

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