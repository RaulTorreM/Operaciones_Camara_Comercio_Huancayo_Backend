namespace DailyTasks.Core.Exceptions;

/// <summary>Credenciales inválidas o usuario inactivo (401).</summary>
public class UnauthorizedException(string message) : Exception(message);
