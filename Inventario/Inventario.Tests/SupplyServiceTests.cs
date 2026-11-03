using Inventario.Application.DTOs.Supplies;
using Inventario.Application.Services;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Exceptions;
using Moq;
using Xunit;

namespace Inventario.Tests;

public class SupplyServiceTests
{
    private static Supply QuantitySupply(int stock = 10, int minStock = 2) => new()
    {
        Id = 1, Name = "Hojas", Unit = "unid.", Mode = SupplyMode.Quantity,
        Stock = stock, MinStock = minStock, Level = 3
    };

    [Fact]
    public async Task CreateAsync_QuantityWithNegativeStock_Throws()
    {
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new SupplyRequest(
            "Hojas", "unid.", SupplyMode.Quantity, -1, 0, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_QuantityWithNegativeMinStock_Throws()
    {
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new SupplyRequest(
            "Hojas", "unid.", SupplyMode.Quantity, 10, -1, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Visual_ForcesZeroStockAndKeepsLevel()
    {
        var supplies = TestHelpers.SupplyRepository();
        Supply? saved = null;
        supplies.Setup(x => x.AddAsync(It.IsAny<Supply>()))
            .Callback<Supply>(s => { s.Id = 5; saved = s; })
            .Returns(Task.CompletedTask);
        var service = new SupplyService(supplies.Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new SupplyRequest(
            "  Papel higiénico  ", null, SupplyMode.Visual, 50, 10, 2);

        var dto = await service.CreateAsync(request);

        Assert.Equal(5, dto.Id);
        Assert.NotNull(saved);
        Assert.Equal("Papel higiénico", saved!.Name);
        Assert.Equal("", saved.Unit);
        Assert.Equal(SupplyMode.Visual, saved.Mode);
        Assert.Equal(0, saved.Stock);
        Assert.Equal(0, saved.MinStock);
        Assert.Equal(2, saved.Level);
    }

    [Fact]
    public async Task CreateAsync_Quantity_ForcesLevelThree()
    {
        var supplies = TestHelpers.SupplyRepository();
        Supply? saved = null;
        supplies.Setup(x => x.AddAsync(It.IsAny<Supply>()))
            .Callback<Supply>(s => saved = s)
            .Returns(Task.CompletedTask);
        var service = new SupplyService(supplies.Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new SupplyRequest(
            "Hojas", "unid.", SupplyMode.Quantity, 5, 1, 1);

        await service.CreateAsync(request);

        Assert.NotNull(saved);
        Assert.Equal(SupplyMode.Quantity, saved!.Mode);
        Assert.Equal(5, saved.Stock);
        Assert.Equal(1, saved.MinStock);
        Assert.Equal(3, saved.Level);
    }

    [Fact]
    public async Task CreateAsync_InvalidLevel_Throws()
    {
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new SupplyRequest(
            "Hojas", "unid.", SupplyMode.Visual, 0, 0, 4);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_InvalidMode_Throws()
    {
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new SupplyRequest(
            "Hojas", "unid.", (SupplyMode)99, 1, 0, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_EmptyName_Throws()
    {
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new SupplyRequest(
            "   ", "unid.", SupplyMode.Quantity, 1, 0, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task RegisterMovementAsync_OutMoreThanStock_ThrowsAndRollsBack()
    {
        var supply = QuantitySupply(stock: 3);
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(supply);
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(supplies.Object, uow.Object, new FakeUser());

        var request = new MovementRequest(MovementType.Out, 5, "Sala A");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegisterMovementAsync(1, request));

        Assert.Equal(3, supply.Stock);
        Assert.Empty(supply.Movements);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterMovementAsync_OutValid_DecrementsStockAndRecordsMovement()
    {
        var supply = QuantitySupply(stock: 10);
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(supply);
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(supplies.Object, uow.Object, new FakeUser());

        var request = new MovementRequest(MovementType.Out, 4, "  Sala A  ");

        var dto = await service.RegisterMovementAsync(1, request);

        Assert.Equal(6, supply.Stock);
        Assert.Equal(6, dto.Stock);
        var movement = Assert.Single(supply.Movements);
        Assert.Equal(MovementType.Out, movement.Type);
        Assert.Equal(4, movement.Quantity);
        Assert.Equal(6, movement.ResultingStock);
        Assert.Equal("Sala A", movement.Destination);
        Assert.Equal("tester", movement.User);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterMovementAsync_InValid_IncrementsStock()
    {
        var supply = QuantitySupply(stock: 1);
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(supply);
        var service = new SupplyService(
            supplies.Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var dto = await service.RegisterMovementAsync(
            1, new MovementRequest(MovementType.In, 2, null));

        Assert.Equal(3, supply.Stock);
        Assert.Equal(3, dto.Stock);
        Assert.Single(supply.Movements);
    }

    [Fact]
    public async Task RegisterMovementAsync_InOverflow_ThrowsAndRollsBack()
    {
        var supply = QuantitySupply(stock: int.MaxValue - 1);
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(supply);
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(supplies.Object, uow.Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegisterMovementAsync(1, new MovementRequest(MovementType.In, 2, null)));

        Assert.Equal(int.MaxValue - 1, supply.Stock);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterMovementAsync_AdjustmentZero_SetsStockToZero()
    {
        var supply = QuantitySupply(stock: 10);
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(supply);
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(supplies.Object, uow.Object, new FakeUser());

        var dto = await service.RegisterMovementAsync(
            1, new MovementRequest(MovementType.Adjustment, 0, null));

        Assert.Equal(0, supply.Stock);
        Assert.Equal(0, dto.Stock);
        Assert.Single(supply.Movements);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterMovementAsync_VisualSupply_ThrowsAndRollsBack()
    {
        var supply = new Supply
        {
            Id = 1, Name = "Papel", Mode = SupplyMode.Visual, Level = 2
        };
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(supply);
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(supplies.Object, uow.Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegisterMovementAsync(1, new MovementRequest(MovementType.Out, 1, null)));

        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterMovementAsync_ZeroOut_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, uow.Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegisterMovementAsync(1, new MovementRequest(MovementType.Out, 0, null)));

        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterMovementAsync_NegativeQuantity_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, uow.Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegisterMovementAsync(1, new MovementRequest(MovementType.In, -1, null)));

        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterMovementAsync_NullType_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, uow.Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegisterMovementAsync(1, new MovementRequest(null, 1, null)));

        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterMovementAsync_InvalidType_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, uow.Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegisterMovementAsync(1, new MovementRequest((MovementType)99, 1, null)));

        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterMovementAsync_SupplyNotFound_ThrowsNotFoundAndRollsBack()
    {
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync((Supply?)null);
        var uow = TestHelpers.UnitOfWork();
        var service = new SupplyService(supplies.Object, uow.Object, new FakeUser());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.RegisterMovementAsync(1, new MovementRequest(MovementType.Out, 1, null)));

        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetLevelAsync_InvalidLevel_Throws()
    {
        var service = new SupplyService(
            TestHelpers.SupplyRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetLevelAsync(1, new LevelRequest(4)));
    }

    [Fact]
    public async Task SetLevelAsync_QuantityMode_Throws()
    {
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(QuantitySupply());
        var service = new SupplyService(
            supplies.Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetLevelAsync(1, new LevelRequest(2)));
    }

    [Fact]
    public async Task SetLevelAsync_VisualSupply_UpdatesLevel()
    {
        var supply = new Supply
        {
            Id = 1, Name = "Papel", Mode = SupplyMode.Visual, Level = 2
        };
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(supply);
        var service = new SupplyService(
            supplies.Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var dto = await service.SetLevelAsync(1, new LevelRequest(1));

        Assert.Equal(1, supply.Level);
        Assert.Equal(1, dto.Level);
    }

    [Fact]
    public async Task SetLevelAsync_SupplyNotFound_Throws()
    {
        var supplies = TestHelpers.SupplyRepository();
        supplies.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Supply?)null);
        var service = new SupplyService(
            supplies.Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SetLevelAsync(1, new LevelRequest(2)));
    }
}
