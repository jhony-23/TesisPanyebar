const pagoPath = '/pagos'

export function getPagos(authenticatedRequest) {
  return authenticatedRequest(pagoPath)
}

export function getPagoById(authenticatedRequest, id) {
  return authenticatedRequest(`${pagoPath}/${id}`)
}

export function registerPago(authenticatedRequest, input) {
  return authenticatedRequest(pagoPath, {
    body: input,
    method: 'POST',
  })
}

export function getPagoComprobante(authenticatedRequest, id) {
  return authenticatedRequest(`${pagoPath}/${id}/comprobante`)
}
export function annulPayment(authenticatedRequest, id) {
  return authenticatedRequest(`/pagos/${id}/anulacion`, {
    method: 'POST',
  })
}
