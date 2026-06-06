using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebApplication1.Data;
using WebApplication1.Services;

namespace WebApplication1.ServiceCollections
{
    public static class ConnectionInjection
    {
        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetRequiredConnectionString("MyExpressConnection");

            services.AddDbContext<LearningContext>(options =>
                options.UseSqlServer(connectionString));


            services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IBlobStorageService, BlobStorageService>();
            services.AddScoped<IAzurePracticeMessagingService, AzurePracticeMessagingService>();

            return services;
        }
    }
}
