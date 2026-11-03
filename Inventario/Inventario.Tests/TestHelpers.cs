using Inventario.Application.Interfaces;
using Inventario.Core.Entities;
using Inventario.Core.Interfaces;
using Moq;

namespace Inventario.Tests;

internal sealed class FakeUser(string name = "tester") : ICurrentUser
{
    public string Name { get; } = name;
}

internal static class TestHelpers
{
    public static Mock<IUnitOfWork> UnitOfWork()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return uow;
    }

    public static Mock<IAssetRepository> AssetRepository()
    {
        var assets = new Mock<IAssetRepository>();
        assets.Setup(x => x.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);
        assets.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Asset?)null);
        assets.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Asset>());
        assets.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Asset>());
        assets.Setup(x => x.GetAllCodesAsync()).ReturnsAsync(new HashSet<string>());
        assets.Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<AssetLog>());
        assets.Setup(x => x.HasLoanHistoryAsync(It.IsAny<int>())).ReturnsAsync(false);
        assets.Setup(x => x.AddAsync(It.IsAny<Asset>())).Returns(Task.CompletedTask);
        assets.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Asset>>())).Returns(Task.CompletedTask);
        return assets;
    }

    public static Mock<IRoomRepository> RoomRepository()
    {
        var rooms = new Mock<IRoomRepository>();
        rooms.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Room>());
        rooms.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Room?)null);
        rooms.Setup(x => x.NameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        rooms.Setup(x => x.AddAsync(It.IsAny<Room>())).Returns(Task.CompletedTask);
        rooms.Setup(x => x.ResetVerificationAsync(It.IsAny<int>())).ReturnsAsync(0);
        return rooms;
    }

    public static Mock<IFileStorage> FileStorage()
    {
        var storage = new Mock<IFileStorage>();
        storage.Setup(x => x.SaveImageAsync(It.IsAny<string>())).ReturnsAsync("https://example.test/foto.jpg");
        storage.Setup(x => x.DeleteAsync(It.IsAny<string?>())).Returns(Task.CompletedTask);
        return storage;
    }

    public static Mock<ILoanRepository> LoanRepository()
    {
        var loans = new Mock<ILoanRepository>();
        loans.Setup(x => x.ListAsync()).ReturnsAsync(new List<Loan>());
        loans.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Loan?)null);
        loans.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<int>())).ReturnsAsync((Loan?)null);
        loans.Setup(x => x.AddAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);
        return loans;
    }

    public static Mock<ISupplyRepository> SupplyRepository()
    {
        var supplies = new Mock<ISupplyRepository>();
        supplies.Setup(x => x.ListAsync()).ReturnsAsync(new List<Supply>());
        supplies.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Supply?)null);
        supplies.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<int>())).ReturnsAsync((Supply?)null);
        supplies.Setup(x => x.GetMovementsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<SupplyMovement>());
        supplies.Setup(x => x.AddAsync(It.IsAny<Supply>())).Returns(Task.CompletedTask);
        return supplies;
    }
}
