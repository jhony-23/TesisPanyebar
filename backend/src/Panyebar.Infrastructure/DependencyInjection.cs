using Panyebar.Application.AdministracionComite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Panyebar.Application.SolicitudesNuevoServicio;
using Panyebar.Application.Suministros;
using Panyebar.Application.Security;
using Panyebar.Application.Personas;
using Panyebar.Application.Sectores;
using Panyebar.Application.Cuotas;
using Panyebar.Application.Obligaciones;
using Panyebar.Application.Pagos;
using Panyebar.Application.Jornadas;
using Panyebar.Application.Finanzas;
using Panyebar.Application.DashboardReportes;
using Panyebar.Application.Abastecimiento;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión requerida 'DefaultConnection' no está configurada.");
        }

        services.AddDbContext<PanyebarDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPasswordHashService, PasswordHasherAdapter>();
        services.AddScoped<IInitialAdministratorProvisioner, InitialAdministratorProvisioner>();
        services.AddScoped<IUsuarioAdministrativoAuthenticationRepository, UsuarioAdministrativoAuthenticationRepository>();
        services.AddScoped<IUsuarioAdministrativoAuthenticationService, UsuarioAdministrativoAuthenticationService>();
        services.AddScoped<IUsuarioPermissionRepository, UsuarioPermissionRepository>();
        services.AddScoped<IAdministrativeAccessService, AdministrativeAccessService>();
        services.AddScoped<ISectorService, SectorService>();
        services.AddScoped<IPersonaService, PersonaService>();
        services.AddScoped<ISuministroService, SuministroService>();
        services.AddScoped<ISolicitudNuevoServicioService, SolicitudNuevoServicioService>();
        services.AddScoped<ICuotaService, CuotaService>();
        services.AddScoped<IObligacionService, ObligacionService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<IJornadaService, JornadaService>();
        services.AddScoped<IFinanzaService, FinanzaService>();
        services.AddScoped<IDashboardReportesService, DashboardReportesService>();
        services.AddScoped<IAbastecimientoService, AbastecimientoService>();
        services.AddScoped<IAdministracionComiteService, AdministracionComiteService>();
        services.AddScoped<ISuministroNisGenerator, SuministroNisGenerator>();
        services.AddSingleton<ISuministroQrTokenGenerator, SuministroQrTokenGenerator>();

        services.Configure<SuministroQrOptions>(
            configuration.GetSection(SuministroQrOptions.SectionName));

        services.Configure<JwtTokenOptions>(configuration.GetSection(JwtTokenOptions.SectionName));
        services.AddScoped<IAccessTokenService, JwtAccessTokenService>();

        return services;
    }
}
