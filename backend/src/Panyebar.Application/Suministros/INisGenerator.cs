namespace Panyebar.Application.Suministros;

public interface ISuministroNisGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}