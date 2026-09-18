import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../app/useAuth.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import {
  getReporteJornadas,
  getReporteObligacionesPendientes,
  getReportePagos,
} from '../services/dashboardReporteService.js'
import { formatCivilDate, formatGuatemalaDateTime } from '../utils/dateTime.js'

const REPORT_PAGOS = 'pagos'
const REPORT_OBLIGACIONES = 'obligaciones'
const REPORT_JORNADAS = 'jornadas'

const initialFilters = {
  fechaDesde: '',
  fechaHasta: '',
}

function ReportesPage() {
  const { authenticatedRequest } = useAuth()
  const requestRef = useRef(authenticatedRequest)

  const [reportType, setReportType] = useState(REPORT_PAGOS)
  const [filters, setFilters] = useState(initialFilters)
  const [data, setData] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState(null)

  const loadReport = useCallback(async (type, currentFilters = initialFilters) => {
    setIsLoading(true)
    setError(null)

    try {
      let result

      if (type === REPORT_PAGOS) {
        result = await getReportePagos(requestRef.current, currentFilters)
      } else if (type === REPORT_OBLIGACIONES) {
        result = await getReporteObligacionesPendientes(requestRef.current)
      } else {
        result = await getReporteJornadas(requestRef.current, currentFilters)
      }

      setData(Array.isArray(result) ? result : [])
    } catch (requestError) {
      setData([])
      setError(getRequestMessage(requestError))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false

    async function loadInitialReport() {
      try {
        const result = await getReportePagos(requestRef.current)

        if (!cancelled) {
          setData(Array.isArray(result) ? result : [])
        }
      } catch (requestError) {
        if (!cancelled) {
          setData([])
          setError(getRequestMessage(requestError))
        }
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }

    loadInitialReport()

    return () => {
      cancelled = true
    }
  }, [])

  function selectReport(type) {
    setReportType(type)
    setFilters(initialFilters)
    loadReport(type, initialFilters)
  }

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

    loadReport(reportType, filters)
  }

  function clearFilters() {
    setFilters(initialFilters)
    loadReport(reportType, initialFilters)
  }

  const supportsDates = reportType !== REPORT_OBLIGACIONES

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        description="Consulta información estructurada de pagos, obligaciones pendientes y participación en jornadas."
        eyebrow="Información administrativa"
        title="Reportes"
      />

      {error && <Alert message={error} />}

      <Panel>
        <div>
          <h2 className="text-base font-semibold text-slate-900">Tipo de reporte</h2>
          <p className="mt-1 text-sm text-slate-500">
            Selecciona la información administrativa que deseas consultar.
          </p>
        </div>

        <div className="mt-5 grid gap-2 sm:grid-cols-3">
          <ReportButton
            active={reportType === REPORT_PAGOS}
            label="Pagos"
            onClick={() => selectReport(REPORT_PAGOS)}
          />
          <ReportButton
            active={reportType === REPORT_OBLIGACIONES}
            label="Obligaciones pendientes"
            onClick={() => selectReport(REPORT_OBLIGACIONES)}
          />
          <ReportButton
            active={reportType === REPORT_JORNADAS}
            label="Participación en jornadas"
            onClick={() => selectReport(REPORT_JORNADAS)}
          />
        </div>
      </Panel>

      {supportsDates && (
        <Panel>
          <h2 className="text-base font-semibold text-slate-900">Filtros</h2>
          <p className="mt-1 text-sm text-slate-500">
            Puedes limitar el reporte utilizando un rango de fechas.
          </p>

          <form className="mt-5 grid gap-4 md:grid-cols-2" onSubmit={applyFilters}>
            <label className="text-sm font-medium text-slate-700" htmlFor="reporte-desde">
              Fecha desde
              <input
                className={inputClass}
                id="reporte-desde"
                name="fechaDesde"
                onChange={changeFilter}
                type="date"
                value={filters.fechaDesde}
              />
            </label>

            <label className="text-sm font-medium text-slate-700" htmlFor="reporte-hasta">
              Fecha hasta
              <input
                className={inputClass}
                id="reporte-hasta"
                min={filters.fechaDesde || undefined}
                name="fechaHasta"
                onChange={changeFilter}
                type="date"
                value={filters.fechaHasta}
              />
            </label>

            <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end md:col-span-2">
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
      )}

      <Panel>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold text-slate-900">
              {reportTitle(reportType)}
            </h2>
            <p className="mt-1 text-sm leading-6 text-slate-500">
              {reportDescription(reportType)}
            </p>
          </div>

          <span className="rounded-md bg-slate-50 px-3 py-2 text-xs text-slate-600">
            {data.length} {data.length === 1 ? 'registro' : 'registros'}
          </span>
        </div>

        <div className="mt-5">
          {isLoading ? (
            <LoadingState message="Cargando reporte..." />
          ) : data.length === 0 ? (
            <EmptyState
              description="No existen registros que coincidan con la consulta seleccionada."
              title="Sin resultados"
            />
          ) : reportType === REPORT_PAGOS ? (
            <PaymentsReport rows={data} />
          ) : reportType === REPORT_OBLIGACIONES ? (
            <ObligationsReport rows={data} />
          ) : (
            <WorkDaysReport rows={data} />
          )}
        </div>
      </Panel>
    </div>
  )
}

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20'

