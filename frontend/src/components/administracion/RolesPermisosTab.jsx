import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  createRolAdministrativo,
  getPermisosAdministrativos,
  getRolAdministrativo,
  getRolesAdministrativos,
  setRolAdministrativoEstado,
  setRolPermisos,
} from '../../services/administracionService'

const ACTIVE = 1
const INACTIVE = 2

function RolesPermisosTab({ authenticatedRequest }) {
  const [roles, setRoles] = useState([])
  const [permisos, setPermisos] = useState([])
  const [selectedRoleId, setSelectedRoleId] = useState(null)
  const [selectedRole, setSelectedRole] = useState(null)
  const [selectedPermissions, setSelectedPermissions] = useState(
    () => new Set(),
  )
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingRole, setIsLoadingRole] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [search, setSearch] = useState('')
  const [permissionSearch, setPermissionSearch] = useState('')
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)
  const [dialog, setDialog] = useState(null)

  const loadBase = useCallback(async () => {
    const [roleData, permissionData] = await Promise.all([
      getRolesAdministrativos(authenticatedRequest),
      getPermisosAdministrativos(authenticatedRequest),
    ])

    const normalizedRoles =
      Array.isArray(roleData) ? roleData : []

    setRoles(normalizedRoles)
    setPermisos(
      Array.isArray(permissionData) ? permissionData : [],
    )

    return normalizedRoles
  }, [authenticatedRequest])

  const loadRole = useCallback(
    async (roleId) => {
      if (!roleId) {
        setSelectedRole(null)
        setSelectedPermissions(new Set())
        return
      }

      setIsLoadingRole(true)

      try {
        const role = await getRolAdministrativo(
          authenticatedRequest,
          roleId,
        )

        setSelectedRole(role)
        setSelectedPermissions(
          new Set(
            Array.isArray(role?.permisos)
              ? role.permisos.map((permission) => permission.id)
              : [],
          ),
        )
      } catch (requestError) {
        setError(
          requestMessage(
            requestError,
            'No se pudo cargar el detalle del rol.',
          ),
        )
      } finally {
        setIsLoadingRole(false)
      }
    },
    [authenticatedRequest],
  )

  useEffect(() => {
    let cancelled = false

    async function loadInitialData() {
      try {
        const [roleData, permissionData] = await Promise.all([
          getRolesAdministrativos(authenticatedRequest),
          getPermisosAdministrativos(authenticatedRequest),
        ])

        if (cancelled) return

        const normalizedRoles =
          Array.isArray(roleData) ? roleData : []

        setRoles(normalizedRoles)
        setPermisos(
          Array.isArray(permissionData)
            ? permissionData
            : [],
        )

        if (normalizedRoles.length > 0) {
          const firstRole = normalizedRoles[0]
          setSelectedRoleId(firstRole.id)

          const detail = await getRolAdministrativo(
            authenticatedRequest,
            firstRole.id,
          )

          if (cancelled) return

          setSelectedRole(detail)
          setSelectedPermissions(
            new Set(
              Array.isArray(detail?.permisos)
                ? detail.permisos.map(
                    (permission) => permission.id,
                  )
                : [],
            ),
          )
        }
      } catch (requestError) {
        if (cancelled) return

        setError(
          requestMessage(
            requestError,
            'No se pudieron cargar los roles y permisos.',
          ),
        )
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    loadInitialData()

    return () => {
      cancelled = true
    }
  }, [authenticatedRequest])

  const filteredRoles = useMemo(() => {
    const normalized =
      search.trim().toLocaleLowerCase('es')

    return roles
      .filter((role) => {
        if (!normalized) return true

        return `${role.nombre} ${role.descripcion ?? ''}`
          .toLocaleLowerCase('es')
          .includes(normalized)
      })
      .sort((first, second) =>
        first.nombre.localeCompare(
          second.nombre,
          'es',
          { sensitivity: 'base' },
        ),
      )
  }, [roles, search])

  const permissionGroups = useMemo(
    () =>
      groupPermissions(
        permisos,
        permissionSearch,
      ),
    [permissionSearch, permisos],
  )

  async function selectRole(role) {
    setSelectedRoleId(role.id)
    setError(null)
    setNotice(null)
    await loadRole(role.id)
  }

  function togglePermission(permissionId) {
    setSelectedPermissions((current) => {
      const next = new Set(current)

      if (next.has(permissionId)) {
        next.delete(permissionId)
      } else {
        next.add(permissionId)
      }

      return next
    })
  }

  function toggleGroup(group) {
    const ids = group.permissions
      .filter(
        (permission) =>
          permission.estado === ACTIVE ||
          selectedPermissions.has(permission.id),
      )
      .map((permission) => permission.id)

    const allSelected =
      ids.length > 0 &&
      ids.every((id) => selectedPermissions.has(id))

    setSelectedPermissions((current) => {
      const next = new Set(current)

      ids.forEach((id) => {
        if (allSelected) {
          next.delete(id)
        } else {
          next.add(id)
        }
      })

      return next
    })
  }

  async function savePermissions() {
    if (!selectedRole) return

    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      const updated = await setRolPermisos(
        authenticatedRequest,
        selectedRole.id,
        Array.from(selectedPermissions),
      )

      setSelectedRole(updated)
      setSelectedPermissions(
        new Set(
          Array.isArray(updated?.permisos)
            ? updated.permisos.map(
                (permission) => permission.id,
              )
            : [],
        ),
      )

      const refreshedRoles = await loadBase()
      const refreshed =
        refreshedRoles.find(
          (role) => role.id === selectedRole.id,
        ) ?? updated

      setSelectedRole((current) => ({
        ...current,
        estado: refreshed.estado,
      }))

      setNotice(
        `Los permisos de ${selectedRole.nombre} fueron actualizados.`,
      )
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudieron actualizar los permisos.',
        ),
      )
    } finally {
      setIsSaving(false)
    }
  }

  async function changeRoleStatus() {
    if (!selectedRole) return

    const nextState =
      selectedRole.estado === ACTIVE
        ? INACTIVE
        : ACTIVE

    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      const updated = await setRolAdministrativoEstado(
        authenticatedRequest,
        selectedRole.id,
        nextState,
      )

      setSelectedRole(updated)
      await loadBase()

      setNotice(
        nextState === ACTIVE
          ? `El rol ${updated.nombre} fue activado.`
          : `El rol ${updated.nombre} fue desactivado.`,
      )
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo cambiar el estado del rol.',
        ),
      )
    } finally {
      setIsSaving(false)
    }
  }

  async function createRole(payload) {
    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      const created = await createRolAdministrativo(
        authenticatedRequest,
        payload,
      )

      const refreshedRoles = await loadBase()

      setDialog(null)
      setSelectedRoleId(created.id)
      setSelectedRole(created)
      setSelectedPermissions(
        new Set(
          Array.isArray(created.permisos)
            ? created.permisos.map(
                (permission) => permission.id,
              )
            : [],
        ),
      )

      if (
        !refreshedRoles.some(
          (role) => role.id === created.id,
        )
      ) {
        setRoles((current) => [
          ...current,
          created,
        ])
      }

      setNotice(
        `El rol ${created.nombre} fue creado.`,
      )
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo crear el rol.',
        ),
      )
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return (
      <LoadingState message="Cargando roles y permisos..." />
    )
  }

  return (
    <div className="space-y-5">
      {error && (
        <Alert
          message={error}
          onDismiss={() => setError(null)}
          type="error"
        />
      )}

      {notice && (
        <Alert
          message={notice}
          onDismiss={() => setNotice(null)}
          type="success"
        />
      )}

      <div className="grid gap-5 lg:grid-cols-[18rem_minmax(0,1fr)]">
        <aside className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-100 p-4">
            <div className="flex items-center justify-between gap-3">
              <div>
                <h3 className="font-semibold text-slate-950">
                  Roles
                </h3>
                <p className="mt-1 text-xs text-slate-500">
                  {roles.length} registrados
                </p>
              </div>

              <button
                aria-label="Crear rol"
                className="flex h-9 w-9 items-center justify-center rounded-md bg-[#123b43] text-lg font-semibold text-white hover:bg-[#0d2d33]"
                onClick={() =>
                  setDialog({ type: 'create' })}
                type="button"
              >
                +
              </button>
            </div>

            <input
              className={inputClass}
              onChange={(event) =>
                setSearch(event.target.value)}
              placeholder="Buscar rol"
              type="search"
              value={search}
            />
          </div>

          {filteredRoles.length === 0 ? (
            <p className="p-5 text-sm text-slate-500">
              No se encontraron roles.
            </p>
          ) : (
            <div className="max-h-[34rem] overflow-y-auto p-2">
              {filteredRoles.map((role) => {
                const selected =
                  role.id === selectedRoleId

                return (
                  <button
                    className={`mb-1 w-full rounded-lg px-3 py-3 text-left transition ${
                      selected
                        ? 'bg-[#edf7f6] text-[#123b43]'
                        : 'text-slate-700 hover:bg-slate-50'
                    }`}
                    key={role.id}
                    onClick={() => selectRole(role)}
                    type="button"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <span className="font-semibold">
                        {role.nombre}
                      </span>

                      <span
                        className={`mt-1 h-2 w-2 shrink-0 rounded-full ${
                          role.estado === ACTIVE
                            ? 'bg-emerald-500'
                            : 'bg-slate-300'
                        }`}
                      />
                    </div>

                    <p className="mt-1 line-clamp-2 text-xs leading-5 text-slate-500">
                      {role.descripcion ||
                        'Sin descripción'}
                    </p>
                  </button>
                )
              })}
            </div>
          )}
        </aside>

        <section className="min-w-0">
          {!selectedRole ? (
            <EmptyRoleState
              onCreate={() =>
                setDialog({ type: 'create' })}
            />
          ) : isLoadingRole ? (
            <LoadingState message="Cargando detalle del rol..." />
          ) : (
            <RoleEditor
              isSaving={isSaving}
              onChangeStatus={changeRoleStatus}
              onSave={savePermissions}
              onToggleGroup={toggleGroup}
              onTogglePermission={togglePermission}
              permissionGroups={permissionGroups}
              permissionSearch={permissionSearch}
              role={selectedRole}
              selectedPermissions={selectedPermissions}
              setPermissionSearch={setPermissionSearch}
            />
          )}
        </section>
      </div>

      {dialog?.type === 'create' && (
        <CreateRoleDialog
          isSaving={isSaving}
          onCancel={() => setDialog(null)}
          onConfirm={createRole}
        />
      )}
    </div>
  )
}

