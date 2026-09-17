namespace Panyebar.Application.Pagos;

public interface IPagoService
{
    Task<IReadOnlyList<PagoDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagoDetalleDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagoOperationResult<PagoDetalleDto>> RegisterAsync(
        RegistrarPagoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<ComprobantePagoDto?> GetComprobanteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagoOperationResult<PagoDetalleDto>> AnnulAsync(
        int id,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
}
