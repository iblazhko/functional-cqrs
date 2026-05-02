using CQRS.Mapping.Inventory;
using CQRS.Mapping.Inventory.V1;
using Microsoft.Extensions.DependencyInjection;

namespace CQRS.Mapping;

public static class MappingModule
{
    public static IServiceCollection AddMappingServices(this IServiceCollection services)
    {
        services.AddSingleton<IInventoryCommandMapper, InventoryCommandV1Mapper>();
        services.AddSingleton<IInventoryEventMapper, InventoryEventV1Mapper>();

        return services;
    }
}
