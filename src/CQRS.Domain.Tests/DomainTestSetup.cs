using CQRS.Domain.Inventory;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CQRS.Domain.Tests;

// ReSharper disable InconsistentNaming

internal static class DomainTestSetup
{
    private static InventoryName CreateTestInventoryName(string name) =>
        InventoryName.CreateUnsafe(name);

    public static PositiveInteger CreateTestStockQuantity(int count) =>
        PositiveInteger.CreateUnsafe(count);

    public static readonly InventoryId TestInventoryId = InventoryId.NewId();
    public static readonly InventoryName TestInventoryName = CreateTestInventoryName("INV-123");
    public static readonly InventoryName TestInventoryName_Updated = CreateTestInventoryName(
        "INV-123-Updated"
    );

    // Non-existent inventory (write-side state before any InventoryCreated event)
    public static readonly Option<InventoryState> TestInventoryState_None = None;

    public static readonly InventoryState TestInventoryState_Current = new(
        TestInventoryId,
        TestInventoryName,
        Some(CreateTestStockQuantity(5)),
        true
    );

    public static readonly InventoryState TestInventoryState_NoStock = new(
        TestInventoryId,
        TestInventoryName,
        None,
        true
    );

    public static readonly InventoryState TestInventoryState_Deactivated = new(
        TestInventoryId,
        TestInventoryName,
        None,
        false
    );
}
