using Inventario.Application.Common;
using Inventario.Application.DTOs.Loans;
using Inventario.Application.Services;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Exceptions;
using Moq;
using Xunit;

namespace Inventario.Tests;

public class LoanServiceTests
{
    private static Loan CreateLoan(params (int AssetId, string Name)[] assets)
    {
        var loan = new Loan
        {
            Id = 1,
            Borrower = "Asociado",
            DueDate = PeruTime.Today.AddDays(2),
            OutAt = DateTime.UtcNow
        };
        foreach (var (assetId, name) in assets)
        {
            loan.Items.Add(new LoanItem
            {
                AssetId = assetId,
                Asset = new Asset
                {
                    Id = assetId,
                    Name = name,
                    Type = AssetType.Individual,
                    OnLoan = true
                }
            });
        }
        return loan;
    }

    [Fact]
    public async Task CreateAsync_IndividualAsset_CreatesLoanAndMarksAssetOnLoan()
    {
        var asset = new Asset
        {
            Id = 1, Name = "Proyector", Code = "P-1",
            Type = AssetType.Individual, Condition = AssetCondition.Good
        };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Asset> { asset });
        var loans = TestHelpers.LoanRepository();
        Loan? created = null;
        loans.Setup(x => x.AddAsync(It.IsAny<Loan>()))
            .Callback<Loan>(l => { l.Id = 99; created = l; })
            .Returns(Task.CompletedTask);
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(loans.Object, assets.Object, uow.Object, new FakeUser());

        var request = new CreateLoanRequest(
            "  Asociado  ", PeruTime.Today.AddDays(3), new List<int> { 1 }, "  nota  ");

        var id = await service.CreateAsync(request);

        Assert.Equal(99, id);
        Assert.NotNull(created);
        Assert.Equal("Asociado", created!.Borrower);
        Assert.Equal("nota", created.Notes);
        Assert.Single(created.Items);
        Assert.True(asset.OnLoan);
        Assert.Contains(asset.Logs, l => l.Action == "Préstamo");
        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_LotAsset_ThrowsAndRollsBack()
    {
        var asset = new Asset
        {
            Id = 1, Name = "Sillas", Type = AssetType.Lot, Quantity = 20
        };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Asset> { asset });
        var loans = TestHelpers.LoanRepository();
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(loans.Object, assets.Object, uow.Object, new FakeUser());

