namespace Panyebar.Application.Pagos;

public enum PagoOperationError
{
    None = 0,
    Invalid = 1,
    NotFound = 2,
    Conflict = 3
}

public sealed record PagoOperationResult<T>(
    T? Value,
    PagoOperationError Error)
{
    public bool Succeeded => Error == PagoOperationError.None;

    public static PagoOperationResult<T> Success(T value) =>
        new(value, PagoOperationError.None);

    public static PagoOperationResult<T> Failure(PagoOperationError error) =>
        new(default, error);
}
