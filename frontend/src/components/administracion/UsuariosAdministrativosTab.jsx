import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  createUsuarioAdministrativo,
  getRolesAdministrativos,
  getUsuariosAdministrativos,
  resetUsuarioAdministrativoPassword,
  setUsuarioAdministrativoEstado,
  setUsuarioAdministrativoRoles,
} from '../../services/administracionService'

const ACTIVE = 1
const INACTIVE = 2

function UsuariosAdministrativosTab({ authenticatedRequest }) {
  const [usuarios, setUsuarios] = useState([])
  const [roles, setRoles] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [dialog, setDialog] = useState(null)

  const load = useCallback(async () => {
    try {
      const [userData, roleData] = await Promise.all([
        getUsuariosAdministrativos(authenticatedRequest),
        getRolesAdministrativos(authenticatedRequest),
      ])

      setUsuarios(Array.isArray(userData) ? userData : [])
      setRoles(Array.isArray(roleData) ? roleData : [])
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudieron cargar los usuarios administrativos.',
        ),
      )
    }
  }, [authenticatedRequest])

  useEffect(() => {
    let cancelled = false

    async function loadInitialData() {
      try {
        const [userData, roleData] = await Promise.all([
          getUsuariosAdministrativos(authenticatedRequest),
          getRolesAdministrativos(authenticatedRequest),
        ])

        if (cancelled) return

        setUsuarios(Array.isArray(userData) ? userData : [])
        setRoles(Array.isArray(roleData) ? roleData : [])
      } catch (requestError) {
        if (cancelled) return

        setError(
          requestMessage(
            requestError,
            'No se pudieron cargar los usuarios administrativos.',
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

  const filteredUsers = useMemo(() => {
    const normalized = search.trim().toLocaleLowerCase('es')

    return usuarios
      .filter((usuario) => {
        if (
          statusFilter === 'active' &&
          usuario.estado !== ACTIVE
        ) {
          return false
        }

        if (
          statusFilter === 'inactive' &&
          usuario.estado !== INACTIVE
        ) {
          return false
        }

        if (!normalized) return true

        const roleNames = usuario.roles
          .map((role) => role.nombre)
          .join(' ')

        return `${usuario.nombreUsuario} ${roleNames}`
          .toLocaleLowerCase('es')
          .includes(normalized)
      })
      .sort((first, second) =>
        first.nombreUsuario.localeCompare(
          second.nombreUsuario,
          'es',
          { sensitivity: 'base' },
        ),
      )
  }, [search, statusFilter, usuarios])

  const summary = useMemo(
    () => ({
      total: usuarios.length,
      active: usuarios.filter(
        (usuario) => usuario.estado === ACTIVE,
      ).length,
      inactive: usuarios.filter(
        (usuario) => usuario.estado === INACTIVE,
      ).length,
    }),
    [usuarios],
  )

  async function execute(operation, successMessage) {
    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      await operation()
      setDialog(null)
      setNotice(successMessage)
      await load()
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo completar la operación.',
        ),
      )
    } finally {
      setIsSaving(false)
    }
  }

  function createUser(payload) {
    return execute(
      () =>
        createUsuarioAdministrativo(
          authenticatedRequest,
          payload,
        ),
      'El usuario administrativo fue creado.',
    )
  }

  function updateRoles(usuario, roleIds) {
    return execute(
      () =>
        setUsuarioAdministrativoRoles(
          authenticatedRequest,
          usuario.id,
          roleIds,
        ),
      `Los roles de ${usuario.nombreUsuario} fueron actualizados.`,
    )
  }

  function updateStatus(usuario) {
    const nextState =
      usuario.estado === ACTIVE ? INACTIVE : ACTIVE

    return execute(
      () =>
        setUsuarioAdministrativoEstado(
          authenticatedRequest,
          usuario.id,
          nextState,
        ),
      nextState === ACTIVE
        ? `La cuenta ${usuario.nombreUsuario} fue activada.`
        : `La cuenta ${usuario.nombreUsuario} fue desactivada.`,
    )
  }

  function resetPassword(usuario, nuevaPassword) {
    return execute(
      () =>
        resetUsuarioAdministrativoPassword(
          authenticatedRequest,
          usuario.id,
          nuevaPassword,
        ),
      `La contraseña de ${usuario.nombreUsuario} fue restablecida.`,
    )
  }

  if (isLoading) {
    return (
      <LoadingState message="Cargando usuarios administrativos..." />
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

      <section className="grid gap-3 sm:grid-cols-3">
        <SummaryCard
          label="Usuarios"
          value={summary.total}
        />
        <SummaryCard
          label="Activos"
          value={summary.active}
        />
        <SummaryCard
          label="Inactivos"
          value={summary.inactive}
        />
      </section>

      <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 p-4 sm:p-5">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h3 className="font-semibold text-slate-950">
                Cuentas administrativas
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Controla quién puede iniciar sesión y qué roles tiene asignados.
              </p>
            </div>

            <button
              className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2"
              onClick={() => setDialog({ type: 'create' })}
              type="button"
            >
              Nuevo usuario
            </button>
          </div>

          <div className="mt-5 grid gap-3 md:grid-cols-[minmax(0,1fr)_12rem]">
            <label className="block text-sm font-medium text-slate-700">
              Buscar
              <input
                className={inputClass}
                onChange={(event) =>
                  setSearch(event.target.value)}
                placeholder="Usuario o rol"
                type="search"
                value={search}
              />
            </label>

            <label className="block text-sm font-medium text-slate-700">
              Estado
              <select
                className={inputClass}
                onChange={(event) =>
                  setStatusFilter(event.target.value)}
                value={statusFilter}
              >
                <option value="all">Todos</option>
                <option value="active">Activos</option>
                <option value="inactive">Inactivos</option>
              </select>
            </label>
          </div>
        </div>

        {filteredUsers.length === 0 ? (
          <div className="px-5 py-12 text-center">
            <p className="font-medium text-slate-700">
              No se encontraron usuarios
            </p>
            <p className="mt-1 text-sm text-slate-500">
              Ajusta la búsqueda o los filtros utilizados.
            </p>
          </div>
        ) : (
          <div className="divide-y divide-slate-100">
            {filteredUsers.map((usuario) => (
              <UserRow
                key={usuario.id}
                onManage={() =>
                  setDialog({
                    type: 'manage',
                    usuario,
                  })}
                onPassword={() =>
                  setDialog({
                    type: 'password',
                    usuario,
                  })}
                usuario={usuario}
              />
            ))}
          </div>
        )}
      </section>

      {dialog?.type === 'create' && (
        <CreateUserDialog
          isSaving={isSaving}
          onCancel={() => setDialog(null)}
          onConfirm={createUser}
        />
      )}

      {dialog?.type === 'manage' && (
        <ManageUserDialog
          isSaving={isSaving}
          onCancel={() => setDialog(null)}
          onChangeStatus={() =>
            updateStatus(dialog.usuario)}
          onSaveRoles={(roleIds) =>
            updateRoles(dialog.usuario, roleIds)}
          roles={roles}
          usuario={dialog.usuario}
        />
      )}

      {dialog?.type === 'password' && (
        <PasswordDialog
          isSaving={isSaving}
          onCancel={() => setDialog(null)}
          onConfirm={(password) =>
            resetPassword(dialog.usuario, password)}
          usuario={dialog.usuario}
        />
      )}
    </div>
  )
}

function SummaryCard({ label, value }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-5 py-4 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
        {label}
      </p>
      <p className="mt-2 text-2xl font-bold text-slate-950">
        {value}
      </p>
    </div>
  )
}

function UserRow({ onManage, onPassword, usuario }) {
  const active = usuario.estado === ACTIVE

  return (
    <article className="p-4 sm:p-5">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-[#edf7f6] text-[#28727a]">
              <UserIcon />
            </div>

            <div>
              <h4 className="font-semibold text-slate-950">
                {usuario.nombreUsuario}
              </h4>
              <p className="text-xs text-slate-400">
                Usuario administrativo
              </p>
            </div>

            <StatusBadge active={active} />
          </div>

          <div className="mt-3 flex flex-wrap gap-1.5">
            {usuario.roles.length === 0 ? (
              <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-medium text-amber-800">
                Sin roles asignados
              </span>
            ) : (
              usuario.roles.map((role) => (
                <span
                  className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600"
                  key={role.id}
                >
                  {role.nombre}
                </span>
              ))
            )}
          </div>
        </div>

        <div className="flex flex-col gap-2 sm:flex-row">
          <button
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            onClick={onManage}
            type="button"
          >
            Administrar
          </button>

          <button
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            onClick={onPassword}
            type="button"
          >
            Restablecer contraseña
          </button>
        </div>
      </div>
    </article>
  )
}

function CreateUserDialog({
  isSaving,
  onCancel,
  onConfirm,
}) {
  const [form, setForm] = useState({
    nombreUsuario: '',
    password: '',
    confirmation: '',
  })
  const [error, setError] = useState(null)

  function submit(event) {
    event.preventDefault()

    const nombreUsuario = form.nombreUsuario.trim()

    if (!nombreUsuario) {
      setError('Ingresa un nombre de usuario.')
      return
    }

    if (form.password.length < 8) {
      setError('La contraseña debe tener al menos 8 caracteres.')
      return
    }

    if (form.password !== form.confirmation) {
      setError('Las contraseñas no coinciden.')
      return
    }

    setError(null)

    onConfirm({
      nombreUsuario,
      password: form.password,
    })
  }

  return (
    <Dialog
      description="La cuenta se creará activa. Después podrás asignarle uno o más roles."
      onClose={onCancel}
      title="Nuevo usuario administrativo"
    >
      <form className="space-y-4" onSubmit={submit}>
        <Field label="Nombre de usuario">
          <input
            autoComplete="off"
            autoFocus
            className={inputClass}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                nombreUsuario: event.target.value,
              }))}
            placeholder="Ej. operador.admin"
            required
            value={form.nombreUsuario}
          />
        </Field>

        <Field label="Contraseña">
          <input
            autoComplete="new-password"
            className={inputClass}
            minLength="8"
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                password: event.target.value,
              }))}
            required
            type="password"
            value={form.password}
          />
        </Field>

        <Field label="Confirmar contraseña">
          <input
            autoComplete="new-password"
            className={inputClass}
            minLength="8"
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                confirmation: event.target.value,
              }))}
            required
            type="password"
            value={form.confirmation}
          />
        </Field>

        {error && <Alert message={error} type="error" />}

        <DialogActions
          confirmLabel="Crear usuario"
          isSaving={isSaving}
          onCancel={onCancel}
        />
      </form>
    </Dialog>
  )
}

