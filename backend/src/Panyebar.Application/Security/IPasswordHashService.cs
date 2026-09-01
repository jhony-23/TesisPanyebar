namespace Panyebar.Application.Security;

public interface IPasswordHashService
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
