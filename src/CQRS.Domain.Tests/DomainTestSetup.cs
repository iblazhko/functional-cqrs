using CQRS.Domain.Inventory;
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

    public static readonly InventoryState TestInventoryState_New = new(
        TestInventoryId,
        CreateTestInventoryName("N/A"),
        None,
        true,
        false
    );

    public static readonly InventoryState TestInventoryState_Current = new(
        TestInventoryId,
        TestInventoryName,
        Some(CreateTestStockQuantity(5)),
        false,
        true
    );

    public static readonly InventoryState TestInventoryState_NoStock = new(
        TestInventoryId,
        TestInventoryName,
        None,
        false,
        true
    );

    public static readonly InventoryState TestInventoryState_Deactivated = new(
        TestInventoryId,
        TestInventoryName,
        None,
        false,
        false
    );
}
