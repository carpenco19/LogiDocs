using LogiDocs.Application.Abstractions;
using LogiDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LogiDocs.Infrastructure.Blockchain;

namespace LogiDocs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<LogiDocsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IBlockchainRegistrar, FakeBlockchainRegistrar>();

        return services;
    }
}