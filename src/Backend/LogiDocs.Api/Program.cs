using LogiDocs.Application.Abstractions;
using LogiDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using LogiDocs.Application.Transports.Commands;
using LogiDocs.Application.Transports.Queries;
using LogiDocs.Application.Documents.Commands;
using LogiDocs.Application.Documents.Queries;
using LogiDocs.Infrastructure.Persistence.Storage; 
var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
builder.Services.AddScoped<UploadDocumentUseCase>();
builder.Services.AddScoped<GetDocumentsByTransportUseCase>();

builder.Services.AddSingleton<IDocumentStorage, LocalDocumentStorage>();

// DbContext
builder.Services.AddDbContext<LogiDocsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LogiDocs")));

builder.Services.AddSingleton<IDocumentStorage>(_ =>
    new LocalDocumentStorage(Path.Combine(builder.Environment.ContentRootPath, "storage")));
// Leg?m interfa?a de implementare
builder.Services.AddScoped<ILogiDocsDbContext>(sp =>
    sp.GetRequiredService<LogiDocsDbContext>());

builder.Services.AddScoped<CreateTransportUseCase>();
builder.Services.AddScoped<GetTransportsUseCase>();

// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------- Middleware ----------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// ---------- Endpoints ----------
app.MapControllers();

app.Run();