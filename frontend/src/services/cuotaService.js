const cuotaPath = '/cuotas'

export function getCuotas(authenticatedRequest) {
  return authenticatedRequest(cuotaPath)
}

export function createCuota(authenticatedRequest, cuota) {
  return authenticatedRequest(cuotaPath, { body: cuota, method: 'POST' })
}

export function updateCuota(authenticatedRequest, id, cuota) {
  return authenticatedRequest(`${cuotaPath}/${id}`, { body: cuota, method: 'PUT' })
}

export function setCuotaEstado(authenticatedRequest, id, estado) {
  return authenticatedRequest(`${cuotaPath}/${id}/estado`, {
    body: { estado },
    method: 'PATCH',
  })
}
