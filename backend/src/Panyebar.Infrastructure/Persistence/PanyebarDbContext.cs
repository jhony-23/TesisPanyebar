using Microsoft.EntityFrameworkCore;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence
{
    /// <summary>
    /// Contexto de base de datos para el sistema Panyebar.
    /// Proporciona acceso a todas las entidades del dominio y aplica las configuraciones de persistencia.
    /// </summary>
    public class PanyebarDbContext : DbContext
    {
        public PanyebarDbContext(DbContextOptions<PanyebarDbContext> options) : base(options)
        {
        }

        // DbSets para todas las entidades del dominio
        public DbSet<Persona> Personas { get; set; }
        public DbSet<Sector> Sectores { get; set; }
        public DbSet<Suministro> Suministros { get; set; }
        public DbSet<PersonaSuministro> PersonaSuministros { get; set; }
        public DbSet<Cuota> Cuotas { get; set; }
        public DbSet<Obligacion> Obligaciones { get; set; }
        public DbSet<Jornada> Jornadas { get; set; }
        public DbSet<ParticipacionJornada> ParticipacionesJornada { get; set; }
        public DbSet<ObligacionJornada> ObligacionesJornada { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<AplicacionPago> AplicacionesPago { get; set; }
        public DbSet<Egreso> Egresos { get; set; }
        public DbSet<UsuarioAdministrativo> UsuariosAdministrativos { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<UsuarioRol> UsuarioRoles { get; set; }
        public DbSet<RolPermiso> RolPermisos { get; set; }
        public DbSet<AdministracionComite> AdministracionesComite { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<IntegranteAdministracion> IntegrantesAdministracion { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<ProgramacionAbastecimiento> ProgramacionesAbastecimiento { get; set; }
        public DbSet<ProcesoSuministro> ProcesosSuministro { get; set; }
        public DbSet<SolicitudNuevoServicio> SolicitudesNuevoServicio { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<long>("SuministroNisSequence", "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            // Aplicar todas las configuraciones de relaciones desde el ensamblado
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PanyebarDbContext).Assembly);
        }
    }
}
