using Microsoft.AspNetCore.Identity;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Security;

public sealed class PasswordHasherAdapter : IPasswordHashService
{
    private readonly PasswordHasher<UsuarioAdministrativo> _passwordHasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(new UsuarioAdministrativo(), password);
    }

    public bool Verify(string passwordHash, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        var result = _passwordHasher.VerifyHashedPassword(
            new UsuarioAdministrativo(),
            passwordHash,
            providedPassword);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
