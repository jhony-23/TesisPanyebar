import { Link } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'
import { NAVIGATION_ITEMS } from '../app/permissions.js'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'

function AdminPage() {
  const { hasAnyPermission, user } = useAuth()

  const availableModules = NAVIGATION_ITEMS.filter(
    (item) =>
      item.path !== '/admin' &&
      item.available &&
      hasAnyPermission(item.permissions),
  )

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        description="Consulta y organiza la información del servicio de agua potable de la comunidad."
        eyebrow="Inicio"
        title={`Bienvenido, ${user.nombreUsuario}`}
      />

      <Panel className="border-l-4 border-l-[#d6a85f]">
        <div>
          <p className="text-sm font-semibold text-slate-900">
            Panel administrativo
          </p>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
            Accede a los módulos habilitados según los permisos de tu cuenta.
          </p>
        </div>
      </Panel>

      <Panel>
        <h2 className="text-base font-semibold text-slate-900">
          Módulos disponibles
        </h2>

        {availableModules.length > 0 ? (
          <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {availableModules.map((item) => (
              <Link
                className="rounded-md border border-slate-200 bg-white p-4 transition hover:border-[#28727a] hover:bg-[#eef6f5] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2"
                key={item.path}
                to={item.path}
              >
                <p className="text-sm font-semibold text-[#1c5961]">
                  {item.label}
                </p>
                <p className="mt-1 text-xs leading-5 text-slate-600">
                  Abrir módulo
                </p>
              </Link>
            ))}
          </div>
        ) : (
          <div className="mt-4 rounded-md border border-slate-200 bg-slate-50 p-5">
            <p className="text-sm font-semibold text-slate-800">
              Sin módulos adicionales disponibles
            </p>
            <p className="mt-1 text-sm leading-6 text-slate-600">
              Tu cuenta no tiene permisos asignados para consultar otros módulos administrativos.
            </p>
          </div>
        )}
      </Panel>
    </div>
  )
}

export default AdminPage