        var request = new CreateLoanRequest(
            "Asociado", PeruTime.Today.AddDays(1), new List<int> { 1 }, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));

        Assert.Contains("individuales", ex.Message);
        loans.Verify(x => x.AddAsync(It.IsAny<Loan>()), Times.Never);
        Assert.False(asset.OnLoan);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AssetAlreadyOnLoan_ThrowsConflict()
    {
        var asset = new Asset
        {
            Id = 1, Name = "Proyector", Type = AssetType.Individual, OnLoan = true
        };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Asset> { asset });
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, assets.Object, uow.Object, new FakeUser());

        var request = new CreateLoanRequest(
            "Asociado", PeruTime.Today.AddDays(1), new List<int> { 1 }, null);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DecommissionedAsset_ThrowsConflict()
    {
        var asset = new Asset
        {
            Id = 1, Name = "Roto", Type = AssetType.Individual,
            Condition = AssetCondition.Decommissioned
        };
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Asset> { asset });
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, assets.Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new CreateLoanRequest(
            "Asociado", PeruTime.Today.AddDays(1), new List<int> { 1 }, null);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_MissingAsset_Throws()
    {
        var assets = TestHelpers.AssetRepository();
        assets.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Asset>());
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, assets.Object, uow.Object, new FakeUser());

        var request = new CreateLoanRequest(
            "Asociado", PeruTime.Today.AddDays(1), new List<int> { 1 }, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NoAssets_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, TestHelpers.AssetRepository().Object,
            uow.Object, new FakeUser());

        var request = new CreateLoanRequest(
            "Asociado", PeruTime.Today.AddDays(1), new List<int>(), null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PastDueDate_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, TestHelpers.AssetRepository().Object,
            uow.Object, new FakeUser());

        var request = new CreateLoanRequest(
            "Asociado", PeruTime.Today.AddDays(-1), new List<int> { 1 }, null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnAsync_EmptyItems_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, TestHelpers.AssetRepository().Object,
            uow.Object, new FakeUser());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ReturnAsync(1, new ReturnLoanRequest(new List<ReturnItemRequest>())));

        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnAsync_DuplicateAsset_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, TestHelpers.AssetRepository().Object,
            uow.Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, AssetCondition.Good),
            new(1, AssetCondition.Fair)
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReturnAsync(1, request));
        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnAsync_InvalidCondition_ThrowsBeforeTransaction()
    {
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            TestHelpers.LoanRepository().Object, TestHelpers.AssetRepository().Object,
            uow.Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, (AssetCondition)99)
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReturnAsync(1, request));
        uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnAsync_AlreadyReturnedLoan_ThrowsAndRollsBack()
    {
        var loan = CreateLoan((1, "Proyector"));
        loan.ReturnedAt = DateTime.UtcNow;
        var loans = TestHelpers.LoanRepository();
        loans.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(loan);
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            loans.Object, TestHelpers.AssetRepository().Object, uow.Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, AssetCondition.Good)
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReturnAsync(1, request));
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnAsync_UnknownLoan_ThrowsNotFoundAndRollsBack()
    {
        var loans = TestHelpers.LoanRepository();
        loans.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync((Loan?)null);
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            loans.Object, TestHelpers.AssetRepository().Object, uow.Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, AssetCondition.Good)
        });

        await Assert.ThrowsAsync<NotFoundException>(() => service.ReturnAsync(1, request));
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnAsync_UnknownAsset_Throws()
    {
        var loan = CreateLoan((1, "Proyector"));
        var loans = TestHelpers.LoanRepository();
        loans.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(loan);
        var service = new LoanService(
            loans.Object, TestHelpers.AssetRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(99, AssetCondition.Good)
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReturnAsync(1, request));
    }

    [Fact]
    public async Task ReturnAsync_ItemAlreadyReturned_Throws()
    {
        var loan = CreateLoan((1, "Proyector"));
        loan.Items[0].ReturnedAt = DateTime.UtcNow;
        loan.Items[0].Asset!.OnLoan = false;
        var loans = TestHelpers.LoanRepository();
        loans.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(loan);
        var service = new LoanService(
            loans.Object, TestHelpers.AssetRepository().Object, TestHelpers.UnitOfWork().Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, AssetCondition.Good)
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReturnAsync(1, request));
    }

    [Fact]
    public async Task ReturnAsync_PartialReturn_MarksOnlySelectedItem()
    {
        var loan = CreateLoan((1, "Proyector"), (2, "Laptop"));
        var loans = TestHelpers.LoanRepository();
        loans.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(loan);
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            loans.Object, TestHelpers.AssetRepository().Object, uow.Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, AssetCondition.Fair)
        });

        var result = await service.ReturnAsync(1, request);

        Assert.False(result.Complete);
        var item1 = loan.Items.Single(i => i.AssetId == 1);
        var item2 = loan.Items.Single(i => i.AssetId == 2);
        Assert.NotNull(item1.ReturnedAt);
        Assert.Equal(AssetCondition.Fair, item1.ReturnCondition);
        Assert.False(item1.Asset!.OnLoan);
        Assert.Equal(AssetCondition.Fair, item1.Asset.Condition);
        Assert.Contains(item1.Asset.Logs, l => l.Action == "Devolución");
        Assert.Null(item2.ReturnedAt);
        Assert.True(item2.Asset!.OnLoan);
        Assert.Null(loan.ReturnedAt);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnAsync_AllItems_CompletesLoan()
    {
        var loan = CreateLoan((1, "Proyector"), (2, "Laptop"));
        var loans = TestHelpers.LoanRepository();
        loans.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(loan);
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            loans.Object, TestHelpers.AssetRepository().Object, uow.Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, AssetCondition.Good),
            new(2, AssetCondition.Poor)
        });

        var result = await service.ReturnAsync(1, request);

        Assert.True(result.Complete);
        Assert.NotNull(loan.ReturnedAt);
        Assert.All(loan.Items, item => Assert.NotNull(item.ReturnedAt));
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnAsync_ItemWithoutAsset_ThrowsAndRollsBack()
    {
        var loan = new Loan
        {
            Id = 1,
            Borrower = "Asociado",
            DueDate = PeruTime.Today.AddDays(2)
        };
        loan.Items.Add(new LoanItem { AssetId = 1, Asset = null });
        var loans = TestHelpers.LoanRepository();
        loans.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(loan);
        var uow = TestHelpers.UnitOfWork();
        var service = new LoanService(
            loans.Object, TestHelpers.AssetRepository().Object, uow.Object, new FakeUser());

        var request = new ReturnLoanRequest(new List<ReturnItemRequest>
        {
            new(1, AssetCondition.Good)
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReturnAsync(1, request));
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
