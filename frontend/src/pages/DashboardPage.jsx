import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import { getDashboardResumen, getRecaudacionPorSector } from '../services/dashboardReporteService.js'
import { guatemalaToday } from '../utils/dateTime.js'

const monthNames = [
  'Enero',
  'Febrero',
  'Marzo',
  'Abril',
  'Mayo',
  'Junio',
  'Julio',
  'Agosto',
  'Septiembre',
  'Octubre',
  'Noviembre',
  'Diciembre',
]

function currentPeriod() {
  const [year, month] = guatemalaToday().split('-')

  return {
    anio: Number(year),
    mes: Number(month),
  }
}

function periodDateRange({ anio, mes }) {
  const leapYear = anio % 4 === 0 && (anio % 100 !== 0 || anio % 400 === 0)
  const days = [31, leapYear ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
  const prefix = `${String(anio).padStart(4, '0')}-${String(mes).padStart(2, '0')}`

  return { fechaDesde: `${prefix}-01`, fechaHasta: `${prefix}-${days[mes - 1]}` }
}

function DashboardPage() {
  const { authenticatedRequest } = useAuth()
  const requestRef = useRef(authenticatedRequest)
  const periodRequestRef = useRef(0)
  const [period, setPeriod] = useState(currentPeriod)
  const [appliedPeriod, setAppliedPeriod] = useState(currentPeriod)
  const [summary, setSummary] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState(null)
  const [sectorRevenue, setSectorRevenue] = useState([])
  const [isRevenueLoading, setIsRevenueLoading] = useState(true)
  const [revenueError, setRevenueError] = useState(null)

  const loadSummary = useCallback(async (selectedPeriod) => {
    const requestId = ++periodRequestRef.current

    async function loadTotals() {
      try {
        const data = await getDashboardResumen(requestRef.current, selectedPeriod.anio, selectedPeriod.mes)
        if (requestId === periodRequestRef.current) setSummary(data)
      } catch (requestError) {
        if (requestId === periodRequestRef.current) setError(getRequestMessage(requestError))
      } finally {
        if (requestId === periodRequestRef.current) setIsLoading(false)
      }
    }

    async function loadRevenue() {
      try {
        const data = await getRecaudacionPorSector(requestRef.current, periodDateRange(selectedPeriod))
        if (requestId === periodRequestRef.current) setSectorRevenue(Array.isArray(data) ? data : [])
      } catch (requestError) {
        if (requestId === periodRequestRef.current) {
          setRevenueError(requestError?.status === 403
            ? 'No tienes permiso para consultar la recaudación por sector.'
            : 'No se pudo cargar la recaudación por sector. Vuelve a consultar el período.')
        }
      } finally {
        if (requestId === periodRequestRef.current) setIsRevenueLoading(false)
      }
    }

    await Promise.all([loadTotals(), loadRevenue()])
  }, [])

  useEffect(() => {
    loadSummary(currentPeriod())

    return () => {
      periodRequestRef.current += 1
    }
  }, [loadSummary])

  function consultPeriod(selectedPeriod) {
    setAppliedPeriod(selectedPeriod)
    setIsLoading(true)
    setError(null)
    setSummary(null)
    setIsRevenueLoading(true)
    setRevenueError(null)
    setSectorRevenue([])
    loadSummary(selectedPeriod)
  }

  function changePeriod(event) {
    const { name, value } = event.target

    setPeriod((current) => ({
      ...current,
      [name]: Number(value),
    }))
  }

  function applyPeriod(event) {
    event.preventDefault()

    if (!Number.isInteger(period.anio) || period.anio < 1 || period.anio > 9999) {
      setError('Ingresa un año válido.')
      return
    }

    if (!Number.isInteger(period.mes) || period.mes < 1 || period.mes > 12) {
      setError('Selecciona un mes válido.')
      return
    }

    if (period.anio === 9999 && period.mes === 12) {
      setError('Diciembre del año 9999 no admite un intervalo mensual.')
      return
    }

    const nextPeriod = { ...period }
    consultPeriod(nextPeriod)
  }

  function restoreCurrentPeriod() {
    const nextPeriod = currentPeriod()

    setPeriod(nextPeriod)
    consultPeriod(nextPeriod)
  }

  const cards = [
    {
      label: 'Ingresos',
      value: formatMoney(summary?.ingresosTotales),
      description: 'Pagos registrados durante el mes',
    },
    {
      label: 'Egresos',
      value: formatMoney(summary?.egresosTotales),
      description: 'Gastos vigentes durante el mes',
    },
    {
      label: 'Balance',
      value: formatMoney(summary?.balance),
      description: 'Ingresos menos egresos',
    },
    {
      label: 'Pagos',
      value: formatNumber(summary?.cantidadPagos),
      description: 'Pagos vigentes registrados',
    },
    {
      label: 'Obligaciones pendientes',
      value: formatNumber(summary?.cantidadObligacionesPendientes),
      description: 'Generadas en el mes y aún pendientes',
    },
    {
      label: 'Monto pendiente',
      value: formatMoney(summary?.montoObligacionesPendientes),
      description: 'Monto de obligaciones pendientes del mes',
    },
  ]

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        description="Consulta los principales totales administrativos correspondientes a un mes específico."
        eyebrow="Resumen administrativo"
        title="Dashboard"
      />

      {error && <Alert message={error} />}

      <Panel>
        <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <h2 className="text-base font-semibold text-slate-900">Período administrativo</h2>
            <p className="mt-1 text-sm leading-6 text-slate-500">
              Los valores mostrados corresponden exclusivamente al mes seleccionado.
            </p>
          </div>

          <form
            className="grid gap-3 sm:grid-cols-[140px_180px_auto] sm:items-end"
            onSubmit={applyPeriod}
          >
            <label className="text-sm font-medium text-slate-700" htmlFor="dashboard-anio">
              Año
              <input
                className={inputClass}
                id="dashboard-anio"
                max="9999"
                min="1"
                name="anio"
                onChange={changePeriod}
                required
                type="number"
                value={period.anio}
              />
            </label>

            <label className="text-sm font-medium text-slate-700" htmlFor="dashboard-mes">
              Mes
              <select
                className={inputClass}
                id="dashboard-mes"
                name="mes"
                onChange={changePeriod}
                value={period.mes}
              >
                {monthNames.map((name, index) => (
                  <option key={name} value={index + 1}>{name}</option>
                ))}
              </select>
            </label>

            <div className="flex flex-wrap gap-2">
              <button
                className="min-h-11 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
                onClick={restoreCurrentPeriod}
                type="button"
              >
                Mes actual
              </button>
              <button
                className="min-h-11 rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33]"
                type="submit"
              >
                Consultar
              </button>
            </div>
          </form>
        </div>
      </Panel>

      <div>
        <div className="mb-4">
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[#28727a]">
            {monthNames[appliedPeriod.mes - 1]} {appliedPeriod.anio}
          </p>
          <h2 className="mt-1 text-lg font-semibold text-slate-900">Totales del período</h2>
        </div>

        {isLoading ? (
          <LoadingState message="Cargando resumen administrativo..." />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {cards.map((card) => (
              <Panel className="min-w-0" key={card.label}>
                <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[#28727a]">
                  {card.label}
                </p>
                <p className="mt-3 break-words text-2xl font-semibold tracking-tight text-slate-900">
                  {card.value}
                </p>
                <p className="mt-1 text-xs leading-5 text-slate-500">{card.description}</p>
              </Panel>
            ))}
          </div>
        )}
      </div>

      <Panel>
        <h2 className="text-base font-semibold text-slate-900">Recaudación por sector</h2>
        <p className="mt-1 text-sm leading-6 text-slate-500">
          Pagos registrados aplicados a obligaciones de suministros durante el período seleccionado.
        </p>
        <div className="mt-5">
          {isRevenueLoading ? (
            <LoadingState message="Cargando recaudación..." />
          ) : revenueError ? (
            <Alert message={revenueError} />
          ) : (
            <>
              <dl className="mb-5 grid gap-3 sm:grid-cols-2">
                <div className="rounded-md bg-slate-50 p-3">
                  <dt className="text-xs text-slate-500">Recaudación atribuible a suministros</dt>
                  <dd className="mt-1 break-words text-lg font-semibold text-slate-900">
                    {formatMoney(sectorRevenue.reduce((total, row) => total + Number(row.montoTotal), 0))}
                  </dd>
                </div>
                <div className="rounded-md bg-slate-50 p-3">
                  <dt className="text-xs text-slate-500">Sectores con recaudación</dt>
                  <dd className="mt-1 text-lg font-semibold text-slate-900">{formatNumber(sectorRevenue.length)}</dd>
                </div>
              </dl>
              <SectorRevenueChart rows={sectorRevenue} />
            </>
          )}
        </div>
        <p className="mt-4 text-xs leading-5 text-slate-500">
          Excluye pagos anulados y obligaciones personales sin suministro.
        </p>
      </Panel>

      <Panel>
        <h2 className="text-base font-semibold text-slate-900">Alcance del resumen</h2>
        <p className="mt-2 text-sm leading-6 text-slate-600">
          Las obligaciones mostradas aquí son únicamente las generadas durante el período
          seleccionado que actualmente continúan pendientes. Para consultar todas las
          obligaciones pendientes utiliza el módulo de Reportes.
        </p>
      </Panel>
    </div>
  )
}

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20'

function SectorRevenueChart({ rows }) {
  if (rows.length === 0) {
    return <EmptyState title="Sin recaudación por sector" description="No hay pagos registrados por sector para el período seleccionado." />
  }

  const maximum = Math.max(...rows.map((row) => row.montoTotal), 1)

  return (
    <div className="overflow-x-auto rounded-md focus:outline-none focus:ring-2 focus:ring-[#28727a]" tabIndex={0} role="region" aria-label="Gráfica de recaudación por sector, montos en quetzales">
      <ul className="flex min-w-full items-start gap-4 px-2 pb-2" style={{ width: `${rows.length * 128}px` }}>
        {rows.map((row) => (
          <li className="min-w-0 flex-1 text-center" key={row.sectorId}>
            <p className="mb-2 break-words text-xs font-semibold tabular-nums text-slate-800">{formatMoney(row.montoTotal)}</p>
            <div className="flex h-48 items-end justify-center border-b border-slate-300 bg-slate-50" aria-hidden="true">
              <div className="w-12 rounded-t-md bg-[#28727a] sm:w-16" style={{ height: `${(row.montoTotal / maximum) * 100}%` }} />
            </div>
            <p className="mt-3 break-words text-sm font-medium text-slate-700">{row.nombreSector}</p>
          </li>
        ))}
      </ul>
    </div>
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

function formatMoney(value) {
  return new Intl.NumberFormat('es-GT', {
    style: 'currency',
    currency: 'GTQ',
  }).format(Number(value) || 0)
}

function formatNumber(value) {
  return new Intl.NumberFormat('es-GT').format(Number(value) || 0)
}

function getRequestMessage(error) {
  if (error?.status === 400) return error.message || 'El período seleccionado no es válido.'
  if (error?.status === 401) return 'Tu sesión ya no es válida. Inicia sesión nuevamente.'
  if (error?.status === 403) return 'No tienes permiso para consultar el Dashboard.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor.'
  return 'No se pudo cargar el resumen administrativo.'
}

export default DashboardPage