function ManageUserDialog({
  isSaving,
  onCancel,
  onChangeStatus,
  onSaveRoles,
  roles,
  usuario,
}) {
  const [selectedRoles, setSelectedRoles] = useState(
    () => new Set(usuario.roles.map((role) => role.id)),
  )

  const activeRoles = useMemo(
    () =>
      roles
        .filter(
          (role) =>
            role.estado === ACTIVE ||
            selectedRoles.has(role.id),
        )
        .sort((first, second) =>
          first.nombre.localeCompare(
            second.nombre,
            'es',
            { sensitivity: 'base' },
          ),
        ),
    [roles, selectedRoles],
  )

  function toggleRole(id) {
    setSelectedRoles((current) => {
      const next = new Set(current)

      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }

      return next
    })
  }

  function submit(event) {
    event.preventDefault()
    onSaveRoles(Array.from(selectedRoles))
  }

  const active = usuario.estado === ACTIVE

  return (
    <Dialog
      description="Administra el estado de la cuenta y los roles que determinan sus accesos."
      onClose={onCancel}
      title={usuario.nombreUsuario}
    >
      <form className="space-y-5" onSubmit={submit}>
        <div className="flex items-center justify-between gap-4 rounded-lg bg-slate-50 p-4">
          <div>
            <p className="font-semibold text-slate-900">
              Estado de la cuenta
            </p>
            <p className="mt-1 text-sm text-slate-500">
              {active
                ? 'El usuario puede iniciar sesión.'
                : 'El usuario se encuentra inactivo.'}
            </p>
          </div>

          <StatusBadge active={active} />
        </div>

        <div>
          <div className="flex items-end justify-between gap-3">
            <div>
              <h4 className="text-sm font-semibold text-slate-900">
                Roles
              </h4>
              <p className="mt-1 text-xs text-slate-500">
                Selecciona todos los roles que correspondan.
              </p>
            </div>

            <span className="text-xs font-semibold text-slate-400">
              {selectedRoles.size} seleccionados
            </span>
          </div>

          {activeRoles.length === 0 ? (
            <p className="mt-3 rounded-lg bg-amber-50 px-3 py-3 text-sm text-amber-900">
              No existen roles disponibles.
            </p>
          ) : (
            <div className="mt-3 max-h-64 space-y-2 overflow-y-auto pr-1">
              {activeRoles.map((role) => (
                <label
                  className="flex cursor-pointer items-start gap-3 rounded-lg border border-slate-200 p-3 hover:bg-slate-50"
                  key={role.id}
                >
                  <input
                    checked={selectedRoles.has(role.id)}
                    className="mt-0.5 h-4 w-4 accent-[#28727a]"
                    onChange={() => toggleRole(role.id)}
                    type="checkbox"
                  />

                  <span className="min-w-0">
                    <span className="block text-sm font-semibold text-slate-800">
                      {role.nombre}
                    </span>

                    {role.descripcion && (
                      <span className="mt-0.5 block text-xs leading-5 text-slate-500">
                        {role.descripcion}
                      </span>
                    )}

                    {role.estado === INACTIVE && (
                      <span className="mt-1 block text-xs font-semibold text-amber-700">
                        Rol inactivo actualmente
                      </span>
                    )}
                  </span>
                </label>
              ))}
            </div>
          )}

          <button
            className="mt-4 w-full rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#0d2d33] disabled:opacity-60"
            disabled={isSaving}
            type="submit"
          >
            {isSaving ? 'Guardando...' : 'Guardar roles'}
          </button>
        </div>

        <div className="border-t border-slate-100 pt-5">
          <h4 className="text-sm font-semibold text-slate-900">
            Acceso a la cuenta
          </h4>
          <p className="mt-1 text-xs leading-5 text-slate-500">
            El backend protege automáticamente las cuentas y roles necesarios para conservar un administrador funcional.
          </p>

          <button
            className={`mt-3 rounded-md border px-3 py-2 text-sm font-semibold ${
              active
                ? 'border-red-200 text-red-700 hover:bg-red-50'
                : 'border-emerald-200 text-emerald-700 hover:bg-emerald-50'
            }`}
            disabled={isSaving}
            onClick={onChangeStatus}
            type="button"
          >
            {active ? 'Desactivar usuario' : 'Activar usuario'}
          </button>
        </div>

        <div className="flex justify-end border-t border-slate-100 pt-4">
          <button
            className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
            disabled={isSaving}
            onClick={onCancel}
            type="button"
          >
            Cerrar
          </button>
        </div>
      </form>
    </Dialog>
  )
}

