import { useEffect, useRef, useState } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import { useAuth } from '../app/useAuth.js'
import { PERMISOS } from '../app/permissions.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import { getPersonas } from '../services/personaService.js'
import { getSectores } from '../services/sectorService.js'
import { formatGuatemalaDateTime } from '../utils/dateTime.js'
import {
  cancelSuministro,
  createSuministro,
  getSuministroByNis,
  getSuministroProcesos,
  getSuministroQr,
  getSuministroResponsables,
  getSuministros,
  reconnectSuministro,
  setSuministroResponsable,
  updateSuministro,
} from '../services/suministroService.js'

const initialForm = { sectorId: '', direccionReferencia: '' }

function SuministrosPage() {
  const { authenticatedRequest, hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.SUMINISTROS_GESTIONAR)
  const requestRef = useRef(authenticatedRequest)
  const [suministros, setSuministros] = useState([])
  const [showAll, setShowAll] = useState(false)
  const visibleItems = showAll ? suministros : suministros.slice(0, 5)
  const [sectores, setSectores] = useState([])
  const [personas, setPersonas] = useState([])
  const [form, setForm] = useState(initialForm)
  const [editingId, setEditingId] = useState(null)
  const [pendingProcess, setPendingProcess] = useState(null)
  const [searchNis, setSearchNis] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)
  const [searchError, setSearchError] = useState(null)
  const [feedback, setFeedback] = useState(null)
  const [responsibleForm, setResponsibleForm] = useState({ suministroId: null, personaId: '' })
  const [pendingResponsible, setPendingResponsible] = useState(null)
  const [history, setHistory] = useState(null)
  const [historyLoading, setHistoryLoading] = useState(false)
  const [processHistory, setProcessHistory] = useState(null)
  const [processHistoryLoading, setProcessHistoryLoading] = useState(false)
  const [qrModal, setQrModal] = useState(null)
  const [qrLoading, setQrLoading] = useState(false)

  useEffect(() => {
    let mounted = true

    async function loadData() {
      setIsLoading(true)
      setError(null)
      try {
        const [supplyData, sectorData, peopleData] = await Promise.all([
          getSuministros(requestRef.current),
          getSectores(requestRef.current),
          getPersonas(requestRef.current).catch(() => []),
        ])
        if (mounted) {
          setSuministros(Array.isArray(supplyData) ? supplyData : [])
          setSectores(Array.isArray(sectorData) ? sectorData.filter((sector) => sector.estado === 1) : [])
          setPersonas(Array.isArray(peopleData) ? peopleData.filter((persona) => persona.estado === 1) : [])
        }
      } catch (requestError) {
        if (mounted) setError(getRequestMessage(requestError, 'No se pudieron cargar los suministros.'))
      } finally {
        if (mounted) setIsLoading(false)
      }
    }

    loadData()
    return () => { mounted = false }
  }, [])

  function resetForm() {
    setForm(initialForm)
    setEditingId(null)
    setFormError(null)
  }

  function editSuministro(suministro) {
    setEditingId(suministro.id)
    setForm({ sectorId: String(suministro.sectorId), direccionReferencia: suministro.direccionReferencia })
    setFormError(null)
    window.scrollTo({ behavior: 'smooth', top: 0 })
  }

  function changeField(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function validateForm() {
    if (!form.sectorId) return 'Selecciona un sector.'
    if (!form.direccionReferencia.trim()) return 'La dirección de referencia es obligatoria.'
    if (form.direccionReferencia.trim().length > 500) return 'La dirección no puede superar 500 caracteres.'
    return null
  }

  async function saveSuministro(event) {
    event.preventDefault()
    const validationError = validateForm()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    try {
      const payload = { sectorId: Number(form.sectorId), direccionReferencia: form.direccionReferencia.trim() }
      const saved = editingId === null
        ? await createSuministro(authenticatedRequest, payload)
        : await updateSuministro(authenticatedRequest, editingId, payload)

      setSuministros((current) => editingId === null
        ? [...current, saved].sort(compareSuministros)
        : current.map((suministro) => suministro.id === editingId ? saved : suministro).sort(compareSuministros))
      setFeedback(editingId === null ? `Suministro creado con NIS ${saved.nis}.` : 'Suministro actualizado correctamente.')
      resetForm()
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo guardar el suministro.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function searchByNis(event) {
    event.preventDefault()
    const normalized = searchNis.trim()
    if (!normalized) {
      clearSearch()
      return
    }

    setSearchError(null)
    setIsLoading(true)
    try {
      const result = await getSuministroByNis(authenticatedRequest, normalized)
      setSuministros(result ? [result] : [])
      if (!result) setSearchError('No se encontró un suministro con ese NIS.')
    } catch (requestError) {
      setSuministros([])
      setSearchError(requestError?.status === 404 ? 'No se encontró un suministro con ese NIS.' : getRequestMessage(requestError, 'No se pudo buscar el suministro.'))
    } finally {
      setIsLoading(false)
    }
  }

  async function clearSearch() {
    setSearchNis('')
    setSearchError(null)
    setIsLoading(true)
    try {
      const data = await getSuministros(authenticatedRequest)
      setSuministros(Array.isArray(data) ? data : [])
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudieron cargar los suministros.'))
    } finally {
      setIsLoading(false)
    }
  }

  async function processSupply(proceso) {
    if (!pendingProcess) return
    setIsSaving(true)
    setError(null)
    try {
      const update = pendingProcess.operation === 'cancelar' ? cancelSuministro : reconnectSuministro
      const updated = await update(authenticatedRequest, pendingProcess.suministro.id, {
        motivo: proceso.motivo.trim(),
        observacion: proceso.observacion.trim() || null,
      })
      setSuministros((current) => current.map((suministro) => suministro.id === updated.id ? updated : suministro))
      setPendingProcess(null)
      setFeedback(pendingProcess.operation === 'cancelar' ? 'Suministro cancelado correctamente.' : 'Suministro reconectado correctamente.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo procesar el cambio de estado.'))
    } finally {
      setIsSaving(false)
    }
  }

  function openResponsibleForm(suministro) {
    setResponsibleForm({ suministroId: suministro.id, personaId: '' })
    setFormError(null)
  }

  async function saveResponsible() {
    const personaId = Number(responsibleForm.personaId)
    if (!responsibleForm.suministroId || !personaId) {
      setFormError('Selecciona una persona responsable.')
      return
    }

    const suministro = suministros.find((item) => item.id === responsibleForm.suministroId)
    const persona = personas.find((item) => item.id === personaId)
    if (suministro?.responsableActual) {
      setPendingResponsible({ persona, suministro })
      return
    }

    await assignResponsible(responsibleForm.suministroId, personaId)
  }

  async function assignResponsible(suministroId, personaId) {
    setIsSaving(true)
    setFormError(null)
    try {
      const updated = await setSuministroResponsable(authenticatedRequest, suministroId, personaId)
      setSuministros((current) => current.map((item) => item.id === updated.id ? updated : item))
      setResponsibleForm({ suministroId: null, personaId: '' })
      setPendingResponsible(null)
      setFeedback('Responsable actualizado correctamente.')
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo actualizar el responsable.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function showHistory(suministro) {
    setHistory({ suministro, items: [] })
    setHistoryLoading(true)
    try {
      const items = await getSuministroResponsables(authenticatedRequest, suministro.id)
      setHistory({ suministro, items: Array.isArray(items) ? items : [] })
    } catch (requestError) {
      setHistory(null)
      setError(getRequestMessage(requestError, 'No se pudo cargar el historial de responsables.'))
    } finally {
      setHistoryLoading(false)
    }
  }

  async function showProcessHistory(suministro) {
    setProcessHistory({ suministro, items: [] })
    setProcessHistoryLoading(true)
    try {
      const items = await getSuministroProcesos(authenticatedRequest, suministro.id)
      setProcessHistory({ suministro, items: Array.isArray(items) ? items : [] })
    } catch (requestError) {
      setProcessHistory(null)
      setError(getRequestMessage(requestError, 'No se pudo cargar el historial de procesos.'))
    } finally {
      setProcessHistoryLoading(false)
    }
  }

  async function showQr(suministro) {
    setQrLoading(true)
    setError(null)
    try {
      const qr = await getSuministroQr(authenticatedRequest, suministro.id)
      setQrModal({ ...qr, suministro })
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo cargar el código QR.'))
    } finally {
      setQrLoading(false)
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={canManage ? <button className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2" onClick={resetForm} type="button">Nuevo suministro</button> : null}
        description="Administra la identidad y la información básica de los suministros del servicio de agua potable."
        eyebrow="Gestión comunitaria"
        title="Suministros"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}
      {feedback && <div aria-live="polite" className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">{feedback}</div>}

      <Panel>
        <form className="flex flex-col gap-3 sm:flex-row sm:items-end" onSubmit={searchByNis}>
          <div className="min-w-0 flex-1">
            <label className="text-sm font-medium text-slate-700" htmlFor="suministro-search">Buscar por NIS</label>
            <input className="mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" id="suministro-search" onChange={(event) => setSearchNis(event.target.value)} placeholder="Ej. PAN-000002" value={searchNis} />
          </div>
          <button className="min-h-11 rounded-md bg-[#28727a] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#1c5961]" type="submit">Buscar</button>
          <button className="min-h-11 rounded-md border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50" onClick={clearSearch} type="button">Limpiar</button>
        </form>
        {searchError && <p className="mt-3 text-sm text-red-700" role="alert">{searchError}</p>}
      </Panel>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_21rem]">
        <Panel>
          <h2 className="text-base font-semibold text-slate-900">Suministros registrados</h2>
          <p className="mt-1 text-sm text-slate-500">{suministros.length} {suministros.length === 1 ? 'suministro' : 'suministros'}</p>
          <div className="mt-5">
            {isLoading ? <LoadingState message="Cargando suministros..." /> : suministros.length === 0 ? <EmptyState description="Los suministros creados aparecerán aquí." title="No hay suministros registrados" /> : <SupplyList canManage={canManage} isSaving={isSaving} isQrLoading={qrLoading} onEdit={editSuministro} onHistory={showHistory} onProcessHistory={showProcessHistory} onQr={showQr} onResponsible={openResponsibleForm} onProcess={setPendingProcess} suministros={visibleItems} />}
          </div>
          {!isLoading && suministros.length > 5 && (
            <button className="mt-4 rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-[#28727a]" onClick={() => setShowAll((current) => !current)} type="button">
              {showAll ? 'Mostrar menos' : 'Mostrar más'}
            </button>
          )}
        </Panel>

        {canManage && responsibleForm.suministroId !== null && (
          <Panel>
            <h2 className="text-base font-semibold text-slate-900">{suministros.find((item) => item.id === responsibleForm.suministroId)?.responsableActual ? 'Cambiar responsable' : 'Asignar responsable'}</h2>
            <p className="mt-1 text-sm leading-6 text-slate-500">El NIS y la identidad del suministro se conservarán.</p>
            <select className="mt-5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" onChange={(event) => setResponsibleForm((current) => ({ ...current, personaId: event.target.value }))} value={responsibleForm.personaId}>
              <option value="">Selecciona una persona</option>
              {personas.map((persona) => <option key={persona.id} value={persona.id}>{persona.nombres} {persona.apellidos}</option>)}
            </select>
            <div className="mt-4 flex justify-end gap-2">
              <button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" onClick={() => setResponsibleForm({ suministroId: null, personaId: '' })} type="button">Cancelar</button>
              <button className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] disabled:opacity-60" disabled={isSaving} onClick={saveResponsible} type="button">{isSaving ? 'Guardando...' : 'Guardar responsable'}</button>
            </div>
          </Panel>
        )}

        {canManage && (
          <Panel className="h-fit xl:sticky xl:top-28">
          <h2 className="text-base font-semibold text-slate-900">{editingId === null ? 'Crear suministro' : 'Editar suministro'}</h2>
          <p className="mt-1 text-sm leading-6 text-slate-500">El NIS se genera automáticamente y no puede editarse.</p>
          <form className="mt-5 space-y-4" onSubmit={saveSuministro}>
            {editingId !== null && <div className="rounded-md bg-slate-50 px-3 py-2.5 text-sm"><span className="font-medium text-slate-500">NIS: </span><span className="font-semibold text-slate-900">{suministros.find((item) => item.id === editingId)?.nis}</span></div>}
            <div>
              <label className="text-sm font-medium text-slate-700" htmlFor="suministro-sector">Sector</label>
              <select className="mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" id="suministro-sector" name="sectorId" onChange={changeField} required value={form.sectorId}>
                <option value="">Selecciona un sector</option>
                {sectores.map((sector) => <option key={sector.id} value={sector.id}>{sector.nombre}</option>)}
              </select>
            </div>
            <div>
              <label className="text-sm font-medium text-slate-700" htmlFor="suministro-direccion">Dirección de referencia</label>
              <textarea className="mt-1.5 block min-h-24 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" id="suministro-direccion" maxLength="500" name="direccionReferencia" onChange={changeField} required rows="3" value={form.direccionReferencia} />
              <p className="mt-1 text-right text-xs text-slate-400">{form.direccionReferencia.length}/500</p>
            </div>
            {formError && <Alert message={formError} />}
            <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
              {editingId !== null && <button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" onClick={resetForm} type="button">Cancelar</button>}
              <button className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] hover:bg-[#c99a50] disabled:cursor-not-allowed disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Guardando...' : editingId === null ? 'Crear suministro' : 'Guardar cambios'}</button>
            </div>
          </form>
          </Panel>
        )}
      </div>

      {canManage && pendingProcess && <ProcessDialog isSaving={isSaving} onCancel={() => setPendingProcess(null)} onConfirm={processSupply} operation={pendingProcess.operation} suministro={pendingProcess.suministro} />}
      {canManage && pendingResponsible && <ResponsibleConfirmation isSaving={isSaving} onCancel={() => setPendingResponsible(null)} onConfirm={() => assignResponsible(pendingResponsible.suministro.id, pendingResponsible.persona.id)} persona={pendingResponsible.persona} suministro={pendingResponsible.suministro} />}
      {history && <HistoryDialog history={history.items} isLoading={historyLoading} onClose={() => setHistory(null)} suministro={history.suministro} />}
      {processHistory && <ProcessHistoryDialog history={processHistory.items} isLoading={processHistoryLoading} onClose={() => setProcessHistory(null)} suministro={processHistory.suministro} />}
      {qrModal && <QrDialog onClose={() => setQrModal(null)} qr={qrModal} />}
    </div>
  )
}

function SupplyList({ canManage, isQrLoading, isSaving, onEdit, onHistory, onProcessHistory, onQr, onResponsible, onProcess, suministros }) {
  return <>
    <div className="hidden overflow-x-auto md:block"><table className="w-full min-w-[920px] text-left text-sm"><thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500"><tr><th className="pb-3 pr-4 font-semibold">NIS</th><th className="pb-3 pr-4 font-semibold">Sector</th><th className="pb-3 pr-4 font-semibold">Dirección</th><th className="pb-3 pr-4 font-semibold">Responsable actual</th><th className="pb-3 pr-4 font-semibold">Estado</th><th className="pb-3 text-right font-semibold">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{suministros.map((suministro) => <SupplyRow canManage={canManage} isQrLoading={isQrLoading} isSaving={isSaving} key={suministro.id} onEdit={onEdit} onHistory={onHistory} onProcessHistory={onProcessHistory} onQr={onQr} onResponsible={onResponsible} onProcess={onProcess} suministro={suministro} />)}</tbody></table></div>
    <div className="space-y-3 md:hidden">{suministros.map((suministro) => <SupplyCard canManage={canManage} isQrLoading={isQrLoading} isSaving={isSaving} key={suministro.id} onEdit={onEdit} onHistory={onHistory} onProcessHistory={onProcessHistory} onQr={onQr} onResponsible={onResponsible} onProcess={onProcess} suministro={suministro} />)}</div>
  </>
}

function SupplyRow({ canManage, isQrLoading, isSaving, onEdit, onHistory, onProcessHistory, onQr, onResponsible, onProcess, suministro }) {
  return <tr><td className="py-4 pr-4 font-semibold text-slate-900">{suministro.nis}</td><td className="py-4 pr-4 text-slate-600">{suministro.sectorNombre}</td><td className="max-w-48 py-4 pr-4 text-slate-600">{suministro.direccionReferencia}</td><td className="py-4 pr-4 text-slate-600">{responsableLabel(suministro)}</td><td className="py-4 pr-4"><StatusBadge estado={suministro.estado} /></td><td className="py-4 text-right"><SupplyActions canManage={canManage} isQrLoading={isQrLoading} isSaving={isSaving} onEdit={onEdit} onHistory={onHistory} onProcessHistory={onProcessHistory} onQr={onQr} onResponsible={onResponsible} onProcess={onProcess} suministro={suministro} /></td></tr>
}

function SupplyCard({ canManage, isQrLoading, isSaving, onEdit, onHistory, onProcessHistory, onQr, onResponsible, onProcess, suministro }) {
  return <article className="rounded-md border border-slate-200 p-4"><div className="flex items-start justify-between gap-3"><div className="min-w-0"><h3 className="font-semibold text-slate-900">{suministro.nis}</h3><dl className="mt-2 space-y-1 text-sm text-slate-600"><div><dt className="inline font-medium text-slate-500">Sector: </dt><dd className="inline">{suministro.sectorNombre}</dd></div><div><dt className="inline font-medium text-slate-500">Dirección: </dt><dd className="inline">{suministro.direccionReferencia}</dd></div><div><dt className="inline font-medium text-slate-500">Responsable: </dt><dd className="inline">{responsableLabel(suministro)}</dd></div></dl></div><StatusBadge estado={suministro.estado} /></div><div className="mt-4 border-t border-slate-100 pt-3"><SupplyActions canManage={canManage} isQrLoading={isQrLoading} isSaving={isSaving} onEdit={onEdit} onHistory={onHistory} onProcessHistory={onProcessHistory} onQr={onQr} onResponsible={onResponsible} onProcess={onProcess} suministro={suministro} /></div></article>
}

function SupplyActions({ canManage, isQrLoading, isSaving, onEdit, onHistory, onProcessHistory, onQr, onResponsible, onProcess, suministro }) {
  return (
    <div className="flex flex-wrap justify-end gap-2">
      <button className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50" disabled={isQrLoading} onClick={() => onQr(suministro)} type="button">
        {isQrLoading ? 'Cargando...' : 'Ver QR'}
      </button>

      {canManage && (
        <button className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50" onClick={() => onEdit(suministro)} type="button">
          Editar
        </button>
      )}

      {canManage && (
        <button className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50" onClick={() => onResponsible(suministro)} type="button">
          {suministro.responsableActual ? 'Cambiar responsable' : 'Asignar responsable'}
        </button>
      )}

      <button className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50" onClick={() => onHistory(suministro)} type="button">
        Responsables
      </button>

      <button className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50" onClick={() => onProcessHistory(suministro)} type="button">
        Procesos
      </button>

      {canManage && (
        suministro.estado === 1 ? (
          <button className="rounded-md border border-[#28727a]/40 px-3 py-1.5 text-xs font-semibold text-[#1c5961] hover:bg-[#eef6f5] disabled:opacity-60" disabled={isSaving} onClick={() => onProcess({ operation: 'cancelar', suministro })} type="button">
            Cancelar
          </button>
        ) : (
          <button className="rounded-md border border-[#28727a]/40 px-3 py-1.5 text-xs font-semibold text-[#1c5961] hover:bg-[#eef6f5] disabled:opacity-60" disabled={isSaving} onClick={() => onProcess({ operation: 'reconectar', suministro })} type="button">
            Reconectar
          </button>
        )
      )}
    </div>
  )
}

function StatusBadge({ estado }) {
  const status = estado === 1 ? { label: 'Activo', className: 'bg-[#e3f2ed] text-[#17644e]', dot: 'bg-[#238568]' } : estado === 2 ? { label: 'Cancelado', className: 'bg-slate-100 text-slate-600', dot: 'bg-slate-400' } : { label: 'Estado desconocido', className: 'bg-slate-100 text-slate-600', dot: 'bg-slate-400' }
  return <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${status.className}`}><span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${status.dot}`} />{status.label}</span>
}

function RelationshipStatusBadge({ estado }) {
  const isCurrent = estado === 1
  return <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${isCurrent ? 'bg-[#e3f2ed] text-[#17644e]' : 'bg-slate-100 text-slate-600'}`}><span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${isCurrent ? 'bg-[#238568]' : 'bg-slate-400'}`} />{isCurrent ? 'Vigente' : 'Finalizada'}</span>
}

function ProcessDialog({ isSaving, onCancel, onConfirm, operation, suministro }) {
  const [form, setForm] = useState({ motivo: '', observacion: '' })
  const [error, setError] = useState(null)
  if (!suministro) return null
  const isCancellation = operation === 'cancelar'

  function submit(event) {
    event.preventDefault()
    const motivo = form.motivo.trim()
    const observacion = form.observacion.trim()
    if (!motivo) return setError('El motivo es obligatorio.')
    if (motivo.length > 250) return setError('El motivo no puede superar 250 caracteres.')
    if (observacion.length > 1000) return setError('La observación no puede superar 1000 caracteres.')
    setError(null)
    onConfirm({ motivo, observacion })
  }

  return <div aria-labelledby="process-dialog-title" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/40 p-4" role="dialog"><div className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-md overflow-y-auto rounded-lg bg-white p-6 shadow-xl"><h2 className="text-lg font-semibold text-slate-900" id="process-dialog-title">{isCancellation ? 'Cancelar suministro' : 'Reconectar suministro'}</h2><p className="mt-2 text-sm leading-6 text-slate-600">NIS: <strong>{suministro.nis}</strong></p><form className="mt-5 space-y-4" onSubmit={submit}><div><label className="text-sm font-medium text-slate-700" htmlFor="process-motivo">Motivo <span className="text-red-700">*</span></label><textarea autoFocus className="mt-1.5 block min-h-24 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" id="process-motivo" maxLength="250" onChange={(event) => setForm((current) => ({ ...current, motivo: event.target.value }))} required rows="3" value={form.motivo} /><p className="mt-1 text-right text-xs text-slate-400">{form.motivo.length}/250</p></div><div><label className="text-sm font-medium text-slate-700" htmlFor="process-observacion">Observación <span className="font-normal text-slate-400">(opcional)</span></label><textarea className="mt-1.5 block min-h-24 w-full resize-y rounded-md border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" id="process-observacion" maxLength="1000" onChange={(event) => setForm((current) => ({ ...current, observacion: event.target.value }))} rows="3" value={form.observacion} /><p className="mt-1 text-right text-xs text-slate-400">{form.observacion.length}/1000</p></div>{error && <p className="text-sm text-red-700" role="alert">{error}</p>}<div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={onCancel} type="button">Volver</button><button className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Guardando...' : isCancellation ? 'Confirmar cancelación' : 'Confirmar reconexión'}</button></div></form></div></div>
}

function ResponsibleConfirmation({ isSaving, onCancel, onConfirm, persona, suministro }) {
  if (!persona || !suministro?.responsableActual) return null
  return <div aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4" role="dialog"><div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl"><h2 className="text-lg font-semibold text-slate-900">¿Quieres cambiar el responsable?</h2><p className="mt-2 text-sm leading-6 text-slate-600">{suministro.responsableActual.nombres} {suministro.responsableActual.apellidos} será reemplazado por {persona.nombres} {persona.apellidos}.</p><p className="mt-2 text-sm text-slate-500">El NIS y la identidad del suministro se conservarán.</p><div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" onClick={onCancel} type="button">Cancelar</button><button className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60" disabled={isSaving} onClick={onConfirm} type="button">{isSaving ? 'Actualizando...' : 'Confirmar cambio'}</button></div></div></div>
}

function QrDialog({ onClose, qr }) {
  return <div aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4" role="dialog"><div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl"><div className="flex items-start justify-between gap-4"><div><h2 className="text-lg font-semibold text-slate-900">Código QR del suministro</h2><p className="mt-1 text-sm text-slate-500">NIS: {qr.nis}</p></div><button aria-label="Cerrar código QR" className="rounded-md px-2 py-1 text-sm font-semibold text-slate-500 hover:bg-slate-100" onClick={onClose} type="button">Cerrar</button></div><div className="mt-6 flex justify-center rounded-md border border-slate-200 bg-white p-5"><QRCodeSVG bgColor="#ffffff" fgColor="#123b43" includeMargin level="M" size={240} value={qr.qrValue} /></div><p className="mt-4 text-center text-sm leading-6 text-slate-600">Este código identifica técnicamente el suministro. No contiene información personal ni financiera.</p></div></div>
}

function HistoryDialog({ history, isLoading, onClose, suministro }) {
  return <div aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4" role="dialog"><div className="max-h-[85vh] w-full max-w-2xl overflow-y-auto rounded-lg bg-white p-6 shadow-xl"><div className="flex items-start justify-between gap-4"><div><h2 className="text-lg font-semibold text-slate-900">Historial de responsables</h2><p className="mt-1 text-sm text-slate-500">{suministro.nis}</p></div><button aria-label="Cerrar historial" className="rounded-md px-2 py-1 text-sm font-semibold text-slate-500 hover:bg-slate-100" onClick={onClose} type="button">Cerrar</button></div>{isLoading ? <div className="mt-5"><LoadingState message="Cargando historial..." /></div> : history.length === 0 ? <div className="mt-5"><EmptyState description="Este suministro aún no tiene responsables registrados." title="Sin historial" /></div> : <div className="mt-5 space-y-3">{history.map((item) => <div className="rounded-md border border-slate-200 p-4" key={item.personaSuministroId}><div className="flex flex-wrap items-start justify-between gap-3"><div><p className="font-semibold text-slate-900">{item.nombres} {item.apellidos}</p><p className="mt-1 text-sm text-slate-500">Inicio: {formatGuatemalaDateTime(item.fechaInicio)}</p><p className="text-sm text-slate-500">Fin: {item.fechaFin ? formatGuatemalaDateTime(item.fechaFin) : 'Actual'}</p></div><RelationshipStatusBadge estado={item.estado} /></div></div>)}</div>}</div></div>
}

function ProcessHistoryDialog({ history, isLoading, onClose, suministro }) {
  return <div aria-labelledby="process-history-title" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/40 p-4" role="dialog"><div className="my-4 max-h-[85vh] w-full max-w-2xl overflow-y-auto rounded-lg bg-white p-6 shadow-xl"><div className="flex items-start justify-between gap-4"><div><h2 className="text-lg font-semibold text-slate-900" id="process-history-title">Historial de procesos</h2><p className="mt-1 text-sm text-slate-500">NIS: {suministro.nis}</p></div><button aria-label="Cerrar historial de procesos" className="rounded-md px-2 py-1 text-sm font-semibold text-slate-500 hover:bg-slate-100" onClick={onClose} type="button">Cerrar</button></div>{isLoading ? <div className="mt-5"><LoadingState message="Cargando procesos..." /></div> : history.length === 0 ? <div className="mt-5"><EmptyState description="Este suministro aún no tiene cancelaciones ni reconexiones registradas." title="Sin procesos" /></div> : <div className="mt-5 space-y-3">{history.map((item) => <article className="rounded-md border border-slate-200 p-4" key={item.procesoSuministroId}><div className="flex flex-wrap items-start justify-between gap-3"><div><p className="font-semibold text-slate-900">{item.tipoProceso === 1 ? 'Cancelación' : 'Reconexión'}</p><p className="mt-1 text-sm text-slate-600">{estadoLabel(item.estadoAnterior)} → {estadoLabel(item.estadoNuevo)}</p><p className="mt-1 text-sm text-slate-500">{formatGuatemalaDateTime(item.fecha)} · {item.usuario}</p></div></div><dl className="mt-3 space-y-1 text-sm text-slate-600"><div><dt className="inline font-medium text-slate-500">Motivo: </dt><dd className="inline">{item.motivo}</dd></div>{item.observacion && <div><dt className="inline font-medium text-slate-500">Observación: </dt><dd className="inline">{item.observacion}</dd></div>}</dl></article>)}</div>}</div></div>
}

function Alert({ message, onDismiss }) {
  return <div aria-live="polite" className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"><span>{message}</span>{onDismiss && <button aria-label="Cerrar mensaje" className="shrink-0 font-semibold text-red-700" onClick={onDismiss} type="button">Cerrar</button>}</div>
}

function responsableLabel(suministro) {
  return suministro.responsableActual ? `${suministro.responsableActual.nombres} ${suministro.responsableActual.apellidos}` : 'Sin responsable'
}

function estadoLabel(estado) {
  return estado === 1 ? 'Activo' : estado === 2 ? 'Cancelado' : 'Desconocido'
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400) return 'Los datos proporcionados no son válidos.'
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return 'El suministro ya no existe.'
  if (error?.status === 409) return 'El estado del suministro cambió y esta operación ya no puede realizarse.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

function compareSuministros(first, second) {
  return first.nis.localeCompare(second.nis) || first.id - second.id
}

export default SuministrosPage
