namespace Inventario.Core.Exceptions;

/// <summary>Un servicio externo necesario no está disponible (503).</summary>
public class StorageUnavailableException(string message) : Exception(message);
