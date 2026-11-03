using Inventario.Application.DTOs.Assets;
using Inventario.Application.Services;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Exceptions;
using Moq;
using Xunit;

namespace Inventario.Tests;

public class AssetServiceTests
{
    [Fact]
    public async Task CreateAsync_IndividualWithQuantityFive_SavesQuantityOneAndPending()
    {
        var assets = TestHelpers.AssetRepository();
        var rooms = TestHelpers.RoomRepository();
        var uow = TestHelpers.UnitOfWork();
        var storage = TestHelpers.FileStorage();
        Asset? saved = null;
        assets.Setup(x => x.AddAsync(It.IsAny<Asset>()))
            .Callback<Asset>(a => { a.Id = 42; saved = a; })
            .Returns(Task.CompletedTask);

        var service = new AssetService(assets.Object, rooms.Object, uow.Object, storage.Object, new FakeUser());
        var request = new AssetRequest(
            null, "Proyector", null, AssetType.Individual, 5, null, null,
            AssetCondition.Good, null, null, null);

        var id = await service.CreateAsync(request);

        Assert.Equal(42, id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved!.Quantity);
        Assert.Equal(AssetType.Individual, saved.Type);
        Assert.Equal(VerificationStatus.Pending, saved.Verification);
        Assert.Single(saved.Logs);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LotWithNegativeQuantity_ThrowsAndDoesNotSave()
    {
        var assets = TestHelpers.AssetRepository();
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        var request = new AssetRequest(
            null, "Sillas", null, AssetType.Lot, -1, null, null,
            null, null, null, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
        assets.Verify(x => x.AddAsync(It.IsAny<Asset>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NotFoundVerification_Throws()
    {
        var service = new AssetService(
            TestHelpers.AssetRepository().Object, TestHelpers.RoomRepository().Object,
            TestHelpers.UnitOfWork().Object, TestHelpers.FileStorage().Object, new FakeUser());

        var request = new AssetRequest(
            null, "Bien", null, AssetType.Individual, 1, null, null,
            null, VerificationStatus.NotFound, null, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_InvalidEnumValues_Throw()
    {
        var service = new AssetService(
            TestHelpers.AssetRepository().Object, TestHelpers.RoomRepository().Object,
            TestHelpers.UnitOfWork().Object, TestHelpers.FileStorage().Object, new FakeUser());

        var invalidType = new AssetRequest(
            null, "Bien", null, (AssetType)99, 1, null, null, null, null, null, null);
        var invalidCondition = new AssetRequest(
            null, "Bien", null, AssetType.Individual, 1, null, null, (AssetCondition)99, null, null, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(invalidType));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(invalidCondition));
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsConflictWithExistingId()
    {
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByCodeAsync("ABC"))
            .ReturnsAsync(new Asset { Id = 7, Name = "Existente", Code = "ABC" });

        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());
        var request = new AssetRequest(
            "ABC", "Nuevo", null, AssetType.Individual, 1, null, null,
            null, null, null, null);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));
        Assert.Equal(7, ex.ExistingId);
        assets.Verify(x => x.AddAsync(It.IsAny<Asset>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithoutConditionOrType_PreservesCurrentValues()
    {
        var asset = new Asset
        {
            Id = 1, Name = "Viejo", Type = AssetType.Lot, Quantity = 10,
            Condition = AssetCondition.Fair, Verification = VerificationStatus.Verified
        };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(asset);
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        var request = new AssetRequest(
            null, "Nuevo", null, null, null, null, null, null, null, null, null);

        await service.UpdateAsync(1, request);

        Assert.Equal("Nuevo", asset.Name);
        Assert.Equal(AssetType.Lot, asset.Type);
        Assert.Equal(AssetCondition.Fair, asset.Condition);
        Assert.Equal(10, asset.Quantity);
        Assert.Equal(VerificationStatus.Verified, asset.Verification);
    }

    [Fact]
    public async Task UpdateAsync_ChangingToIndividual_ForcesQuantityOne()
    {
        var asset = new Asset { Id = 1, Name = "Lote", Type = AssetType.Lot, Quantity = 25 };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(asset);
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        var request = new AssetRequest(
            null, "Lote", null, AssetType.Individual, 25, null, null, null, null, null, null);

        await service.UpdateAsync(1, request);

        Assert.Equal(AssetType.Individual, asset.Type);
        Assert.Equal(1, asset.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_UnknownRoom_Throws()
    {
        var asset = new Asset { Id = 1, Name = "Bien", RoomId = null };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(asset);
        var rooms = TestHelpers.RoomRepository();
        rooms.Setup(x => x.GetByIdAsync(5)).ReturnsAsync((Room?)null);
        var service = new AssetService(
            assets.Object, rooms.Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        var request = new AssetRequest(
            null, "Bien", null, null, null, 5, null, null, null, null, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task VerifyAsync_PendingResult_ThrowsBeforeLoading()
    {
        var assets = TestHelpers.AssetRepository();
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.VerifyAsync(1, new VerifyRequest(VerificationStatus.Pending, null, null)));

        assets.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task VerifyAsync_InvalidEnum_Throws()
    {
        var service = new AssetService(
            TestHelpers.AssetRepository().Object, TestHelpers.RoomRepository().Object,
            TestHelpers.UnitOfWork().Object, TestHelpers.FileStorage().Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.VerifyAsync(1, new VerifyRequest((VerificationStatus)99, null, null)));
    }

    [Fact]
    public async Task VerifyAsync_VerifiedLot_UpdatesQuantityAndVerification()
    {
        var asset = new Asset
        {
            Id = 1, Name = "Sillas", Type = AssetType.Lot, Quantity = 10,
            Verification = VerificationStatus.Pending
        };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(asset);
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        await service.VerifyAsync(1, new VerifyRequest(VerificationStatus.Verified, null, 7));

        Assert.Equal(VerificationStatus.Verified, asset.Verification);
        Assert.Equal(7, asset.Quantity);
        Assert.NotNull(asset.LastVerifiedAt);
        Assert.Contains(asset.Logs, l => l.Action == "Verificación");
    }

    [Fact]
    public async Task DeleteAsync_OnLoan_ThrowsConflict()
    {
        var asset = new Asset { Id = 1, Name = "Prestado", OnLoan = true };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(asset);
        var uow = TestHelpers.UnitOfWork();
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, uow.Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(1));

        assets.Verify(x => x.Remove(It.IsAny<Asset>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithLoanHistory_ThrowsConflict()
    {
        var asset = new Asset { Id = 1, Name = "Con historial" };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(asset);
        assets.Setup(x => x.HasLoanHistoryAsync(1)).ReturnsAsync(true);
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(1));
        assets.Verify(x => x.Remove(It.IsAny<Asset>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithPhoto_DeletesCloudinaryImageAfterSave()
    {
        var asset = new Asset
        {
            Id = 1, Name = "Con foto", PhotoUrl = "https://example.test/foto.jpg"
        };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(asset);
        var storage = TestHelpers.FileStorage();
        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            storage.Object, new FakeUser());

        await service.DeleteAsync(1);

        assets.Verify(x => x.Remove(asset), Times.Once);
        storage.Verify(x => x.DeleteAsync("https://example.test/foto.jpg"), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_ForcesIndividualQuantityAndPendingVerification()
    {
        var assets = TestHelpers.AssetRepository();
        var uow = TestHelpers.UnitOfWork();
        List<Asset>? added = null;
        assets.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Asset>>()))
            .Callback<IEnumerable<Asset>>(list => added = list.ToList())
            .Returns(Task.CompletedTask);

        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, uow.Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        var items = new List<AssetRequest>
        {
            new(null, "Individual", null, AssetType.Individual, 5, null, null, null, null, null, null),
            new(null, "Lote", null, AssetType.Lot, 12, null, null, null, null, null, null)
        };

        var result = await service.ImportAsync(items);

        Assert.Equal(2, result.Created);
        Assert.Equal(0, result.Skipped);
        Assert.NotNull(added);
        Assert.Equal(1, added![0].Quantity);
        Assert.Equal(VerificationStatus.Pending, added[0].Verification);
        Assert.Equal(12, added[1].Quantity);
    }

    [Fact]
    public async Task ImportAsync_SkipsInvalidAndDuplicateRows()
    {
        var assets = TestHelpers.AssetRepository();
        List<Asset>? added = null;
        assets.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Asset>>()))
            .Callback<IEnumerable<Asset>>(list => added = list.ToList())
            .Returns(Task.CompletedTask);

        var service = new AssetService(
            assets.Object, TestHelpers.RoomRepository().Object, TestHelpers.UnitOfWork().Object,
            TestHelpers.FileStorage().Object, new FakeUser());

        var items = new List<AssetRequest>
        {
            new("A-1", "Uno", null, AssetType.Individual, 1, null, null, null, null, null, null),
            new("a-1", "Duplicado", null, AssetType.Individual, 1, null, null, null, null, null, null),
            new(null, "", null, AssetType.Individual, 1, null, null, null, null, null, null),
            new(null, "Tipo inválido", null, (AssetType)99, 1, null, null, null, null, null, null)
        };

        var result = await service.ImportAsync(items);

        Assert.Equal(1, result.Created);
        Assert.Equal(3, result.Skipped);
        Assert.NotNull(added);
        Assert.Single(added!);
    }
}
