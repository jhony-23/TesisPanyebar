namespace Panyebar.Application.Security;

public sealed class AccessTokenResult
{
    public string Token { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
