import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import {
  createAdministracion,
  finishAdministracion,
  getAdministraciones,
  getCargosAdministracion,
  setIntegranteAdministracion,
} from '../services/administracionService'
import { getPersonas } from '../services/personaService'
import UsuariosAdministrativosTab from '../components/administracion/UsuariosAdministrativosTab'
import RolesPermisosTab from '../components/administracion/RolesPermisosTab'
import AuditoriaTab from '../components/administracion/AuditoriaTab'

const tabs = [
  {
    id: 'comite',
    label: 'Comité',
    description: 'Períodos e integrantes de la administración comunitaria.',
  },
  {
    id: 'usuarios',
    label: 'Usuarios',
    description: 'Cuentas con acceso administrativo al sistema.',
  },
  {
    id: 'roles',
    label: 'Roles y permisos',
    description: 'Accesos y responsabilidades dentro del sistema.',
  },
  {
    id: 'auditoria',
    label: 'Auditoría',
    description: 'Trazabilidad de operaciones administrativas.',
  },
]

function AdministracionPage() {
  const { authenticatedRequest } = useOutletContext()
  const [activeTab, setActiveTab] = useState('comite')

  const selectedTab = useMemo(
    () => tabs.find((tab) => tab.id === activeTab) ?? tabs[0],
    [activeTab],
  )

  return (
    <div className="space-y-6">
      <header className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-5 py-5 sm:px-6">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[#28727a]">
            Configuración institucional
          </p>

          <div className="mt-2 flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-950 sm:text-3xl">
                Administración
              </h1>
              <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">
                Gestiona el comité comunitario, las cuentas administrativas,
                los roles de acceso y la trazabilidad del sistema.
              </p>
            </div>

            <div className="hidden rounded-lg bg-[#edf7f6] px-4 py-3 text-right lg:block">
              <p className="text-xs font-semibold uppercase tracking-wide text-[#28727a]">
                Módulo administrativo
              </p>
              <p className="mt-1 text-sm font-medium text-[#123b43]">
                Control institucional y seguridad
              </p>
            </div>
          </div>
        </div>

        <nav
          aria-label="Secciones de administración"
          className="overflow-x-auto px-3 sm:px-4"
        >
          <div className="flex min-w-max gap-1">
            {tabs.map((tab) => {
              const active = tab.id === activeTab

              return (
                <button
                  aria-current={active ? 'page' : undefined}
                  className={`relative min-h-14 px-4 text-sm font-semibold transition ${
                    active
                      ? 'text-[#123b43]'
                      : 'text-slate-500 hover:text-slate-800'
                  }`}
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  type="button"
                >
                  {tab.label}

                  {active && (
                    <span
                      aria-hidden="true"
                      className="absolute inset-x-3 bottom-0 h-0.5 rounded-full bg-[#28727a]"
                    />
                  )}
                </button>
              )
            })}
          </div>
        </nav>
      </header>

      <section aria-labelledby={`admin-tab-${selectedTab.id}`}>
        <div className="mb-4">
          <h2
            className="text-lg font-semibold text-slate-900"
            id={`admin-tab-${selectedTab.id}`}
          >
            {selectedTab.label}
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            {selectedTab.description}
          </p>
        </div>

        {activeTab === 'comite' && (
          <CommitteeTab authenticatedRequest={authenticatedRequest} />
        )}

        {activeTab === 'usuarios' && (
          <UsuariosAdministrativosTab
            authenticatedRequest={authenticatedRequest}
          />
        )}

        {activeTab === 'roles' && (
          <RolesPermisosTab
            authenticatedRequest={authenticatedRequest}
          />
        )}

        {activeTab === 'auditoria' && (
          <AuditoriaTab
            authenticatedRequest={authenticatedRequest}
          />
        )}
      </section>
    </div>
  )
}

