import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'

function PublicOnlyRoute() {
  const { isAuthenticated, isInitializing } = useAuth()

  if (isInitializing) return <p className="p-6 text-slate-600">Comprobando sesión...</p>

  return isAuthenticated ? <Navigate replace to="/admin" /> : <Outlet />
}

export default PublicOnlyRoute