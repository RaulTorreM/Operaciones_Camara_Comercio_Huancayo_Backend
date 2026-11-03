namespace Inventario.Core.Exceptions;

/// <summary>Conflicto con algo que ya existe (409). Puede indicar el id del registro existente.</summary>
public class ConflictException(string message, int? existingId = null) : Exception(message)
{
    public int? ExistingId { get; } = existingId;
}
