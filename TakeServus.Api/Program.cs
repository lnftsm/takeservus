using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TakeServus.Api.Swagger;
using TakeServus.Infrastructure.Extensions;
using TakeServus.Persistence.DbContexts;
using TakeServus.Persistence.Seed;
using TakeServus.Shared.Settings;
using TakeServus.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Configuration
// ----------------------------
var configuration = builder.Configuration;
var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

// ----------------------------
// Register Settings
// ----------------------------
builder.Services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
builder.Services.Configure<FirebaseSettings>(configuration.GetSection("FirebaseSettings"));
builder.Services.Configure<FileStorageSettings>(configuration.GetSection("FileStorageSettings"));
builder.Services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));

// ----------------------------
// Kestrel Port Binding
// ----------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5050); // Enable 192.168.X.X access
});

// ----------------------------
// Context + Health Checks
// ----------------------------
builder.Services.AddDbContext<TakeServusDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!);

// ----------------------------
// Dependency Injection Setup
// ----------------------------
builder.Services.AddHttpContextAccessor(); // Required for audit interceptor
builder.Services.UseFileStorage(configuration); // Firebase vs Local handled internally
builder.Services.AddInfrastructureServices(configuration); // IEmailService, IInvoiceService, etc.

// ----------------------------
// Authentication - JWT
// ----------------------------
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
        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

// ----------------------------
// Swagger & API Setup
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TakeServus API",
        Version = "v1",
        Description = "TakeServus Service Management API"
    });

    options.SupportNonNullableReferenceTypes();
    options.CustomSchemaIds(type => type.FullName);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<SwaggerFileUploadFilter>(); // Enables IFormFile in Swagger
});

// ----------------------------
// Model Validation
// ----------------------------
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => new
            {
                Field = x.Key,
                Errors = x.Value!.Errors.Select(e => e.ErrorMessage)
            });

        return new BadRequestObjectResult(new
        {
            Message = "Validation failed",
            Details = errors
        });
    };
});

var app = builder.Build();

// ----------------------------
// Database Seeding
// ----------------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TakeServusDbContext>();
    DataSeeder.SeedInitialData(context);
}

// ----------------------------
// Middleware Pipeline
// ----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();              // wwwroot/uploads support
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();