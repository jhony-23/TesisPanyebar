import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'
import { getSuministroQrPublic } from '../services/suministroQrService.js'

const statusLabels = {
  1: 'Activo',
  2: 'Cancelado',
}

function SuministroQrPage() {
  const { token } = useParams()
  const { isAuthenticated } = useAuth()
  const [state, setState] = useState({ status: 'loading', suministro: null })

  useEffect(() => {
    let mounted = true

    async function resolveQr() {
      setState({ status: 'loading', suministro: null })

      try {
        const suministro = await getSuministroQrPublic(token)
        if (mounted) setState({ status: 'found', suministro })
      } catch (error) {
        if (!mounted) return
        setState({
          status: error?.status === 404 ? 'not-found' : 'error',
          suministro: null,
        })
      }
    }

    resolveQr()
    return () => { mounted = false }
  }, [token])

  return (
    <main className="mx-auto flex min-h-[calc(100vh-73px)] max-w-5xl items-center px-4 py-10 sm:px-6 sm:py-14">
      <section className="mx-auto w-full max-w-xl overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="bg-[#123b43] px-6 py-7 text-center text-white sm:px-10">
          <p className="text-sm font-semibold uppercase tracking-[0.18em] text-cyan-100">Comité de Agua Potable</p>
          <h1 className="mt-2 text-3xl font-bold tracking-tight">Panyebar</h1>
        </div>

        <div className="px-6 py-8 sm:px-10 sm:py-10">
          {state.status === 'loading' && <LoadingContent />}
          {state.status === 'found' && (
            <FoundContent isAuthenticated={isAuthenticated} suministro={state.suministro} />
          )}
          {state.status === 'not-found' && <NotFoundContent />}
          {state.status === 'error' && <ErrorContent />}
        </div>
      </section>
    </main>
  )
}

function LoadingContent() {
  return (
    <div aria-live="polite" className="py-8 text-center" role="status">
      <span className="mx-auto block h-10 w-10 animate-spin rounded-full border-4 border-slate-200 border-t-cyan-700" />
      <p className="mt-4 text-sm font-medium text-slate-600">Verificando el código QR...</p>
    </div>
  )
}

function FoundContent({ isAuthenticated, suministro }) {
  const estado = statusLabels[suministro.estado] || 'No disponible'
  const active = suministro.estado === 1

  return (
    <div>
      <div className="text-center">
        <span className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-700">
          <CheckIcon />
        </span>
        <h2 className="mt-4 text-2xl font-bold text-slate-900">Suministro identificado</h2>
        <p className="mt-2 text-sm leading-6 text-slate-600">El código QR corresponde a un suministro registrado.</p>
      </div>

      <dl className="mt-8 divide-y divide-slate-200 rounded-xl border border-slate-200 bg-slate-50 px-5">
        <Detail label="NIS" value={suministro.nis} />
        <Detail label="Sector" value={suministro.sectorNombre} />
        <div className="flex items-center justify-between gap-4 py-4">
          <dt className="text-sm font-medium text-slate-500">Estado</dt>
          <dd className={`rounded-full px-3 py-1 text-sm font-semibold ${active ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-800'}`}>
            {estado}
          </dd>
        </div>
        <Detail label="Obligaciones pendientes" value={suministro.cantidadObligacionesPendientes} />
        <Detail label="Total pendiente" value={`Q${suministro.totalPendiente.toFixed(2)}`} />
      </dl>

      <p className="mt-5 flex items-center justify-center gap-2 text-sm font-semibold text-emerald-700">
        <CheckIcon small /> Código QR válido
      </p>

      <Link
        className="mt-7 block w-full rounded-lg bg-cyan-800 px-4 py-3 text-center text-sm font-semibold text-white transition hover:bg-cyan-900 focus:outline-none focus:ring-2 focus:ring-cyan-700 focus:ring-offset-2"
        to={isAuthenticated ? '/admin/suministros' : '/login'}
      >
        {isAuthenticated ? 'Ver suministro en administración' : 'Acceso administrativo'}
      </Link>
    </div>
  )
}

function Detail({ label, value }) {
  return (
    <div className="py-4">
      <dt className="text-sm font-medium text-slate-500">{label}</dt>
      <dd className="mt-1 break-words text-lg font-semibold text-slate-900">{value}</dd>
    </div>
  )
}

function NotFoundContent() {
  return (
    <MessageContent
      description="No fue posible identificar un suministro con este código. Verifica el QR o solicita apoyo al comité."
      title="Código QR no válido"
    />
  )
}

function ErrorContent() {
  return (
    <MessageContent
      description="No pudimos conectar con el servicio en este momento. Revisa tu conexión e intenta nuevamente."
      title="No se pudo verificar el código"
    />
  )
}

function MessageContent({ description, title }) {
  return (
    <div className="py-4 text-center" role="alert">
      <span className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-amber-100 text-amber-800">
        <span aria-hidden="true" className="text-2xl font-bold">!</span>
      </span>
      <h2 className="mt-4 text-2xl font-bold text-slate-900">{title}</h2>
      <p className="mt-3 text-sm leading-6 text-slate-600">{description}</p>
      <Link className="mt-7 inline-block text-sm font-semibold text-cyan-800 underline-offset-4 hover:underline" to="/">
        Volver al inicio
      </Link>
    </div>
  )
}

function CheckIcon({ small = false }) {
  return (
    <svg aria-hidden="true" className={small ? 'h-4 w-4' : 'h-7 w-7'} fill="none" viewBox="0 0 24 24">
      <path d="m5 12 4 4L19 6" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" />
    </svg>
  )
}

export default SuministroQrPage
