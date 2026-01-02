using System.Text;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PlanWriter.Application.Interfaces;
using PlanWriter.Application.Services;
using PlanWriter.Application.Validators;
using PlanWriter.Domain.Helpers;
using PlanWriter.Domain.Interfaces;
using PlanWriter.Domain.Interfaces.Repositories;
using PlanWriter.Domain.Interfaces.Services;
using PlanWriter.Infrastructure.Data;
using PlanWriter.Infrastructure.Repositories;
using IProjectService = PlanWriter.Application.Interfaces.IProjectService;

var builder = WebApplication.CreateBuilder(args);

// ===== Controllers + JSON + FluentValidation =====
builder.Services
    .AddControllers()
    .AddJsonOptions(opt =>
    {
        // enums como string (opcional, ajuda no Swagger/Front)
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // conversores para DateOnly/TimeOnly
        opt.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        opt.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    })
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<RegisterUserDtoValidator>());

// ===== JWT Config =====
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

// ===== Swagger (única chamada) =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PlanWriter API", Version = "v1" });

    // JWT Bearer
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

    // Mapear DateOnly/TimeOnly para o schema
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<DateOnly?>(() => new OpenApiSchema { Type = "string", Format = "date", Nullable = true });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
    c.MapType<TimeOnly?>(() => new OpenApiSchema { Type = "string", Format = "time", Nullable = true });

    // Se houver conflito de nomes de tipos em namespaces diferentes, habilite:
    // c.CustomSchemaIds(t => t.FullName);
});

// ===== EF Core =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== DI =====
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectProgressRepository, ProjectProgressRepository>();
builder.Services.AddScoped<IBadgeServices, BadgeServices>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IEventValidationService, EventValidationService>();
builder.Services.AddScoped<IProjectEventsRepository, ProjectEventsRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserFollowRepository, UserFollowRepository>();
builder.Services.AddScoped<IBuddiesRepository, BuddiesRepository>();
builder.Services.AddScoped<IBuddiesService, BuddiesService>();
builder.Services.AddScoped<IWordCountService, WordCountService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IRegionsRepository, RegionsRepository>();
builder.Services.AddScoped<IRegionsService, RegionsService>();
builder.Services.AddScoped<IMilestonesService, MilestonesService>();
builder.Services.AddScoped<IMilestonesRepository, MilestonesRepository>();



// ===== CORS =====
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
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

// ===== Swagger UI =====
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// =======================
// Conversores JSON (DateOnly/TimeOnly)
// =======================