function CommitteeTab({ authenticatedRequest }) {
  const [administraciones, setAdministraciones] = useState([])
  const [cargos, setCargos] = useState([])
  const [personas, setPersonas] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)
  const [dialog, setDialog] = useState(null)
  const [historyDetail, setHistoryDetail] = useState(null)

  const load = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      const [administrationData, cargoData, personaData] = await Promise.all([
        getAdministraciones(authenticatedRequest),
        getCargosAdministracion(authenticatedRequest),
        getPersonas(authenticatedRequest),
      ])

      setAdministraciones(Array.isArray(administrationData) ? administrationData : [])
      setCargos(Array.isArray(cargoData) ? cargoData : [])
      setPersonas(Array.isArray(personaData) ? personaData : [])
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo cargar la administración del comité.',
        ),
      )
    } finally {
      setIsLoading(false)
    }
  }, [authenticatedRequest])

  useEffect(() => {
    let cancelled = false

    async function loadInitialData() {
      try {
        const [administrationData, cargoData, personaData] = await Promise.all([
          getAdministraciones(authenticatedRequest),
          getCargosAdministracion(authenticatedRequest),
          getPersonas(authenticatedRequest),
        ])

        if (cancelled) return

        setAdministraciones(
          Array.isArray(administrationData) ? administrationData : [],
        )
        setCargos(Array.isArray(cargoData) ? cargoData : [])
        setPersonas(Array.isArray(personaData) ? personaData : [])
      } catch (requestError) {
        if (cancelled) return

        setError(
          requestMessage(
            requestError,
            'No se pudo cargar la administración del comité.',
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

  const current = useMemo(
    () => administraciones.find((item) => item.activa) ?? null,
    [administraciones],
  )

  const history = useMemo(
    () => administraciones.filter((item) => !item.activa),
    [administraciones],
  )

  const activeCargos = useMemo(
    () => cargos.filter((cargo) => cargo.activo),
    [cargos],
  )

  const activePersonas = useMemo(
    () =>
      personas
        .filter((persona) => persona.estado === 1)
        .sort(comparePersonas),
    [personas],
  )

  async function createPeriod(payload) {
    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      await createAdministracion(authenticatedRequest, payload)
      setDialog(null)
      setNotice('La nueva administración del comité fue creada.')
      await load()
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo crear la administración del comité.',
        ),
      )
    } finally {
      setIsSaving(false)
    }
  }

  async function assignMember(payload) {
    if (!current) return

    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      await setIntegranteAdministracion(
        authenticatedRequest,
        current.id,
        payload,
      )

      setDialog(null)
      setNotice('La integración del comité fue actualizada.')
      await load()
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo actualizar el integrante del comité.',
        ),
      )
    } finally {
      setIsSaving(false)
    }
  }

  async function finishPeriod(fechaFin) {
    if (!current) return

    setIsSaving(true)
    setError(null)
    setNotice(null)

    try {
      await finishAdministracion(
        authenticatedRequest,
        current.id,
        fechaFin,
      )

      setDialog(null)
      setNotice('La administración fue finalizada y quedó en el historial.')
      await load()
    } catch (requestError) {
      setError(
        requestMessage(
          requestError,
          'No se pudo finalizar la administración.',
        ),
      )
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return <LoadingState message="Cargando administración del comité..." />
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

      {!current ? (
        <EmptyCommittee
          hasHistory={history.length > 0}
          onCreate={() => setDialog({ type: 'create' })}
        />
      ) : (
        <CurrentAdministration
          administration={current}
          cargos={activeCargos}
          onAssign={(cargo) =>
            setDialog({
              type: 'member',
              cargo,
            })}
          onFinish={() => setDialog({ type: 'finish' })}
        />
      )}

      <HistorySection
        administrations={history}
        onOpen={setHistoryDetail}
      />

      {dialog?.type === 'create' && (
        <CreatePeriodDialog
          isSaving={isSaving}
          onCancel={() => setDialog(null)}
          onConfirm={createPeriod}
        />
      )}

      {dialog?.type === 'member' && current && (
        <MemberDialog
          administration={current}
          cargo={dialog.cargo}
          isSaving={isSaving}
          onCancel={() => setDialog(null)}
          onConfirm={assignMember}
          personas={activePersonas}
        />
      )}

      {dialog?.type === 'finish' && current && (
        <FinishPeriodDialog
          administration={current}
          isSaving={isSaving}
          onCancel={() => setDialog(null)}
          onConfirm={finishPeriod}
        />
      )}

      {historyDetail && (
        <HistoryDialog
          administration={historyDetail}
          onClose={() => setHistoryDetail(null)}
        />
      )}
    </div>
  )
}

