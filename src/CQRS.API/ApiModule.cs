using CQRS.API.Inventory;
using CQRS.Projections.Repositories.Inventory.V1;

namespace CQRS.API;

public static class ApiModule
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IInventoriesApiService, InventoryApiService>();
        services.AddScoped<InventoryViewModelQueryRepository>();

        return services;
    }

    public static WebApplication AddApiEndpoints(this WebApplication app)
    {
        app.AddInventoriesApiEndpoints();

        return app;
    }
}
