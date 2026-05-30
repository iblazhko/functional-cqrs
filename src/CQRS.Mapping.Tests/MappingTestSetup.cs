using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.EntityIds;
using LanguageExt;

namespace CQRS.Mapping.Tests;

// ReSharper disable InconsistentNaming

internal static class MappingTestSetup
{
    // 14 chars from EntityId alphabet (ABCDEFGHJKMNPQRSTUVWXYZ0123456789, excluding I, L, O)
    public const string ValidInventoryIdString = "ABCDEFGHJKMNPQ";
    public const string ValidInventoryName = "Test Inventory";
    public const string ValidUpdatedName = "Updated Name";

    public static readonly InventoryId TestInventoryId = InventoryId.Create(
        EntityId.CreateUnsafe(ValidInventoryIdString)
    );

    public static readonly InventoryName TestInventoryName = InventoryName.CreateUnsafe(
        ValidInventoryName
    );

    public static readonly InventoryName TestInventoryName_Updated = InventoryName.CreateUnsafe(
        ValidUpdatedName
    );

    public static PositiveInteger Stock(int count) => PositiveInteger.CreateUnsafe(count);

    public static TRight RightOf<TLeft, TRight>(Either<TLeft, TRight> either) =>
        either.Match(
            Left: l => throw new InvalidOperationException($"Expected Right but got Left: {l}"),
            Right: r => r
        );
}
