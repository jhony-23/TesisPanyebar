using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Obligaciones;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class PersonalObligacionServiceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Generates_independent_personal_obligations_and_audits(int count)
    {
        await using var db = CreateContext();
        Seed(db);
        var ids = Enumerable.Range(1, count).ToArray();
        var due = new DateTime(2025, 1, 1);
        var before = DateTime.UtcNow;
        var result = await new ObligacionService(db).GeneratePersonalAsync(
            new(ids, "  Aporte  ", 25.50m, "  Asamblea 2026  ", due), 1);
        Assert.True(result.Succeeded);
        Assert.Equal(count, result.Value!.Count);
        Assert.Equal(count, result.Value.Select(x => x.Id).Distinct().Count());
        Assert.Equal(ids, result.Value.Select(x => x.PersonaId!.Value).OrderBy(x => x));
        db.ChangeTracker.Clear();
        var saved = await db.Obligaciones.OrderBy(x => x.PersonaId).ToListAsync();
        Assert.Equal(count, saved.Count);
        foreach (var obligation in saved)
        {
            Assert.Null(obligation.SuministroId);
            Assert.Null(obligation.CuotaId);
            Assert.Equal(OrigenObligacion.Administrativa, obligation.Origen);
            Assert.Equal(EstadoObligacion.Pendiente, obligation.Estado);
            Assert.Equal("Aporte", obligation.Concepto);
            Assert.Equal(25.50m, obligation.Monto);
            Assert.Equal("Asamblea 2026", obligation.Periodo);
            Assert.Equal(due, obligation.FechaVencimiento);
            Assert.InRange(obligation.FechaGeneracion, before, DateTime.UtcNow);
            var audit = await db.Auditorias.SingleAsync(a => a.EntidadId == obligation.Id);
            Assert.Equal(1, audit.UsuarioAdministrativoId);
            Assert.Equal("OBLIGACION.GENERAR.PERSONAL", audit.Accion);
            Assert.Equal("Obligacion", audit.Entidad);
            Assert.Equal(obligation.FechaGeneracion, audit.Fecha);
            Assert.Contains($"PersonaId:{obligation.PersonaId}", audit.ValorNuevo);
            Assert.Contains($"CantidadOperacion:{count}", audit.ValorNuevo);
        }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("nonpositive-id")]
    [InlineData("missing")]
    [InlineData("inactive")]
    [InlineData("concept-empty")]
    [InlineData("concept-long")]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("precision")]
    [InlineData("overflow")]
    [InlineData("period-long")]
    [InlineData("user")]
    public async Task Invalid_batch_creates_zero_obligations_and_zero_audits(string scenario)
    {
        await using var db = CreateContext();
        Seed(db);
        var input = new GenerarObligacionesPersonalesInput(new[] { 1, 2, 3 }, "Aporte", 20m, null, null);
        input = scenario switch
        {
            "null" => input with { PersonaIds = null },
            "empty" => input with { PersonaIds = Array.Empty<int>() },
            "duplicate" => input with { PersonaIds = new[] { 1, 1 } },
            "nonpositive-id" => input with { PersonaIds = new[] { 1, 0 } },
            "missing" => input with { PersonaIds = new[] { 1, 2, 999 } },
            "concept-empty" => input with { Concepto = "  " },
            "concept-long" => input with { Concepto = new string('a', 201) },
            "zero" => input with { Monto = 0 },
            "negative" => input with { Monto = -1 },
            "precision" => input with { Monto = 1.001m },
            "overflow" => input with { Monto = 10000000000000000m },
            "period-long" => input with { Periodo = new string('a', 21) },
            _ => input
        };
        if (scenario == "inactive")
        {
            (await db.Personas.FindAsync(3))!.Estado = EstadoRegistro.Inactivo;
            await db.SaveChangesAsync();
        }
        var result = await new ObligacionService(db).GeneratePersonalAsync(input, scenario == "user" ? 999 : 1);
        Assert.Equal(scenario == "missing" ? ObligacionOperationError.NotFound : ObligacionOperationError.Invalid, result.Error);
        Assert.Empty(await db.Obligaciones.ToListAsync());
        Assert.Empty(await db.Auditorias.ToListAsync());
        Assert.DoesNotContain(db.ChangeTracker.Entries<Obligacion>(), e => e.State == EntityState.Added);
    }

    [Fact]
    public async Task Optional_fields_and_similar_legitimate_obligations_are_allowed()
    {
        await using var db = CreateContext();
        Seed(db);
        var service = new ObligacionService(db);
        var input = new GenerarObligacionesPersonalesInput(new[] { 1 }, "Aporte", 10, "  ", null);
        Assert.True((await service.GeneratePersonalAsync(input, 1)).Succeeded);
        Assert.True((await service.GeneratePersonalAsync(input, 1)).Succeeded);
        Assert.Equal(2, await db.Obligaciones.CountAsync());
        Assert.All(await db.Obligaciones.ToListAsync(), o => { Assert.Null(o.Periodo); Assert.Null(o.FechaVencimiento); });
    }

    private static void Seed(PanyebarDbContext db)
    {
        db.UsuariosAdministrativos.Add(new() { Id = 1, NombreUsuario = "admin", PasswordHash = "test" });
        db.Personas.AddRange(Enumerable.Range(1, 3).Select(id => new Persona { Id = id, Nombres = "Persona", Apellidos = id.ToString() }));
        db.SaveChanges();
    }

    private static PanyebarDbContext CreateContext() => new(new DbContextOptionsBuilder<PanyebarDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
