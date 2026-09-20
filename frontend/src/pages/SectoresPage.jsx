import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import { PERMISOS } from '../app/permissions.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import {
  createSector,
  getSectores,
  setSectorEstado,
  updateSector,
} from '../services/sectorService.js'

const initialForm = { nombre: '', descripcion: '' }

function SectoresPage() {
  const { authenticatedRequest, hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.SECTORES_GESTIONAR)
  const authenticatedRequestRef = useRef(authenticatedRequest)
  const [sectores, setSectores] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)
  const [form, setForm] = useState(initialForm)
  const [editingId, setEditingId] = useState(null)
  const [pendingStatusChange, setPendingStatusChange] = useState(null)

  useEffect(() => {
    let isMounted = true

    async function loadSectores() {
      setIsLoading(true)
      setError(null)
      try {
        const data = await getSectores(authenticatedRequestRef.current)
        if (isMounted) setSectores(Array.isArray(data) ? data : [])
      } catch (requestError) {
        if (isMounted) setError(getRequestMessage(requestError, 'No se pudieron cargar los sectores.'))
      } finally {
        if (isMounted) setIsLoading(false)
      }
    }

    loadSectores()
    return () => { isMounted = false }
  }, [])

  function startCreate() {
    setEditingId(null)
    setForm(initialForm)
    setFormError(null)
  }

  function startEdit(sector) {
    setEditingId(sector.id)
    setForm({ nombre: sector.nombre, descripcion: sector.descripcion || '' })
    setFormError(null)
    window.scrollTo({ behavior: 'smooth', top: 0 })
  }

  function handleFieldChange(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function validateForm() {
    if (!form.nombre.trim()) return 'El nombre del sector es obligatorio.'
    if (form.nombre.trim().length > 100) return 'El nombre no puede superar 100 caracteres.'
    if (form.descripcion.length > 500) return 'La descripción no puede superar 500 caracteres.'
    return null
  }

  async function handleSubmit(event) {
    event.preventDefault()
    const validationError = validateForm()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    try {
      const payload = {
        nombre: form.nombre.trim(),
        descripcion: form.descripcion.trim() || null,
      }
      const savedSector = editingId === null
        ? await createSector(authenticatedRequest, payload)
        : await updateSector(authenticatedRequest, editingId, payload)

      setSectores((current) => editingId === null
        ? [...current, savedSector].sort(compareSectors)
        : current.map((sector) => sector.id === editingId ? savedSector : sector).sort(compareSectors))
      setForm(initialForm)
      setEditingId(null)
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo guardar el sector.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleStatusChange() {
    if (!pendingStatusChange) return

    const { id, estado } = pendingStatusChange
    setIsSaving(true)
    setError(null)
    try {
      const updatedSector = await setSectorEstado(authenticatedRequest, id, estado)
      setSectores((current) => current.map((sector) => sector.id === id ? updatedSector : sector))
      setPendingStatusChange(null)
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo actualizar el estado del sector.'))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={canManage ? (
          <button
            className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2"
            onClick={startCreate}
            type="button"
          >
            Nuevo sector
          </button>
        ) : null}
        description="Administra los sectores que organizan territorialmente el servicio de agua potable."
        eyebrow="Catálogo territorial"
        title="Sectores"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_20rem]">
        <Panel>
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-base font-semibold text-slate-900">Sectores registrados</h2>
              <p className="mt-1 text-sm text-slate-500">{sectores.length} {sectores.length === 1 ? 'sector' : 'sectores'}</p>
            </div>
          </div>
          <div className="mt-5">
            {isLoading ? <LoadingState message="Cargando sectores..." /> : sectores.length === 0 ? (
              <EmptyState
                description="Crea el primer sector para comenzar a organizar el catálogo territorial."
                title="Aún no hay sectores"
              />
            ) : (
              <SectorList
                canManage={canManage}
                isSaving={isSaving}
                onEdit={startEdit}
                onRequestStatusChange={setPendingStatusChange}
                sectores={sectores}
              />
            )}
          </div>
        </Panel>

        {canManage && (
          <Panel className="h-fit xl:sticky xl:top-28">
          <h2 className="text-base font-semibold text-slate-900">{editingId === null ? 'Crear sector' : 'Editar sector'}</h2>
          <p className="mt-1 text-sm leading-6 text-slate-500">
            {editingId === null ? 'Añade un sector al catálogo administrable.' : 'Actualiza el nombre o la descripción del sector.'}
          </p>
          <form className="mt-5 space-y-4" onSubmit={handleSubmit}>
            <div>
              <label className="text-sm font-medium text-slate-700" htmlFor="sector-nombre">Nombre</label>
              <input
                className="mt-1.5 block w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20"
                id="sector-nombre"
                maxLength="100"
                name="nombre"
                onChange={handleFieldChange}
                placeholder="Ej. Sector central"
                required
                value={form.nombre}
              />
              <p className="mt-1 text-right text-xs text-slate-400">{form.nombre.length}/100</p>
            </div>
            <div>
              <label className="text-sm font-medium text-slate-700" htmlFor="sector-descripcion">Descripción <span className="font-normal text-slate-400">(opcional)</span></label>
              <textarea
                className="mt-1.5 block min-h-28 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20"
                id="sector-descripcion"
                maxLength="500"
                name="descripcion"
                onChange={handleFieldChange}
                placeholder="Describe brevemente el sector"
                rows="4"
                value={form.descripcion}
              />
              <p className="mt-1 text-right text-xs text-slate-400">{form.descripcion.length}/500</p>
            </div>
            {formError && <Alert message={formError} />}
            <div className="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:justify-end">
              {editingId !== null && (
                <button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-[#28727a]" onClick={startCreate} type="button">Cancelar</button>
              )}
              <button className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] hover:bg-[#c99a50] focus:outline-none focus:ring-2 focus:ring-[#d6a85f] focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60" disabled={isSaving} type="submit">
                {isSaving ? 'Guardando...' : editingId === null ? 'Crear sector' : 'Guardar cambios'}
              </button>
            </div>
          </form>
          </Panel>
        )}
      </div>

      {pendingStatusChange && (
        <StatusConfirmation
          isSaving={isSaving}
          onCancel={() => setPendingStatusChange(null)}
          onConfirm={handleStatusChange}
          sector={sectores.find((item) => item.id === pendingStatusChange.id)}
          targetEstado={pendingStatusChange.estado}
        />
      )}
    </div>
  )
}

function SectorList({ canManage, isSaving, onEdit, onRequestStatusChange, sectores }) {
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[620px] text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="pb-3 pr-4 font-semibold">Nombre</th>
              <th className="pb-3 pr-4 font-semibold">Descripción</th>
              <th className="pb-3 pr-4 font-semibold">Estado</th>
              {canManage && <th className="pb-3 text-right font-semibold">Acciones</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {sectores.map((sector) => (
              <SectorRow
                canManage={canManage}
                isSaving={isSaving}
                key={sector.id}
                onEdit={onEdit}
                onRequestStatusChange={onRequestStatusChange}
                sector={sector}
              />
            ))}
          </tbody>
        </table>
      </div>
      <div className="space-y-3 md:hidden">
        {sectores.map((sector) => (
          <SectorCard
            canManage={canManage}
            isSaving={isSaving}
            key={sector.id}
            onEdit={onEdit}
            onRequestStatusChange={onRequestStatusChange}
            sector={sector}
          />
        ))}
      </div>
    </>
  )
}

function SectorRow({ canManage, isSaving, onEdit, onRequestStatusChange, sector }) {
  return (
    <tr>
      <td className="max-w-48 py-4 pr-4 font-semibold text-slate-900">{sector.nombre}</td>
      <td className="max-w-xs py-4 pr-4 text-slate-600">{sector.descripcion || <span className="text-slate-400">Sin descripción</span>}</td>
      <td className="py-4 pr-4"><StatusBadge estado={sector.estado} /></td>
      {canManage && (
        <td className="py-4 text-right">
          <SectorActions
            isSaving={isSaving}
            onEdit={onEdit}
            onRequestStatusChange={onRequestStatusChange}
            sector={sector}
          />
        </td>
      )}
    </tr>
  )
}

function SectorCard({ canManage, isSaving, onEdit, onRequestStatusChange, sector }) {
  return (
    <article className="rounded-md border border-slate-200 p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="truncate font-semibold text-slate-900">{sector.nombre}</h3>
          <p className="mt-1 text-sm leading-6 text-slate-600">
            {sector.descripcion || <span className="text-slate-400">Sin descripción</span>}
          </p>
        </div>
        <StatusBadge estado={sector.estado} />
      </div>
      {canManage && (
        <div className="mt-4 border-t border-slate-100 pt-3">
          <SectorActions
            isSaving={isSaving}
            onEdit={onEdit}
            onRequestStatusChange={onRequestStatusChange}
            sector={sector}
          />
        </div>
      )}
    </article>
  )
}

function SectorActions({ isSaving, onEdit, onRequestStatusChange, sector }) {
  const canChangeStatus = sector.estado === 1 || sector.estado === 2
  const nextEstado = sector.estado === 1 ? 2 : 1
  return (
    <div className="flex flex-wrap items-center justify-end gap-2">
      <button
        className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-[#28727a]"
        onClick={() => onEdit(sector)}
        type="button"
      >
        Editar
      </button>
      {canChangeStatus && (
        <button
          className="rounded-md border border-[#28727a]/40 px-3 py-1.5 text-xs font-semibold text-[#1c5961] hover:bg-[#eef6f5] focus:outline-none focus:ring-2 focus:ring-[#28727a]"
          disabled={isSaving}
          onClick={() => onRequestStatusChange({ estado: nextEstado, id: sector.id })}
          type="button"
        >
          {sector.estado === 1 ? 'Inactivar' : 'Activar'}
        </button>
      )}
    </div>
  )
}

function StatusBadge({ estado }) {
  const status = estado === 1
    ? { label: 'Activo', className: 'bg-[#e3f2ed] text-[#17644e]', dotClassName: 'bg-[#238568]' }
    : estado === 2
      ? { label: 'Inactivo', className: 'bg-slate-100 text-slate-600', dotClassName: 'bg-slate-400' }
      : { label: 'Estado desconocido', className: 'bg-slate-100 text-slate-600', dotClassName: 'bg-slate-400' }

  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${status.className}`}>
      <span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${status.dotClassName}`} />
      {status.label}
    </span>
  )
}