function CurrentAdministration({
  administration,
  cargos,
  onAssign,
  onFinish,
}) {
  const membersByCargo = useMemo(
    () =>
      new Map(
        administration.integrantes.map((member) => [
          member.cargoId,
          member,
        ]),
      ),
    [administration.integrantes],
  )

  return (
    <article className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-100 bg-gradient-to-r from-[#f2faf8] to-white px-5 py-5 sm:px-6">
        <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[#28727a]">
                Administración vigente
              </p>
              <StatusBadge active />
            </div>

            <h3 className="mt-2 text-xl font-bold text-slate-950">
              {administration.nombre}
            </h3>

            <p className="mt-1 text-sm text-slate-500">
              {formatDate(administration.fechaInicio)} — Actual
            </p>
          </div>

          <button
            className="rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-[#28727a]"
            onClick={onFinish}
            type="button"
          >
            Finalizar período
          </button>
        </div>
      </div>

      <div className="px-5 py-5 sm:px-6">
        <div className="mb-4">
          <h4 className="font-semibold text-slate-900">
            Integrantes
          </h4>
          <p className="mt-1 text-sm text-slate-500">
            Cada cargo puede tener una persona asignada durante el período vigente.
          </p>
        </div>

        {cargos.length === 0 ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-4 text-sm text-amber-900">
            No hay cargos activos disponibles para integrar el comité.
          </div>
        ) : (
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {cargos.map((cargo) => {
              const member = membersByCargo.get(cargo.id)

              return (
                <MemberCard
                  cargo={cargo}
                  key={cargo.id}
                  member={member}
                  onAssign={() => onAssign(cargo)}
                />
              )
            })}
          </div>
        )}
      </div>
    </article>
  )
}

function MemberCard({ cargo, member, onAssign }) {
  return (
    <div className="flex min-h-40 flex-col rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#edf7f6] text-[#28727a]">
          <PersonIcon />
        </div>

        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-500">
          {cargo.nombre}
        </span>
      </div>

      <div className="mt-4 flex-1">
        {member ? (
          <>
            <p className="font-semibold text-slate-900">
              {member.personaNombre}
            </p>
            <p className="mt-1 text-xs text-slate-400">
              Integrante asignado
            </p>
          </>
        ) : (
          <>
            <p className="font-medium text-slate-500">
              Sin asignar
            </p>
            <p className="mt-1 text-xs leading-5 text-slate-400">
              Este cargo todavía no tiene una persona asignada.
            </p>
          </>
        )}
      </div>

      <button
        className="mt-4 self-start text-sm font-semibold text-[#176b72] hover:text-[#123b43] focus:outline-none focus:ring-2 focus:ring-[#28727a]"
        onClick={onAssign}
        type="button"
      >
        {member ? 'Cambiar persona' : 'Asignar persona'}
      </button>
    </div>
  )
}

function EmptyCommittee({ hasHistory, onCreate }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-6 py-10 text-center shadow-sm">
      <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-[#edf7f6] text-[#28727a]">
        <CommitteeIcon />
      </div>

      <h3 className="mt-4 text-lg font-semibold text-slate-900">
        No hay una administración vigente
      </h3>

      <p className="mx-auto mt-2 max-w-xl text-sm leading-6 text-slate-500">
        {hasHistory
          ? 'El período anterior ya fue finalizado. Puedes registrar la nueva administración del comité.'
          : 'Registra el primer período administrativo para comenzar a organizar los cargos del comité.'}
      </p>

      <button
        className="mt-5 rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2"
        onClick={onCreate}
        type="button"
      >
        Crear administración
      </button>
    </div>
  )
}

function HistorySection({ administrations, onOpen }) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div>
        <h3 className="font-semibold text-slate-900">
          Administraciones anteriores
        </h3>
        <p className="mt-1 text-sm text-slate-500">
          Consulta la composición histórica del comité sin alterar sus registros.
        </p>
      </div>

      {administrations.length === 0 ? (
        <div className="mt-5 rounded-lg bg-slate-50 px-4 py-5 text-sm text-slate-500">
          Todavía no existen períodos administrativos finalizados.
        </div>
      ) : (
        <div className="mt-5 divide-y divide-slate-100 rounded-lg border border-slate-200">
          {administrations.map((administration) => (
            <button
              className="flex w-full items-center justify-between gap-4 px-4 py-4 text-left transition hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-[#28727a]"
              key={administration.id}
              onClick={() => onOpen(administration)}
              type="button"
            >
              <div className="min-w-0">
                <p className="truncate font-semibold text-slate-900">
                  {administration.nombre}
                </p>
                <p className="mt-1 text-sm text-slate-500">
                  {formatDate(administration.fechaInicio)}
                  {' — '}
                  {administration.fechaFin
                    ? formatDate(administration.fechaFin)
                    : 'Sin fecha final'}
                </p>
              </div>

              <span className="shrink-0 text-sm font-semibold text-[#176b72]">
                Ver detalle
              </span>
            </button>
          ))}
        </div>
      )}
    </section>
  )
}

