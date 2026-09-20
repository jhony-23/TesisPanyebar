import { useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useAuth } from '../app/useAuth.js'
import { PERMISOS } from '../app/permissions.js'
import EmptyState from '../components/ui/EmptyState.jsx'
import LoadingState from '../components/ui/LoadingState.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Panel from '../components/ui/Panel.jsx'
import { getObligaciones } from '../services/obligacionService.js'
import {
  annulPayment,
  getPagoById,
  getPagoComprobante,
  getPagos,
  registerPago,
} from '../services/pagoService.js'
import { getPersonas } from '../services/personaService.js'
import { getSuministros } from '../services/suministroService.js'
import { formatCivilDate, formatGuatemalaDateTime } from '../utils/dateTime.js'

const initialForm = {
  holderKey: '',
  concepto: '',
  obligationIds: [],
}

function PagosPage() {
  const { authenticatedRequest, hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.PAGOS_GESTIONAR)
  const requestRef = useRef(authenticatedRequest)

  const [pagos, setPagos] = useState([])
  const [obligaciones, setObligaciones] = useState([])
  const [personas, setPersonas] = useState([])
  const [suministros, setSuministros] = useState([])
  const [form, setForm] = useState(initialForm)
  const [selected, setSelected] = useState(null)
  const [receipt, setReceipt] = useState(null)
  const [pendingConfirmation, setPendingConfirmation] = useState(false)
  const [pendingAnnulment, setPendingAnnulment] = useState(null)
  const [isRegisterOpen, setIsRegisterOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isReferencesLoading, setIsReferencesLoading] = useState(true)
  const [isDetailLoading, setIsDetailLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState(null)
  const [formError, setFormError] = useState(null)
  const [referenceError, setReferenceError] = useState(null)
  const [feedback, setFeedback] = useState(null)

  useEffect(() => {
    let mounted = true

    async function loadData() {
      setIsLoading(true)
      setIsReferencesLoading(true)
      setError(null)
      setReferenceError(null)

      const [paymentsResult, obligationsResult, peopleResult, suppliesResult] =
        await Promise.allSettled([
          getPagos(requestRef.current),
          getObligaciones(requestRef.current),
          getPersonas(requestRef.current),
          getSuministros(requestRef.current),
        ])

      if (!mounted) return

      if (paymentsResult.status === 'fulfilled') {
        setPagos(
          Array.isArray(paymentsResult.value)
            ? paymentsResult.value
            : [],
        )
      } else {
        setError(
          getRequestMessage(
            paymentsResult.reason,
            'No se pudieron cargar los pagos.',
          ),
        )
      }

      if (obligationsResult.status === 'fulfilled') {
        setObligaciones(
          Array.isArray(obligationsResult.value)
            ? obligationsResult.value
            : [],
        )
      } else {
        setReferenceError(
          getRequestMessage(
            obligationsResult.reason,
            'No se pudieron cargar las obligaciones disponibles para pago.',
          ),
        )
      }

      if (peopleResult.status === 'fulfilled') {
        setPersonas(
          Array.isArray(peopleResult.value)
            ? peopleResult.value
            : [],
        )
      } else {
        setReferenceError(
          'No se pudieron cargar todas las referencias necesarias para identificar titulares.',
        )
      }

      if (suppliesResult.status === 'fulfilled') {
        setSuministros(
          Array.isArray(suppliesResult.value)
            ? suppliesResult.value
            : [],
        )
      } else {
        setReferenceError(
          'No se pudieron cargar todas las referencias necesarias para identificar titulares.',
        )
      }

      setIsLoading(false)
      setIsReferencesLoading(false)
    }

    loadData()

    return () => {
      mounted = false
    }
  }, [])

  const personaById = useMemo(
    () => new Map(personas.map((persona) => [persona.id, persona])),
    [personas],
  )

  const supplyById = useMemo(
    () => new Map(suministros.map((supply) => [supply.id, supply])),
    [suministros],
  )

  const pendingObligations = useMemo(
    () => obligaciones.filter((obligation) => obligation.estado === 1),
    [obligaciones],
  )

  const holders = useMemo(
    () =>
      buildHolders(
        pendingObligations,
        personaById,
        supplyById,
      ),
    [pendingObligations, personaById, supplyById],
  )

  const selectedHolder = holders.find(
    (holder) => holder.key === form.holderKey,
  )

  const holderObligations = selectedHolder?.obligations ?? []

  const selectedObligations = holderObligations.filter(
    (obligation) => form.obligationIds.includes(obligation.id),
  )

  const total = selectedObligations.reduce(
    (sum, obligation) => sum + Number(obligation.monto || 0),
    0,
  )

  function openRegister() {
    setForm(initialForm)
    setFormError(null)
    setFeedback(null)
    setPendingConfirmation(false)
    setIsRegisterOpen(true)

    requestAnimationFrame(() => {
      document
        .getElementById('registro-pago')
        ?.scrollIntoView({
          behavior: 'smooth',
          block: 'start',
        })
    })
  }

  function closeRegister() {
    if (isSaving) return

    setForm(initialForm)
    setFormError(null)
    setPendingConfirmation(false)
    setIsRegisterOpen(false)
  }

  function selectHolder(event) {
    setForm((current) => ({
      ...current,
      holderKey: event.target.value,
      obligationIds: [],
    }))

    setFormError(null)
  }

  function toggleObligation(id) {
    setForm((current) => ({
      ...current,
      obligationIds: current.obligationIds.includes(id)
        ? current.obligationIds.filter(
            (obligationId) => obligationId !== id,
          )
        : [...current.obligationIds, id],
    }))

    setFormError(null)
  }

  function selectAllHolderObligations() {
    setForm((current) => ({
      ...current,
      obligationIds:
        current.obligationIds.length === holderObligations.length
          ? []
          : holderObligations.map((obligation) => obligation.id),
    }))

    setFormError(null)
  }

  function changeConcept(event) {
    setForm((current) => ({
      ...current,
      concepto: event.target.value,
    }))

    setFormError(null)
  }

  function validatePayment() {
    if (!selectedHolder) {
      return 'Selecciona el titular del pago.'
    }

    if (selectedObligations.length === 0) {
      return 'Selecciona al menos una obligación pendiente.'
    }

    if (!form.concepto.trim()) {
      return 'Ingresa el concepto del pago.'
    }

    if (form.concepto.trim().length > 200) {
      return 'El concepto no puede superar 200 caracteres.'
    }

    if (total <= 0) {
      return 'El total seleccionado debe ser mayor que cero.'
    }

    return null
  }

  function requestConfirmation(event) {
    event.preventDefault()

    const validationError = validatePayment()

    if (validationError) {
      setFormError(validationError)
      return
    }

    setFormError(null)
    setPendingConfirmation(true)
  }

  async function confirmPayment() {
    const validationError = validatePayment()

    if (validationError) {
      setFormError(validationError)
      setPendingConfirmation(false)
      return
    }

    setIsSaving(true)
    setError(null)
    setFormError(null)
    setFeedback(null)

    try {
      const created = await registerPago(
        authenticatedRequest,
        {
          monto: total,
          concepto: form.concepto.trim(),
          obligacionIds: selectedObligations.map(
            (obligation) => obligation.id,
          ),
        },
      )

      setPagos((current) => [
        toPaymentSummary(created),
        ...current.filter((payment) => payment.id !== created.id),
      ])

      const paidIds = new Set(
        created.obligaciones.map((obligation) => obligation.id),
      )

      setObligaciones((current) =>
        current.map((obligation) =>
          paidIds.has(obligation.id)
            ? { ...obligation, estado: 2, esMorosa: false }
            : obligation,
        ),
      )

      setSelected(created)

      try {
        const receiptResult = await getPagoComprobante(
          authenticatedRequest,
          created.id,
        )
        setReceipt(receiptResult)
      } catch {
        setReceipt(null)
      }

      setFeedback(
        `Pago registrado correctamente. Comprobante ${created.numeroComprobante}.`,
      )

      setForm(initialForm)
      setPendingConfirmation(false)
      setIsRegisterOpen(false)
    } catch (requestError) {
      setFormError(
        getRequestMessage(
          requestError,
          'No se pudo registrar el pago.',
        ),
      )
      setPendingConfirmation(false)
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmAnnulment() {
    if (!pendingAnnulment || isSaving) return

    setIsSaving(true)
    setError(null)
    setFeedback(null)

    try {
      const annulled = await annulPayment(
        authenticatedRequest,
        pendingAnnulment.id,
      )

      setPagos((current) =>
        current.map((payment) =>
          payment.id === annulled.id
            ? toPaymentSummary(annulled)
            : payment,
        ),
      )

      if (selected?.id === annulled.id) {
        setSelected(annulled)

        try {
          const receiptResult = await getPagoComprobante(
            authenticatedRequest,
            annulled.id,
          )
          setReceipt(receiptResult)
        } catch {
          setReceipt(null)
        }
      }

      try {
        const obligationsResult = await getObligaciones(
          authenticatedRequest,
        )
        setObligaciones(
          Array.isArray(obligationsResult)
            ? obligationsResult
            : [],
        )
      } catch {
        // La anulacion ya fue confirmada por el servidor.
        // Una recarga posterior volvera a sincronizar referencias.
      }

      setPendingAnnulment(null)
      setFeedback(
        'Pago anulado correctamente. Las obligaciones asociadas volvieron a estar pendientes.',
      )
    } catch (requestError) {
      setError(
        getRequestMessage(
          requestError,
          'No se pudo anular el pago.',
        ),
      )

      if (requestError?.status === 409) {
        try {
          const refreshed = await getPagoById(
            authenticatedRequest,
            pendingAnnulment.id,
          )

          setPagos((current) =>
            current.map((payment) =>
              payment.id === refreshed.id
                ? toPaymentSummary(refreshed)
                : payment,
            ),
          )

          if (selected?.id === refreshed.id) {
            setSelected(refreshed)
          }
        } catch {
          // Conservamos el error original de anulacion.
        }
      }
    } finally {
      setIsSaving(false)
    }
  }
  async function openPayment(id) {
    setIsDetailLoading(true)
    setError(null)
    setSelected(null)
    setReceipt(null)

    try {
      const [detail, receiptResult] = await Promise.all([
        getPagoById(authenticatedRequest, id),
        getPagoComprobante(authenticatedRequest, id),
      ])

      setSelected(detail)
      setReceipt(receiptResult)
    } catch (requestError) {
      setError(
        getRequestMessage(
          requestError,
          'No se pudo cargar el detalle del pago.',
        ),
      )
    } finally {
      setIsDetailLoading(false)
    }
  }

  const registerUnavailable =
    isReferencesLoading ||
    Boolean(referenceError) ||
    pendingObligations.length === 0

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <PageHeader
        action={canManage ? (
          <button
            className="rounded-md bg-[#123b43] px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-[#0d2d33] focus:outline-none focus:ring-2 focus:ring-[#28727a] focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={registerUnavailable}
            onClick={openRegister}
            type="button"
          >
            Registrar pago
          </button>
        ) : null}
        description="Consulta pagos registrados y cancela una o varias obligaciones completas del mismo titular."
        eyebrow="Gestión financiera"
        title="Pagos"
      />

      {error && (
        <Alert
          message={error}
          onDismiss={() => setError(null)}
        />
      )}

      {referenceError && (
        <Alert message={referenceError} />
      )}

      {feedback && (
        <SuccessAlert message={feedback} />
      )}

      {!isReferencesLoading &&
        !referenceError &&
        pendingObligations.length === 0 && (
          <InfoAlert message="No existen obligaciones pendientes disponibles para registrar un pago." />
        )}

      {canManage && isRegisterOpen && (
        <div id="registro-pago">
          <PaymentForm
            form={form}
            formError={formError}
            holderObligations={holderObligations}
            holders={holders}
            isSaving={isSaving}
            onCancel={closeRegister}
            onConceptChange={changeConcept}
            onHolderChange={selectHolder}
            onSelectAll={selectAllHolderObligations}
            onSubmit={requestConfirmation}
            onToggleObligation={toggleObligation}
            selectedHolder={selectedHolder}
            selectedObligations={selectedObligations}
            total={total}
          />
        </div>
      )}

      <Panel>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold text-slate-900">
              Pagos registrados
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              {pagos.length}{' '}
              {pagos.length === 1 ? 'pago' : 'pagos'}
            </p>
          </div>

          <div className="rounded-md bg-slate-50 px-3 py-2 text-xs leading-5 text-slate-600">
            Sin pagos parciales, sobrepagos ni saldos a favor.
          </div>
        </div>

        <div className="mt-5">
          {isLoading ? (
            <LoadingState message="Cargando pagos..." />
          ) : pagos.length === 0 ? (
            <EmptyState
              description="Los pagos registrados aparecerán aquí junto con su número de comprobante."
              title="No hay pagos registrados"
            />
          ) : (
            <PaymentList
              isSaving={isSaving}
              onAnnul={setPendingAnnulment}
              onOpen={openPayment}
              pagos={pagos}
            />
          )}
        </div>
      </Panel>

      {isDetailLoading && (
        <Panel>
          <LoadingState message="Cargando detalle del pago..." />
        </Panel>
      )}

      {selected && !isDetailLoading && (
        <PaymentDetail
          payment={selected}
          receipt={receipt}
        />
      )}

      {canManage && pendingAnnulment && (
        <PaymentAnnulmentDialog
          isSaving={isSaving}
          onCancel={() => setPendingAnnulment(null)}
          onConfirm={confirmAnnulment}
          payment={pendingAnnulment}
        />
      )}
      {canManage && pendingConfirmation && selectedHolder && (
        <ConfirmationDialog
          holder={selectedHolder}
          isSaving={isSaving}
          obligations={selectedObligations}
          onCancel={() => setPendingConfirmation(false)}
          onConfirm={confirmPayment}
          total={total}
        />
      )}
    </div>
  )
}

function PaymentForm({
  form,
  formError,
  holderObligations,
  holders,
  isSaving,
  onCancel,
  onConceptChange,
  onHolderChange,
  onSelectAll,
  onSubmit,
  onToggleObligation,
  selectedHolder,
  selectedObligations,
  total,
}) {
  return (
    <Panel>
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-base font-semibold text-slate-900">
            Registrar pago
          </h2>
          <p className="mt-1 text-sm leading-6 text-slate-500">
            Selecciona un titular y las obligaciones completas que cancelará.
          </p>
        </div>

        <button
          aria-label="Cerrar formulario de pago"
          className="rounded-md px-2 py-1 text-sm font-semibold text-slate-500 hover:bg-slate-100"
          disabled={isSaving}
          onClick={onCancel}
          type="button"
        >
          Cerrar
        </button>
      </div>

      <form
        className="mt-6 space-y-6"
        onSubmit={onSubmit}
      >
        <Field
          inputId="pago-titular"
          label="Titular"
        >
          <select
            className={inputClass}
            disabled={isSaving}
            id="pago-titular"
            onChange={onHolderChange}
            required
            value={form.holderKey}
          >
            <option value="">
              Selecciona un titular con obligaciones pendientes
            </option>

            {holders.map((holder) => (
              <option
                key={holder.key}
                value={holder.key}
              >
                {holder.optionLabel}
              </option>
            ))}
          </select>
        </Field>

        {selectedHolder && (
          <div className="rounded-md border border-[#28727a]/20 bg-[#eef6f5] p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-[#28727a]">
              Titular seleccionado
            </p>
            <p className="mt-1 font-semibold text-[#123b43]">
              {selectedHolder.name}
            </p>
            <p className="mt-1 text-sm text-[#1c5961]">
              {selectedHolder.description}
            </p>
          </div>
        )}

        {selectedHolder && (
          <div>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h3 className="text-sm font-semibold text-slate-900">
                  Obligaciones pendientes
                </h3>
                <p className="mt-1 text-xs text-slate-500">
                  Solo puedes seleccionar obligaciones completas de este titular.
                </p>
              </div>

              <button
                className={secondaryButton}
                disabled={isSaving}
                onClick={onSelectAll}
                type="button"
              >
                {selectedObligations.length === holderObligations.length
                  ? 'Quitar selección'
                  : 'Seleccionar todas'}
              </button>
            </div>

            <div className="mt-4 space-y-3">
              {holderObligations.map((obligation) => {
                const checked =
                  form.obligationIds.includes(obligation.id)

                return (
                  <label
                    className={`flex cursor-pointer items-start gap-3 rounded-md border p-4 transition-colors ${
                      checked
                        ? 'border-[#28727a] bg-[#eef6f5]'
                        : 'border-slate-200 bg-white hover:bg-slate-50'
                    }`}
                    key={obligation.id}
                  >
                    <input
                      checked={checked}
                      className="mt-1 h-4 w-4 accent-[#28727a]"
                      disabled={isSaving}
                      onChange={() =>
                        onToggleObligation(obligation.id)
                      }
                      type="checkbox"
                    />

                    <div className="min-w-0 flex-1">
                      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                        <div>
                          <p className="font-semibold text-slate-900">
                            {obligation.concepto}
                          </p>
                          <p className="mt-1 text-xs text-slate-500">
                            Obligación #{obligation.id}
                            {obligation.periodo
                              ? ` · Período ${obligation.periodo}`
                              : ''}
                          </p>
                          <p className="mt-1 text-xs text-slate-500">
                            Generada {formatGuatemalaDateTime(obligation.fechaGeneracion)}
                            {obligation.fechaVencimiento
                              ? ` · Vence ${formatCivilDate(obligation.fechaVencimiento)}`
                              : ''}
                          </p>
                        </div>

                        <p className="whitespace-nowrap font-semibold text-slate-800">
                          {formatMoney(obligation.monto)}
                        </p>
                      </div>

                      {obligation.esMorosa && (
                        <span className="mt-2 inline-flex rounded-full bg-red-50 px-2.5 py-1 text-xs font-semibold text-red-700">
                          Morosa
                        </span>
                      )}
                    </div>
                  </label>
                )
              })}
            </div>
          </div>
        )}

        <Field
          inputId="pago-concepto"
          label="Concepto"
        >
          <input
            className={inputClass}
            disabled={isSaving}
            id="pago-concepto"
            maxLength="200"
            onChange={onConceptChange}
            placeholder="Ej. Pago de cuotas pendientes"
            required
            value={form.concepto}
          />
        </Field>

        <div className="rounded-md border border-slate-200 bg-slate-50 p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Obligaciones seleccionadas
              </p>
              <p className="mt-1 text-sm text-slate-600">
                {selectedObligations.length}{' '}
                {selectedObligations.length === 1
                  ? 'obligación completa'
                  : 'obligaciones completas'}
              </p>
            </div>

            <div className="sm:text-right">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Total exacto
              </p>
              <p className="mt-1 text-2xl font-bold text-[#123b43]">
                {formatMoney(total)}
              </p>
            </div>
          </div>

          <p className="mt-3 border-t border-slate-200 pt-3 text-xs leading-5 text-slate-500">
            El monto se calcula automáticamente. No se admiten pagos parciales ni montos distintos al total seleccionado.
          </p>
        </div>

        {formError && (
          <Alert message={formError} />
        )}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
            disabled={isSaving}
            onClick={onCancel}
            type="button"
          >
            Volver
          </button>

          <button
            className="rounded-md bg-[#d6a85f] px-4 py-2 text-sm font-semibold text-[#123b43] disabled:cursor-not-allowed disabled:opacity-60"
            disabled={
              isSaving ||
              !selectedHolder ||
              selectedObligations.length === 0
            }
            type="submit"
          >
            Revisar pago
          </button>
        </div>
      </form>
    </Panel>
  )
}

function ConfirmationDialog({
  holder,
  isSaving,
  obligations,
  onCancel,
  onConfirm,
  total,
}) {
  return (
    <Dialog title="Confirmar pago">
      <p className="text-sm leading-6 text-slate-600">
        Confirma que el pago corresponde a{' '}
        <strong>{holder.name}</strong>.
      </p>

      <div className="mt-4 rounded-md bg-slate-50 p-4">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          Obligaciones
        </p>

        <div className="mt-3 space-y-2">
          {obligations.map((obligation) => (
            <div
              className="flex items-start justify-between gap-3 text-sm"
              key={obligation.id}
            >
              <span className="text-slate-600">
                {obligation.concepto}
              </span>
              <strong className="whitespace-nowrap text-slate-800">
                {formatMoney(obligation.monto)}
              </strong>
            </div>
          ))}
        </div>

        <div className="mt-4 flex items-end justify-between border-t border-slate-200 pt-4">
          <span className="text-sm font-semibold text-slate-700">
            Total
          </span>
          <span className="text-xl font-bold text-[#123b43]">
            {formatMoney(total)}
          </span>
        </div>
      </div>

      <p className="mt-4 text-xs leading-5 text-slate-500">
        Al confirmar, las obligaciones seleccionadas quedarán pagadas y el sistema generará un comprobante.
      </p>

      <div className="mt-6">
        <DialogActions
          confirmLabel="Registrar pago"
          isSaving={isSaving}
          onCancel={onCancel}
          onConfirm={onConfirm}
        />
      </div>
    </Dialog>
  )
}

function PaymentList({ isSaving, onAnnul, onOpen, pagos }) {
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISOS.PAGOS_GESTIONAR)
  return (
    <>
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[820px] text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="pb-3 pr-4 font-semibold">Comprobante</th>
              <th className="pb-3 pr-4 font-semibold">Fecha</th>
              <th className="pb-3 pr-4 font-semibold">Titular</th>
              <th className="pb-3 pr-4 font-semibold">Concepto</th>
              <th className="pb-3 pr-4 font-semibold">Monto</th>
              <th className="pb-3 text-right font-semibold">Acciones</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100">
            {pagos.map((payment) => (
              <tr key={payment.id}>
                <td className="py-4 pr-4">
                  <p className="font-semibold text-[#123b43]">
                    {payment.numeroComprobante}
                  </p>
                </td>

                <td className="py-4 pr-4 text-slate-600">
                  {formatGuatemalaDateTime(payment.fecha)}
                </td>

                <td className="py-4 pr-4">
                  <Holder payment={payment} />
                </td>

                <td className="max-w-60 py-4 pr-4 text-slate-600">
                  {payment.concepto}
                </td>

                <td className="whitespace-nowrap py-4 pr-4 font-semibold text-slate-800">
                  {formatMoney(payment.monto)}
                </td>

                <td className="py-4 text-right">
                  <div className="flex flex-wrap justify-end gap-2">
                    <button
                      className={secondaryButton}
                      onClick={() => onOpen(payment.id)}
                      type="button"
                    >
                      Ver detalle
                    </button>

                    {canManage && isRegisteredPayment(payment.estado) && (
                      <button
                        className={dangerButton}
                        disabled={isSaving}
                        onClick={() => onAnnul(payment)}
                        type="button"
                      >
                        Anular
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="space-y-3 md:hidden">
        {pagos.map((payment) => (
          <article
            className="rounded-md border border-slate-200 p-4"
            key={payment.id}
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="font-semibold text-[#123b43]">
                  {payment.numeroComprobante}
                </p>
                <p className="mt-1 text-sm text-slate-500">
                  {formatGuatemalaDateTime(payment.fecha)}
                </p>
              </div>

              <PaymentStatus estado={payment.estado} />
            </div>

            <div className="mt-3">
              <Holder payment={payment} />
            </div>

            <dl className="mt-4 grid grid-cols-2 gap-3 rounded-md bg-slate-50 p-3 text-sm">
              <Data
                label="Monto"
                strong
                value={formatMoney(payment.monto)}
              />
              <Data
                label="Concepto"
                value={payment.concepto}
              />
            </dl>

            <div className="mt-4 flex flex-wrap justify-end gap-2 border-t border-slate-100 pt-3">
              <button
                className={secondaryButton}
                onClick={() => onOpen(payment.id)}
                type="button"
              >
                Ver detalle
              </button>

              {canManage && isRegisteredPayment(payment.estado) && (
                <button
                  className={dangerButton}
                  disabled={isSaving}
                  onClick={() => onAnnul(payment)}
                  type="button"
                >
                  Anular
                </button>
              )}
            </div>
          </article>
        ))}
      </div>
    </>
  )
}

function PaymentAnnulmentDialog({
  isSaving,
  onCancel,
  onConfirm,
  payment,
}) {
  return (
    <Dialog title="¿Deseas anular este pago?">
      <p className="text-sm leading-6 text-slate-600">
        El pago permanecerá en el historial. Las obligaciones
        asociadas volverán a estar pendientes y este pago dejará
        de contabilizarse como ingreso financiero.
      </p>

      <dl className="mt-4 space-y-2 rounded-md bg-slate-50 p-4 text-sm">
        <div>
          <dt className="inline font-medium text-slate-500">
            Comprobante:{' '}
          </dt>
          <dd className="inline font-semibold text-slate-800">
            {payment.numeroComprobante}
          </dd>
        </div>

        <div>
          <dt className="inline font-medium text-slate-500">
            Titular:{' '}
          </dt>
          <dd className="inline text-slate-700">
            {payment.titular.nombre}
          </dd>
        </div>

        <div>
          <dt className="inline font-medium text-slate-500">
            Monto:{' '}
          </dt>
          <dd className="inline font-semibold text-slate-800">
            {formatMoney(payment.monto)}
          </dd>
        </div>
      </dl>

      <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button
          className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
          disabled={isSaving}
          onClick={onCancel}
          type="button"
        >
          Volver
        </button>

        <button
          className="rounded-md bg-red-700 px-4 py-2 text-sm font-semibold text-white hover:bg-red-800 disabled:cursor-not-allowed disabled:opacity-60"
          disabled={isSaving}
          onClick={onConfirm}
          type="button"
        >
          {isSaving ? 'Anulando...' : 'Confirmar anulación'}
        </button>
      </div>
    </Dialog>
  )
}
function PaymentDetail({ payment, receipt }) {
  return (
    <Panel>
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-lg font-semibold text-slate-900">
              {payment.numeroComprobante}
            </h2>
            <PaymentStatus estado={payment.estado} />
          </div>

          <p className="mt-2 text-sm text-slate-600">
            {payment.concepto}
          </p>

          <p className="mt-2 text-xs text-slate-500">
            Registrado {formatGuatemalaDateTime(payment.fecha)}
          </p>
        </div>

        <div className="rounded-md bg-[#eef6f5] px-4 py-3 text-right">
          <p className="text-xs font-semibold uppercase tracking-wide text-[#28727a]">
            Total pagado
          </p>
          <p className="mt-1 text-xl font-bold text-[#123b43]">
            {formatMoney(payment.monto)}
          </p>
        </div>
      </div>

      <div className="mt-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <DataCard
          label="Titular"
          value={payment.titular.nombre}
        />
        <DataCard
          label="Tipo"
          value={payment.titular.tipo}
        />
        <DataCard
          label="NIS"
          value={payment.titular.nis || 'No aplica'}
        />
        <DataCard
          label="Registrado por"
          value={payment.usuarioAdministrativo}
        />
        <DataCard
          label="Obligaciones"
          value={payment.obligaciones.length}
        />
      </div>

      <div className="mt-7">
        <h3 className="text-base font-semibold text-slate-900">
          Obligaciones canceladas
        </h3>

        <div className="mt-4 space-y-3">
          {payment.obligaciones.map((obligation) => (
            <article
              className="rounded-md border border-slate-200 p-4"
              key={obligation.id}
            >
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="font-semibold text-slate-900">
                    {obligation.concepto}
                  </p>
                  <p className="mt-1 text-xs text-slate-500">
                    Obligación #{obligation.id}
                    {obligation.periodo
                      ? ` · Período ${obligation.periodo}`
                      : ''}
                  </p>
                </div>

                <p className="font-semibold text-slate-800">
                  {formatMoney(obligation.monto)}
                </p>
              </div>
            </article>
          ))}
        </div>
      </div>

      {receipt && (
        <Receipt receipt={receipt} />
      )}
    </Panel>
  )
}

function Receipt({ receipt }) {
  const printRoot =
    document.getElementById('payment-print-root')

  function printReceipt() {
    window.print()
  }

  return (
    <>
      <ReceiptContent
        receipt={receipt}
        showPrintAction
        onPrint={printReceipt}
      />

      {printRoot &&
        createPortal(
          <ReceiptContent
            printCopy
            receipt={receipt}
          />,
          printRoot,
        )}
    </>
  )
}
function ReceiptContent({
  onPrint,
  printCopy = false,
  receipt,
  showPrintAction = false,
}) {
  return (
    <section
      aria-hidden={printCopy ? 'true' : undefined}
      className={`payment-receipt ${
        printCopy
          ? 'payment-receipt-print'
          : 'payment-receipt-screen mt-8'
      } overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm`}
    >
      <div className="border-b border-slate-200 bg-[#123b43] px-5 py-5 text-white sm:px-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-lg font-semibold tracking-tight">
              Panyebar
            </p>
            <p className="mt-1 text-xs text-teal-100/75">
              Comité de Agua Potable
            </p>
          </div>

          <div className="sm:text-right">
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-teal-100/70">
              Comprobante
            </p>
            <p className="mt-1 text-xl font-bold">
              {receipt.numero}
            </p>
          </div>
        </div>
      </div>

      <div className="p-5 sm:p-6">
        <div className="flex flex-col gap-4 border-b border-slate-200 pb-5 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[#28727a]">
              Comprobante de pago
            </p>
            <h3 className="mt-1 text-lg font-semibold text-slate-900">
              {receipt.titular.nombre}
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              {receipt.titular.nis
                ? `${receipt.titular.nis} · ${receipt.titular.tipo}`
                : receipt.titular.tipo}
            </p>
          </div>

          <PaymentStatus estado={receipt.estado} />
        </div>

        <dl className="mt-5 grid gap-x-6 gap-y-4 sm:grid-cols-2">
          <ReceiptData
            label="Fecha"
            value={formatGuatemalaDateTime(receipt.fecha)}
          />
          <ReceiptData
            label="Registrado por"
            value={receipt.usuarioAdministrativo}
          />
          <ReceiptData
            label="Concepto"
            value={receipt.concepto}
          />
          <ReceiptData
            label="NIS"
            value={receipt.titular.nis || 'No aplica'}
          />
        </dl>

        <div className="mt-7">
          <h4 className="text-sm font-semibold text-slate-900">
            Obligaciones canceladas
          </h4>

          <div className="mt-3 overflow-hidden rounded-md border border-slate-200">
            <div className="hidden grid-cols-[1fr_130px_130px] gap-4 border-b border-slate-200 bg-slate-50 px-4 py-2.5 text-xs font-semibold uppercase tracking-wide text-slate-500 sm:grid">
              <span>Concepto</span>
              <span>Período</span>
              <span className="text-right">Monto</span>
            </div>

            <div className="divide-y divide-slate-100">
              {receipt.obligaciones.map((obligation) => (
                <div
                  className="grid gap-2 px-4 py-3 text-sm sm:grid-cols-[1fr_130px_130px] sm:items-center sm:gap-4"
                  key={obligation.id}
                >
                  <div>
                    <p className="font-medium text-slate-800">
                      {obligation.concepto}
                    </p>
                    <p className="mt-0.5 text-xs text-slate-500 sm:hidden">
                      {obligation.periodo
                        ? `Período ${obligation.periodo}`
                        : 'Sin período'}
                    </p>
                  </div>

                  <span className="hidden text-slate-600 sm:block">
                    {obligation.periodo || '—'}
                  </span>

                  <span className="font-semibold text-slate-800 sm:text-right">
                    {formatMoney(obligation.monto)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-6 flex flex-col gap-4 border-t-2 border-[#123b43] pt-5 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Estado
            </p>
            <p className="mt-1 text-sm font-semibold text-[#17644e]">
              {formatPaymentStatus(receipt.estado)}
            </p>
          </div>

          <div className="sm:text-right">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Total pagado
            </p>
            <p className="mt-1 text-2xl font-bold text-[#123b43]">
              {formatMoney(receipt.total)}
            </p>
          </div>
        </div>

        {showPrintAction && (
          <div className="receipt-actions mt-6 flex justify-end border-t border-slate-100 pt-5">
            <button
              className="rounded-md border border-[#123b43] px-4 py-2 text-sm font-semibold text-[#123b43] hover:bg-[#eef6f5] focus:outline-none focus:ring-2 focus:ring-[#28727a]"
              onClick={onPrint}
              type="button"
            >
              Imprimir comprobante
            </button>
          </div>
        )}

        <p className="mt-5 text-center text-[11px] leading-5 text-slate-400">
          Comprobante generado por el sistema administrativo del Comité de Agua Potable de Panyebar.
        </p>
      </div>
    </section>
  )
}
function ReceiptData({ label, value }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">
        {label}
      </dt>
      <dd className="mt-1 break-words text-sm font-semibold text-slate-800">
        {value}
      </dd>
    </div>
  )
}
function Holder({ payment }) {
  return (
    <div>
      <p className="font-medium text-slate-800">
        {payment.titular.nombre}
      </p>
      <p className="mt-1 text-xs text-slate-500">
        {payment.titular.nis
          ? `${payment.titular.nis} · ${payment.titular.tipo}`
          : payment.titular.tipo}
      </p>
    </div>
  )
}

function PaymentStatus({ estado }) {
  const registered =
    estado === 1 ||
    String(estado).toLowerCase() === 'registrado'

  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${
        registered
          ? 'bg-[#e3f2ed] text-[#17644e]'
          : 'bg-slate-100 text-slate-600'
      }`}
    >
      {registered ? 'Registrado' : formatPaymentStatus(estado)}
    </span>
  )
}

function Field({ children, inputId, label }) {
  return (
    <div>
      <label
        className="text-sm font-medium text-slate-700"
        htmlFor={inputId}
      >
        {label}
      </label>
      {children}
    </div>
  )
}

function Data({ label, strong = false, value }) {
  return (
    <div>
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd
        className={`mt-1 ${
          strong
            ? 'font-semibold text-slate-800'
            : 'text-slate-700'
        }`}
      >
        {value}
      </dd>
    </div>
  )
}

function DataCard({ label, value }) {
  return (
    <div className="rounded-md bg-slate-50 p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
        {label}
      </p>
      <p className="mt-1 break-words text-sm font-semibold text-slate-800">
        {value}
      </p>
    </div>
  )
}

function Dialog({ children, title }) {
  return (
    <div
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/40 p-4"
      role="dialog"
    >
      <div className="my-4 max-h-[calc(100vh-2rem)] w-full max-w-lg overflow-y-auto rounded-lg bg-white p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-slate-900">
          {title}
        </h2>
        <div className="mt-3">
          {children}
        </div>
      </div>
    </div>
  )
}

function DialogActions({
  confirmLabel,
  isSaving,
  onCancel,
  onConfirm,
}) {
  return (
    <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
      <button
        className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
        disabled={isSaving}
        onClick={onCancel}
        type="button"
      >
        Volver
      </button>

      <button
        className="rounded-md bg-[#123b43] px-4 py-2 text-sm font-semibold text-white hover:bg-[#0d2d33] disabled:cursor-not-allowed disabled:opacity-60"
        disabled={isSaving}
        onClick={onConfirm}
        type="button"
      >
        {isSaving ? 'Registrando...' : confirmLabel}
      </button>
    </div>
  )
}

function Alert({ message, onDismiss }) {
  return (
    <div
      aria-live="polite"
      className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
    >
      <span>{message}</span>

      {onDismiss && (
        <button
          className="shrink-0 font-semibold text-red-700"
          onClick={onDismiss}
          type="button"
        >
          Cerrar
        </button>
      )}
    </div>
  )
}

function SuccessAlert({ message }) {
  return (
    <div
      aria-live="polite"
      className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800"
    >
      {message}
    </div>
  )
}

function InfoAlert({ message }) {
  return (
    <div className="rounded-md border border-slate-200 bg-white px-4 py-3 text-sm text-slate-600">
      {message}
    </div>
  )
}

function buildHolders(
  obligations,
  personaById,
  supplyById,
) {
  const grouped = new Map()

  for (const obligation of obligations) {
    let holder

    if (obligation.suministroId) {
      const supply = supplyById.get(obligation.suministroId)
      const responsible = supply?.responsableActual
      const name = responsible
        ? `${responsible.nombres} ${responsible.apellidos}`.trim()
        : `Suministro ${supply?.nis || `#${obligation.suministroId}`}`

      holder = {
        key: `suministro:${obligation.suministroId}`,
        type: 'Suministro',
        id: obligation.suministroId,
        name,
        nis: supply?.nis || null,
        description: supply
          ? `${supply.nis} · ${supply.sectorNombre}`
          : `Suministro #${obligation.suministroId}`,
        optionLabel: supply
          ? `${supply.nis} · ${name}`
          : `Suministro #${obligation.suministroId}`,
      }
    } else if (obligation.personaId) {
      const person = personaById.get(obligation.personaId)
      const name = person
        ? `${person.nombres} ${person.apellidos}`.trim()
        : `Persona #${obligation.personaId}`

      holder = {
        key: `persona:${obligation.personaId}`,
        type: 'Persona',
        id: obligation.personaId,
        name,
        nis: null,
        description: 'Obligación personal',
        optionLabel: `${name} · Obligación personal`,
      }
    } else {
      continue
    }

    const existing = grouped.get(holder.key)

    if (existing) {
      existing.obligations.push(obligation)
    } else {
      grouped.set(holder.key, {
        ...holder,
        obligations: [obligation],
      })
    }
  }

  return Array.from(grouped.values())
    .map((holder) => ({
      ...holder,
      obligations: [...holder.obligations].sort(
        compareObligations,
      ),
    }))
    .sort((first, second) =>
      first.optionLabel.localeCompare(
        second.optionLabel,
        'es',
        { sensitivity: 'base' },
      ),
    )
}

function compareObligations(first, second) {
  return (
    String(first.fechaVencimiento || '9999')
      .localeCompare(
        String(second.fechaVencimiento || '9999'),
      ) ||
    first.id - second.id
  )
}

function toPaymentSummary(payment) {
  return {
    id: payment.id,
    numeroComprobante: payment.numeroComprobante,
    monto: payment.monto,
    fecha: payment.fecha,
    concepto: payment.concepto,
    estado: payment.estado,
    usuarioAdministrativoId:
      payment.usuarioAdministrativoId,
    titular: payment.titular,
  }
}

const inputClass =
  'mt-1.5 block min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 py-2.5 text-sm font-normal text-slate-900 outline-none focus:border-[#28727a] focus:ring-2 focus:ring-[#28727a]/20 disabled:cursor-not-allowed disabled:bg-slate-100'

const dangerButton =
  'rounded-md border border-red-200 px-3 py-2 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-60'

function isRegisteredPayment(value) {
  return (
    value === 1 ||
    String(value).toLowerCase() === 'registrado'
  )
}
const secondaryButton =
  'rounded-md border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60'

function formatPaymentStatus(value) {
  if (
    value === 1 ||
    String(value).toLowerCase() === 'registrado'
  ) {
    return 'Registrado'
  }

  if (
    value === 2 ||
    String(value).toLowerCase() === 'anulado'
  ) {
    return 'Anulado'
  }

  return String(value || 'Desconocido')
}
function formatMoney(value) {
  return new Intl.NumberFormat('es-GT', {
    style: 'currency',
    currency: 'GTQ',
  }).format(Number(value || 0))
}

function getRequestMessage(error, fallback) {
  if (error?.status === 400 || error?.status === 409) {
    return error.message || fallback
  }

  if (error?.status === 403) {
    return 'No tienes permiso para realizar esta operación.'
  }

  if (error?.status === 404) {
    return error.message || 'El recurso solicitado ya no existe.'
  }

  if (error?.kind === 'network') {
    return 'No se pudo conectar con el servidor.'
  }

  if (error?.status >= 500) {
    return 'Ocurrió un error en el servidor. Intenta nuevamente.'
  }

  return fallback
}

export default PagosPage
