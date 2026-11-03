namespace Inventario.Core.Exceptions;

/// <summary>Credenciales incorrectas (401).</summary>
public class UnauthorizedException(string message) : Exception(message);
