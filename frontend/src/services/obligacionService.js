const obligacionPath = '/obligaciones'

export function getObligaciones(authenticatedRequest) {
  return authenticatedRequest(obligacionPath)
}

export function generateObligacionFromCuota(authenticatedRequest, input) {
  return authenticatedRequest(`${obligacionPath}/desde-cuota`, {
    body: input,
    method: 'POST',
  })
}

export function annulObligacion(authenticatedRequest, id, motivo) {
  return authenticatedRequest(`${obligacionPath}/${id}/anulacion`, {
    body: { motivo },
    method: 'POST',
  })
}
