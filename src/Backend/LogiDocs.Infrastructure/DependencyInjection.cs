using LogiDocs.Application.Abstractions;
using LogiDocs.Infrastructure.Blockchain;
using LogiDocs.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogiDocs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IBlockchainRegistrar, FakeBlockchainRegistrar>();

        return services;
    }
}