function RoleEditor({
  isSaving,
  onChangeStatus,
  onSave,
  onToggleGroup,
  onTogglePermission,
  permissionGroups,
  permissionSearch,
  role,
  selectedPermissions,
  setPermissionSearch,
}) {
  const active = role.estado === ACTIVE

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-100 p-5 sm:p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-xl font-bold text-slate-950">
                {role.nombre}
              </h3>
              <StatusBadge active={active} />
            </div>

            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-500">
              {role.descripcion ||
                'Este rol no tiene una descripción registrada.'}
            </p>
          </div>

          <button
            className={`shrink-0 rounded-md border px-3 py-2 text-sm font-semibold ${
              active
                ? 'border-red-200 text-red-700 hover:bg-red-50'
                : 'border-emerald-200 text-emerald-700 hover:bg-emerald-50'
            }`}
            disabled={isSaving}
            onClick={onChangeStatus}
            type="button"
          >
            {active ? 'Desactivar rol' : 'Activar rol'}
          </button>
        </div>

        <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <label className="block min-w-0 flex-1 text-sm font-medium text-slate-700">
            Buscar permiso
            <input
              className={inputClass}
              onChange={(event) =>
                setPermissionSearch(
                  event.target.value,
                )}
              placeholder="Ej. personas, pagos, reportes..."
              type="search"
              value={permissionSearch}
            />
          </label>

          <div className="shrink-0 pb-2 text-sm text-slate-500">
            <strong className="text-slate-900">
              {selectedPermissions.size}
            </strong>{' '}
            permisos asignados
          </div>
        </div>
      </div>

      <div className="space-y-3 bg-slate-50/60 p-4 sm:p-5">
        {permissionGroups.length === 0 ? (
          <div className="rounded-lg bg-white px-4 py-8 text-center text-sm text-slate-500">
            No hay permisos que coincidan con la búsqueda.
          </div>
        ) : (
          permissionGroups.map((group) => (
            <PermissionGroup
              group={group}
              key={group.key}
              onToggleGroup={() =>
                onToggleGroup(group)}
              onTogglePermission={
                onTogglePermission
              }
              selectedPermissions={
                selectedPermissions
              }
            />
          ))
        )}
      </div>

      <div className="flex flex-col gap-3 border-t border-slate-100 bg-white p-4 sm:flex-row sm:items-center sm:justify-between sm:p-5">
        <p className="max-w-xl text-xs leading-5 text-slate-500">
          Los cambios críticos son validados nuevamente por el servidor para evitar dejar el sistema sin un administrador funcional.
        </p>

        <button
          className="shrink-0 rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#0d2d33] disabled:cursor-not-allowed disabled:opacity-60"
          disabled={isSaving}
          onClick={onSave}
          type="button"
        >
          {isSaving
            ? 'Guardando...'
            : 'Guardar permisos'}
        </button>
      </div>
    </div>
  )
}

