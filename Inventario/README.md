# Módulo de Inventario

Este directorio contiene el código aislado del inventario dentro del backend
independiente de Operaciones.

- Host: `Inventario.API`
- Base de datos: `cch_operaciones`
- Esquema: `inventory`
- Script de producción: `migrations-inventory.sql`

La configuración, el despliegue y los endpoints están documentados en el
[README principal](../README.md).

## Migraciones

```powershell
dotnet ef database update `
  --project Inventario\Inventario.Infrastructure `
  --startup-project Inventario\Inventario.API `
  --context InventarioDbContext
```

En producción se recomienda `Inventario__AutoMigrate=false` y aplicar
`migrations-inventory.sql` manualmente.
