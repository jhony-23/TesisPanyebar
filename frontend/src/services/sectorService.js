const sectorPath = '/sectores'

export function getSectores(authenticatedRequest) {
  return authenticatedRequest(sectorPath)
}

export function getSectorById(authenticatedRequest, id) {
  return authenticatedRequest(`${sectorPath}/${id}`)
}

export function createSector(authenticatedRequest, sector) {
  return authenticatedRequest(sectorPath, {
    body: sector,
    method: 'POST',
  })
}

export function updateSector(authenticatedRequest, id, sector) {
  return authenticatedRequest(`${sectorPath}/${id}`, {
    body: sector,
    method: 'PUT',
  })
}

export function setSectorEstado(authenticatedRequest, id, estado) {
  return authenticatedRequest(`${sectorPath}/${id}/estado`, {
    body: { estado },
    method: 'PATCH',
  })
}
