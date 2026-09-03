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
    <main className="mx-auto flex min-h-[calc(100vh-73px)] max-w-5xl items-center px-4 py-8 sm:px-6 sm:py-12">
      <div className="grid w-full overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm md:grid-cols-[0.85fr_1.15fr]">
        <div className="hidden bg-[#123b43] p-8 text-white md:block lg:p-10">
          <p className="text-sm font-semibold uppercase tracking-[0.16em] text-teal-100/75">Panyebar</p>
          <h1 className="mt-16 text-3xl font-semibold leading-tight">Administración del servicio de agua potable.</h1>
          <p className="mt-5 text-sm leading-6 text-teal-50/75">Un espacio claro para acompañar la gestión del comité y la comunidad.</p>
        </div>
        <form className="w-full space-y-5 p-6 sm:p-9 lg:p-10" onSubmit={handleSubmit}>
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.16em] text-[#28727a]">Acceso administrativo</p>
            <h1 className="mt-3 text-3xl font-semibold tracking-tight text-slate-900">Iniciar sesión</h1>
            <p className="mt-2 text-sm text-slate-500">Ingresa tus credenciales para continuar.</p>
          </div>
        <div>
          <label className="block text-sm font-medium text-slate-700" htmlFor="nombreUsuario">Nombre de usuario</label>
          <input
            autoComplete="username"
            className="mt-2 min-h-11 w-full rounded-md border border-slate-300 px-3 py-2 text-base outline-none transition focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20"
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
            className="mt-2 min-h-11 w-full rounded-md border border-slate-300 px-3 py-2 text-base outline-none transition focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20"
            id="password"
            onChange={(event) => setPassword(event.target.value)}
            required
            type="password"
            value={password}
          />
        </div>
        {errorMessage && <p aria-live="polite" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">{errorMessage}</p>}
        <button
          className="min-h-11 w-full rounded-md bg-[#28727a] px-4 py-2 font-medium text-white transition hover:bg-[#1c5961] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
          disabled={isSubmitting}
          type="submit"
        >
          {isSubmitting ? 'Accediendo...' : 'Iniciar sesión'}
        </button>
        </form>
      </div>
    </main>
  )
}

export default LoginPage
