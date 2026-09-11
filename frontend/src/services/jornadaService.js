const jornadaPath = '/jornadas'

export function getJornadas(authenticatedRequest) {
  return authenticatedRequest(jornadaPath)
}

export function getJornadaById(authenticatedRequest, id) {
  return authenticatedRequest(`${jornadaPath}/${id}`)
}

export function createJornada(authenticatedRequest, jornada) {
  return authenticatedRequest(jornadaPath, {
    body: jornada,
    method: 'POST',
  })
}

export function updateJornada(authenticatedRequest, id, jornada) {
  return authenticatedRequest(`${jornadaPath}/${id}`, {
    body: jornada,
    method: 'PUT',
  })
}

export function cancelJornada(authenticatedRequest, id, motivo) {
  return authenticatedRequest(`${jornadaPath}/${id}/cancelacion`, {
    body: { motivo },
    method: 'POST',
  })
}

export function addJornadaParticipants(authenticatedRequest, id, personaIds) {
  return authenticatedRequest(`${jornadaPath}/${id}/participantes`, {
    body: { personaIds },
    method: 'POST',
  })
}

export function removeJornadaParticipant(authenticatedRequest, id, personaId) {
  return authenticatedRequest(`${jornadaPath}/${id}/participantes/${personaId}`, {
    method: 'DELETE',
  })
}

export function updateJornadaParticipant(
  authenticatedRequest,
  id,
  personaId,
  input,
) {
  return authenticatedRequest(`${jornadaPath}/${id}/participantes/${personaId}`, {
    body: input,
    method: 'PUT',
  })
}

export function closeJornada(authenticatedRequest, id) {
  return authenticatedRequest(`${jornadaPath}/${id}/cierre`, {
    method: 'POST',
  })
}
