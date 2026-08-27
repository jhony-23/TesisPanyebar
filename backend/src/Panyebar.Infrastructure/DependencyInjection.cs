using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Panyebar.Infrastructure.Persistence;

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

        return services;
    }
}
