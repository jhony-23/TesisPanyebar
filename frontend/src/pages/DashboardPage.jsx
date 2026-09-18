import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import { getDashboardResumen } from '../services/dashboardReporteService.js'
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

function DashboardPage() {
  const { authenticatedRequest } = useAuth()
  const requestRef = useRef(authenticatedRequest)
  const [period, setPeriod] = useState(currentPeriod)
  const [appliedPeriod, setAppliedPeriod] = useState(currentPeriod)
  const [summary, setSummary] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState(null)

  const loadSummary = useCallback(async (selectedPeriod) => {
    setIsLoading(true)
    setError(null)

    try {
      const data = await getDashboardResumen(
        requestRef.current,
        selectedPeriod.anio,
        selectedPeriod.mes,
      )

      setSummary(data)
    } catch (requestError) {
      setError(getRequestMessage(requestError))
      setSummary(null)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    const selectedPeriod = currentPeriod()

    let cancelled = false

    async function loadInitialSummary() {
      try {
        const data = await getDashboardResumen(
          requestRef.current,
          selectedPeriod.anio,
          selectedPeriod.mes,
        )

        if (!cancelled) setSummary(data)
      } catch (requestError) {
        if (!cancelled) {
          setError(getRequestMessage(requestError))
          setSummary(null)
        }
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }

    loadInitialSummary()

    return () => {
      cancelled = true
    }
  }, [])

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
    setAppliedPeriod(nextPeriod)
    loadSummary(nextPeriod)
  }

  function restoreCurrentPeriod() {
    const nextPeriod = currentPeriod()

    setPeriod(nextPeriod)
    setAppliedPeriod(nextPeriod)
    loadSummary(nextPeriod)
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
