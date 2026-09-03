export class HttpError extends Error {
  constructor(message, { status = 0, data = null } = {}) {
    super(message)
    this.name = 'HttpError'
    this.status = status
    this.data = data
    this.kind = getErrorKind(status)
  }
}

function getErrorKind(status) {
  if (status === 401) return 'unauthorized'
  if (status === 403) return 'forbidden'
  if (status >= 400 && status < 500) return 'client'
  if (status >= 500) return 'server'
  return 'http'
}

export function isNetworkError(error) {
  return error instanceof HttpError && error.kind === 'network'
}

export function createNetworkError(error) {
  const message = error instanceof Error ? error.message : 'No se pudo conectar con la API'
  const networkError = new HttpError(message)
  networkError.kind = 'network'
  return networkError
}