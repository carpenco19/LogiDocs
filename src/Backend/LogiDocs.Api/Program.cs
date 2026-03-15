using System.Security.Claims;
using System.Text;
using LogiDocs.Api.Security;
using LogiDocs.Application.Abstractions;
using LogiDocs.Application.Audit.Queries;
using LogiDocs.Application.Documents.Commands;
using LogiDocs.Application.Documents.Queries;
using LogiDocs.Application.Transports.Commands;
using LogiDocs.Application.Transports.Queries;
using LogiDocs.Infrastructure;
using LogiDocs.Infrastructure.Persistence;
using LogiDocs.Infrastructure.Persistence.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------- JWT ----------------

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing.");

var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtOptions.Key);
if (jwtKeyBytes.Length < 32)
    throw new InvalidOperationException("JWT key must be at least 32 bytes long.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ---------------- STORAGE ----------------

var storageRoot = Path.Combine(builder.Environment.ContentRootPath, "storage");

builder.Services.AddSingleton<IDocumentStorage>(_ =>
    new LocalDocumentStorage(storageRoot));

// ---------------- DATABASE ----------------

builder.Services.AddDbContext<LogiDocsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LogiDocs")));

builder.Services.AddScoped<ILogiDocsDbContext>(sp =>
    sp.GetRequiredService<LogiDocsDbContext>());

builder.Services.AddScoped<IAuditWriter, AuditWriter>();

// ---------------- USE CASES ----------------

builder.Services.AddScoped<CreateTransportUseCase>();
builder.Services.AddScoped<GetTransportsUseCase>();
builder.Services.AddScoped<DeleteTransportUseCase>();

builder.Services.AddScoped<UploadDocumentUseCase>();
builder.Services.AddScoped<GetDocumentsByTransportUseCase>();

builder.Services.AddScoped<DownloadDocumentUseCase>();
builder.Services.AddScoped<RegisterDocumentOnChainUseCase>();
builder.Services.AddScoped<VerifyDocumentUseCase>();
builder.Services.AddScoped<GetAuditEntriesUseCase>();

// ---------------- INFRASTRUCTURE ----------------

builder.Services.AddInfrastructure(builder.Configuration);

// ---------------- CONTROLLERS ----------------

builder.Services.AddControllers();

// ---------------- SWAGGER ----------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------- BUILD APP ----------------

var app = builder.Build();

// ---------------- DEVELOPMENT ----------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------------- MIDDLEWARE ----------------

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();