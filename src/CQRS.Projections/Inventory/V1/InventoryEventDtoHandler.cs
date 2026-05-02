using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.Mapping;
using CQRS.Mapping.Inventory;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.ViewModels.Inventory.V1;
using LanguageExt;

namespace CQRS.Projections.Inventory.V1;

// ReSharper disable once ClassNeverInstantiated.Global
public interface IInventoryDomainEventDtoHandlerContext<TViewModel>
    where TViewModel : class
{
    IProjectionStore<TViewModel> ProjectionStore { get; }
    DocumentCollectionId GetDocumentCollectionId(IInventoryEvent @event) =>
        InventoryCollection.CollectionId;
    DocumentId GetDocumentId(IInventoryEvent @event) => (DocumentId)((string)@event.Id);
    TViewModel UpdateViewModel(TViewModel vm, IInventoryEvent @event);
}

public class InventoryInventoryDomainEventDtoHandlerContext<TViewModel>(
    IProjectionStore<TViewModel> projectionStore,
    Func<TViewModel, IInventoryEvent, TViewModel> updateVm
) : IInventoryDomainEventDtoHandlerContext<TViewModel>
    where TViewModel : class
{
    public IProjectionStore<TViewModel> ProjectionStore => projectionStore;

    public TViewModel UpdateViewModel(TViewModel vm, IInventoryEvent @event) =>
        updateVm(vm, @event);
}

public static class InventoryEventDtoHandler
{
    public static Task<Either<MappingFault, Unit>> Handle<TViewModel>(
        IInventoryEventDto dto,
        IInventoryDomainEventDtoHandlerContext<TViewModel> context
    )
        where TViewModel : class =>
        EventMapper
            .ToDomainEvent(dto)
            .Match(
                fault => Task.FromResult((Either<MappingFault, Unit>)fault),
                async domainEvent =>
                {
                    var documentCollectionId = context.GetDocumentCollectionId(domainEvent);
                    var documentId = context.GetDocumentId(domainEvent);
                    var documentCollection = await context.ProjectionStore.OpenDocumentCollection(
                        documentCollectionId
                    );
                    await documentCollection.Update(
                        documentId,
                        vm => context.UpdateViewModel(vm, domainEvent)
                    );
                    return (Either<MappingFault, Unit>)Unit.Default;
                }
            );

    private static readonly IInventoryEventMapper EventMapper =
        new Mapping.Inventory.V1.InventoryEventV1Mapper();
}