function CreatePeriodDialog({ isSaving, onCancel, onConfirm }) {
  const [form, setForm] = useState({
    nombre: '',
    fechaInicio: todayIso(),
  })
  const [error, setError] = useState(null)

  function submit(event) {
    event.preventDefault()

    const nombre = form.nombre.trim()

    if (!nombre) {
      setError('Ingresa un nombre para identificar el período administrativo.')
      return
    }

    if (!form.fechaInicio) {
      setError('Selecciona la fecha de inicio.')
      return
    }

    setError(null)

    onConfirm({
      nombre,
      fechaInicio: form.fechaInicio,
    })
  }

  return (
    <Dialog
      description="Este período quedará como la administración vigente hasta que sea finalizado."
      onClose={onCancel}
      title="Nueva administración del comité"
    >
      <form className="space-y-4" onSubmit={submit}>
        <Field label="Nombre del período">
          <input
            autoFocus
            className={inputClass}
            maxLength="200"
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                nombre: event.target.value,
              }))}
            placeholder="Ej. Comité 2026-2028"
            required
            value={form.nombre}
          />
        </Field>

        <Field label="Fecha de inicio">
          <input
            className={inputClass}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                fechaInicio: event.target.value,
              }))}
            required
            type="date"
            value={form.fechaInicio}
          />
        </Field>

        {error && <Alert message={error} type="error" />}

        <DialogActions
          confirmLabel="Crear administración"
          isSaving={isSaving}
          onCancel={onCancel}
        />
      </form>
    </Dialog>
  )
}

