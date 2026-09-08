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

export function getSuministroQr(authenticatedRequest, id) {
  return authenticatedRequest(`${suministroPath}/${id}/qr`)
}

export function getSuministroByQrToken(authenticatedRequest, token) {
  return authenticatedRequest(`${suministroPath}/qr/${encodeURIComponent(token)}`)
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

export function cancelSuministro(authenticatedRequest, id, proceso) {
  return authenticatedRequest(`${suministroPath}/${id}/cancelacion`, {
    body: proceso,
    method: 'POST',
  })
}

export function reconnectSuministro(authenticatedRequest, id, proceso) {
  return authenticatedRequest(`${suministroPath}/${id}/reconexion`, {
    body: proceso,
    method: 'POST',
  })
}

export function getSuministroProcesos(authenticatedRequest, id) {
  return authenticatedRequest(`${suministroPath}/${id}/procesos`)
}