export const PERMISOS = {
  // Sectores
  SECTORES_VER: 'SECTORES.VER',
  SECTORES_GESTIONAR: 'SECTORES.GESTIONAR',

  // Personas
  PERSONAS_VER: 'PERSONAS.VER',
  PERSONAS_GESTIONAR: 'PERSONAS.GESTIONAR',

  // Suministros (y solicitudes que reutilizan este contrato)
  SUMINISTROS_VER: 'SUMINISTROS.VER',
  SUMINISTROS_GESTIONAR: 'SUMINISTROS.GESTIONAR',

  // Cuotas
  CUOTAS_VER: 'CUOTAS.VER',
  CUOTAS_GESTIONAR: 'CUOTAS.GESTIONAR',

  // Obligaciones
  OBLIGACIONES_VER: 'OBLIGACIONES.VER',
  OBLIGACIONES_GESTIONAR: 'OBLIGACIONES.GESTIONAR',

  // Jornadas
  JORNADAS_VER: 'JORNADAS.VER',
  JORNADAS_GESTIONAR: 'JORNADAS.GESTIONAR',

  // Pagos
  PAGOS_VER: 'PAGOS.VER',
  PAGOS_GESTIONAR: 'PAGOS.GESTIONAR',

  // Finanzas
  FINANZAS_VER: 'FINANZAS.VER',
  FINANZAS_GESTIONAR: 'FINANZAS.GESTIONAR',

  // Dashboard & Reportes
  DASHBOARD_VER: 'DASHBOARD.VER',
  REPORTES_VER: 'REPORTES.VER',

  // Abastecimiento
  ABASTECIMIENTO_VER: 'ABASTECIMIENTO.VER',
  ABASTECIMIENTO_GESTIONAR: 'ABASTECIMIENTO.GESTIONAR',

  // Administración del Comité
  ADMINISTRACION_VER: 'ADMINISTRACION.VER',
  ADMINISTRACION_GESTIONAR: 'ADMINISTRACION.GESTIONAR',

  // Seguridad / Granulares
  USUARIOS_VER: 'SEGURIDAD.USUARIOS.VER',
  USUARIOS_GESTIONAR: 'SEGURIDAD.USUARIOS.GESTIONAR',
  ROLES_VER: 'SEGURIDAD.ROLES.VER',
  ROLES_GESTIONAR: 'SEGURIDAD.ROLES.GESTIONAR',
  PERMISOS_VER: 'SEGURIDAD.PERMISOS.VER',
  PERMISOS_ASIGNAR: 'SEGURIDAD.PERMISOS.ASIGNAR',
  AUDITORIA_VER: 'SEGURIDAD.AUDITORIA.VER',
}

export const ADMIN_MODULE_PERMISSIONS = [
  PERMISOS.ADMINISTRACION_VER,
  PERMISOS.ADMINISTRACION_GESTIONAR,
  PERMISOS.USUARIOS_VER,
  PERMISOS.USUARIOS_GESTIONAR,
  PERMISOS.ROLES_VER,
  PERMISOS.ROLES_GESTIONAR,
  PERMISOS.PERMISOS_VER,
  PERMISOS.PERMISOS_ASIGNAR,
  PERMISOS.AUDITORIA_VER,
]

export const NAVIGATION_ITEMS = [
  { label: 'Inicio', path: '/admin', available: true, icon: 'home', permissions: null },
  { label: 'Personas', path: '/admin/personas', available: true, icon: 'users', permissions: [PERMISOS.PERSONAS_VER, PERMISOS.PERSONAS_GESTIONAR] },
  { label: 'Sectores', path: '/admin/sectores', available: true, icon: 'map', permissions: [PERMISOS.SECTORES_VER, PERMISOS.SECTORES_GESTIONAR] },
  { label: 'Suministros', path: '/admin/suministros', available: true, icon: 'drop', permissions: [PERMISOS.SUMINISTROS_VER, PERMISOS.SUMINISTROS_GESTIONAR] },
  { label: 'Solicitudes', path: '/admin/solicitudes', available: true, icon: 'inbox', permissions: [PERMISOS.SUMINISTROS_VER, PERMISOS.SUMINISTROS_GESTIONAR] },
  { label: 'Cuotas', path: '/admin/cuotas', available: true, icon: 'coins', permissions: [PERMISOS.CUOTAS_VER, PERMISOS.CUOTAS_GESTIONAR] },
  { label: 'Obligaciones', path: '/admin/obligaciones', available: true, icon: 'document', permissions: [PERMISOS.OBLIGACIONES_VER, PERMISOS.OBLIGACIONES_GESTIONAR] },
  { label: 'Jornadas', path: '/admin/jornadas', available: true, icon: 'calendar', permissions: [PERMISOS.JORNADAS_VER, PERMISOS.JORNADAS_GESTIONAR] },
  { label: 'Pagos', path: '/admin/pagos', available: true, icon: 'card', permissions: [PERMISOS.PAGOS_VER, PERMISOS.PAGOS_GESTIONAR] },
  { label: 'Finanzas', path: '/admin/finanzas', available: true, icon: 'chart', permissions: [PERMISOS.FINANZAS_VER, PERMISOS.FINANZAS_GESTIONAR] },
  { label: 'Dashboard', path: '/admin/dashboard', available: true, icon: 'chart', permissions: [PERMISOS.DASHBOARD_VER] },
  { label: 'Reportes', path: '/admin/reportes', available: true, icon: 'report', permissions: [PERMISOS.REPORTES_VER] },
  { label: 'Abastecimiento', path: '/admin/abastecimiento', available: true, icon: 'water', permissions: [PERMISOS.ABASTECIMIENTO_VER, PERMISOS.ABASTECIMIENTO_GESTIONAR] },
  { label: 'Administración', path: '/admin/administracion', available: true, icon: 'settings', permissions: ADMIN_MODULE_PERMISSIONS },
]