function ReportButton({ active, label, onClick }) {
  return (
    <button
      className={`min-h-11 rounded-md border px-4 py-2.5 text-sm font-semibold transition-colors ${
        active
          ? 'border-[#123b43] bg-[#123b43] text-white'
          : 'border-slate-300 bg-white text-slate-700 hover:bg-slate-50'
      }`}
      onClick={onClick}
      type="button"
    >
      {label}
    </button>
  )
}

function PaymentsReport({ rows }) {
  return (
    <ResponsiveList
      columns={['Fecha', 'Concepto', 'Estado', 'Monto']}
      rows={rows.map((row) => ({
        key: row.pagoId,
        cells: [
          formatGuatemalaDateTime(row.fecha),
          row.concepto,
          <PaymentStatus key="status" status={row.estado} />,
          formatMoney(row.monto),
        ],
        mobileTitle: row.concepto,
        mobileMeta: formatGuatemalaDateTime(row.fecha),
        mobileItems: [
          ['Estado', paymentStatusName(row.estado)],
          ['Monto', formatMoney(row.monto)],
        ],
      }))}
    />
  )
}

function ObligationsReport({ rows }) {
  return (
    <ResponsiveList
      columns={['Titular', 'Concepto', 'Origen', 'Generación', 'Monto', 'Situación']}
      rows={rows.map((row) => ({
        key: row.obligacionId,
        cells: [
          obligationHolder(row),
          row.concepto,
          originName(row.origen),
          formatGuatemalaDateTime(row.fechaGeneracion),
          formatMoney(row.monto),
          row.esMorosa ? 'Pendiente · Morosa' : 'Pendiente',
        ],
        mobileTitle: row.concepto,
        mobileMeta: obligationHolder(row),
        mobileItems: [
          ['Origen', originName(row.origen)],
          ['Generación', formatGuatemalaDateTime(row.fechaGeneracion)],
          ['Monto', formatMoney(row.monto)],
          ['Situación', row.esMorosa ? 'Pendiente · Morosa' : 'Pendiente'],
        ],
      }))}
    />
  )
}

function WorkDaysReport({ rows }) {
  return (
    <ResponsiveList
      columns={['Jornada', 'Fecha', 'Persona', 'Resultado', 'Estado']}
      rows={rows.map((row) => ({
        key: row.participacionId,
        cells: [
          row.nombreJornada,
          formatCivilDate(row.fechaJornada),
          row.nombrePersona,
          participationName(row.resultado),
          workDayStatusName(row.estadoJornada),
        ],
        mobileTitle: row.nombreJornada,
        mobileMeta: formatCivilDate(row.fechaJornada),
        mobileItems: [
          ['Persona', row.nombrePersona],
          ['Resultado', participationName(row.resultado)],
          ['Estado', workDayStatusName(row.estadoJornada)],
          ...(row.observacion ? [['Observación', row.observacion]] : []),
        ],
      }))}
    />
  )
}

