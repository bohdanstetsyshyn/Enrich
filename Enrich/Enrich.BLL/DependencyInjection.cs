using Enrich.BLL.Interfaces;
using Enrich.BLL.Services;
using Enrich.BLL.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enrich.BLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBllServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<PaginationSettings>(
                configuration.GetSection(PaginationSettings.Section));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWordService, WordService>();
            services.AddScoped<IBundleService, BundleService>();
            services.AddScoped<ICategoryService, CategoryService>();

            return services;
        }
    }
}
