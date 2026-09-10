import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import { getCuotas } from '../services/cuotaService.js'
import { annulObligacion, generateObligacionFromCuota, getObligaciones } from '../services/obligacionService.js'
import { getSuministros } from '../services/suministroService.js'

const initialForm = { cuotaId: '', suministroId: '', periodo: '', fechaVencimiento: '' }

function ObligacionesPage() {
  const { authenticatedRequest } = useAuth()
  const requestRef = useRef(authenticatedRequest)
  const [obligaciones, setObligaciones] = useState([])
  const [cuotas, setCuotas] = useState([])
  const [suministros, setSuministros] = useState([])
  const [form, setForm] = useState(initialForm)
  const [isGenerateOpen, setIsGenerateOpen] = useState(false)
  const [pendingAnnulment, setPendingAnnulment] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [referenceError, setReferenceError] = useState(null)
  const [formError, setFormError] = useState(null)
  const [feedback, setFeedback] = useState(null)

  useEffect(() => {
    let mounted = true

    async function loadData() {
      setIsLoading(true)
      setError(null)
      setReferenceError(null)
      const [obligationResult, feeResult, supplyResult] = await Promise.allSettled([
        getObligaciones(requestRef.current),
        getCuotas(requestRef.current),
        getSuministros(requestRef.current),
      ])

      if (!mounted) return
      if (obligationResult.status === 'fulfilled') {
        setObligaciones(Array.isArray(obligationResult.value) ? obligationResult.value : [])
      } else {
        setError(getRequestMessage(obligationResult.reason, 'No se pudieron cargar las obligaciones.'))
      }
      if (feeResult.status === 'fulfilled') setCuotas(Array.isArray(feeResult.value) ? feeResult.value : [])
      if (supplyResult.status === 'fulfilled') setSuministros(Array.isArray(supplyResult.value) ? supplyResult.value : [])
      if (feeResult.status === 'rejected' || supplyResult.status === 'rejected') {
        setReferenceError('No se pudieron cargar las cuotas o suministros necesarios para generar una obligación.')
      }
      setIsLoading(false)
    }

    loadData()
    return () => { mounted = false }
  }, [])

  const selectedCuota = cuotas.find((cuota) => cuota.id === Number(form.cuotaId))

  function openGenerate() {
    setForm(initialForm)
    setFormError(null)
    setFeedback(null)
    setIsGenerateOpen(true)
  }

  function closeGenerate() {
    setForm(initialForm)
    setFormError(null)
    setIsGenerateOpen(false)
  }

  function changeField(event) {
    const { name, value } = event.target
    setForm((current) => ({
      ...current,
      [name]: value,
      ...(name === 'cuotaId' ? { periodo: '' } : {}),
    }))
  }

  function validateGeneration() {
    if (!form.cuotaId) return 'Selecciona una cuota.'
    if (!form.suministroId) return 'Selecciona un suministro.'
    if (!selectedCuota) return 'La cuota seleccionada no está disponible.'
    const period = form.periodo.trim()
    if (selectedCuota.periodicidad === 1 && !/^\d{4}$/.test(period)) return 'El período anual debe tener el formato YYYY.'
    if (selectedCuota.periodicidad === 2 && !/^\d{4}-(0[1-9]|1[0-2])$/.test(period)) return 'El período mensual debe tener el formato YYYY-MM.'
    return null
  }

  async function generateObligation(event) {
    event.preventDefault()
    const validationError = validateGeneration()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    setFeedback(null)
    try {
      const created = await generateObligacionFromCuota(authenticatedRequest, {
        cuotaId: Number(form.cuotaId),
        suministroId: Number(form.suministroId),
        periodo: form.periodo.trim(),
        fechaVencimiento: form.fechaVencimiento || null,
      })
      setObligaciones((current) => [created, ...current.filter((item) => item.id !== created.id)])
      setFeedback('Obligación generada correctamente. El monto quedó registrado con el valor actual de la cuota.')
      closeGenerate()
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo generar la obligación.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function annulObligation(motivo) {
    if (!pendingAnnulment) return
    setIsSaving(true)
    setError(null)
    setFeedback(null)
    try {
      const updated = await annulObligacion(authenticatedRequest, pendingAnnulment.id, motivo)
      setObligaciones((current) => current.map((item) => item.id === updated.id ? updated : item))
      setPendingAnnulment(null)
      setFeedback('Obligación anulada correctamente.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo anular la obligación.'))
    } finally {
      setIsSaving(false)
    }
  }

  const supplyById = new Map(suministros.map((suministro) => [suministro.id, suministro]))
  const cuotaById = new Map(cuotas.map((cuota) => [cuota.id, cuota]))
  const generationUnavailable = Boolean(referenceError || cuotas.length === 0 || suministros.length === 0)

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={<button className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60" disabled={generationUnavailable} onClick={openGenerate} type="button">Generar obligación</button>}
        description="Consulta las deudas históricas de los suministros y administra las obligaciones generadas desde cuotas."
        eyebrow="Gestión financiera"
        title="Obligaciones"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}
      {referenceError && <Alert message={referenceError} />}
      {feedback && <SuccessAlert message={feedback} />}

      {isGenerateOpen && (
        <Panel>
          <div className="flex items-start justify-between gap-4"><div><h2 className="text-base font-semibold text-slate-900">Generar desde cuota</h2><p className="mt-1 text-sm leading-6 text-slate-500">La obligación se asignará al suministro y el backend congelará el monto de la cuota.</p></div><button aria-label="Cerrar formulario de generación" className="rounded-md px-2 py-1 text-sm font-semibold text-slate-500 hover:bg-slate-100" disabled={isSaving} onClick={closeGenerate} type="button">Cerrar</button></div>
          <form className="mt-5 grid gap-4 md:grid-cols-2" onSubmit={generateObligation}>
            <label className="text-sm font-medium text-slate-700" htmlFor="obligacion-cuota">Cuota *<select className={inputClass} id="obligacion-cuota" name="cuotaId" onChange={changeField} required value={form.cuotaId}><option value="">Selecciona una cuota</option>{cuotas.map((cuota) => <option key={cuota.id} value={cuota.id}>{cuota.nombre} · {formatMoney(cuota.monto)} · {periodicityLabel(cuota.periodicidad)}</option>)}</select></label>
            <label className="text-sm font-medium text-slate-700" htmlFor="obligacion-suministro">Suministro *<select className={inputClass} id="obligacion-suministro" name="suministroId" onChange={changeField} required value={form.suministroId}><option value="">Selecciona un suministro</option>{suministros.map((suministro) => <option key={suministro.id} value={suministro.id}>{suministro.nis} · {suministro.sectorNombre}</option>)}</select></label>
            <label className="text-sm font-medium text-slate-700" htmlFor="obligacion-periodo">Período *<input className={inputClass} disabled={!selectedCuota} id="obligacion-periodo" inputMode="numeric" name="periodo" onChange={changeField} placeholder={selectedCuota?.periodicidad === 2 ? 'Ej. 2026-01' : 'Ej. 2026'} required value={form.periodo} /><span className="mt-1 block text-xs font-normal text-slate-500">{selectedCuota ? `Formato ${selectedCuota.periodicidad === 2 ? 'mensual: YYYY-MM' : 'anual: YYYY'}.` : 'Selecciona una cuota para conocer el formato.'}</span></label>
            <label className="text-sm font-medium text-slate-700" htmlFor="obligacion-vencimiento">Fecha de vencimiento <span className="font-normal text-slate-400">(opcional)</span><input className={inputClass} id="obligacion-vencimiento" name="fechaVencimiento" onChange={changeField} type="date" value={form.fechaVencimiento} /></label>
            {selectedCuota && <div className="rounded-md border border-[#28727a]/20 bg-[#eef6f5] p-4 text-sm text-[#1c5961] md:col-span-2"><p className="font-semibold">Referencia de la cuota</p><p className="mt-1">{selectedCuota.nombre}: {formatMoney(selectedCuota.monto)} · {periodicityLabel(selectedCuota.periodicidad)}. El servidor determinará el monto histórico.</p></div>}
            {formError && <div className="md:col-span-2"><Alert message={formError} /></div>}
            <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end md:col-span-2"><button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={closeGenerate} type="button">Volver</button><button className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] disabled:cursor-not-allowed disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Generando...' : 'Generar obligación'}</button></div>
          </form>
        </Panel>
      )}

      <Panel>
        <div className="flex flex-wrap items-start justify-between gap-4"><div><h2 className="text-base font-semibold text-slate-900">Obligaciones registradas</h2><p className="mt-1 text-sm text-slate-500">{obligaciones.length} {obligaciones.length === 1 ? 'obligación' : 'obligaciones'}</p></div><div className="rounded-md bg-slate-50 px-3 py-2 text-xs leading-5 text-slate-600">La morosidad mostrada proviene del servidor.</div></div>
        <div className="mt-5">
          {isLoading ? <LoadingState message="Cargando obligaciones..." /> : obligaciones.length === 0 ? <EmptyState description="Las obligaciones generadas desde cuotas aparecerán aquí." title="No hay obligaciones registradas" /> : <ObligationList cuotaById={cuotaById} isSaving={isSaving} obligaciones={obligaciones} onAnnul={setPendingAnnulment} supplyById={supplyById} />}
        </div>
      </Panel>

      {pendingAnnulment && <AnnulmentDialog isSaving={isSaving} obligation={pendingAnnulment} onCancel={() => setPendingAnnulment(null)} onConfirm={annulObligation} supply={supplyById.get(pendingAnnulment.suministroId)} />}
    </div>
  )
}

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:cursor-not-allowed disabled:bg-slate-100'

