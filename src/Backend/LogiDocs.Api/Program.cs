using LogiDocs.Application.Abstractions;
using LogiDocs.Application.Documents.Commands;
using LogiDocs.Application.Documents.Queries;
using LogiDocs.Application.Transports.Commands;
using LogiDocs.Application.Transports.Queries;
using LogiDocs.Infrastructure.Persistence;
using LogiDocs.Infrastructure.Persistence.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Storage (local disk) ----------
var storageRoot = Path.Combine(builder.Environment.ContentRootPath, "storage");
builder.Services.AddSingleton<IDocumentStorage>(_ => new LocalDocumentStorage(storageRoot));

// ---------- DbContext ----------
builder.Services.AddDbContext<LogiDocsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LogiDocs")));

// leag? interfa?a de implementare
builder.Services.AddScoped<ILogiDocsDbContext>(sp =>
    sp.GetRequiredService<LogiDocsDbContext>());

// ---------- UseCases ----------
builder.Services.AddScoped<CreateTransportUseCase>();
builder.Services.AddScoped<GetTransportsUseCase>();

builder.Services.AddScoped<UploadDocumentUseCase>();
builder.Services.AddScoped<GetDocumentsByTransportUseCase>();

// ---------- Controllers ----------
builder.Services.AddControllers();

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();