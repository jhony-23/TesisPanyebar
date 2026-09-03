import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'

function ProtectedRoute() {
  const { isAuthenticated, isInitializing } = useAuth()
  const location = useLocation()

  if (isInitializing) return <p className="p-6 text-slate-600">Comprobando sesión...</p>
  if (!isAuthenticated) return <Navigate replace state={{ from: location }} to="/login" />

  return <Outlet />
}

export default ProtectedRoute