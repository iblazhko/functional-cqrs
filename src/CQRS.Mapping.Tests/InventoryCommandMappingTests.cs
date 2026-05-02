using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.Mapping.Inventory.V1;
using Shouldly;
using static CQRS.Mapping.Tests.MappingTestSetup;

namespace CQRS.Mapping.Tests;

public sealed class InventoryCommandV1Mapper_ToCommandDto_Tests
{
    private readonly InventoryCommandV1Mapper _mapper = new();

    [Fact]
    public void CreateInventory_MapsToDto()
    {
        var domain = new CreateInventory(TestInventoryId, TestInventoryName);

        var dto = (CreateInventoryCommand)_mapper.ToCommandDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.Name.ShouldBe(ValidInventoryName);
    }

    [Fact]
    public void RenameInventory_MapsToDto()
    {
        var domain = new RenameInventory(TestInventoryId, TestInventoryName_Updated);

        var dto = (RenameInventoryCommand)_mapper.ToCommandDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.NewName.ShouldBe(ValidUpdatedName);
    }

    [Fact]
    public void AddItemsToInventory_MapsToDto()
    {
        var domain = new AddItemsToInventory(TestInventoryId, Stock(10));

        var dto = (AddItemsToInventoryCommand)_mapper.ToCommandDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.Count.ShouldBe(10);
    }

    [Fact]
    public void RemoveItemsFromInventory_MapsToDto()
    {
        var domain = new RemoveItemsFromInventory(TestInventoryId, Stock(3));

        var dto = (RemoveItemsFromInventoryCommand)_mapper.ToCommandDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.Count.ShouldBe(3);
    }

    [Fact]
    public void DeactivateInventory_MapsToDto()
    {
        var domain = new DeactivateInventory(TestInventoryId);

        var dto = (DeactivateInventoryCommand)_mapper.ToCommandDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
    }

    [Fact]
    public void UnknownCommandType_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() =>
            _mapper.ToCommandDto(new UnknownDomainCommand(TestInventoryId))
        );
    }

    private sealed record UnknownDomainCommand(InventoryId Id) : IInventoryCommand;
}

public sealed class InventoryCommandV1Mapper_ToDomainCommand_Tests
{
    private readonly InventoryCommandV1Mapper _mapper = new();

    [Fact]
    public void CreateInventoryCommand_MappsToDomain()
    {
        var dto = new CreateInventoryCommand { InventoryId = ValidInventoryIdString, Name = ValidInventoryName };

        var result = _mapper.ToDomainCommand(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (CreateInventory)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
        domain.Name.ToString().ShouldBe(ValidInventoryName);
    }

    [Fact]
    public void RenameInventoryCommand_MappsToDomain()
    {
        var dto = new RenameInventoryCommand { InventoryId = ValidInventoryIdString, NewName = ValidUpdatedName };

        var result = _mapper.ToDomainCommand(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (RenameInventory)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
        domain.NewName.ToString().ShouldBe(ValidUpdatedName);
    }

    [Fact]
    public void AddItemsToInventoryCommand_MappsToDomain()
    {
        var dto = new AddItemsToInventoryCommand { InventoryId = ValidInventoryIdString, Count = 7 };

        var result = _mapper.ToDomainCommand(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (AddItemsToInventory)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
        ((int)domain.Count).ShouldBe(7);
    }

    [Fact]
    public void RemoveItemsFromInventoryCommand_MappsToDomain()
    {
        var dto = new RemoveItemsFromInventoryCommand { InventoryId = ValidInventoryIdString, Count = 4 };

        var result = _mapper.ToDomainCommand(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (RemoveItemsFromInventory)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
        ((int)domain.Count).ShouldBe(4);
    }

    [Fact]
    public void DeactivateInventoryCommand_MappsToDomain()
    {
        var dto = new DeactivateInventoryCommand { InventoryId = ValidInventoryIdString };

        var result = _mapper.ToDomainCommand(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (DeactivateInventory)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
    }

    [Fact]
    public void InvalidInventoryId_ReturnsLeft()
    {
        var dto = new CreateInventoryCommand { InventoryId = "invalid-id", Name = ValidInventoryName };

        var result = _mapper.ToDomainCommand(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void EmptyInventoryName_ReturnsLeft()
    {
        var dto = new CreateInventoryCommand { InventoryId = ValidInventoryIdString, Name = "" };

        var result = _mapper.ToDomainCommand(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void NameExceedingMaxLength_ReturnsLeft()
    {
        var dto = new CreateInventoryCommand
        {
            InventoryId = ValidInventoryIdString,
            Name = new string('x', 51),
        };

        var result = _mapper.ToDomainCommand(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void InvalidItemCount_ReturnsLeft()
    {
        var dto = new AddItemsToInventoryCommand { InventoryId = ValidInventoryIdString, Count = 0 };

        var result = _mapper.ToDomainCommand(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void UnknownDtoType_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() =>
            _mapper.ToDomainCommand(new UnknownCommandDto())
        );
    }

    private sealed record UnknownCommandDto : IInventoryCommandDto;
}
