const requestPath = '/solicitudes-nuevo-servicio'

export function getSolicitudesNuevoServicio(authenticatedRequest) {
  return authenticatedRequest(requestPath)
}

export function getSolicitudNuevoServicioById(authenticatedRequest, id) {
  return authenticatedRequest(`${requestPath}/${id}`)
}

export function createSolicitudNuevoServicio(authenticatedRequest, solicitud) {
  return authenticatedRequest(requestPath, { body: solicitud, method: 'POST' })
}

export function approveSolicitudNuevoServicio(authenticatedRequest, id, resolution) {
  return authenticatedRequest(`${requestPath}/${id}/aprobacion`, { body: resolution, method: 'POST' })
}

export function rejectSolicitudNuevoServicio(authenticatedRequest, id, resolution) {
  return authenticatedRequest(`${requestPath}/${id}/rechazo`, { body: resolution, method: 'POST' })
}