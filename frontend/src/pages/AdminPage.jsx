import { useNavigate } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'

function AdminPage() {
  const navigate = useNavigate()
  const { logout, user } = useAuth()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <main className="mx-auto max-w-5xl px-6 py-12">
      <section className="max-w-xl">
        <p className="text-sm font-semibold uppercase tracking-wide text-cyan-700">Área administrativa</p>
        <h1 className="mt-3 text-3xl font-bold text-slate-900">Sesión iniciada</h1>
        <p className="mt-3 text-slate-600">Usuario: {user.nombreUsuario}</p>
        <button
          className="mt-6 rounded-md bg-slate-900 px-4 py-2 font-medium text-white hover:bg-slate-700"
          onClick={handleLogout}
          type="button"
        >
          Cerrar sesión
        </button>
      </section>
    </main>
  )
}

export default AdminPage