function PasswordDialog({
  isSaving,
  onCancel,
  onConfirm,
  usuario,
}) {
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [error, setError] = useState(null)

  function submit(event) {
    event.preventDefault()

    if (password.length < 8) {
      setError('La nueva contraseña debe tener al menos 8 caracteres.')
      return
    }

    if (password !== confirmation) {
      setError('Las contraseñas no coinciden.')
      return
    }

    setError(null)
    onConfirm(password)
  }

  return (
    <Dialog
      description="La contraseña anterior dejará de ser válida inmediatamente."
      onClose={onCancel}
      title={`Restablecer contraseña · ${usuario.nombreUsuario}`}
    >
      <form className="space-y-4" onSubmit={submit}>
        <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm leading-6 text-amber-900">
          Utiliza una contraseña temporal segura y entrégala únicamente a la persona autorizada.
        </div>

        <Field label="Nueva contraseña">
          <input
            autoComplete="new-password"
            autoFocus
            className={inputClass}
            minLength="8"
            onChange={(event) =>
              setPassword(event.target.value)}
            required
            type="password"
            value={password}
          />
        </Field>

        <Field label="Confirmar nueva contraseña">
          <input
            autoComplete="new-password"
            className={inputClass}
            minLength="8"
            onChange={(event) =>
              setConfirmation(event.target.value)}
            required
            type="password"
            value={confirmation}
          />
        </Field>

        {error && <Alert message={error} type="error" />}

        <DialogActions
          confirmLabel="Restablecer contraseña"
          isSaving={isSaving}
          onCancel={onCancel}
        />
      </form>
    </Dialog>
  )
}

