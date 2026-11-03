# Backend de Inventario / Operaciones

Backend independiente para los módulos de **Inventario** y **DailyTasks (pendientes diarios)**
de la Cámara de Comercio de Huancayo. Ambos módulos viven en el mismo host y en la misma
base PostgreSQL (`cch_operaciones`), pero cada uno tiene su propio esquema, su propia
identidad y su propio JWT.

La web principal y el módulo de Asociados viven en el backend separado
`Web_Camara_Comercio_Huancayo_Backend`.

## Estructura

```text
Web_Camara_Comercio_Huancayo_Operaciones_Backend/
├── CamaraComercio.Inventario.sln
├── Dockerfile
├── .env.example
├── docs/
│   └── DAILYTASKS_FRONTEND_HANDOFF.md
├── Inventario/
│   ├── Inventario.API/              <- host ejecutable + capa API de Inventario
│   ├── Inventario.Application/
│   ├── Inventario.Core/
│   ├── Inventario.Infrastructure/
│   │   └── Migrations/
│   ├── Inventario.Tests/
│   ├── migrations-inventory.sql
│   └── README.md
├── DailyTasks/
│   ├── DailyTasks.API/              <- controladores, hub SignalR y auth de DailyTasks
│   ├── DailyTasks.Application/      <- DTOs, permisos, servicios e informes
│   ├── DailyTasks.Core/             <- dominio de pendientes diarios
│   ├── DailyTasks.Infrastructure/   <- EF Core, JWT, BCrypt, seed y migraciones
│   ├── DailyTasks.Tests/            <- pruebas unitarias
│   └── migrations-dailytasks.sql
└── README.md
```

`Inventario.API` es el host: registra ambos módulos, sus JWT, CORS, rate limiting,
SignalR, healthchecks y los seeds iniciales. La capa API de DailyTasks vive en
`DailyTasks.API` (controladores, hub y autenticación) y se conecta al host mediante
`AddDailyTasksApi()` y `MapDailyTasksApi()`.

## Requisitos

- .NET 8 SDK
- PostgreSQL 14+
- Cloudinary (para las fotos del inventario)

## Configuración local

1. Copia `.env.example` a `.env`.
2. Define la conexión a `cch_operaciones`:

```text
ConnectionStrings__cch_operaciones=Host=localhost;Port=5432;Database=cch_operaciones;Username=postgres;Password=postgres
```

3. Define una clave JWT propia de al menos 32 caracteres para cada módulo:

```text
Inventario__Jwt__Key=una-clave-larga-y-propia-del-inventario
DailyTasks__Jwt__Key=una-clave-larga-y-propia-de-dailytasks
```

4. Define Cloudinary y el usuario administrador:

```text
Cloudinary__CloudName=...
Cloudinary__ApiKey=...
Cloudinary__ApiSecret=...
ADMIN_USER=admin
ADMIN_PASSWORD=cambiame-en-produccion

# Respaldo para DailyTasks (solo si algún usuario no define su propia contraseña).
DAILYTASKS_SEED_PASSWORD=cambiame-en-produccion

# Credenciales únicas por usuario (recomendado). Incluye los 11 usuarios:
# DailyTasks__SeedUsers__0__Username=milagros
# DailyTasks__SeedUsers__0__FullName=Milagros
# DailyTasks__SeedUsers__0__Area=GESTION_ASOCIADO
# DailyTasks__SeedUsers__0__Role=USER
# DailyTasks__SeedUsers__0__Password=una-clave-unica-para-milagros
# ... repetir para brizeth, arely, maria, maribel, johonny, patricia,
#     ginger, carmen, german y klaus (Role=MANAGER, Area=GERENCIA_GENERAL).
```

5. Ejecuta:

```powershell
dotnet run --project Inventario\Inventario.API
```

En Development, `appsettings.Development.json` deja `Inventario:AutoMigrate=true` y
`DailyTasks:AutoMigrate=true`, así que la primera ejecución crea los esquemas `inventory`
y `dailytasks`, siembra el administrador y los 11 usuarios de pendientes.
La API queda en `http://localhost:5095`.

## Base de datos

