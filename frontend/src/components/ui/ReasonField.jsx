import { useEffect, useId, useRef, useState } from 'react'

const inputClass = 'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:opacity-60'

export default function ReasonField({
  label = 'Motivo',
  options,
  value,
  onChange,
  disabled = false,
  required = true,
  maxLength,
  autoFocus = false,
  placeholder = 'Seleccione un motivo',
  otherLabel = 'Especifique el motivo',
}) {
  const id = useId()
  const customRef = useRef(null)
  const [selection, setSelection] = useState(() => value && !options.includes(value) ? 'other' : value)

  useEffect(() => {
    if (selection === 'other') customRef.current?.focus()
  }, [selection])

  function selectReason(event) {
    const next = event.target.value
    setSelection(next)
    onChange(next === 'other' ? '' : next)
  }

  return (
    <div>
      <label className="text-sm font-medium text-slate-700" htmlFor={id}>
        {label} {required ? <span className="text-red-700">*</span> : <span className="font-normal text-slate-400">(opcional)</span>}
      </label>
      <select autoFocus={autoFocus} className={inputClass} disabled={disabled} id={id} onChange={selectReason} required={required} value={selection}>
        <option value="">{placeholder}</option>
        {options.map((option) => <option key={option} value={option}>{option}</option>)}
        <option value="other">Otro</option>
      </select>
      {selection === 'other' && (
        <div className="mt-3">
          <label className="text-sm font-medium text-slate-700" htmlFor={`${id}-other`}>
            {otherLabel} <span className="text-red-700">*</span>
          </label>
          <input
            className={inputClass}
            disabled={disabled}
            id={`${id}-other`}
            maxLength={maxLength}
            onChange={(event) => onChange(event.target.value)}
            pattern=".*\S.*"
            ref={customRef}
            required
            title={`${otherLabel}; no puede contener solo espacios.`}
            value={value}
          />
        </div>
      )}
    </div>
  )
}
