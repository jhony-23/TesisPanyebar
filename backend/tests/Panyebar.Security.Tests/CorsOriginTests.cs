using Panyebar.Api;

namespace Panyebar.Security.Tests;

public sealed class CorsOriginTests
{
    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("https://panel.example.test")]
    public void Accepts_http_or_https_origins(string origin)
    {
        Assert.True(CorsOrigin.IsValid(origin));
    }

    [Theory]
    [InlineData("")]
    [InlineData("*")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://panel.example.test/path")]
    [InlineData("https://user:password@panel.example.test")]
    public void Rejects_permissive_or_non_origin_values(string origin)
    {
        Assert.False(CorsOrigin.IsValid(origin));
    }
}