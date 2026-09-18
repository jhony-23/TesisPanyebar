const adminPath = '/admin'
const committeePath = '/administracion-comite'

export function getAdministraciones(authenticatedRequest) {
  return authenticatedRequest(committeePath)
}

export function getAdministracion(authenticatedRequest, id) {
  return authenticatedRequest(`${committeePath}/${id}`)
}

export function getCargosAdministracion(authenticatedRequest) {
  return authenticatedRequest(`${committeePath}/cargos`)
}

export function createAdministracion(authenticatedRequest, payload) {
  return authenticatedRequest(committeePath, {
    body: payload,
    method: 'POST',
  })
}

export function setIntegranteAdministracion(
  authenticatedRequest,
  administracionId,
  payload,
) {
  return authenticatedRequest(
    `${committeePath}/${administracionId}/integrante`,
    {
      body: payload,
      method: 'PUT',
    },
  )
}

export function finishAdministracion(
  authenticatedRequest,
  administracionId,
  fechaFin,
) {
  return authenticatedRequest(
    `${committeePath}/${administracionId}/finalizar`,
    {
      body: { fechaFin },
      method: 'POST',
    },
  )
}

export function getUsuariosAdministrativos(authenticatedRequest) {
  return authenticatedRequest(`${adminPath}/usuarios`)
}

export function getUsuarioAdministrativo(authenticatedRequest, id) {
  return authenticatedRequest(`${adminPath}/usuarios/${id}`)
}

export function createUsuarioAdministrativo(authenticatedRequest, payload) {
  return authenticatedRequest(`${adminPath}/usuarios`, {
    body: payload,
    method: 'POST',
  })
}

export function setUsuarioAdministrativoEstado(
  authenticatedRequest,
  id,
  estado,
) {
  return authenticatedRequest(`${adminPath}/usuarios/${id}/estado`, {
    body: { estado },
    method: 'PATCH',
  })
}

export function setUsuarioAdministrativoRoles(
  authenticatedRequest,
  id,
  roleIds,
) {
  return authenticatedRequest(`${adminPath}/usuarios/${id}/roles`, {
    body: { roleIds },
    method: 'PUT',
  })
}

export function resetUsuarioAdministrativoPassword(
  authenticatedRequest,
  id,
  nuevaPassword,
) {
  return authenticatedRequest(`${adminPath}/usuarios/${id}/password`, {
    body: { nuevaPassword },
    method: 'PUT',
  })
}

export function getRolesAdministrativos(authenticatedRequest) {
  return authenticatedRequest(`${adminPath}/roles`)
}

export function getRolAdministrativo(authenticatedRequest, id) {
  return authenticatedRequest(`${adminPath}/roles/${id}`)
}

export function createRolAdministrativo(authenticatedRequest, payload) {
  return authenticatedRequest(`${adminPath}/roles`, {
    body: payload,
    method: 'POST',
  })
}

export function setRolAdministrativoEstado(
  authenticatedRequest,
  id,
  estado,
) {
  return authenticatedRequest(`${adminPath}/roles/${id}/estado`, {
    body: { estado },
    method: 'PATCH',
  })
}

export function setRolPermisos(authenticatedRequest, id, permisoIds) {
  return authenticatedRequest(`${adminPath}/roles/${id}/permisos`, {
    body: { permisoIds },
    method: 'PUT',
  })
}

export function getPermisosAdministrativos(authenticatedRequest) {
  return authenticatedRequest(`${adminPath}/permisos`)
}
export function getAuditoriaAdministrativa(
  authenticatedRequest,
  filters = {},
) {
  const params = new URLSearchParams()

  if (filters.fechaDesde) {
    params.set('fechaDesde', filters.fechaDesde)
  }

  if (filters.fechaHasta) {
    params.set('fechaHasta', filters.fechaHasta)
  }

  if (filters.usuarioId) {
    params.set('usuarioId', String(filters.usuarioId))
  }

  if (filters.accion?.trim()) {
    params.set('accion', filters.accion.trim())
  }

  if (filters.entidad?.trim()) {
    params.set('entidad', filters.entidad.trim())
  }

  params.set('limite', String(filters.limite ?? 200))

  return authenticatedRequest(
    `${adminPath}/auditoria?${params.toString()}`,
  )
}