function ObligationList({ cuotaById, isSaving, obligaciones, onAnnul, supplyById }) {
  return <>
    <div className="hidden overflow-x-auto lg:block"><table className="w-full min-w-[980px] text-left text-sm"><thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500"><tr><th className="pb-3 pr-4 font-semibold">Concepto</th><th className="pb-3 pr-4 font-semibold">Titular</th><th className="pb-3 pr-4 font-semibold">Período</th><th className="pb-3 pr-4 font-semibold">Monto</th><th className="pb-3 pr-4 font-semibold">Fechas</th><th className="pb-3 pr-4 font-semibold">Situación</th><th className="pb-3 text-right font-semibold">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{obligaciones.map((obligation) => <ObligationRow cuota={cuotaById.get(obligation.cuotaId)} isSaving={isSaving} key={obligation.id} obligation={obligation} onAnnul={onAnnul} supply={supplyById.get(obligation.suministroId)} />)}</tbody></table></div>
    <div className="space-y-3 lg:hidden">{obligaciones.map((obligation) => <ObligationCard cuota={cuotaById.get(obligation.cuotaId)} isSaving={isSaving} key={obligation.id} obligation={obligation} onAnnul={onAnnul} supply={supplyById.get(obligation.suministroId)} />)}</div>
  </>
}

