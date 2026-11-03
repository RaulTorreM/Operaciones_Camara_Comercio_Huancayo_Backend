using Microsoft.Extensions.Configuration;

namespace Inventario.Infrastructure.Data;

public static class ConnectionStringResolver
{
    /// <summary>Railway entrega DATABASE_URL (postgresql://user:pass@host:port/db); Npgsql necesita otro formato.</summary>
    public static string Resolve(IConfiguration cfg)
    {
        // Backend independiente: base propia de operaciones (inventario + futuros módulos).
        var db = cfg.GetConnectionString("cch_operaciones");
        if (string.IsNullOrWhiteSpace(db)) db = cfg.GetConnectionString("inventario_db");
        if (!string.IsNullOrWhiteSpace(db)) return db;

        var url = cfg["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(url))
            return cfg.GetConnectionString("Default")
                   ?? "Host=localhost;Port=5432;Database=cch_operaciones;Username=postgres;Password=postgres";

        var u = new Uri(url);
        var ui = u.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(ui[0]);
        var pass = ui.Length > 1 ? Uri.UnescapeDataString(ui[1]) : "";
        var port = u.Port > 0 ? u.Port : 5432;
        return $"Host={u.Host};Port={port};Database={u.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Prefer;Trust Server Certificate=true";
    }
}