- Base compartida: `cch_operaciones`.
- Inventario: esquema `inventory`, historial `inventory."__EFMigrationsHistory"`.
- DailyTasks: esquema `dailytasks`, historial `dailytasks."__EFMigrationsHistory"`.
- Scripts de producción idempotentes:
  - `Inventario/migrations-inventory.sql`
  - `DailyTasks/migrations-dailytasks.sql`

### Aplicar migraciones en producción

1. Haz backup de la base.
2. Ejecuta ambos scripts:

```powershell
psql "$env:DATABASE_URL" -f Inventario\migrations-inventory.sql
psql "$env:DATABASE_URL" -f DailyTasks\migrations-dailytasks.sql
```

3. Deja `Inventario__AutoMigrate=false` y `DailyTasks__AutoMigrate=false`.
4. Define `ADMIN_USER`, `ADMIN_PASSWORD` y las credenciales de DailyTasks
   (`DAILYTASKS_SEED_PASSWORD` como respaldo o `DailyTasks__SeedUsers__N__Password` por usuario).
5. Arranca el backend una vez para crear el administrador y los 11 usuarios de pendientes.

Los seeds son idempotentes: no duplican usuarios ni datos si ya existen.

## Autenticación

Cada módulo tiene su propio esquema JWT; un token de Inventario no sirve en DailyTasks
y viceversa.

### Inventario

- Login: `POST /api/inventario/auth/login`
- Token propio del inventario, separado del token de la web.
- Usuarios en `inventory."Users"`.
- Roles admitidos: `ROLE_ADMIN`.

### DailyTasks

- Login: `POST /api/dailytasks/auth/login`
- Token propio de DailyTasks, separado del inventario y de la web.
- Usuarios en `dailytasks."Users"` (11 usuarios iniciales, sin CRUD en el MVP).
- Roles: `USER` y `MANAGER` (Klaus, Gerencia General).
- SignalR: `/hubs/dailytasks` (acepta `access_token` en query string solo para el hub).

## Endpoints

| Recurso | Ruta |
|---|---|
| Login | `POST /api/inventario/auth/login` |
| Bienes | `/api/inventario/assets` |
| Salas | `/api/inventario/rooms` |
| Consumibles | `/api/inventario/supplies` |
| Préstamos | `/api/inventario/loans` |
| Resumen | `/api/inventario/summary` |
| Consulta pública por etiqueta | `GET /api/inventario/public/assets/by-code?code=...` |
| Health | `GET /health` |
| Readiness | `GET /health/ready` |

`/health/ready` comprueba la conexión a PostgreSQL e informa si Cloudinary está configurado.

### DailyTasks

| Recurso | Ruta |
|---|---|
| Login | `POST /api/dailytasks/auth/login` |
| Usuario actual | `GET /api/dailytasks/auth/me` |
| Cambiar contraseña | `POST /api/dailytasks/auth/change-password` |
| Lista con filtros y paginación | `GET /api/dailytasks/tasks` |
| Crear pendiente | `POST /api/dailytasks/tasks` |
| Detalle | `GET /api/dailytasks/tasks/{id}` |
| Editar título/descripción/prioridad/fecha | `PUT /api/dailytasks/tasks/{id}` |
| Cambiar estado | `PATCH /api/dailytasks/tasks/{id}/status` |
| Reasignar (solo Gerencia) | `PATCH /api/dailytasks/tasks/{id}/assignee` |
| Eliminar lógicamente (solo Gerencia) | `DELETE /api/dailytasks/tasks/{id}` |
| Restaurar (solo Gerencia) | `POST /api/dailytasks/tasks/{id}/restore` |
| Historial/auditoría | `GET /api/dailytasks/tasks/{id}/history` |
| Tablero Kanban (3 columnas) | `GET /api/dailytasks/tasks/board` |
| Calendario | `GET /api/dailytasks/tasks/calendar?from=...&to=...` |
| Cambios para polling de respaldo | `GET /api/dailytasks/tasks/changes?since=...` |
| Catálogo de usuarios | `GET /api/dailytasks/users` |
| Catálogo de áreas | `GET /api/dailytasks/areas` |
| Resumen general | `GET /api/dailytasks/summary` |
| Informe global (solo Gerencia) | `GET /api/dailytasks/reports` |
| Informe propio | `GET /api/dailytasks/reports/me` |
| Auditoría global (solo Gerencia) | `GET /api/dailytasks/audit` |
| Hub en tiempo real | `/hubs/dailytasks` |

