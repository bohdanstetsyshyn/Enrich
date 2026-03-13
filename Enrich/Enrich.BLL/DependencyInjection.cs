using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enrich.BLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBllServices(this IServiceCollection services)
        {
            // services.AddScoped<IService, Service>();

            return services;
        }
    }
}
