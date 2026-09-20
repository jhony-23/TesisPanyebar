import { Navigate } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'

function PermissionRoute({ permissions, children }) {
  const { hasAnyPermission } = useAuth()

  if (!permissions || permissions.length === 0) {
    return children
  }

  if (!hasAnyPermission(permissions)) {
    return <Navigate replace to="/admin" />
  }

  return children
}

export default PermissionRoute
