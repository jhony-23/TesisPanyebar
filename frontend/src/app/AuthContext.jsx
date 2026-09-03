import { useState } from 'react'
import { AuthContext } from './authContext.js'
import { authService } from '../services/authService.js'
import { httpClient } from '../services/httpClient.js'

export function AuthProvider({ children }) {
  const [accessToken, setAccessToken] = useState(null)
  const [user, setUser] = useState(null)
  const isInitializing = false

  async function login(credentials) {
    const result = await authService.login(credentials)
    const authenticatedUser = await authService.me(result.accessToken)

    setAccessToken(result.accessToken)
    setUser(authenticatedUser)
  }

  function logout() {
    setAccessToken(null)
    setUser(null)
  }

  async function authenticatedRequest(path, options = {}) {
    if (!accessToken) {
      throw new Error('No existe una sesión autenticada')
    }

    try {
      return await httpClient.request(path, { ...options, accessToken })
    } catch (error) {
      if (error.status === 401) logout()
      throw error
    }
  }

  return (
    <AuthContext.Provider
      value={{
        accessToken,
        authenticatedRequest,
        isAuthenticated: Boolean(accessToken && user),
        isInitializing,
        login,
        logout,
        user,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}