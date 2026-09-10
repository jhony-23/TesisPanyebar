import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import { createCuota, getCuotas, setCuotaEstado, updateCuota } from '../services/cuotaService.js'

const today = new Date().toLocaleDateString('en-CA')
const initialForm = {
  nombre: '',
  descripcion: '',
  monto: '',
  periodicidad: '1',
  fechaInicioVigencia: today,
  fechaFinVigencia: '',
}

function CuotasPage() {
  const { authenticatedRequest } = useAuth()
  const requestRef = useRef(authenticatedRequest)
  const [cuotas, setCuotas] = useState([])
  const [form, setForm] = useState(initialForm)
  const [editingId, setEditingId] = useState(null)
  const [pendingStatus, setPendingStatus] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)
  const [feedback, setFeedback] = useState(null)

  useEffect(() => {
    let mounted = true

    async function loadCuotas() {
      setIsLoading(true)
      setError(null)
      try {
        const data = await getCuotas(requestRef.current)
        if (mounted) setCuotas(Array.isArray(data) ? data : [])
      } catch (requestError) {
        if (mounted) setError(getRequestMessage(requestError, 'No se pudieron cargar las cuotas.'))
      } finally {
        if (mounted) setIsLoading(false)
      }
    }

    loadCuotas()
    return () => { mounted = false }
  }, [])

  function resetForm() {
    setForm(initialForm)
    setEditingId(null)
    setFormError(null)
  }

  function editCuota(cuota) {
    setEditingId(cuota.id)
    setForm({
      nombre: cuota.nombre,
      descripcion: cuota.descripcion || '',
      monto: String(cuota.monto),
      periodicidad: String(cuota.periodicidad),
      fechaInicioVigencia: dateInputValue(cuota.fechaInicioVigencia),
      fechaFinVigencia: dateInputValue(cuota.fechaFinVigencia),
    })
    setFormError(null)
    window.scrollTo({ behavior: 'smooth', top: 0 })
  }

  function changeField(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function validateForm() {
    const normalizedAmount = form.monto.trim()
    if (!form.nombre.trim()) return 'El nombre de la cuota es obligatorio.'
    if (form.nombre.trim().length > 100) return 'El nombre no puede superar 100 caracteres.'
    if (form.descripcion.trim().length > 500) return 'La descripción no puede superar 500 caracteres.'
    if (!normalizedAmount || !/^-?\d+(?:\.\d{1,2})?$/.test(normalizedAmount)) return 'Ingresa un monto válido con hasta dos decimales.'
    if (!['1', '2'].includes(form.periodicidad)) return 'Selecciona una periodicidad válida.'
    if (!form.fechaInicioVigencia) return 'La fecha de inicio de vigencia es obligatoria.'
    if (form.fechaFinVigencia && form.fechaFinVigencia < form.fechaInicioVigencia) return 'La fecha de fin no puede ser anterior al inicio de vigencia.'
    return null
  }

  async function saveCuota(event) {
    event.preventDefault()
    const validationError = validateForm()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    setFeedback(null)
    try {
      const payload = {
        nombre: form.nombre.trim(),
        descripcion: form.descripcion.trim() || null,
        monto: Number(form.monto),
        periodicidad: Number(form.periodicidad),
        fechaInicioVigencia: form.fechaInicioVigencia,
        fechaFinVigencia: form.fechaFinVigencia || null,
      }
      const saved = editingId === null
        ? await createCuota(authenticatedRequest, payload)
        : await updateCuota(authenticatedRequest, editingId, payload)

      setCuotas((current) => editingId === null
        ? [...current, saved].sort(compareCuotas)
        : current.map((cuota) => cuota.id === editingId ? saved : cuota).sort(compareCuotas))
      setFeedback(editingId === null ? 'Cuota creada correctamente.' : 'Cuota actualizada correctamente.')
      resetForm()
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo guardar la cuota.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function changeStatus() {
    if (!pendingStatus) return
    setIsSaving(true)
    setError(null)
    setFeedback(null)
    try {
      const updated = await setCuotaEstado(authenticatedRequest, pendingStatus.cuota.id, pendingStatus.estado)
      setCuotas((current) => current.map((cuota) => cuota.id === updated.id ? updated : cuota))
      setPendingStatus(null)
      setFeedback(`Cuota ${updated.estado === 1 ? 'activada' : 'inactivada'} correctamente.`)
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo actualizar el estado de la cuota.'))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={<button className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2" onClick={resetForm} type="button">Nueva cuota</button>}
        description="Configura los cobros administrativos que podrán generar obligaciones para los suministros."
        eyebrow="Gestión financiera"
        title="Cuotas"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}
      {feedback && <SuccessAlert message={feedback} />}

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <Panel>
          <h2 className="text-base font-semibold text-slate-900">Cuotas registradas</h2>
          <p className="mt-1 text-sm text-slate-500">{cuotas.length} {cuotas.length === 1 ? 'cuota' : 'cuotas'}</p>
          <div className="mt-5">
            {isLoading ? <LoadingState message="Cargando cuotas..." /> : cuotas.length === 0 ? (
              <EmptyState description="Crea la primera cuota para configurar un cobro administrativo." title="Aún no hay cuotas" />
            ) : (
              <CuotaList cuotas={cuotas} isSaving={isSaving} onEdit={editCuota} onStatus={setPendingStatus} />
            )}
          </div>
        </Panel>

        <Panel className="h-fit xl:sticky xl:top-28">
          <h2 className="text-base font-semibold text-slate-900">{editingId === null ? 'Crear cuota' : 'Editar cuota'}</h2>
          <p className="mt-1 text-sm leading-6 text-slate-500">El monto se copiará y conservará en cada obligación cuando sea generada.</p>
          <form className="mt-5 space-y-4" onSubmit={saveCuota}>
            <Field label="Nombre" inputId="cuota-nombre">
              <input className={inputClass} id="cuota-nombre" maxLength="100" name="nombre" onChange={changeField} required value={form.nombre} />
              <Counter value={form.nombre.length} maximum={100} />
            </Field>
            <Field label="Descripción" optional inputId="cuota-descripcion">
              <textarea className={`${inputClass} min-h-24 resize-y`} id="cuota-descripcion" maxLength="500" name="descripcion" onChange={changeField} rows="3" value={form.descripcion} />
              <Counter value={form.descripcion.length} maximum={500} />
            </Field>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-1">
              <Field label="Monto (Q)" inputId="cuota-monto">
                <input className={inputClass} id="cuota-monto" inputMode="decimal" name="monto" onChange={changeField} placeholder="0.00" required value={form.monto} />
              </Field>
              <Field label="Periodicidad" inputId="cuota-periodicidad">
                <select className={inputClass} id="cuota-periodicidad" name="periodicidad" onChange={changeField} required value={form.periodicidad}>
                  <option value="1">Anual</option>
                  <option value="2">Mensual</option>
                </select>
              </Field>
            </div>
            <Field label="Inicio de vigencia" inputId="cuota-inicio">
              <input className={inputClass} id="cuota-inicio" name="fechaInicioVigencia" onChange={changeField} required type="date" value={form.fechaInicioVigencia} />
            </Field>
            <Field label="Fin de vigencia" optional inputId="cuota-fin">
              <input className={inputClass} id="cuota-fin" min={form.fechaInicioVigencia} name="fechaFinVigencia" onChange={changeField} type="date" value={form.fechaFinVigencia} />
            </Field>
            {formError && <Alert message={formError} />}
            <div className="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:justify-end">
              {editingId !== null && <button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={resetForm} type="button">Cancelar</button>}
              <button className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] hover:bg-[#c99a50] disabled:cursor-not-allowed disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Guardando...' : editingId === null ? 'Crear cuota' : 'Guardar cambios'}</button>
            </div>
          </form>
        </Panel>
      </div>

      {pendingStatus && <StatusDialog isSaving={isSaving} onCancel={() => setPendingStatus(null)} onConfirm={changeStatus} pendingStatus={pendingStatus} />}
    </div>
  )
}

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20'

function Field({ children, inputId, label, optional = false }) {
  return <div><label className="text-sm font-medium text-slate-700" htmlFor={inputId}>{label} {optional && <span className="font-normal text-slate-400">(opcional)</span>}</label>{children}</div>
}

function Counter({ maximum, value }) {
  return <p className="mt-1 text-right text-xs text-slate-400">{value}/{maximum}</p>
}

function CuotaList({ cuotas, isSaving, onEdit, onStatus }) {
  return <>
    <div className="hidden overflow-x-auto md:block">
      <table className="w-full min-w-[760px] text-left text-sm">
        <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500"><tr><th className="pb-3 pr-4 font-semibold">Cuota</th><th className="pb-3 pr-4 font-semibold">Monto</th><th className="pb-3 pr-4 font-semibold">Periodicidad</th><th className="pb-3 pr-4 font-semibold">Vigencia</th><th className="pb-3 pr-4 font-semibold">Estado</th><th className="pb-3 text-right font-semibold">Acciones</th></tr></thead>
        <tbody className="divide-y divide-slate-100">{cuotas.map((cuota) => <CuotaRow cuota={cuota} isSaving={isSaving} key={cuota.id} onEdit={onEdit} onStatus={onStatus} />)}</tbody>
      </table>
    </div>
    <div className="space-y-3 md:hidden">{cuotas.map((cuota) => <CuotaCard cuota={cuota} isSaving={isSaving} key={cuota.id} onEdit={onEdit} onStatus={onStatus} />)}</div>
  </>
}

function CuotaRow({ cuota, isSaving, onEdit, onStatus }) {
  return <tr><td className="max-w-56 py-4 pr-4"><p className="font-semibold text-slate-900">{cuota.nombre}</p><p className="mt-1 text-xs leading-5 text-slate-500">{cuota.descripcion || 'Sin descripción'}</p></td><td className="whitespace-nowrap py-4 pr-4 font-semibold text-slate-700">{formatMoney(cuota.monto)}</td><td className="py-4 pr-4 text-slate-600">{periodicityLabel(cuota.periodicidad)}</td><td className="py-4 pr-4 text-slate-600">{formatValidity(cuota)}</td><td className="py-4 pr-4"><CuotaStatusBadge estado={cuota.estado} /></td><td className="py-4 text-right"><CuotaActions cuota={cuota} isSaving={isSaving} onEdit={onEdit} onStatus={onStatus} /></td></tr>
}

function CuotaCard({ cuota, isSaving, onEdit, onStatus }) {
  return <article className="rounded-md border border-slate-200 p-4"><div className="flex items-start justify-between gap-3"><div className="min-w-0"><h3 className="font-semibold text-slate-900">{cuota.nombre}</h3><p className="mt-1 text-sm leading-6 text-slate-500">{cuota.descripcion || 'Sin descripción'}</p></div><CuotaStatusBadge estado={cuota.estado} /></div><dl className="mt-3 grid grid-cols-2 gap-3 rounded-md bg-slate-50 p-3 text-sm"><div><dt className="text-xs text-slate-500">Monto</dt><dd className="mt-1 font-semibold text-slate-800">{formatMoney(cuota.monto)}</dd></div><div><dt className="text-xs text-slate-500">Periodicidad</dt><dd className="mt-1 text-slate-700">{periodicityLabel(cuota.periodicidad)}</dd></div><div className="col-span-2"><dt className="text-xs text-slate-500">Vigencia</dt><dd className="mt-1 text-slate-700">{formatValidity(cuota)}</dd></div></dl><div className="mt-4 border-t border-slate-100 pt-3"><CuotaActions cuota={cuota} isSaving={isSaving} onEdit={onEdit} onStatus={onStatus} /></div></article>
}

function CuotaActions({ cuota, isSaving, onEdit, onStatus }) {
  const targetEstado = cuota.estado === 1 ? 2 : 1
  return <div className="flex flex-wrap justify-end gap-2"><button className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50" onClick={() => onEdit(cuota)} type="button">Editar</button>{(cuota.estado === 1 || cuota.estado === 2) && <button className="rounded-md border border-[#28727a]/40 px-3 py-1.5 text-xs font-semibold text-[#1c5961] hover:bg-[#eef6f5] disabled:opacity-60" disabled={isSaving} onClick={() => onStatus({ cuota, estado: targetEstado })} type="button">{cuota.estado === 1 ? 'Inactivar' : 'Activar'}</button>}</div>
}

function CuotaStatusBadge({ estado }) {
  const active = estado === 1
  return <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${active ? 'bg-[#e3f2ed] text-[#17644e]' : 'bg-slate-100 text-slate-600'}`}><span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${active ? 'bg-[#238568]' : 'bg-slate-400'}`} />{active ? 'Activa' : estado === 2 ? 'Inactiva' : 'Desconocido'}</span>
}

function StatusDialog({ isSaving, onCancel, onConfirm, pendingStatus }) {
  const active = pendingStatus.estado === 1
  return <div aria-labelledby="cuota-status-title" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4" role="dialog"><div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl"><h2 className="text-lg font-semibold text-slate-900" id="cuota-status-title">¿Quieres {active ? 'activar' : 'inactivar'} esta cuota?</h2><p className="mt-2 text-sm leading-6 text-slate-600"><strong>{pendingStatus.cuota.nombre}</strong> quedará {active ? 'activa' : 'inactiva'}.</p><div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={onCancel} type="button">Volver</button><button className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60" disabled={isSaving} onClick={onConfirm} type="button">{isSaving ? 'Actualizando...' : `Sí, ${active ? 'activar' : 'inactivar'}`}</button></div></div></div>
}

function Alert({ message, onDismiss }) {
  return <div aria-live="polite" className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"><span>{message}</span>{onDismiss && <button aria-label="Cerrar mensaje" className="shrink-0 font-semibold text-red-700" onClick={onDismiss} type="button">Cerrar</button>}</div>
}

function SuccessAlert({ message }) {
  return <div aria-live="polite" className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">{message}</div>
}

function periodicityLabel(periodicity) {
  return periodicity === 1 ? 'Anual' : periodicity === 2 ? 'Mensual' : 'Desconocida'
}

function formatMoney(value) {
  return new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'GTQ' }).format(value)
}

function formatValidity(cuota) {
  return `${formatDate(cuota.fechaInicioVigencia)} — ${cuota.fechaFinVigencia ? formatDate(cuota.fechaFinVigencia) : 'Sin fecha de fin'}`
}

function formatDate(value) {
  if (!value) return 'Sin fecha'
  const datePart = String(value).slice(0, 10)
  const [year, month, day] = datePart.split('-')
  return year && month && day ? `${day}/${month}/${year}` : value
}

function dateInputValue(value) {
  return value ? String(value).slice(0, 10) : ''
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400) return error.message || 'Los datos proporcionados no son válidos.'
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return 'La cuota ya no existe.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

function compareCuotas(first, second) {
  return first.nombre.localeCompare(second.nombre, 'es', { sensitivity: 'base' }) || first.id - second.id
}

export default CuotasPage
