function Panel({ children, className = '' }) {
  return <section className={`rounded-lg border border-slate-200 bg-white p-5 shadow-sm ${className}`}>{children}</section>
}

export default Panel