namespace DailyTasks.Core.Exceptions;

/// <summary>Conflicto de estado o concurrencia (409).</summary>
public class ConflictException(string message) : Exception(message);
