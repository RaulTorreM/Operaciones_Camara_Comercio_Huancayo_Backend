using Inventario.Application.DTOs.Assets;
using Inventario.Application.DTOs.Auth;
using Inventario.Application.DTOs.Loans;
using Inventario.Application.DTOs.Rooms;
using Inventario.Application.DTOs.Summary;
using Inventario.Application.DTOs.Supplies;
using Inventario.Core.Enums;

namespace Inventario.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}

public interface IAssetService
{
    Task<List<AssetListItemDto>> SearchAsync(string? q, int? roomId, VerificationStatus? status, AssetType? type);
    Task<AssetDetailDto> GetAsync(int id);
    Task<AssetCodeLookupDto> GetByCodeAsync(string code);
    Task<PublicAssetDto> GetPublicByCodeAsync(string code);
    Task<int> CreateAsync(AssetRequest request);
    Task<int> UpdateAsync(int id, AssetRequest request);
    Task DeleteAsync(int id);
    Task VerifyAsync(int id, VerifyRequest request);
    Task<ImportResultDto> ImportAsync(List<AssetRequest> items);
}

public interface IRoomService
{
    Task<List<RoomDto>> ListAsync();
    Task<RoomDto> CreateAsync(RoomRequest request);
    Task<ResetResultDto> ResetVerificationAsync(int id);
}

public interface ISupplyService
{
    Task<List<SupplyDto>> ListAsync();
    Task<SupplyDto> CreateAsync(SupplyRequest request);
    Task<SupplyDto> RegisterMovementAsync(int id, MovementRequest request);
    Task<SupplyDto> SetLevelAsync(int id, LevelRequest request);
    Task<List<SupplyMovementDto>> GetMovementsAsync(int id);
}

public interface ILoanService
{
    Task<List<LoanDto>> ListAsync();
    Task<int> CreateAsync(CreateLoanRequest request);
    Task<ReturnLoanResultDto> ReturnAsync(int id, ReturnLoanRequest request);
}

public interface ISummaryService
{
    Task<SummaryDto> GetAsync();
}
