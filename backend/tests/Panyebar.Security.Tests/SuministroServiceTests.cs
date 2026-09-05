using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public class SuministroServiceTests
{
    [Fact]
    public async Task CreateAsync_GeneratesAndKeepsNisAndQrToken()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000321", "token-creado");

        var result = await service.CreateAsync(new SuministroInput(1, " Calle Principal "));

        Assert.True(result.Succeeded);
        Assert.Equal("PAN-000321", result.Value!.Nis);
        Assert.Equal("token-creado", await dbContext.Suministros.Select(s => s.CodigoQrToken).SingleAsync());
        Assert.Equal("Calle Principal", result.Value.DireccionReferencia);
        Assert.Equal(EstadoSuministro.Activo, result.Value.Estado);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotAlterNisOrQrToken()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.AddRange(
            new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo },
            new Sector { Id = 2, Nombre = "Norte", Estado = EstadoRegistro.Activo });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token-original",
            DireccionReferencia = "Anterior"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-999999", "token-nuevo-no-usado");

        var result = await service.UpdateAsync(10, new SuministroInput(2, "Nueva dirección"));

        Assert.True(result.Succeeded);
        Assert.Equal("PAN-000010", result.Value!.Nis);
        Assert.Equal("token-original", await dbContext.Suministros.Select(s => s.CodigoQrToken).SingleAsync());
        Assert.Equal(2, result.Value.SectorId);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingOrInactiveSector()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Inactivo", Estado = EstadoRegistro.Inactivo });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000001", "token");

        var missing = await service.CreateAsync(new SuministroInput(99, "Dirección"));
        var inactive = await service.CreateAsync(new SuministroInput(1, "Dirección"));

        Assert.Equal(SuministroOperationError.Invalid, missing.Error);
        Assert.Equal(SuministroOperationError.Invalid, inactive.Error);
        Assert.Empty(await dbContext.Suministros.ToListAsync());
    }

    [Fact]
    public async Task GetByNisAsync_TrimsNisAndReturnsCurrentResponsible()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Personas.Add(new Persona { Id = 4, Nombres = "Ana", Apellidos = "Pérez" });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token",
            DireccionReferencia = "Dirección"
        });
        dbContext.PersonaSuministros.Add(new PersonaSuministro
        {
            PersonaId = 4,
            SuministroId = 10,
            FechaInicio = DateTime.UtcNow,
            Estado = EstadoRelacionSuministro.Vigente
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000011", "token-2");

        var result = await service.GetByNisAsync(" PAN-000010 ");

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
        Assert.Equal(4, result.ResponsableActual!.PersonaId);
        Assert.Equal("Ana", result.ResponsableActual.Nombres);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByNisThenIdAndAllowsMissingResponsible()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Suministros.AddRange(
            new Suministro { Id = 2, SectorId = 1, Nis = "PAN-000002", CodigoQrToken = "token-2", DireccionReferencia = "Dos" },
            new Suministro { Id = 1, SectorId = 1, Nis = "PAN-000001", CodigoQrToken = "token-1", DireccionReferencia = "Uno" });
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext, "PAN-000003", "token-3").GetAllAsync();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("PAN-000001", first.Nis);
                Assert.Null(first.ResponsableActual);
            },
            second => Assert.Equal("PAN-000002", second.Nis));
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PanyebarDbContext(options);
    }

    private static SuministroService CreateService(
        PanyebarDbContext dbContext,
        string nis,
        string token)
    {
        return new SuministroService(
            dbContext,
            new FixedNisGenerator(nis),
            new FixedQrTokenGenerator(token));
    }

    private sealed class FixedNisGenerator : ISuministroNisGenerator
    {
        private readonly string _nis;

        public FixedNisGenerator(string nis)
        {
            _nis = nis;
        }

        public Task<string> GenerateAsync(CancellationToken cancellationToken = default) => Task.FromResult(_nis);
    }

    private sealed class FixedQrTokenGenerator : ISuministroQrTokenGenerator
    {
        private readonly string _token;

        public FixedQrTokenGenerator(string token)
        {
            _token = token;
        }

        public string Generate() => _token;
    }
}