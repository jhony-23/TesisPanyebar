const dashboardPath = '/dashboard'
const reportesPath = '/reportes'

function reportQuery(filters = {}) {
  const params = new URLSearchParams()

  if (filters.fechaDesde) params.set('fechaDesde', filters.fechaDesde)
  if (filters.fechaHasta) params.set('fechaHasta', filters.fechaHasta)

  const query = params.toString()
  return query ? `?${query}` : ''
}

export function getDashboardResumen(authenticatedRequest, anio, mes) {
  const params = new URLSearchParams({
    anio: String(anio),
    mes: String(mes),
  })

  return authenticatedRequest(`${dashboardPath}/resumen?${params}`)
}

export function getReportePagos(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${reportesPath}/pagos${reportQuery(filters)}`)
}

export function getRecaudacionPorSector(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${reportesPath}/recaudacion-por-sector${reportQuery(filters)}`)
}

export function getReporteObligacionesPendientes(authenticatedRequest) {
  return authenticatedRequest(`${reportesPath}/obligaciones-pendientes`)
}

export function getReporteJornadas(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${reportesPath}/jornadas${reportQuery(filters)}`)
}