function StatusConfirmation({ isSaving, onCancel, onConfirm, sector, targetEstado }) {
  const dialogRef = useRef(null)

  useEffect(() => {
    dialogRef.current?.focus()

    function handleKeyDown(event) {
      if (event.key === 'Escape' && !isSaving) onCancel()
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isSaving, onCancel])

  if (!sector) return null
  const action = targetEstado === 1 ? 'activar' : 'inactivar'
  return (
    <div
      aria-labelledby="status-confirmation-title"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4"
      ref={dialogRef}
      role="dialog"
      tabIndex="-1"
    >
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-slate-900" id="status-confirmation-title">
          ¿Quieres {action} este sector?
        </h2>
        <p className="mt-2 text-sm leading-6 text-slate-600">
          El sector <strong>{sector.nombre}</strong> quedará como {targetEstado === 1 ? 'Activo' : 'Inactivo'}.
        </p>
        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-[#28727a]"
            onClick={onCancel}
            type="button"
          >
            Cancelar
          </button>
          <button
            className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2 disabled:opacity-60"
            disabled={isSaving}
            onClick={onConfirm}
            type="button"
          >
            {isSaving ? 'Actualizando...' : `Sí, ${action}`}
          </button>
        </div>
      </div>
    </div>
  )
}

function Alert({ message, onDismiss }) {
  return <div aria-live="polite" className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"><span>{message}</span>{onDismiss && <button aria-label="Cerrar mensaje" className="shrink-0 font-semibold text-red-700 hover:text-red-900 focus:outline-none focus:ring-2 focus:ring-red-700" onClick={onDismiss} type="button">Cerrar</button>}</div>
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400) return 'Los datos proporcionados no son válidos.'
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return 'El sector ya no existe.'
  if (error?.status === 409) return 'Ya existe un sector con ese nombre.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

function compareSectors(first, second) {
  return first.nombre.localeCompare(second.nombre, 'es', { sensitivity: 'base' }) || first.id - second.id
}

export default SectoresPage
