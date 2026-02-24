using LogiDocs.Application.Abstractions;
using LogiDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

// DbContext
builder.Services.AddDbContext<LogiDocsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LogiDocs")));

// Leg?m interfa?a de implementare
builder.Services.AddScoped<ILogiDocsDbContext>(sp =>
    sp.GetRequiredService<LogiDocsDbContext>());

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