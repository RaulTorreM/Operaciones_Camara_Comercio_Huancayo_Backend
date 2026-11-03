namespace Inventario.Core.Exceptions;

/// <summary>No se encontró lo solicitado (404).</summary>
public class NotFoundException(string message) : Exception(message);