function ObligationRow({ cuota, isSaving, obligation, onAnnul, supply }) {
  return <tr><td className="max-w-52 py-4 pr-4"><p className="font-semibold text-slate-900">{obligation.concepto}</p><p className="mt-1 text-xs text-slate-500">{cuota ? `Cuota: ${cuota.nombre}` : `Cuota #${obligation.cuotaId ?? '—'}`}</p></td><td className="py-4 pr-4 text-slate-600">{holderLabel(obligation, supply)}</td><td className="py-4 pr-4 font-medium text-slate-700">{obligation.periodo || '—'}</td><td className="whitespace-nowrap py-4 pr-4 font-semibold text-slate-700">{formatMoney(obligation.monto)}</td><td className="py-4 pr-4 text-xs leading-5 text-slate-600"><span className="block">Generada: {formatDateTime(obligation.fechaGeneracion)}</span><span className="block">Vence: {formatDate(obligation.fechaVencimiento)}</span></td><td className="py-4 pr-4"><Situation obligation={obligation} /></td><td className="py-4 text-right"><ObligationActions isSaving={isSaving} obligation={obligation} onAnnul={onAnnul} /></td></tr>
}

function ObligationCard({ cuota, isSaving, obligation, onAnnul, supply }) {
  return <article className="rounded-md border border-slate-200 p-4"><div className="flex items-start justify-between gap-3"><div className="min-w-0"><h3 className="font-semibold text-slate-900">{obligation.concepto}</h3><p className="mt-1 text-sm text-slate-500">{holderLabel(obligation, supply)}</p></div><Situation obligation={obligation} /></div><dl className="mt-4 grid grid-cols-2 gap-3 rounded-md bg-slate-50 p-3 text-sm"><Data label="Monto" value={formatMoney(obligation.monto)} strong /><Data label="Período" value={obligation.periodo || '—'} /><Data label="Cuota" value={cuota?.nombre || `#${obligation.cuotaId ?? '—'}`} /><Data label="Generación" value={formatDateTime(obligation.fechaGeneracion)} /><Data label="Vencimiento" value={formatDate(obligation.fechaVencimiento)} /></dl><div className="mt-4 border-t border-slate-100 pt-3"><ObligationActions isSaving={isSaving} obligation={obligation} onAnnul={onAnnul} /></div></article>
}

function Data({ label, strong = false, value }) {
  return <div><dt className="text-xs text-slate-500">{label}</dt><dd className={`mt-1 ${strong ? 'font-semibold text-slate-800' : 'text-slate-700'}`}>{value}</dd></div>
}

