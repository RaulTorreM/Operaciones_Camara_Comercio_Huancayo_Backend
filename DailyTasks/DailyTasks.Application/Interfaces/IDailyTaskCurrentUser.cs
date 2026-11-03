namespace DailyTasks.Application.Interfaces;

/// <summary>Usuario autenticado que realiza la petición. Lo implementa la capa API.</summary>
public interface IDailyTaskCurrentUser
{
    int Id { get; }
    string Name { get; }
    string Role { get; }
    string Area { get; }
    bool IsManager { get; }
}
