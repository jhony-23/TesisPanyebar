using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Panyebar.Application.Security;
using Panyebar.Infrastructure;
using Panyebar.Infrastructure.Security;
using Panyebar.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    var publicWebBaseUrl = builder.Configuration["PublicWeb:BaseUrl"]?.Trim().TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(publicWebBaseUrl) && !CorsOrigin.IsValid(publicWebBaseUrl))
    {
        throw new InvalidOperationException("PublicWeb:BaseUrl debe ser un origen HTTP o HTTPS válido.");
    }

    options.AddPolicy("FrontendDevelopment", policy =>
    {
        if (string.IsNullOrWhiteSpace(publicWebBaseUrl))
        {
            throw new InvalidOperationException(
                "La configuración PublicWeb:BaseUrl es requerida en desarrollo.");
        }

        policy.WithOrigins(publicWebBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    if (!string.IsNullOrWhiteSpace(publicWebBaseUrl))
    {
        options.AddPolicy("ConfiguredFrontend", policy => policy
            .WithOrigins(publicWebBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod());
    }
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT bearer token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtTokenOptions.SectionName).Get<JwtTokenOptions>()
    ?? throw new InvalidOperationException("La configuración JWT es requerida.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) ||
    string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("La configuración JWT no es válida. Faltan issuer, audience o key.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

var app = builder.Build();

if (args.Contains("--bootstrap-admin", StringComparer.Ordinal))
{
    var username = Environment.GetEnvironmentVariable("PANYEBAR_BOOTSTRAP_ADMIN_USERNAME");
    if (string.IsNullOrWhiteSpace(username))
    {
        Console.Error.WriteLine("Defina PANYEBAR_BOOTSTRAP_ADMIN_USERNAME para ejecutar el bootstrap.");
        Environment.ExitCode = 2;
        return;
    }

    Console.Write("Contraseña del administrador inicial: ");
    var password = ReadHiddenInput();
    Console.WriteLine();

    await using var scope = app.Services.CreateAsyncScope();
    var provisioner = scope.ServiceProvider.GetRequiredService<IInitialAdministratorProvisioner>();
    var result = await provisioner.ProvisionAsync(username, password);
    Console.WriteLine(result.Message);
    Environment.ExitCode = result.Succeeded ? 0 : 1;
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("FrontendDevelopment");
}
else if (CorsOrigin.IsValid(builder.Configuration["PublicWeb:BaseUrl"]))
{
    app.UseCors("ConfiguredFrontend");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string ReadHiddenInput()
{
    var password = new System.Text.StringBuilder();
    ConsoleKeyInfo key;
    do
    {
        key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password.Length--;
        }
        else if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    } while (key.Key != ConsoleKey.Enter);

    return password.ToString();
}
