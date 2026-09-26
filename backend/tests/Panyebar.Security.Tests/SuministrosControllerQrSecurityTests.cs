using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panyebar.Api.Controllers;
using Panyebar.Application.Security;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Enums;

namespace Panyebar.Security.Tests;

public sealed class SuministrosControllerQrSecurityTests
{
    [Fact]
    public void PublicQrEndpoint_IsAnonymousAndUsesSeparatePublicRoute()
    {
        var method = typeof(SuministrosController).GetMethod(
            nameof(SuministrosController.GetPublicByQrToken),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.Single(method!.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(
            "public/qr/{token}",
            Assert.Single(method.GetCustomAttributes<HttpGetAttribute>()).Template);
    }

    [Fact]
    public void AdministrativeQrEndpoint_KeepsDynamicPermission()
    {
        var method = typeof(SuministrosController).GetMethod(
            nameof(SuministrosController.GetByQrToken),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);
        var authorize = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(
            "Permission:" + AdministrativePermissionCodes.SuministrosVer,
            authorize.Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task PublicQrEndpoint_ReturnsMinimumDtoForValidToken()
    {
        var expected = new SuministroQrPublicDto(
            "PAN-000010",
            "Panyebar Centro",
            EstadoSuministro.Activo,
            2,
            60m);
        var controller = new SuministrosController(new PublicQrStub(expected));

        var result = await controller.GetPublicByQrToken("valid-token", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task PublicQrEndpoint_ReturnsGenericNotFoundForUnknownToken()
    {
        var controller = new SuministrosController(new PublicQrStub(null));

        var result = await controller.GetPublicByQrToken("unknown-token", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.DoesNotContain("unknown-token", notFound.Value!.ToString());
    }

    private sealed class PublicQrStub : ISuministroService
    {
        private readonly SuministroQrPublicDto? _result;

        public PublicQrStub(SuministroQrPublicDto? result)
        {
            _result = result;
        }

        public Task<SuministroQrPublicDto?> GetPublicByQrTokenAsync(
            string token,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);

        public Task<IReadOnlyList<SuministroDto>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroDto?> GetByNisAsync(string nis, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroQrDto?> GetQrAsync(int suministroId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroDto?> GetByQrTokenAsync(string token, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ResponsableHistorialDto>?> GetResponsablesAsync(int suministroId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProcesoSuministroDto>?> GetProcesosAsync(int suministroId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroOperationResult<SuministroDto>> CreateAsync(SuministroInput input, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroOperationResult<SuministroDto>> UpdateAsync(int id, SuministroInput input, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroOperationResult<SuministroDto>> CancelAsync(int suministroId, SuministroProcesoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroOperationResult<SuministroDto>> ReconnectAsync(int suministroId, SuministroProcesoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SuministroOperationResult<SuministroDto>> SetResponsableAsync(int suministroId, SetResponsableInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
