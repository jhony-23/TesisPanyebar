using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Suministros;

namespace Panyebar.Infrastructure.Persistence;

public sealed class SuministroNisGenerator : ISuministroNisGenerator
{
    private const string SequenceQuery = "SELECT NEXT VALUE FOR [dbo].[SuministroNisSequence]";
    private readonly PanyebarDbContext _dbContext;

    public SuministroNisGenerator(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State == ConnectionState.Closed;

        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = SequenceQuery;
            command.CommandType = CommandType.Text;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException("La secuencia NIS no devolvió un valor.");
            }

            long nextValue;
            try
            {
                nextValue = Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException("La secuencia NIS devolvió un valor inválido.", exception);
            }
            catch (InvalidCastException exception)
            {
                throw new InvalidOperationException("La secuencia NIS devolvió un valor inválido.", exception);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException("La secuencia NIS devolvió un valor fuera de rango.", exception);
            }

            return NisFormatter.Format(nextValue);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}