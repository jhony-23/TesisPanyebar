import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import EmptyState from '../components/ui/EmptyState.jsx'
import { useAuth } from '../app/useAuth.js'

function AdminPage() {
  const { user } = useAuth()

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        description="Consulta y organiza la información del servicio de agua potable de la comunidad."
        eyebrow="Inicio"
        title={`Bienvenido, ${user.nombreUsuario}`}
      />
      <Panel className="border-l-4 border-l-[#d6a85f]">
        <p className="text-sm font-semibold text-slate-900">Panel administrativo</p>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
          Esta es la base de trabajo para las próximas áreas del sistema. Los módulos se habilitarán conforme se incorporen sus funciones.
        </p>
      </Panel>
      <div className="grid gap-5 lg:grid-cols-2">
        <Panel>
          <h2 className="text-base font-semibold text-slate-900">Próximas áreas</h2>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            <div className="rounded-md bg-[#eef6f5] p-4">
              <p className="text-sm font-semibold text-[#1c5961]">Gestión comunitaria</p>
              <p className="mt-1 text-xs leading-5 text-slate-600">Personas y suministros estarán disponibles próximamente.</p>
            </div>
            <div className="rounded-md bg-[#f8f1e5] p-4">
              <p className="text-sm font-semibold text-[#795b2e]">Operación del servicio</p>
              <p className="mt-1 text-xs leading-5 text-slate-600">Jornadas, pagos y abastecimiento están en preparación.</p>
            </div>
          </div>
        </Panel>
        <Panel>
          <h2 className="text-base font-semibold text-slate-900">Estado del sistema</h2>
          <div className="mt-4">
            <EmptyState
              description="Todavía no hay información operativa disponible en esta versión base."
              title="Sin datos operativos"
            />
          </div>
        </Panel>
      </div>
    </div>
  )
}

export default AdminPage