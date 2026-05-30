using CQRS.Domain.Failures;
using CQRS.EntityIds;

namespace CQRS.Domain.Inventory;

public sealed record InventoryId
{
    private EntityId Value { get; }

    private InventoryId(EntityId value)
    {
        Value = value;
    }

    public static implicit operator EntityId(InventoryId id) => id.Value;

    public static implicit operator string(InventoryId id) => id.Value;

    public override string ToString() => Value;

    public static InventoryId NewId() => new(EntityId.NewId());

    public static InventoryId Create(EntityId value) => new(value);
}

public sealed record InventoryName
{
    private MediumString Name { get; }

    private InventoryName(MediumString name)
    {
        Name = name;
    }

    public static implicit operator string(InventoryName name) => name.Name;

    public override string ToString() => Name;

    public static InventoryName Create(MediumString name) => new(name);

    public static Either<ValidationFault, InventoryName> Create(string value) =>
        MediumString.Create(value).Map(Create);

    internal static InventoryName CreateUnsafe(string value) =>
        new(MediumString.CreateUnsafe(value));
}

public sealed record InventoryState(
    InventoryId Id,
    InventoryName Name,
    Option<PositiveInteger> Quantity,
    bool IsActive
);