function ResponsiveList({ columns, rows }) {
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[780px] text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              {columns.map((column) => (
                <th className="pb-3 pr-4 font-semibold last:pr-0" key={column}>
                  {column}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {rows.map((row) => (
              <tr key={row.key}>
                {row.cells.map((cell, index) => (
                  <td
                    className="max-w-72 py-4 pr-4 align-top text-slate-700 last:pr-0"
                    key={`${row.key}-${columns[index]}`}
                  >
                    {cell}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="space-y-3 md:hidden">
        {rows.map((row) => (
          <article className="rounded-md border border-slate-200 p-4" key={row.key}>
            <h3 className="font-semibold text-slate-900">{row.mobileTitle}</h3>
            <p className="mt-1 text-xs text-slate-500">{row.mobileMeta}</p>

            <dl className="mt-4 grid gap-3">
              {row.mobileItems.map(([label, value]) => (
                <div className="rounded-md bg-slate-50 p-3" key={label}>
                  <dt className="text-xs text-slate-500">{label}</dt>
                  <dd className="mt-1 break-words text-sm font-medium text-slate-800">{value}</dd>
                </div>
              ))}
            </dl>
          </article>
        ))}
      </div>
    </>
  )
}

function PaymentStatus({ status }) {
  const registered = status === 1

  return (
    <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${
      registered
        ? 'bg-emerald-50 text-emerald-700'
        : 'bg-slate-100 text-slate-600'
    }`}>
      {paymentStatusName(status)}
    </span>
  )
}

function paymentStatusName(status) {
  if (status === 1) return 'Registrado'
  if (status === 2) return 'Anulado'
  return 'Desconocido'
}

function participationName(result) {
  if (result === 0) return 'Pendiente'
  if (result === 1) return 'Participación'
  if (result === 2) return 'Ausencia'
  if (result === 3) return 'Ausencia justificada'
  return 'Desconocido'
}

function workDayStatusName(status) {
  if (status === 1) return 'Planificada'
  if (status === 2) return 'Cerrada'
  if (status === 3) return 'Cancelada'
  return 'Desconocido'
}

function originName(origin) {
  if (origin === 1) return 'Cuota ordinaria'
  if (origin === 2) return 'Jornada'
  if (origin === 3) return 'Reconexión'
  if (origin === 4) return 'Otro'
  return 'Desconocido'
}

function obligationHolder(row) {
  if (row.suministroId) return row.nis || `Suministro #${row.suministroId}`
  if (row.personaId) return row.nombrePersona || `Persona #${row.personaId}`
  return 'Sin titular'
}

function reportTitle(type) {
  if (type === REPORT_PAGOS) return 'Reporte de pagos'
  if (type === REPORT_OBLIGACIONES) return 'Obligaciones pendientes'
  return 'Participación en jornadas'
}

function reportDescription(type) {
  if (type === REPORT_PAGOS) {
    return 'Historial de pagos registrados y anulados dentro del rango consultado.'
  }

  if (type === REPORT_OBLIGACIONES) {
    return 'Obligaciones que actualmente permanecen pendientes de cancelación.'
  }

  return 'Resultados registrados para las personas participantes en jornadas comunitarias.'
}

function formatMoney(value) {
  return new Intl.NumberFormat('es-GT', {
    style: 'currency',
    currency: 'GTQ',
  }).format(Number(value) || 0)
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

function getRequestMessage(error) {
  if (error?.status === 400) return error.message || 'El rango de fechas no es válido.'
  if (error?.status === 401) return 'Tu sesión ya no es válida. Inicia sesión nuevamente.'
  if (error?.status === 403) return 'No tienes permiso para consultar los reportes.'
  if (error?.kind === 'network') return 'No se pudo conectar con el servidor.'
  if (error?.status >= 500) return 'Ocurrió un error en el servidor.'
  return 'No se pudo cargar el reporte.'
}

export default ReportesPage
