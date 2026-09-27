import comiteLogo from '../assets/branding/comite-agua-panyebar.png'

function HomePage() {
  return (
    <main className="mx-auto flex min-h-[calc(100vh-73px)] max-w-5xl items-center px-6 py-12">
      <section className="grid w-full items-center gap-10 md:grid-cols-[1fr_280px] md:gap-12">
        <div>
          <p className="text-sm font-semibold uppercase tracking-wide text-cyan-700">COMITÉ DE AGUA POTABLE</p>
          <h1 className="mt-3 text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
            Agua potable para nuestra comunidad
          </h1>
          <p className="mt-5 text-base font-medium text-[#28727a]">Aldea Panyebar, San Juan La Laguna, Sololá</p>
          <p className="mt-4 max-w-xl text-lg text-slate-600">
            Sistema digital para apoyar la administración del servicio de agua potable de la comunidad.
          </p>
        </div>
        <img alt="Comité de Agua Potable de Panyebar" className="mx-auto h-56 w-56 rounded-2xl object-cover md:h-64 md:w-64" src={comiteLogo} />
      </section>
    </main>
  )
}

export default HomePage
