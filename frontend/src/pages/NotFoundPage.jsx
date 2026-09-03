import { Link } from 'react-router-dom'

function NotFoundPage() {
  return (
    <main className="mx-auto max-w-5xl px-6 py-16">
      <h1 className="text-3xl font-bold text-slate-900">Página no encontrada</h1>
      <Link className="mt-4 inline-block text-cyan-700 underline" to="/">
        Volver al inicio
      </Link>
    </main>
  )
}

export default NotFoundPage