namespace DailyTasks.Core.Exceptions;

/// <summary>Recurso inexistente (404).</summary>
public class NotFoundException(string message) : Exception(message);
