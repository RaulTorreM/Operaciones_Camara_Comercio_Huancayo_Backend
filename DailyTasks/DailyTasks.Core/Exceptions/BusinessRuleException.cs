namespace DailyTasks.Core.Exceptions;

/// <summary>Regla de negocio incumplida (400).</summary>
public class BusinessRuleException(string message) : Exception(message);