Filtros de lista/tablero/calendario: `status`, `priority`, `assignedToId`, `area`,
`createdFrom`, `createdTo`, `dueFrom`, `dueTo`, `dueDate`, `overdue`, `q`,
`page`, `pageSize`, `sortBy`, `sortDir`, `includeDeleted` (solo Gerencia).

La auditoría global (`/audit`) consulta la tabla existente
`dailytasks."TaskHistory"`; **no crea una tabla nueva**. Acepta `from`, `to`,
`userId`, `taskId`, `action`, `field`, `q`, `sensitiveOnly`, `page` y `pageSize`.
`TaskHistory` ya registra usuario, fecha, acción, campo y valores anterior/nuevo.

## Frontend

El frontend de Inventario es independiente. Solo debe cambiar la URL base:

```text
VITE_API_URL=http://localhost:5095/api/inventario
```

El frontend de DailyTasks usa:

```text
VITE_API_URL=http://localhost:5095/api/dailytasks
VITE_HUB_URL=http://localhost:5095/hubs/dailytasks
```

En producción:

```text
VITE_API_URL=https://<backend-operaciones>/api/dailytasks
VITE_HUB_URL=https://<backend-operaciones>/hubs/dailytasks
```

Agrega cada dominio en `Inventario__FrontendURL` y `DailyTasks__FrontendURL` para CORS.

## Tests

```powershell
dotnet test Inventario\Inventario.Tests\Inventario.Tests.csproj -c Release
dotnet test DailyTasks\DailyTasks.Tests\DailyTasks.Tests.csproj -c Release
```

## Docker

```powershell
docker build -t cch-inventario .
docker run --rm -p 8080:8080 --env-file .env cch-inventario
```

## Módulo DailyTasks

Módulo de pendientes diarios aislado dentro del mismo backend, con esquema e identidad propios.

### Reglas principales

- Estados: `Pending`, `InProgress`, `Done`.
- Prioridades: `Low`, `Medium`, `High`.
- Usuarios regulares (`USER`): crean, editan y cambian el estado solo de sus propios
  pendientes. No pueden asignar, eliminar ni modificar tareas ajenas.
- Gerencia General (`MANAGER`, Klaus): puede crear, editar, priorizar, cambiar fechas,
  estado, asignar, supervisar, eliminar/restaurar e informar sobre cualquier pendiente.
- Al crear un pendiente, el responsable por defecto es el usuario autenticado y el
  vencimiento por defecto es el día actual (zona horaria Perú, UTC-5).
- El estado inicial siempre es `Pending`.
- Las tareas vencidas se calculan visualmente (`isOverdue`, `daysOverdue`); no cambian
  de estado automáticamente.
- El historial es append-only: usuario, fecha/hora UTC, acción, campo, valor anterior
  y valor nuevo. Se agrupa por `changeGroupId` cuando una edición toca varios campos.
- Un usuario regular solo puede consultar el historial de sus propias tareas; Gerencia
  General puede consultar el historial de cualquier pendiente.
- Eliminación lógica solo para Gerencia, con restauración y registro en historial.
- Los usuarios iniciales se siembran una vez; no hay CRUD de usuarios en el MVP.
- Cada usuario puede tener su propia contraseña con `DailyTasks__SeedUsers__N__Password`.
  Si no se define, se usa `DAILYTASKS_SEED_PASSWORD` como respaldo.
- Para aplicar las contraseñas del seed a usuarios ya existentes, usa
  `DAILYTASKS_SEED_UPDATE_PASSWORDS=true` una sola vez y luego vuelve a `false`.

### Vistas soportadas por la API

- Lista con filtros, ordenamiento y paginación.
- Tablero Kanban con columnas `Pending`, `InProgress`, `Done`.
- Calendario por fecha de vencimiento, con navegación mensual y filtro por día.
- Resumen general y gráficos por estado, prioridad y área.
- Informe global para Gerencia y informe propio para cada usuario.
- Actualización en tiempo real por SignalR y polling de respaldo en `/tasks/changes`.

### Zona horaria

- `createdAt`, `updatedAt`, `completedAt` y `changedAt` se guardan en UTC.
- `createdOn`, `dueDate` y `completedOn` son fechas de Perú (UTC-5).
- El cálculo de vencidos usa la fecha actual de Perú.