function PermissionGroup({
  group,
  onToggleGroup,
  onTogglePermission,
  selectedPermissions,
}) {
  const availableIds = group.permissions
    .filter(
      (permission) =>
        permission.estado === ACTIVE ||
        selectedPermissions.has(permission.id),
    )
    .map((permission) => permission.id)

  const allSelected =
    availableIds.length > 0 &&
    availableIds.every((id) =>
      selectedPermissions.has(id),
    )

  return (
    <section className="overflow-hidden rounded-lg border border-slate-200 bg-white">
      <div className="flex flex-col gap-3 border-b border-slate-100 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h4 className="font-semibold text-slate-900">
            {group.label}
          </h4>
          <p className="mt-0.5 text-xs text-slate-400">
            {group.permissions.length}{' '}
            {group.permissions.length === 1
              ? 'permiso'
              : 'permisos'}
          </p>
        </div>

        <button
          className="self-start text-xs font-semibold text-[#176b72] hover:text-[#123b43]"
          onClick={onToggleGroup}
          type="button"
        >
          {allSelected
            ? 'Quitar grupo'
            : 'Seleccionar grupo'}
        </button>
      </div>

      <div className="divide-y divide-slate-100">
        {group.permissions.map((permission) => {
          const inactive =
            permission.estado === INACTIVE

          return (
            <label
              className={`flex gap-3 px-4 py-3 ${
                inactive
                  ? 'bg-slate-50'
                  : 'cursor-pointer hover:bg-slate-50'
              }`}
              key={permission.id}
            >
              <input
                checked={selectedPermissions.has(
                  permission.id,
                )}
                className="mt-0.5 h-4 w-4 shrink-0 accent-[#28727a]"
                disabled={
                  inactive &&
                  !selectedPermissions.has(
                    permission.id,
                  )
                }
                onChange={() =>
                  onTogglePermission(permission.id)}
                type="checkbox"
              />

              <span className="min-w-0">
                <span className="flex flex-wrap items-center gap-2">
                  <span className="text-sm font-semibold text-slate-800">
                    {permission.nombre}
                  </span>

                  {inactive && (
                    <span className="rounded-full bg-slate-200 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-500">
                      Inactivo
                    </span>
                  )}
                </span>

                {permission.descripcion && (
                  <span className="mt-0.5 block text-xs leading-5 text-slate-500">
                    {permission.descripcion}
                  </span>
                )}

                <code className="mt-1 block break-all text-[10px] text-slate-400">
                  {permission.codigo}
                </code>
              </span>
            </label>
          )
        })}
      </div>
    </section>
  )
}

