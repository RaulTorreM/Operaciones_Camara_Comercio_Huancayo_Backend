using Inventario.Application.Common;
using Inventario.Application.DTOs.Assets;
using Inventario.Application.Interfaces;
using Inventario.Application.Mappings;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Exceptions;
using Inventario.Core.Interfaces;

namespace Inventario.Application.Services;

public class AssetService(
    IAssetRepository assets, IRoomRepository rooms, IUnitOfWork uow, IFileStorage storage, ICurrentUser user) : IAssetService
{
    private const int MaxPhotoChars = 4_000_000;
    private const int MaxNameLength = 200;
    private const int MaxCodeLength = 60;
    private const int MaxRoomNameLength = 120;

    public async Task<List<AssetListItemDto>> SearchAsync(string? q, int? roomId, VerificationStatus? status, AssetType? type) =>
        (await assets.SearchAsync(q, roomId, status, type, 1000)).Select(a => a.ToListItem()).ToList();

    public async Task<AssetDetailDto> GetAsync(int id)
    {
        var asset = await assets.GetByIdAsync(id) ?? throw new NotFoundException("No se encontró el bien.");
        return asset.ToDetail(await assets.GetLogsAsync(id, 20));
    }

    public async Task<AssetCodeLookupDto> GetByCodeAsync(string code)
    {
        var normalized = code?.Trim() ?? "";
        if (normalized.Length == 0) throw new BusinessRuleException("Escribe un código.");
        Validation.MaxLength(normalized, MaxCodeLength, "El código no debe superar 60 caracteres.");

        var asset = await assets.GetByCodeAsync(normalized)
            ?? throw new NotFoundException("No hay ningún bien con ese código.");
        return asset.ToLookup();
    }

    /// <summary>
    /// Consulta pública (sin sesión) usada desde el login: devuelve solo datos básicos del bien.
    /// No permite modificarlo y no expone notas, historial ni fotos.
    /// </summary>
    public async Task<PublicAssetDto> GetPublicByCodeAsync(string code)
    {
        var normalized = code?.Trim() ?? "";
        if (normalized.Length == 0) throw new BusinessRuleException("Escribe un código.");
        Validation.MaxLength(normalized, MaxCodeLength, "El código no debe superar 60 caracteres.");

        var asset = await assets.GetByCodeAsync(normalized)
            ?? throw new NotFoundException("No hay ningún bien con ese código.");
        return asset.ToPublic();
    }

    public async Task<int> CreateAsync(AssetRequest r)
    {
        Validation.CheckEnum(r.Type, "Tipo de bien no válido.");
        Validation.CheckEnum(r.Condition, "Estado del bien no válido.");
        Validation.CheckEnum(r.Verification, "Estado de verificación no válido.");
        if (r.Verification == VerificationStatus.NotFound)
            throw new BusinessRuleException("Un bien nuevo no puede marcarse como no encontrado.");

        var name = PeruTime.Clean(r.Name) ?? throw new BusinessRuleException("El nombre es obligatorio.");
        Validation.MaxLength(name, MaxNameLength, "El nombre no debe superar 200 caracteres.");
        CheckPhoto(r.PhotoDataUrl);
        await CheckRoomAsync(r.RoomId);

        var code = PeruTime.Clean(r.Code);
        Validation.MaxLength(code, MaxCodeLength, "El código no debe superar 60 caracteres.");
        if (code != null)
        {
            var dup = await assets.GetByCodeAsync(code);
            if (dup != null) throw new ConflictException($"Ese código ya está asignado a: {dup.Name}", dup.Id);
        }

        var type = r.Type ?? AssetType.Individual;
        var quantity = InitialQuantity(type, r.Quantity);

        // Solo un "Verified" explícito nace verificado. Si no se envía, queda por verificar.
        var verified = r.Verification == VerificationStatus.Verified;
        var asset = new Asset
        {
            Code = code,
            Name = name,
            Category = PeruTime.Clean(r.Category),
            Type = type,
            Quantity = quantity,
            RoomId = r.RoomId,
            Condition = r.Condition ?? AssetCondition.Good,
            Verification = verified ? VerificationStatus.Verified : VerificationStatus.Pending,
            LastVerifiedAt = verified ? DateTime.UtcNow : null,
            Notes = PeruTime.Clean(r.Notes)
        };

        string? newPhoto = null;
        if (r.PhotoDataUrl != null)
        {
            newPhoto = await storage.SaveImageAsync(r.PhotoDataUrl);
            asset.PhotoUrl = newPhoto;
        }

        asset.Logs.Add(NewLog("Registro", code != null ? $"Etiqueta {code}" : "Sin etiqueta"));

        await assets.AddAsync(asset);
        try
        {
            await uow.SaveChangesAsync();
        }
        catch
        {
            // Si no se pudo guardar el bien, la foto nueva quedaría huérfana en Cloudinary.
            if (newPhoto != null) await storage.DeleteAsync(newPhoto);
            throw;
        }

        return asset.Id;
    }

    public async Task<int> UpdateAsync(int id, AssetRequest r)
    {
        Validation.CheckEnum(r.Type, "Tipo de bien no válido.");
        Validation.CheckEnum(r.Condition, "Estado del bien no válido.");
        Validation.CheckEnum(r.Verification, "Estado de verificación no válido.");

        var asset = await assets.GetByIdAsync(id) ?? throw new NotFoundException("No se encontró el bien.");
        var name = PeruTime.Clean(r.Name) ?? throw new BusinessRuleException("El nombre es obligatorio.");
        Validation.MaxLength(name, MaxNameLength, "El nombre no debe superar 200 caracteres.");
        CheckPhoto(r.PhotoDataUrl);

        var code = PeruTime.Clean(r.Code);
        Validation.MaxLength(code, MaxCodeLength, "El código no debe superar 60 caracteres.");
        if (code != null && !string.Equals(code, asset.Code, StringComparison.OrdinalIgnoreCase))
        {
            var dup = await assets.GetByCodeAsync(code);
            if (dup != null && dup.Id != id) throw new ConflictException($"Ese código ya está asignado a: {dup.Name}", dup.Id);
        }

        var newType = r.Type ?? asset.Type;
        var newQuantity = UpdatedQuantity(newType, asset, r.Quantity);

        var changes = new List<string>();
        if (r.RoomId != asset.RoomId)
        {
            var newRoom = r.RoomId.HasValue
                ? await rooms.GetByIdAsync(r.RoomId.Value) ?? throw new BusinessRuleException("La sala indicada no existe.")
                : null;
            changes.Add($"sala: {asset.Room?.Name ?? "sin sala"} → {newRoom?.Name ?? "sin sala"}");
            asset.Room = newRoom;
            asset.RoomId = newRoom?.Id;
        }

        // Si el cliente no manda el estado o el tipo, se conserva el valor actual.
        if (r.Condition.HasValue && r.Condition.Value != asset.Condition)
        {
            changes.Add($"estado: {asset.Condition} → {r.Condition.Value}");
            asset.Condition = r.Condition.Value;
        }

        if (code != asset.Code)
        {
            changes.Add($"etiqueta: {code ?? "quitada"}");
            asset.Code = code;
        }

        if (newType != asset.Type)
        {
            changes.Add($"tipo: {asset.Type} → {newType}");
            asset.Type = newType;
        }

        if (newQuantity != asset.Quantity)
        {
            changes.Add($"cantidad: {asset.Quantity} → {newQuantity}");
            asset.Quantity = newQuantity;
        }

        asset.Name = name;
        asset.Category = PeruTime.Clean(r.Category);
        asset.Notes = PeruTime.Clean(r.Notes);

        string? oldPhoto = null;
        string? newPhoto = null;
        if (r.PhotoDataUrl != null)
        {
            oldPhoto = asset.PhotoUrl;
            newPhoto = await storage.SaveImageAsync(r.PhotoDataUrl);
            asset.PhotoUrl = newPhoto;
            changes.Add("foto actualizada");
        }

        asset.UpdatedAt = DateTime.UtcNow;
        asset.Logs.Add(NewLog("Edición", changes.Count > 0 ? string.Join(" · ", changes) : null));

        try
        {
            await uow.SaveChangesAsync();
        }
        catch
        {
            // Si falla el guardado, la imagen nueva quedaría huérfana en Cloudinary.
            if (newPhoto != null) await storage.DeleteAsync(newPhoto);
            throw;
        }

        // La imagen anterior ya no se usa: se borra también de Cloudinary.
        if (newPhoto != null) await storage.DeleteAsync(oldPhoto);
        return asset.Id;
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await assets.GetByIdAsync(id) ?? throw new NotFoundException("No se encontró el bien.");

        if (asset.OnLoan)
            throw new ConflictException("No se puede eliminar un bien que está prestado. Regístralo primero como devuelto.");

        if (await assets.HasLoanHistoryAsync(id))
            throw new ConflictException("No se puede eliminar: el bien tiene préstamos registrados en el historial.");

        var photo = asset.PhotoUrl;
        assets.Remove(asset);
        await uow.SaveChangesAsync();

        // Se borra después del commit: si Cloudinary falla, el bien ya no existe en la base
        // y solo queda un archivo huérfano (se registra en el log).
        if (!string.IsNullOrWhiteSpace(photo)) await storage.DeleteAsync(photo);
    }

    public async Task VerifyAsync(int id, VerifyRequest r)
    {
        Validation.CheckEnum(r.Result, "Resultado de verificación no válido.");
        if (r.Result is not (VerificationStatus.Verified or VerificationStatus.NotFound))
            throw new BusinessRuleException("Indica si el bien fue encontrado o no.");

        var asset = await assets.GetByIdAsync(id) ?? throw new NotFoundException("No se encontró el bien.");
        var result = r.Result.Value;
        var detail = result == VerificationStatus.Verified ? "Encontrado" : "No encontrado";

        if (result == VerificationStatus.Verified && r.RoomId.HasValue && r.RoomId != asset.RoomId)
        {
            var newRoom = await rooms.GetByIdAsync(r.RoomId.Value)
                ?? throw new BusinessRuleException("La sala indicada no existe.");

            detail += $" · movido de {asset.Room?.Name ?? "sin sala"} a {newRoom.Name}";
            asset.Room = newRoom;
            asset.RoomId = newRoom.Id;
        }

        if (result == VerificationStatus.Verified && asset.Type == AssetType.Lot && r.Quantity.HasValue)
        {
            if (r.Quantity.Value < 0)
                throw new BusinessRuleException("La cantidad de un lote no puede ser negativa.");

            if (r.Quantity.Value != asset.Quantity)
            {
                detail += $" · cantidad {asset.Quantity} → {r.Quantity.Value}";
                asset.Quantity = r.Quantity.Value;
            }
        }

        asset.Verification = result;
        asset.LastVerifiedAt = DateTime.UtcNow;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.Logs.Add(NewLog("Verificación", detail));
        await uow.SaveChangesAsync();
    }

    /// <summary>Importación desde el Excel anterior: los bienes entran como "por verificar".</summary>
    public async Task<ImportResultDto> ImportAsync(List<AssetRequest> items)
    {
        if (items.Count == 0) throw new BusinessRuleException("No hay filas para importar.");
        if (items.Count > 3000) throw new BusinessRuleException("Máximo 3000 filas por importación.");

        var roomMap = new Dictionary<string, Room>();
        foreach (var room in await rooms.GetAllAsync())
            roomMap.TryAdd(room.Name.Trim().ToLowerInvariant(), room);

        var existingCodes = (await assets.GetAllCodesAsync())
            .Select(c => c.Trim())
            .Where(c => c.Length > 0);
        var codes = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Asset>();
        int skipped = 0;
        foreach (var r in items)
        {
            if (!Validation.IsValidEnum(r.Type) || !Validation.IsValidEnum(r.Condition))
            {
                skipped++;
                continue;
            }

            var name = PeruTime.Clean(r.Name);
            if (name is null || name.Length > MaxNameLength)
            {
                skipped++;
                continue;
            }

            var code = PeruTime.Clean(r.Code);
            if (code is { Length: > MaxCodeLength })
            {
                skipped++;
                continue;
            }
            if (code != null && !codes.Add(code))
            {
                skipped++;
                continue;
            }

            Room? room = null;
            var roomName = PeruTime.Clean(r.RoomName);
            if (roomName is { Length: > MaxRoomNameLength })
            {
                skipped++;
                continue;
            }
            if (roomName != null && !roomMap.TryGetValue(roomName.ToLowerInvariant(), out room))
            {
                room = new Room { Name = roomName };
                roomMap[roomName.ToLowerInvariant()] = room;
            }

            var type = r.Type ?? AssetType.Individual;
            toAdd.Add(new Asset
            {
                Code = code,
                Name = name,
                Category = PeruTime.Clean(r.Category),
                Type = type,
                Quantity = type == AssetType.Individual ? 1 : Math.Max(0, r.Quantity ?? 1),
                Room = room,
                Condition = r.Condition ?? AssetCondition.Good,
                Verification = VerificationStatus.Pending,
                Notes = PeruTime.Clean(r.Notes)
            });
        }

        await assets.AddRangeAsync(toAdd);
        await uow.SaveChangesAsync();
        return new ImportResultDto(toAdd.Count, skipped);
    }

    private AssetLog NewLog(string action, string? detail = null) =>
        new() { At = DateTime.UtcNow, User = user.Name, Action = action, Detail = detail };

    private static int InitialQuantity(AssetType type, int? quantity)
    {
        // Un bien individual es una sola pieza con etiqueta propia, sin importar lo que envíe el cliente.
        if (type == AssetType.Individual) return 1;

        if (quantity is < 0)
            throw new BusinessRuleException("La cantidad de un lote no puede ser negativa.");
        return quantity ?? 1;
    }

    private static int UpdatedQuantity(AssetType type, Asset asset, int? quantity)
    {
        if (type == AssetType.Individual) return 1;

        if (quantity.HasValue)
        {
            if (quantity.Value < 0)
                throw new BusinessRuleException("La cantidad de un lote no puede ser negativa.");
            return quantity.Value;
        }

        // Al cambiar de tipo sin enviar cantidad, se conserva la actual (nunca negativa).
        return Math.Max(0, asset.Quantity);
    }

    private static void CheckPhoto(string? dataUrl)
    {
        if (dataUrl is { Length: > MaxPhotoChars }) throw new BusinessRuleException("La foto es demasiado grande.");
    }

    private async Task CheckRoomAsync(int? roomId)
    {
        if (roomId.HasValue && await rooms.GetByIdAsync(roomId.Value) is null)
            throw new BusinessRuleException("La sala indicada no existe.");
    }
}
