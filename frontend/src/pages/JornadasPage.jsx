import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import { PERMISOS } from '../app/permissions.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import {
  addJornadaParticipants,
  cancelJornada,
  closeJornada,
  createJornada,
  getJornadaById,
  getJornadas,
  removeJornadaParticipant,
  updateJornada,
  updateJornadaParticipant,
} from '../services/jornadaService.js'
import { getPersonas } from '../services/personaService.js'
import { formatCivilDate, formatCivilTime } from '../utils/dateTime.js'

const initialForm = {
  nombre: '',
  descripcion: '',
  fecha: '',
  horaInicio: '',
  horaFin: '',
  ubicacion: '',
  montoIncumplimiento: '',
}

function JornadasPage() {
  const { authenticatedRequest, hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.JORNADAS_GESTIONAR)
  const requestRef = useRef(authenticatedRequest)

  const [jornadas, setJornadas] = useState([])
  const [search, setSearch] = useState('')
  const normalizedSearch = search.trim().toLowerCase()
  const filteredItems = jornadas.filter((item) =>
    (item.nombre ?? '').toLowerCase().includes(normalizedSearch),
  )
  if (filteredItems.some((item, index) => index > 0 &&
    Date.parse(item.fecha) > Date.parse(filteredItems[index - 1].fecha))) {
    filteredItems.sort((first, second) => Date.parse(second.fecha) - Date.parse(first.fecha))
  }

  const [personas, setPersonas] = useState([])
  const [selected, setSelected] = useState(null)
  const [form, setForm] = useState(initialForm)
  const [editingId, setEditingId] = useState(null)
  const [participantId, setParticipantId] = useState('')
  const [pendingCancel, setPendingCancel] = useState(false)
  const [pendingClose, setPendingClose] = useState(false)

  const [isLoading, setIsLoading] = useState(true)
  const [isDetailLoading, setIsDetailLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)
  const [feedback, setFeedback] = useState(null)

  useEffect(() => {
    let mounted = true

    async function load() {
      setIsLoading(true)
      setError(null)

      const [jornadaResult, personaResult] = await Promise.allSettled([
        getJornadas(requestRef.current),
        getPersonas(requestRef.current),
      ])

      if (!mounted) return

      if (jornadaResult.status === 'fulfilled') {
        setJornadas(Array.isArray(jornadaResult.value) ? jornadaResult.value : [])
      } else {
        setError(getRequestMessage(jornadaResult.reason, 'No se pudieron cargar las jornadas.'))
      }

      if (personaResult.status === 'fulfilled') {
        setPersonas(
          (Array.isArray(personaResult.value) ? personaResult.value : [])
            .filter((persona) => persona.estado === 1)
            .sort(comparePersonas),
        )
      }

      setIsLoading(false)
    }

    load()
    return () => { mounted = false }
  }, [])

  function changeField(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function resetForm() {
    setForm(initialForm)
    setEditingId(null)
    setFormError(null)
  }

  function startEdit(jornada) {
    setEditingId(jornada.id)
    setForm({
      nombre: jornada.nombre || '',
      descripcion: jornada.descripcion || '',
      fecha: dateInputValue(jornada.fecha),
      horaInicio: timeInputValue(jornada.horaInicio),
      horaFin: timeInputValue(jornada.horaFin),
      ubicacion: jornada.ubicacion || '',
      montoIncumplimiento: jornada.montoIncumplimiento == null
        ? ''
        : String(jornada.montoIncumplimiento),
    })
    setFormError(null)
    window.scrollTo({ behavior: 'smooth', top: 0 })
  }

  function validateForm() {
    if (!form.nombre.trim()) return 'El nombre de la jornada es obligatorio.'
    if (form.nombre.trim().length > 150) return 'El nombre no puede superar 150 caracteres.'
    if (form.descripcion.trim().length > 500) return 'La descripción no puede superar 500 caracteres.'
    if (!form.fecha) return 'La fecha de la jornada es obligatoria.'
    if (form.ubicacion.trim().length > 200) return 'La ubicación no puede superar 200 caracteres.'

    if (form.horaInicio && form.horaFin && form.horaFin <= form.horaInicio) {
      return 'La hora de finalización debe ser posterior a la hora de inicio.'
    }

    if (form.montoIncumplimiento.trim()) {
      const amount = form.montoIncumplimiento.trim()
      if (!/^\d+(?:\.\d{1,2})?$/.test(amount) || Number(amount) <= 0) {
        return 'El monto por ausencia debe ser mayor que cero y tener hasta dos decimales.'
      }
    }

    return null
  }

  async function saveJornada(event) {
    event.preventDefault()

    const validationError = validateForm()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    setFeedback(null)

    const payload = {
      nombre: form.nombre.trim(),
      descripcion: form.descripcion.trim() || null,
      fecha: `${form.fecha}T00:00:00`,
      horaInicio: form.horaInicio ? `${form.horaInicio}:00` : null,
      horaFin: form.horaFin ? `${form.horaFin}:00` : null,
      ubicacion: form.ubicacion.trim() || null,
      montoIncumplimiento: form.montoIncumplimiento.trim()
        ? Number(form.montoIncumplimiento)
        : null,
    }

    try {
      const saved = editingId === null
        ? await createJornada(authenticatedRequest, payload)
        : await updateJornada(authenticatedRequest, editingId, payload)

      upsertSummary(saved)
      if (selected?.id === saved.id) setSelected(saved)

      setFeedback(editingId === null
        ? 'Jornada creada correctamente.'
        : 'Jornada actualizada correctamente.')

      resetForm()
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo guardar la jornada.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function openDetail(id) {
    setIsDetailLoading(true)
    setError(null)

    try {
      const detail = await getJornadaById(authenticatedRequest, id)
      setSelected(detail)
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo cargar el detalle de la jornada.'))
    } finally {
      setIsDetailLoading(false)
    }
  }

  async function addParticipant() {
    if (!selected || !participantId) return

    setIsSaving(true)
    setError(null)
    setFeedback(null)

    try {
      const updated = await addJornadaParticipants(
        authenticatedRequest,
        selected.id,
        [Number(participantId)],
      )
      setSelected(updated)
      upsertSummary(updated)
      setParticipantId('')
      setFeedback('Participante agregado correctamente.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo agregar el participante.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function removeParticipant(personaId) {
    if (!selected) return

    setIsSaving(true)
    setError(null)
    setFeedback(null)

    try {
      const updated = await removeJornadaParticipant(
        authenticatedRequest,
        selected.id,
        personaId,
      )
      setSelected(updated)
      upsertSummary(updated)
      setFeedback('Participante retirado correctamente.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo retirar el participante.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function updateParticipant(personaId, resultado, observacion) {
    if (!selected) return

    setIsSaving(true)
    setError(null)
    setFeedback(null)

    try {
      const updated = await updateJornadaParticipant(
        authenticatedRequest,
        selected.id,
        personaId,
        { resultado: Number(resultado), observacion: observacion.trim() || null },
      )
      setSelected(updated)
      setFeedback('Participación actualizada correctamente.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo actualizar la participación.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmCancel(motivo) {
    if (!selected) return

    setIsSaving(true)
    setError(null)
    setFeedback(null)

    try {
      const updated = await cancelJornada(authenticatedRequest, selected.id, motivo)
      setSelected(updated)
      upsertSummary(updated)
      setPendingCancel(false)
      setFeedback('Jornada cancelada correctamente.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo cancelar la jornada.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmClose() {
    if (!selected) return

    setIsSaving(true)
    setError(null)
    setFeedback(null)

    try {
      const updated = await closeJornada(authenticatedRequest, selected.id)
      setSelected(updated)
      upsertSummary(updated)
      setPendingClose(false)
      setFeedback('Jornada cerrada correctamente. Las obligaciones aplicables fueron generadas por el servidor.')
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo cerrar la jornada.'))
    } finally {
      setIsSaving(false)
    }
  }

  function upsertSummary(jornada) {
    setJornadas((current) => {
      const summary = {
        ...jornada,
        cantidadParticipantes: jornada.participantes?.length
          ?? jornada.cantidadParticipantes
          ?? 0,
      }

      return [
        summary,
        ...current.filter((item) => item.id !== jornada.id),
      ].sort(compareJornadas)
    })
  }

  const availablePersonas = personas.filter(
    (persona) => !selected?.participantes?.some((item) => item.personaId === persona.id),
  )

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={canManage ? (
          <button
            className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33]"
            onClick={resetForm}
            type="button"
          >
            Nueva jornada
          </button>
        ) : null}
        description="Programa jornadas comunitarias, registra participantes y asistencia, y cierra la actividad para generar las obligaciones que correspondan."
        eyebrow="Gestión comunitaria"
        title="Jornadas"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}
      {feedback && <SuccessAlert message={feedback} />}

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <Panel>
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-base font-semibold text-slate-900">Jornadas registradas</h2>
              <p className="mt-1 text-sm text-slate-500">
                {jornadas.length} {jornadas.length === 1 ? 'jornada' : 'jornadas'}
              </p>
            </div>
          </div>

          <input aria-label="Buscar jornada" className="mt-4 block min-h-11 w-full max-w-sm rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20" onChange={(event) => { setSearch(event.target.value) }} placeholder="Buscar jornada..." type="search" value={search} />
          <div className="mt-5">
            {isLoading ? (
              <LoadingState message="Cargando jornadas..." />
            ) : jornadas.length === 0 ? (
              <EmptyState
                description="Crea la primera jornada comunitaria para comenzar a registrar participantes."
                title="Aún no hay jornadas"
              />
            ) : filteredItems.length === 0 ? (
              <EmptyState description="Prueba con otro texto de búsqueda." title="No hay coincidencias" />
            ) : (
              <JornadaList
                jornadas={filteredItems}
                onEdit={startEdit}
                onOpen={openDetail}
              />
            )}
          </div>
        </Panel>

        {canManage && (
          <Panel className="h-fit xl:sticky xl:top-28">
          <h2 className="text-base font-semibold text-slate-900">
            {editingId === null ? 'Crear jornada' : 'Editar jornada'}
          </h2>
          <p className="mt-1 text-sm leading-6 text-slate-500">
            El monto es opcional. Si no se configura, las ausencias no generarán obligación económica.
          </p>

          <form className="mt-5 space-y-4" onSubmit={saveJornada}>
            <Field inputId="jornada-nombre" label="Nombre">
              <input
                className={inputClass}
                id="jornada-nombre"
                maxLength="150"
                name="nombre"
                onChange={changeField}
                required
                value={form.nombre}
              />
            </Field>

            <Field inputId="jornada-descripcion" label="Descripción" optional>
              <textarea
                className={`${inputClass} min-h-24 resize-y`}
                id="jornada-descripcion"
                maxLength="500"
                name="descripcion"
                onChange={changeField}
                rows="3"
                value={form.descripcion}
              />
            </Field>

            <Field inputId="jornada-fecha" label="Fecha">
              <input
                className={inputClass}
                id="jornada-fecha"
                name="fecha"
                onChange={changeField}
                required
                type="date"
                value={form.fecha}
              />
            </Field>

            <div className="grid grid-cols-2 gap-3">
              <Field inputId="jornada-inicio" label="Inicio" optional>
                <input
                  className={inputClass}
                  id="jornada-inicio"
                  name="horaInicio"
                  onChange={changeField}
                  type="time"
                  value={form.horaInicio}
                />
              </Field>
              <Field inputId="jornada-fin" label="Fin" optional>
                <input
                  className={inputClass}
                  id="jornada-fin"
                  name="horaFin"
                  onChange={changeField}
                  type="time"
                  value={form.horaFin}
                />
              </Field>
            </div>

            <Field inputId="jornada-ubicacion" label="Ubicación" optional>
              <input
                className={inputClass}
                id="jornada-ubicacion"
                maxLength="200"
                name="ubicacion"
                onChange={changeField}
                value={form.ubicacion}
              />
            </Field>

            <Field inputId="jornada-monto" label="Monto por ausencia (Q)" optional>
              <input
                className={inputClass}
                id="jornada-monto"
                inputMode="decimal"
                name="montoIncumplimiento"
                onChange={changeField}
                placeholder="Ej. 25.00"
                value={form.montoIncumplimiento}
              />
            </Field>

            {formError && <Alert message={formError} />}

            <div className="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:justify-end">
              {editingId !== null && (
                <button
                  className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
                  disabled={isSaving}
                  onClick={resetForm}
                  type="button"
                >
                  Cancelar edición
                </button>
              )}
              <button
                className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] disabled:opacity-60"
                disabled={isSaving}
                type="submit"
              >
                {isSaving ? 'Guardando...' : editingId === null ? 'Crear jornada' : 'Guardar cambios'}
              </button>
            </div>
          </form>
          </Panel>
        )}
      </div>

      {isDetailLoading && <Panel><LoadingState message="Cargando detalle..." /></Panel>}

      {selected && !isDetailLoading && (
        <JornadaDetail
          availablePersonas={availablePersonas}
          canManage={canManage}
          isSaving={isSaving}
          jornada={selected}
          onAddParticipant={addParticipant}
          onCancel={() => setPendingCancel(true)}
          onClose={() => setPendingClose(true)}
          onParticipantChange={updateParticipant}
          onParticipantRemove={removeParticipant}
          participantId={participantId}
          setParticipantId={setParticipantId}
        />
      )}

      {canManage && pendingCancel && selected && (
        <CancelDialog
          isSaving={isSaving}
          jornada={selected}
          onCancel={() => setPendingCancel(false)}
          onConfirm={confirmCancel}
        />
      )}

      {canManage && pendingClose && selected && (
        <CloseDialog
          isSaving={isSaving}
          jornada={selected}
          onCancel={() => setPendingClose(false)}
          onConfirm={confirmClose}
        />
      )}
    </div>
  )
}

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:cursor-not-allowed disabled:bg-slate-100'

function JornadaList({ jornadas, onEdit, onOpen }) {
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.JORNADAS_GESTIONAR)
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[760px] text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="pb-3 pr-4 font-semibold">Jornada</th>
              <th className="pb-3 pr-4 font-semibold">Fecha</th>
              <th className="pb-3 pr-4 font-semibold">Monto</th>
              <th className="pb-3 pr-4 font-semibold">Participantes</th>
              <th className="pb-3 pr-4 font-semibold">Estado</th>
              <th className="pb-3 text-right font-semibold">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {jornadas.map((jornada) => (
              <tr key={jornada.id}>
                <td className="py-4 pr-4">
                  <p className="font-semibold text-slate-900">{jornada.nombre}</p>
                  <p className="mt-1 text-xs text-slate-500">{jornada.ubicacion || 'Sin ubicación'}</p>
                </td>
                <td className="py-4 pr-4 text-slate-600">{formatCivilDate(jornada.fecha)}</td>
                <td className="py-4 pr-4 text-slate-700">
                  {jornada.montoIncumplimiento == null ? 'Sin penalización' : formatMoney(jornada.montoIncumplimiento)}
                </td>
                <td className="py-4 pr-4 text-slate-600">{jornada.cantidadParticipantes ?? 0}</td>
                <td className="py-4 pr-4"><JornadaStatus estado={jornada.estado} /></td>
                <td className="py-4">
                  <div className="flex justify-end gap-2">
                    <button className={secondaryButton} onClick={() => onOpen(jornada.id)} type="button">Gestionar</button>
                    {canManage && jornada.estado === 1 && <button className={secondaryButton} onClick={() => onEdit(jornada)} type="button">Editar</button>}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="space-y-3 md:hidden">
        {jornadas.map((jornada) => (
          <article className="rounded-md border border-slate-200 p-4" key={jornada.id}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="font-semibold text-slate-900">{jornada.nombre}</h3>
                <p className="mt-1 text-sm text-slate-500">{formatCivilDate(jornada.fecha)}</p>
              </div>
              <JornadaStatus estado={jornada.estado} />
            </div>
            <dl className="mt-3 grid grid-cols-2 gap-3 rounded-md bg-slate-50 p-3 text-sm">
              <Data label="Ubicación" value={jornada.ubicacion || '—'} />
              <Data label="Participantes" value={jornada.cantidadParticipantes ?? 0} />
              <Data
                label="Monto ausencia"
                value={jornada.montoIncumplimiento == null ? 'Sin penalización' : formatMoney(jornada.montoIncumplimiento)}
              />
            </dl>
            <div className="mt-4 flex flex-wrap justify-end gap-2 border-t border-slate-100 pt-3">
              <button className={secondaryButton} onClick={() => onOpen(jornada.id)} type="button">Gestionar</button>
              {canManage && jornada.estado === 1 && <button className={secondaryButton} onClick={() => onEdit(jornada)} type="button">Editar</button>}
            </div>
          </article>
        ))}
      </div>
    </>
  )
}

function JornadaDetail({
  availablePersonas,
  canManage,
  isSaving,
  jornada,
  onAddParticipant,
  onCancel,
  onClose,
  onParticipantChange,
  onParticipantRemove,
  participantId,
  setParticipantId,
}) {
  const planificada = jornada.estado === 1
  const pendingCount = jornada.participantes.filter((item) => item.resultado === 0).length

  return (
    <Panel>
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-lg font-semibold text-slate-900">{jornada.nombre}</h2>
            <JornadaStatus estado={jornada.estado} />
          </div>
          <p className="mt-2 text-sm leading-6 text-slate-600">{jornada.descripcion || 'Sin descripción.'}</p>
          <p className="mt-2 text-xs text-slate-500">
            {formatCivilDate(jornada.fecha)}
            {jornada.horaInicio ? ` · ${formatCivilTime(jornada.horaInicio)}` : ''}
            {jornada.horaFin ? `–${formatCivilTime(jornada.horaFin)}` : ''}
            {jornada.ubicacion ? ` · ${jornada.ubicacion}` : ''}
          </p>
        </div>

        {canManage && planificada && (
          <div className="flex flex-wrap gap-2">
            <button className="rounded-md border border-red-200 px-3 py-2 text-sm font-semibold text-red-700 hover:bg-red-50" disabled={isSaving} onClick={onCancel} type="button">Cancelar jornada</button>
            <button className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60" disabled={isSaving} onClick={onClose} type="button">Cerrar jornada</button>
          </div>
        )}
      </div>

      <div className="mt-5 grid gap-3 sm:grid-cols-3">
        <Metric label="Participantes" value={jornada.participantes.length} />
        <Metric label="Pendientes" value={pendingCount} />
        <Metric
          label="Monto por ausencia"
          value={jornada.montoIncumplimiento == null ? 'No aplica' : formatMoney(jornada.montoIncumplimiento)}
        />
      </div>

      {canManage && planificada && (
        <div className="mt-6 rounded-md border border-slate-200 bg-slate-50 p-4">
          <label className="text-sm font-medium text-slate-700" htmlFor="jornada-participante">
            Agregar participante
          </label>
          <div className="mt-2 flex flex-col gap-2 sm:flex-row">
            <select
              className={`${inputClass} mt-0 flex-1`}
              id="jornada-participante"
              onChange={(event) => setParticipantId(event.target.value)}
              value={participantId}
            >
              <option value="">Selecciona una persona activa</option>
              {availablePersonas.map((persona) => (
                <option key={persona.id} value={persona.id}>
                  {persona.nombres} {persona.apellidos}
                </option>
              ))}
            </select>
            <button
              className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] disabled:opacity-60"
              disabled={isSaving || !participantId}
              onClick={onAddParticipant}
              type="button"
            >
              Agregar
            </button>
          </div>
        </div>
      )}

      <div className="mt-6">
        <h3 className="text-base font-semibold text-slate-900">Participación</h3>

        {jornada.participantes.length === 0 ? (
          <div className="mt-4">
            <EmptyState
              description={planificada ? 'Agrega las personas convocadas antes de cerrar la jornada.' : 'Esta jornada no tiene participantes registrados.'}
              title="Sin participantes"
            />
          </div>
        ) : (
          <div className="mt-4 space-y-3">
            {jornada.participantes.map((participant) => (
              <ParticipantCard
                canManage={canManage}
                isSaving={isSaving}
                key={participant.id}
                onChange={onParticipantChange}
                onRemove={onParticipantRemove}
                participant={participant}
                planificada={planificada}
              />
            ))}
          </div>
        )}
      </div>
    </Panel>
  )
}

function ParticipantCard({ canManage, isSaving, onChange, onRemove, participant, planificada }) {
  const [resultado, setResultado] = useState(String(participant.resultado))
  const [observacion, setObservacion] = useState(participant.observacion || '')


  return (
    <article className="rounded-md border border-slate-200 p-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <p className="font-semibold text-slate-900">{participant.nombrePersona}</p>
          <div className="mt-2 flex flex-wrap gap-2">
            <ParticipationStatus resultado={participant.resultado} />
            {participant.obligacionId && (
              <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-800">
                Obligación #{participant.obligacionId}
              </span>
            )}
          </div>
        </div>

        {canManage && planificada ? (
          <div className="grid w-full gap-3 lg:max-w-2xl lg:grid-cols-[12rem_minmax(0,1fr)_auto]">
            <select
              className={`${inputClass} mt-0`}
              disabled={isSaving}
              onChange={(event) => setResultado(event.target.value)}
              value={resultado}
            >
              <option value="0">Pendiente</option>
              <option value="1">Participó</option>
              <option value="2">Ausencia</option>
              {/* <option value="3">Ausencia justificada</option> */}
            </select>

            <input
              className={`${inputClass} mt-0`}
              disabled={isSaving}
              maxLength="500"
              onChange={(event) => setObservacion(event.target.value)}
              placeholder="Observación opcional"
              value={observacion}
            />

            <div className="flex gap-2">
              <button
                className={secondaryButton}
                disabled={isSaving}
                onClick={() => onChange(participant.personaId, resultado, observacion)}
                type="button"
              >
                Guardar
              </button>
              <button
                className="rounded-md border border-red-200 px-3 py-2 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:opacity-60"
                disabled={isSaving}
                onClick={() => onRemove(participant.personaId)}
                type="button"
              >
                Retirar
              </button>
            </div>
          </div>
        ) : (
          <p className="text-sm text-slate-500">{participant.observacion || 'Sin observación'}</p>
        )}
      </div>
    </article>
  )
}

function CancelDialog({ isSaving, jornada, onCancel, onConfirm }) {
  const [motivo, setMotivo] = useState('')
  const [dialogError, setDialogError] = useState(null)

  function submit(event) {
    event.preventDefault()
    if (!motivo.trim()) {
      setDialogError('El motivo de cancelación es obligatorio.')
      return
    }
    onConfirm(motivo.trim())
  }

  return (
    <Dialog title="Cancelar jornada">
      <p className="text-sm leading-6 text-slate-600">
        <strong>{jornada.nombre}</strong> quedará Cancelada. No se generarán obligaciones.
      </p>
      <form className="mt-5 space-y-4" onSubmit={submit}>
        <Field inputId="jornada-cancel-reason" label="Motivo">
          <textarea
            autoFocus
            className={`${inputClass} min-h-24 resize-y`}
            id="jornada-cancel-reason"
            onChange={(event) => setMotivo(event.target.value)}
            value={motivo}
          />
        </Field>
        {dialogError && <Alert message={dialogError} />}
        <DialogActions
          confirmLabel="Confirmar cancelación"
          danger
          isSaving={isSaving}
          onCancel={onCancel}
        />
      </form>
    </Dialog>
  )
}

function CloseDialog({ isSaving, jornada, onCancel, onConfirm }) {
  const pending = jornada.participantes.filter((item) => item.resultado === 0).length

  return (
    <Dialog title="Cerrar jornada">
      <p className="text-sm leading-6 text-slate-600">
        Al cerrar <strong>{jornada.nombre}</strong>, el servidor generará obligaciones únicamente por ausencias injustificadas cuando exista un monto configurado.
      </p>
      <div className="mt-4 rounded-md bg-slate-50 p-4 text-sm text-slate-600">
        <p>Participantes: <strong>{jornada.participantes.length}</strong></p>
        <p className="mt-1">Pendientes: <strong>{pending}</strong></p>
      </div>
      {pending > 0 && (
        <div className="mt-4">
          <Alert message="No puedes cerrar mientras existan participantes pendientes." />
        </div>
      )}
      <div className="mt-6">
        <DialogActions
          confirmLabel="Cerrar jornada"
          disabled={pending > 0 || jornada.participantes.length === 0}
          isSaving={isSaving}
          onCancel={onCancel}
          onConfirm={onConfirm}
        />
      </div>
    </Dialog>
  )
}

function Dialog({ children, title }) {
  return (
    <div aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/40 p-4" role="dialog">
      <div className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-lg overflow-y-auto rounded-lg bg-white p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
        <div className="mt-3">{children}</div>
      </div>
    </div>
  )
}

function DialogActions({ confirmLabel, danger = false, disabled = false, isSaving, onCancel, onConfirm }) {
  return (
    <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
      <button className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" disabled={isSaving} onClick={onCancel} type="button">Volver</button>
      <button
        className={`rounded-md px-4 py-2 text-sm font-semibold text-white disabled:opacity-60 ${danger ? 'bg-red-700 hover:bg-red-800' : 'bg-[#123b43] hover:bg-[#0d2d33]'}`}
        disabled={isSaving || disabled}
        onClick={onConfirm}
        type={onConfirm ? 'button' : 'submit'}
      >
        {isSaving ? 'Procesando...' : confirmLabel}
      </button>
    </div>
  )
}

function JornadaStatus({ estado }) {
  const status = estado === 1
    ? { label: 'Planificada', classes: 'bg-blue-50 text-blue-700' }
    : estado === 2
      ? { label: 'Cerrada', classes: 'bg-[#e3f2ed] text-[#17644e]' }
      : estado === 3
        ? { label: 'Cancelada', classes: 'bg-slate-100 text-slate-600' }
        : { label: 'Desconocida', classes: 'bg-slate-100 text-slate-600' }

  return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${status.classes}`}>{status.label}</span>
}

function ParticipationStatus({ resultado }) {
  const label = resultado === 0
    ? 'Pendiente'
    : resultado === 1
      ? 'Participó'
      : resultado === 2
        ? 'Ausencia'
        : resultado === 3
          ? 'Ausencia justificada'
          : 'Desconocido'

  return <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700">{label}</span>
}

function Metric({ label, value }) {
  return <div className="rounded-md bg-slate-50 p-4"><p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p><p className="mt-1 text-lg font-semibold text-slate-900">{value}</p></div>
}

function Field({ children, inputId, label, optional = false }) {
  return <div><label className="text-sm font-medium text-slate-700" htmlFor={inputId}>{label} {optional && <span className="font-normal text-slate-400">(opcional)</span>}</label>{children}</div>
}

function Data({ label, value }) {
  return <div><dt className="text-xs text-slate-500">{label}</dt><dd className="mt-1 text-slate-700">{value}</dd></div>
}

function Alert({ message, onDismiss }) {
  return <div aria-live="polite" className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"><span>{message}</span>{onDismiss && <button className="font-semibold text-red-700" onClick={onDismiss} type="button">Cerrar</button>}</div>
}

function SuccessAlert({ message }) {
  return <div aria-live="polite" className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">{message}</div>
}

const secondaryButton = 'rounded-md border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60'

function formatMoney(value) {
  return new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'GTQ' }).format(value)
}

function dateInputValue(value) {
  return value ? String(value).slice(0, 10) : ''
}

function timeInputValue(value) {
  return value ? String(value).slice(0, 5) : ''
}

function compareJornadas(first, second) {
  return String(second.fecha).localeCompare(String(first.fecha)) || second.id - first.id
}

function comparePersonas(first, second) {
  return `${first.apellidos} ${first.nombres}`.localeCompare(
    `${second.apellidos} ${second.nombres}`,
    'es',
    { sensitivity: 'base' },
  )
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400 || error?.status === 409) return error.message || fallback
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return error.message || 'El recurso solicitado ya no existe.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

export default JornadasPage
