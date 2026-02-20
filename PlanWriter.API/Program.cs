using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PlanWriter.API.Common.Middleware;
using PlanWriter.API.Middleware;
using PlanWriter.API.Security;
using PlanWriter.Application;
using PlanWriter.Application.Common.Events;
using PlanWriter.Application.Common.WinnerEligibility;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Services;
using PlanWriter.Application.Validators;
using PlanWriter.Domain.Configurations;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Helpers;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Badges;
using PlanWriter.Domain.Interfaces.ReadModels.Certificates;
using PlanWriter.Domain.Interfaces.ReadModels.DailyWordLogWrite;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.ReadModels.Milestones;
using PlanWriter.Domain.Interfaces.ReadModels.ProjectEvents;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.ReadModels.WordWars;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Repositories.Auth;
using PlanWriter.Domain.Interfaces.Repositories.DailyWordLogWrite;
using PlanWriter.Domain.Interfaces.Repositories.Events.Admin;
using PlanWriter.Domain.Interfaces.Repositories.WordWars;
using PlanWriter.Infrastructure.Auth;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Auth;
using PlanWriter.Infrastructure.ReadModels.Badges;
using PlanWriter.Infrastructure.ReadModels.Certificates;
using PlanWriter.Infrastructure.ReadModels.DailyWordLogWrite;
using PlanWriter.Infrastructure.ReadModels.Events.Admin;
using PlanWriter.Infrastructure.ReadModels.Events;
using PlanWriter.Infrastructure.ReadModels.Milestones;
using PlanWriter.Infrastructure.ReadModels.ProjectEvents;
using PlanWriter.Infrastructure.ReadModels.Projects;
using PlanWriter.Infrastructure.ReadModels.WordWars;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Infrastructure.Repositories.DailyWordWrite;
using PlanWriter.Infrastructure.Repositories.Events.Admin;
using PlanWriter.Infrastructure.Repositories.Auth;
using PlanWriter.Infrastructure.Repositories.Auth.Register;
using PlanWriter.Infrastructure.Repositories.ProjectEvents;
using PlanWriter.Infrastructure.Repositories.WordWars;
using IEventReadRepository = PlanWriter.Domain.Interfaces.ReadModels.Events.IEventReadRepository;
using IUserReadRepository = PlanWriter.Domain.Interfaces.ReadModels.Users.IUserReadRepository;
using UserReadRepository = PlanWriter.Infrastructure.ReadModels.Users.UserReadRepository;


var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        opt.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserDtoValidator>();
builder.Services.Configure<AuthTokenOptions>(builder.Configuration.GetSection("AuthTokens"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AuthAuditOptions>(builder.Configuration.GetSection("AuthAudit"));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-login", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth-login:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    options.AddPolicy("auth-register", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth-register:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    options.AddPolicy("auth-refresh", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth-refresh:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>() ?? new JwtOptions();
JwtSecurityConfiguration.ValidateForStartup(jwtOptions, builder.Environment.IsProduction());
var signingKeys = JwtSecurityConfiguration.BuildSigningKeys(jwtOptions);
var clockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds);

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
        ValidateAudience = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        ClockSkew = clockSkew,
        IssuerSigningKeyResolver = (_, _, kid, _) =>
        {
            if (!string.IsNullOrWhiteSpace(kid) && signingKeys.TryGetValue(kid, out var signingKey))
            {
                return [signingKey];
            }

            return signingKeys.Values;
        }
    };
});
builder.Services.AddApplication();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PlanWriter API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite o token JWT assim: Bearer {seu token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme, Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<DateOnly?>(() => new OpenApiSchema { Type = "string", Format = "date", Nullable = true });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
    c.MapType<TimeOnly?>(() => new OpenApiSchema { Type = "string", Format = "time", Nullable = true });
});

// ===== DI =====
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectProgressRepository, ProjectProgressRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>(); 
builder.Services.AddScoped<IBadgeReadRepository, BadgeReadRepository>();
builder.Services.AddScoped<IProjectEventsRepository, ProjectEventsRepository>();
builder.Services.AddScoped<IEventValidationAuditRepository, EventValidationAuditRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IAdminEventRepository, AdminEventRepository>();
builder.Services.AddScoped<IUserFollowRepository, UserFollowRepository>();
builder.Services.AddScoped<IBuddiesRepository, BuddiesRepository>();
builder.Services.AddScoped<IMilestonesRepository, MilestonesRepository>();
builder.Services.AddScoped<IMilestonesReadRepository, MilestonesReadRepository>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IDailyWordLogRepository, DailyWordLogRepository>();
builder.Services.AddScoped<IDailyWordLogWriteRepository, DailyWordLogWriteRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IProjectReadRepository, ProjectReadRepository>();
builder.Services.AddScoped<IProjectProgressReadRepository, ProjectProgressReadRepository>();
builder.Services.AddScoped<IEventReadRepository, EventReadRepository>();
builder.Services.AddScoped<IAdminEventReadRepository, AdminEventReadRepository>();
builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
builder.Services.AddScoped<IUserPasswordRepository, UserPasswordRepository>();
builder.Services.AddScoped<IUserAuthReadRepository, UserAuthReadRepository>();
builder.Services.AddScoped<IAuthAuditReadRepository, AuthAuditReadRepository>();
builder.Services.AddScoped<IUserRegistrationReadRepository, UserRegistrationReadRepository>();
builder.Services.AddScoped<IUserRegistrationRepository, UserRegistrationRepository>();
builder.Services.AddScoped<IAuthAuditRepository, AuthAuditRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICertificateReadRepository, CertificateReadRepository>();
builder.Services.AddScoped<IDailyWordLogReadRepository, DailyWordLogReadRepository>();
builder.Services.AddScoped<IProjectEventsReadRepository, ProjectEventsReadRepository>();
builder.Services.AddScoped<IWinnerEligibilityService, WinnerEligibilityService>();
builder.Services.AddScoped<IEventProgressCalculator, EventProgressCalculator>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILoginLockoutService, InMemoryLoginLockoutService>();

builder.Services.AddScoped<IWordWarParticipantReadRepository, WordWarParticipantReadRepository>();
builder.Services.AddScoped<IWordWarRepository, WordWarRepository>();
builder.Services.AddScoped<IWordWarReadRepository, WordWarReadRepository>();


builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IDbExecutor, DapperDbExecutor>();

builder.Services.AddHttpContextAccessor();


// ===== CORS =====
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://127.0.0.1:5173",
            "http://192.168.15.36:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var bootstrapOptions = builder.Configuration
        .GetSection("AuthBootstrap")
        .Get<AuthBootstrapOptions>() ?? new AuthBootstrapOptions();

    AdminBootstrapper.ValidateConfiguration(bootstrapOptions, app.Environment.IsProduction());

    var cancellationToken = new CancellationToken();
    var usersRead = scope.ServiceProvider.GetRequiredService<IUserReadRepository>();
    var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    await AdminBootstrapper.EnsureBootstrapAdminAsync(
        bootstrapOptions,
        usersRead,
        users,
        hasher,
        app.Logger,
        cancellationToken);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
