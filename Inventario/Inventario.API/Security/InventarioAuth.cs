namespace Inventario.API.Security;

/// <summary>
/// Constantes de autenticación del módulo de inventario.
/// El inventario usa su PROPIO esquema de token: un token de la web no sirve aquí
/// y un token del inventario no sirve en los endpoints de la web.
/// </summary>
public static class InventarioAuth
{
    public const string Scheme = "Inventario";

    public const string AdminPolicy = "InventarioAdmin";
}