function Situation({ obligation }) {
  return <div className="flex flex-col items-start gap-1.5"><ObligationStatusBadge estado={obligation.estado} />{obligation.esMorosa && <span className="inline-flex rounded-full bg-red-50 px-2.5 py-1 text-xs font-semibold text-red-700">Morosa</span>}</div>
}

function ObligationStatusBadge({ estado }) {
  const status = estado === 1
    ? { label: 'Pendiente', classes: 'bg-amber-50 text-amber-800', dot: 'bg-amber-500' }
    : estado === 2
      ? { label: 'Pagada', classes: 'bg-[#e3f2ed] text-[#17644e]', dot: 'bg-[#238568]' }
      : estado === 3
        ? { label: 'Anulada', classes: 'bg-slate-100 text-slate-600', dot: 'bg-slate-400' }
        : { label: 'Desconocido', classes: 'bg-slate-100 text-slate-600', dot: 'bg-slate-400' }
  return <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${status.classes}`}><span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${status.dot}`} />{status.label}</span>
}

function ObligationActions({ isSaving, obligation, onAnnul }) {
  if (obligation.estado !== 1) return <span className="text-xs text-slate-400">Sin acciones disponibles</span>
  return <button className="rounded-md border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-60" disabled={isSaving} onClick={() => onAnnul(obligation)} type="button">Anular</button>
}

function AnnulmentDialog({ isSaving, obligation, onCancel, onConfirm, supply }) {
  const [motivo, setMotivo] = useState('')
  const [dialogError, setDialogError] = useState(null)

  function submit(event) {
    event.preventDefault()
    if (!motivo.trim()) {
      setDialogError('El motivo de anulación es obligatorio.')
      return
    }
    setDialogError(null)
    onConfirm(motivo.trim())
  }

  return <div aria-labelledby="annulment-title" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/40 p-4" role="dialog"><div className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-lg overflow-y-auto rounded-lg bg-white p-6 shadow-xl"><h2 className="text-lg font-semibold text-slate-900" id="annulment-title">Anular obligación</h2><p className="mt-2 text-sm leading-6 text-slate-600">Esta acción cambiará la obligación pendiente a Anulada. No eliminará su registro histórico.</p><dl className="mt-4 space-y-1 rounded-md bg-slate-50 p-4 text-sm text-slate-600"><div><dt className="inline font-medium text-slate-500">Concepto: </dt><dd className="inline">{obligation.concepto}</dd></div><div><dt className="inline font-medium text-slate-500">Suministro: </dt><dd className="inline">{supply?.nis || `#${obligation.suministroId}`}</dd></div><div><dt className="inline font-medium text-slate-500">Monto: </dt><dd className="inline">{formatMoney(obligation.monto)}</dd></div></dl><form className="mt-5 space-y-4" onSubmit={submit}><div><label className="text-sm font-medium text-slate-700" htmlFor="annulment-reason">Motivo <span className="text-red-700">*</span></label><textarea autoFocus className={`${inputClass} min-h-28 resize-y`} id="annulment-reason" onChange={(event) => setMotivo(event.target.value)} required rows="4" value={motivo} /></div>{dialogError && <Alert message={dialogError} />}<div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={onCancel} type="button">Volver</button><button className="rounded-md bg-red-700 px-4 py-2 text-sm font-semibold text-white hover:bg-red-800 disabled:cursor-not-allowed disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Anulando...' : 'Confirmar anulación'}</button></div></form></div></div>
}

function Alert({ message, onDismiss }) {
  return <div aria-live="polite" className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"><span>{message}</span>{onDismiss && <button aria-label="Cerrar mensaje" className="shrink-0 font-semibold text-red-700" onClick={onDismiss} type="button">Cerrar</button>}</div>
}

function SuccessAlert({ message }) {
  return <div aria-live="polite" className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">{message}</div>
}

function holderLabel(obligation, supply) {
  if (supply) return `${supply.nis} · ${supply.sectorNombre}`
  if (obligation.suministroId) return `Suministro #${obligation.suministroId}`
  if (obligation.personaId) return `Persona #${obligation.personaId}`
  return 'Titular no disponible'
}

function periodicityLabel(periodicity) {
  return periodicity === 1 ? 'Anual' : periodicity === 2 ? 'Mensual' : 'Desconocida'
}

function formatMoney(value) {
  return new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'GTQ' }).format(value)
}

function formatDate(value) {
  if (!value) return 'Sin vencimiento'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('es-GT')
}

function formatDateTime(value) {
  if (!value) return 'Sin fecha'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-GT')
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400 || error?.status === 409) return error.message || fallback
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return error.message || 'La cuota, el suministro o la obligación ya no existen.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

export default ObligacionesPage
