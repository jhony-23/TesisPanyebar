import { useEffect, useMemo, useState } from 'react'
import {
  getAuditoriaAdministrativa,
  getCargosAdministracion,
  getPermisosAdministrativos,
  getRolesAdministrativos,
  getUsuariosAdministrativos,
} from '../../services/administracionService'
import { getCuotas } from '../../services/cuotaService.js'
import { getJornadas } from '../../services/jornadaService.js'
import { getObligaciones } from '../../services/obligacionService.js'
import { getPersonas } from '../../services/personaService.js'
import { getSectores } from '../../services/sectorService.js'
import { getSuministros } from '../../services/suministroService.js'

const initialFilters = {
  fechaDesde: '',
  fechaHasta: '',
  usuarioId: '',
  accion: '',
  entidad: '',
  limite: 200,
}

function AuditoriaTab({ authenticatedRequest }) {
  const [rows, setRows] = useState([])
  const [usuarios, setUsuarios] = useState([])
  const [auditCatalogs, setAuditCatalogs] = useState(emptyAuditCatalogs)
  const [filters, setFilters] = useState(initialFilters)
  const [appliedFilters, setAppliedFilters] = useState(initialFilters)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState(null)
  const [detail, setDetail] = useState(null)

  useEffect(() => {
    let cancelled = false

    async function loadInitialData() {
      try {
        const [
          auditData,
          userData,
          personasData,
          cargosData,
          rolesData,
          permisosData,
          sectoresData,
          suministrosData,
          cuotasData,
          jornadasData,
          obligacionesData,
        ] = await Promise.all([
          getAuditoriaAdministrativa(
            authenticatedRequest,
            initialFilters,
          ),
          getUsuariosAdministrativos(
            authenticatedRequest,
          ),
          getPersonas(authenticatedRequest),
          getCargosAdministracion(authenticatedRequest),
          getRolesAdministrativos(authenticatedRequest),
          getPermisosAdministrativos(authenticatedRequest),
          getSectores(authenticatedRequest),
          getSuministros(authenticatedRequest),
          getCuotas(authenticatedRequest),
          getJornadas(authenticatedRequest),
          getObligaciones(authenticatedRequest),
        ])

        if (cancelled) return

        setRows(
          Array.isArray(auditData) ? auditData : [],
        )

        setUsuarios(
          Array.isArray(userData) ? userData : [],
        )

        setAuditCatalogs({
          personas: toMap(personasData),
          cargos: toMap(cargosData),
          roles: toMap(rolesData),
          permisos: toMap(permisosData),
          sectores: toMap(sectoresData),
          suministros: toMap(suministrosData),
          cuotas: toMap(cuotasData),
          jornadas: toMap(jornadasData),
          obligaciones: toMap(obligacionesData),
        })
      } catch (requestError) {
        if (cancelled) return

        setError(
          requestMessage(
            requestError,
            'No se pudo cargar la auditoría.',
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

  const orderedUsers = useMemo(
    () =>
      [...usuarios].sort((first, second) =>
        first.nombreUsuario.localeCompare(
          second.nombreUsuario,
          'es',
          { sensitivity: 'base' },
        ),
      ),
    [usuarios],
  )

  const summary = useMemo(() => {
    const uniqueUsers =
      new Set(
        rows.map(
          (row) => row.usuarioAdministrativoId,
        ),
      ).size

    const uniqueEntities =
      new Set(
        rows.map((row) => row.entidad),
      ).size

    return {
      total: rows.length,
      users: uniqueUsers,
      entities: uniqueEntities,
    }
  }, [rows])

  async function applyFilters(event) {
    event.preventDefault()

    if (
      filters.fechaDesde &&
      filters.fechaHasta &&
      filters.fechaDesde > filters.fechaHasta
    ) {
      setError(
        'La fecha inicial no puede ser posterior a la fecha final.',
      )
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const data =
        await getAuditoriaAdministrativa(
          authenticatedRequest,
          filters,
        )

      setRows(Array.isArray(data) ? data : [])
      setAppliedFilters({ ...filters })
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo consultar la auditoría.',
        ),
      )
    } finally {
      setIsLoading(false)
    }
  }

  async function clearFilters() {
    setFilters(initialFilters)
    setIsLoading(true)
    setError(null)

    try {
      const data =
        await getAuditoriaAdministrativa(
          authenticatedRequest,
          initialFilters,
        )

      setRows(Array.isArray(data) ? data : [])
      setAppliedFilters(initialFilters)
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo actualizar la auditoría.',
        ),
      )
    } finally {
      setIsLoading(false)
    }
  }

  const hasFilters =
    appliedFilters.fechaDesde ||
    appliedFilters.fechaHasta ||
    appliedFilters.usuarioId ||
    appliedFilters.accion ||
    appliedFilters.entidad

  return (
    <div className="space-y-5">
      {error && (
        <Alert
          message={error}
          onDismiss={() => setError(null)}
        />
      )}

      <section className="grid gap-3 sm:grid-cols-3">
        <SummaryCard
          label="Registros mostrados"
          value={summary.total}
        />
        <SummaryCard
          label="Usuarios"
          value={summary.users}
        />
        <SummaryCard
          label="Entidades"
          value={summary.entities}
        />
      </section>

      <section className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 p-4 sm:p-5">
          <div>
            <h3 className="font-semibold text-slate-950">
              Consultar actividad
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              Filtra las operaciones registradas por fecha,
              usuario, acción o entidad.
            </p>
          </div>

          <form
            className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-5"
            onSubmit={applyFilters}
          >
            <Field label="Desde">
              <input
                className={inputClass}
                onChange={(event) =>
                  setFilters((current) => ({
                    ...current,
                    fechaDesde:
                      event.target.value
                        ? `${event.target.value}T00:00:00Z`
                        : '',
                  }))}
                type="date"
                value={
                  filters.fechaDesde
                    ? filters.fechaDesde.slice(0, 10)
                    : ''
                }
              />
            </Field>

            <Field label="Hasta">
              <input
                className={inputClass}
                onChange={(event) =>
                  setFilters((current) => ({
                    ...current,
                    fechaHasta:
                      event.target.value
                        ? `${event.target.value}T23:59:59.9999999Z`
                        : '',
                  }))}
                type="date"
                value={
                  filters.fechaHasta
                    ? filters.fechaHasta.slice(0, 10)
                    : ''
                }
              />
            </Field>

            <Field label="Usuario">
              <select
                className={inputClass}
                onChange={(event) =>
                  setFilters((current) => ({
                    ...current,
                    usuarioId: event.target.value,
                  }))}
                value={filters.usuarioId}
              >
                <option value="">Todos</option>

                {orderedUsers.map((usuario) => (
                  <option
                    key={usuario.id}
                    value={usuario.id}
                  >
                    {usuario.nombreUsuario}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Acción">
              <input
                className={inputClass}
                onChange={(event) =>
                  setFilters((current) => ({
                    ...current,
                    accion: event.target.value,
                  }))}
                placeholder="Ej. PAGO"
                value={filters.accion}
              />
            </Field>

            <Field label="Entidad">
              <input
                className={inputClass}
                onChange={(event) =>
                  setFilters((current) => ({
                    ...current,
                    entidad: event.target.value,
                  }))}
                placeholder="Ej. Persona"
                value={filters.entidad}
              />
            </Field>

            <div className="flex flex-col gap-2 sm:col-span-2 sm:flex-row xl:col-span-5 xl:justify-end">
              {hasFilters && (
                <button
                  className="rounded-md px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100"
                  onClick={clearFilters}
                  type="button"
                >
                  Limpiar filtros
                </button>
              )}

              <button
                className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33]"
                disabled={isLoading}
                type="submit"
              >
                {isLoading
                  ? 'Consultando...'
                  : 'Consultar'}
              </button>
            </div>
          </form>
        </div>

        {isLoading ? (
          <LoadingState />
        ) : rows.length === 0 ? (
          <EmptyState />
        ) : (
          <>
            <div className="hidden md:block">
              <AuditTable
                onOpen={setDetail}
                rows={rows}
              />
            </div>

            <div className="divide-y divide-slate-100 md:hidden">
              {rows.map((row) => (
                <AuditCard
                  key={row.id}
                  onOpen={() => setDetail(row)}
                  row={row}
                />
              ))}
            </div>
          </>
        )}

        <div className="border-t border-slate-100 bg-slate-50 px-4 py-3 text-xs leading-5 text-slate-500 sm:px-5">
          Se muestran como máximo 200 registros por consulta.
          Los registros de auditoría son únicamente de lectura.
        </div>
      </section>

      {detail && (
        <AuditDetailDialog
          catalogs={auditCatalogs}
          onClose={() => setDetail(null)}
          row={detail}
        />
      )}
    </div>
  )
}

function AuditTable({ onOpen, rows }) {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-slate-200">
        <thead className="bg-slate-50">
          <tr>
            <Header>Fecha</Header>
            <Header>Usuario</Header>
            <Header>Acción</Header>
            <Header>Registro</Header>
            <Header align="right">
              Detalle
            </Header>
          </tr>
        </thead>

        <tbody className="divide-y divide-slate-100 bg-white">
          {rows.map((row) => (
            <tr
              className="hover:bg-slate-50"
              key={row.id}
            >
              <Cell>
                <div className="whitespace-nowrap">
                  <p className="font-medium text-slate-800">
                    {formatDateTime(row.fecha).date}
                  </p>
                  <p className="mt-0.5 text-xs text-slate-400">
                    {formatDateTime(row.fecha).time}
                  </p>
                </div>
              </Cell>

              <Cell>
                <p className="font-medium text-slate-800">
                  {row.nombreUsuario}
                </p>
              </Cell>

              <Cell>
                <ActionLabel action={row.accion} />
              </Cell>

              <Cell>
                <p className="font-medium text-slate-800">
                  {friendlyEntity(row.entidad)}
                </p>
                <p className="mt-0.5 text-xs text-slate-400">
                  Registro #{row.entidadId}
                </p>
              </Cell>

              <Cell align="right">
                <button
                  className="text-sm font-semibold text-[#176b72] hover:text-[#123b43]"
                  onClick={() => onOpen(row)}
                  type="button"
                >
                  Ver detalle
                </button>
              </Cell>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function AuditCard({ onOpen, row }) {
  const date = formatDateTime(row.fecha)

  return (
    <article className="p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-semibold text-slate-900">
            {friendlyAction(row.accion)}
          </p>

          <p className="mt-1 text-sm text-slate-500">
            {row.nombreUsuario}
          </p>
        </div>

        <span className="shrink-0 text-right text-xs text-slate-400">
          {date.date}
          <br />
          {date.time}
        </span>
      </div>

      <div className="mt-3 rounded-lg bg-slate-50 px-3 py-2">
        <p className="text-sm font-medium text-slate-700">
          {friendlyEntity(row.entidad)}
        </p>
        <p className="mt-0.5 text-xs text-slate-400">
          Registro #{row.entidadId}
        </p>
      </div>

      <button
        className="mt-3 text-sm font-semibold text-[#176b72]"
        onClick={onOpen}
        type="button"
      >
        Ver detalle
      </button>
    </article>
  )
}

function AuditDetailDialog({ catalogs, onClose, row }) {
  const date = formatDateTime(row.fecha)

  return (
    <Dialog
      onClose={onClose}
      title="Detalle de auditoría"
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <DetailField
          label="Fecha y hora"
          value={`${date.date} · ${date.time}`}
        />

        <DetailField
          label="Usuario"
          value={row.nombreUsuario}
        />

        <DetailField
          label="Acción"
          value={friendlyAction(row.accion)}
        />

        <DetailField
          label="Entidad"
          value={`${friendlyEntity(row.entidad)} #${row.entidadId}`}
        />
      </div>

      <div className="mt-5 space-y-4">
        <AuditValue
          label="Valor anterior"
          catalogs={catalogs}
          value={row.valorAnterior}
        />

        <AuditValue
          label="Valor nuevo"
          catalogs={catalogs}
          value={row.valorNuevo}
        />
      </div>

      <div className="mt-5 rounded-lg bg-slate-50 px-3 py-2">
        <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-400">
          Código técnico
        </p>
        <code className="mt-1 block break-all text-xs text-slate-500">
          {row.accion}
        </code>
      </div>

      <div className="mt-5 flex justify-end">
        <button
          className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33]"
          onClick={onClose}
          type="button"
        >
          Cerrar
        </button>
      </div>
    </Dialog>
  )
}

function AuditValue({ catalogs, label, value }) {
  const items = formatAuditValue(value, catalogs)

  return (
    <div>
      <p className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-slate-400">
        {label}
      </p>

      {items ? (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          {items.map((item, index) => (
            <div
              className={`px-3 py-2.5 ${
                index > 0 ? 'border-t border-slate-100' : ''
              }`}
              key={`${item.label}-${index}`}
            >
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-400">
                {item.label}
              </p>
              <p className="mt-1 break-words text-sm font-medium text-slate-800">
                {item.value}
              </p>
            </div>
          ))}
        </div>
      ) : (
        <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-3 text-sm text-slate-400">
          Sin información registrada
        </div>
      )}
    </div>
  )
}

function ActionLabel({ action }) {
  return (
    <div>
      <p className="font-medium text-slate-800">
        {friendlyAction(action)}
      </p>
      <code className="mt-0.5 block text-[10px] text-slate-400">
        {action}
      </code>
    </div>
  )
}

function DetailField({ label, value }) {
  return (
    <div className="rounded-lg bg-slate-50 p-3">
      <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-400">
        {label}
      </p>
      <p className="mt-1 break-words text-sm font-medium text-slate-800">
        {value || '—'}
      </p>
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

function Header({ align, children }) {
  return (
    <th
      className={`px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-500 ${
        align === 'right'
          ? 'text-right'
          : 'text-left'
      }`}
    >
      {children}
    </th>
  )
}

function Cell({ align, children }) {
  return (
    <td
      className={`px-4 py-3 text-sm ${
        align === 'right'
          ? 'text-right'
          : 'text-left'
      }`}
    >
      {children}
    </td>
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

function Dialog({ children, onClose, title }) {
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
      <div className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-2xl overflow-y-auto rounded-xl bg-white p-5 shadow-2xl sm:p-6">
        <div className="mb-5 flex items-start justify-between gap-4">
          <h3 className="text-lg font-semibold text-slate-950">
            {title}
          </h3>

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

function LoadingState() {
  return (
    <div className="px-6 py-14 text-center">
      <div className="mx-auto h-7 w-7 animate-spin rounded-full border-2 border-slate-200 border-t-[#28727a]" />
      <p className="mt-4 text-sm text-slate-500">
        Consultando auditoría...
      </p>
    </div>
  )
}

function EmptyState() {
  return (
    <div className="px-6 py-14 text-center">
      <p className="font-medium text-slate-700">
        No se encontraron registros
      </p>
      <p className="mt-1 text-sm text-slate-500">
        Prueba con otro rango o elimina algunos filtros.
      </p>
    </div>
  )
}

function Alert({ message, onDismiss }) {
  return (
    <div
      aria-live="polite"
      className="flex items-start justify-between gap-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
    >
      <span>{message}</span>

      <button
        className="shrink-0 font-semibold"
        onClick={onDismiss}
        type="button"
      >
        Cerrar
      </button>
    </div>
  )
}

function formatDateTime(value) {
  if (!value) {
    return {
      date: '—',
      time: '—',
    }
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return {
      date: value,
      time: '—',
    }
  }

  return {
    date: new Intl.DateTimeFormat(
      'es-GT',
      {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
      },
    ).format(date),

    time: new Intl.DateTimeFormat(
      'es-GT',
      {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      },
    ).format(date),
  }
}

function formatAuditValue(value, catalogs) {
  if (!value) return null

  try {
    const parsed = JSON.parse(value)

    if (
      parsed &&
      typeof parsed === 'object' &&
      !Array.isArray(parsed)
    ) {
      return Object.entries(parsed).map(([key, item]) => ({
        label: friendlyAuditField(key),
        value: friendlyAuditValue(key, item, catalogs),
      }))
    }

    return [
      {
        label: 'Valor',
        value: friendlyAuditValue('', parsed, catalogs),
      },
    ]
  } catch {
    return parseAuditText(value, catalogs)
  }
}

function parseAuditText(value, catalogs) {
  const parts = String(value)
    .split(';')
    .map((part) => part.trim())
    .filter(Boolean)

  if (
    parts.length > 0 &&
    parts.every((part) => part.includes(':'))
  ) {
    return parts.map((part) => {
      const separator = part.indexOf(':')
      const key = part.slice(0, separator).trim()
      const item = part.slice(separator + 1).trim()

      return {
        label: friendlyAuditField(key),
        value: friendlyAuditValue(key, item, catalogs),
      }
    })
  }

  return [
    {
      label: 'Detalle',
      value: String(value),
    },
  ]
}

function friendlyAuditField(key) {
  const labels = {
    AdministracionComiteId: 'Administración del comité',
    Asignaciones: 'Integrantes asignados',
    CargoId: 'Cargo',
    Estado: 'Estado',
    FechaFin: 'Fecha de finalización',
    FechaInicio: 'Fecha de inicio',
    Monto: 'Monto',
    MontoIncumplimiento: 'Monto por ausencia',
    Nombre: 'Nombre',
    Concepto: 'Concepto',
    Resultado: 'Resultado',
    Observacion: 'Observación',
    Periodo: 'Período',
    Periodicidad: 'Periodicidad',
    Ubicacion: 'Ubicación',
    Horario: 'Horario',
    ObligacionIds: 'Obligaciones relacionadas',
    PersonaId: 'Persona',
    PersonaIds: 'Personas',
    RolIds: 'Roles asignados',
    PermisoIds: 'Permisos asignados',
    CuotaId: 'Cuota',
    JornadaId: 'Jornada',
    SectorId: 'Sector',
    SuministroId: 'Suministro',
  }

  if (labels[key]) return labels[key]

  return String(key)
    .replace(/Id$/u, '')
    .replace(/([a-záéíóúñ])([A-Z])/gu, '$1 $2')
    .replace(/^./u, (letter) =>
      letter.toLocaleUpperCase('es'),
    )
}

function friendlyAuditValue(key, value, catalogs) {
  if (Array.isArray(value) && value.length === 0) {
    if (key === 'RolIds') {
      return 'Sin roles asignados'
    }

    if (key === 'PermisoIds') {
      return 'Sin permisos asignados'
    }

    if (key === 'PersonaIds') {
      return 'Sin personas registradas'
    }

    if (key === 'ObligacionIds') {
      return 'Sin obligaciones relacionadas'
    }

    if (key === 'Asignaciones') {
      return 'No había integrantes asignados'
    }

    return 'Sin información registrada'
  }

  const resolved = resolveAuditReference(key, value, catalogs)

  if (resolved !== null) {
    return resolved
  }

  if (Array.isArray(value)) {

    return value
      .map((item) => {
        if (
          item &&
          typeof item === 'object' &&
          !Array.isArray(item)
        ) {
          return Object.entries(item)
            .filter(([nestedKey]) => nestedKey !== 'Id')
            .map(
              ([nestedKey, nestedValue]) =>
                `${friendlyAuditField(nestedKey)}: ${friendlyAuditValue(
                  nestedKey,
                  nestedValue,
                  catalogs,
                )}`,
            )
            .join(' · ')
        }

        return (
          resolveAuditReference(
            key,
            item,
            catalogs,
            true,
          ) ||
          String(item)
        )
      })
      .join(' | ')
  }

  if (value === null || value === undefined || value === '') {
    return 'Sin información'
  }

  if (key === 'Monto' || key === 'MontoIncumplimiento') {
    const amount = Number(value)

    if (Number.isFinite(amount)) {
      return new Intl.NumberFormat(
        'es-GT',
        {
          style: 'currency',
          currency: 'GTQ',
        },
      ).format(amount)
    }
  }

  if (
    key.endsWith('Id') &&
    !key.endsWith('Ids')
  ) {
    return 'Registro relacionado no disponible'
  }

  if (typeof value === 'boolean') {
    return value ? 'Sí' : 'No'
  }

  if (typeof value === 'object') {
    return Object.entries(value)
      .map(
        ([nestedKey, nestedValue]) =>
          `${friendlyAuditField(nestedKey)}: ${friendlyAuditValue(
            nestedKey,
            nestedValue,
            catalogs,
          )}`,
      )
      .join(' · ')
  }

  return String(value)
}

const emptyAuditCatalogs = {
  personas: new Map(),
  cargos: new Map(),
  roles: new Map(),
  permisos: new Map(),
  sectores: new Map(),
  suministros: new Map(),
  cuotas: new Map(),
  jornadas: new Map(),
  obligaciones: new Map(),
}

function toMap(items) {
  return new Map(
    (Array.isArray(items) ? items : []).map((item) => [
      Number(item.id),
      item,
    ]),
  )
}

function resolveAuditReference(
  key,
  value,
  catalogs,
  arrayItem = false,
) {
  if (!catalogs) return null

  const numericValue = Number(value)

  if (!Number.isInteger(numericValue)) {
    return null
  }

  if (key === 'PersonaId' || key === 'PersonaIds') {
    const persona = catalogs.personas.get(numericValue)

    return persona
      ? `${persona.nombres} ${persona.apellidos}`.trim()
      : 'Persona que ya no se encuentra disponible'
  }

  if (key === 'CargoId') {
    const cargo = catalogs.cargos.get(numericValue)

    return cargo?.nombre ||
      'Cargo que ya no se encuentra disponible'
  }

  if (key === 'RolIds') {
    const rol = catalogs.roles.get(numericValue)

    return rol?.nombre ||
      'Rol que ya no se encuentra disponible'
  }

  if (key === 'PermisoIds') {
    const permiso = catalogs.permisos.get(numericValue)

    if (!permiso) {
      return 'Permiso que ya no se encuentra disponible'
    }

    return permiso.nombre
      ? `${permiso.nombre} (${permiso.codigo})`
      : permiso.codigo
  }

  if (key === 'SectorId') {
    const sector = catalogs.sectores.get(numericValue)

    return sector?.nombre ||
      'Sector que ya no se encuentra disponible'
  }

  if (key === 'SuministroId') {
    const suministro =
      catalogs.suministros.get(numericValue)

    return suministro?.nis
      ? `${suministro.nis} · ${suministro.sectorNombre || 'Suministro'}`
      : 'Suministro que ya no se encuentra disponible'
  }

  if (key === 'CuotaId') {
    const cuota = catalogs.cuotas.get(numericValue)

    return cuota?.nombre ||
      'Cuota que ya no se encuentra disponible'
  }

  if (key === 'JornadaId') {
    const jornada = catalogs.jornadas.get(numericValue)

    return jornada?.nombre ||
      'Jornada que ya no se encuentra disponible'
  }

  if (key === 'ObligacionIds') {
    const obligacion =
      catalogs.obligaciones.get(numericValue)

    if (obligacion) {
      const concepto =
        obligacion.concepto ||
        obligacion.descripcion ||
        `Obligación #${numericValue}`

      return concepto
    }

    return 'Obligación que ya no se encuentra disponible'
  }

  if (arrayItem) {
    return null
  }

  return null
}
function friendlyAction(action) {
  if (!action) return 'Operación'

  const parts = action
    .split('.')
    .filter(Boolean)

  const operation =
    parts[parts.length - 1] || action

  const labels = {
    ACTIVAR: 'Activación',
    ANULAR: 'Anulación',
    ASIGNAR: 'Asignación',
    CAMBIAR: 'Cambio',
    COMPLETAR: 'Completado',
    CREAR: 'Creación',
    DESACTIVAR: 'Desactivación',
    FINALIZAR: 'Finalización',
    GESTIONAR: 'Gestión',
    INICIAR: 'Inicio',
    REGISTRAR: 'Registro',
    RESTABLECER: 'Restablecimiento',
    TRANSICION: 'Cambio de estado',
    ACTUALIZAR: 'Actualización',
  }

  return (
    labels[operation] ||
    operation
      .toLocaleLowerCase('es')
      .replace(
        /(^|\s)\S/g,
        (letter) =>
          letter.toLocaleUpperCase('es'),
      )
  )
}

function friendlyEntity(entity) {
  if (!entity) return 'Registro'

  const labels = {
    AdministracionComite: 'Administración del comité',
    Obligacion: 'Obligación',
    Pago: 'Pago',
    Persona: 'Persona',
    ProgramacionAbastecimiento:
      'Programación de abastecimiento',
    Suministro: 'Suministro',
    UsuarioAdministrativo:
      'Usuario administrativo',
  }

  return labels[entity] || entity
}

function requestMessage(error, fallback) {
  if (error?.status === 400) {
    return error.message ||
      'Los filtros proporcionados no son válidos.'
  }

  if (error?.status === 401) {
    return 'Tu sesión ya no es válida. Inicia sesión nuevamente.'
  }

  if (error?.status === 403) {
    return 'No tienes permiso para consultar la auditoría.'
  }

  if (error?.kind === 'network') {
    return 'No se pudo conectar con el servidor.'
  }

  return error?.message || fallback
}

const inputClass =
  'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20'

export default AuditoriaTab
