import { httpClient } from './httpClient.js'

export function getSuministroQrPublic(token) {
  return httpClient.get(`/suministros/public/qr/${encodeURIComponent(token)}`)
}
