const GUATEMALA_LOCALE = 'es-GT'
const GUATEMALA_TIME_ZONE = 'America/Guatemala'

const guatemalaDateTimeFormatter = new Intl.DateTimeFormat(
  GUATEMALA_LOCALE,
  {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: GUATEMALA_TIME_ZONE,
    hourCycle: 'h23',
  },
)

const guatemalaDateInputFormatter = new Intl.DateTimeFormat('en-CA', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  timeZone: GUATEMALA_TIME_ZONE,
})

export function formatGuatemalaDateTime(value, emptyValue = 'Sin fecha') {
  if (!value) return emptyValue

  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? String(value)
    : guatemalaDateTimeFormatter.format(date)
}

export function formatCivilDate(value, emptyValue = 'Sin fecha') {
  if (!value) return emptyValue

  const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/)

  return match
    ? `${match[3]}/${match[2]}/${match[1]}`
    : String(value)
}

export function formatCivilTime(value, emptyValue = '') {
  if (!value) return emptyValue

  const match = String(value).match(/^(\d{2}):(\d{2})/)

  return match ? `${match[1]}:${match[2]}` : String(value)
}

export function guatemalaToday() {
  return guatemalaDateInputFormatter.format(new Date())
}