function Dialog({ children, description, onClose, title }) {
  useEffect(() => {
    function handleKeyDown(event) {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener('keydown', handleKeyDown)

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
        <div className="mb-5">
          <div className="flex items-start justify-between gap-4">
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
        className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33] disabled:cursor-not-allowed disabled:opacity-60"
        disabled={isSaving}
        type="submit"
      >
        {isSaving ? 'Guardando...' : confirmLabel}
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
          active ? 'bg-emerald-500' : 'bg-slate-400'
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

function Alert({ message, onDismiss, type = 'error' }) {
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

function UserIcon() {
  return (
    <svg
      aria-hidden="true"
      className="h-5 w-5"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      viewBox="0 0 24 24"
    >
      <circle cx="12" cy="8" r="3" />
      <path d="M6 19c.7-3.5 2.7-5.5 6-5.5s5.3 2 6 5.5" />
    </svg>
  )
}

function requestMessage(error, fallback) {
  if (error?.status === 400) {
    return error.message || 'Los datos proporcionados no son válidos.'
  }

  if (error?.status === 401) {
    return 'Tu sesión ya no es válida. Inicia sesión nuevamente.'
  }

  if (error?.status === 403) {
    return 'No tienes permiso para realizar esta operación.'
  }

  if (error?.status === 404) {
    return error.message || 'El usuario solicitado ya no existe.'
  }

  if (error?.status === 409) {
    return error.message || 'La operación entra en conflicto con la seguridad administrativa.'
  }

  if (error?.kind === 'network') {
    return 'No se pudo conectar con el servidor.'
  }

  return error?.message || fallback
}

const inputClass =
  'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:cursor-not-allowed disabled:bg-slate-100'

export default UsuariosAdministrativosTab
