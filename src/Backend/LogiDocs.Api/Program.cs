using LogiDocs.Application.Abstractions;
using LogiDocs.Application.Documents.Commands;
using LogiDocs.Application.Documents.Queries;
using LogiDocs.Application.Transports.Commands;
using LogiDocs.Application.Transports.Queries;
using LogiDocs.Infrastructure.Persistence;
using LogiDocs.Infrastructure.Persistence.Storage;
using LogiDocs.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


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


// ---------------- USE CASES ----------------

builder.Services.AddScoped<CreateTransportUseCase>();
builder.Services.AddScoped<GetTransportsUseCase>();

builder.Services.AddScoped<UploadDocumentUseCase>();
builder.Services.AddScoped<GetDocumentsByTransportUseCase>();

builder.Services.AddScoped<DownloadDocumentUseCase>();
builder.Services.AddScoped<RegisterDocumentOnChainUseCase>();
builder.Services.AddScoped<VerifyDocumentUseCase>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();