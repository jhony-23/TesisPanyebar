import { useMemo, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../app/useAuth.js'
import { NAVIGATION_ITEMS } from '../app/permissions.js'
import comiteLogo from '../assets/branding/comite-agua-panyebar-white.png'

function AdminLayout() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const { hasAnyPermission, logout, user } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  const navigation = useMemo(() => {
    return NAVIGATION_ITEMS.filter((item) => hasAnyPermission(item.permissions))
  }, [hasAnyPermission])

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  function closeSidebar() {
    setIsSidebarOpen(false)
  }

  return (
    <div className="min-h-screen bg-[#f5f7f6] text-slate-900">
      {isSidebarOpen && (
        <button
          aria-label="Cerrar menú"
          className="fixed inset-0 z-30 bg-slate-950/35 md:hidden"
          onClick={closeSidebar}
          type="button"
        />
      )}

      <aside className={`fixed inset-y-0 left-0 z-40 flex w-72 flex-col bg-[#123b43] text-white transition-transform duration-200 md:translate-x-0 ${isSidebarOpen ? 'translate-x-0' : '-translate-x-full'}`}>
        <div className="flex h-20 items-center border-b border-white/10 px-6">
          <img alt="Comité de Agua Potable de Panyebar" className="mr-2 h-16 w-16 shrink-0 rounded-md object-contain" src={comiteLogo} />
          <div>
            <p className="text-lg font-semibold tracking-tight">Panyebar</p>
            <p className="text-xs text-teal-100/70">Comité de agua potable</p>
          </div>
          <button
            aria-label="Cerrar menú"
            className="ml-auto rounded-md p-2 text-teal-100 hover:bg-white/10 focus:outline-none focus:ring-2 focus:ring-teal-200 md:hidden"
            onClick={closeSidebar}
            type="button"
          >
            <CloseIcon />
          </button>
        </div>
        <nav aria-label="Navegación administrativa" className="flex-1 space-y-1 overflow-y-auto px-4 py-6">
          <p className="mb-3 px-3 text-[11px] font-semibold uppercase tracking-[0.16em] text-teal-100/55">Menú principal</p>
          {navigation.map((item) => (
            item.available ? (
              <NavLink
                className={({ isActive }) => `flex items-center gap-3 rounded-md px-3 py-2.5 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-teal-200 ${isActive ? 'bg-white text-[#123b43] font-semibold' : 'text-teal-50 hover:bg-white/10'}`}
                end
                key={item.label}
                onClick={closeSidebar}
                to={item.path}
              >
                <NavigationIcon name={item.icon} />
                {item.label}
              </NavLink>
            ) : (
              <div className="flex items-center gap-3 rounded-md px-3 py-2.5 text-sm text-teal-100/45" key={item.label}>
                <NavigationIcon name={item.icon} />
                <span>{item.label}</span>
                <span className="ml-auto text-[10px] uppercase tracking-wide">Próximo</span>
              </div>
            )
          ))}
        </nav>
        <div className="border-t border-white/10 p-4">
          <div className="flex items-center gap-3 rounded-md bg-white/10 p-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-[#d6a85f] text-sm font-bold text-[#123b43]">
              {user?.nombreUsuario?.slice(0, 1).toUpperCase()}
            </div>
            <div className="min-w-0">
              <p className="truncate text-sm font-medium">{user?.nombreUsuario}</p>
              <p className="text-xs text-teal-100/65">Sesión activa</p>
            </div>
          </div>
        </div>
      </aside>

      <div className="md:pl-72">
        <header className="sticky top-0 z-20 flex h-20 items-center justify-between border-b border-slate-200 bg-[#f5f7f6]/95 px-4 backdrop-blur sm:px-8">
          <div className="flex items-center gap-3">
            <button
              aria-label="Abrir menú"
              className="rounded-md p-2 text-slate-700 hover:bg-white focus:outline-none focus:ring-2 focus:ring-teal-700 md:hidden"
              onClick={() => setIsSidebarOpen(true)}
              type="button"
            >
              <MenuIcon />
            </button>
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[#28727a]">Área administrativa</p>
              <p className="text-sm text-slate-500">Gestión del servicio comunitario</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <span className="hidden text-sm text-slate-600 sm:block">{user?.nombreUsuario}</span>
            <button
              className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:border-slate-400 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-teal-700"
              onClick={handleLogout}
              type="button"
            >
              Cerrar sesión
            </button>
          </div>
        </header>
        <main className="min-w-0 px-4 py-8 sm:px-8 sm:py-10">
          <Outlet context={{ currentPath: location.pathname }} />
        </main>
      </div>
    </div>
  )
}

function NavigationIcon({ name }) {
  const paths = {
    calendar: <><rect height="15" rx="2" width="16" x="4" y="5" /><path d="M8 3v4M16 3v4M4 10h16" /></>,
    card: <><rect height="14" rx="2" width="18" x="3" y="5" /><path d="M3 10h18M7 15h3" /></>,
    chart: <><path d="M4 20V10M10 20V4M16 20v-7M22 20H2" /></>,
    coins: <><ellipse cx="12" cy="6" rx="7" ry="3" /><path d="M5 6v5c0 1.7 3.1 3 7 3s7-1.3 7-3V6M5 11v5c0 1.7 3.1 3 7 3s7-1.3 7-3v-5" /></>,
    document: <><path d="M6 3h9l4 4v14H6z" /><path d="M14 3v5h5M9 13h6M9 17h6" /></>,
    drop: <path d="M12 3S6 10 6 14a6 6 0 0 0 12 0c0-4-6-11-6-11Z" />,
    water: <><path d="M12 3.5c-2.8 3.7-5.5 6.7-5.5 10.2a5.5 5.5 0 0 0 11 0C17.5 10.2 14.8 7.2 12 3.5Z" /><path d="M9.5 14.2a2.8 2.8 0 0 0 2.8 2.8" /></>,
    home: <><path d="m3 11 9-8 9 8" /><path d="M5 10v10h14V10M9 20v-6h6v6" /></>,
    inbox: <><path d="M4 5h16v14H4z" /><path d="M4 14h4l2 2h4l2-2h4" /></>,
    map: <><path d="m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3z" /><path d="M9 3v15M15 6v15" /></>,
    report: <><path d="M5 3h14v18H5z" /><path d="M8 8h8M8 12h8M8 16h5" /></>,
    settings: <><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1-1.8 1.8-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.5V20h-2.6v-.1a1.7 1.7 0 0 0-1-1.5 1.7 1.7 0 0 0-1.9.3l-.1.1-1.8-1.8.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.5-1H6v-2.6h.1a1.7 1.7 0 0 0 1.5-1 1.7 1.7 0 0 0-.3-1.9l-.1-.1L9 6.6l.1.1a1.7 1.7 0 0 0 1.9.3 1.7 1.7 0 0 0 1-1.5V5h2.6v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.9-.3l.1-.1 1.8 1.8-.1.1a1.7 1.7 0 0 0-.3 1.9 1.7 1.7 0 0 0 1.5 1h.1v2.6h-.1a1.7 1.7 0 0 0-1.5 1Z" /></>,
    truck: <><path d="M3 6h11v10H3zM14 10h4l3 3v3h-7z" /><circle cx="7" cy="18" r="2" /><circle cx="18" cy="18" r="2" /></>,
    users: <><circle cx="9" cy="8" r="3" /><path d="M3 20a6 6 0 0 1 12 0M16 5a3 3 0 0 1 0 6M18 14a5 5 0 0 1 3 6" /></>,
  }

  return <svg aria-hidden="true" className="h-[18px] w-[18px] shrink-0" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.7" viewBox="0 0 24 24">{paths[name]}</svg>
}

function MenuIcon() {
  return <svg aria-hidden="true" className="h-6 w-6" fill="none" stroke="currentColor" strokeLinecap="round" strokeWidth="2" viewBox="0 0 24 24"><path d="M4 7h16M4 12h16M4 17h16" /></svg>
}

function CloseIcon() {
  return <svg aria-hidden="true" className="h-5 w-5" fill="none" stroke="currentColor" strokeLinecap="round" strokeWidth="2" viewBox="0 0 24 24"><path d="m6 6 12 12M18 6 6 18" /></svg>
}

export default AdminLayout
