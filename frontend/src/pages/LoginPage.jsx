import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'

function LoginPage() {
  return (
    <LoginForm />
  )
}

function LoginForm() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login } = useAuth()
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [password, setPassword] = useState('')
  const [errorMessage, setErrorMessage] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    if (isSubmitting) return

    setErrorMessage('')
    setIsSubmitting(true)
    try {
      await login({ nombreUsuario, password })
      setPassword('')
      navigate(location.state?.from?.pathname || '/admin', { replace: true })
    } catch (error) {
      if (error.kind === 'unauthorized') {
        setErrorMessage('El nombre de usuario o la contraseña no son válidos.')
      } else if (error.kind === 'forbidden') {
        setErrorMessage('Tu usuario no tiene autorización para acceder.')
      } else if (error.kind === 'network') {
        setErrorMessage('No se pudo conectar con el servidor. Inténtalo de nuevo.')
      } else if (error.kind === 'server') {
        setErrorMessage('El servidor no está disponible en este momento.')
      } else {
        setErrorMessage('No se pudo completar el acceso. Inténtalo de nuevo.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="mx-auto flex min-h-[calc(100vh-73px)] max-w-5xl items-center px-6 py-12">
      <form className="w-full max-w-md space-y-5" onSubmit={handleSubmit}>
        <div>
          <p className="text-sm font-semibold uppercase tracking-wide text-cyan-700">Acceso administrativo</p>
          <h1 className="mt-3 text-3xl font-bold text-slate-900">Iniciar sesión</h1>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700" htmlFor="nombreUsuario">Nombre de usuario</label>
          <input
            autoComplete="username"
            className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2"
            id="nombreUsuario"
            onChange={(event) => setNombreUsuario(event.target.value)}
            required
            type="text"
            value={nombreUsuario}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700" htmlFor="password">Contraseña</label>
          <input
            autoComplete="current-password"
            className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2"
            id="password"
            onChange={(event) => setPassword(event.target.value)}
            required
            type="password"
            value={password}
          />
        </div>
        {errorMessage && <p className="text-sm text-red-700" role="alert">{errorMessage}</p>}
        <button
          className="w-full rounded-md bg-cyan-700 px-4 py-2 font-medium text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:opacity-50"
          disabled={isSubmitting}
          type="submit"
        >
          {isSubmitting ? 'Accediendo...' : 'Iniciar sesión'}
        </button>
      </form>
    </main>
  )
}

export default LoginPage
