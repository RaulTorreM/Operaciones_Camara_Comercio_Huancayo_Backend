namespace DailyTasks.API.Security;

/// <summary>
/// Constantes de autenticación del módulo DailyTasks.
/// Usa su propio esquema JWT: un token del inventario no sirve aquí.
/// </summary>
public static class DailyTasksAuth
{
    public const string Scheme = "DailyTasks";

    public const string UserPolicy = "DailyTasksUser";

    public const string ManagerPolicy = "DailyTasksManager";
}
