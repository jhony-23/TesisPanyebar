using System.Security.Cryptography;
using Panyebar.Application.Suministros;

namespace Panyebar.Infrastructure.Security;

public sealed class SuministroQrTokenGenerator : ISuministroQrTokenGenerator
{
    private const int TokenByteLength = 32;

    public string Generate()
    {
        Span<byte> tokenBytes = stackalloc byte[TokenByteLength];
        RandomNumberGenerator.Fill(tokenBytes);

        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}