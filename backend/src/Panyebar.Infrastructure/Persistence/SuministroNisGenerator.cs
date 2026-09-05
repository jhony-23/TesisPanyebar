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
        var nextValue = await _dbContext.Database
            .SqlQueryRaw<long>(SequenceQuery)
            .SingleAsync(cancellationToken);

        return NisFormatter.Format(nextValue);
    }
}