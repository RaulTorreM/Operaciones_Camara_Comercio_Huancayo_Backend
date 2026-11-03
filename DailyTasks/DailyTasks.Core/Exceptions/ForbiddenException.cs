namespace DailyTasks.Core.Exceptions;

/// <summary>El usuario autenticado no tiene permiso sobre el recurso (403).</summary>
public class ForbiddenException(string message) : Exception(message);
