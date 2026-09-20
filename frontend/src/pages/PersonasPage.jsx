import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import { PERMISOS } from '../app/permissions.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import {
  createPersona,
  getPersonas,
  setPersonaEstado,
  updatePersona,
} from '../services/personaService.js'

const initialForm = {
  nombres: '',
  apellidos: '',
  identificacion: '',
  telefono: '',
  direccionReferencia: '',
}

function PersonasPage() {
  const { authenticatedRequest, hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.PERSONAS_GESTIONAR)
  const requestRef = useRef(authenticatedRequest)
  const [personas, setPersonas] = useState([])
  const [form, setForm] = useState(initialForm)
  const [editingId, setEditingId] = useState(null)
  const [pendingStatus, setPendingStatus] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)

  useEffect(() => {
    let mounted = true

    async function loadPersonas() {
      setError(null)
      try {
        const data = await getPersonas(requestRef.current)
        if (mounted) setPersonas(Array.isArray(data) ? data : [])
      } catch (requestError) {
        if (mounted) setError(getRequestMessage(requestError, 'No se pudieron cargar las personas.'))
      } finally {
        if (mounted) setIsLoading(false)
      }
    }

    loadPersonas()
    return () => { mounted = false }
  }, [])

  function resetForm() {
    setForm(initialForm)
    setEditingId(null)
    setFormError(null)
  }

  function editPersona(persona) {
    setEditingId(persona.id)
    setForm({
      nombres: persona.nombres,
      apellidos: persona.apellidos,
      identificacion: persona.identificacion || '',
      telefono: persona.telefono || '',
      direccionReferencia: persona.direccionReferencia || '',
    })
    setFormError(null)
    window.scrollTo({ behavior: 'smooth', top: 0 })
  }

  function changeField(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function validateForm() {
    if (!form.nombres.trim() || !form.apellidos.trim()) return 'Nombres y apellidos son obligatorios.'
    if (form.nombres.trim().length > 150 || form.apellidos.trim().length > 150) return 'Nombres y apellidos no pueden superar 150 caracteres.'
    if (form.identificacion.length > 50) return 'La identificación no puede superar 50 caracteres.'
    if (form.telefono.length > 30) return 'El teléfono no puede superar 30 caracteres.'
    if (form.direccionReferencia.length > 500) return 'La dirección no puede superar 500 caracteres.'
    return null
  }

  async function savePersona(event) {
    event.preventDefault()
    const validationError = validateForm()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    const payload = {
      nombres: form.nombres.trim(),
      apellidos: form.apellidos.trim(),
      identificacion: form.identificacion.trim() || null,
      telefono: form.telefono.trim() || null,
      direccionReferencia: form.direccionReferencia.trim() || null,
    }

    try {
      const saved = editingId === null
        ? await createPersona(authenticatedRequest, payload)
        : await updatePersona(authenticatedRequest, editingId, payload)
      setPersonas((current) => editingId === null
        ? [...current, saved].sort(comparePersonas)
        : current.map((persona) => persona.id === editingId ? saved : persona).sort(comparePersonas))
      resetForm()
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo guardar la persona.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function changeStatus() {
    if (!pendingStatus) return
    setIsSaving(true)
    setError(null)
    try {
      const updated = await setPersonaEstado(authenticatedRequest, pendingStatus.id, pendingStatus.estado)
      setPersonas((current) => current.map((persona) => persona.id === updated.id ? updated : persona))
      setPendingStatus(null)
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo actualizar el estado de la persona.'))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={canManage ? (
          <button
            className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2"
            onClick={resetForm}
            type="button"
          >
            Nueva persona
          </button>
        ) : null}
        description="Administra las personas registradas en el sistema de agua potable."
        eyebrow="Registro comunitario"
        title="Personas"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}
      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_21rem]">
        <Panel>
          <h2 className="text-base font-semibold text-slate-900">Personas registradas</h2>
          <p className="mt-1 text-sm text-slate-500">{personas.length} {personas.length === 1 ? 'persona' : 'personas'}</p>
          <div className="mt-5">
            {isLoading ? <LoadingState message="Cargando personas..." /> : personas.length === 0 ? (
              <EmptyState description="Crea la primera persona para comenzar el registro comunitario." title="Aún no hay personas" />
            ) : <PersonaList canManage={canManage} isSaving={isSaving} onEdit={editPersona} onStatus={setPendingStatus} personas={personas} />}
          </div>
        </Panel>

        {canManage && (
        <Panel className="h-fit xl:sticky xl:top-28">
          <h2 className="text-base font-semibold text-slate-900">{editingId === null ? 'Crear persona' : 'Editar persona'}</h2>
          <p className="mt-1 text-sm leading-6 text-slate-500">La identificación y el teléfono son opcionales.</p>
          <form className="mt-5 space-y-4" onSubmit={savePersona}>
            <FormField label="Nombres" maxLength="150" name="nombres" onChange={changeField} required value={form.nombres} />
            <FormField label="Apellidos" maxLength="150" name="apellidos" onChange={changeField} required value={form.apellidos} />
            <FormField hint="(opcional)" label="Identificación" maxLength="50" name="identificacion" onChange={changeField} value={form.identificacion} />
            <FormField hint="(opcional)" label="Teléfono" maxLength="30" name="telefono" onChange={changeField} value={form.telefono} />
            <div>
              <label className="text-sm font-medium text-slate-700" htmlFor="persona-direccion">Dirección de referencia <span className="font-normal text-slate-400">(opcional)</span></label>
              <textarea
                className="mt-1.5 block min-h-24 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20"
                id="persona-direccion"
                maxLength="500"
                name="direccionReferencia"
                onChange={changeField}
                rows="3"
                value={form.direccionReferencia}
              />
              <p className="mt-1 text-right text-xs text-slate-400">{form.direccionReferencia.length}/500</p>
            </div>
            {formError && <Alert message={formError} />}
            <div className="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:justify-end">
              {editingId !== null && <button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-[#28727a]" onClick={resetForm} type="button">Cancelar</button>}
              <button className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] hover:bg-[#c99a50] focus:outline-none focus:ring-2 focus:ring-[#d6a85f] focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Guardando...' : editingId === null ? 'Crear persona' : 'Guardar cambios'}</button>
            </div>
          </form>
        </Panel>
        )}
      </div>

      {pendingStatus && (
        <StatusConfirmation
          isSaving={isSaving}
          onCancel={() => setPendingStatus(null)}
          onConfirm={changeStatus}
          persona={personas.find((persona) => persona.id === pendingStatus.id)}
          targetEstado={pendingStatus.estado}
        />
      )}
    </div>
  )
}

function FormField({ hint, label, maxLength, name, onChange, required, value }) {
  const inputId = `persona-${name}`
  return (
    <div>
      <label className="text-sm font-medium text-slate-700" htmlFor={inputId}>{label} {hint && <span className="font-normal text-slate-400">{hint}</span>}</label>
      <input
        className="mt-1.5 block w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20"
        id={inputId}
        maxLength={maxLength}
        name={name}
        onChange={onChange}
        required={required}
        value={value}
      />
      <p className="mt-1 text-right text-xs text-slate-400">{value.length}/{maxLength}</p>
    </div>
  )
}

function PersonaList({ canManage, isSaving, onEdit, onStatus, personas }) {
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[700px] text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
            <tr><th className="pb-3 pr-4 font-semibold">Nombre completo</th><th className="pb-3 pr-4 font-semibold">Identificación</th><th className="pb-3 pr-4 font-semibold">Teléfono</th><th className="pb-3 pr-4 font-semibold">Estado</th>{canManage && <th className="pb-3 text-right font-semibold">Acciones</th>}</tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {personas.map((persona) => <PersonaRow canManage={canManage} isSaving={isSaving} key={persona.id} onEdit={onEdit} onStatus={onStatus} persona={persona} />)}
          </tbody>
        </table>
      </div>
      <div className="space-y-3 md:hidden">
        {personas.map((persona) => <PersonaCard canManage={canManage} isSaving={isSaving} key={persona.id} onEdit={onEdit} onStatus={onStatus} persona={persona} />)}
      </div>
    </>
  )
}