function CreateRoleDialog({
  isSaving,
  onCancel,
  onConfirm,
}) {
  const [form, setForm] = useState({
    nombre: '',
    descripcion: '',
  })
  const [error, setError] = useState(null)

  function submit(event) {
    event.preventDefault()

    const nombre = form.nombre.trim()

    if (!nombre) {
      setError('Ingresa un nombre para el rol.')
      return
    }

    setError(null)

    onConfirm({
      nombre,
      descripcion:
        form.descripcion.trim() || null,
    })
  }

  return (
    <Dialog
      description="Después de crear el rol podrás seleccionar los permisos que le corresponden."
      onClose={onCancel}
      title="Nuevo rol administrativo"
    >
      <form className="space-y-4" onSubmit={submit}>
        <Field label="Nombre">
          <input
            autoFocus
            className={inputClass}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                nombre: event.target.value,
              }))}
            placeholder="Ej. Encargado de cobros"
            required
            value={form.nombre}
          />
        </Field>

        <Field label="Descripción">
          <textarea
            className={`${inputClass} min-h-24 resize-y`}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                descripcion:
                  event.target.value,
              }))}
            placeholder="Describe brevemente las responsabilidades de este rol."
            value={form.descripcion}
          />
        </Field>

        {error && (
          <Alert
            message={error}
            type="error"
          />
        )}

        <DialogActions
          confirmLabel="Crear rol"
          isSaving={isSaving}
          onCancel={onCancel}
        />
      </form>
    </Dialog>
  )
}