function MemberDialog({
  administration,
  cargo,
  isSaving,
  onCancel,
  onConfirm,
  personas,
}) {
  const existing = administration.integrantes.find(
    (member) => member.cargoId === cargo.id,
  )

  const [personaId, setPersonaId] = useState(
    existing ? String(existing.personaId) : '',
  )
  const [search, setSearch] = useState('')
  const [error, setError] = useState(null)

  const available = useMemo(() => {
    const assignedIds = new Set(
      administration.integrantes
        .filter((member) => member.cargoId !== cargo.id)
        .map((member) => member.personaId),
    )

    const normalized = search.trim().toLocaleLowerCase('es')

    return personas.filter((persona) => {
      if (assignedIds.has(persona.id)) return false

      if (!normalized) return true

      return `${persona.nombres} ${persona.apellidos}`
        .toLocaleLowerCase('es')
        .includes(normalized)
    })
  }, [administration.integrantes, cargo.id, personas, search])

  function submit(event) {
    event.preventDefault()

    const parsed = Number(personaId)

    if (!Number.isInteger(parsed) || parsed <= 0) {
      setError('Selecciona una persona para este cargo.')
      return
    }

    setError(null)

    onConfirm({
      personaId: parsed,
      cargoId: cargo.id,
    })
  }

  return (
    <Dialog
      description={`Selecciona la persona que ocupará el cargo de ${cargo.nombre}.`}
      onClose={onCancel}
      title={existing ? 'Cambiar integrante' : 'Asignar integrante'}
    >
      <form className="space-y-4" onSubmit={submit}>
        <div className="rounded-lg bg-slate-50 p-4">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Cargo
          </p>
          <p className="mt-1 font-semibold text-slate-900">
            {cargo.nombre}
          </p>

          {existing && (
            <p className="mt-2 text-sm text-slate-500">
              Actualmente: {existing.personaNombre}
            </p>
          )}
        </div>

        <Field label="Buscar persona">
          <input
            className={inputClass}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Nombre o apellido"
            type="search"
            value={search}
          />
        </Field>

        <Field label="Persona">
          <select
            className={inputClass}
            onChange={(event) => setPersonaId(event.target.value)}
            required
            value={personaId}
          >
            <option value="">Selecciona una persona</option>
            {available.map((persona) => (
              <option key={persona.id} value={persona.id}>
                {persona.apellidos}, {persona.nombres}
              </option>
            ))}
          </select>
        </Field>

        {available.length === 0 && (
          <p className="rounded-md bg-amber-50 px-3 py-2 text-sm text-amber-900">
            No hay personas activas disponibles con ese criterio.
          </p>
        )}

        {error && <Alert message={error} type="error" />}

        <DialogActions
          confirmLabel={existing ? 'Guardar cambio' : 'Asignar persona'}
          isSaving={isSaving}
          onCancel={onCancel}
        />
      </form>
    </Dialog>
  )
}

function FinishPeriodDialog({
  administration,
  isSaving,
  onCancel,
  onConfirm,
}) {
  const [fechaFin, setFechaFin] = useState(todayIso())
  const [error, setError] = useState(null)

  function submit(event) {
    event.preventDefault()

    if (!fechaFin) {
      setError('Selecciona la fecha de finalización.')
      return
    }

    if (fechaFin < administration.fechaInicio) {
      setError('La fecha final no puede ser anterior a la fecha de inicio.')
      return
    }

    setError(null)
    onConfirm(fechaFin)
  }

  return (
    <Dialog
      description="La administración pasará al historial y sus integrantes se conservarán."
      onClose={onCancel}
      title="Finalizar período administrativo"
    >
      <form className="space-y-4" onSubmit={submit}>
        <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
          <p className="font-semibold text-amber-950">
            {administration.nombre}
          </p>
          <p className="mt-1 text-sm leading-6 text-amber-900">
            Después de finalizar este período ya no podrás modificar sus integrantes.
          </p>
        </div>

        <Field label="Fecha de finalización">
          <input
            className={inputClass}
            min={administration.fechaInicio}
            onChange={(event) => setFechaFin(event.target.value)}
            required
            type="date"
            value={fechaFin}
          />
        </Field>

        {error && <Alert message={error} type="error" />}

        <DialogActions
          confirmLabel="Finalizar período"
          danger
          isSaving={isSaving}
          onCancel={onCancel}
        />
      </form>
    </Dialog>
  )
}

