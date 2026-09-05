const suministroPath = '/suministros'

export function getSuministros(authenticatedRequest) {
  return authenticatedRequest(suministroPath)
}

export function getSuministroById(authenticatedRequest, id) {
  return authenticatedRequest(`${suministroPath}/${id}`)
}

export function getSuministroByNis(authenticatedRequest, nis) {
  return authenticatedRequest(`${suministroPath}/nis/${encodeURIComponent(nis)}`)
}

export function getSuministroResponsables(authenticatedRequest, id) {
  return authenticatedRequest(`${suministroPath}/${id}/responsables`)
}

export function setSuministroResponsable(authenticatedRequest, id, personaId) {
  return authenticatedRequest(`${suministroPath}/${id}/responsable`, {
    body: { personaId },
    method: 'PUT',
  })
}

export function createSuministro(authenticatedRequest, suministro) {
  return authenticatedRequest(suministroPath, { body: suministro, method: 'POST' })
}

export function updateSuministro(authenticatedRequest, id, suministro) {
  return authenticatedRequest(`${suministroPath}/${id}`, { body: suministro, method: 'PUT' })
}

export function setSuministroEstado(authenticatedRequest, id, estado) {
  return authenticatedRequest(`${suministroPath}/${id}/estado`, {
    body: { estado },
    method: 'PATCH',
  })
}