function EmptyRoleState({ onCreate }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-6 py-12 text-center shadow-sm">
      <h3 className="font-semibold text-slate-900">
        No hay un rol seleccionado
      </h3>
      <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-500">
        Selecciona un rol de la lista o crea uno nuevo para configurar sus permisos.
      </p>
      <button
        className="mt-4 rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white"
        onClick={onCreate}
        type="button"
      >
        Crear rol
      </button>
    </div>
  )
}

function Dialog({
  children,
  description,
  onClose,
  title,
}) {
  useEffect(() => {
    function handleKeyDown(event) {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener(
      'keydown',
      handleKeyDown,
    )

    return () =>
      document.removeEventListener(
        'keydown',
        handleKeyDown,
      )
  }, [onClose])

  return (
    <div
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/45 p-4"
      role="dialog"
    >
      <div className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-xl overflow-y-auto rounded-xl bg-white p-5 shadow-2xl sm:p-6">
        <div className="mb-5 flex items-start justify-between gap-4">
          <div>
            <h3 className="text-lg font-semibold text-slate-950">
              {title}
            </h3>

            {description && (
              <p className="mt-1 text-sm leading-6 text-slate-500">
                {description}
              </p>
            )}
          </div>

          <button
            aria-label="Cerrar"
            className="rounded-md px-2 py-1 text-sm font-semibold text-slate-400 hover:bg-slate-100 hover:text-slate-700"
            onClick={onClose}
            type="button"
          >
            Cerrar
          </button>
        </div>

        {children}
      </div>
    </div>
  )
}

function DialogActions({
  confirmLabel,
  isSaving,
  onCancel,
}) {
  return (
    <div className="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end">
      <button
        className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
        disabled={isSaving}
        onClick={onCancel}
        type="button"
      >
        Volver
      </button>

      <button
        className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33] disabled:opacity-60"
        disabled={isSaving}
        type="submit"
      >
        {isSaving
          ? 'Guardando...'
          : confirmLabel}
      </button>
    </div>
  )
}

function Field({ children, label }) {
  return (
    <label className="block text-sm font-medium text-slate-700">
      {label}
      {children}
    </label>
  )
}