function HistoryDialog({ administration, onClose }) {
  return (
    <Dialog
      description={`${formatDate(administration.fechaInicio)} — ${
        administration.fechaFin
          ? formatDate(administration.fechaFin)
          : 'Sin fecha final'
      }`}
      onClose={onClose}
      title={administration.nombre}
    >
      {administration.integrantes.length === 0 ? (
        <p className="rounded-lg bg-slate-50 px-4 py-5 text-sm text-slate-500">
          No se registraron integrantes para este período.
        </p>
      ) : (
        <div className="space-y-2">
          {administration.integrantes.map((member) => (
            <div
              className="flex flex-col gap-1 rounded-lg border border-slate-200 px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
              key={member.id}
            >
              <p className="font-semibold text-slate-900">
                {member.personaNombre}
              </p>
              <p className="text-sm text-slate-500">
                {member.cargoNombre}
              </p>
            </div>
          ))}
        </div>
      )}

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

function Dialog({ children, description, onClose, title }) {
  const dialogRef = useRef(null)

  useEffect(() => {
    dialogRef.current?.focus()

    function handleKeyDown(event) {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener('keydown', handleKeyDown)

    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  return (
    <div
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/45 p-4"
      role="dialog"
    >
      <div
        className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-xl overflow-y-auto rounded-xl bg-white p-5 shadow-2xl outline-none sm:p-6"
        ref={dialogRef}
        tabIndex="-1"
      >
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
  danger = false,
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
        className={`rounded-md px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60 ${
          danger
            ? 'bg-red-700 hover:bg-red-800'
            : 'bg-[#123b43] hover:bg-[#0d2d33]'
        }`}
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
          ? 'bg-[#e3f2ed] text-[#17644e]'
          : 'bg-slate-100 text-slate-600'
      }`}
    >
      <span
        className={`h-1.5 w-1.5 rounded-full ${
          active ? 'bg-[#238568]' : 'bg-slate-400'
        }`}
      />
      {active ? 'Vigente' : 'Finalizada'}
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

function PersonIcon() {
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
      <path d="M6.5 19c.7-3.2 2.6-5 5.5-5s4.8 1.8 5.5 5" />
    </svg>
  )
}

function CommitteeIcon() {
  return (
    <svg
      aria-hidden="true"
      className="h-6 w-6"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      viewBox="0 0 24 24"
    >
      <circle cx="9" cy="8" r="2.5" />
      <circle cx="16.5" cy="9.5" r="2" />
      <path d="M4.5 18c.5-3.1 2-5 4.5-5s4 1.9 4.5 5M14 14.5c2.8-.5 4.6.8 5.5 3.5" />
    </svg>
  )
}

function comparePersonas(first, second) {
  return (
    first.apellidos.localeCompare(second.apellidos, 'es', {
      sensitivity: 'base',
    }) ||
    first.nombres.localeCompare(second.nombres, 'es', {
      sensitivity: 'base',
    }) ||
    first.id - second.id
  )
}

function formatDate(value) {
  if (!value) return '—'

  const parts = value.split('-')

  if (parts.length !== 3) return value

  const [year, month, day] = parts

  return new Intl.DateTimeFormat('es-GT', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(
    new Date(
      Date.UTC(
        Number(year),
        Number(month) - 1,
        Number(day),
      ),
    ),
  )
}

function todayIso() {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  const local = new Date(now.getTime() - offset * 60000)

  return local.toISOString().slice(0, 10)
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
    return error.message || 'El registro solicitado ya no existe.'
  }

  if (error?.status === 409) {
    return error.message || 'La operación entra en conflicto con el estado actual.'
  }

  if (error?.kind === 'network') {
    return 'No se pudo conectar con el servidor.'
  }

  return error?.message || fallback
}

const inputClass =
  'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:cursor-not-allowed disabled:bg-slate-100'

export default AdministracionPage
