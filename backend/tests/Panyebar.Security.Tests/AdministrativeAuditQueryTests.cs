using Microsoft.EntityFrameworkCore;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class AdministrativeAuditQueryTests
{
    [Fact]
    public async Task Audit_query_returns_newest_first_with_user_name()
    {
        await using var db = CreateContext();

        db.UsuariosAdministrativos.Add(
            new UsuarioAdministrativo
            {
                Id = 1,
                NombreUsuario = "demo.admin",
                PasswordHash = "test",
                Estado = EstadoRegistro.Activo
            });

        db.Auditorias.AddRange(
            new Auditoria
            {
                Id = 1,
                UsuarioAdministrativoId = 1,
                Accion = "PERSONA.CREAR",
                Entidad = "Persona",
                EntidadId = 10,
                Fecha = new DateTime(
                    2026, 9, 17, 12, 0, 0,
                    DateTimeKind.Utc),
                ValorNuevo = "PersonaId:10"
            },
            new Auditoria
            {
                Id = 2,
                UsuarioAdministrativoId = 1,
                Accion = "PAGO.REGISTRAR",
                Entidad = "Pago",
                EntidadId = 20,
                Fecha = new DateTime(
                    2026, 9, 17, 13, 0, 0,
                    DateTimeKind.Utc),
                ValorNuevo = "Monto:50.00"
            });

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetAuditoriaAsync(
            null, null, null, null, null);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal("demo.admin", result[0].NombreUsuario);
        Assert.Equal("PAGO.REGISTRAR", result[0].Accion);
        Assert.Equal(1, result[0].UsuarioAdministrativoId);
    }

    [Fact]
    public async Task Audit_query_applies_filters_and_limit()
    {
        await using var db = CreateContext();

        db.UsuariosAdministrativos.AddRange(
            new UsuarioAdministrativo
            {
                Id = 1,
                NombreUsuario = "uno",
                PasswordHash = "test",
                Estado = EstadoRegistro.Activo
            },
            new UsuarioAdministrativo
            {
                Id = 2,
                NombreUsuario = "dos",
                PasswordHash = "test",
                Estado = EstadoRegistro.Activo
            });

        db.Auditorias.AddRange(
            Audit(
                1, 1, "PAGO.REGISTRAR", "Pago",
                new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc)),
            Audit(
                2, 2, "PAGO.ANULAR", "Pago",
                new DateTime(2026, 9, 17, 11, 0, 0, DateTimeKind.Utc)),
            Audit(
                3, 2, "PERSONA.CREAR", "Persona",
                new DateTime(2026, 9, 18, 11, 0, 0, DateTimeKind.Utc)));

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetAuditoriaAsync(
            new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc),
            2,
            "PAGO",
            "Pago",
            1);

        var row = Assert.Single(result);

        Assert.Equal(2, row.Id);
        Assert.Equal("dos", row.NombreUsuario);
        Assert.Equal("PAGO.ANULAR", row.Accion);
    }

    [Fact]
    public async Task Audit_query_is_read_only()
    {
        await using var db = CreateContext();

        db.UsuariosAdministrativos.Add(
            new UsuarioAdministrativo
            {
                Id = 1,
                NombreUsuario = "demo.admin",
                PasswordHash = "test",
                Estado = EstadoRegistro.Activo
            });

        db.Auditorias.Add(
            Audit(
                1, 1, "TEST.ACCION", "Test",
                DateTime.UtcNow));

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = CreateService(db);

        _ = await service.GetAuditoriaAsync(
            null, null, null, null, null);

        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Single(db.Auditorias);
    }

    private static Auditoria Audit(
        int id,
        int userId,
        string action,
        string entity,
        DateTime date) =>
        new()
        {
            Id = id,
            UsuarioAdministrativoId = userId,
            Accion = action,
            Entidad = entity,
            EntidadId = id,
            Fecha = date
        };

    private static AdministrativeAccessService CreateService(
        PanyebarDbContext db) =>
        new(
            db,
            new PasswordHasherAdapter());

    private static PanyebarDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PanyebarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new PanyebarDbContext(options);
    }
}