function PersonaRow({ canManage, isSaving, onEdit, onStatus, persona }) {
  return (
    <tr>
      <td className="max-w-56 py-4 pr-4 font-semibold text-slate-900">{persona.nombres} {persona.apellidos}</td>
      <td className="py-4 pr-4 text-slate-600">{persona.identificacion || <span className="text-slate-400">Sin identificación</span>}</td>
      <td className="py-4 pr-4 text-slate-600">{persona.telefono || <span className="text-slate-400">Sin teléfono</span>}</td>
      <td className="py-4 pr-4"><StatusBadge estado={persona.estado} /></td>
      {canManage && <td className="py-4 text-right"><PersonaActions isSaving={isSaving} onEdit={onEdit} onStatus={onStatus} persona={persona} /></td>}
    </tr>
  )
}

function PersonaCard({ canManage, isSaving, onEdit, onStatus, persona }) {
  return (
    <article className="rounded-md border border-slate-200 p-4">
      <div className="flex items-start justify-between gap-3"><div className="min-w-0"><h3 className="font-semibold text-slate-900">{persona.nombres} {persona.apellidos}</h3><dl className="mt-2 space-y-1 text-sm text-slate-600"><div><dt className="inline font-medium text-slate-500">Identificación: </dt><dd className="inline">{persona.identificacion || 'Sin identificación'}</dd></div><div><dt className="inline font-medium text-slate-500">Teléfono: </dt><dd className="inline">{persona.telefono || 'Sin teléfono'}</dd></div></dl></div><StatusBadge estado={persona.estado} /></div>
      {canManage && <div className="mt-4 border-t border-slate-100 pt-3"><PersonaActions isSaving={isSaving} onEdit={onEdit} onStatus={onStatus} persona={persona} /></div>}
    </article>
  )
}

