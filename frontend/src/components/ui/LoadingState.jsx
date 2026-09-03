function LoadingState({ message = 'Cargando...' }) {
  return <p aria-live="polite" className="rounded-md bg-slate-100 px-4 py-3 text-sm text-slate-600">{message}</p>
}

export default LoadingState