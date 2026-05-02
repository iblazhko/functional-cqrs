using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.EntityIds;
using CQRS.Mapping;
using CQRS.Mapping.Inventory.V1;
using CQRS.Ports.MessageBus;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.Repositories.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using LanguageExt;

namespace CQRS.API.Inventory;

public interface IInventoriesApiService
{
    Task<Option<InventoryResponse>> GetInventory(string inventoryId);
    Task<Either<MappingFault, AcceptedResponse>> CreateInventory(CreateInventoryRequest request);
    Task<Either<MappingFault, AcceptedResponse>> RenameInventory(
        string inventoryId,
        RenameInventoryRequest request
    );
    Task<Either<MappingFault, AcceptedResponse>> AddItemsToInventory(
        string inventoryId,
        AddItemsToInventoryRequest request
    );
    Task<Either<MappingFault, AcceptedResponse>> RemoveItemsFromInventory(
        string inventoryId,
        RemoveItemsFromInventoryRequest request
    );
    Task<Either<MappingFault, AcceptedResponse>> DeactivateInventory(
        string inventoryId,
        DeactivateInventoryRequest request
    );
}

public class InventoryApiService(
    IMessageBus messageBus,
    InventoryViewModelQueryRepository queryRepository,
    TimeProvider timeProvider
) : IInventoriesApiService
{
    public async Task<Option<InventoryResponse>> GetInventory(string inventoryId)
    {
        var vm = await queryRepository.GetById((DocumentId)inventoryId);
        return ((Option<InventoryViewModel>)vm).Map(x => new InventoryResponse
        {
            InventoryId = x.Id,
            Name = x.Name,
            StockQuantity = x.StockQuantity,
            IsActive = x.IsActive,
        });
    }

    public async Task<Either<MappingFault, AcceptedResponse>> CreateInventory(
        CreateInventoryRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.InventoryId))
            request = request with { InventoryId = EntityId.NewId() };

        var commandDto = new CreateInventoryCommand
        {
            InventoryId = request.InventoryId,
            Name = request.Name,
        };

        var commandMetadata = ExtractRequestMetadata().GetResponseMetadata(timeProvider.GetUtcNow());
        var validationResult = commandDto.ToDomain();
        return await validationResult.Match(
            Left: fault => Task.FromResult<Either<MappingFault, AcceptedResponse>>(fault),
            Right: async command => (Either<MappingFault, AcceptedResponse>)await SendCommandDto(commandDto, command.Id, commandMetadata)
        );
    }

    public async Task<Either<MappingFault, AcceptedResponse>> RenameInventory(
        string inventoryId,
        RenameInventoryRequest request
    )
    {
        var commandDto = new RenameInventoryCommand
        {
            InventoryId = inventoryId,
            NewName = request.Name,
        };

        var commandMetadata = ExtractRequestMetadata().GetResponseMetadata(timeProvider.GetUtcNow());
        var validationResult = commandDto.ToDomain();
        return await validationResult.Match(
            Left: fault => Task.FromResult<Either<MappingFault, AcceptedResponse>>(fault),
            Right: async command => (Either<MappingFault, AcceptedResponse>)await SendCommandDto(commandDto, command.Id, commandMetadata)
        );
    }

    public async Task<Either<MappingFault, AcceptedResponse>> AddItemsToInventory(
        string inventoryId,
        AddItemsToInventoryRequest request
    )
    {
        var commandDto = new AddItemsToInventoryCommand
        {
            InventoryId = inventoryId,
            Count = request.Count,
        };

        var commandMetadata = ExtractRequestMetadata().GetResponseMetadata(timeProvider.GetUtcNow());
        var validationResult = commandDto.ToDomain();
        return await validationResult.Match(
            Left: fault => Task.FromResult<Either<MappingFault, AcceptedResponse>>(fault),
            Right: async command => (Either<MappingFault, AcceptedResponse>)await SendCommandDto(commandDto, command.Id, commandMetadata)
        );
    }

    public async Task<Either<MappingFault, AcceptedResponse>> RemoveItemsFromInventory(
        string inventoryId,
        RemoveItemsFromInventoryRequest request
    )
    {
        var commandDto = new RemoveItemsFromInventoryCommand
        {
            InventoryId = inventoryId,
            Count = request.Count,
        };

        var commandMetadata = ExtractRequestMetadata().GetResponseMetadata(timeProvider.GetUtcNow());
        var validationResult = commandDto.ToDomain();
        return await validationResult.Match(
            Left: fault => Task.FromResult<Either<MappingFault, AcceptedResponse>>(fault),
            Right: async command => (Either<MappingFault, AcceptedResponse>)await SendCommandDto(commandDto, command.Id, commandMetadata)
        );
    }

    public async Task<Either<MappingFault, AcceptedResponse>> DeactivateInventory(
        string inventoryId,
        DeactivateInventoryRequest request
    )
    {
        var commandDto = new DeactivateInventoryCommand { InventoryId = inventoryId };
        var commandMetadata = ExtractRequestMetadata().GetResponseMetadata(timeProvider.GetUtcNow());
        var validationResult = commandDto.ToDomain();
        return await validationResult.Match(
            Left: fault => Task.FromResult<Either<MappingFault, AcceptedResponse>>(fault),
            Right: async command => (Either<MappingFault, AcceptedResponse>)await SendCommandDto(commandDto, command.Id, commandMetadata)
        );
    }

    async Task<AcceptedResponse> SendCommandDto<TDto>(
        TDto commandDto,
        string inventoryId,
        Context commandMetadata
    )
        where TDto : IInventoryCommandDto
    {
        await messageBus.Send(commandDto, commandMetadata);
        return new AcceptedResponse
        {
            InventoryId = inventoryId,
            CommandId = commandMetadata.MessageId.Id,
            CorrelationId = commandMetadata.CorrelationId.Id,
            CausationId = commandMetadata.CausationId?.Id,
        };
    }

    Context ExtractRequestMetadata() => Context.GetNew(timeProvider.GetUtcNow()); // TODO: Extract from HTTP context
}

public static class InventoriesApiHttpExtensions
{
    public static IResult ToHttpResult<T>(this Option<T> outcome) =>
        outcome.Match(Results.Ok, Results.NotFound());

    public static IResult ToHttpResult(this Either<MappingFault, AcceptedResponse> outcome) =>
        outcome.Match(f => Results.BadRequest(f), r => Results.Accepted(value: r));
}
