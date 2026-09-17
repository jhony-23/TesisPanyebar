import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import {
  annulEgreso,
  createEgreso,
  getEgresos,
  getMovimientosFinancieros,
  getResumenFinanciero,
  updateEgreso,
} from '../services/finanzaService.js'
import { formatCivilDate, guatemalaToday } from '../utils/dateTime.js'

const initialFilters = {
  fechaDesde: '',
  fechaHasta: '',
  tipo: '',
}

const initialForm = {
  concepto: '',
  monto: '',
  fecha: guatemalaToday(),
}

function FinanzasPage() {
  const { authenticatedRequest } = useAuth()
  const requestRef = useRef(authenticatedRequest)

  const [filters, setFilters] = useState(initialFilters)
  const [appliedFilters, setAppliedFilters] = useState(initialFilters)
  const [resumen, setResumen] = useState(null)
  const [movimientos, setMovimientos] = useState([])
  const [egresos, setEgresos] = useState([])

  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [feedback, setFeedback] = useState(null)

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(initialForm)
  const [formError, setFormError] = useState(null)
  const [pendingAnnulment, setPendingAnnulment] = useState(null)

  const loadFinancialData = useCallback(async (currentFilters, showLoading = true) => {
    if (showLoading) setIsLoading(true)
    setError(null)

    try {
      const commonFilters = {
        fechaDesde: currentFilters.fechaDesde,
        fechaHasta: currentFilters.fechaHasta,
      }

      const movementFilters = {
        ...commonFilters,
        tipo: currentFilters.tipo,
      }

      const [summaryData, movementData, expenseData] = await Promise.all([
        getResumenFinanciero(requestRef.current, commonFilters),
        getMovimientosFinancieros(requestRef.current, movementFilters),
        getEgresos(requestRef.current, commonFilters),
      ])

      setResumen(summaryData)
      setMovimientos(Array.isArray(movementData) ? movementData : [])
      setEgresos(Array.isArray(expenseData) ? expenseData : [])
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo cargar la información financiera.'))
    } finally {
      if (showLoading) setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    let mounted = true

    async function loadInitialData() {
      setIsLoading(true)
      setError(null)

      try {
        const [summaryData, movementData, expenseData] = await Promise.all([
          getResumenFinanciero(requestRef.current),
          getMovimientosFinancieros(requestRef.current),
          getEgresos(requestRef.current),
        ])

        if (!mounted) return

        setResumen(summaryData)
        setMovimientos(Array.isArray(movementData) ? movementData : [])
        setEgresos(Array.isArray(expenseData) ? expenseData : [])
      } catch (requestError) {
        if (mounted) {
          setError(getRequestMessage(requestError, 'No se pudo cargar la información financiera.'))
        }
      } finally {
        if (mounted) setIsLoading(false)
      }
    }

    loadInitialData()

    return () => {
      mounted = false
    }
  }, [])

  function changeFilter(event) {
    const { name, value } = event.target
    setFilters((current) => ({ ...current, [name]: value }))
  }

  function applyFilters(event) {
    event.preventDefault()

    if (filters.fechaDesde && filters.fechaHasta && filters.fechaDesde > filters.fechaHasta) {
      setError('La fecha desde no puede ser posterior a la fecha hasta.')
      return
    }

    setError(null)
    setAppliedFilters(filters)
    loadFinancialData(filters)
  }

  function clearFilters() {
    setFilters(initialFilters)
    setAppliedFilters(initialFilters)
    setError(null)
    loadFinancialData(initialFilters)
  }

  function openCreate() {
    setEditingId(null)
    setForm({
      concepto: '',
      monto: '',
      fecha: guatemalaToday(),
    })
    setFormError(null)
    setFeedback(null)
    setIsFormOpen(true)
    window.scrollTo({ behavior: 'smooth', top: 0 })
  }

  function openEdit(egreso) {
    if (egreso.estado !== 1) return

    setEditingId(egreso.id)
    setForm({
      concepto: egreso.concepto || '',
      monto: String(egreso.monto ?? ''),
      fecha: dateInputValue(egreso.fecha),
    })
    setFormError(null)
    setFeedback(null)
    setIsFormOpen(true)
    window.scrollTo({ behavior: 'smooth', top: 0 })
  }

  function closeForm() {
    if (isSaving) return
    setIsFormOpen(false)
    setEditingId(null)
    setFormError(null)
  }

  function changeField(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function validateForm() {
    const concept = form.concepto.trim()
    const amount = form.monto.trim()

    if (!concept) return 'El concepto del egreso es obligatorio.'
    if (concept.length > 200) return 'El concepto no puede superar 200 caracteres.'
    if (!amount || !/^\d+(?:\.\d{1,2})?$/.test(amount)) {
      return 'Ingresa un monto válido con hasta dos decimales.'
    }
    if (Number(amount) <= 0) return 'El monto debe ser mayor que cero.'
    if (!form.fecha) return 'La fecha del egreso es obligatoria.'

    return null
  }

  async function saveExpense(event) {
    event.preventDefault()

    const validationError = validateForm()
    if (validationError) {
      setFormError(validationError)
      return
    }

    setIsSaving(true)
    setFormError(null)
    setError(null)
    setFeedback(null)

    const payload = {
      concepto: form.concepto.trim(),
      monto: Number(form.monto),
      fecha: form.fecha,
    }

    try {
      if (editingId === null) {
        await createEgreso(authenticatedRequest, payload)
        setFeedback('Egreso registrado correctamente.')
      } else {
        await updateEgreso(authenticatedRequest, editingId, payload)
        setFeedback('Egreso actualizado correctamente.')
      }

      setIsFormOpen(false)
      setEditingId(null)
      await loadFinancialData(appliedFilters, false)
    } catch (requestError) {
      setFormError(getRequestMessage(requestError, 'No se pudo guardar el egreso.'))

      if (requestError?.status === 409) {
        await loadFinancialData(appliedFilters, false)
      }
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmAnnulment() {
    if (!pendingAnnulment) return

    setIsSaving(true)
    setError(null)
    setFeedback(null)

    try {
      await annulEgreso(authenticatedRequest, pendingAnnulment.id)
      setPendingAnnulment(null)
      setFeedback('Egreso anulado correctamente. El registro permanece en el historial.')
      await loadFinancialData(appliedFilters, false)
    } catch (requestError) {
      setError(getRequestMessage(requestError, 'No se pudo anular el egreso.'))

      if (requestError?.status === 409) {
        setPendingAnnulment(null)
        await loadFinancialData(appliedFilters, false)
      }
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={(
          <button
            className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={isSaving}
            onClick={openCreate}
            type="button"
          >
            Nuevo egreso
          </button>
        )}
        description="Consulta ingresos, egresos y balance administrativo del Comité de Agua Potable."
        eyebrow="Gestión financiera"
        title="Finanzas"
      />

      {error && <Alert message={error} onDismiss={() => setError(null)} />}
      {feedback && <SuccessAlert message={feedback} />}

      {isFormOpen && (
        <Panel>
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-base font-semibold text-slate-900">
                {editingId === null ? 'Registrar egreso' : 'Editar egreso'}
              </h2>
              <p className="mt-1 text-sm leading-6 text-slate-500">
                {editingId === null
                  ? 'Registra un gasto administrativo. La fecha se conserva como fecha civil.'
                  : 'Corrige los datos del egreso. El sistema conservará la modificación en auditoría.'}
              </p>
            </div>
            <button
              aria-label="Cerrar formulario de egreso"
              className="rounded-md px-2 py-1 text-sm font-semibold text-slate-500 hover:bg-slate-100"
              disabled={isSaving}
              onClick={closeForm}
              type="button"
            >
              Cerrar
            </button>
          </div>

          <form className="mt-5 grid gap-4 md:grid-cols-2" onSubmit={saveExpense}>
            <label className="text-sm font-medium text-slate-700 md:col-span-2" htmlFor="egreso-concepto">
              Concepto *
              <input
                className={inputClass}
                id="egreso-concepto"
                maxLength="200"
                name="concepto"
                onChange={changeField}
                required
                value={form.concepto}
              />
              <span className="mt-1 block text-right text-xs font-normal text-slate-400">
                {form.concepto.length}/200
              </span>
            </label>

            <label className="text-sm font-medium text-slate-700" htmlFor="egreso-monto">
              Monto (Q) *
              <input
                className={inputClass}
                id="egreso-monto"
                inputMode="decimal"
                name="monto"
                onChange={changeField}
                placeholder="0.00"
                required
                value={form.monto}
              />
            </label>

            <label className="text-sm font-medium text-slate-700" htmlFor="egreso-fecha">
              Fecha *
              <input
                className={inputClass}
                id="egreso-fecha"
                name="fecha"
                onChange={changeField}
                required
                type="date"
                value={form.fecha}
              />
            </label>

            {formError && (
              <div className="md:col-span-2">
                <Alert message={formError} />
              </div>
            )}

            <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end md:col-span-2">
              <button
                className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
                disabled={isSaving}
                onClick={closeForm}
                type="button"
              >
                Volver
              </button>
              <button
                className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] hover:bg-[#c99a50] disabled:cursor-not-allowed disabled:opacity-60"
                disabled={isSaving}
                type="submit"
              >
                {isSaving ? 'Guardando...' : editingId === null ? 'Registrar egreso' : 'Guardar cambios'}
              </button>
            </div>
          </form>
        </Panel>
      )}

      <SummaryCards resumen={resumen} loading={isLoading} />

      <Panel>
        <div>
          <h2 className="text-base font-semibold text-slate-900">Filtros financieros</h2>
          <p className="mt-1 text-sm text-slate-500">
            Consulta los movimientos y totales dentro de un rango de fechas.
          </p>
        </div>

        <form className="mt-5 grid gap-4 md:grid-cols-3" onSubmit={applyFilters}>
          <label className="text-sm font-medium text-slate-700" htmlFor="finanzas-desde">
            Fecha desde
            <input
              className={inputClass}
              id="finanzas-desde"
              name="fechaDesde"
              onChange={changeFilter}
              type="date"
              value={filters.fechaDesde}
            />
          </label>

          <label className="text-sm font-medium text-slate-700" htmlFor="finanzas-hasta">
            Fecha hasta
            <input
              className={inputClass}
              id="finanzas-hasta"
              min={filters.fechaDesde || undefined}
              name="fechaHasta"
              onChange={changeFilter}
              type="date"
              value={filters.fechaHasta}
            />
          </label>

          <label className="text-sm font-medium text-slate-700" htmlFor="finanzas-tipo">
            Tipo de movimiento
            <select
              className={inputClass}
              id="finanzas-tipo"
              name="tipo"
              onChange={changeFilter}
              value={filters.tipo}
            >
              <option value="">Todos</option>
              <option value="1">Ingresos</option>
              <option value="2">Egresos</option>
            </select>
          </label>

          <div className="flex flex-col-reverse gap-2 md:col-span-3 sm:flex-row sm:justify-end">
            <button
              className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
              onClick={clearFilters}
              type="button"
            >
              Limpiar
            </button>
            <button
              className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33]"
              type="submit"
            >
              Aplicar filtros
            </button>
          </div>
        </form>
      </Panel>

      <Panel>
        <SectionHeading
          count={movimientos.length}
          description="Los ingresos provienen exclusivamente de pagos registrados; los egresos anulados no participan."
          singular="movimiento"
          title="Movimientos financieros"
        />

        <div className="mt-5">
          {isLoading ? (
            <LoadingState message="Cargando movimientos financieros..." />
          ) : movimientos.length === 0 ? (
            <EmptyState
              description="No hay ingresos o egresos válidos para los filtros seleccionados."
              title="No hay movimientos"
            />
          ) : (
            <MovementList movements={movimientos} />
          )}
        </div>
      </Panel>

      <Panel>
        <SectionHeading
          count={egresos.length}
          description="Los registros anulados permanecen visibles como parte del historial administrativo."
          singular="egreso"
          title="Egresos registrados"
        />

        <div className="mt-5">
          {isLoading ? (
            <LoadingState message="Cargando egresos..." />
          ) : egresos.length === 0 ? (
            <EmptyState
              description="Los gastos administrativos registrados aparecerán aquí."
              title="No hay egresos registrados"
            />
          ) : (
            <ExpenseList
              expenses={egresos}
              isSaving={isSaving}
              onAnnul={setPendingAnnulment}
              onEdit={openEdit}
            />
          )}
        </div>
      </Panel>

      {pendingAnnulment && (
        <AnnulmentDialog
          expense={pendingAnnulment}
          isSaving={isSaving}
          onCancel={() => setPendingAnnulment(null)}
          onConfirm={confirmAnnulment}
        />
      )}
    </div>
  )
}

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:cursor-not-allowed disabled:bg-slate-100'

function SummaryCards({ loading, resumen }) {
  const cards = [
    {
      label: 'Ingresos',
      value: resumen?.ingresosTotales,
      description: 'Pagos registrados',
    },
    {
      label: 'Egresos',
      value: resumen?.egresosTotales,
      description: 'Gastos vigentes',
    },
    {
      label: 'Balance',
      value: resumen?.balance,
      description: 'Ingresos menos egresos',
    },
  ]

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      {cards.map((card) => (
        <Panel className="min-w-0" key={card.label}>
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[#28727a]">{card.label}</p>
          <p className="mt-3 break-words text-2xl font-semibold tracking-tight text-slate-900">
            {loading ? '—' : formatMoney(card.value ?? 0)}
          </p>
          <p className="mt-1 text-xs text-slate-500">{card.description}</p>
        </Panel>
      ))}
    </div>
  )
}

function SectionHeading({ count, description, singular, title }) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h2 className="text-base font-semibold text-slate-900">{title}</h2>
        <p className="mt-1 text-sm leading-6 text-slate-500">{description}</p>
      </div>
      <span className="rounded-md bg-slate-50 px-3 py-2 text-xs text-slate-600">
        {count} {count === 1 ? singular : `${singular}s`}
      </span>
    </div>
  )
}

function MovementList({ movements }) {
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[720px] text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="pb-3 pr-4 font-semibold">Fecha</th>
              <th className="pb-3 pr-4 font-semibold">Tipo</th>
              <th className="pb-3 pr-4 font-semibold">Concepto</th>
              <th className="pb-3 pr-4 font-semibold">Estado</th>
              <th className="pb-3 text-right font-semibold">Monto</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {movements.map((movement) => (
              <tr key={`${movement.tipo}-${movement.referenciaId}`}>
                <td className="py-4 pr-4 text-slate-600">{formatCivilDate(movement.fecha)}</td>
                <td className="py-4 pr-4"><MovementTypeBadge type={movement.tipo} /></td>
                <td className="max-w-72 py-4 pr-4">
                  <p className="font-semibold text-slate-900">{movement.concepto}</p>
                  <p className="mt-1 text-xs text-slate-500">Referencia #{movement.referenciaId}</p>
                </td>
                <td className="py-4 pr-4 text-slate-600">{movement.estado || '—'}</td>
                <td className="whitespace-nowrap py-4 text-right font-semibold text-slate-800">
                  {formatMoney(movement.monto)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="space-y-3 md:hidden">
        {movements.map((movement) => (
          <article
            className="rounded-md border border-slate-200 p-4"
            key={`${movement.tipo}-${movement.referenciaId}`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <h3 className="font-semibold text-slate-900">{movement.concepto}</h3>
                <p className="mt-1 text-xs text-slate-500">
                  {formatCivilDate(movement.fecha)} · Ref. #{movement.referenciaId}
                </p>
              </div>
              <MovementTypeBadge type={movement.tipo} />
            </div>
            <div className="mt-4 flex items-end justify-between gap-3 rounded-md bg-slate-50 p-3">
              <div>
                <p className="text-xs text-slate-500">Estado</p>
                <p className="mt-1 text-sm text-slate-700">{movement.estado || '—'}</p>
              </div>
              <p className="text-lg font-semibold text-slate-900">{formatMoney(movement.monto)}</p>
            </div>
          </article>
        ))}
      </div>
    </>
  )
}

function MovementTypeBadge({ type }) {
  const income = type === 1

  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${
      income ? 'bg-[#e3f2ed] text-[#17644e]' : 'bg-amber-50 text-amber-800'
    }`}>
      <span
        aria-hidden="true"
        className={`h-1.5 w-1.5 rounded-full ${income ? 'bg-[#238568]' : 'bg-amber-500'}`}
      />
      {income ? 'Ingreso' : type === 2 ? 'Egreso' : 'Desconocido'}
    </span>
  )
}

function ExpenseList({ expenses, isSaving, onAnnul, onEdit }) {
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[760px] text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="pb-3 pr-4 font-semibold">Fecha</th>
              <th className="pb-3 pr-4 font-semibold">Concepto</th>
              <th className="pb-3 pr-4 font-semibold">Monto</th>
              <th className="pb-3 pr-4 font-semibold">Estado</th>
              <th className="pb-3 text-right font-semibold">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {expenses.map((expense) => (
              <tr key={expense.id}>
                <td className="py-4 pr-4 text-slate-600">{formatCivilDate(expense.fecha)}</td>
                <td className="max-w-72 py-4 pr-4 font-semibold text-slate-900">{expense.concepto}</td>
                <td className="whitespace-nowrap py-4 pr-4 font-semibold text-slate-700">
                  {formatMoney(expense.monto)}
                </td>
                <td className="py-4 pr-4"><ExpenseStatusBadge status={expense.estado} /></td>
                <td className="py-4 text-right">
                  <ExpenseActions
                    expense={expense}
                    isSaving={isSaving}
                    onAnnul={onAnnul}
                    onEdit={onEdit}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="space-y-3 md:hidden">
        {expenses.map((expense) => (
          <article className="rounded-md border border-slate-200 p-4" key={expense.id}>
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <h3 className="font-semibold text-slate-900">{expense.concepto}</h3>
                <p className="mt-1 text-sm text-slate-500">{formatCivilDate(expense.fecha)}</p>
              </div>
              <ExpenseStatusBadge status={expense.estado} />
            </div>

            <div className="mt-4 rounded-md bg-slate-50 p-3">
              <p className="text-xs text-slate-500">Monto</p>
              <p className="mt-1 text-lg font-semibold text-slate-900">{formatMoney(expense.monto)}</p>
            </div>

            <div className="mt-4 border-t border-slate-100 pt-3">
              <ExpenseActions
                expense={expense}
                isSaving={isSaving}
                onAnnul={onAnnul}
                onEdit={onEdit}
              />
            </div>
          </article>
        ))}
      </div>
    </>
  )
}

function ExpenseStatusBadge({ status }) {
  const registered = status === 1

  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${
      registered ? 'bg-[#e3f2ed] text-[#17644e]' : 'bg-slate-100 text-slate-600'
    }`}>
      <span
        aria-hidden="true"
        className={`h-1.5 w-1.5 rounded-full ${registered ? 'bg-[#238568]' : 'bg-slate-400'}`}
      />
      {registered ? 'Registrado' : status === 2 ? 'Anulado' : 'Desconocido'}
    </span>
  )
}

function ExpenseActions({ expense, isSaving, onAnnul, onEdit }) {
  if (expense.estado !== 1) {
    return <span className="text-xs text-slate-400">Sin acciones disponibles</span>
  }

  return (
    <div className="flex flex-wrap justify-end gap-2">
      <button
        className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
        disabled={isSaving}
        onClick={() => onEdit(expense)}
        type="button"
      >
        Editar
      </button>
      <button
        className="rounded-md border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:opacity-60"
        disabled={isSaving}
        onClick={() => onAnnul(expense)}
        type="button"
      >
        Anular
      </button>
    </div>
  )
}

function AnnulmentDialog({ expense, isSaving, onCancel, onConfirm }) {
  return (
    <div
      aria-labelledby="expense-annulment-title"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/40 p-4"
      role="dialog"
    >
      <div className="my-4 w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-slate-900" id="expense-annulment-title">
          ¿Deseas anular este egreso?
        </h2>
        <p className="mt-2 text-sm leading-6 text-slate-600">
          El registro permanecerá en el historial y dejará de contabilizarse en el balance.
        </p>

        <dl className="mt-4 space-y-2 rounded-md bg-slate-50 p-4 text-sm">
          <div>
            <dt className="inline font-medium text-slate-500">Concepto: </dt>
            <dd className="inline text-slate-700">{expense.concepto}</dd>
          </div>
          <div>
            <dt className="inline font-medium text-slate-500">Fecha: </dt>
            <dd className="inline text-slate-700">{formatCivilDate(expense.fecha)}</dd>
          </div>
          <div>
            <dt className="inline font-medium text-slate-500">Monto: </dt>
            <dd className="inline font-semibold text-slate-800">{formatMoney(expense.monto)}</dd>
          </div>
        </dl>

        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
            disabled={isSaving}
            onClick={onCancel}
            type="button"
          >
            Volver
          </button>
          <button
            className="rounded-md bg-red-700 px-4 py-2 text-sm font-semibold text-white hover:bg-red-800 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={isSaving}
            onClick={onConfirm}
            type="button"
          >
            {isSaving ? 'Anulando...' : 'Confirmar anulación'}
          </button>
        </div>
      </div>
    </div>
  )
}

function Alert({ message, onDismiss }) {
  return (
    <div
      aria-live="polite"
      className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
    >
      <span>{message}</span>
      {onDismiss && (
        <button
          aria-label="Cerrar mensaje"
          className="shrink-0 font-semibold text-red-700"
          onClick={onDismiss}
          type="button"
        >
          Cerrar
        </button>
      )}
    </div>
  )
}

function SuccessAlert({ message }) {
  return (
    <div
      aria-live="polite"
      className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800"
    >
      {message}
    </div>
  )
}

function formatMoney(value) {
  return new Intl.NumberFormat('es-GT', {
    style: 'currency',
    currency: 'GTQ',
  }).format(Number(value) || 0)
}

function dateInputValue(value) {
  return value ? String(value).slice(0, 10) : ''
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400) return error.message || 'Los datos proporcionados no son válidos.'
  if (error?.status === 401) return 'Tu sesión ya no es válida. Inicia sesión nuevamente.'
  if (error?.status === 403) return 'No tienes permiso para realizar esta operación.'
  if (error?.status === 404) return error.message || 'El egreso ya no existe.'
  if (error?.status === 409) return error.message || 'El egreso fue modificado o ya está anulado.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  return fallback
}

export default FinanzasPage
