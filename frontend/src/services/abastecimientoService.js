const abastecimientoPath = '/abastecimiento'

function buildQuery(filters = {}) {
  const params = new URLSearchParams()

  if (filters.fechaDesde) params.set('fechaDesde', filters.fechaDesde)
  if (filters.fechaHasta) params.set('fechaHasta', filters.fechaHasta)
  if (filters.sectorId) params.set('sectorId', String(filters.sectorId))

  const query = params.toString()
  return query ? `?${query}` : ''
}

export function getProgramaciones(authenticatedRequest, filters = {}) {
  return authenticatedRequest(`${abastecimientoPath}${buildQuery(filters)}`)
}

export function createProgramacion(authenticatedRequest, input) {
  return authenticatedRequest(abastecimientoPath, {
    method: 'POST',
    body: input,
  })
}

export function createProgramacionRecurrente(authenticatedRequest, input) {
  return authenticatedRequest(`${abastecimientoPath}/recurrente`, {
    method: 'POST',
    body: input,
  })
}

export function updateProgramacion(authenticatedRequest, id, input) {
  return authenticatedRequest(`${abastecimientoPath}/${id}`, {
    method: 'PUT',
    body: input,
  })
}

export function completeProgramacion(authenticatedRequest, id) {
  return authenticatedRequest(`${abastecimientoPath}/${id}/completado`, {
    method: 'POST',
  })
}

export function cancelProgramacion(authenticatedRequest, id, observacion = null) {
  return authenticatedRequest(`${abastecimientoPath}/${id}/cancelacion`, {
    method: 'POST',
    body: { observacion },
  })
}
