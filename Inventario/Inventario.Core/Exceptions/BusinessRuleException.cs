namespace Inventario.Core.Exceptions;

/// <summary>Se incumple una regla del negocio o dato inválido (400).</summary>
public class BusinessRuleException(string message) : Exception(message);
