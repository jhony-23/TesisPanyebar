using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Identidad administrativa utilizada para registrar acciones administrativas en el sistema.
    /// Contiene el hash de la contraseña; nunca debe almacenar la contraseña en texto legible.
    /// </summary>
    public class UsuarioAdministrativo
    {
        public int Id { get; set; }

        // Identificador de usuario para autenticación (no nulo)
        public string NombreUsuario { get; set; } = string.Empty;

        // Hash seguro de la contraseña (no almacenar contraseñas en texto claro)
        public string PasswordHash { get; set; } = string.Empty;

        // Estado administrativo del registro
        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
