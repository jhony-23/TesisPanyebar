import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import ReasonField from '../components/ui/ReasonField.jsx'
import { useAuth } from '../app/useAuth.js'
import { PERMISOS } from '../app/permissions.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import {
  cancelProgramacion,
  completeProgramacion,
  createProgramacion,
  createProgramacionRecurrente,
  getProgramaciones,
  updateProgramacion,
} from '../services/abastecimientoService.js'
import { getSectores } from '../services/sectorService.js'
import { formatCivilDate, guatemalaToday } from '../utils/dateTime.js'

const monthNames = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
]

const weekDays = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom']

const recurrenceValues = {
  none: '',
  weekly: 1,
  monthly: 2,
  yearly: 3,
}

function todayParts() {
  const [year, month, day] = guatemalaToday().split('-').map(Number)
  return { year, month, day }
}

function emptyForm(date = guatemalaToday()) {
  return {
    sectorId: '',
    fecha: date,
    horaInicio: '',
    horaFin: '',
    observacion: '',
    recurrence: 'none',
    recurrenceEnd: 'count',
    cantidadOcurrencias: '4',
    fechaFin: '',
  }
}

function AbastecimientoPage() {
  const { authenticatedRequest, hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.ABASTECIMIENTO_GESTIONAR)
  const requestRef = useRef(authenticatedRequest)

  const [cursor, setCursor] = useState(() => {
    const today = todayParts()

    return {
      year: today.year,
      month: today.month,
    }
  })
  const [view, setView] = useState('month')
  const [sectorFilter, setSectorFilter] = useState('')
  const [rows, setRows] = useState([])
  const [sectors, setSectors] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)
  const [editor, setEditor] = useState(null)
  const [detail, setDetail] = useState(null)
  const [confirmation, setConfirmation] = useState(null)

  const monthRange = useMemo(
    () => getMonthRange(cursor.year, cursor.month),
    [cursor],
  )

  const loadMonth = useCallback(async (range, selectedSector = '') => {
    setIsLoading(true)
    setError(null)

    try {
      const data = await getProgramaciones(requestRef.current, {
        fechaDesde: range.first,
        fechaHasta: range.last,
        sectorId: selectedSector,
      })

      setRows(Array.isArray(data) ? data : [])
    } catch (requestError) {
      setRows([])
      setError(requestMessage(requestError))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    const today = todayParts()
    const initialRange = getMonthRange(today.year, today.month)

    async function initialize() {
      try {
        const [sectorData, scheduleData] = await Promise.all([
          getSectores(requestRef.current),
          getProgramaciones(requestRef.current, {
            fechaDesde: initialRange.first,
            fechaHasta: initialRange.last,
          }),
        ])

        if (!cancelled) {
          setSectors(
            Array.isArray(sectorData)
              ? sectorData.filter((sector) => sector.estado === 1)
              : [],
          )
          setRows(Array.isArray(scheduleData) ? scheduleData : [])
        }
      } catch (requestError) {
        if (!cancelled) setError(requestMessage(requestError))
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }

    initialize()

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    const media = window.matchMedia('(max-width: 639px)')

    function updateView() {
      if (media.matches) setView('agenda')
    }

    updateView()
    media.addEventListener('change', updateView)

    return () => media.removeEventListener('change', updateView)
  }, [])

  const summary = useMemo(() => ({
    Programado: rows.filter((row) => row.estado === 'Programado').length,
    Completado: rows.filter((row) => row.estado === 'Completado').length,
    Cancelado: rows.filter((row) => row.estado === 'Cancelado').length,
  }), [rows])

  const eventsByDate = useMemo(() => {
    const map = new Map()

    for (const row of rows) {
      const key = civilDateValue(row.fecha)
      const current = map.get(key) || []
      current.push(row)
      map.set(key, current)
    }

    for (const events of map.values()) {
      events.sort((a, b) =>
        String(a.horaInicio).localeCompare(String(b.horaInicio)))
    }

    return map
  }, [rows])

  const calendarDays = useMemo(
    () => buildCalendarDays(cursor.year, cursor.month),
    [cursor],
  )

  function moveMonth(delta) {
    const date = new Date(cursor.year, cursor.month - 1 + delta, 1)
    const next = {
      year: date.getFullYear(),
      month: date.getMonth() + 1,
    }

    setCursor(next)
    loadMonth(getMonthRange(next.year, next.month), sectorFilter)
  }

  function goToday() {
    const today = todayParts()
    const next = { year: today.year, month: today.month }

    setCursor(next)
    loadMonth(getMonthRange(next.year, next.month), sectorFilter)
  }

  function changeSector(event) {
    const value = event.target.value
    setSectorFilter(value)
    loadMonth(monthRange, value)
  }

  function openNew(date = guatemalaToday()) {
    setDetail(null)
    setEditor({
      mode: 'create',
      form: emptyForm(date),
    })
  }

  function openEdit(row) {
    if (!Number.isInteger(Number(row.id)) || Number(row.id) <= 0) {
      setDetail(null)
      setError('No se pudo identificar la programación seleccionada. Actualiza el calendario e inténtalo nuevamente.')
      return
    }

    setDetail(null)
    setEditor({
      mode: 'edit',
      id: Number(row.id),
      form: {
        ...emptyForm(civilDateValue(row.fecha)),
        sectorId: String(row.sectorId),
        horaInicio: timeValue(row.horaInicio),
        horaFin: timeValue(row.horaFin),
        observacion: row.observacion || '',
      },
    })
  }

  async function saveEditor(form) {
    setError(null)
    setNotice(null)

    const validation = validateForm(form, editor.mode)

    if (validation) {
      setError(validation)
      return
    }

    setIsSaving(true)

    try {
      if (editor.mode === 'edit') {
        await updateProgramacion(
          requestRef.current,
          editor.id,
          regularPayload(form),
        )

        setNotice('Programación actualizada correctamente.')
      } else if (form.recurrence === 'none') {
        await createProgramacion(
          requestRef.current,
          regularPayload(form),
        )

        setNotice('Programación creada correctamente.')
      } else {
        const result = await createProgramacionRecurrente(
          requestRef.current,
          recurringPayload(form),
        )

        setNotice(
          `${result.cantidadCreada} programaciones creadas correctamente.`,
        )
      }

      setEditor(null)
      await loadMonth(monthRange, sectorFilter)
    } catch (requestError) {
      setError(requestMessage(requestError))
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmStateChange() {
    if (!confirmation) return

    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      if (confirmation.type === 'complete') {
        await completeProgramacion(
          requestRef.current,
          confirmation.row.id,
        )

        setNotice('Programación marcada como completada.')
      } else {
        await cancelProgramacion(
          requestRef.current,
          confirmation.row.id,
          confirmation.observacion?.trim() || null,
        )

        setNotice('Programación cancelada.')
      }

      setConfirmation(null)
      setDetail(null)
      await loadMonth(monthRange, sectorFilter)
    } catch (requestError) {
      setError(requestMessage(requestError))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="mx-auto max-w-7xl space-y-7">
      <PageHeader
        actions={canManage ? (
          <button className={primaryButton} onClick={() => openNew()} type="button">
            + Nueva programación
          </button>
        ) : null}
        description="Organiza los días y horarios de distribución de agua potable por sector."
        eyebrow="Calendario administrativo"
        title="Abastecimiento"
      />

      {error && <Alert message={error} />}
      {notice && <Notice message={notice} />}

      <div className="grid gap-3 sm:grid-cols-3">
        <SummaryCard
          label="Programados"
          value={summary.Programado}
          description="Pendientes de realizar"
        />
        <SummaryCard
          label="Completados"
          value={summary.Completado}
          description="Realizados este mes"
        />
        <SummaryCard
          label="Cancelados"
          value={summary.Cancelado}
          description="Cancelados este mes"
        />
      </div>

      <Panel>
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex flex-wrap items-center gap-2">
            <button
              aria-label="Mes anterior"
              className={navigationButton}
              onClick={() => moveMonth(-1)}
              type="button"
            >
              ‹
            </button>

            <button className={secondaryButton} onClick={goToday} type="button">
              Hoy
            </button>

            <button
              aria-label="Mes siguiente"
              className={navigationButton}
              onClick={() => moveMonth(1)}
              type="button"
            >
              ›
            </button>

            <h2 className="ml-1 text-lg font-semibold capitalize text-slate-900">
              {monthNames[cursor.month - 1]} {cursor.year}
            </h2>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <label className="text-sm font-medium text-slate-700">
              <span className="sr-only">Filtrar por sector</span>
              <select
                className="min-h-10 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
                onChange={changeSector}
                value={sectorFilter}
              >
                <option value="">Todos los sectores</option>
                {sectors.map((sector) => (
                  <option key={sector.id} value={sector.id}>
                    {sector.nombre}
                  </option>
                ))}
              </select>
            </label>

            <div className="inline-flex rounded-md border border-slate-300 bg-white p-1">
              <ViewButton active={view === 'month'} onClick={() => setView('month')}>
                Mes
              </ViewButton>
              <ViewButton active={view === 'agenda'} onClick={() => setView('agenda')}>
                Agenda
              </ViewButton>
            </div>
          </div>
        </div>

        <div className="mt-6">
          {isLoading ? (
            <LoadingState message="Cargando calendario..." />
          ) : view === 'month' ? (
            <MonthView
              days={calendarDays}
              eventsByDate={eventsByDate}
              month={cursor.month}
              onDayClick={canManage ? openNew : undefined}
              onEventClick={setDetail}
            />
          ) : (
            <AgendaView
              events={rows}
              onDayClick={canManage ? openNew : undefined}
              onEventClick={setDetail}
            />
          )}
        </div>
      </Panel>

      <Panel>
        <div className="flex flex-wrap gap-x-6 gap-y-2 text-xs text-slate-600">
          <Legend label="Programado" className="bg-blue-500" />
          <Legend label="Completado" className="bg-emerald-500" />
          <Legend label="Cancelado" className="bg-slate-400" />
          <span className="text-slate-400">
            Selecciona un día para crear una programación.
          </span>
        </div>
      </Panel>

      {canManage && editor && (
        <ScheduleEditorDialog
          editor={editor}
          isSaving={isSaving}
          onCancel={() => setEditor(null)}
          onSave={saveEditor}
          sectors={sectors}
        />
      )}

      {detail && (
        <DetailDialog
          canManage={canManage}
          onCancel={() => setConfirmation({
            type: 'cancel',
            row: detail,
            observacion: detail.observacion || '',
          })}
          onClose={() => setDetail(null)}
          onComplete={() => setConfirmation({
            type: 'complete',
            row: detail,
          })}
          onEdit={() => openEdit(detail)}
          row={detail}
        />
      )}

      {canManage && confirmation && (
        <StateDialog
          confirmation={confirmation}
          isSaving={isSaving}
          onChange={(observacion) =>
            setConfirmation((current) => ({ ...current, observacion }))}
          onClose={() => setConfirmation(null)}
          onConfirm={confirmStateChange}
        />
      )}
    </div>
  )
}

function MonthView({ days, eventsByDate, month, onDayClick, onEventClick }) {
  return (
    <div className="overflow-hidden rounded-lg border border-slate-200">
      <div className="grid grid-cols-7 border-b border-slate-200 bg-slate-50">
        {weekDays.map((day) => (
          <div
            className="px-2 py-2.5 text-center text-xs font-semibold uppercase tracking-wide text-slate-500"
            key={day}
          >
            {day}
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7 bg-slate-200 gap-px">
        {days.map((day) => {
          const events = eventsByDate.get(day.value) || []
          const isCurrentMonth = day.month === month
          const isToday = day.value === guatemalaToday()

          return (
            <div
              className={`min-h-28 bg-white p-1.5 sm:min-h-32 sm:p-2 ${
                !isCurrentMonth ? 'bg-slate-50/80' : ''
              }`}
              key={day.value}
            >
              <button
                className={`flex h-7 w-7 items-center justify-center rounded-full text-xs font-semibold ${
                  isToday
                    ? 'bg-[#123b43] text-white'
                    : isCurrentMonth
                      ? 'text-slate-700 hover:bg-slate-100'
                      : 'text-slate-400 hover:bg-slate-100'
                }`}
                onClick={() => onDayClick(day.value)}
                type="button"
              >
                {day.day}
              </button>

              <div className="mt-1 space-y-1">
                {events.slice(0, 3).map((event) => (
                  <button
                    className={`block w-full truncate rounded px-1.5 py-1 text-left text-[10px] font-medium sm:text-xs ${eventClass(event.estado)}`}
                    key={event.id}
                    onClick={() => onEventClick(event)}
                    title={`${event.sectorNombre} ${formatTime(event.horaInicio)}-${formatTime(event.horaFin)}`}
                    type="button"
                  >
                    <span className="hidden sm:inline">
                      {formatTime(event.horaInicio)}{' '}
                    </span>
                    {event.sectorNombre}
                  </button>
                ))}

                {events.length > 3 && (
                  <p className="px-1 text-[10px] font-medium text-slate-500">
                    +{events.length - 3} más
                  </p>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

function AgendaView({ events, onDayClick, onEventClick }) {
  const groups = useMemo(() => {
    const result = new Map()

    for (const event of events) {
      const key = civilDateValue(event.fecha)
      const current = result.get(key) || []
      current.push(event)
      result.set(key, current)
    }

    return [...result.entries()].sort(([a], [b]) => a.localeCompare(b))
  }, [events])

  if (groups.length === 0) {
    return (
      <EmptyState
        description="Selecciona una fecha o utiliza Nueva programación para comenzar."
        title="No hay programaciones este mes"
      />
    )
  }

  return (
    <div className="space-y-6">
      {groups.map(([date, dateEvents]) => (
        <section key={date}>
          <div className="mb-2 flex items-center justify-between gap-3">
            <h3 className="text-sm font-semibold capitalize text-slate-900">
              {longDate(date)}
            </h3>
            <button
              className="text-xs font-semibold text-[#28727a] hover:underline"
              onClick={() => onDayClick(date)}
              type="button"
            >
              + Agregar
            </button>
          </div>

          <div className="space-y-2">
            {dateEvents.map((event) => (
              <button
                className="flex w-full items-start gap-3 rounded-lg border border-slate-200 bg-white p-3 text-left hover:border-slate-300 hover:bg-slate-50"
                key={event.id}
                onClick={() => onEventClick(event)}
                type="button"
              >
                <span className={`mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full ${statusDot(event.estado)}`} />

                <span className="min-w-0 flex-1">
                  <span className="block font-semibold text-slate-900">
                    {event.sectorNombre}
                  </span>
                  <span className="mt-1 block text-xs text-slate-500">
                    {formatTime(event.horaInicio)} – {formatTime(event.horaFin)}
                  </span>
                  {event.observacion && (
                    <span className="mt-1 block truncate text-xs text-slate-500">
                      {event.observacion}
                    </span>
                  )}
                </span>

                <StatusBadge status={event.estado} />
              </button>
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

function ScheduleEditorDialog({ editor, isSaving, onCancel, onSave, sectors }) {
  const [form, setForm] = useState(editor.form)

  function change(event) {
    const { name, value } = event.target

    setForm((current) => ({
      ...current,
      [name]: value,
    }))
  }

  function submit(event) {
    event.preventDefault()
    onSave(form)
  }

  const isEdit = editor.mode === 'edit'
  const recurring = !isEdit && form.recurrence !== 'none'

  return (
    <Dialog
      description={isEdit
        ? 'Los cambios afectan únicamente esta programación.'
        : 'Define el horario de distribución para el sector seleccionado.'}
      onClose={onCancel}
      title={isEdit ? 'Editar programación' : 'Nueva programación'}
    >
      <form className="grid gap-4 sm:grid-cols-2" onSubmit={submit}>
        <Field label="Sector">
          <select
            className={inputClass}
            name="sectorId"
            onChange={change}
            required
            value={form.sectorId}
          >
            <option value="">Seleccionar sector</option>
            {sectors.map((sector) => (
              <option key={sector.id} value={sector.id}>
                {sector.nombre}
              </option>
            ))}
          </select>
        </Field>

        <Field label="Fecha">
          <input
            className={inputClass}
            name="fecha"
            onChange={change}
            required
            type="date"
            value={form.fecha}
          />
        </Field>

        <Field label="Desde">
          <input
            className={inputClass}
            name="horaInicio"
            onChange={change}
            required
            type="time"
            value={form.horaInicio}
          />
        </Field>

        <Field label="Hasta">
          <input
            className={inputClass}
            name="horaFin"
            onChange={change}
            required
            type="time"
            value={form.horaFin}
          />
        </Field>

        <div className="sm:col-span-2">
          <Field label="Observación">
            <textarea
              className={`${inputClass} min-h-20 resize-y`}
              maxLength="1000"
              name="observacion"
              onChange={change}
              placeholder="Información opcional"
              value={form.observacion}
            />
          </Field>
        </div>

        {!isEdit && (
          <div className="sm:col-span-2">
            <Field label="Repetir">
              <select
                className={inputClass}
                name="recurrence"
                onChange={change}
                value={form.recurrence}
              >
                <option value="none">No repetir</option>
                <option value="weekly">Cada semana</option>
                <option value="monthly">Cada mes</option>
                <option value="yearly">Cada año</option>
              </select>
            </Field>
          </div>
        )}

        {recurring && (
          <>
            <div className="sm:col-span-2">
              <p className="text-xs leading-5 text-slate-500">
                Se crearán programaciones independientes. Editar una posteriormente
                no modificará las demás.
              </p>
            </div>

            <div className="sm:col-span-2">
              <Field label="Finaliza">
                <select
                  className={inputClass}
                  name="recurrenceEnd"
                  onChange={change}
                  value={form.recurrenceEnd}
                >
                  <option value="count">Después de varias repeticiones</option>
                  <option value="date">En una fecha</option>
                </select>
              </Field>
            </div>

            {form.recurrenceEnd === 'count' ? (
              <Field label="Número de programaciones">
                <input
                  className={inputClass}
                  max="52"
                  min="2"
                  name="cantidadOcurrencias"
                  onChange={change}
                  required
                  type="number"
                  value={form.cantidadOcurrencias}
                />
              </Field>
            ) : (
              <Field label="Fecha final">
                <input
                  className={inputClass}
                  min={form.fecha}
                  name="fechaFin"
                  onChange={change}
                  required
                  type="date"
                  value={form.fechaFin}
                />
              </Field>
            )}
          </>
        )}

        <div className="flex flex-col-reverse gap-2 pt-2 sm:col-span-2 sm:flex-row sm:justify-end">
          <button
            className={secondaryButton}
            disabled={isSaving}
            onClick={onCancel}
            type="button"
          >
            Cancelar
          </button>
          <button className={primaryButton} disabled={isSaving} type="submit">
            {isSaving
              ? 'Guardando...'
              : isEdit
                ? 'Guardar cambios'
                : recurring
                  ? 'Crear programaciones'
                  : 'Crear programación'}
          </button>
        </div>
      </form>
    </Dialog>
  )
}

function DetailDialog({ canManage, onCancel, onClose, onComplete, onEdit, row }) {
  const active = row.estado === 'Programado'

  return (
    <Dialog
      description={`${formatCivilDate(row.fecha)} · ${formatTime(row.horaInicio)} – ${formatTime(row.horaFin)}`}
      onClose={onClose}
      title={row.sectorNombre}
    >

      <div className="space-y-4">
        <div className="flex items-center justify-between gap-4 rounded-lg bg-slate-50 p-4">
          <span className="text-sm text-slate-500">Estado</span>
          <StatusBadge status={row.estado} />
        </div>

        {row.observacion && (
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Observación
            </p>
            <p className="mt-1 text-sm leading-6 text-slate-700">
              {row.observacion}
            </p>
          </div>
        )}

        {canManage && active ? (
          <div className="grid gap-2 pt-2 sm:grid-cols-3">
            <button className={secondaryButton} onClick={onEdit} type="button">
              Editar
            </button>
            <button className={primaryButton} onClick={onComplete} type="button">
              Completar
            </button>
            <button className={dangerButton} onClick={onCancel} type="button">
              Cancelar programación
            </button>
          </div>
        ) : (
          <p className="rounded-md bg-slate-50 px-4 py-3 text-sm text-slate-500">
            Esta programación ya finalizó y no admite modificaciones.
          </p>
        )}
      </div>
    </Dialog>
  )
}

function StateDialog({ confirmation, isSaving, onChange, onClose, onConfirm }) {
  const cancelling = confirmation.type === 'cancel'

  return (
    <Dialog
      description={cancelling
        ? 'La programación quedará registrada históricamente como cancelada.'
        : 'La programación quedará registrada como realizada.'}
      onClose={onClose}
      title={cancelling ? 'Cancelar programación' : 'Marcar como completada'}
    >
      <form className="space-y-5" onSubmit={(event) => {
        event.preventDefault()
        if (!isSaving) onConfirm()
      }}>
        <div className="rounded-lg bg-slate-50 p-4">
          <p className="font-semibold text-slate-900">
            {confirmation.row.sectorNombre}
          </p>
          <p className="mt-1 text-sm text-slate-500">
            {formatCivilDate(confirmation.row.fecha)} ·{' '}
            {formatTime(confirmation.row.horaInicio)} –{' '}
            {formatTime(confirmation.row.horaFin)}
          </p>
        </div>

        {cancelling && (
          <ReasonField
            disabled={isSaving}
            label="Observación de cancelación"
            maxLength={1000}
            onChange={onChange}
            options={[
              'Reprogramación del abastecimiento',
              'Mantenimiento de la red',
              'Falta de disponibilidad de agua',
              'Emergencia operativa',
              'Condiciones climáticas',
            ]}
            required
            value={confirmation.observacion || ''}
          />
        )}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            className={secondaryButton}
            disabled={isSaving}
            onClick={onClose}
            type="button"
          >
            Volver
          </button>
          <button
            className={cancelling ? dangerButton : primaryButton}
            disabled={isSaving}
            type="submit"
          >
            {isSaving
              ? 'Procesando...'
              : cancelling
                ? 'Confirmar cancelación'
                : 'Marcar como completada'}
          </button>
        </div>
      </form>
    </Dialog>
  )
}

function Dialog({ children, description, onClose, title }) {
  const dialogRef = useRef(null)

  const onCloseRef = useRef(onClose)

  useEffect(() => {
    onCloseRef.current = onClose
  }, [onClose])

  useEffect(() => {
    dialogRef.current?.focus()

    function handleKey(event) {
      if (event.key === 'Escape') onCloseRef.current()
    }

    document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [])

  return (
    <div
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/45 p-0 sm:items-center sm:p-4"
      role="dialog"
    >
      <div
        className="max-h-[92vh] w-full overflow-y-auto rounded-t-2xl bg-white shadow-2xl outline-none sm:max-w-xl sm:rounded-xl"
        ref={dialogRef}
        tabIndex="-1"
      >
        <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-5 py-4">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
            {description && (
              <p className="mt-1 text-sm leading-5 text-slate-500">
                {description}
              </p>
            )}
          </div>

          <button
            aria-label="Cerrar"
            className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-xl text-slate-500 hover:bg-slate-100"
            onClick={onClose}
            type="button"
          >
            ×
          </button>
        </div>

        <div className="p-5">{children}</div>
      </div>
    </div>
  )
}

function SummaryCard({ description, label, value }) {
  return (
    <Panel>
      <p className="text-xs font-semibold uppercase tracking-[0.12em] text-[#28727a]">
        {label}
      </p>
      <p className="mt-2 text-2xl font-semibold text-slate-900">{value}</p>
      <p className="mt-1 text-xs text-slate-500">{description}</p>
    </Panel>
  )
}

function ViewButton({ active, children, onClick }) {
  return (
    <button
      className={`rounded px-3 py-1.5 text-xs font-semibold ${
        active
          ? 'bg-[#123b43] text-white'
          : 'text-slate-600 hover:bg-slate-100'
      }`}
      onClick={onClick}
      type="button"
    >
      {children}
    </button>
  )
}

function StatusBadge({ status }) {
  const classes = {
    Programado: 'bg-blue-50 text-blue-700',
    Completado: 'bg-emerald-50 text-emerald-700',
    Cancelado: 'bg-slate-100 text-slate-600',
  }

  return (
    <span className={`inline-flex shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${
      classes[status] || 'bg-slate-100 text-slate-600'
    }`}>
      {status}
    </span>
  )
}

function Field({ children, label }) {
  return (
    <label className="block text-sm font-medium text-slate-700">
      {label}
      {children}
    </label>
  )
}

function Legend({ className, label }) {
  return (
    <span className="inline-flex items-center gap-2">
      <span className={`h-2.5 w-2.5 rounded-full ${className}`} />
      {label}
    </span>
  )
}

function Alert({ message }) {
  return (
    <div
      aria-live="polite"
      className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
    >
      {message}
    </div>
  )
}

function Notice({ message }) {
  return (
    <div
      aria-live="polite"
      className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800"
    >
      {message}
    </div>
  )
}

function getMonthRange(year, month) {
  const lastDay = new Date(year, month, 0).getDate()

  return {
    first: isoDate(year, month, 1),
    last: isoDate(year, month, lastDay),
  }
}

function buildCalendarDays(year, month) {
  const first = new Date(year, month - 1, 1)
  const mondayOffset = (first.getDay() + 6) % 7
  const start = new Date(year, month - 1, 1 - mondayOffset)
  const days = []

  for (let index = 0; index < 42; index += 1) {
    const date = new Date(
      start.getFullYear(),
      start.getMonth(),
      start.getDate() + index,
    )

    days.push({
      value: isoDate(
        date.getFullYear(),
        date.getMonth() + 1,
        date.getDate(),
      ),
      day: date.getDate(),
      month: date.getMonth() + 1,
    })
  }

  return days
}

function isoDate(year, month, day) {
  return `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

function regularPayload(form) {
  return {
    sectorId: Number(form.sectorId),
    fecha: form.fecha,
    horaInicio: apiTime(form.horaInicio),
    horaFin: apiTime(form.horaFin),
    observacion: form.observacion.trim() || null,
  }
}

function recurringPayload(form) {
  return {
    sectorId: Number(form.sectorId),
    fechaInicial: form.fecha,
    horaInicio: apiTime(form.horaInicio),
    horaFin: apiTime(form.horaFin),
    observacion: form.observacion.trim() || null,
    recurrencia: recurrenceValues[form.recurrence],
    cantidadOcurrencias:
      form.recurrenceEnd === 'count'
        ? Number(form.cantidadOcurrencias)
        : null,
    fechaFin:
      form.recurrenceEnd === 'date'
        ? form.fechaFin
        : null,
  }
}

function validateForm(form, mode) {
  if (!form.sectorId) return 'Selecciona un sector.'
  if (!form.fecha) return 'Selecciona una fecha.'
  if (!form.horaInicio || !form.horaFin) return 'Completa el horario.'
  if (form.horaInicio >= form.horaFin) {
    return 'La hora de inicio debe ser anterior a la hora de finalización.'
  }

  if (mode === 'create' && form.recurrence !== 'none') {
    if (
      form.recurrenceEnd === 'count' &&
      (Number(form.cantidadOcurrencias) < 2 ||
       Number(form.cantidadOcurrencias) > 52)
    ) {
      return 'La recurrencia debe generar entre 2 y 52 programaciones.'
    }

    if (
      form.recurrenceEnd === 'date' &&
      (!form.fechaFin || form.fechaFin <= form.fecha)
    ) {
      return 'La fecha final debe ser posterior a la primera fecha.'
    }
  }

  return null
}

function apiTime(value) {
  return value.length === 5 ? `${value}:00` : value
}

function civilDateValue(value) {
  return value ? String(value).slice(0, 10) : ''
}

function timeValue(value) {
  return value ? String(value).slice(0, 5) : ''
}

function formatTime(value) {
  return timeValue(value) || '—'
}

function longDate(value) {
  const [year, month, day] = value.split('-').map(Number)

  return new Intl.DateTimeFormat('es-GT', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date(year, month - 1, day))
}

function eventClass(status) {
  if (status === 'Programado') return 'bg-blue-50 text-blue-700 hover:bg-blue-100'
  if (status === 'Completado') return 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
  return 'bg-slate-100 text-slate-500 hover:bg-slate-200'
}

function statusDot(status) {
  if (status === 'Programado') return 'bg-blue-500'
  if (status === 'Completado') return 'bg-emerald-500'
  return 'bg-slate-400'
}

function requestMessage(error) {
  if (error?.status === 400) return error.message || 'Los datos no son válidos.'
  if (error?.status === 401) return 'Tu sesión ya no es válida.'
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return error.message || 'Programación no encontrada.'
  if (error?.status === 409) return error.message || 'La programación ya finalizó o el horario entra en conflicto con otra programación activa.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor.'
  return 'No se pudo completar la operación.'
}

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20'
const primaryButton = 'min-h-10 rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33] disabled:cursor-not-allowed disabled:opacity-60'
const secondaryButton = 'min-h-10 rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60'
const dangerButton = 'min-h-10 rounded-md border border-red-200 bg-white px-4 py-2 text-sm font-semibold text-red-700 hover:bg-red-50 disabled:opacity-60'
const navigationButton = 'flex h-10 w-10 items-center justify-center rounded-md border border-slate-300 bg-white text-xl text-slate-700 hover:bg-slate-50'

export default AbastecimientoPage
