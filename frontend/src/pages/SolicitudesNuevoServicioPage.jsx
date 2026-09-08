import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import { getPersonas } from '../services/personaService.js'
import { getSectores } from '../services/sectorService.js'
import {
  approveSolicitudNuevoServicio,
  createSolicitudNuevoServicio,
  getSolicitudesNuevoServicio,
  rejectSolicitudNuevoServicio,
} from '../services/solicitudNuevoServicioService.js'

const initialForm = {
  personaSolicitanteId: '',
  sectorId: '',
  direccionReferencia: '',
  observacion: '',
}

function SolicitudesNuevoServicioPage() {
  const { authenticatedRequest } = useAuth()
  const requestRef = useRef(authenticatedRequest)
  const [solicitudes, setSolicitudes] = useState([])
  const [personas, setPersonas] = useState([])
  const [sectores, setSectores] = useState([])
  const [form, setForm] = useState(initialForm)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [pendingResolution, setPendingResolution] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)
  const [feedback, setFeedback] = useState(null)

  useEffect(() => {
    let mounted = true

    async function loadData() {
      setIsLoading(true)
      setError(null)
      try {
        const [requestData, peopleData, sectorData] = await Promise.all([
          getSolicitudesNuevoServicio(requestRef.current),
          getPersonas(requestRef.current),
          getSectores(requestRef.current),
        ])
        if (mounted) {
          setSolicitudes(Array.isArray(requestData) ? requestData : [])
          setPersonas(Array.isArray(peopleData) ? peopleData.filter((person) => person.estado === 1) : [])
          setSectores(Array.isArray(sectorData) ? sectorData.filter((sector) => sector.estado === 1) : [])
        }
      } catch (requestError) {
        if (mounted) setError(getRequestMessage(requestError, 'No se pudieron cargar las solicitudes.'))
      } finally {
        if (mounted) setIsLoading(false)
      }
    }

    loadData()
    return () => { mounted = false }
  }, [])

  function resetForm() {
    setForm(initialForm)
    setFormError(null)
    setIsCreateOpen(false)
  }

  function changeField(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function validateForm() {
    if (!form.personaSolicitanteId) return 'Selecciona la persona solicitante.'
    if (!form.sectorId) return 'Selecciona el sector.'
    if (!form.direccionReferencia.trim()) return 'La dirección de referencia es obligatoria.'
    if (form.direccionReferencia.trim().length > 500) return 'La dirección no puede superar 500 caracteres.'
    if (form.observacion.trim().length > 1000) return 'La observación no puede superar 1000 caracteres.'
    return null
  }

  async function createRequest(event) {
    event.preventDefault()
    const validationError = validateForm()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    try {
      const created = await createSolicitudNuevoServicio(authenticatedRequest, {
        personaSolicitanteId: Number(form.personaSolicitanteId),
        sectorId: Number(form.sectorId),
        direccionReferencia: form.direccionReferencia.trim(),
        observacion: form.observacion.trim() || null,
      })
      setSolicitudes((current) => [created, ...current])
      setFeedback('Solicitud creada correctamente.')
      resetForm()
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo crear la solicitud.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function resolveRequest(observation) {
    if (!pendingResolution) return
    setIsSaving(true)
    setError(null)
    try {
      const resolve = pendingResolution.action === 'approve'
        ? approveSolicitudNuevoServicio
        : rejectSolicitudNuevoServicio
      const updated = await resolve(authenticatedRequest, pendingResolution.request.solicitudNuevoServicioId, {
        observacion: observation.trim() || null,
      })
      setSolicitudes((current) => current.map((item) => item.solicitudNuevoServicioId === updated.solicitudNuevoServicioId ? updated : item))
      setPendingResolution(null)
      setFeedback(pendingResolution.action === 'approve'
        ? `Solicitud aprobada. Se creó el suministro ${updated.nis || 'nuevo'}.`
        : 'Solicitud rechazada correctamente.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo resolver la solicitud.'))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={<button className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2" onClick={() => { setIsCreateOpen(true); setFormError(null) }} type="button">Nueva solicitud</button>}
        description="Administra las solicitudes para registrar nuevos suministros de agua."
        eyebrow="Gestión comunitaria"
        title="Solicitudes de nuevo servicio"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}
      {feedback && <div aria-live="polite" className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">{feedback}</div>}

      {isCreateOpen && <Panel>
        <div className="flex items-start justify-between gap-4">
          <div><h2 className="text-base font-semibold text-slate-900">Nueva solicitud</h2><p className="mt-1 text-sm text-slate-500">La solicitud comenzará en estado Pendiente.</p></div>
          <button aria-label="Cerrar formulario de solicitud" className="rounded-md px-2 py-1 text-sm font-semibold text-slate-500 hover:bg-slate-100" onClick={resetForm} type="button">Cerrar</button>
        </div>
        <form className="mt-5 grid gap-4 md:grid-cols-2" onSubmit={createRequest}>
          <label className="text-sm font-medium text-slate-700">Persona solicitante *<select className="mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 font-normal outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" name="personaSolicitanteId" onChange={changeField} required value={form.personaSolicitanteId}><option value="">Selecciona una persona</option>{personas.map((person) => <option key={person.id} value={person.id}>{person.nombres} {person.apellidos}</option>)}</select></label>
          <label className="text-sm font-medium text-slate-700">Sector *<select className="mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 font-normal outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" name="sectorId" onChange={changeField} required value={form.sectorId}><option value="">Selecciona un sector</option>{sectores.map((sector) => <option key={sector.id} value={sector.id}>{sector.nombre}</option>)}</select></label>
          <label className="text-sm font-medium text-slate-700 md:col-span-2">Dirección de referencia *<textarea className="mt-1.5 block min-h-24 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 font-normal outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" maxLength="500" name="direccionReferencia" onChange={changeField} required rows="3" value={form.direccionReferencia} /><span className="mt-1 block text-right text-xs font-normal text-slate-400">{form.direccionReferencia.length}/500</span></label>
          <label className="text-sm font-medium text-slate-700 md:col-span-2">Observación <span className="font-normal text-slate-400">(opcional)</span><textarea className="mt-1.5 block min-h-24 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 font-normal outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" maxLength="1000" name="observacion" onChange={changeField} rows="3" value={form.observacion} /><span className="mt-1 block text-right text-xs font-normal text-slate-400">{form.observacion.length}/1000</span></label>
          {formError && <p className="md:col-span-2 text-sm text-red-700" role="alert">{formError}</p>}
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end md:col-span-2"><button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={resetForm} type="button">Volver</button><button className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Guardando...' : 'Crear solicitud'}</button></div>
        </form>
      </Panel>}

      <Panel>
        <div className="flex items-start justify-between gap-4"><div><h2 className="text-base font-semibold text-slate-900">Solicitudes registradas</h2><p className="mt-1 text-sm text-slate-500">{solicitudes.length} {solicitudes.length === 1 ? 'solicitud' : 'solicitudes'}</p></div></div>
        <div className="mt-5">{isLoading ? <LoadingState message="Cargando solicitudes..." /> : solicitudes.length === 0 ? <EmptyState description="Las solicitudes de nuevo servicio aparecerán aquí." title="No hay solicitudes registradas" /> : <RequestList isSaving={isSaving} onResolve={setPendingResolution} solicitudes={solicitudes} />}</div>
      </Panel>

      {pendingResolution && <ResolutionDialog isSaving={isSaving} onCancel={() => setPendingResolution(null)} onConfirm={resolveRequest} request={pendingResolution.request} action={pendingResolution.action} />}
    </div>
  )
}

function RequestList({ isSaving, onResolve, solicitudes }) {
  return <><div className="hidden overflow-x-auto md:block"><table className="w-full min-w-[900px] text-left text-sm"><thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500"><tr><th className="pb-3 pr-4 font-semibold">Solicitante</th><th className="pb-3 pr-4 font-semibold">Sector</th><th className="pb-3 pr-4 font-semibold">Dirección</th><th className="pb-3 pr-4 font-semibold">Fecha</th><th className="pb-3 pr-4 font-semibold">Estado</th><th className="pb-3 pr-4 font-semibold">Suministro</th><th className="pb-3 text-right font-semibold">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{solicitudes.map((request) => <RequestRow isSaving={isSaving} key={request.solicitudNuevoServicioId} onResolve={onResolve} request={request} />)}</tbody></table></div><div className="space-y-3 md:hidden">{solicitudes.map((request) => <RequestCard isSaving={isSaving} key={request.solicitudNuevoServicioId} onResolve={onResolve} request={request} />)}</div></>
}

function RequestRow({ isSaving, onResolve, request }) {
  return <tr><td className="py-4 pr-4 font-semibold text-slate-900">{request.personaSolicitante}</td><td className="py-4 pr-4 text-slate-600">{request.sectorNombre}</td><td className="max-w-48 py-4 pr-4 text-slate-600">{request.direccionReferencia}</td><td className="py-4 pr-4 text-slate-600">{formatDate(request.fechaSolicitud)}</td><td className="py-4 pr-4"><RequestStatusBadge estado={request.estado} /></td><td className="py-4 pr-4 text-slate-600">{request.nis || '—'}</td><td className="py-4 text-right"><RequestActions isSaving={isSaving} onResolve={onResolve} request={request} /></td></tr>
}

function RequestCard({ isSaving, onResolve, request }) {
  return <article className="rounded-md border border-slate-200 p-4"><div className="flex items-start justify-between gap-3"><div className="min-w-0"><h3 className="font-semibold text-slate-900">{request.personaSolicitante}</h3><dl className="mt-2 space-y-1 text-sm text-slate-600"><div><dt className="inline font-medium text-slate-500">Sector: </dt><dd className="inline">{request.sectorNombre}</dd></div><div><dt className="inline font-medium text-slate-500">Dirección: </dt><dd className="inline">{request.direccionReferencia}</dd></div><div><dt className="inline font-medium text-slate-500">Fecha: </dt><dd className="inline">{formatDate(request.fechaSolicitud)}</dd></div><div><dt className="inline font-medium text-slate-500">Suministro: </dt><dd className="inline">{request.nis || '—'}</dd></div></dl></div><RequestStatusBadge estado={request.estado} /></div><div className="mt-4 border-t border-slate-100 pt-3"><RequestActions isSaving={isSaving} onResolve={onResolve} request={request} /></div></article>
}

function RequestActions({ isSaving, onResolve, request }) {
  if (request.estado !== 1) return <p className="text-right text-xs text-slate-500">Resuelta el {formatDate(request.fechaResolucion)} por {request.usuarioResolucion || 'usuario administrativo'}</p>
  return <div className="flex flex-wrap justify-end gap-2"><button className="rounded-md border border-[#28727a]/40 px-3 py-1.5 text-xs font-semibold text-[#1c5961] hover:bg-[#eef6f5] disabled:opacity-60" disabled={isSaving} onClick={() => onResolve({ action: 'approve', request })} type="button">Aprobar</button><button className="rounded-md border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:opacity-60" disabled={isSaving} onClick={() => onResolve({ action: 'reject', request })} type="button">Rechazar</button></div>
}

function RequestStatusBadge({ estado }) {
  const status = estado === 1
    ? { label: 'Pendiente', className: 'bg-amber-50 text-amber-800', dot: 'bg-amber-500' }
    : estado === 2
      ? { label: 'Aprobada', className: 'bg-[#e3f2ed] text-[#17644e]', dot: 'bg-[#238568]' }
      : { label: 'Rechazada', className: 'bg-red-50 text-red-700', dot: 'bg-red-500' }
  return <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${status.className}`}><span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${status.dot}`} />{status.label}</span>
}

function ResolutionDialog({ action, isSaving, onCancel, onConfirm, request }) {
  const [observation, setObservation] = useState('')
  const [error, setError] = useState(null)
  const isApproval = action === 'approve'

  function submit(event) {
    event.preventDefault()
    if (observation.trim().length > 1000) {
      setError('La observación no puede superar 1000 caracteres.')
      return
    }
    setError(null)
    onConfirm(observation)
  }

  return <div aria-labelledby="resolution-dialog-title" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/40 p-4" role="dialog"><div className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-lg overflow-y-auto rounded-lg bg-white p-6 shadow-xl"><h2 className="text-lg font-semibold text-slate-900" id="resolution-dialog-title">{isApproval ? 'Aprobar solicitud' : 'Rechazar solicitud'}</h2><dl className="mt-4 space-y-1 rounded-md bg-slate-50 p-4 text-sm text-slate-600"><div><dt className="inline font-medium text-slate-500">Solicitante: </dt><dd className="inline">{request.personaSolicitante}</dd></div><div><dt className="inline font-medium text-slate-500">Sector: </dt><dd className="inline">{request.sectorNombre}</dd></div><div><dt className="inline font-medium text-slate-500">Dirección: </dt><dd className="inline">{request.direccionReferencia}</dd></div></dl>{isApproval && <p className="mt-4 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm leading-6 text-amber-900">Al aprobar se creará un nuevo suministro con NIS y código QR propios.</p>}<form className="mt-5 space-y-4" onSubmit={submit}><label className="text-sm font-medium text-slate-700">Observación <span className="font-normal text-slate-400">{isApproval ? '(opcional)' : '(explicación administrativa recomendada)'}</span><textarea className="mt-1.5 block min-h-24 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 font-normal outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" maxLength="1000" onChange={(event) => setObservation(event.target.value)} rows="3" value={observation} /></label>{error && <p className="text-sm text-red-700" role="alert">{error}</p>}<div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={onCancel} type="button">Volver</button><button className={`rounded-md px-4 py-2 text-sm font-semibold disabled:opacity-60 ${isApproval ? 'bg-[#123b43] text-white' : 'bg-red-700 text-white'}`} disabled={isSaving} type="submit">{isSaving ? 'Guardando...' : isApproval ? 'Aprobar solicitud' : 'Rechazar solicitud'}</button></div></form></div></div>
}

function Alert({ message, onDismiss }) {
  return <div aria-live="polite" className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"><span>{message}</span>{onDismiss && <button aria-label="Cerrar mensaje" className="shrink-0 font-semibold text-red-700" onClick={onDismiss} type="button">Cerrar</button>}</div>
}

function formatDate(value) {
  if (!value) return 'Sin fecha'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-GT')
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400) return 'Los datos proporcionados no son válidos.'
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return 'La solicitud ya no existe.'
  if (error?.status === 409) return 'La persona ya tiene una solicitud pendiente de nuevo servicio.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

export default SolicitudesNuevoServicioPage
