# Migraciones del inventario

Las migraciones de EF Core del inventario viven aquí y se aplican **solo** al
esquema `inventory` de la base de datos `cch_operaciones`.

Desde la raíz del repo:

```bash
# crear una migración
dotnet ef migrations add NombreMigracion \
  --project Inventario/Inventario.Infrastructure \
  --startup-project Inventario/Inventario.API \
  --context InventarioDbContext \
  --output-dir Migrations

# aplicar en local
dotnet ef database update \
  --project Inventario/Inventario.Infrastructure \
  --startup-project Inventario/Inventario.API \
  --context InventarioDbContext
```

En producción se aplica `Inventario/migrations-inventory.sql` (idempotente) y se
deja `Inventario:AutoMigrate = false`. Ver `README.md`.
