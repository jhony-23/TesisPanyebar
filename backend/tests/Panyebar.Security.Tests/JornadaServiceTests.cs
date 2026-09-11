using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Jornadas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class JornadaServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPlanificadaAndAudits()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var service = new JornadaService(context);

        var result = await service.CreateAsync(Input(), user.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoJornada.Planificada, result.Value!.Estado);
        Assert.Equal("Jornada comunitaria", result.Value.Nombre);

        var audit = await context.Auditorias.SingleAsync();
        Assert.Equal("JORNADA.CREAR", audit.Accion);
        Assert.Equal(user.Id, audit.UsuarioAdministrativoId);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidContract()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var service = new JornadaService(context);

        var emptyName = await service.CreateAsync(
            Input() with { Nombre = " " }, user.Id);

        var invalidSchedule = await service.CreateAsync(
            Input() with
            {
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(9, 0)
            }, user.Id);

        var invalidAmount = await service.CreateAsync(
            Input() with { MontoIncumplimiento = 0 }, user.Id);

        Assert.Equal(JornadaOperationError.Invalid, emptyName.Error);
        Assert.Equal(JornadaOperationError.Invalid, invalidSchedule.Error);
        Assert.Equal(JornadaOperationError.Invalid, invalidAmount.Error);
        Assert.Empty(context.Jornadas);
    }

    [Fact]
    public async Task UpdateAsync_OnlyAllowsPlanificada()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context);
        var service = new JornadaService(context);

        var updated = await service.UpdateAsync(
            jornada.Id,
            Input() with { Nombre = "Actualizada" },
            user.Id);

        Assert.True(updated.Succeeded);
        Assert.Equal("Actualizada", updated.Value!.Nombre);

        jornada.Estado = EstadoJornada.Cerrada;
        await context.SaveChangesAsync();

        var rejected = await service.UpdateAsync(
            jornada.Id,
            Input() with { Nombre = "No permitida" },
            user.Id);

        Assert.Equal(JornadaOperationError.Conflict, rejected.Error);
    }

    [Fact]
    public async Task CancelAsync_RequiresReasonAndOnlyPlanificada()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context);
        var service = new JornadaService(context);

        var invalid = await service.CancelAsync(
            jornada.Id,
            new CancelarJornadaInput(" "),
            user.Id);

        Assert.Equal(JornadaOperationError.Invalid, invalid.Error);

        var cancelled = await service.CancelAsync(
            jornada.Id,
            new CancelarJornadaInput("Clima"),
            user.Id);

        Assert.True(cancelled.Succeeded);
        Assert.Equal(EstadoJornada.Cancelada, cancelled.Value!.Estado);

        var second = await service.CancelAsync(
            jornada.Id,
            new CancelarJornadaInput("Otro"),
            user.Id);

        Assert.Equal(JornadaOperationError.Conflict, second.Error);
        Assert.Contains(
            await context.Auditorias.ToListAsync(),
            a => a.Accion == "JORNADA.CANCELAR");
    }

    [Fact]
    public async Task AddParticipantsAsync_AddsOnlyActivePersonsAndPreventsDuplicates()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context);
        var active = AddPersona(context, 10, EstadoRegistro.Activo);
        var inactive = AddPersona(context, 11, EstadoRegistro.Inactivo);
        var service = new JornadaService(context);

        var added = await service.AddParticipantsAsync(
            jornada.Id,
            new AgregarParticipantesJornadaInput(new[] { active.Id }),
            user.Id);

        Assert.True(added.Succeeded);
        var participation = await context.ParticipacionesJornada.SingleAsync();
        Assert.Equal(ResultadoParticipacionJornada.Pendiente, participation.Resultado);

        var duplicate = await service.AddParticipantsAsync(
            jornada.Id,
            new AgregarParticipantesJornadaInput(new[] { active.Id }),
            user.Id);

        Assert.Equal(JornadaOperationError.Conflict, duplicate.Error);

        var inactiveResult = await service.AddParticipantsAsync(
            jornada.Id,
            new AgregarParticipantesJornadaInput(new[] { inactive.Id }),
            user.Id);

        Assert.Equal(JornadaOperationError.NotFound, inactiveResult.Error);
    }

    [Fact]
    public async Task AddParticipantsAsync_RejectsDuplicateIdsInsideBatch()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);
        var service = new JornadaService(context);

        var result = await service.AddParticipantsAsync(
            jornada.Id,
            new AgregarParticipantesJornadaInput(
                new[] { person.Id, person.Id }),
            user.Id);

        Assert.Equal(JornadaOperationError.Invalid, result.Error);
        Assert.Empty(context.ParticipacionesJornada);
    }

    [Fact]
    public async Task UpdateParticipantAsync_AllowsCorrectionOnlyWhilePlanificada()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);
        var participation = AddParticipation(context, jornada, person);
        var service = new JornadaService(context);

        var updated = await service.UpdateParticipantAsync(
            jornada.Id,
            person.Id,
            new ActualizarParticipacionJornadaInput(
                ResultadoParticipacionJornada.AusenciaJustificada,
                "Justificada"),
            user.Id);

        Assert.True(updated.Succeeded);
        Assert.Equal(
            ResultadoParticipacionJornada.AusenciaJustificada,
            participation.Resultado);

        jornada.Estado = EstadoJornada.Cerrada;
        await context.SaveChangesAsync();

        var rejected = await service.UpdateParticipantAsync(
            jornada.Id,
            person.Id,
            new ActualizarParticipacionJornadaInput(
                ResultadoParticipacionJornada.Participacion,
                null),
            user.Id);

        Assert.Equal(JornadaOperationError.Conflict, rejected.Error);
    }

    [Fact]
    public async Task RemoveParticipantAsync_RemovesBeforeClosureAndAudits()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);
        AddParticipation(context, jornada, person);
        var service = new JornadaService(context);

        var result = await service.RemoveParticipantAsync(
            jornada.Id,
            person.Id,
            user.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(context.ParticipacionesJornada);
        Assert.Contains(
            await context.Auditorias.ToListAsync(),
            a => a.Accion == "PARTICIPACION.RETIRAR");
    }

    [Fact]
    public async Task CloseAsync_RejectsWithoutParticipantsOrWithPendingParticipants()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context);
        var service = new JornadaService(context);

        var withoutParticipants = await service.CloseAsync(
            jornada.Id,
            user.Id);

        Assert.Equal(
            JornadaOperationError.Conflict,
            withoutParticipants.Error);

        var person = AddPersona(context, 10, EstadoRegistro.Activo);
        AddParticipation(context, jornada, person);

        var pending = await service.CloseAsync(jornada.Id, user.Id);

        Assert.Equal(JornadaOperationError.Conflict, pending.Error);
        Assert.Equal(EstadoJornada.Planificada, jornada.Estado);
    }

    [Fact]
    public async Task CloseAsync_ParticipationDoesNotGenerateObligation()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context, 25m);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);

        AddParticipation(
            context,
            jornada,
            person,
            ResultadoParticipacionJornada.Participacion);

        var service = new JornadaService(context);

        var result = await service.CloseAsync(jornada.Id, user.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoJornada.Cerrada, result.Value!.Estado);
        Assert.Empty(context.Obligaciones);
        Assert.Empty(context.ObligacionesJornada);
    }

    [Fact]
    public async Task CloseAsync_JustifiedAbsenceDoesNotGenerateObligation()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context, 25m);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);

        AddParticipation(
            context,
            jornada,
            person,
            ResultadoParticipacionJornada.AusenciaJustificada);

        var service = new JornadaService(context);

        var result = await service.CloseAsync(jornada.Id, user.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(context.Obligaciones);
    }

    [Fact]
    public async Task CloseAsync_AbsenceWithoutConfiguredAmountDoesNotGenerateObligation()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context, null);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);

        AddParticipation(
            context,
            jornada,
            person,
            ResultadoParticipacionJornada.Ausencia);

        var service = new JornadaService(context);

        var result = await service.CloseAsync(jornada.Id, user.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(context.Obligaciones);
    }

    [Fact]
    public async Task CloseAsync_PenalizedAbsenceGeneratesPersonObligationAndTraceability()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context, 35m);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);

        var participation = AddParticipation(
            context,
            jornada,
            person,
            ResultadoParticipacionJornada.Ausencia);

        var service = new JornadaService(context);

        var result = await service.CloseAsync(jornada.Id, user.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoJornada.Cerrada, result.Value!.Estado);

        var obligation = await context.Obligaciones.SingleAsync();

        Assert.Equal(person.Id, obligation.PersonaId);
        Assert.Null(obligation.SuministroId);
        Assert.Null(obligation.CuotaId);
        Assert.Equal(OrigenObligacion.Jornada, obligation.Origen);
        Assert.Equal("Ausencia a jornada: Jornada comunitaria", obligation.Concepto);
        Assert.Equal(35m, obligation.Monto);
        Assert.Null(obligation.Periodo);
        Assert.Null(obligation.FechaVencimiento);
        Assert.Equal(EstadoObligacion.Pendiente, obligation.Estado);

        var link = await context.ObligacionesJornada.SingleAsync();
        Assert.Equal(participation.Id, link.ParticipacionJornadaId);
        Assert.Equal(obligation.Id, link.ObligacionId);

        Assert.Contains(
            await context.Auditorias.ToListAsync(),
            a => a.Accion == "OBLIGACION.GENERAR.DESDE_JORNADA");

        Assert.Contains(
            await context.Auditorias.ToListAsync(),
            a => a.Accion == "JORNADA.CERRAR");
    }

    [Fact]
    public async Task CloseAsync_MultipleAbsencesGenerateOneObligationEach()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context, 20m);

        var first = AddPersona(context, 10, EstadoRegistro.Activo);
        var second = AddPersona(context, 11, EstadoRegistro.Activo);
        var third = AddPersona(context, 12, EstadoRegistro.Activo);

        AddParticipation(
            context,
            jornada,
            first,
            ResultadoParticipacionJornada.Ausencia);

        AddParticipation(
            context,
            jornada,
            second,
            ResultadoParticipacionJornada.Ausencia);

        AddParticipation(
            context,
            jornada,
            third,
            ResultadoParticipacionJornada.Participacion);

        var service = new JornadaService(context);

        var result = await service.CloseAsync(jornada.Id, user.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(2, await context.Obligaciones.CountAsync());
        Assert.Equal(2, await context.ObligacionesJornada.CountAsync());
    }

    [Fact]
    public async Task CloseAsync_SecondClosureIsRejectedAndDoesNotDuplicate()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var jornada = AddJornada(context, 30m);
        var person = AddPersona(context, 10, EstadoRegistro.Activo);

        AddParticipation(
            context,
            jornada,
            person,
            ResultadoParticipacionJornada.Ausencia);

        var service = new JornadaService(context);

        var first = await service.CloseAsync(jornada.Id, user.Id);
        var second = await service.CloseAsync(jornada.Id, user.Id);

        Assert.True(first.Succeeded);
        Assert.Equal(JornadaOperationError.Conflict, second.Error);
        Assert.Single(context.Obligaciones);
        Assert.Single(context.ObligacionesJornada);
    }

    [Fact]
    public async Task Mutations_RejectUnknownAdministrativeUser()
    {
        await using var context = CreateContext();
        var service = new JornadaService(context);

        var result = await service.CreateAsync(Input(), 999);

        Assert.Equal(JornadaOperationError.Invalid, result.Error);
        Assert.Empty(context.Jornadas);
        Assert.Empty(context.Auditorias);
    }

    private static JornadaInput Input() => new(
        "Jornada comunitaria",
        "Limpieza comunitaria",
        new DateTime(2026, 9, 20),
        new TimeOnly(8, 0),
        new TimeOnly(12, 0),
        "Salón comunal",
        30m);

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PanyebarDbContext(options);
    }

    private static UsuarioAdministrativo AddUser(PanyebarDbContext context)
    {
        var user = new UsuarioAdministrativo
        {
            Id = 1,
            NombreUsuario = "admin",
            PasswordHash = "hash",
            Estado = EstadoRegistro.Activo
        };

        context.UsuariosAdministrativos.Add(user);
        context.SaveChanges();
        return user;
    }

    private static Jornada AddJornada(
        PanyebarDbContext context,
        decimal? amount = 30m)
    {
        var jornada = new Jornada
        {
            Nombre = "Jornada comunitaria",
            Descripcion = "Limpieza",
            Fecha = new DateTime(2026, 9, 20),
            HoraInicio = new TimeOnly(8, 0),
            HoraFin = new TimeOnly(12, 0),
            Ubicacion = "Salón comunal",
            MontoIncumplimiento = amount,
            Estado = EstadoJornada.Planificada
        };

        context.Jornadas.Add(jornada);
        context.SaveChanges();
        return jornada;
    }

    private static Persona AddPersona(
        PanyebarDbContext context,
        int id,
        EstadoRegistro estado)
    {
        var persona = new Persona
        {
            Id = id,
            Nombres = $"Persona{id}",
            Apellidos = "QA",
            Estado = estado
        };

        context.Personas.Add(persona);
        context.SaveChanges();
        return persona;
    }

    private static ParticipacionJornada AddParticipation(
        PanyebarDbContext context,
        Jornada jornada,
        Persona person,
        ResultadoParticipacionJornada result =
            ResultadoParticipacionJornada.Pendiente)
    {
        var participation = new ParticipacionJornada
        {
            JornadaId = jornada.Id,
            PersonaId = person.Id,
            Resultado = result
        };

        context.ParticipacionesJornada.Add(participation);
        context.SaveChanges();
        return participation;
    }
}
