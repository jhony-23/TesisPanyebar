import { apiBaseUrl } from '../config/api.js'
import { createNetworkError, HttpError } from './httpError.js'

function buildUrl(path) {
  if (/^https?:\/\//i.test(path)) return path

  const baseUrl = apiBaseUrl.endsWith('/') ? apiBaseUrl.slice(0, -1) : apiBaseUrl
  const resourcePath = path.startsWith('/') ? path : `/${path}`
  return `${baseUrl}${resourcePath}`
}

async function parseResponseBody(response) {
  if (response.status === 204) return null

  const contentType = response.headers.get('content-type') || ''
  if (contentType.includes('application/json')) return response.json()

  const text = await response.text()
  return text || null
}

export async function request(path, { accessToken, body, headers = {}, ...options } = {}) {
  const requestHeaders = new Headers(headers)
  if (body !== undefined && !requestHeaders.has('Content-Type')) {
    requestHeaders.set('Content-Type', 'application/json')
  }
  if (accessToken) requestHeaders.set('Authorization', `Bearer ${accessToken}`)

  let response
  try {
    response = await fetch(buildUrl(path), {
      ...options,
      body: body === undefined ? undefined : JSON.stringify(body),
      headers: requestHeaders,
    })
  } catch (error) {
    throw createNetworkError(error)
  }

  const data = await parseResponseBody(response)
  if (!response.ok) {
    const message = typeof data === 'string' ? data : data?.message || 'La solicitud no fue exitosa'
    throw new HttpError(message, { data, status: response.status })
  }

  return data
}

export const httpClient = {
  delete: (path, options) => request(path, { ...options, method: 'DELETE' }),
  get: (path, options) => request(path, { ...options, method: 'GET' }),
  post: (path, body, options) => request(path, { ...options, body, method: 'POST' }),
  put: (path, body, options) => request(path, { ...options, body, method: 'PUT' }),
  request,
}