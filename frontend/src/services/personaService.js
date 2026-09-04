const personaPath = '/personas'

export function getPersonas(authenticatedRequest) {
  return authenticatedRequest(personaPath)
}

export function getPersonaById(authenticatedRequest, id) {
  return authenticatedRequest(`${personaPath}/${id}`)
}

export function createPersona(authenticatedRequest, persona) {
  return authenticatedRequest(personaPath, { body: persona, method: 'POST' })
}

export function updatePersona(authenticatedRequest, id, persona) {
  return authenticatedRequest(`${personaPath}/${id}`, { body: persona, method: 'PUT' })
}

export function setPersonaEstado(authenticatedRequest, id, estado) {
  return authenticatedRequest(`${personaPath}/${id}/estado`, {
    body: { estado },
    method: 'PATCH',
  })
}