function StatusBadge({ active }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${
        active
          ? 'bg-emerald-50 text-emerald-700'
          : 'bg-slate-100 text-slate-600'
      }`}
    >
      <span
        className={`h-1.5 w-1.5 rounded-full ${
          active
            ? 'bg-emerald-500'
            : 'bg-slate-400'
        }`}
      />
      {active ? 'Activo' : 'Inactivo'}
    </span>
  )
}

function LoadingState({ message }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-6 py-12 text-center shadow-sm">
      <div className="mx-auto h-7 w-7 animate-spin rounded-full border-2 border-slate-200 border-t-[#28727a]" />
      <p className="mt-4 text-sm text-slate-500">
        {message}
      </p>
    </div>
  )
}

function Alert({
  message,
  onDismiss,
  type = 'error',
}) {
  const success = type === 'success'

  return (
    <div
      aria-live="polite"
      className={`flex items-start justify-between gap-3 rounded-lg border px-4 py-3 text-sm ${
        success
          ? 'border-emerald-200 bg-emerald-50 text-emerald-900'
          : 'border-red-200 bg-red-50 text-red-800'
      }`}
    >
      <span>{message}</span>

      {onDismiss && (
        <button
          className="shrink-0 font-semibold"
          onClick={onDismiss}
          type="button"
        >
          Cerrar
        </button>
      )}
    </div>
  )
}

function groupPermissions(permisos, search) {
  const normalized =
    search.trim().toLocaleLowerCase('es')

  const groups = new Map()

  permisos.forEach((permission) => {
    const searchable =
      `${permission.nombre} ${permission.descripcion ?? ''} ${permission.codigo}`
        .toLocaleLowerCase('es')

    if (
      normalized &&
      !searchable.includes(normalized)
    ) {
      return
    }

    const prefix =
      permission.codigo
        ?.split('.')[0]
        ?.trim()
        ?.toUpperCase() || 'OTROS'

    if (!groups.has(prefix)) {
      groups.set(prefix, {
        key: prefix,
        label: permissionGroupLabel(prefix),
        permissions: [],
      })
    }

    groups
      .get(prefix)
      .permissions
      .push(permission)
  })

  return Array.from(groups.values())
    .map((group) => ({
      ...group,
      permissions: group.permissions.sort(
        (first, second) =>
          first.nombre.localeCompare(
            second.nombre,
            'es',
            { sensitivity: 'base' },
          ),
      ),
    }))
    .sort((first, second) =>
      first.label.localeCompare(
        second.label,
        'es',
        { sensitivity: 'base' },
      ),
    )
}

function permissionGroupLabel(prefix) {
  const labels = {
    ABASTECIMIENTO: 'Abastecimiento',
    ADMINISTRACION: 'Administración comunitaria',
    CUOTAS: 'Cuotas',
    DASHBOARD: 'Dashboard',
    FINANZAS: 'Finanzas',
    JORNADAS: 'Jornadas',
    OBLIGACIONES: 'Obligaciones',
    PAGOS: 'Pagos',
    PERSONAS: 'Personas',
    REPORTES: 'Reportes',
    SECTORES: 'Sectores',
    SEGURIDAD: 'Seguridad y acceso',
    SUMINISTROS: 'Suministros',
  }

  return (
    labels[prefix] ||
    prefix
      .toLocaleLowerCase('es')
      .replace(/(^|\s)\S/g, (letter) =>
        letter.toLocaleUpperCase('es'),
      )
  )
}

function requestMessage(error, fallback) {
  if (error?.status === 400) {
    return error.message ||
      'Los datos proporcionados no son válidos.'
  }

  if (error?.status === 401) {
    return 'Tu sesión ya no es válida. Inicia sesión nuevamente.'
  }

  if (error?.status === 403) {
    return 'No tienes permiso para realizar esta operación.'
  }

  if (error?.status === 404) {
    return error.message ||
      'El rol solicitado ya no existe.'
  }

  if (error?.status === 409) {
    return error.message ||
      'La operación entra en conflicto con la seguridad administrativa.'
  }

  if (error?.kind === 'network') {
    return 'No se pudo conectar con el servidor.'
  }

  return error?.message || fallback
}

const inputClass =
  'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:cursor-not-allowed disabled:bg-slate-100'

export default RolesPermisosTab