function PersonaActions({ isSaving, onEdit, onStatus, persona }) {
  const validStatus = persona.estado === 1 || persona.estado === 2
  const nextEstado = persona.estado === 1 ? 2 : 1
  return (
    <div className="flex flex-wrap justify-end gap-2">
      <button className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-[#28727a]" onClick={() => onEdit(persona)} type="button">Editar</button>
      {validStatus && <button className="rounded-md border border-[#28727a]/40 px-3 py-1.5 text-xs font-semibold text-[#1c5961] hover:bg-[#eef6f5] focus:outline-none focus:ring-2 focus:ring-[#28727a]" disabled={isSaving} onClick={() => onStatus({ estado: nextEstado, id: persona.id })} type="button">{persona.estado === 1 ? 'Inactivar' : 'Activar'}</button>}
    </div>
  )
}

function StatusConfirmation({ isSaving, onCancel, onConfirm, persona, targetEstado }) {
  const dialogRef = useRef(null)
  useEffect(() => {
    dialogRef.current?.focus()
    function handleKeyDown(event) {
      if (event.key === 'Escape' && !isSaving) onCancel()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isSaving, onCancel])

  if (!persona) return null
  const action = targetEstado === 1 ? 'activar' : 'inactivar'
  return (
    <div
      aria-labelledby="persona-status-title"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4"
      ref={dialogRef}
      role="dialog"
      tabIndex="-1"
    >
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-slate-900" id="persona-status-title">¿Quieres {action} esta persona?</h2>
        <p className="mt-2 text-sm leading-6 text-slate-600">{persona.nombres} {persona.apellidos} quedará como {targetEstado === 1 ? 'Activo' : 'Inactivo'}.</p>
        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-[#28727a]" onClick={onCancel} type="button">Cancelar</button>
          <button className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2 disabled:opacity-60" disabled={isSaving} onClick={onConfirm} type="button">{isSaving ? 'Actualizando...' : `Sí, ${action}`}</button>
        </div>
      </div>
    </div>
  )
}

function StatusBadge({ estado }) {
  const status = estado === 1
    ? { label: 'Activo', badgeClassName: 'bg-[#e3f2ed] text-[#17644e]', dotClassName: 'bg-[#238568]' }
    : estado === 2
      ? { label: 'Inactivo', badgeClassName: 'bg-slate-100 text-slate-600', dotClassName: 'bg-slate-400' }
      : { label: 'Estado desconocido', badgeClassName: 'bg-slate-100 text-slate-600', dotClassName: 'bg-slate-400' }

  return <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${status.badgeClassName}`}><span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${status.dotClassName}`} />{status.label}</span>
}

function Alert({ message, onDismiss }) {
  return <div aria-live="polite" className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"><span>{message}</span>{onDismiss && <button aria-label="Cerrar mensaje" className="shrink-0 font-semibold text-red-700 hover:text-red-900 focus:outline-none focus:ring-2 focus:ring-red-700" onClick={onDismiss} type="button">Cerrar</button>}</div>
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400) return 'Los datos proporcionados no son válidos.'
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return 'La persona ya no existe.'
  if (error?.status === 409) return 'Ya existe una persona con esa identificación.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

function comparePersonas(first, second) {
  return first.apellidos.localeCompare(second.apellidos, 'es', { sensitivity: 'base' }) || first.nombres.localeCompare(second.nombres, 'es', { sensitivity: 'base' }) || first.id - second.id
}

export default PersonasPage
