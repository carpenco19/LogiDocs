using LogiDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
builder.Services.AddDbContext<LogiDocsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("LogiDocs"));
});

// Controllers (API endpoints)
builder.Services.AddControllers();

// OpenAPI / Swagger (template-ul t?u folose?te AddOpenApi + MapOpenApi)
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