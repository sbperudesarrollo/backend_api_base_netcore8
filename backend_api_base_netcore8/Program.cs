using backend_api_base_netcore8.Application.Configuration;
using backend_api_base_netcore8.Application.Interfaces;
using backend_api_base_netcore8.Application.Services;
using backend_api_base_netcore8.Application.Validators;
using backend_api_base_netcore8.Infrastructure.Data;
using backend_api_base_netcore8.Infrastructure.Repositories.MySql;
using backend_api_base_netcore8.Infrastructure.Repositories.Oracle;
using backend_api_base_netcore8.Infrastructure.Repositories.Postgres;
using backend_api_base_netcore8.Infrastructure.Repositories.Sql;
using backend_api_base_netcore8.Infrastructure.Security;
using backend_api_base_netcore8.Infrastructure.Stores;
using backend_api_base_netcore8.Infrastructure.Swagger;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "FrontendCors";

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddLog4Net("log4net.config");

builder.Services.AddControllers();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();

            return;
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth API",
        Version = "v1",
        Description = "API para autenticacion mediante JWT."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Introduce el token JWT con el esquema Bearer."
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.OperationFilter<AuthorizeOperationFilter>();
    options.OperationFilter<AuthLoginOperationFilter>();
});

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddHttpClient();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));

var databaseProviderName = builder.Configuration.GetValue<string>("DatabaseProvider") ?? nameof(DatabaseProvider.MySql);
if (!Enum.TryParse(databaseProviderName, ignoreCase: true, out DatabaseProvider databaseProvider))
{
    throw new InvalidOperationException($"Unsupported database provider '{databaseProviderName}'. Use 'MySql', 'SqlServer', 'PostgreSql', or 'Oracle'.");
}

//builder.Services.Configure<DatabaseOptions>(options =>
//{
//    var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//    var providerConnectionString = builder.Configuration.GetConnectionString(databaseProvider.ToString());

//    var effectiveConnectionString = !string.IsNullOrWhiteSpace(providerConnectionString)
//        ? providerConnectionString
//        : defaultConnectionString;

//    if (string.IsNullOrWhiteSpace(effectiveConnectionString))
//    {
//        throw new InvalidOperationException(
//            $"Connection string for provider '{databaseProvider}' was not found. Configure either ConnectionStrings:{databaseProvider} or ConnectionStrings:DefaultConnection.");
//    }

//    options.ConnectionString = effectiveConnectionString;
//    options.Provider = databaseProvider;
//});

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddSingleton<AppDataStore>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsersService, UsersService>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

switch (databaseProvider)
{
    case DatabaseProvider.MySql:
        builder.Services.AddScoped<IUserRepository, UserRepositoryMySql>();
        builder.Services.AddScoped<IUsersCrudRepository, UserRepositoryMySql>();
        builder.Services.AddScoped<IRoleRepository, UserRepositoryMySql>();
        break;
    case DatabaseProvider.SqlServer:
        builder.Services.AddScoped<IUserRepository, UserRepositorySql>();
        builder.Services.AddScoped<IUsersCrudRepository, UserRepositorySql>();
        builder.Services.AddScoped<IRoleRepository, UserRepositorySql>();
        break;
    case DatabaseProvider.PostgreSql:
        builder.Services.AddScoped<IUserRepository, UserRepositoryPostgres>();
        builder.Services.AddScoped<IUsersCrudRepository, UserRepositoryPostgres>();
        builder.Services.AddScoped<IRoleRepository, UserRepositoryPostgres>();
        break;
    case DatabaseProvider.Oracle:
        builder.Services.AddScoped<IUserRepository, UserRepositoryOracle>();
        builder.Services.AddScoped<IUsersCrudRepository, UserRepositoryOracle>();
        builder.Services.AddScoped<IRoleRepository, UserRepositoryOracle>();
        break;
    default:
        throw new InvalidOperationException($"Unsupported database provider '{databaseProvider}'.");
}


var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("JWT configuration is missing the signing key.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("JWT configuration requires both Issuer and Audience.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
