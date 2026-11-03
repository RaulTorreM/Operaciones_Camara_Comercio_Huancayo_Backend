using Inventario.Application.Common;
using Inventario.Application.DTOs.Assets;
using Inventario.Application.DTOs.Loans;
using Inventario.Application.DTOs.Rooms;
using Inventario.Application.DTOs.Supplies;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Models;

namespace Inventario.Application.Mappings;

public static class InventoryMappings
{
    public static AssetListItemDto ToListItem(this Asset a) => new(
        a.Id, a.Code, a.Name, a.Category, a.Type, a.Quantity, a.RoomId, a.Room?.Name,
        a.Condition, a.Verification, a.OnLoan, a.PhotoUrl != null);

    public static AssetDetailDto ToDetail(this Asset a, IEnumerable<AssetLog> logs) => new(
        a.Id, a.Code, a.Name, a.Category, a.Type, a.Quantity, a.RoomId, a.Room?.Name,
        a.Condition, a.Verification, a.OnLoan, a.Notes, a.LastVerifiedAt, a.PhotoUrl,
        logs.Select(l => new AssetLogDto(l.At, l.User, l.Action, l.Detail)).ToList());

    public static AssetCodeLookupDto ToLookup(this Asset a) => new(
        a.Id, a.Name, a.Type, a.RoomId, a.Room?.Name, a.Verification);

    /// <summary>Versión reducida para la consulta pública por etiqueta.</summary>
    public static PublicAssetDto ToPublic(this Asset a) => new(
        a.Code, a.Name, a.Category, a.Type, a.Quantity, a.Room?.Name, a.Condition, a.Verification, a.OnLoan);

    public static RoomDto ToDto(this RoomStats r) => new(r.Id, r.Name, r.Total, r.Verified, r.NotFound);

    public static SupplyDto ToDto(this Supply s) => new(s.Id, s.Name, s.Unit, s.Mode, s.Stock, s.MinStock, s.Level, s.IsLow);

    public static SupplyMovementDto ToDto(this SupplyMovement m) => new(m.At, m.Type, m.Quantity, m.ResultingStock, m.Destination, m.User);

    public static LoanDto ToDto(this Loan l) => new(
        l.Id, l.Borrower, l.OutAt, l.DueDate, l.ReturnedAt, l.Notes, l.User,
        l.ReturnedAt == null && l.DueDate < PeruTime.Today,
        l.Items.Select(i => new LoanItemDto(
            i.Id, i.AssetId, i.Asset?.Name ?? "", i.Asset?.Code, i.Asset?.Condition ?? AssetCondition.Good,
            i.ReturnedAt != null, i.ReturnCondition)).ToList());
}
