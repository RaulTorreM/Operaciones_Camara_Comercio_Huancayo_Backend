using Inventario.Application.Common;
using Inventario.Application.DTOs.Summary;
using Inventario.Application.Interfaces;
using Inventario.Core.Interfaces;

namespace Inventario.Application.Services;

public class SummaryService(IAssetRepository assets, ISupplyRepository supplies, ILoanRepository loans) : ISummaryService
{
    public async Task<SummaryDto> GetAsync()
    {
        var counts = await assets.CountsAsync();
        return new SummaryDto(
            counts.Total, counts.Verified, counts.NotFound, counts.Pending,
            await supplies.CountLowAsync(),
            await loans.CountActiveAsync(),
            await loans.CountOverdueAsync(PeruTime.Today));
    }
}
