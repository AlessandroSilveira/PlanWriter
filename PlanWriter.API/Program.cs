using System.Data;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PlanWriter.API.Common.Middleware;
using PlanWriter.API.Middleware;
using PlanWriter.Application;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Services;
using PlanWriter.Application.Validators;
using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Helpers;
using PlanWriter.Domain.Interfaces.Auth;
using PlanWriter.Domain.Interfaces.Auth.Regsitration;
using PlanWriter.Domain.Interfaces.ReadModels.Auth;
using PlanWriter.Domain.Interfaces.ReadModels.Certificates;
using PlanWriter.Domain.Interfaces.ReadModels.DailyWordLogWrite;
using PlanWriter.Domain.Interfaces.ReadModels.Events.Admin;
using PlanWriter.Domain.Interfaces.ReadModels.Projects;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Infrastructure.Auth;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.ReadModels.Auth;
using PlanWriter.Infrastructure.ReadModels.Certificates;
using PlanWriter.Infrastructure.ReadModels.DailyWordLogWrite;
using PlanWriter.Infrastructure.ReadModels.Events;
using PlanWriter.Infrastructure.ReadModels.Projects;
using PlanWriter.Infrastructure.Repositories;
using PlanWriter.Infrastructure.Repositories.Auth;
using PlanWriter.Infrastructure.Repositories.Auth.Register;


var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        opt.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    })
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<RegisterUserDtoValidator>());

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

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
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey!))
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

// ===== EF Core =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== DI =====
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectProgressRepository, ProjectProgressRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>(); 
builder.Services.AddScoped<IProjectEventsRepository, ProjectEventsRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserFollowRepository, UserFollowRepository>();
builder.Services.AddScoped<IBuddiesRepository, BuddiesRepository>();
builder.Services.AddScoped<IMilestonesRepository, MilestonesRepository>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IDailyWordLogRepository, DailyWordLogRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IProjectReadRepository, ProjectReadRepository>();
builder.Services.AddScoped<IProjectProgressReadRepository, ProjectProgressReadRepository>();
builder.Services.AddScoped<IEventReadRepository, EventReadRepository>();
builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
builder.Services.AddScoped<IUserPasswordRepository, UserPasswordRepository>();
builder.Services.AddScoped<IUserAuthReadRepository, UserAuthReadRepository>();
builder.Services.AddScoped<IUserRegistrationReadRepository, UserRegistrationReadRepository>();
builder.Services.AddScoped<IUserRegistrationRepository, UserRegistrationRepository>();
builder.Services.AddScoped<ICertificateReadRepository, CertificateReadRepository>();
builder.Services.AddScoped<IDailyWordLogReadRepository, DailyWordLogReadRepository>();

builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var admin = await users.GetByEmailAsync("admin@admin.com");

    if (admin == null)
    {
        var user = new User
        {
            FirstName = "Admin",
            LastName = "System",
            Email = "admin@admin.com",
            IsProfilePublic = false,
            DisplayName = "Administrador"
        };
        user.ChangePassword(
            hasher.HashPassword(user, "admin")
        );
        user.MakeAdmin();
        await users.AddAsync(user);
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);
app.UseAuthentication();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();