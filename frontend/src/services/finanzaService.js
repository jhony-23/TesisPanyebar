const finanzasPath = '/finanzas'
const egresosPath = '/egresos'

function queryString(filters = {}) {
  const params = new URLSearchParams()

  if (filters.fechaDesde) params.set('fechaDesde', filters.fechaDesde)
  if (filters.fechaHasta) params.set('fechaHasta', filters.fechaHasta)
  if (filters.tipo) params.set('tipo', filters.tipo)
  if (filters.estado) params.set('estado', filters.estado)

  const query = params.toString()
  return query ? `?${query}` : ''
}

export function getResumenFinanciero(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${finanzasPath}/resumen${queryString(filters)}`)
}

export function getMovimientosFinancieros(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${finanzasPath}/movimientos${queryString(filters)}`)
}

export function getIngresosFinancieros(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${finanzasPath}/ingresos${queryString(filters)}`)
}

export function getEgresos(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${egresosPath}${queryString(filters)}`)
}

export function getEgresoById(authenticatedRequest, id) {
  return authenticatedRequest(`${egresosPath}/${id}`)
}

export function createEgreso(authenticatedRequest, input) {
  return authenticatedRequest(egresosPath, {
    body: input,
    method: 'POST',
  })
}

export function updateEgreso(authenticatedRequest, id, input) {
  return authenticatedRequest(`${egresosPath}/${id}`, {
    body: input,
    method: 'PUT',
  })
}

export function annulEgreso(authenticatedRequest, id) {
  return authenticatedRequest(`${egresosPath}/${id}/anulacion`, {
    method: 'POST',
  })
}
